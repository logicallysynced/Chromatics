using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Devices.Lights;
using Color = RGB.NET.Core.Color;
using WinColor = Windows.UI.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    public class DynamicLightingUpdateQueue : UpdateQueue
    {
        #region Properties & Fields

        private readonly DynamicLightingClientDefinition _def;
        private readonly Lock _lock = new();
        private volatile bool _shuttingDown;
        // Per-device disable gate — parity with LIFX/Hue/QMK/Yeelight.
        private volatile bool _perDeviceDisable;

        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // LedId → lamp index, set by DynamicLightingDevice after layout
        // construction. Not built here because the device is the only
        // place that knows about VirtualKey-based semantic mapping.
        private Dictionary<LedId, int> _ledIndexByLedId = new();

        // Per-lamp colour cache. Skips redundant SetSingleColor calls
        // when nothing changed for a lamp this frame.
        private byte[] _lampBytes;
        private int _lampCount;

        // Reusable buffers for SetColorsForIndices to avoid per-frame
        // allocations on the trigger thread.
        private int[] _dirtyIndicesBuffer;
        private WinColor[] _dirtyColorsBuffer;

        #endregion

        #region Constructors

        public DynamicLightingUpdateQueue(IDeviceUpdateTrigger trigger, DynamicLightingClientDefinition def)
            : base(trigger)
        {
            _def = def;
            _lampCount = def.LampArray?.LampCount ?? 0;
            _lampBytes = new byte[Math.Max(_lampCount, 1) * 3];
            _dirtyIndicesBuffer = new int[Math.Max(_lampCount, 1)];
            _dirtyColorsBuffer = new WinColor[Math.Max(_lampCount, 1)];
        }

        #endregion

        #region Lifecycle

        public void BeginShutdown() => _shuttingDown = true;
        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        public void ResetCache()
        {
            lock (_lock)
            {
                Array.Clear(_lampBytes, 0, _lampBytes.Length);
            }
        }

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _perDeviceBrightness = correction;

        // Called by DynamicLightingDevice once layout has been built.
        // We don't construct the map here because the device is the only
        // layer that knows about VirtualKey-based semantic mapping vs
        // Custom1..N fallback.
        public void SetLedIndexMap(Dictionary<LedId, int> map)
        {
            lock (_lock)
            {
                _ledIndexByLedId = map ?? new Dictionary<LedId, int>();
            }
        }

        #endregion

        #region Update

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            lock (_lock)
            {
                if (_shuttingDown || _perDeviceDisable) return true;
                if (dataSet.IsEmpty) return true;

                var lampArray = _def.LampArray;
                if (lampArray == null) return true;

                // Skip the entire frame when the OS has yielded our
                // device to a higher-priority owner (a foreground app
                // with the lighting AppExtension, or system overrides).
                // Without the IsEnabled gate every paint frame would
                // throw an unhandled COMException since the WinRT
                // surface refuses writes when our app isn't the
                // currently-allowed lighting client.
                //
                // IsConnected reports whether the underlying USB / BT
                // device is physically present; IsEnabled reports
                // whether Windows is currently letting us write to
                // it (foreground / background priority gate).
                if (!lampArray.IsConnected || !lampArray.IsEnabled) return true;

                try
                {
                    int globalPct = GlobalBrightnessCorrection.Instance.BrightnessPercent;
                    int perDevicePct = _perDeviceBrightness?.BrightnessPercent ?? 100;
                    double brightnessScale = (globalPct / 100.0) * (perDevicePct / 100.0);

                    int dirtyCount = 0;
                    foreach (var (key, color) in dataSet)
                    {
                        if (!TryResolveLampIndex(key, out int idx)) continue;
                        if (idx < 0 || idx >= _lampCount) continue;

                        ToRgb255(color, brightnessScale, out byte r, out byte g, out byte b);
                        int off = idx * 3;
                        if (_lampBytes[off] == r && _lampBytes[off + 1] == g && _lampBytes[off + 2] == b) continue;

                        _lampBytes[off]     = r;
                        _lampBytes[off + 1] = g;
                        _lampBytes[off + 2] = b;

                        _dirtyIndicesBuffer[dirtyCount] = idx;
                        // WinRT Color.FromArgb takes (a, r, g, b) — alpha 255 since
                        // LampArray ignores alpha but the API still requires it.
                        _dirtyColorsBuffer[dirtyCount] = WinColor.FromArgb(255, r, g, b);
                        dirtyCount++;
                    }

                    if (dirtyCount == 0) return true;

                    // SetColorsForIndices applies all changed lamps in one
                    // WinRT call. Cheaper than N SetSingleColor calls
                    // (one IPC trip vs N), and the WinRT layer batches
                    // the underlying HID multi-update reports for us.
                    // The pre-allocated buffers are passed whole to avoid a
                    // per-frame slice allocation, so the tail beyond
                    // dirtyCount is padded with copies of entry 0. Leaving
                    // stale entries there instead would repaint a lamp with
                    // an OLDER colour whenever its position in the dirty
                    // ordering drops between frames.
                    for (int i = dirtyCount; i < _lampCount; i++)
                    {
                        _dirtyIndicesBuffer[i] = _dirtyIndicesBuffer[0];
                        _dirtyColorsBuffer[i] = _dirtyColorsBuffer[0];
                    }
                    lampArray.SetColorsForIndices(_dirtyColorsBuffer, _dirtyIndicesBuffer);
                    return true;
                }
                catch (Exception ex)
                {
                    // COMException with E_ACCESSDENIED happens when the
                    // OS revokes our write permission mid-frame (another
                    // app took foreground priority). It's expected under
                    // the foreground/background priority model and not
                    // worth alerting the user about each frame; we just
                    // wait for AvailabilityChanged on the device side.
                    if (ex is System.Runtime.InteropServices.COMException) return true;
                    DynamicLightingRGBDeviceProvider.Instance?.Throw(ex);
                    return false;
                }
            }
        }

        private bool TryResolveLampIndex(object key, out int idx)
        {
            if (key is LedId id && _ledIndexByLedId.TryGetValue(id, out idx)) return true;
            if (key is Led led && _ledIndexByLedId.TryGetValue(led.Id, out idx)) return true;
            idx = -1;
            return false;
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
