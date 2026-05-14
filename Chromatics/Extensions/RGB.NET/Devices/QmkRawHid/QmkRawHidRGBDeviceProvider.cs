using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol;
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
    //   - OpenRGB-QMK plugin (per-key control via direct mode)
    //
    // Protocol is decided per device at handshake. VIA-only keyboards
    // become a single-LED device that drives the firmware's RGB matrix
    // base colour; OpenRGB-QMK keyboards become a full per-key keyboard
    // with semantic LedId.Keyboard_* IDs when a VIA keymap is fetchable
    // from www.caniusevia.com (Custom1..N fallback otherwise).
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
                        layout: layout);

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

        // For VIA-only: a single Custom1 LED.
        // For OpenRGB-QMK: enumerate the firmware's LED records (paged via
        // Cmd_GetLedInfo), then merge against the optional VIA keymap to
        // produce the semantic LedId list.
        private static IReadOnlyList<QmkLedLayoutEntry> BuildLayoutForCandidate(
            HidStream stream,
            QmkRawHidDiscovery.Candidate candidate,
            QmkKeymapFetcher.QmkKeymap keymap)
        {
            if (candidate.Protocol == QmkRawHidDiscovery.ProtocolSupport.ViaOnly)
            {
                return new[]
                {
                    new QmkLedLayoutEntry(0, 0, 0, LedId.Custom1, new Point(0, 0), new Size(60, 60)),
                };
            }

            var records = FetchAllLedRecords(stream, candidate.LedCount);
            if (records.Count == 0)
            {
                // OpenRGB-QMK responded to GetDeviceInfo but failed to return
                // LED records — degrade to a synthetic grid sized by LedCount
                // so the device still appears for the user to position
                // manually in the Mapping tab.
                return SyntheticGrid(candidate.LedCount, candidate.MatrixColumns);
            }

            return QmkKeymapFetcher.BuildLayout(keymap, records);
        }

        private static List<(int firmwareIndex, byte col, byte row)> FetchAllLedRecords(HidStream stream, int totalLeds)
        {
            var records = new List<(int, byte, byte)>(totalLeds);
            byte[] outBuf = new byte[QmkRawHidConstants.OutputReportBytes];
            byte[] inBuf = new byte[QmkRawHidConstants.ReportPayloadBytes + 1];

            int next = 0;
            int safetyBudget = (totalLeds / 8) + 32; // upper bound on batch iterations
            while (next < totalLeds && safetyBudget-- > 0)
            {
                OpenRgbQmkProtocol.BuildGetLedInfo(
                    new Span<byte>(outBuf, 1, QmkRawHidConstants.ReportPayloadBytes),
                    (ushort)next);
                try
                {
                    stream.Write(outBuf);
                    int n = stream.Read(inBuf, 0, inBuf.Length);
                    if (n <= 0) break;
                }
                catch { break; }

                if (!OpenRgbQmkProtocol.TryParseLedInfoBatch(
                        new ReadOnlySpan<byte>(inBuf, 1, inBuf.Length - 1),
                        out int batchCount, out var batch))
                {
                    break;
                }
                if (batchCount == 0) break;

                for (int i = 0; i < batchCount && next < totalLeds; i++, next++)
                {
                    byte col = batch[i * 3];
                    byte row = batch[i * 3 + 1];
                    // batch[i*3+2] is the flags byte; not used here.
                    records.Add((next, col, row));
                }
            }
            return records;
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
                        c.MatrixColumns, c.MatrixRows, string.Empty, layout);

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
