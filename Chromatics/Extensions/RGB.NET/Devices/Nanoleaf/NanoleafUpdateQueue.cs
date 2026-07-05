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

        private readonly Lock _lock = new();
        private volatile bool _shuttingDown;
        private volatile bool _perDeviceDisable;

        private UdpClient _udp;
        private IPEndPoint _streamEndpoint;
        private NanoleafOriginalState _original;
        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // Dirty-check: skip a send when nothing changed, but force one at
        // least once per second so a silent stream can't drop the controller
        // out of extControl. Tracked in stopwatch ticks to avoid the clock
        // helpers that are unavailable in some contexts.
        private byte[] _lastFrame;
        private long _lastSendTicks;
        private long _lastWatchdogTicks;
        private static readonly long OneSecondTicks = TimeSpan.FromSeconds(1).Ticks;
        private static readonly long WatchdogIntervalTicks = TimeSpan.FromSeconds(30).Ticks;

        public NanoleafUpdateQueue(IDeviceUpdateTrigger updateTrigger, NanoleafClientDefinition def, IReadOnlyList<int> panelOrder, Func<UdpClient> udpFactory = null)
            : base(updateTrigger)
        {
            _def = def;
            _panelOrder = panelOrder;
            _rest = new NanoleafRestClient(def.Endpoint.Address.ToString(), def.Endpoint.Port, def.AuthToken);
            _udpFactory = udpFactory ?? (() => new UdpClient(0));
        }

        public void BeginShutdown() => _shuttingDown = true;
        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;
        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction) => _perDeviceBrightness = correction;

        // Capture device-owned state, then enter streaming mode. turnOnIfOff
        // false for persisted-disabled devices: capture their state but never
        // wake them (the LIFX subtlety - a woken bulb poisons the next
        // capture and the disable stops turning it off).
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

            if (turnOnIfOff && !state.On)
                await _rest.SetOnAsync(true, ct).ConfigureAwait(false);

            if (!turnOnIfOff && !state.On)
            {
                // Persisted-disabled and off: leave it off, don't stream.
                return;
            }

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
                _streamEndpoint = new IPEndPoint(addr, info.Port);
                _def.StreamEndpoint = _streamEndpoint;
                _udp ??= _udpFactory();
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {_def.Label}: failed to enter streaming mode ({ex.Message}).");
            }
        }

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            lock (_lock)
            {
                if (_shuttingDown || _perDeviceDisable) return true;
                if (_streamEndpoint == null || _udp == null) return true;

                try
                {
                    // Map incoming LedId->Color to panel colours in the fixed
                    // panelOrder, applying brightness corrections.
                    var byLed = new Dictionary<LedId, Color>();
                    foreach (var (key, color) in dataSet)
                        if (key is LedId led) byLed[led] = color;

                    int globalPct = GlobalBrightnessCorrection.Instance.BrightnessPercent;
                    int perDevicePct = _perDeviceBrightness?.BrightnessPercent ?? 100;
                    double scale = (globalPct / 100.0) * (perDevicePct / 100.0);

                    var frameInput = new List<(int, byte, byte, byte)>(_panelOrder.Count);
                    for (int i = 0; i < _panelOrder.Count; i++)
                    {
                        var ledId = (LedId)((int)LedId.Custom1 + i);
                        Color c = byLed.TryGetValue(ledId, out var col) ? col : new Color(0, 0, 0);
                        byte r = (byte)Math.Clamp(c.R * 255.0 * scale, 0, 255);
                        byte g = (byte)Math.Clamp(c.G * 255.0 * scale, 0, 255);
                        byte b = (byte)Math.Clamp(c.B * 255.0 * scale, 0, 255);
                        frameInput.Add((_panelOrder[i], r, g, b));
                    }

                    var frame = NanoleafStreamProtocol.EncodeFrame(frameInput);
                    long now = Environment.TickCount64 * TimeSpan.TicksPerMillisecond;

                    bool changed = _lastFrame == null || !FramesEqual(_lastFrame, frame);
                    bool keepAliveDue = now - _lastSendTicks >= OneSecondTicks;

                    if (changed || keepAliveDue)
                    {
                        _udp.Send(frame, frame.Length, _streamEndpoint);
                        _lastFrame = frame;
                        _lastSendTicks = now;
                    }

                    // Streaming watchdog on the same tick cadence, off the
                    // hot path: fire a fire-and-forget REST check every 30s.
                    if (now - _lastWatchdogTicks >= WatchdogIntervalTicks)
                    {
                        _lastWatchdogTicks = now;
                        _ = WatchdogCheckAsync();
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {_def.Label}: stream send failed ({ex.Message}).", forwardToSentry: false);
                }

                return true;
            }
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
        }

        private static bool FramesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        // Restore device-owned state on close / disable. Exits streaming
        // (re-selecting the captured scene does this implicitly), re-selects
        // the scene by name, re-asserts brightness + power with retry, and
        // verify-repairs the power state (the Hue shape). Best-effort.
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
            try { _udp?.Close(); } catch { }
            try { _udp?.Dispose(); } catch { }
            _udp = null;
            base.Dispose();
        }
    }
}
