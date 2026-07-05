using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    // Per-controller update queue. Streams extControl v2 frames over UDP,
    // captures/restores device-owned state via REST, and self-heals the
    // streaming session with a periodic watchdog. Structured after
    // LifxUpdateQueue: fire-and-forget data plane, retrying control plane,
    // _shuttingDown / _perDeviceDisable gates.
    public class NanoleafUpdateQueue : UpdateQueue
    {
        private readonly NanoleafClientDefinition _def;
        private readonly NanoleafRestClient _rest;
        private readonly Func<UdpClient> _udpFactory;
        private readonly IReadOnlyList<int> _panelOrder; // LedId.Custom1..N -> panelId

        // Guards the _streamEndpoint/_udp handoff between the watchdog's
        // re-entry (thread pool) and Update's per-tick snapshot (trigger
        // thread). Everything else on the hot path is trigger-thread-only.
        private readonly Lock _lock = new();
        private volatile bool _shuttingDown;
        private volatile bool _perDeviceDisable;
        private volatile bool _watchdogInFlight;

        private UdpClient _udp;
        private IPEndPoint _streamEndpoint;
        private NanoleafOriginalState _original;
        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // Hot-path scratch, allocated once: Update runs at up to 20Hz per
        // controller, so per-tick Dictionary/List/byte[] churn adds up.
        // _lastInput doubles as the dirty check; _lastFrame is the encoded
        // buffer the keep-alive resends. _hasLastInput is volatile so
        // ResetCache (UI thread, on re-enable) can force a full send
        // without taking a lock on the trigger thread's path.
        private readonly Color[] _colorScratch;
        private readonly (int panelId, byte r, byte g, byte b)[] _lastInput;
        private volatile bool _hasLastInput;
        private byte[] _lastFrame;
        private long _lastSendMs;
        private long _lastWatchdogMs;

        public NanoleafUpdateQueue(IDeviceUpdateTrigger updateTrigger, NanoleafClientDefinition def, IReadOnlyList<int> panelOrder, Func<UdpClient> udpFactory = null)
            : base(updateTrigger)
        {
            _def = def;
            _panelOrder = panelOrder;
            _rest = new NanoleafRestClient(def.Endpoint.Address.ToString(), def.Endpoint.Port, def.AuthToken);
            _udpFactory = udpFactory ?? (() => new UdpClient(0));
            _colorScratch = new Color[panelOrder.Count];
            _lastInput = new (int, byte, byte, byte)[panelOrder.Count];
        }

        public void BeginShutdown() => _shuttingDown = true;
        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;
        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction) => _perDeviceBrightness = correction;

        // Drop the dirty cache so the next Update sends a full frame even
        // when the colours match what was on the panels before a disable.
        public void ResetCache()
        {
            _hasLastInput = false;
            _lastFrame = null;
        }

        // Capture device-owned state, then enter streaming mode. A
        // persisted-disabled device gets its state captured for a later
        // enable but is otherwise left alone: no power-on AND no streaming
        // handshake, because entering extControl stops whatever scene the
        // controller is showing - exactly the "never touch it" contract the
        // Mapping-tab disable promises.
        public async Task CaptureAndStartAsync(bool turnOnIfOff = true, CancellationToken ct = default)
        {
            NanoleafState state = null;
            for (int attempt = 0; attempt < 3 && state == null; attempt++)
            {
                state = await _rest.GetStateAsync(ct).ConfigureAwait(false);
                if (state == null && attempt < 2) await Task.Delay(300 * (attempt + 1), ct).ConfigureAwait(false);
            }

            if (state == null)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {_def.Label}: could not read state to capture; restore on close will be skipped.");
                return;
            }

            _original = new NanoleafOriginalState
            {
                On = state.On,
                Brightness = state.Brightness,
                SelectedEffect = state.SelectedEffect,
            };

            if (!turnOnIfOff) return;

            if (!state.On)
                await _rest.SetOnAsync(true, ct).ConfigureAwait(false);

            await EnterStreamingAsync(ct).ConfigureAwait(false);
        }

        // Re-enable path (Mapping tab): power the controller on and
        // re-negotiate extControl. Always re-runs the handshake - after a
        // disable the restore re-selected the user's scene, which exits
        // streaming, so a stale _streamEndpoint would mean UDP frames the
        // firmware ignores.
        public async Task EnsureStreamingAsync(CancellationToken ct = default)
        {
            if (_shuttingDown || _perDeviceDisable) return;
            await _rest.SetOnAsync(true, ct).ConfigureAwait(false);
            await EnterStreamingAsync(ct).ConfigureAwait(false);
        }

        private async Task EnterStreamingAsync(CancellationToken ct)
        {
            try
            {
                var info = await _rest.EnableStreamingAsync(ct).ConfigureAwait(false);
                if (info == null) return;
                if (!IPAddress.TryParse(info.Host, out var addr))
                    addr = _def.Endpoint.Address;

                lock (_lock)
                {
                    _streamEndpoint = new IPEndPoint(addr, info.Port);
                    _udp ??= _udpFactory();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {_def.Label}: failed to enter streaming mode ({ex.Message}).");
            }
        }

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            try
            {
                if (_shuttingDown || _perDeviceDisable) return true;

                IPEndPoint streamEndpoint;
                UdpClient udp;
                lock (_lock)
                {
                    streamEndpoint = _streamEndpoint;
                    udp = _udp;
                }
                if (streamEndpoint == null || udp == null) return true;

                int count = _panelOrder.Count;
                Array.Clear(_colorScratch, 0, count);
                foreach (var (key, color) in dataSet)
                {
                    if (key is LedId led)
                    {
                        int idx = (int)led - (int)LedId.Custom1;
                        if (idx >= 0 && idx < count) _colorScratch[idx] = color;
                    }
                }

                int globalPct = GlobalBrightnessCorrection.Instance.BrightnessPercent;
                int perDevicePct = _perDeviceBrightness?.BrightnessPercent ?? 100;
                double scale = (globalPct / 100.0) * (perDevicePct / 100.0);

                bool changed = !_hasLastInput;
                for (int i = 0; i < count; i++)
                {
                    var c = _colorScratch[i];
                    var entry = (_panelOrder[i],
                        (byte)Math.Clamp(c.R * 255.0 * scale, 0, 255),
                        (byte)Math.Clamp(c.G * 255.0 * scale, 0, 255),
                        (byte)Math.Clamp(c.B * 255.0 * scale, 0, 255));
                    if (_lastInput[i] != entry)
                    {
                        changed = true;
                        _lastInput[i] = entry;
                    }
                }
                _hasLastInput = true;

                long nowMs = Environment.TickCount64;
                bool keepAliveDue = nowMs - _lastSendMs >= 1000;

                if (changed || keepAliveDue)
                {
                    if (changed || _lastFrame == null)
                        _lastFrame = NanoleafStreamProtocol.EncodeFrame(_lastInput);
                    udp.Send(_lastFrame, _lastFrame.Length, streamEndpoint);
                    _lastSendMs = nowMs;
                }

                if (nowMs - _lastWatchdogMs >= 30_000 && !_watchdogInFlight)
                {
                    _lastWatchdogMs = nowMs;
                    _watchdogInFlight = true;
                    _ = WatchdogCheckAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {_def.Label}: stream send failed ({ex.Message}).", forwardToSentry: false);
            }

            return true;
        }

        private async Task WatchdogCheckAsync()
        {
            try
            {
                if (_shuttingDown || _perDeviceDisable) return;
                bool streaming = await _rest.IsStreamingAsync().ConfigureAwait(false);
                if (!streaming && !_shuttingDown && !_perDeviceDisable)
                {
                    Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {_def.Label}: streaming stopped (reboot or app conflict); re-entering.", forwardToSentry: false);
                    await EnterStreamingAsync(default).ConfigureAwait(false);
                }
            }
            catch { /* best-effort */ }
            finally
            {
                _watchdogInFlight = false;
            }
        }

        // Restore device-owned state on close / disable. Re-selecting the
        // captured scene by name exits extControl; brightness and power are
        // re-asserted with retry, then the power state is verify-repaired
        // (the Hue shape). Two capture shapes can't fully restore: a scene
        // name of "*ExtControl*" means another app owned the panels when we
        // captured (their live stream is unrecoverable), and an empty name
        // has no scene to select back - both fall through to the
        // brightness/power re-assert, which is the best available.
        public async Task RestoreOriginalStateAsync(CancellationToken ct = default)
        {
            if (_original == null) return;
            try
            {
                if (!string.IsNullOrEmpty(_original.SelectedEffect) &&
                    !string.Equals(_original.SelectedEffect, "*ExtControl*", StringComparison.OrdinalIgnoreCase))
                {
                    await _rest.SelectEffectAsync(_original.SelectedEffect, ct).ConfigureAwait(false);
                }

                await Task.Delay(100, ct).ConfigureAwait(false);
                await _rest.SetBrightnessAsync(_original.Brightness, ct).ConfigureAwait(false);

                await Task.Delay(100, ct).ConfigureAwait(false);
                await _rest.SetOnAsync(_original.On, ct).ConfigureAwait(false);

                // Verify + repair: re-assert power if the controller
                // acknowledged without applying.
                await Task.Delay(250, ct).ConfigureAwait(false);
                var live = await _rest.GetOnAsync(ct).ConfigureAwait(false);
                if (live != null && live != _original.On)
                    await _rest.SetOnAsync(_original.On, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {_def.Label}: failed to restore original state ({ex.Message}).", forwardToSentry: false);
            }
        }

        public override void Dispose()
        {
            lock (_lock)
            {
                try { _udp?.Close(); } catch { }
                try { _udp?.Dispose(); } catch { }
                _udp = null;
                _streamEndpoint = null;
            }
            base.Dispose();
        }
    }
}
