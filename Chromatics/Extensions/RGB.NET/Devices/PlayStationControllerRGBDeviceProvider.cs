using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Devices.PlayStation;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices
{
    // Custom RGB.NET device provider for Sony's PlayStation controllers — DualShock 4
    // (PS4) and DualSense / DualSense Edge (PS5). Talks raw HID via HidSharp; no
    // third-party drivers (no DS4Windows, no SignalRGB, no HidHide). Both USB and
    // Bluetooth transports are supported.
    //
    // Lighting only — input reports continue to flow through Windows' HID stack to
    // games normally. Sony's HID gamepads accept *shared* output writes by default
    // on Windows (FILE_SHARE_READ | FILE_SHARE_WRITE), so coexisting with Steam or
    // a game's native lighting integration is the expected case. Last-writer-wins
    // per output report period; at our 30Hz cadence we comfortably override most
    // intermittent setters (Steam profile changes, game state events).
    //
    // Hot-plug: HidSharp.DeviceList.Local.Changed fires on Windows PnP events
    // (USB connect/disconnect, BT pair/unpair). We debounce briefly and then
    // reconcile our open set against the current HID enumeration — new
    // controllers get opened + AddDevice'd (which raises DevicesChanged so
    // RGBController attaches brightness corrections), removed ones are
    // disposed and RemoveDevice'd.
    //
    // Known collisions, surfaced in the log:
    //   - DS4Windows / reWASD with "Exclusive Mode" enabled — they hold the HID
    //     handle exclusive, our open throws UnauthorizedAccessException / IOException.
    //   - HidHide hiding the controller from non-allow-listed apps — the device
    //     never appears in HidSharp enumeration. Indistinguishable from "controller
    //     not connected" so we just emit a hint when zero controllers were found
    //     after the user enabled the provider.
    public class PlayStationControllerRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        // Sony Interactive Entertainment's USB vendor id.
        private const int SonyVendorId = 0x054C;

        // PlayStation HID product ids relevant for lighting.
        // DualShock 4 v1: 0x05C4 (original "JDM-001/011").
        // DualShock 4 v2: 0x09CC (revised "JDM-040/050/055" with lightbar visible
        //                          through touchpad).
        // DualSense:      0x0CE6 (PS5 launch model "CFI-ZCT1").
        // DualSense Edge: 0x0DF2 (PS5 pro variant "CFI-ZCP1").
        // The "Wireless Adapter" 0x0BA0 is the BT bridge for DS4 — also has the
        // Sony VID and reports as a DualShock 4. Treat it like DS4 v2 (later
        // firmware, supports same lighting protocol).
        private const int Pid_DualShock4_V1 = 0x05C4;
        private const int Pid_DualShock4_V2 = 0x09CC;
        private const int Pid_DualShock4_Wireless = 0x0BA0;
        private const int Pid_DualSense = 0x0CE6;
        private const int Pid_DualSenseEdge = 0x0DF2;

        // 30Hz update rate. Faster than Steam's intermittent profile-change writes,
        // slower than USB full-speed bandwidth (we'd run fine at 250Hz but it's
        // wasted writes — perceptual change isn't there). Matches what OpenRGB's
        // DualSense plugin uses.
        private const double UpdateFrequencySeconds = 1.0 / 30.0;

        // PnP can fire several Changed events for one logical connect (driver
        // initialisation, child interface enumeration, etc.). Coalesce them.
        private const int HotplugDebounceMs = 500;

        private static PlayStationControllerRGBDeviceProvider _instance;
        public static PlayStationControllerRGBDeviceProvider Instance =>
            _instance ?? new PlayStationControllerRGBDeviceProvider();

        // Per-device state needed for lifecycle: the open HidStream (for dispose
        // on remove) and the HidDevice's DevicePath (for identity comparison
        // during reconcile, since serial isn't always available, especially on
        // BT-paired controllers). Both keyed by IRGBDevice so RemoveDevice can
        // find them when given the device instance.
        private readonly Dictionary<IRGBDevice, HidStream> _openStreams = new();
        private readonly Dictionary<IRGBDevice, string> _devicePaths = new();
        private readonly System.Threading.Lock _stateLock = new();

        // Hot-plug bookkeeping: subscription flag (so re-init doesn't double-subscribe),
        // and a serial counter so debounced reconciles on stale enqueues short-circuit.
        private bool _hotplugSubscribed;
        private int _hotplugScheduleSeq;

        public PlayStationControllerRGBDeviceProvider()
        {
            if (_instance != null)
                Throw(new Exception($"There can be only one instance of {nameof(PlayStationControllerRGBDeviceProvider)}"), true);
            _instance = this;
        }

        protected override void InitializeSDK()
        {
            // Subscribe once for the lifetime of this provider instance. The
            // subscription is unhooked in Dispose. Guard against double-subscribe
            // in case Initialize is invoked twice (which AbstractRGBDeviceProvider
            // tolerates).
            if (!_hotplugSubscribed)
            {
                DeviceList.Local.Changed += OnHidDeviceListChanged;
                _hotplugSubscribed = true;
            }
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
        {
            return new DeviceUpdateTrigger(UpdateFrequencySeconds);
        }

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            var devices = new List<IRGBDevice>();

            HidDevice[] all;
            try
            {
                all = DeviceList.Local.GetHidDevices(vendorID: SonyVendorId).ToArray();
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"[PlayStation] HID enumeration failed: {ex.Message}", forwardToSentry: false);
                return devices;
            }

            int candidateCount = 0;
            foreach (var hid in all)
            {
                int pid = hid.ProductID;
                if (!IsSupportedPid(pid)) continue;

                candidateCount++;
                if (TryOpenAndCreateDevice(hid, pid, out var device))
                    devices.Add(device);
            }

            if (candidateCount == 0)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Devices,
                    "[PlayStation] No PlayStation controllers detected. " +
                    "Connect a DualShock 4 or DualSense over USB or Bluetooth — Chromatics will pick it up automatically. " +
                    "If one is already connected, ensure it isn't hidden by HidHide and isn't bound to DS4Windows / reWASD in exclusive mode.");
            }
            else if (devices.Count == 0)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"[PlayStation] Found {candidateCount} controller(s) but could not open any for lighting. " +
                    "Likely cause: another application has exclusive HID access (DS4Windows / reWASD).",
                    forwardToSentry: false);
            }

            return devices;
        }

        private static bool IsSupportedPid(int pid)
            => pid == Pid_DualShock4_V1
            || pid == Pid_DualShock4_V2
            || pid == Pid_DualShock4_Wireless
            || pid == Pid_DualSense
            || pid == Pid_DualSenseEdge;

        // Centralised "open + construct + register" path used by both initial
        // enumeration and hot-plug. Handles the predictable failure modes
        // (TryOpen returns false, UnauthorizedAccessException) with friendly
        // logging and returns false silently in those cases — caller doesn't
        // need to distinguish "not openable" from "openable but build failed".
        private bool TryOpenAndCreateDevice(HidDevice hid, int pid, out IRGBDevice device)
        {
            device = null;

            HidStream opened;
            try
            {
                if (!hid.TryOpen(out opened))
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error,
                        $"[PlayStation] Could not open controller (VID 0x{hid.VendorID:X4} PID 0x{pid:X4}). " +
                        "Another application may have exclusive HID access (DS4Windows / reWASD with exclusive mode enabled).",
                        forwardToSentry: false);
                    return false;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"[PlayStation] Access denied opening controller (VID 0x{hid.VendorID:X4} PID 0x{pid:X4}). " +
                    "Another application has exclusive HID access — close DS4Windows / reWASD or disable their exclusive mode.",
                    forwardToSentry: false);
                return false;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"[PlayStation] Failed to open controller (VID 0x{hid.VendorID:X4} PID 0x{pid:X4}): {ex.Message}",
                    forwardToSentry: false);
                return false;
            }

            try
            {
                // Transport detection: DS4 USB max output report is 32 bytes (incl. report
                // ID), DS4 BT is 78. DS5 USB is 64, DS5 BT is 78. Any controller that
                // reports an output buffer of 78+ is on Bluetooth.
                int maxOut;
                try { maxOut = opened.Device.GetMaxOutputReportLength(); }
                catch { maxOut = 0; }
                var transport = maxOut >= 78 ? PlayStationTransport.Bluetooth : PlayStationTransport.Usb;

                string serial;
                try { serial = hid.GetSerialNumber() ?? ""; }
                catch { serial = ""; }

                string devicePath;
                try { devicePath = hid.DevicePath ?? ""; }
                catch { devicePath = ""; }

                var controllerType = pid switch
                {
                    Pid_DualSense => PlayStationControllerType.DualSense,
                    Pid_DualSenseEdge => PlayStationControllerType.DualSenseEdge,
                    _ => PlayStationControllerType.DualShock4,
                };

                var info = new PlayStationDeviceInfo(controllerType, transport, serial);

                IRGBDevice newDevice;
                if (controllerType == PlayStationControllerType.DualShock4)
                {
                    var queue = new DualShock4UpdateQueue(GetUpdateTrigger(), opened, transport);
                    newDevice = new DualShock4Device(info, queue);
                }
                else
                {
                    var queue = new DualSenseUpdateQueue(GetUpdateTrigger(), opened, transport);
                    newDevice = new DualSenseDevice(info, queue);
                }

                lock (_stateLock)
                {
                    _openStreams[newDevice] = opened;
                    _devicePaths[newDevice] = devicePath;
                }

                Logger.WriteConsole(Enums.LoggerTypes.Devices,
                    $"[PlayStation] Connected {info.DeviceName}{(string.IsNullOrEmpty(serial) ? "" : $" S/N {serial}")}.");

                device = newDevice;
                return true;
            }
            catch (Exception ex)
            {
                try { opened.Dispose(); } catch { }
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"[PlayStation] Failed to construct device for VID 0x{hid.VendorID:X4} PID 0x{pid:X4}: {ex.Message}",
                    forwardToSentry: false);
                return false;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Hot-plug
        // ────────────────────────────────────────────────────────────────────

        private void OnHidDeviceListChanged(object sender, DeviceListChangedEventArgs e)
        {
            // PnP can fire multiple Changed events for one logical connect/disconnect
            // (parent device + child interfaces, BT pairing dance). Schedule a
            // reconcile after a short debounce; cancel earlier scheduled ones via
            // the seq counter so only the latest tick wins.
            int mySeq = System.Threading.Interlocked.Increment(ref _hotplugScheduleSeq);
            Task.Run(async () =>
            {
                await Task.Delay(HotplugDebounceMs).ConfigureAwait(false);
                if (System.Threading.Volatile.Read(ref _hotplugScheduleSeq) != mySeq) return;
                try { Reconcile(); }
                catch (Exception ex)
                {
                    Logger.WriteVerbose($"[PlayStation] Hot-plug reconcile threw: {ex.Message}");
                }
            });
        }

        // Compare current HID enumeration to our open set; add new ones, remove
        // gone ones. Called from the debounced PnP callback. Holds _stateLock for
        // the snapshot read so we don't race with a concurrent Dispose; opens and
        // AddDevice/RemoveDevice are done outside the lock so we don't deadlock
        // against any handler that might call back into the provider.
        private void Reconcile()
        {
            HashSet<string> currentPaths;
            try
            {
                currentPaths = DeviceList.Local.GetHidDevices(vendorID: SonyVendorId)
                    .Where(h => IsSupportedPid(h.ProductID))
                    .Select(h => h.DevicePath ?? "")
                    .Where(p => p.Length > 0)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[PlayStation] Reconcile enumeration failed: {ex.Message}");
                return;
            }

            // Snapshot — list of (device, path) pairs we currently hold open.
            List<KeyValuePair<IRGBDevice, string>> snapshot;
            lock (_stateLock)
            {
                snapshot = _devicePaths.ToList();
            }

            // Removals first (devices we hold but no longer enumerate) — done
            // before adds so a controller that quickly reconnects on a different
            // path can be re-added cleanly.
            foreach (var kvp in snapshot)
            {
                if (string.IsNullOrEmpty(kvp.Value)) continue;
                if (!currentPaths.Contains(kvp.Value))
                    RemoveDevice(kvp.Key);
            }

            // Additions: any enumerated path not currently open.
            HashSet<string> openedPaths;
            lock (_stateLock)
            {
                openedPaths = _devicePaths.Values
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var hid in DeviceList.Local.GetHidDevices(vendorID: SonyVendorId))
            {
                if (!IsSupportedPid(hid.ProductID)) continue;
                string path;
                try { path = hid.DevicePath ?? ""; } catch { continue; }
                if (string.IsNullOrEmpty(path)) continue;
                if (openedPaths.Contains(path)) continue;

                if (TryOpenAndCreateDevice(hid, hid.ProductID, out var newDevice))
                {
                    // AddDevice (inherited from AbstractRGBDeviceProvider) tracks
                    // it in InternalDevices and fires DevicesChanged.Added, which
                    // RGBController catches to attach the global + per-device
                    // brightness corrections.
                    AddDevice(newDevice);
                }
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // RemoveDevice override — clean up our HidStream + cached state when
        // either we (hot-plug) or external code (provider unload) removes a
        // device. Falls through to base.RemoveDevice which fires
        // DevicesChanged.Removed.
        // ────────────────────────────────────────────────────────────────────
        protected override bool RemoveDevice(IRGBDevice device)
        {
            HidStream stream = null;
            string path = null;
            lock (_stateLock)
            {
                if (_openStreams.TryGetValue(device, out stream))
                    _openStreams.Remove(device);
                if (_devicePaths.TryGetValue(device, out path))
                    _devicePaths.Remove(device);
            }

            // Send a final off-frame so the controller doesn't sit on our last
            // colour after disconnect. Best-effort — the device may already be
            // gone (BT unpair, USB unplug) in which case the write throws.
            try { (device as DualShock4Device)?.Shutdown(); } catch { }
            try { (device as DualSenseDevice)?.Shutdown(); } catch { }

            if (stream != null)
            {
                try { stream.Dispose(); } catch { }
            }

            try
            {
                Logger.WriteConsole(Enums.LoggerTypes.Devices,
                    $"[PlayStation] Disconnected {device.DeviceInfo.DeviceName}.");
            }
            catch { }

            return base.RemoveDevice(device);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hotplugSubscribed)
                {
                    try { DeviceList.Local.Changed -= OnHidDeviceListChanged; } catch { }
                    _hotplugSubscribed = false;
                }

                // Snapshot devices to remove. RemoveDevice mutates the
                // dictionaries, so iterate a copy.
                List<IRGBDevice> snapshot;
                lock (_stateLock)
                {
                    snapshot = _openStreams.Keys.ToList();
                }
                foreach (var d in snapshot)
                {
                    try { RemoveDevice(d); } catch { }
                }
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
