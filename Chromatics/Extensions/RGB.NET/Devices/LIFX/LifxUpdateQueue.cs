using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX
{
    public class LifxUpdateQueue : UpdateQueue
    {
        #region Properties & Fields

        private readonly LifxClientDefinition _def;
        private readonly UdpClient _udp;
        private readonly byte[] _target;
        private readonly uint _source;
        private readonly LifxProductCatalog.ProductInfo _product;
        private readonly Lock _lock = new();
        private volatile bool _shuttingDown;
        // Set true while the device is disabled in the Mapping tab. Causes
        // Update() to no-op so any decorator data already buffered in
        // _currentDataSet (or arriving from a TimerUpdateTrigger tick that
        // raced surface.Detach) gets silently dropped instead of racing the
        // restore-to-original send. Toggled back to false on re-enable in
        // RGBController.AddDevice, after the queue's diff cache has been
        // reset and the device has been re-attached.
        private volatile bool _perDeviceDisable;
        private byte _seq;

        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // Captured at adoption / first-update time so disable / Dispose can
        // restore the bulb to whatever the user had before Chromatics took
        // over. Null until OriginalState() succeeds; if the device is
        // unreachable when we try to capture, we simply don't restore.
        private OriginalState _original;

        // Last-color cache to skip redundant frames. RGB.NET emits a frame on
        // every surface tick even when colour is unchanged for our LEDs;
        // shipping every one of those at 20Hz over UDP burns the bulb's
        // packet budget for no visible benefit. Comparing against the last
        // sent payload lets us silently coalesce.
        private byte[] _lastSinglePayload;

        #endregion

        #region Constructors

        public LifxUpdateQueue(IDeviceUpdateTrigger updateTrigger, LifxClientDefinition def, UdpClient udp, uint source)
            : base(updateTrigger)
        {
            _def = def;
            _udp = udp;
            _source = source;
            _target = LifxHeader.TargetFromMac(def.Mac);
            _product = LifxProductCatalog.GetOrDefault(def.ProductId);
        }

        #endregion

        #region Methods

        public void BeginShutdown() => _shuttingDown = true;

        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        // Send a stand-alone SetLightPower(on=true) without touching
        // _original. Used by RGBController.AddDevice on per-device re-enable
        // so a bulb that was left powered-off (e.g. honoured at startup
        // via CaptureOriginalStateAsync(turnOnIfOff: false)) actually
        // shows the freshly-painted layers. The previous code path piggy-
        // backed on the recapture's auto-on branch, which had the side
        // effect of overwriting _original.Zones with whatever paint
        // frames had reached the bulb between AddDevice and the
        // recapture query — poisoning the next disable's restore target.
        // Sending power-on directly keeps _original intact.
        public void EnsurePoweredOn(uint durationMs = 300)
        {
            try
            {
                lock (_lock)
                {
                    if (_shuttingDown || _perDeviceDisable) return;
                    SendSetLightPower(on: true, durationMs: durationMs);
                }
            }
            catch { /* best-effort */ }
        }

        // Drop the diff caches so the next Update sends every zone (or every
        // matrix cell) regardless of what the strip cached the last time it
        // was active. Without this, a re-enable after the user toggles the
        // device off-then-on in the Mapping tab can be silently suppressed:
        // _strip's per-zone HSBK still matches the new frame's HSBK because
        // the LedGroups continued painting the same colours during the
        // disabled period, so SendMultizoneFrame would skip every chunk.
        public void ResetCache()
        {
            lock (_lock)
            {
                _strip = null;
                _stripZones = 0;
                _lastSinglePayload = null;
                _matrixCells = null;
            }
        }

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _perDeviceBrightness = correction;

        // Captures original power + colour. Caller (provider) does this once at
        // start-up so we have something to restore to on disable / app close.
        // Best-effort: a 500ms timeout keeps a single unreachable bulb from
        // stalling provider startup for the others.
        public async Task CaptureOriginalStateAsync(bool turnOnIfOff = true, CancellationToken ct = default)
        {
            try
            {
                var state = await QueryOriginalStateAsync(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                if (state != null)
                {
                    _original = state;

                    // If the bulb was off when we adopted it AND the caller
                    // wants us to make layers visible, turn it on. SetColor
                    // on a powered-off LIFX bulb queues the colour but
                    // doesn't switch it on — we have to send SetLightPower
                    // explicitly. The original power state stays in
                    // _original.Powered for restoration on disable / app
                    // close.
                    //
                    // turnOnIfOff is set false by the provider for devices
                    // that are persisted-disabled in the Mapping tab: we
                    // still need to capture state (so a later re-enable
                    // can restore correctly), but the bulb must remain in
                    // whatever state the user left it. Without this gate,
                    // a restart with a disabled device would silently
                    // wake the bulb up — and worse, the next capture
                    // cycle would observe Powered=true and poison
                    // _original so subsequent disables stop turning the
                    // bulb back off.
                    if (turnOnIfOff && !state.Powered)
                    {
                        SendSetLightPower(on: true, durationMs: 300);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[LIFX] {_def.Label}: failed to capture original state ({ex.Message})");
            }
        }

        // Best-effort GetExtendedColorZones probe to refresh ZoneCount
        // post-discovery. Discovery's StateExtendedColorZones reply can be
        // dropped on noisy LANs; this gives the provider a second shot
        // before the device's layout gets locked in. Static so the provider
        // can call it BEFORE constructing the queue/device — the answer
        // determines layout, so the device has to be built with the right
        // count. Returns the zone count observed, or 0 if no reply.
        public static async Task<ushort> ProbeZoneCountAsync(System.Net.IPEndPoint endpoint, string mac, TimeSpan timeout, CancellationToken ct = default)
        {
            if (endpoint == null || string.IsNullOrEmpty(mac)) return 0;
            byte[] target = LifxHeader.TargetFromMac(mac);

            using var qudp = new UdpClient(0);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            var deadlineTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var reg = cts.Token.Register(() => deadlineTcs.TrySetResult());

            uint qSource = (uint)Random.Shared.Next(2, int.MaxValue);
            byte[] req = LifxPacket.Build(LifxMessageTypes.GetExtendedColorZones, target, qSource, 1, ReadOnlySpan<byte>.Empty);
            try { await qudp.SendAsync(req, endpoint).ConfigureAwait(false); }
            catch { return 0; }

            while (!cts.IsCancellationRequested)
            {
                var rcv = await ReceiveOrCancelAsync(qudp, deadlineTcs.Task).ConfigureAwait(false);
                if (rcv == null) return 0;
                if (!LifxPacket.TryReadHeader(rcv.Value.Buffer, out var hdr)) continue;
                if (hdr.Source != qSource) continue;
                if (hdr.MessageType != LifxMessageTypes.StateExtendedColorZones) continue;
                var p = LifxPacket.Payload(rcv.Value.Buffer);
                if (p.Length < 2) return 0;
                return BinaryPrimitives.ReadUInt16LittleEndian(p[..2]);
            }
            return 0;
        }

        public async Task RestoreOriginalStateAsync(CancellationToken ct = default)
        {
            if (_original == null) return;
            try
            {
                // Take _lock so the colour send can't interleave with an
                // in-flight Update from the trigger thread. RemoveDevice
                // sets _perDeviceDisable=true before invoking us, so any
                // queued decorator data that races surface.Detach gets
                // dropped (no-op Update); the lock here covers the case
                // where Update was already inside its critical section,
                // mid-chunk, when the disable fired. Without this, the
                // restore's per-chunk SetExtendedColorZones packets
                // interleaved with the decorator's late chunks and
                // produced "half the strip restored, half left at the
                // last decorator colour" on chained Beam setups.
                lock (_lock)
                {
                    bool isMultizone = _def.ZoneCount > 1 || _product.IsMultizone;
                    if (isMultizone && _original.Zones != null && _original.Zones.Length > 0)
                    {
                        SendExtendedColorZones(_original.Zones, _original.Zones.Length, durationMs: 200);
                    }
                    else
                    {
                        SendSetColor(_original.Hue, _original.Saturation, _original.Brightness, _original.Kelvin, durationMs: 200);
                    }
                }

                // Brief gap so the bulb finishes the colour transition
                // before we toggle power. Done outside the lock so
                // shorter-running calls (a one-shot SetColor on a single
                // bulb) can release the queue between the colour and
                // power packets.
                await Task.Delay(50, ct).ConfigureAwait(false);

                lock (_lock)
                {
                    SendSetLightPower(_original.Powered, durationMs: 200);
                }
            }
            catch { /* swallow — best-effort during teardown */ }
        }

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

                    // Multizone if the device itself reports more than one
                    // zone OR the catalog flags the SKU. Matches the gate
                    // in LifxDevice.InitializeLayout so layout and dispatch
                    // can't disagree.
                    bool isMultizone = _def.ZoneCount > 1 || _product.IsMultizone;

                    if (isMultizone)
                    {
                        SendMultizoneFrame(dataSet, brightnessScale);
                    }
                    else if (_product.IsMatrix)
                    {
                        SendMatrixFrame(dataSet, brightnessScale);
                    }
                    else
                    {
                        SendSingleColorFrame(dataSet, brightnessScale);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LifxRGBDeviceProvider.Instance?.Throw(ex);
                    return false;
                }
            }
        }

        private void SendSingleColorFrame(ReadOnlySpan<(object key, Color color)> dataSet, double brightnessScale)
        {
            // Use the brightest LED's color when several are mapped to a single
            // bulb (uncommon but possible with custom layouts). Falls back to
            // dataSet[0] which matches simple 1-LED layouts.
            var color = PickRepresentativeColor(dataSet);
            ToHsbk(color, brightnessScale, out ushort h, out ushort s, out ushort b, out ushort k);

            byte[] payload = BuildSetColorPayload(h, s, b, k, durationMs: 150);
            if (PayloadEqual(payload, _lastSinglePayload)) return;
            _lastSinglePayload = payload;

            byte[] packet = BuildPacket(LifxMessageTypes.SetColor, payload);
            SendPacket(packet);
        }

        // RGB.NET's UpdateQueue.OnUpdate only fires our Update() when there's
        // dirty data, AND it passes only the LEDs that changed since last
        // update — NOT the full strip every frame. The dataSet's `key` is
        // the LedId (or CustomData) the device's GetUpdateData returned,
        // which for our LifxDevice layout is `LedId.Custom1 + zoneIndex`.
        //
        // This means the queue has to maintain its OWN persistent strip
        // state, indexed by zone, and only patch the entries whose LedId
        // shows up in dataSet. Treating dataSet as "the full strip
        // contents" — like an early version of this code did — was sending
        // the wrong colours to the wrong zones for sparse decorators
        // (starfield: only the moving sparks land in dataSet, getting
        // packed into the first N zones rather than their actual zone
        // indices). Beyond just looking wrong it also confined every
        // sparse update to whichever chunk happened to contain the first
        // N zones, which is the "first 23 LEDs respond, rest stale"
        // behaviour from before.
        private const int ChunkSize = 22;
        private const byte ApplyAndCommit = 1;
        private byte[] _strip;       // persistent zone HSBK state, [zoneCount * 8] bytes
        private int _stripZones;     // cached zone count for _strip's allocation

        private void SendMultizoneFrame(ReadOnlySpan<(object key, Color color)> dataSet, double brightnessScale)
        {
            int zones = _def.ZoneCount > 0 ? _def.ZoneCount : 1;
            if (_strip == null || _stripZones != zones)
            {
                _strip = new byte[zones * 8];
                _stripZones = zones;
            }

            int chunkCount = (zones + ChunkSize - 1) / ChunkSize;
            Span<bool> chunkDirty = chunkCount <= 16 ? stackalloc bool[chunkCount] : new bool[chunkCount];

            foreach (var (key, color) in dataSet)
            {
                int idx = ZoneIndexFromKey(key);
                if (idx < 0 || idx >= zones) continue;

                ToHsbk(color, brightnessScale, out ushort h, out ushort s, out ushort b, out ushort k);
                int off = idx * 8;
                ushort prevH = BinaryPrimitives.ReadUInt16LittleEndian(_strip.AsSpan(off + 0, 2));
                ushort prevS = BinaryPrimitives.ReadUInt16LittleEndian(_strip.AsSpan(off + 2, 2));
                ushort prevB = BinaryPrimitives.ReadUInt16LittleEndian(_strip.AsSpan(off + 4, 2));
                ushort prevK = BinaryPrimitives.ReadUInt16LittleEndian(_strip.AsSpan(off + 6, 2));

                if (prevH == h && prevS == s && prevB == b && prevK == k) continue;

                BinaryPrimitives.WriteUInt16LittleEndian(_strip.AsSpan(off + 0, 2), h);
                BinaryPrimitives.WriteUInt16LittleEndian(_strip.AsSpan(off + 2, 2), s);
                BinaryPrimitives.WriteUInt16LittleEndian(_strip.AsSpan(off + 4, 2), b);
                BinaryPrimitives.WriteUInt16LittleEndian(_strip.AsSpan(off + 6, 2), k);

                chunkDirty[idx / ChunkSize] = true;
            }

            // Devices flagged as non-extended in the catalog use legacy
            // SetColorZones (run-length-encoded over the full strip).
            if (_product.IsMultizone && !_product.IsExtendedMultizone)
            {
                bool anyDirty = false;
                foreach (var d in chunkDirty) if (d) { anyDirty = true; break; }
                if (anyDirty) SendLegacyMultizoneFrame(_strip, zones);
                return;
            }

            // Send each dirty chunk, paced. Sparse updates (1-2 zones
            // change in starfield) typically dirty 1 chunk → 1 packet, no
            // pacing. Dense updates (gradient sweep) dirty all chunks →
            // 30ms pacing keeps them under the firmware's burst-drop
            // threshold (6ms wasn't enough; 30ms × 3 = 90ms total send
            // time leaves the trigger thread ~10ms of slack within a 100ms
            // budget).
            bool firstSent = true;
            for (int c = 0; c < chunkCount; c++)
            {
                if (!chunkDirty[c]) continue;

                if (!firstSent) Thread.Sleep(30);
                firstSent = false;

                int start = c * ChunkSize;
                int count = Math.Min(ChunkSize, zones - start);
                SendExtendedChunk(_strip, start, count, durationMs: 150);
            }
        }

        private static int ZoneIndexFromKey(object key)
        {
            return key switch
            {
                LedId id => (int)id - (int)LedId.Custom1,
                int i => i - (int)LedId.Custom1,
                _ => -1,
            };
        }

        private void SendExtendedChunk(byte[] frame, int start, int count, uint durationMs)
        {
            byte[] payload = new byte[8 + count * 8];
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), durationMs);
            payload[4] = ApplyAndCommit;
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(5, 2), (ushort)start);
            payload[7] = (byte)count;
            Array.Copy(frame, start * 8, payload, 8, count * 8);

            byte[] packet = BuildPacket(LifxMessageTypes.SetExtendedColorZones, payload);
            SendPacket(packet);
        }

        // Legacy SetColorZones path: one packet per run of consecutive
        // same-coloured zones. Most decorator output has spatial coherence
        // (gradients, solid blocks, repeating patterns), so this typically
        // collapses 70 zones into a handful of packets. Safe on any LIFX
        // multizone firmware.
        //
        // SetColorZones payload:
        //   start_index (u8) | end_index (u8) | hsbk (8 bytes) | duration (u32) | apply (u8)
        private void SendLegacyMultizoneFrame(byte[] frame, int zones)
        {
            int runStart = 0;
            for (int i = 1; i <= zones; i++)
            {
                bool sameAsPrev = i < zones && SpanEquals(frame.AsSpan(i * 8, 8), frame.AsSpan((i - 1) * 8, 8));
                if (sameAsPrev) continue;

                int runEnd = i - 1;

                byte[] payload = new byte[15];
                payload[0] = (byte)runStart;
                payload[1] = (byte)runEnd;
                Array.Copy(frame, runStart * 8, payload, 2, 8);
                BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(10, 4), 150);
                payload[14] = ApplyAndCommit;

                byte[] packet = BuildPacket(LifxMessageTypes.SetColorZones, payload);
                SendPacket(packet);

                runStart = i;
            }
        }

        private static bool SpanEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        // Persistent matrix state, same indexing as multizone: dataSet
        // contains only changed cells, and we patch them into the saved
        // grid by LedId rather than packing them into the first N cells.
        private byte[] _matrixCells; // [64 * 8] HSBK bytes

        private void SendMatrixFrame(ReadOnlySpan<(object key, Color color)> dataSet, double brightnessScale)
        {
            int width = _product.HintMatrixWidth > 0 ? _product.HintMatrixWidth : 8;
            int height = _product.HintMatrixHeight > 0 ? _product.HintMatrixHeight : 8;
            int cells = Math.Min(64, width * height);

            if (_matrixCells == null) _matrixCells = new byte[64 * 8];

            bool anyChanged = false;
            foreach (var (key, color) in dataSet)
            {
                int idx = ZoneIndexFromKey(key);
                if (idx < 0 || idx >= cells) continue;

                ToHsbk(color, brightnessScale, out ushort h, out ushort s, out ushort b, out ushort k);
                int off = idx * 8;
                ushort prevH = BinaryPrimitives.ReadUInt16LittleEndian(_matrixCells.AsSpan(off + 0, 2));
                ushort prevS = BinaryPrimitives.ReadUInt16LittleEndian(_matrixCells.AsSpan(off + 2, 2));
                ushort prevB = BinaryPrimitives.ReadUInt16LittleEndian(_matrixCells.AsSpan(off + 4, 2));
                ushort prevK = BinaryPrimitives.ReadUInt16LittleEndian(_matrixCells.AsSpan(off + 6, 2));
                if (prevH == h && prevS == s && prevB == b && prevK == k) continue;

                BinaryPrimitives.WriteUInt16LittleEndian(_matrixCells.AsSpan(off + 0, 2), h);
                BinaryPrimitives.WriteUInt16LittleEndian(_matrixCells.AsSpan(off + 2, 2), s);
                BinaryPrimitives.WriteUInt16LittleEndian(_matrixCells.AsSpan(off + 4, 2), b);
                BinaryPrimitives.WriteUInt16LittleEndian(_matrixCells.AsSpan(off + 6, 2), k);
                anyChanged = true;
            }
            if (!anyChanged) return;

            // SetTileState64 payload:
            //   tile_index (u8) | length (u8) | reserved (u8) | x (u8) | y (u8) | width (u8) | duration (u32) | 64 * HSBK (8b)
            byte[] payload = new byte[10 + 64 * 8];
            payload[0] = 0;             // tile_index
            payload[1] = 1;             // length (single tile)
            payload[2] = 0;             // reserved
            payload[3] = 0;             // x
            payload[4] = 0;             // y
            payload[5] = (byte)width;   // width
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(6, 4), 150);
            Array.Copy(_matrixCells, 0, payload, 10, 64 * 8);

            byte[] packet = BuildPacket(LifxMessageTypes.SetTileState64, payload);
            SendPacket(packet);
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

        // RGB.NET's Color uses 0..1 floats; LIFX HSBK uses 0..65535 for HSB and
        // 1500..9000 for K. Black collapses to brightness 0 (saturation kept
        // at 0 so the bulb cleanly snaps off without going through a coloured
        // off-state). Kelvin is fixed at 3500K — the bulb's xy chromaticity
        // comes from H/S, so K only matters when S=0; 3500K reads as a
        // pleasant neutral white in that edge case.
        private static void ToHsbk(Color rgb, double brightnessScale, out ushort h, out ushort s, out ushort b, out ushort k)
        {
            double r = Math.Clamp(rgb.R, 0.0, 1.0);
            double g = Math.Clamp(rgb.G, 0.0, 1.0);
            double bb = Math.Clamp(rgb.B, 0.0, 1.0);

            double max = Math.Max(r, Math.Max(g, bb));
            double min = Math.Min(r, Math.Min(g, bb));
            double delta = max - min;

            double hue = 0;
            if (delta > 0)
            {
                if (max == r)      hue = ((g - bb) / delta) % 6;
                else if (max == g) hue = ((bb - r) / delta) + 2;
                else               hue = ((r - g) / delta) + 4;
                hue *= 60;
                if (hue < 0) hue += 360;
            }

            double sat = max == 0 ? 0 : delta / max;
            double bri = max * Math.Clamp(brightnessScale, 0.0, 1.0);

            h = (ushort)Math.Round((hue / 360.0) * 65535);
            s = (ushort)Math.Round(sat * 65535);
            b = (ushort)Math.Round(bri * 65535);
            k = 3500;
        }

        private static byte[] BuildSetColorPayload(ushort hue, ushort sat, ushort bri, ushort kelvin, uint durationMs)
        {
            // Layout: reserved (u8) | hue (u16) | sat (u16) | bri (u16) | kelvin (u16) | duration (u32)
            byte[] payload = new byte[13];
            payload[0] = 0;
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(1, 2), hue);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(3, 2), sat);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(5, 2), bri);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(7, 2), kelvin);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(9, 4), durationMs);
            return payload;
        }

        private void SendSetColor(ushort h, ushort s, ushort b, ushort k, uint durationMs)
        {
            byte[] payload = BuildSetColorPayload(h, s, b, k, durationMs);
            byte[] packet = BuildPacket(LifxMessageTypes.SetColor, payload);
            SendPacket(packet);
        }

        private void SendSetLightPower(bool on, uint durationMs)
        {
            byte[] payload = new byte[6];
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), on ? (ushort)65535 : (ushort)0);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(2, 4), durationMs);
            byte[] packet = BuildPacket(LifxMessageTypes.SetLightPower, payload);
            SendPacket(packet);
        }

        // Restore-time multizone send: chunked SetExtendedColorZones for
        // extended-capable products, run-length-encoded SetColorZones for
        // legacy. Mirrors the active-frame path so a Beam restored on
        // disable behaves the same as a Beam being painted live.
        private void SendExtendedColorZones(HSBK[] zones, int zoneCount, uint durationMs)
        {
            byte[] frame = new byte[zoneCount * 8];
            for (int i = 0; i < zoneCount; i++)
            {
                int off = i * 8;
                BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(off + 0, 2), zones[i].H);
                BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(off + 2, 2), zones[i].S);
                BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(off + 4, 2), zones[i].B);
                BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(off + 6, 2), zones[i].K);
            }

            if (_product.IsMultizone && !_product.IsExtendedMultizone)
            {
                SendLegacyMultizoneFrame(frame, zoneCount);
                return;
            }

            // Restore is a one-shot during Dispose — pace chunks generously
            // (60ms apart) so the LIFX firmware processes each before the
            // next arrives. Total restore time scales linearly with zone
            // count: 61 zones = 3 chunks ≈ 120ms, comfortably within the
            // 1.5s per-device timeout in LifxRGBDeviceProvider.Dispose.
            int chunks = (zoneCount + ChunkSize - 1) / ChunkSize;
            for (int c = 0; c < chunks; c++)
            {
                int start = c * ChunkSize;
                int count = Math.Min(ChunkSize, zoneCount - start);
                SendExtendedChunk(frame, start, count, durationMs);
                if (c < chunks - 1) Thread.Sleep(60);
            }
        }

        private byte[] BuildPacket(ushort messageType, ReadOnlySpan<byte> payload)
        {
            byte seq;
            unchecked { seq = ++_seq; }
            return LifxPacket.Build(messageType, _target, _source, seq, payload);
        }

        private void SendPacket(byte[] packet)
        {
            try
            {
                if (_def.Endpoint != null)
                    _udp.Send(packet, packet.Length, _def.Endpoint);
            }
            catch (ObjectDisposedException) { /* shutdown race */ }
            catch (SocketException) { /* network blip; trigger will retry */ }
        }

        private static bool PayloadEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        // ── Original state capture (provider-driven, blocking-wait based) ──
        // No CancellationToken on the UDP I/O calls — passing one makes them
        // throw on cancellation, which the debugger breaks on first-chance
        // even when the exception is caught (every adoption-dialog open
        // would trigger N breaks, one per device). Instead we race each
        // ReceiveAsync against a TaskCompletionSource that completes
        // (without throwing) when the deadline fires; the orphan receive
        // task is left to fault when its socket is disposed.
        private async Task<OriginalState> QueryOriginalStateAsync(TimeSpan perCallTimeout, CancellationToken ct)
        {
            using var qudp = new UdpClient(0);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(perCallTimeout * 4);

            var deadlineTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var reg = cts.Token.Register(() => deadlineTcs.TrySetResult());

            uint qSource = (uint)Random.Shared.Next(2, int.MaxValue);
            byte[] tgt = _target;

            // GetColor → LightState
            byte[] getColor = LifxPacket.Build(LifxMessageTypes.GetColor, tgt, qSource, 1, ReadOnlySpan<byte>.Empty);
            try { await qudp.SendAsync(getColor, _def.Endpoint).ConfigureAwait(false); }
            catch { return null; }

            ushort h = 0, s = 0, b = 0, k = 3500;
            bool gotColor = false;
            while (!gotColor && !cts.IsCancellationRequested)
            {
                var rcv = await ReceiveOrCancelAsync(qudp, deadlineTcs.Task).ConfigureAwait(false);
                if (rcv == null) break;
                if (!LifxPacket.TryReadHeader(rcv.Value.Buffer, out var hdr)) continue;
                if (hdr.Source != qSource) continue;
                if (hdr.MessageType != LifxMessageTypes.LightState) continue;

                var p = LifxPacket.Payload(rcv.Value.Buffer);
                if (p.Length < 8) break;
                h = BinaryPrimitives.ReadUInt16LittleEndian(p[..2]);
                s = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(2, 2));
                b = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(4, 2));
                k = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(6, 2));
                gotColor = true;
            }
            if (!gotColor) return null;

            // GetPower → StatePower (uint16 level, >0 = on)
            byte[] getPower = LifxPacket.Build(LifxMessageTypes.GetPower, tgt, qSource, 2, ReadOnlySpan<byte>.Empty);
            try { await qudp.SendAsync(getPower, _def.Endpoint).ConfigureAwait(false); }
            catch { return null; }

            bool powered = true;
            bool gotPower = false;
            while (!gotPower && !cts.IsCancellationRequested)
            {
                var rcv = await ReceiveOrCancelAsync(qudp, deadlineTcs.Task).ConfigureAwait(false);
                if (rcv == null) break;
                if (!LifxPacket.TryReadHeader(rcv.Value.Buffer, out var hdr)) continue;
                if (hdr.Source != qSource) continue;
                if (hdr.MessageType != LifxMessageTypes.StatePower) continue;
                var p = LifxPacket.Payload(rcv.Value.Buffer);
                if (p.Length < 2) break;
                powered = BinaryPrimitives.ReadUInt16LittleEndian(p[..2]) > 0;
                gotPower = true;
            }

            HSBK[] zoneSnapshot = null;
            if (_def.ZoneCount > 1 || _product.IsMultizone)
            {
                zoneSnapshot = await QueryZonesAsync(qudp, qSource, deadlineTcs.Task).ConfigureAwait(false);
            }

            return new OriginalState
            {
                Hue = h, Saturation = s, Brightness = b, Kelvin = k,
                Powered = powered,
                Zones = zoneSnapshot,
            };
        }

        // Race a single UdpClient.ReceiveAsync against the deadline TCS. Returns
        // null when the deadline wins or when the receive faults.
        private static async Task<System.Net.Sockets.UdpReceiveResult?> ReceiveOrCancelAsync(UdpClient udp, Task cancelTask)
        {
            try
            {
                var receiveTask = udp.ReceiveAsync();
                var winner = await Task.WhenAny(receiveTask, cancelTask).ConfigureAwait(false);
                if (winner != receiveTask) return null;
                return receiveTask.Result;
            }
            catch { return null; }
        }

        private async Task<HSBK[]> QueryZonesAsync(UdpClient qudp, uint qSource, Task cancelTask)
        {
            byte[] req = LifxPacket.Build(LifxMessageTypes.GetExtendedColorZones, _target, qSource, 3, ReadOnlySpan<byte>.Empty);
            try { await qudp.SendAsync(req, _def.Endpoint).ConfigureAwait(false); }
            catch { return null; }

            try
            {
                var rcv = await ReceiveOrCancelAsync(qudp, cancelTask).ConfigureAwait(false);
                if (rcv == null) return null;
                if (!LifxPacket.TryReadHeader(rcv.Value.Buffer, out var hdr)) return null;
                if (hdr.Source != qSource) return null;
                if (hdr.MessageType != LifxMessageTypes.StateExtendedColorZones) return null;
                var p = LifxPacket.Payload(rcv.Value.Buffer);
                if (p.Length < 5) return null;

                ushort zonesCount = BinaryPrimitives.ReadUInt16LittleEndian(p[..2]);
                byte colorsCount = p[4];
                int n = Math.Min(zonesCount, colorsCount);
                if (n <= 0) return null;
                if (p.Length < 5 + n * 8) return null;

                var arr = new HSBK[n];
                for (int i = 0; i < n; i++)
                {
                    int off = 5 + i * 8;
                    arr[i].H = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(off + 0, 2));
                    arr[i].S = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(off + 2, 2));
                    arr[i].B = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(off + 4, 2));
                    arr[i].K = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(off + 6, 2));
                }
                return arr;
            }
            catch { return null; }
        }

        private struct HSBK { public ushort H, S, B, K; }

        private class OriginalState
        {
            public ushort Hue, Saturation, Brightness, Kelvin;
            public bool Powered;
            public HSBK[] Zones;
        }

        #endregion
    }
}
