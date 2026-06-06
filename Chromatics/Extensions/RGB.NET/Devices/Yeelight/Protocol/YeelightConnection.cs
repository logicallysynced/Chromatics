using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight.Protocol
{
    // Single TCP connection to one Yeelight bulb on its LAN control port
    // (55443). Wraps a TcpClient and exposes the small subset of JSON-over-
    // TCP commands Chromatics actually paints with: set_rgb, set_bright,
    // set_power, set_ct_abx.
    //
    // Music Mode caveat (load-bearing for paint rate):
    //
    //   Yeelight's LAN protocol caps outbound commands at 60 per minute
    //   per bulb in the normal direction. That's well below any usable
    //   paint rate. Music Mode flips the direction — we tell the bulb our
    //   IP + a listening TCP port via `set_music`, the bulb opens a TCP
    //   connection BACK to us, and that reverse channel has no rate cap.
    //   Once Music Mode is on we can send 60+Hz updates without the bulb
    //   silently dropping frames.
    //
    //   Set up by YeelightConnection.EnterMusicModeAsync. If Music Mode
    //   fails (firewall blocks the reverse TCP, NAT in the way for a
    //   user on a corporate LAN, etc.) we fall back to the outbound
    //   connection and let the LIFX-equivalent throttle in
    //   YeelightUpdateQueue keep us under the rate cap.
    //
    // JSON wire format example (one line, newline-terminated):
    //
    //   {"id":1,"method":"set_rgb","params":[16711680,"smooth",30]}\r\n
    public sealed class YeelightConnection : IDisposable
    {
        // Yeelight's LAN protocol terminator. Every command is one JSON
        // object followed by CR+LF. The bulb parses one line at a time.
        private static readonly byte[] LineTerminator = new byte[] { 0x0D, 0x0A };

        // We use Smooth transitions with a tiny duration (30ms). Sudden
        // jumps in colour look harsh on Yeelight Color 1S / Color 1 because
        // the firmware step-functions otherwise; 30ms smooth blends frames
        // into a continuous gradient without adding perceptible lag at
        // 30Hz paint cadence. Smooth durations below 30ms are silently
        // rounded up to 30 by the firmware.
        private const int SmoothDurationMs = 30;

        private readonly IPEndPoint _endpoint;
        private TcpClient _client;
        private NetworkStream _stream;
        private TcpClient _musicClient;
        private NetworkStream _musicStream;
        private int _nextId;

        public YeelightConnection(IPEndPoint endpoint)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public IPEndPoint Endpoint => _endpoint;
        public bool IsConnected => _client?.Connected == true;
        public bool IsMusicMode => _musicClient?.Connected == true;

        // Establish the outbound control channel. Idempotent — if we're
        // already connected, no-op. Caller should not assume Music Mode is
        // on; use EnterMusicModeAsync separately and let it fall back.
        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (IsConnected) return;
            _client?.Dispose();
            _client = new TcpClient { NoDelay = true };
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            try
            {
                await _client.ConnectAsync(_endpoint.Address, _endpoint.Port, cts.Token).ConfigureAwait(false);
                _stream = _client.GetStream();
            }
            catch
            {
                _client?.Dispose();
                _client = null;
                _stream = null;
                throw;
            }
        }

        // Hand the bulb our IP + a local listening TCP port; the bulb will
        // open a TCP connection back to us. Returns true if Music Mode is
        // active afterwards. Falls back gracefully on any failure — the
        // outbound channel remains usable.
        //
        // We accept the reverse connection synchronously up to `acceptTimeout`,
        // then store the resulting socket. From that point on, command sends
        // go down the reverse channel; the outbound channel stays open as
        // a control / fallback path.
        public async Task<bool> EnterMusicModeAsync(IPAddress localAddress, TimeSpan acceptTimeout, CancellationToken cancellationToken = default)
        {
            if (!IsConnected) return false;
            if (localAddress == null) throw new ArgumentNullException(nameof(localAddress));

            // Bind a TCP listener on a free port. We loopback this to the
            // bulb so it knows where to reach us.
            var listener = new TcpListener(IPAddress.Any, 0);
            try
            {
                listener.Start();
                int listenPort = ((IPEndPoint)listener.LocalEndpoint).Port;

                // Fire `set_music` on the outbound channel. Params:
                //   action: 1 (turn music mode on)
                //   host:   our IP as the bulb sees us
                //   port:   our listening port
                int id = NextId();
                string payload = $"{{\"id\":{id},\"method\":\"set_music\",\"params\":[1,\"{localAddress}\",{listenPort}]}}";
                await SendRawAsync(_stream, payload, cancellationToken).ConfigureAwait(false);

                // Wait for the bulb to dial back. Yeelight bulbs typically
                // connect in <500ms when on the same subnet.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(acceptTimeout);

                Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
                Task winner = await Task.WhenAny(acceptTask, Task.Delay(acceptTimeout, cts.Token)).ConfigureAwait(false);

                if (winner == acceptTask && acceptTask.IsCompletedSuccessfully)
                {
                    _musicClient = acceptTask.Result;
                    _musicClient.NoDelay = true;
                    _musicStream = _musicClient.GetStream();
                    return true;
                }

                // Reverse connect didn't happen in time. Tell the bulb to
                // turn Music Mode back off so we don't sit in a half-state.
                try
                {
                    int offId = NextId();
                    string off = $"{{\"id\":{offId},\"method\":\"set_music\",\"params\":[0]}}";
                    await SendRawAsync(_stream, off, cancellationToken).ConfigureAwait(false);
                }
                catch { /* best-effort cleanup */ }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { listener.Stop(); } catch { /* ignore */ }
            }
        }

        // Send one packed 24-bit RGB value. Encoded as `(R<<16) | (G<<8) | B`,
        // matching the protocol's `set_rgb` integer parameter.
        public Task SetRgbAsync(byte r, byte g, byte b, CancellationToken cancellationToken = default)
        {
            int packed = (r << 16) | (g << 8) | b;
            // Yeelight rejects rgb=0 (it's the "uninitialised" sentinel in
            // their state machine). Map it to (0,0,1) which is visually
            // indistinguishable from off but accepted by firmware.
            if (packed == 0) packed = 1;
            int id = NextId();
            string payload = $"{{\"id\":{id},\"method\":\"set_rgb\",\"params\":[{packed},\"smooth\",{SmoothDurationMs}]}}";
            return SendCommandAsync(payload, cancellationToken);
        }

        public Task SetBrightnessAsync(int percent, CancellationToken cancellationToken = default)
        {
            percent = Math.Clamp(percent, 1, 100);
            int id = NextId();
            string payload = $"{{\"id\":{id},\"method\":\"set_bright\",\"params\":[{percent},\"smooth\",{SmoothDurationMs}]}}";
            return SendCommandAsync(payload, cancellationToken);
        }

        public Task SetPowerAsync(bool on, CancellationToken cancellationToken = default)
        {
            int id = NextId();
            string state = on ? "on" : "off";
            string payload = $"{{\"id\":{id},\"method\":\"set_power\",\"params\":[\"{state}\",\"smooth\",{SmoothDurationMs}]}}";
            return SendCommandAsync(payload, cancellationToken);
        }

        // Background-light variants for dual-element bulbs (Bedside Lamp 2,
        // some ceiling lights). The bulb routes `bg_set_*` commands to the
        // secondary light element while `set_*` keeps driving the main one,
        // so calling both in the same frame addresses them independently.
        // Bulbs that don't have a background element silently ignore these.
        public Task SetBackgroundRgbAsync(byte r, byte g, byte b, CancellationToken cancellationToken = default)
        {
            int packed = (r << 16) | (g << 8) | b;
            if (packed == 0) packed = 1;
            int id = NextId();
            string payload = $"{{\"id\":{id},\"method\":\"bg_set_rgb\",\"params\":[{packed},\"smooth\",{SmoothDurationMs}]}}";
            return SendCommandAsync(payload, cancellationToken);
        }

        public Task SetBackgroundBrightnessAsync(int percent, CancellationToken cancellationToken = default)
        {
            percent = Math.Clamp(percent, 1, 100);
            int id = NextId();
            string payload = $"{{\"id\":{id},\"method\":\"bg_set_bright\",\"params\":[{percent},\"smooth\",{SmoothDurationMs}]}}";
            return SendCommandAsync(payload, cancellationToken);
        }

        public Task SetBackgroundPowerAsync(bool on, CancellationToken cancellationToken = default)
        {
            int id = NextId();
            string state = on ? "on" : "off";
            string payload = $"{{\"id\":{id},\"method\":\"bg_set_power\",\"params\":[\"{state}\",\"smooth\",{SmoothDurationMs}]}}";
            return SendCommandAsync(payload, cancellationToken);
        }

        // Colour temperature in Kelvin (1700-6500 range per Yeelight spec).
        // Used when the incoming colour is grey-scale; produces better white
        // light than synthesising RGB(255,255,255) which the bulb interprets
        // as full RGB and rendering with a slight tint.
        public Task SetColorTemperatureAsync(int kelvin, CancellationToken cancellationToken = default)
        {
            kelvin = Math.Clamp(kelvin, 1700, 6500);
            int id = NextId();
            string payload = $"{{\"id\":{id},\"method\":\"set_ct_abx\",\"params\":[{kelvin},\"smooth\",{SmoothDurationMs}]}}";
            return SendCommandAsync(payload, cancellationToken);
        }

        // Best-effort write to whichever channel is active (Music Mode if up,
        // outbound otherwise). Throws on a fully-broken connection; caller
        // is expected to surface that to the UpdateQueue which will mark the
        // device for reconnect.
        private Task SendCommandAsync(string payload, CancellationToken cancellationToken)
        {
            NetworkStream target = _musicStream ?? _stream;
            if (target == null) throw new InvalidOperationException("Yeelight connection is not open.");
            return SendRawAsync(target, payload, cancellationToken);
        }

        private async Task SendRawAsync(NetworkStream stream, string payload, CancellationToken cancellationToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(LineTerminator, 0, LineTerminator.Length, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private int NextId() => Interlocked.Increment(ref _nextId);

        public void Dispose()
        {
            try { _musicStream?.Dispose(); } catch { /* ignore */ }
            try { _musicClient?.Dispose(); } catch { /* ignore */ }
            try { _stream?.Dispose(); } catch { /* ignore */ }
            try { _client?.Dispose(); } catch { /* ignore */ }
        }
    }
}
