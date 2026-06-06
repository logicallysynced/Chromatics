using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol;
using Chromatics.Localization;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // Custom RGB.NET device provider for QMK Raw HID keyboards — covers
    // NovelKeys, KBDFans, Drop, GMMK, Glorious and anything else running
    // QMK firmware with Raw HID enabled. Talks to two protocols on the
    // same HID interface:
    //   - VIA (universal, exposes only RGB matrix mode + base hue/sat/val)
    //   - OpenRGB-QMK firmware module (per-key control via direct mode)
    //
    // Protocol is decided per device at handshake. VIA-only keyboards
    // get a synthetic ANSI-104 layout built from KeyLocalization.QWERTY_Grid
    // — every LedId.Keyboard_* maps to firmware index 0 because VIA can't
    // address individual keys. Chromatics keyboard layers paint the keys
    // normally; the UpdateQueue picks one representative colour per frame
    // and sends it as the RGB matrix base colour. OpenRGB-QMK keyboards
    // get a full per-key keyboard with semantic LedId.Keyboard_* IDs when
    // a VIA keymap is fetchable from www.caniusevia.com (Custom1..N
    // fallback otherwise).
    //
    // Hot-plug parity with PlayStationControllerRGBDeviceProvider —
    // DeviceList.Local.Changed reconciles the open set on USB connect /
    // disconnect events. Adoption picker UX parity with LIFX/Hue —
    // ClientDefinitions is hydrated from SettingsModel before LoadDevices
    // runs; an empty list short-circuits the load so users explicitly opt
    // in to which boards Chromatics drives.
    public class QmkRawHidRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Singleton

        private static QmkRawHidRGBDeviceProvider _instance;
        public static QmkRawHidRGBDeviceProvider Instance => _instance ?? new QmkRawHidRGBDeviceProvider();

        public QmkRawHidRGBDeviceProvider()
        {
            if (_instance != null) Throw(new Exception($"There can be only one instance of type {nameof(QmkRawHidRGBDeviceProvider)}"));
            _instance = this;
        }

        #endregion

        #region Configuration & state

        // 30Hz update rate — same cadence as the PlayStation provider. QMK
        // boards accept sustained 30Hz over Raw HID without flooding the
        // USB EP RX queue; faster than this risks dropped packets on
        // multi-zone bulk updates.
        private const double UpdateFrequencySeconds = 1.0 / 30.0;

        // Same debounce as PlayStation. Windows fires several PnP events
        // for one logical USB connect; we wait long enough for the device
        // tree to settle before re-enumerating, otherwise TryOpen wins a
        // partially-enumerated handle and the first write fails.
        private const int HotplugDebounceMs = 1500;

        public List<QmkRawHidAdoptedDeviceFilter> AdoptedDevices { get; } = new();

        private readonly Dictionary<IRGBDevice, HidStream> _openStreams = new();
        private readonly Dictionary<IRGBDevice, string> _devicePaths = new();
        private bool _hotplugSubscribed;
        private int _hotplugScheduleSeq;

        #endregion

        protected override void InitializeSDK()
        {
            if (!_hotplugSubscribed)
            {
                DeviceList.Local.Changed += OnHidDeviceListChanged;
                _hotplugSubscribed = true;
            }
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
            => new QmkRawHidUpdateTrigger(UpdateFrequencySeconds);

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            return LoadDevicesAsync().GetAwaiter().GetResult();
        }

        // Async device load — runs the handshake on every candidate, fetches
        // VIA keymaps in parallel for adopted boards (network calls run
        // concurrently rather than serial), then materialises one
        // QmkRawHidDevice per adopted+responsive board.
        private async Task<IEnumerable<IRGBDevice>> LoadDevicesAsync()
        {
            var devices = new List<IRGBDevice>();
            if (AdoptedDevices.Count == 0) return devices;

            var candidates = QmkRawHidDiscovery.Discover();
            if (candidates.Count == 0)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    "[QMK] No QMK Raw HID keyboards detected on the USB bus. " +
                    "Make sure your keyboard's firmware has Raw HID enabled (it's the default for most VIA-compatible builds) " +
                    "and that no other app (VIA, Vial, OpenRGB) is holding the Raw HID interface exclusively.",
                    forwardToSentry: false);
                return devices;
            }

            // Reduce candidate list to adopted entries up-front — no point
            // running keymap fetches for boards the user didn't pick.
            var adoptedCandidates = new List<QmkRawHidDiscovery.Candidate>(candidates.Count);
            foreach (var c in candidates)
            {
                if (!IsAdopted(c)) continue;
                adoptedCandidates.Add(c);
            }
            if (adoptedCandidates.Count == 0) return devices;

            // Fetch keymaps in parallel for the OpenRGB-QMK ones. VIA-only
            // boards don't use keymaps (single LED), so skip those.
            var keymapTasks = new Dictionary<(int vid, int pid), Task<QmkKeymapFetcher.QmkKeymap>>();
            foreach (var c in adoptedCandidates)
            {
                if (c.Protocol != QmkRawHidDiscovery.ProtocolSupport.OpenRgbQmk) continue;
                var key = (c.Hid.VendorID, c.Hid.ProductID);
                if (keymapTasks.ContainsKey(key)) continue;
                keymapTasks[key] = QmkKeymapFetcher.TryGetKeymapAsync(c.Hid.VendorID, c.Hid.ProductID);
            }
            if (keymapTasks.Count > 0)
                await Task.WhenAll(keymapTasks.Values).ConfigureAwait(false);

            foreach (var c in adoptedCandidates)
            {
                try
                {
                    if (!c.Hid.TryOpen(out HidStream stream))
                    {
                        Logger.WriteConsole(LoggerTypes.Error,
                            $"[QMK] Could not open {SafeProductName(c.Hid)} ({c.Hid.VendorID:X4}:{c.Hid.ProductID:X4}). " +
                            "Another app may be holding the Raw HID interface exclusively (VIA, Vial, OpenRGB).",
                            forwardToSentry: false);
                        continue;
                    }
                    stream.ReadTimeout = QmkRawHidConstants.ResponseTimeoutMs;
                    stream.WriteTimeout = QmkRawHidConstants.ResponseTimeoutMs;

                    QmkKeymapFetcher.QmkKeymap keymap = null;
                    if (c.Protocol == QmkRawHidDiscovery.ProtocolSupport.OpenRgbQmk
                        && keymapTasks.TryGetValue((c.Hid.VendorID, c.Hid.ProductID), out var kmTask))
                    {
                        keymap = kmTask.Result;
                    }

                    var layout = BuildLayoutForCandidate(stream, c, keymap);

                    var def = new QmkRawHidClientDefinition(
                        vendorId: c.Hid.VendorID,
                        productId: c.Hid.ProductID,
                        manufacturer: SafeManufacturer(c.Hid),
                        product: SafeProductName(c.Hid),
                        firmwareDeviceName: c.FirmwareDeviceName,
                        ledCount: layout.Count == 0 ? 1 : layout.Count,
                        protocol: c.Protocol == QmkRawHidDiscovery.ProtocolSupport.OpenRgbQmk
                                  ? QmkRawHidProtocolMode.OpenRgbQmk
                                  : QmkRawHidProtocolMode.ViaOnly,
                        matrixColumns: c.MatrixColumns,
                        matrixRows: c.MatrixRows,
                        viaKeymapKey: string.Empty,
                        layout: layout,
                        inputReportByteLength: c.InputReportByteLength,
                        outputReportByteLength: c.OutputReportByteLength);

                    var trigger = (QmkRawHidUpdateTrigger)GetUpdateTrigger();
                    var queue = new QmkRawHidUpdateQueue(trigger, def, stream);
                    var info = new QmkRawHidDeviceInfo(def);
                    var dev = new QmkRawHidDevice(info, queue, def);

                    _openStreams[dev] = stream;
                    _devicePaths[dev] = c.Hid.DevicePath;
                    devices.Add(dev);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error,
                        $"[QMK] Failed to set up {SafeProductName(c.Hid)}: {ex.Message}",
                        forwardToSentry: false);
                }
            }

            return devices;
        }

        // For VIA-only:
        //   - Keyboards (HasKeyboardSibling): synthetic ANSI-104 layout
        //     from KeyLocalization.QWERTY_Grid. VIA's protocol only
        //     exposes a single RGB-matrix base colour + effect mode (not
        //     per-key addressing), but Chromatics's keyboard layers paint
        //     individual LedId.Keyboard_* LEDs — and a device that only
        //     owns Custom1 never receives those paints, so the firmware
        //     would never see a frame. Exposing the full key set lets
        //     keyboard processors paint normally; the UpdateQueue picks
        //     one representative colour from the dataset and sends it as
        //     the matrix colour. Every entry maps to firmware index 0.
        //   - Non-keyboards (macropads, knob boards, etc.): single Custom1
        //     entry. Keyboard layers would paint nonsense onto these so
        //     we don't expose phantom keyboard keys.
        //   Both paths include a Custom1 slot so saved layer configs from
        //   pre-4.2.34 builds (when every VIA-only device was a single
        //   Custom1 LED) keep painting after upgrade.
        // For OpenRGB-QMK: enumerate the firmware's LED records (paged via
        //   Cmd_GetLedInfo), then merge against the optional VIA keymap to
        //   produce the semantic LedId list.
        private static IReadOnlyList<QmkLedLayoutEntry> BuildLayoutForCandidate(
            HidStream stream,
            QmkRawHidDiscovery.Candidate candidate,
            QmkKeymapFetcher.QmkKeymap keymap)
        {
            if (candidate.Protocol == QmkRawHidDiscovery.ProtocolSupport.ViaOnly)
            {
                if (!candidate.HasKeyboardSibling)
                {
                    return new[]
                    {
                        new QmkLedLayoutEntry(0, 0, 0, LedId.Custom1, new Point(0, 0), new Size(60, 60)),
                    };
                }

                const float Cell = 19f;
                var grid = KeyLocalization.QWERTY_Grid;
                var list = new List<QmkLedLayoutEntry>(grid.Count + 1);
                foreach (var (ledId, rowCol) in grid)
                {
                    int row = rowCol[0];
                    int col = rowCol[1];
                    list.Add(new QmkLedLayoutEntry(
                        firmwareIndex: 0,
                        matrixCol: (byte)col,
                        matrixRow: (byte)row,
                        preferredLedId: ledId,
                        location: new Point(col * Cell, row * Cell),
                        size: new Size(Cell, Cell)));
                }
                // Backward-compat Custom1 slot: layers.chromatics4 entries
                // generated against the original single-Custom1 layout keep
                // painting after upgrade. Off-grid position so it doesn't
                // show up alongside the keyboard layout in the Mappings tab.
                list.Add(new QmkLedLayoutEntry(
                    firmwareIndex: 0,
                    matrixCol: 0, matrixRow: 0,
                    preferredLedId: LedId.Custom1,
                    location: new Point(-1000, -1000),
                    size: new Size(0, 0)));
                return list;
            }

            var raw = FetchAllLedRecords(stream, candidate);
            if (raw.Count == 0)
            {
                // OpenRGB-QMK responded to GetDeviceInfo but failed to return
                // LED records — degrade to a synthetic grid sized by LedCount
                // so the device still appears for the user to position
                // manually in the Mapping tab.
                return SyntheticGrid(candidate.LedCount, candidate.MatrixColumns);
            }

            return BuildLayoutFromKeycodes(raw);
        }

        // Pulls the firmware's per-LED records via Cmd_GetLedInfo. Each record
        // is 7 bytes: x | y | flags | r | g | b | keycode. The keycode byte
        // is the HID usage ID for the keymap[0][row][col] entry at the
        // matrix position this LED occupies — QmkKeycodeMap.FromKeycodeByte
        // resolves that to LedId.Keyboard_* directly, skipping the lossy
        // x/y → col/row binning the previous implementation used.
        private static List<(int firmwareIndex, byte x, byte y, byte keycode)> FetchAllLedRecords(HidStream stream, QmkRawHidDiscovery.Candidate candidate)
        {
            int outLen = candidate.OutputReportByteLength > 0 ? candidate.OutputReportByteLength : 33;
            int inLen  = candidate.InputReportByteLength  > 0 ? candidate.InputReportByteLength  : 33;
            int payloadOut = outLen - 1;

            int recordsPerPacket = OpenRgbQmkProtocol.MaxLedRecordsPerGetLedInfo(payloadOut);
            if (recordsPerPacket <= 0) recordsPerPacket = 1;

            byte[] outBuf = new byte[outLen];
            byte[] inBuf  = new byte[inLen];

            int totalLeds = candidate.LedCount;
            var raw = new List<(int idx, byte x, byte y, byte keycode)>(totalLeds);

            int next = 0;
            while (next < totalLeds)
            {
                int wantCount = Math.Min(recordsPerPacket, totalLeds - next);
                Array.Clear(outBuf, 0, outBuf.Length);
                OpenRgbQmkProtocol.BuildGetLedInfo(
                    new Span<byte>(outBuf, 1, payloadOut),
                    (byte)next, (byte)wantCount);
                try
                {
                    stream.Write(outBuf);
                    int n = stream.Read(inBuf, 0, inBuf.Length);
                    if (n <= 1) break;
                }
                catch { break; }

                var reply = new ReadOnlySpan<byte>(inBuf, 1, inBuf.Length - 1);
                bool gotAny = false;
                for (int i = 0; i < wantCount; i++)
                {
                    if (!OpenRgbQmkProtocol.TryParseLedInfoRecord(reply, i, out byte x, out byte y, out byte flags, out byte keycode))
                        break;
                    // Firmware writes OPENRGB_FAILURE (25) into the flags slot
                    // when the LED index is out of range — bail.
                    if (flags == OpenRgbQmkProtocol.Response_Failure) { gotAny = false; break; }
                    raw.Add((next + i, x, y, keycode));
                    gotAny = true;
                }
                if (!gotAny) break;
                next += wantCount;
            }

            return raw;
        }

        // Maps each LED to a semantic LedId via its firmware-reported keycode.
        // Underglow LEDs and vendor-custom keycodes (Keychron FN, brightness
        // cycle, etc. — usually 0xA0+) return LedId.Invalid from
        // QmkKeycodeMap.FromKeycodeByte and fall back to Custom1+i so they
        // remain individually addressable on the Mappings tab. Position on
        // the rendered keyboard uses the firmware's (x, y) pixel coords
        // (QMK's rgb_matrix coordinate system, x in 0..224, y in 0..64).
        private static IReadOnlyList<QmkLedLayoutEntry> BuildLayoutFromKeycodes(
            IReadOnlyList<(int firmwareIndex, byte x, byte y, byte keycode)> raw)
        {
            const float scale = 4f; // scale 0..224 pixel coords down to a reasonable on-screen footprint
            const float cell = 60f;
            var entries = new List<QmkLedLayoutEntry>(raw.Count);
            var seen = new HashSet<LedId>();
            var diag = new System.Text.StringBuilder();
            diag.Append("[QMK] per-LED resolution (firmwareIndex / keycode / resolved LedId):");

            for (int i = 0; i < raw.Count; i++)
            {
                var rec = raw[i];
                LedId resolved = ResolveKeycodePositionAware(rec.keycode, rec.x, rec.y);
                LedId finalId = resolved;
                string suffix = string.Empty;
                if (finalId == LedId.Invalid)
                {
                    finalId = (LedId)((int)LedId.Custom1 + i);
                    suffix = " (no keycode mapping)";
                }
                else if (!seen.Add(finalId))
                {
                    finalId = (LedId)((int)LedId.Custom1 + i);
                    suffix = $" (collision with earlier {resolved})";
                }

                diag.Append($"\n  #{rec.firmwareIndex} kc=0x{rec.keycode:X2} (x={rec.x},y={rec.y}) -> {finalId}{suffix}");

                var location = new Point(rec.x * scale, rec.y * scale);
                var size = new Size(cell, cell);
                entries.Add(new QmkLedLayoutEntry(rec.firmwareIndex, rec.x, rec.y, finalId, location, size));
            }

            Logger.WriteVerbose(diag.ToString());
            return entries;
        }

        // QMK exposes only the LOW BYTE of the 16-bit keycode in GetLedInfo,
        // so Keychron's QK_KB_0+N customs (KC_MCTRL=0x7E00 → low byte 0x00,
        // KC_LOPTN=0x7E02 → 0x02, KC_LCMMD=0x7E04 → 0x04 collides with KC_A,
        // etc.) can't be distinguished from standard HID usage IDs by the
        // byte alone. The Y coordinate disambiguates: QMK's rgb_matrix
        // coordinate system places the F-row near y=0, the bottom modifier
        // row near y=64, and the alpha keys between roughly y=26 and y=49.
        // We override the standard mapping for the low-byte values that
        // Keychron's c3_pro_8k custom-keycode enum claims, but only when the
        // LED is in the matching row range — alpha keys at y∈[26,49] keep
        // their KC_A..KC_Z mappings.
        private const byte FRowYMax       = 10;
        private const byte ModRowYMin     = 60;

        private static LedId ResolveKeycodePositionAware(byte keycode, byte x, byte y)
        {
            if (y <= FRowYMax)
            {
                // F-row Keychron customs + underglow control keys whose low
                // bytes collide with real keys further down the keyboard.
                // Returning a specific LedId (F3/F4/F5/F6/PrintScreen) gives
                // the F-row LED a semantic slot; returning Invalid sends the
                // LED to Custom_* and lets the real key claim its standard
                // LedId.
                switch (keycode)
                {
                    case 0x00: return LedId.Keyboard_F3;          // KC_MAC_MISSION_CONTROL
                    case 0x01: return LedId.Keyboard_F4;          // KC_MAC_LAUCHPAD
                    case 0x06: return LedId.Invalid;              // KC_MAC_SIRI — collides with KC_C, no LedId target
                    case 0x09: return LedId.Keyboard_PrintScreen; // KC_MAC_SCREEN_SHOT (KC_SNAP)
                    case 0x21: return LedId.Invalid;              // UG_NEXT — collides with KC_4
                    case 0x27: return LedId.Keyboard_F6;          // UG_VALU at the F6 position
                    case 0x28: return LedId.Keyboard_F5;          // UG_VALD at the F5 position
                }
            }
            else if (y >= ModRowYMin)
            {
                // Bottom modifier-row Keychron customs. Same enum, different
                // physical positions.
                switch (keycode)
                {
                    case 0x01: return LedId.Keyboard_Application; // MO(MAC_FN) — FN_MAC at the Right App / Menu position. Low byte is the layer index (1 for MAC_FN).
                    case 0x02: return LedId.Keyboard_LeftAlt;    // KC_LOPTN
                    case 0x03: return LedId.Keyboard_RightAlt;   // KC_ROPTN
                    case 0x04: return LedId.Keyboard_LeftGui;    // KC_LCMMD (overrides KC_A here)
                    case 0x05: return LedId.Keyboard_RightGui;   // KC_RCMMD (overrides KC_B here)
                }
            }
            return QmkKeycodeMap.FromKeycodeByte(keycode);
        }

        private static IReadOnlyList<QmkLedLayoutEntry> SyntheticGrid(int ledCount, byte hintColumns)
        {
            int cols = hintColumns > 0 ? hintColumns : Math.Max(1, (int)Math.Ceiling(Math.Sqrt(ledCount * 4.0 / 3.0)));
            const float cell = 60f;
            var list = new List<QmkLedLayoutEntry>(ledCount);
            for (int i = 0; i < ledCount; i++)
            {
                int col = i % cols;
                int row = i / cols;
                list.Add(new QmkLedLayoutEntry(
                    firmwareIndex: i,
                    matrixCol: (byte)col,
                    matrixRow: (byte)row,
                    preferredLedId: (LedId)((int)LedId.Custom1 + i),
                    location: new Point(col * cell, row * cell),
                    size: new Size(cell, cell)));
            }
            return list;
        }

        // ── Adopted-device matching ───────────────────────────────────

        private bool IsAdopted(QmkRawHidDiscovery.Candidate candidate)
        {
            foreach (var ad in AdoptedDevices)
            {
                if (ad.Matches(candidate.Hid)) return true;
            }
            return false;
        }

        // ── Hot-plug ──────────────────────────────────────────────────

        private void OnHidDeviceListChanged(object sender, DeviceListChangedEventArgs e)
        {
            int seq = System.Threading.Interlocked.Increment(ref _hotplugScheduleSeq);
            Task.Run(async () =>
            {
                await Task.Delay(HotplugDebounceMs).ConfigureAwait(false);
                if (seq != _hotplugScheduleSeq) return; // newer event superseded us
                try { Reconcile(); } catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error, $"[QMK] hot-plug reconcile failed: {ex.Message}", forwardToSentry: false);
                }
            });
        }

        // Re-enumerate USB and compare to our open set: remove disappeared
        // devices, add freshly-connected adopted ones. Same shape as the
        // PlayStation provider's reconcile.
        private void Reconcile()
        {
            var currentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var hid in DeviceList.Local.GetHidDevices())
                {
                    try { currentPaths.Add(hid.DevicePath); } catch { /* ignore */ }
                }
            }
            catch { return; }

            // Drop devices whose path is no longer present.
            var toRemove = new List<IRGBDevice>();
            foreach (var kvp in _devicePaths)
            {
                if (!currentPaths.Contains(kvp.Value))
                    toRemove.Add(kvp.Key);
            }
            foreach (var dev in toRemove)
            {
                RemoveDevice(dev);
                if (_openStreams.TryGetValue(dev, out var s))
                {
                    try { s.Dispose(); } catch { /* ignore */ }
                    _openStreams.Remove(dev);
                }
                _devicePaths.Remove(dev);
            }

            // Add newly-connected adopted devices.
            var existingPaths = new HashSet<string>(_devicePaths.Values, StringComparer.OrdinalIgnoreCase);
            var freshCandidates = QmkRawHidDiscovery.Discover();
            foreach (var c in freshCandidates)
            {
                if (!IsAdopted(c)) continue;
                if (existingPaths.Contains(c.Hid.DevicePath)) continue;

                try
                {
                    if (!c.Hid.TryOpen(out HidStream stream)) continue;
                    stream.ReadTimeout = QmkRawHidConstants.ResponseTimeoutMs;
                    stream.WriteTimeout = QmkRawHidConstants.ResponseTimeoutMs;

                    var keymap = c.Protocol == QmkRawHidDiscovery.ProtocolSupport.OpenRgbQmk
                        ? QmkKeymapFetcher.TryGetKeymapAsync(c.Hid.VendorID, c.Hid.ProductID).GetAwaiter().GetResult()
                        : null;

                    var layout = BuildLayoutForCandidate(stream, c, keymap);

                    var def = new QmkRawHidClientDefinition(
                        c.Hid.VendorID, c.Hid.ProductID,
                        SafeManufacturer(c.Hid), SafeProductName(c.Hid),
                        c.FirmwareDeviceName, layout.Count == 0 ? 1 : layout.Count,
                        c.Protocol == QmkRawHidDiscovery.ProtocolSupport.OpenRgbQmk
                            ? QmkRawHidProtocolMode.OpenRgbQmk : QmkRawHidProtocolMode.ViaOnly,
                        c.MatrixColumns, c.MatrixRows, string.Empty, layout,
                        c.InputReportByteLength, c.OutputReportByteLength);

                    var trigger = (QmkRawHidUpdateTrigger)GetUpdateTrigger();
                    var queue = new QmkRawHidUpdateQueue(trigger, def, stream);
                    var info = new QmkRawHidDeviceInfo(def);
                    var dev = new QmkRawHidDevice(info, queue, def);

                    _openStreams[dev] = stream;
                    _devicePaths[dev] = c.Hid.DevicePath;
                    AddDevice(dev);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error, $"[QMK] hot-plug add failed for {SafeProductName(c.Hid)}: {ex.Message}", forwardToSentry: false);
                }
            }
        }

        // ── Dispose ───────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hotplugSubscribed)
                {
                    try { DeviceList.Local.Changed -= OnHidDeviceListChanged; } catch { /* ignore */ }
                    _hotplugSubscribed = false;
                }

                foreach (var dev in Devices.OfType<QmkRawHidDevice>())
                {
                    try { dev.BeginShutdown(); } catch { /* ignore */ }
                }
                foreach (var s in _openStreams.Values)
                {
                    try { s.Dispose(); } catch { /* ignore */ }
                }
                _openStreams.Clear();
                _devicePaths.Clear();
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }

        // ── HidSharp string accessors (Manufacturer/Product can throw on disconnected handles) ──

        private static string SafeManufacturer(HidDevice hid)
        {
            try { return hid.GetManufacturer() ?? string.Empty; }
            catch { return string.Empty; }
        }
        private static string SafeProductName(HidDevice hid)
        {
            try { return hid.GetProductName() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }

    // Identity filter used by the adopted-set check. Built from the
    // SettingsModel's persisted QmkRawHidAdoptedDevice records so the
    // provider doesn't have to depend on the Models layer for runtime
    // matching.
    public sealed class QmkRawHidAdoptedDeviceFilter
    {
        public int VendorId { get; }
        public int ProductId { get; }
        public string Manufacturer { get; }
        public string Product { get; }

        public QmkRawHidAdoptedDeviceFilter(int vendorId, int productId, string manufacturer, string product)
        {
            VendorId = vendorId;
            ProductId = productId;
            Manufacturer = manufacturer ?? string.Empty;
            Product = product ?? string.Empty;
        }

        public bool Matches(HidDevice hid)
        {
            if (hid.VendorID != VendorId) return false;
            if (hid.ProductID != ProductId) return false;
            string mfg, prod;
            try { mfg = hid.GetManufacturer() ?? string.Empty; } catch { mfg = string.Empty; }
            try { prod = hid.GetProductName() ?? string.Empty; } catch { prod = string.Empty; }
            return string.Equals(mfg, Manufacturer, StringComparison.OrdinalIgnoreCase)
                && string.Equals(prod, Product, StringComparison.OrdinalIgnoreCase);
        }
    }
}
