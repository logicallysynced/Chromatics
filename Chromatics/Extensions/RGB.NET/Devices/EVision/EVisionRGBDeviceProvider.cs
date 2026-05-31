using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.EVision.Protocol;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.EVision
{
    // Custom RGB.NET device provider for EVision-firmware keyboards.
    // One OpenRGB driver (EVisionKeyboardController, V1) covers every
    // device this provider speaks to: Glorious GMMK TKL, Redragon K550
    // / K552 / K552-2 / K556, Tecware Phantom Elite, Womier K66/K87,
    // Mars Gaming MKMini, Skillkorp K5, DEXP Blaze, Warrior Kane TC235,
    // Gamepower Ogre RGB. All 13 share the same Sonix VS11K28A chip
    // and the same wire protocol.
    //
    // Auto-enrol on enable - every detected board comes up at once and
    // the Mapping tab is the per-device disable. Hot-plug mirrors the
    // QMK / Redragon pattern (DeviceList.Local.Changed +
    // 1500ms debounced reconcile).
    //
    // The protocol writes to the keyboard's firmware flash on every
    // accepted frame. SettingsViewModel surfaces a one-shot dialog on
    // first enable that explains the trade-off; the dialog text lives
    // in en.json and is shown via EVisionFlashHintDialog.
    public class EVisionRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Singleton

        private static EVisionRGBDeviceProvider _instance;
        public static EVisionRGBDeviceProvider Instance => _instance ?? new EVisionRGBDeviceProvider();

        public EVisionRGBDeviceProvider()
        {
            if (_instance != null) Throw(new Exception($"There can be only one instance of type {nameof(EVisionRGBDeviceProvider)}"));
            _instance = this;
        }

        #endregion

        private readonly Dictionary<IRGBDevice, HidStream> _openStreams = new();
        private readonly Dictionary<IRGBDevice, string> _devicePaths = new();
        private bool _hotplugSubscribed;
        private int _hotplugScheduleSeq;

        // 10Hz. Lower than the standard 30Hz to cut flash wear from
        // the V1 protocol's write-per-frame behaviour. The EVision
        // firmware also synchronously ACKs every one of the nine HID
        // reports that make up a frame, so 30Hz would put the device
        // close to its USB EP throughput ceiling regardless.
        private const double UpdateFrequencySeconds = 1.0 / 10.0;

        private const int HotplugDebounceMs = 1500;

        protected override void InitializeSDK()
        {
            if (!_hotplugSubscribed)
            {
                DeviceList.Local.Changed += OnHidDeviceListChanged;
                _hotplugSubscribed = true;
            }
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
            => new EVisionUpdateTrigger(UpdateFrequencySeconds);

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            var devices = new List<IRGBDevice>();

            var candidates = EVisionDiscovery.Discover();
            if (candidates.Count == 0)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    "[EVision] No EVision-firmware keyboards detected on the USB bus. " +
                    "Plug the keyboard in directly (not through a hub that strips vendor-defined HID interfaces) " +
                    "and close any other lighting app holding the HID interface exclusively (OpenRGB, the vendor utility).",
                    forwardToSentry: false);
                return devices;
            }

            foreach (var c in candidates)
            {
                try
                {
                    if (!c.Hid.TryOpen(out HidStream stream))
                    {
                        Logger.WriteConsole(LoggerTypes.Devices,
                            $"[EVision] Could not open {c.Model.DisplayName(c.Hid.VendorID, c.Hid.ProductID)} ({c.Hid.VendorID:X4}:{c.Hid.ProductID:X4}). " +
                            "Another app may be holding the HID interface (OpenRGB, the vendor utility).",
                            forwardToSentry: false);
                        continue;
                    }
                    // Generous timeouts - each frame's nine ACKs go in
                    // sequence, and the device sometimes pauses on the
                    // Begin ACK while it stages flash. 500ms per packet
                    // covers the worst stagger we've seen in OpenRGB
                    // community reports.
                    stream.ReadTimeout = 500;
                    stream.WriteTimeout = 500;

                    var def = new EVisionClientDefinition(
                        vendorId: c.Hid.VendorID,
                        productId: c.Hid.ProductID,
                        model: c.Model,
                        manufacturer: c.Manufacturer,
                        product: string.IsNullOrEmpty(c.Product) ? c.Model.DisplayName(c.Hid.VendorID, c.Hid.ProductID) : c.Product,
                        devicePath: c.Hid.DevicePath);

                    var trigger = (EVisionUpdateTrigger)GetUpdateTrigger();
                    var queue = new EVisionUpdateQueue(trigger, def, stream);
                    var info = new EVisionDeviceInfo(def);
                    var dev = new EVisionDevice(info, queue, def);

                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[EVision] Adopted {def.Product} ({def.VendorId:X4}:{def.ProductId:X4}).",
                        forwardToSentry: false);

                    _openStreams[dev] = stream;
                    _devicePaths[dev] = c.Hid.DevicePath;
                    devices.Add(dev);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error,
                        $"[EVision] Failed to set up {c.Model.DisplayName(c.Hid.VendorID, c.Hid.ProductID)}: {ex.Message}",
                        forwardToSentry: false);
                }
            }

            return devices;
        }

        // ── Hot-plug ──────────────────────────────────────────────────

        private void OnHidDeviceListChanged(object sender, DeviceListChangedEventArgs e)
        {
            int seq = System.Threading.Interlocked.Increment(ref _hotplugScheduleSeq);
            Task.Run(async () =>
            {
                await Task.Delay(HotplugDebounceMs).ConfigureAwait(false);
                if (seq != _hotplugScheduleSeq) return;
                try { Reconcile(); }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error, $"[EVision] hot-plug reconcile failed: {ex.Message}", forwardToSentry: false);
                }
            });
        }

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

            var existingPaths = new HashSet<string>(_devicePaths.Values, StringComparer.OrdinalIgnoreCase);
            var freshCandidates = EVisionDiscovery.Discover();
            foreach (var c in freshCandidates)
            {
                if (existingPaths.Contains(c.Hid.DevicePath)) continue;

                try
                {
                    if (!c.Hid.TryOpen(out HidStream stream)) continue;
                    stream.ReadTimeout = 500;
                    stream.WriteTimeout = 500;

                    var def = new EVisionClientDefinition(
                        c.Hid.VendorID, c.Hid.ProductID, c.Model,
                        c.Manufacturer,
                        string.IsNullOrEmpty(c.Product) ? c.Model.DisplayName(c.Hid.VendorID, c.Hid.ProductID) : c.Product,
                        c.Hid.DevicePath);

                    var trigger = (EVisionUpdateTrigger)GetUpdateTrigger();
                    var queue = new EVisionUpdateQueue(trigger, def, stream);
                    var info = new EVisionDeviceInfo(def);
                    var dev = new EVisionDevice(info, queue, def);

                    _openStreams[dev] = stream;
                    _devicePaths[dev] = c.Hid.DevicePath;
                    AddDevice(dev);

                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[EVision] Hot-plug adopted {def.Product} ({def.VendorId:X4}:{def.ProductId:X4}).",
                        forwardToSentry: false);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error,
                        $"[EVision] Hot-plug add failed for {c.Model.DisplayName(c.Hid.VendorID, c.Hid.ProductID)}: {ex.Message}",
                        forwardToSentry: false);
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

                foreach (var dev in Devices.OfType<EVisionDevice>())
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
    }
}
