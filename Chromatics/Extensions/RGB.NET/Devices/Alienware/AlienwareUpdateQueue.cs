using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware
{
    public class AlienwareUpdateQueue : UpdateQueue
    {
        #region Properties & Fields

        private readonly AlienwareClientDefinition _def;
        private readonly HidStream _stream;
        private readonly Lock _lock = new();
        private volatile bool _shuttingDown;
        private volatile bool _perDeviceDisable;

        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // LedId → flat hardware light index. Built from the LightCount once
        // at construction so the per-frame Resolve doesn't allocate.
        private readonly Dictionary<LedId, int> _ledIndexByLedId;

        // Per-light RGB cache. Sparse decorator updates only re-send the
        // lights whose values actually changed; per-frame the queue diffs
        // against this and skips the HID writes entirely when nothing's
        // dirty. 3 bytes per light (R, G, B).
        private readonly byte[] _ledBytes;

        // V8 announce/data sequencing uses an incrementing batch counter
        // per frame.
        private byte _v8BatchCounter;

        #endregion

        #region Constructors

        public AlienwareUpdateQueue(IDeviceUpdateTrigger trigger, AlienwareClientDefinition def, HidStream stream)
            : base(trigger)
        {
            _def = def;
            _stream = stream;

            int count = Math.Max(1, _def.LightCount);
            _ledIndexByLedId = new Dictionary<LedId, int>(count);
            for (int i = 0; i < count; i++)
                _ledIndexByLedId[(LedId)((int)LedId.Custom1 + i)] = i;

            _ledBytes = new byte[count * 3];
        }

        #endregion

        #region Lifecycle

        public void BeginShutdown() => _shuttingDown = true;
        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        public void ResetCache()
        {
            lock (_lock)
            {
                Array.Clear(_ledBytes, 0, _ledBytes.Length);
                _v8BatchCounter = 0;
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

                    // Patch dirty lights into the per-light cache. Track
                    // the dirty index list so we only resend lights whose
                    // value changed.
                    var dirty = new List<int>(dataSet.Length);
                    foreach (var (key, color) in dataSet)
                    {
                        int idx = ResolveLightIndex(key);
                        if (idx < 0 || idx >= _def.LightCount) continue;

                        ToRgb255(color, brightnessScale, out byte r, out byte g, out byte b);
                        int off = idx * 3;
                        if (_ledBytes[off] == r && _ledBytes[off + 1] == g && _ledBytes[off + 2] == b) continue;

                        _ledBytes[off]     = r;
                        _ledBytes[off + 1] = g;
                        _ledBytes[off + 2] = b;
                        dirty.Add(idx);
                    }

                    if (dirty.Count == 0) return true;

                    switch (_def.ApiVersion)
                    {
                        case AlienwareApiVersion.PerKeyV5:
                            SendV5Frame(dirty);
                            break;
                        case AlienwareApiVersion.PerKeyV8:
                            SendV8Frame(dirty);
                            break;
                        case AlienwareApiVersion.ZoneV4:
                            SendV4Frame(dirty);
                            break;
                        default:
                            // Unknown version — silent no-op so we don't
                            // pelt the device with garbage.
                            return true;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    AlienwareRGBDeviceProvider.Instance?.Throw(ex);
                    return false;
                }
            }
        }

        private int ResolveLightIndex(object key)
        {
            if (key is LedId id && _ledIndexByLedId.TryGetValue(id, out int idx)) return idx;
            if (key is Led led && _ledIndexByLedId.TryGetValue(led.Id, out int idx2)) return idx2;
            return -1;
        }

        #endregion

        #region V5 path (per-key notebook keyboards, VID 0x0d62)

        private void SendV5Frame(List<int> dirty)
        {
            int reportLen = _def.ReportLength > 0 ? _def.ReportLength : AlienwarePerKeyV5Protocol.ReportLength;
            byte[] buffer = new byte[reportLen];

            // Pack dirty lights into colour-set reports of up to 15 lights
            // each. Each report = one HidStream.SetFeature call.
            int packedThisReport = 0;
            var batch = new (byte, byte, byte, byte)[AlienwarePerKeyV5Protocol.MaxLightsPerReport];

            for (int i = 0; i < dirty.Count; i++)
            {
                int idx = dirty[i];
                int off = idx * 3;
                batch[packedThisReport++] = ((byte)idx, _ledBytes[off], _ledBytes[off + 1], _ledBytes[off + 2]);

                bool isLast = i == dirty.Count - 1;
                if (packedThisReport == AlienwarePerKeyV5Protocol.MaxLightsPerReport || isLast)
                {
                    AlienwarePerKeyV5Protocol.BuildColorSet(buffer, new ReadOnlySpan<(byte, byte, byte, byte)>(batch, 0, packedThisReport));
                    SendFeatureReport(buffer);
                    packedThisReport = 0;
                }
            }

            // Loop marker (required between SetColor sequence and Update).
            AlienwarePerKeyV5Protocol.BuildLoop(buffer);
            SendFeatureReport(buffer);

            // Commit.
            AlienwarePerKeyV5Protocol.BuildUpdate(buffer);
            SendFeatureReport(buffer);
        }

        #endregion

        #region V8 path (per-key external keyboards, VID 0x04f2)

        private void SendV8Frame(List<int> dirty)
        {
            int reportLen = _def.ReportLength > 0 ? _def.ReportLength : AlienwarePerKeyV8Protocol.ReportLength;
            byte[] buffer = new byte[reportLen];

            // Step 1: announce the batch size via feature report. Tells
            // the firmware how many lights to expect in the data packets
            // that follow.
            AlienwarePerKeyV8Protocol.BuildAnnounceBatch(buffer, (byte)dirty.Count);
            SendFeatureReport(buffer);

            // Step 2: stream up to 4 lights per write report.
            _v8BatchCounter = 1;
            var batch = new (byte, byte, byte, byte)[AlienwarePerKeyV8Protocol.MaxLightsPerPacket];
            int packedThisPacket = 0;

            for (int i = 0; i < dirty.Count; i++)
            {
                int idx = dirty[i];
                int off = idx * 3;
                batch[packedThisPacket++] = ((byte)idx, _ledBytes[off], _ledBytes[off + 1], _ledBytes[off + 2]);

                bool isLast = i == dirty.Count - 1;
                if (packedThisPacket == AlienwarePerKeyV8Protocol.MaxLightsPerPacket || isLast)
                {
                    AlienwarePerKeyV8Protocol.BuildDataPacket(buffer, _v8BatchCounter, new ReadOnlySpan<(byte, byte, byte, byte)>(batch, 0, packedThisPacket));
                    WriteOutputReport(buffer);
                    _v8BatchCounter++;
                    packedThisPacket = 0;
                }
            }
        }

        #endregion

        #region V4 path (zone-based chassis, VID 0x187C)

        private void SendV4Frame(List<int> dirty)
        {
            int reportLen = AlienwareZoneV4Protocol.ReportLength;
            byte[] buffer = new byte[reportLen];

            // Reset (Remove + Start) opens a new colour sequence on the
            // device. Required before the first SetColor of every frame —
            // skipping it leaves the device on the previous frame's
            // sequence and the new SetColors get appended rather than
            // overriding.
            AlienwareZoneV4Protocol.BuildReset_Remove(buffer);
            WriteOutputReport(buffer);
            AlienwareZoneV4Protocol.BuildReset_Start(buffer);
            WriteOutputReport(buffer);

            // Group dirty lights by colour so lights sharing a colour go
            // out in one HID write rather than N. Common case for static
            // overlays / single-colour layers.
            var byColor = new Dictionary<uint, List<byte>>();
            foreach (int idx in dirty)
            {
                int off = idx * 3;
                byte r = _ledBytes[off], g = _ledBytes[off + 1], b = _ledBytes[off + 2];
                uint packed = (uint)((r << 16) | (g << 8) | b);
                if (!byColor.TryGetValue(packed, out var list))
                    byColor[packed] = list = new List<byte>(dirty.Count);
                list.Add((byte)idx);
            }

            // Each SetColor command packs at most 26 light ids
            // (ReportLength=34 minus 8-byte header). Split colour groups
            // larger than that across multiple writes.
            const int maxIdsPerWrite = 34 - 8;
            foreach (var kvp in byColor)
            {
                byte r = (byte)((kvp.Key >> 16) & 0xFF);
                byte g = (byte)((kvp.Key >> 8) & 0xFF);
                byte b = (byte)(kvp.Key & 0xFF);
                var ids = kvp.Value;
                for (int start = 0; start < ids.Count; start += maxIdsPerWrite)
                {
                    int chunk = Math.Min(maxIdsPerWrite, ids.Count - start);
                    AlienwareZoneV4Protocol.BuildSetColor(
                        buffer, r, g, b,
                        new ReadOnlySpan<byte>(ids.GetRange(start, chunk).ToArray()));
                    WriteOutputReport(buffer);
                }
            }

            // Commit (firmware applies the freshly-set sequence).
            AlienwareZoneV4Protocol.BuildCommit(buffer);
            WriteOutputReport(buffer);
        }

        #endregion

        #region HID I/O

        private void SendFeatureReport(byte[] buffer)
        {
            try { _stream.SetFeature(buffer); }
            catch (System.IO.IOException ex) { OnIoError(ex); }
            catch (ObjectDisposedException) { _shuttingDown = true; }
        }

        private void WriteOutputReport(byte[] buffer)
        {
            try { _stream.Write(buffer); }
            catch (System.IO.IOException ex) { OnIoError(ex); }
            catch (ObjectDisposedException) { _shuttingDown = true; }
        }

        private void OnIoError(System.IO.IOException ex)
        {
            // PnP unplug between Write call and current frame. Mark
            // shutting down so the rest of this batch becomes no-ops;
            // the provider's hot-plug reconcile (or next session) will
            // dispose us.
            Logger.WriteConsole(LoggerTypes.Devices, $"[Alienware] {_def.Product}: HID write failed ({ex.Message}); pausing queue.");
            _shuttingDown = true;
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
