using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    public class QmkRawHidUpdateQueue : UpdateQueue
    {
        #region Properties & Fields

        private readonly QmkRawHidClientDefinition _def;
        private readonly HidStream _stream;
        private readonly Lock _lock = new();
        private volatile bool _shuttingDown;
        // Per-device disable gate — parity with LifxUpdateQueue. Toggled
        // from RGBController.RemoveDevice / AddDevice so paint frames are
        // dropped while the user has the keyboard disabled in the Mapping
        // tab. Cleared on re-enable.
        private volatile bool _perDeviceDisable;

        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // LedId → firmware LED index. Built once at construction from the
        // client definition's Layout so per-frame lookups avoid linear scans.
        private readonly Dictionary<LedId, int> _ledIndexByLedId;

        // Persistent RGB byte cache for the entire strip — same idea as
        // LifxUpdateQueue._strip. Lets sparse decorator updates (a few LEDs
        // changed) only resend their chunks rather than the whole strip.
        // 3 bytes per LED (R, G, B). Null until the first frame.
        private byte[] _ledBytes;

        // Single-LED VIA path coalescing: last sent hue/sat/brightness/effect
        // tuple. Set to 0xFFFF when empty so the first frame always sends.
        private ushort _lastViaHsbHash = 0xFFFF;

        // True once we've put the OpenRGB-QMK firmware into direct mode
        // (mode 0). On enter direct mode the firmware suspends built-in
        // RGB matrix effects so our Set commands aren't fighting them.
        private bool _openRgbDirectModeArmed;

        #endregion

        #region Constructors

        public QmkRawHidUpdateQueue(IDeviceUpdateTrigger trigger, QmkRawHidClientDefinition def, HidStream stream)
            : base(trigger)
        {
            _def = def;
            _stream = stream;
            _ledIndexByLedId = new Dictionary<LedId, int>(def.Layout.Count);
            for (int i = 0; i < def.Layout.Count; i++)
                _ledIndexByLedId[def.Layout[i].PreferredLedId] = def.Layout[i].FirmwareIndex;
        }

        #endregion

        #region Methods

        public void BeginShutdown() => _shuttingDown = true;

        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        public void ResetCache()
        {
            lock (_lock)
            {
                _ledBytes = null;
                _lastViaHsbHash = 0xFFFF;
                _openRgbDirectModeArmed = false;
            }
        }

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _perDeviceBrightness = correction;

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

                    if (_def.Protocol == QmkRawHidProtocolMode.OpenRgbQmk)
                        SendOpenRgbQmkFrame(dataSet, brightnessScale);
                    else
                        SendViaFrame(dataSet, brightnessScale);

                    return true;
                }
                catch (Exception ex)
                {
                    QmkRawHidRGBDeviceProvider.Instance?.Throw(ex);
                    return false;
                }
            }
        }

        // ── VIA path (Tier 1: single representative colour) ──────────

        private void SendViaFrame(ReadOnlySpan<(object key, Color color)> dataSet, double brightnessScale)
        {
            Color picked = PickRepresentativeColor(dataSet);
            ToHsv255(picked, brightnessScale, out byte hue, out byte sat, out byte val);

            // Pack hue/sat/val + effect index into a single 16-bit hash to
            // cheaply detect "nothing changed since last frame" and skip
            // the three USB writes the VIA path would otherwise burn.
            // 7-bit hue, 5-bit sat, 4-bit val is enough resolution for
            // change detection without ever sending a no-op.
            ushort hash = (ushort)(((hue >> 1) & 0x7F) << 9
                                | ((sat >> 3) & 0x1F) << 4
                                |  ((val >> 4) & 0x0F));
            if (hash == _lastViaHsbHash) return;
            _lastViaHsbHash = hash;

            Span<byte> outBuf = stackalloc byte[QmkRawHidConstants.OutputReportBytes];
            Span<byte> payload = outBuf.Slice(1, QmkRawHidConstants.ReportPayloadBytes);

            ViaProtocol.BuildSetRgbMatrixEffect(payload, ViaProtocol.Effect_SolidColor);
            WritePayload(outBuf);

            ViaProtocol.BuildSetRgbMatrixColor(payload, hue, sat);
            WritePayload(outBuf);

            ViaProtocol.BuildSetRgbMatrixBrightness(payload, val);
            WritePayload(outBuf);
        }

        // ── OpenRGB-QMK path (Tier 2: per-key) ───────────────────────

        private void SendOpenRgbQmkFrame(ReadOnlySpan<(object key, Color color)> dataSet, double brightnessScale)
        {
            int total = _def.LedCount;
            if (total <= 0) return;

            if (_ledBytes == null) _ledBytes = new byte[total * 3];

            // Arm direct mode once per session. The firmware persists the
            // mode setting in RAM, so we don't have to re-arm every frame —
            // but DO re-arm after a ResetCache (which clears _openRgbDirectModeArmed
            // alongside _ledBytes, so the next Update arms again).
            if (!_openRgbDirectModeArmed)
            {
                Span<byte> armBuf = stackalloc byte[QmkRawHidConstants.OutputReportBytes];
                OpenRgbQmkProtocol.BuildSetMode(armBuf.Slice(1, QmkRawHidConstants.ReportPayloadBytes), modeIndex: 0);
                WritePayload(armBuf);
                _openRgbDirectModeArmed = true;
            }

            // Patch dirty LEDs into _ledBytes and remember which chunks
            // (LED indices grouped by MaxLedsPerSetRange) need re-sending.
            int chunkSize = OpenRgbQmkProtocol.MaxLedsPerSetRange;
            int chunkCount = (total + chunkSize - 1) / chunkSize;
            Span<bool> dirty = chunkCount <= 256 ? stackalloc bool[chunkCount] : new bool[chunkCount];

            foreach (var (key, color) in dataSet)
            {
                int idx = TryResolveLedIndex(key);
                if (idx < 0 || idx >= total) continue;

                ToRgb255(color, brightnessScale, out byte r, out byte g, out byte b);
                int off = idx * 3;
                if (_ledBytes[off] == r && _ledBytes[off + 1] == g && _ledBytes[off + 2] == b) continue;

                _ledBytes[off]     = r;
                _ledBytes[off + 1] = g;
                _ledBytes[off + 2] = b;
                dirty[idx / chunkSize] = true;
            }

            // Send each dirty chunk. Inter-packet pacing keeps us within the
            // firmware's RX queue depth on full-speed USB — 1ms is well above
            // QMK's worst-case raw_hid_receive turnaround.
            Span<byte> outBuf = stackalloc byte[QmkRawHidConstants.OutputReportBytes];
            bool first = true;
            for (int c = 0; c < chunkCount; c++)
            {
                if (!dirty[c]) continue;
                if (!first) Thread.Sleep(1);
                first = false;

                ushort start = (ushort)(c * chunkSize);
                int count = Math.Min(chunkSize, total - start);
                OpenRgbQmkProtocol.BuildSetLedRange(
                    outBuf.Slice(1, QmkRawHidConstants.ReportPayloadBytes),
                    start, (byte)count,
                    new ReadOnlySpan<byte>(_ledBytes, start * 3, count * 3));
                WritePayload(outBuf);
            }
        }

        private int TryResolveLedIndex(object key)
        {
            if (key is LedId id && _ledIndexByLedId.TryGetValue(id, out int idx)) return idx;
            return -1;
        }

        // ── HID write ────────────────────────────────────────────────

        private void WritePayload(ReadOnlySpan<byte> outBuf)
        {
            if (_stream == null) return;
            try { _stream.Write(outBuf.ToArray()); }
            catch (System.IO.IOException ex)
            {
                // PnP unplug between Write call and current frame. Mark
                // shutting down so the rest of this batch becomes no-ops;
                // the provider's hot-plug reconcile will dispose us shortly.
                Logger.WriteConsole(LoggerTypes.Devices, $"[QMK] {_def.Product}: write failed ({ex.Message}); pausing queue.");
                _shuttingDown = true;
            }
            catch (ObjectDisposedException)
            {
                _shuttingDown = true;
            }
        }

        // ── Colour helpers ───────────────────────────────────────────

        private static Color PickRepresentativeColor(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            if (dataSet.Length == 1) return dataSet[0].color;
            int maxIdx = 0;
            double maxL = -1;
            for (int i = 0; i < dataSet.Length; i++)
            {
                double l = dataSet[i].color.R + dataSet[i].color.G + dataSet[i].color.B;
                if (l > maxL) { maxL = l; maxIdx = i; }
            }
            return dataSet[maxIdx].color;
        }

        private static void ToHsv255(Color rgb, double brightnessScale, out byte hue, out byte sat, out byte val)
        {
            double r = Math.Clamp(rgb.R, 0.0, 1.0);
            double g = Math.Clamp(rgb.G, 0.0, 1.0);
            double bb = Math.Clamp(rgb.B, 0.0, 1.0);

            double max = Math.Max(r, Math.Max(g, bb));
            double min = Math.Min(r, Math.Min(g, bb));
            double delta = max - min;

            double h = 0;
            if (delta > 0)
            {
                if (max == r)      h = ((g - bb) / delta) % 6;
                else if (max == g) h = ((bb - r) / delta) + 2;
                else               h = ((r - g) / delta) + 4;
                h *= 60;
                if (h < 0) h += 360;
            }

            double s = max == 0 ? 0 : delta / max;
            double v = max * Math.Clamp(brightnessScale, 0.0, 1.0);

            hue = (byte)Math.Round((h / 360.0) * 255);
            sat = (byte)Math.Round(s * 255);
            val = (byte)Math.Round(v * 255);
        }

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
