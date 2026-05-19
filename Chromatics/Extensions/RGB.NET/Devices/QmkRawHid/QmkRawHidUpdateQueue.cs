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
        private volatile bool _perDeviceDisable;

        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        private readonly Dictionary<LedId, int> _ledIndexByLedId;

        private byte[] _ledBytes;

        private ushort _lastViaHsbHash = 0xFFFF;

        private bool _openRgbDirectModeArmed;
        private byte _openRgbDirectModeIndex;

        // Buffer size derived from the device's reported OutputReportByteLength.
        // VIA boards typically run with RAW_EPSIZE=32 → 33-byte reports; the
        // OpenRGB-QMK plugin bumps RAW_EPSIZE to 64 → 65-byte reports. Sending
        // a too-short report to a 64-byte endpoint leaves the firmware waiting
        // for the rest of the packet and times out the read.
        private readonly int _outputReportByteLength;
        private readonly int _inputReportByteLength;
        private readonly int _payloadOutBytes;

        #endregion

        #region Constructors

        public QmkRawHidUpdateQueue(IDeviceUpdateTrigger trigger, QmkRawHidClientDefinition def, HidStream stream)
            : base(trigger)
        {
            _def = def;
            _stream = stream;
            _outputReportByteLength = def.OutputReportByteLength > 0 ? def.OutputReportByteLength : 33;
            _inputReportByteLength  = def.InputReportByteLength  > 0 ? def.InputReportByteLength  : 33;
            _payloadOutBytes        = _outputReportByteLength - 1;
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

            ushort hash = (ushort)(((hue >> 1) & 0x7F) << 9
                                | ((sat >> 3) & 0x1F) << 4
                                |  ((val >> 4) & 0x0F));
            if (hash == _lastViaHsbHash) return;
            _lastViaHsbHash = hash;

            byte[] outBuf = new byte[_outputReportByteLength];

            ViaProtocol.BuildSetRgbMatrixEffect(new Span<byte>(outBuf, 1, _payloadOutBytes), ViaProtocol.Effect_SolidColor);
            WritePayload(outBuf);

            ViaProtocol.BuildSetRgbMatrixColor(new Span<byte>(outBuf, 1, _payloadOutBytes), hue, sat);
            WritePayload(outBuf);

            ViaProtocol.BuildSetRgbMatrixBrightness(new Span<byte>(outBuf, 1, _payloadOutBytes), val);
            WritePayload(outBuf);
        }

        // ── OpenRGB-QMK path (Tier 2: per-LED) ───────────────────────

        private void SendOpenRgbQmkFrame(ReadOnlySpan<(object key, Color color)> dataSet, double brightnessScale)
        {
            int total = _def.LedCount;
            if (total <= 0) return;

            if (_ledBytes == null) _ledBytes = new byte[total * 3];

            // Switch the firmware to OPENRGB_DIRECT mode once per session.
            // SET_LEDS writes to g_openrgb_direct_mode_colors[] regardless of
            // active mode, but those colours only reach the hardware when the
            // active rgb_matrix effect is OPENRGB_DIRECT (the custom effect
            // appended to the enum by the firmware module).
            if (!_openRgbDirectModeArmed)
            {
                ArmOpenRgbDirectMode();
                _openRgbDirectModeArmed = true;
            }

            int chunkSize = OpenRgbQmkProtocol.MaxLedsPerSetLeds(_payloadOutBytes);
            if (chunkSize <= 0) chunkSize = 1;
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

            byte[] outBuf = new byte[_outputReportByteLength];
            bool first = true;
            for (int c = 0; c < chunkCount; c++)
            {
                if (!dirty[c]) continue;
                if (!first) Thread.Sleep(1);
                first = false;

                int start = c * chunkSize;
                int count = Math.Min(chunkSize, total - start);
                OpenRgbQmkProtocol.BuildDirectModeSetLeds(
                    new Span<byte>(outBuf, 1, _payloadOutBytes),
                    (byte)start, (byte)count,
                    new ReadOnlySpan<byte>(_ledBytes, start * 3, count * 3));
                WritePayload(outBuf);
            }
        }

        // OPENRGB_DIRECT lives at the tail of the firmware's rgb_matrix
        // effect enum because the RGB_MATRIX_EFFECT() macro appends custom
        // effects after every built-in. The firmware reports the indices of
        // its built-in enabled effects via Cmd_GetEnabledModes; OPENRGB_DIRECT
        // is the highest valid index NOT in that list (i.e. max(enabled)+1).
        // If the read fails or returns nothing, we fall back to mode 0 which
        // is harmless (no effect) but won't paint either — the user can fix
        // by selecting the OpenRGB direct mode via VIA's mode-cycling keymap.
        private void ArmOpenRgbDirectMode()
        {
            byte mode = ResolveOpenRgbDirectModeIndex();
            _openRgbDirectModeIndex = mode;

            byte[] outBuf = new byte[_outputReportByteLength];
            OpenRgbQmkProtocol.BuildSetMode(
                new Span<byte>(outBuf, 1, _payloadOutBytes),
                hue: 0, sat: 0, val: 255, mode: mode, speed: 0, save: false);
            WritePayload(outBuf);
        }

        private byte ResolveOpenRgbDirectModeIndex()
        {
            byte[] outBuf = new byte[_outputReportByteLength];
            byte[] inBuf  = new byte[_inputReportByteLength];

            OpenRgbQmkProtocol.BuildGetEnabledModes(new Span<byte>(outBuf, 1, _payloadOutBytes));
            try
            {
                _stream.Write(outBuf);
                int n = _stream.Read(inBuf, 0, inBuf.Length);
                if (n <= 1) return 0;
            }
            catch
            {
                return 0;
            }

            byte highest = 0;
            for (int i = 2; i < inBuf.Length - 1; i++)
            {
                byte mode = inBuf[i];
                if (mode == 0) break;
                if (mode > highest) highest = mode;
            }
            return highest == 0 ? (byte)0 : (byte)(highest + 1);
        }

        private int TryResolveLedIndex(object key)
        {
            if (key is LedId id && _ledIndexByLedId.TryGetValue(id, out int idx)) return idx;
            return -1;
        }

        private void WritePayload(byte[] outBuf)
        {
            if (_stream == null) return;
            try { _stream.Write(outBuf); }
            catch (System.IO.IOException ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[QMK] {_def.Product}: write failed ({ex.Message}); pausing queue.");
                _shuttingDown = true;
            }
            catch (ObjectDisposedException)
            {
                _shuttingDown = true;
            }
        }

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
