using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.Redragon.Protocol;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Threading;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon
{
    // Single-zone HID-feature-report update queue. All Redragon mice on
    // the OpenRGB protocol family expose exactly one addressable colour,
    // so the queue is simpler than Alienware's per-key path: no dirty
    // index list, no chunked packet packing — just a one-LED diff against
    // the last frame.
    //
    // Per dirty frame we send two HID feature reports:
    //   1. address-write to 0x0449 with [R, G, B]
    //   2. apply (commit)
    //
    // The firmware accepts sustained 30Hz over these two-report frames
    // without dropping. Going faster than that has been seen to cause the
    // firmware to fall back to a stored mode after a few seconds in
    // OpenRGB's own testing.
    public class RedragonUpdateQueue : UpdateQueue
    {
        #region Properties & Fields

        private readonly RedragonClientDefinition _def;
        private readonly HidStream _stream;
        private readonly Lock _lock = new();
        private volatile bool _shuttingDown;
        private volatile bool _perDeviceDisable;
        private bool _initialized;

        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // Last frame's RGB triplet. Used to skip the two-report HID write
        // when nothing changed since the previous tick.
        private byte _lastR = 0xFF;
        private byte _lastG = 0xFF;
        private byte _lastB = 0xFF;
        private bool _haveLastColor;

        #endregion

        #region Constructors

        public RedragonUpdateQueue(IDeviceUpdateTrigger trigger, RedragonClientDefinition def, HidStream stream)
            : base(trigger)
        {
            _def = def;
            _stream = stream;
        }

        #endregion

        #region Lifecycle

        public void BeginShutdown() => _shuttingDown = true;
        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        public void ResetCache()
        {
            lock (_lock)
            {
                _haveLastColor = false;
                _initialized = false;
            }
        }

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _perDeviceBrightness = correction;

        #endregion

        #region Update

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            lock (_lock)
            {
                if (_shuttingDown || _perDeviceDisable) return true;
                if (dataSet.IsEmpty) return true;

                try
                {
                    int globalPct = GlobalBrightnessCorrection.Instance.BrightnessPercent;
                    int perDevicePct = _perDeviceBrightness?.BrightnessPercent ?? 100;
                    double brightnessScale = (globalPct / 100.0) * (perDevicePct / 100.0);

                    // Pick the first matching LED's colour. Single-zone
                    // hardware — Chromatics paints LedId.Mouse1 (or whatever
                    // the device exposes), the firmware shows that one
                    // colour everywhere.
                    Color? source = null;
                    foreach (var (_, color) in dataSet)
                    {
                        source = color;
                        break;
                    }
                    if (source == null) return true;

                    ToRgb255(source.Value, brightnessScale, out byte r, out byte g, out byte b);

                    if (!_initialized)
                    {
                        EnsureInitialized();
                        // Force a write through after init even if the
                        // colour matches what we'd diff against, so the
                        // first frame paints reliably.
                        _haveLastColor = false;
                    }

                    if (_haveLastColor && r == _lastR && g == _lastG && b == _lastB)
                        return true;

                    if (!SendColorFrame(r, g, b))
                        return false;

                    _lastR = r; _lastG = g; _lastB = b;
                    _haveLastColor = true;
                    return true;
                }
                catch (Exception ex)
                {
                    RedragonRGBDeviceProvider.Instance?.Throw(ex);
                    return false;
                }
            }
        }

        #endregion

        #region HID I/O

        // Pin the mouse to profile 0 + Static mode once. Without the mode
        // write the firmware can stay on a hardware-driven animation
        // (wave, rainbow) and our per-frame colour writes get visibly
        // overridden by the animation loop.
        private void EnsureInitialized()
        {
            Span<byte> buf = stackalloc byte[RedragonMouseProtocol.ReportLength];

            RedragonMouseProtocol.BuildSetProfile(buf, 0);
            if (!SendFeatureReport(buf)) return;
            RedragonMouseProtocol.BuildApply(buf);
            if (!SendFeatureReport(buf)) return;

            RedragonMouseProtocol.BuildSetMode(buf, RedragonMouseProtocol.Mode_Static);
            if (!SendFeatureReport(buf)) return;
            RedragonMouseProtocol.BuildApply(buf);
            if (!SendFeatureReport(buf)) return;

            _initialized = true;
        }

        private bool SendColorFrame(byte r, byte g, byte b)
        {
            Span<byte> buf = stackalloc byte[RedragonMouseProtocol.ReportLength];
            RedragonMouseProtocol.BuildSetColor(buf, r, g, b);
            if (!SendFeatureReport(buf)) return false;
            RedragonMouseProtocol.BuildApply(buf);
            return SendFeatureReport(buf);
        }

        // HidSharp's SetFeature wants a byte[]. Allocate once per write —
        // single-LED firmware writes are infrequent enough that pooling
        // would be overkill. Returns false on any I/O failure so the
        // caller can short-circuit the rest of the frame.
        private bool SendFeatureReport(ReadOnlySpan<byte> buf)
        {
            byte[] arr = buf.ToArray();
            try
            {
                _stream.SetFeature(arr);
                return true;
            }
            catch (System.IO.IOException ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[Redragon] {_def.Product}: HID write failed ({ex.Message}); pausing queue.",
                    forwardToSentry: false);
                _shuttingDown = true;
                return false;
            }
            catch (ObjectDisposedException)
            {
                _shuttingDown = true;
                return false;
            }
        }

        #endregion

        #region Helpers

        private static void ToRgb255(Color rgb, double brightnessScale, out byte r, out byte g, out byte b)
        {
            double scale = Math.Clamp(brightnessScale, 0.0, 1.0);
            r = (byte)Math.Round(Math.Clamp(rgb.R, 0.0, 1.0) * 255 * scale);
            g = (byte)Math.Round(Math.Clamp(rgb.G, 0.0, 1.0) * 255 * scale);
            b = (byte)Math.Round(Math.Clamp(rgb.B, 0.0, 1.0) * 255 * scale);
        }

        #endregion
    }
}
