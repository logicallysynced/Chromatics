using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.Redragon.Protocol;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon
{
    // Custom RGB.NET device provider for Redragon mice on the shared
    // OpenRGB protocol family. Pure managed implementation via HidSharp —
    // no OpenRGB server required, no SDK to install, no precompiled
    // bridge. All 13 known models (9 confirmed by OpenRGB, 4 by
    // dokutan/mouse_m908) share one HID feature-report protocol; the
    // single RedragonMouseProtocol class covers them all.
    //
    // Discovery model is auto-enrol: when the user enables the provider
    // in Settings, every Redragon mouse on the bus is adopted, no
    // selection dialog. Per-device disable lives on the Mapping tab —
    // same UX as Yeelight / Alienware. Hot-plug parity mirrors the QMK
    // provider: DeviceList.Local.Changed reconciles the open set on
    // connect / disconnect with a 1500ms debounce so PnP storms settle
    // before we re-enumerate.
    public class RedragonRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Singleton

        private static RedragonRGBDeviceProvider _instance;
        public static RedragonRGBDeviceProvider Instance => _instance ?? new RedragonRGBDeviceProvider();

        public RedragonRGBDeviceProvider()
        {
            if (_instance != null) Throw(new Exception($"There can be only one instance of type {nameof(RedragonRGBDeviceProvider)}"));
            _instance = this;
        }

        #endregion

        // Track open HID streams + USB paths so Dispose can release them
        // cleanly and the hot-plug reconcile knows what's already open.
        private readonly Dictionary<IRGBDevice, HidStream> _openStreams = new();
        private readonly Dictionary<IRGBDevice, string> _devicePaths = new();
        private bool _hotplugSubscribed;
        private int _hotplugScheduleSeq;

        // Update rate ceiling, floor, and default. Each frame is two HID
        // feature reports (colour write to 0x0449 + apply commit 0xF1), and
        // the firmware briefly dips PWM output on every commit. At 30Hz
        // those dips manifest as visible flicker - the M908 Impact is the
        // most affected board the OpenRGB driver covers. 15Hz puts the
        // dips below perception while keeping cycling effects smooth.
        // OpenRGB's effects engine pulses Direct mode at the effect's own
        // cadence (typically 5-15Hz), which is why OpenRGB users don't
        // see the same flicker we did at 30Hz.
        //
        // Power users override via SettingsModel.redragonUpdateRateHz -
        // hidden field, not exposed in the UI - clamped to [1, 30] at
        // provider startup so an out-of-range value can't break the
        // trigger.
        private const double MinUpdateRateHz = 1.0;
        private const double MaxUpdateRateHz = 30.0;
        private const double DefaultUpdateRateHz = 15.0;

        // Windows fires several PnP events for one logical USB connect; we
        // wait long enough for the device tree to settle before
        // re-enumerating, otherwise TryOpen wins a partially-enumerated
        // handle and the first feature-report write fails.
        private const int HotplugDebounceMs = 1500;

        private static double GetConfiguredUpdateRateSeconds()
        {
            double hz;
            try { hz = AppSettings.GetSettings()?.redragonUpdateRateHz ?? DefaultUpdateRateHz; }
            catch { hz = DefaultUpdateRateHz; }
            if (double.IsNaN(hz) || hz <= 0) hz = DefaultUpdateRateHz;
            hz = Math.Clamp(hz, MinUpdateRateHz, MaxUpdateRateHz);
            return 1.0 / hz;
        }

        protected override void InitializeSDK()
        {
            if (!_hotplugSubscribed)
            {
                DeviceList.Local.Changed += OnHidDeviceListChanged;
                _hotplugSubscribed = true;
            }
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
            => new RedragonUpdateTrigger(GetConfiguredUpdateRateSeconds());

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            var devices = new List<IRGBDevice>();

            var candidates = RedragonDiscovery.Discover();
            if (candidates.Count == 0)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    "[Redragon] No Redragon mice detected on the USB bus. " +
                    "Make sure your mouse is plugged in directly (not through a hub that strips vendor-defined HID interfaces) " +
                    "and that no other lighting app (OpenRGB, Razer Synapse, the Redragon utility) is holding the HID interface exclusively.",
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
                            $"[Redragon] Could not open {c.Model.DisplayName(c.Hid.ProductID)} ({c.Hid.VendorID:X4}:{c.Hid.ProductID:X4}). " +
                            "Another app may be holding the HID interface exclusively (OpenRGB, Redragon's own utility).",
                            forwardToSentry: false);
                        continue;
                    }

                    var def = new RedragonClientDefinition(
                        vendorId: c.Hid.VendorID,
                        productId: c.Hid.ProductID,
                        model: c.Model,
                        manufacturer: c.Manufacturer,
                        product: string.IsNullOrEmpty(c.Product) ? c.Model.DisplayName(c.Hid.ProductID) : c.Product,
                        devicePath: c.Hid.DevicePath);

                    var trigger = (RedragonUpdateTrigger)GetUpdateTrigger();
                    var queue = new RedragonUpdateQueue(trigger, def, stream);
                    var info = new RedragonDeviceInfo(def);
                    var dev = new RedragonDevice(info, queue, def);

                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[Redragon] Adopted {def.Product} ({def.VendorId:X4}:{def.ProductId:X4}).",
                        forwardToSentry: false);

                    _openStreams[dev] = stream;
                    _devicePaths[dev] = c.Hid.DevicePath;
                    devices.Add(dev);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error,
                        $"[Redragon] Failed to set up {c.Model.DisplayName(c.Hid.ProductID)}: {ex.Message}",
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
                if (seq != _hotplugScheduleSeq) return; // newer event superseded us
                try { Reconcile(); }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error, $"[Redragon] hot-plug reconcile failed: {ex.Message}", forwardToSentry: false);
                }
            });
        }

        // Re-enumerate USB and reconcile against the open set: drop
        // devices whose path is no longer present, add newly-connected
        // Redragon mice. Mirrors QmkRawHidRGBDeviceProvider.Reconcile.
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

            // Drop disappeared devices.
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

            // Add freshly-connected Redragon mice.
            var existingPaths = new HashSet<string>(_devicePaths.Values, StringComparer.OrdinalIgnoreCase);
            var freshCandidates = RedragonDiscovery.Discover();
            foreach (var c in freshCandidates)
            {
                if (existingPaths.Contains(c.Hid.DevicePath)) continue;

                try
                {
                    if (!c.Hid.TryOpen(out HidStream stream)) continue;

                    var def = new RedragonClientDefinition(
                        c.Hid.VendorID, c.Hid.ProductID, c.Model,
                        c.Manufacturer,
                        string.IsNullOrEmpty(c.Product) ? c.Model.DisplayName(c.Hid.ProductID) : c.Product,
                        c.Hid.DevicePath);

                    var trigger = (RedragonUpdateTrigger)GetUpdateTrigger();
                    var queue = new RedragonUpdateQueue(trigger, def, stream);
                    var info = new RedragonDeviceInfo(def);
                    var dev = new RedragonDevice(info, queue, def);

                    _openStreams[dev] = stream;
                    _devicePaths[dev] = c.Hid.DevicePath;
                    AddDevice(dev);

                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[Redragon] Hot-plug adopted {def.Product} ({def.VendorId:X4}:{def.ProductId:X4}).",
                        forwardToSentry: false);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error,
                        $"[Redragon] Hot-plug add failed for {c.Model.DisplayName(c.Hid.ProductID)}: {ex.Message}",
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

                foreach (var dev in Devices.OfType<RedragonDevice>())
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
