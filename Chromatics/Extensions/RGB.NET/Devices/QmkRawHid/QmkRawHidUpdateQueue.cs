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

        // Captured before we switch the firmware into OPENRGB_DIRECT so we
        // can restore the user's prior mode/speed/HSV on shutdown. Without
        // this, the LEDs freeze on the last frame Chromatics sent because
        // the firmware stays in direct mode with no host driving it.
        private bool _priorModeCaptured;
        private byte _priorMode;
        private byte _priorSpeed;
        private byte _priorHue;
        private byte _priorSat;
        private byte _priorVal;

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

        public void BeginShutdown()
        {
            // Take _lock so this serializes with any in-flight Update() —
            // otherwise the provider can call BeginShutdown, set _shuttingDown,
            // dispose the stream, and an Update mid-write will throw
            // ObjectDisposedException at WritePayload's _stream.Write call.
            lock (_lock)
            {
                if (_shuttingDown) return;

                if (_def.Protocol == QmkRawHidProtocolMode.OpenRgbQmk && _priorModeCaptured)
                {
                    try
                    {
                        byte[] outBuf = new byte[_outputReportByteLength];
                        OpenRgbQmkProtocol.BuildSetMode(
                            new Span<byte>(outBuf, 1, _payloadOutBytes),
                            hue: _priorHue, sat: _priorSat, val: _priorVal,
                            mode: _priorMode, speed: _priorSpeed, save: false);
                        _stream?.Write(outBuf);
                        Logger.WriteVerbose(
                            $"[QMK] {_def.Product}: restored prior mode={_priorMode}, speed={_priorSpeed}, HSV=({_priorHue},{_priorSat},{_priorVal}) on shutdown.");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteVerbose(
                            $"[QMK] {_def.Product}: restore-on-shutdown SET_MODE failed: {ex.Message}");
                    }
                }
                _shuttingDown = true;
            }
        }

        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        public void ResetCache()
        {
            lock (_lock)
            {
                _ledBytes = null;
                _lastViaHsbHash = 0xFFFF;
                _openRgbDirectModeArmed = false;
                _firstFrameLogged = false;
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
            int packetsSent = 0;
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
                packetsSent++;
            }

            if (!_firstFrameLogged && packetsSent > 0)
            {
                _firstFrameLogged = true;
                Logger.WriteVerbose(
                    $"[QMK] {_def.Product}: first paint frame — {packetsSent} SET_LEDS packet(s), {chunkSize} LEDs/packet, first LED RGB = ({_ledBytes[0]}, {_ledBytes[1]}, {_ledBytes[2]}).");
            }
        }

        private bool _firstFrameLogged;

        // OPENRGB_DIRECT's position in the firmware's rgb_matrix effect enum
        // depends on the include order in rgb_matrix_effects.inc. Two
        // conventions in the wild:
        //   - Front-loaded (Keychron + modern qmk_firmware forks): openrgb
        //     _direct_anim.h is the FIRST include, so OPENRGB_DIRECT = 1.
        //     The literals in openrgb_rgb_matrix_effects_indexes[] are off
        //     by one from real enum positions in this layout.
        //   - Tail-loaded (vanilla upstream OpenRGB-QMK): openrgb_direct
        //     _anim.h is included last, so OPENRGB_DIRECT = max(enabled)+1.
        // Try mode=1 first; if the firmware returns FAILURE (mode >= MAX),
        // fall back to max(enabled)+1. Diagnostic GET_MODE_INFO after each
        // SET_MODE tells us what the firmware ended up on.
        private void ArmOpenRgbDirectMode()
        {
            CapturePriorMode();
            byte highestEnabled = ResolveHighestEnabledMode();

            if (TrySetMode(1)) { _openRgbDirectModeIndex = 1; return; }

            byte fallback = highestEnabled == 0 ? (byte)0 : (byte)(highestEnabled + 1);
            if (fallback > 0 && TrySetMode(fallback)) { _openRgbDirectModeIndex = fallback; return; }

            Logger.WriteConsole(LoggerTypes.Devices,
                $"[QMK] {_def.Product}: neither mode=1 nor mode={fallback} (max enabled + 1) was accepted. " +
                "OpenRGB direct mode couldn't be armed — per-LED paint will write to the firmware's direct-mode buffer but the active rgb_matrix effect will overwrite it. " +
                "Cycle the keyboard's RGB mode key until you find the OpenRGB direct effect, or open an issue with the SET_MODE reply bytes above.");
        }

        private bool TrySetMode(byte mode)
        {
            Logger.WriteVerbose(
                $"[QMK] {_def.Product}: trying SET_MODE with mode={mode} (h=0, s=0, v=255)...");

            byte[] outBuf = new byte[_outputReportByteLength];
            byte[] inBuf  = new byte[_inputReportByteLength];
            OpenRgbQmkProtocol.BuildSetMode(
                new Span<byte>(outBuf, 1, _payloadOutBytes),
                hue: 0, sat: 0, val: 255, mode: mode, speed: 0, save: false);
            byte status;
            try
            {
                _stream.Write(outBuf);
                int n = _stream.Read(inBuf, 0, inBuf.Length);
                if (n <= 1) { Logger.WriteVerbose($"[QMK] {_def.Product}:   SET_MODE returned {n} bytes (no payload)."); return false; }
                status = (n >= 64) ? inBuf[n - 2] : (byte)0;
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[QMK] {_def.Product}:   SET_MODE I/O failed: {ex.Message}");
                return false;
            }

            string statusName = status == OpenRgbQmkProtocol.Response_Success ? "SUCCESS"
                : status == OpenRgbQmkProtocol.Response_Failure ? "FAILURE"
                : $"0x{status:X2}";
            Logger.WriteVerbose(
                $"[QMK] {_def.Product}:   SET_MODE reply status = {statusName}");
            return status == OpenRgbQmkProtocol.Response_Success;
        }

        // Query GET_MODE_INFO to record the firmware's current mode + HSV.
        // Run once per session, before we switch into OPENRGB_DIRECT. On
        // shutdown BeginShutdown() replays these values via SET_MODE so the
        // user's prior lighting state comes back instead of freezing on the
        // last frame we sent.
        private void CapturePriorMode()
        {
            if (_priorModeCaptured) return;

            byte[] outBuf = new byte[_outputReportByteLength];
            byte[] inBuf  = new byte[_inputReportByteLength];
            Span<byte> payload = new Span<byte>(outBuf, 1, _payloadOutBytes);
            payload.Clear();
            payload[0] = OpenRgbQmkProtocol.Cmd_GetModeInfo;

            try
            {
                _stream.Write(outBuf);
                int n = _stream.Read(inBuf, 0, inBuf.Length);
                if (n < 7 || inBuf[1] != OpenRgbQmkProtocol.Cmd_GetModeInfo) return;
                // Reply layout from openrgb.c openrgb_get_mode_info:
                //   [1]=mode, [2]=speed, [3]=h, [4]=s, [5]=v (after the
                //   leading report-id byte at [0] which HidSharp prepends).
                _priorMode  = inBuf[2];
                _priorSpeed = inBuf[3];
                _priorHue   = inBuf[4];
                _priorSat   = inBuf[5];
                _priorVal   = inBuf[6];
                _priorModeCaptured = true;
                Logger.WriteVerbose(
                    $"[QMK] {_def.Product}: captured prior firmware state — mode={_priorMode}, speed={_priorSpeed}, HSV=({_priorHue},{_priorSat},{_priorVal}).");
            }
            catch (Exception ex)
            {
                // Keep on Console: if we can't capture, the keyboard *will*
                // freeze on the last paint frame at shutdown — the user
                // should know that's the cause.
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] {_def.Product}: couldn't capture prior mode ({ex.Message}); the keyboard will freeze on the last paint frame when Chromatics releases it.");
            }
        }

        private byte ResolveHighestEnabledMode()
        {
            byte[] outBuf = new byte[_outputReportByteLength];
            byte[] inBuf  = new byte[_inputReportByteLength];

            OpenRgbQmkProtocol.BuildGetEnabledModes(new Span<byte>(outBuf, 1, _payloadOutBytes));
            int n;
            try
            {
                _stream.Write(outBuf);
                n = _stream.Read(inBuf, 0, inBuf.Length);
                if (n <= 1)
                {
                    Logger.WriteVerbose(
                        $"[QMK] {_def.Product}: GET_ENABLED_MODES returned no data ({n} bytes).");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose(
                    $"[QMK] {_def.Product}: GET_ENABLED_MODES I/O failed ({ex.Message}).");
                return 0;
            }

            byte highest = 0;
            for (int i = 2; i < inBuf.Length - 1; i++)
            {
                byte mode = inBuf[i];
                if (mode == 0) break;
                if (mode > highest) highest = mode;
            }

            Logger.WriteVerbose(
                $"[QMK] {_def.Product}: GET_ENABLED_MODES returned {n} bytes; highest enabled built-in mode = {highest}.");
            return highest;
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
