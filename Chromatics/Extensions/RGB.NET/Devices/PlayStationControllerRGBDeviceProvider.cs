using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Devices.PlayStation;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        // initialisation, child interface enumeration, etc.). Coalesce them
        // AND wait long enough that Windows has finished setting up the HID
        // device — TryOpen can succeed against a partially-enumerated device
        // and the first write will then fail with "A device which does not
        // exist was specified". 1500ms is generous but the user only sees a
        // 1.5s lag once on connect, which is fine for a controller.
        private const int HotplugDebounceMs = 1500;

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
        // Tracks devices that Reconcile has already confirmed as physically
        // disconnected. RemoveDevice consults this to decide whether the
        // graceful "send a final all-black frame" attempt is worth making —
        // for a device that's already gone the write throws IOException
        // "The device is not connected", which is harmless but produces a
        // noisy first-chance break under the debugger.
        private readonly HashSet<IRGBDevice> _confirmedDisconnected = new();
        // Snapshot of currently-alive Sony controller DevicePaths, refreshed
        // synchronously by SuspendDeadDevices on every DeviceList.Changed
        // and at the end of LoadDevices / Reconcile. UpdateQueues consult
        // it via IsDevicePathAlive before each HidStream.Write — this
        // closes the race between PnP unplug and the next 30Hz trigger
        // tick. Without a pre-check, even when our PnP handler runs
        // promptly, a tick already in flight can still call Write against
        // a now-invalid handle and throw IOException.
        private static volatile HashSet<string> _alivePathsSnapshot = new(StringComparer.OrdinalIgnoreCase);
        // Set true inside Dispose so RemoveDevice can also skip the off-frame
        // at app shutdown — the OS may already have invalidated the HID
        // handle even though the controller is physically connected, and
        // the firmware resets to its default indicator on process exit
        // regardless of whether we send black first.
        private volatile bool _disposing;
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

            // Seed the alive-path snapshot so UpdateQueues' per-frame
            // pre-check answers correctly from the very first trigger tick.
            // Without this seed the snapshot starts empty and every queue
            // would short-circuit until the first PnP event repopulates it.
            try { SuspendDeadDevices(); } catch { }

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
        // (TryOpen returns false, UnauthorizedAccessException, the broader
        // DeviceIOException family that HidSharp throws when the kernel
        // rejects the descriptor-query handle) with friendly logging and
        // returns false silently — caller doesn't need to distinguish
        // "not openable" from "openable but build failed".
        //
        // Only call HidDevice methods that are absolutely necessary, and only
        // call them in this order:
        //   1. DevicePath (cheap property, no descriptor query)
        //   2. TryOpen   (this also primes the ReportInfo cache as a side
        //                 effect, see WinHidDevice.OpenDeviceDirectly)
        //   3. GetMaxOutputReportLength on the open stream (free — ReportInfo
        //      is now cached, no second descriptor query needed)
        //
        // We deliberately do NOT call GetSerialNumber. HidSharp's
        // RequiresGetInfo opens a *separate* read-info handle via
        // TryOpenToGetInfo(_path, ...) to satisfy any flag not already
        // cached — and on some hardware (DS4 v1 in particular, also any
        // controller whose descriptor query can't get a handle because
        // Steam / driver / power state is holding the device) this throws
        // DeviceIOException("Failed to get info."). Even a try/catch around
        // the call surfaces the throw as a first-chance exception in the
        // debugger, which is alarming for users.
        //
        // Identity always comes from a stable hash of DevicePath (Windows
        // instance ID — stable across restarts for the same physical
        // controller in the same USB port / BT pairing), so we don't need
        // the real serial for mapping persistence anyway.
        private bool TryOpenAndCreateDevice(HidDevice hid, int pid, out IRGBDevice device)
        {
            device = null;

            string devicePath;
            try { devicePath = hid.DevicePath ?? ""; }
            catch { devicePath = ""; }

            string serial = string.IsNullOrEmpty(devicePath) ? "" : ShortHashOf(devicePath);

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
                // HidSharp.Exceptions.DeviceIOException ("Failed to get info.")
                // lands here when the kernel refuses the descriptor-query
                // handle. Surface it as a connection failure rather than a
                // crash; user can retry by replugging or closing the
                // conflicting tool.
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"[PlayStation] Failed to open controller (VID 0x{hid.VendorID:X4} PID 0x{pid:X4}): {ex.Message} " +
                    "If this persists, another tool (Steam Input, DS4Windows, reWASD, HidHide) may be blocking access. " +
                    "Try closing it and replugging the controller.",
                    forwardToSentry: false);
                return false;
            }

            try
            {
                // Transport detection: DS4 USB max output report is 32 bytes (incl. report
                // ID), DS4 BT is 78. DS5 USB is 64, DS5 BT is 78. Any controller that
                // reports an output buffer of 78+ is on Bluetooth. ReportInfo was
                // cached by TryOpen above, so this call is free and won't throw.
                int maxOut;
                try { maxOut = opened.Device.GetMaxOutputReportLength(); }
                catch { maxOut = 0; /* default to USB byte count */ }
                var transport = maxOut >= 78 ? PlayStationTransport.Bluetooth : PlayStationTransport.Usb;

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
                    var queue = new DualShock4UpdateQueue(GetUpdateTrigger(), opened, transport, devicePath);
                    newDevice = new DualShock4Device(info, queue);
                }
                else
                {
                    var queue = new DualSenseUpdateQueue(GetUpdateTrigger(), opened, transport, devicePath);
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
            // Two-pass design.
            //
            // Pass 1 (immediate, no debounce): walk our open set against the
            // current HID enumeration and call SuspendWrites() on any device
            // that has disappeared. This sets the queue's _disposed flag
            // BEFORE the next 30Hz trigger tick fires, so the trigger's
            // OnUpdate->Update never reaches HidStream.Write — no IOException
            // is thrown at all (not even one caught first-chance break in
            // the debugger). The device stays attached to the surface until
            // pass 2 cleans it up; suspended writes just no-op until then.
            //
            // Pass 2 (debounced 1500ms): full Reconcile that handles
            //   - the slow-side cleanup (RemoveDevice + stream dispose +
            //     surface.Detach via the bookkeeping handler)
            //   - new-device opens (which need the debounce to let Windows
            //     finish enumerating — TryOpen on a partially-enumerated
            //     device succeeds but the first Write fails)
            // The seq counter cancels stale debounces so only the latest
            // PnP burst's Reconcile actually runs.
            try { SuspendDeadDevices(); }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[PlayStation] Suspend-dead-devices pass threw: {ex.Message}");
            }

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

        // Public per-frame pre-check used by UpdateQueues. Queries HidSharp's
        // device list LIVE (rather than a snapshot replaced asynchronously by
        // SuspendDeadDevices) — HidSharp invalidates its internal device-keys
        // cache synchronously on WM_DEVICECHANGE inside DeviceMonitorWindowProc
        // on the message-pump thread, BEFORE pulsing its notify thread that
        // eventually fires DeviceList.Changed. So a live GetHidDevices() call
        // sees the unplug ahead of any DeviceList.Changed subscriber, which
        // is exactly the race that was leaving our snapshot stale through
        // the first post-unplug 30Hz tick.
        //
        // Cost: a single SetupDi enumeration filtered to the Sony VID,
        // gated on HidSharp's per-PnP-event cache. Most ticks hit the cache
        // (sub-microsecond hashtable lookup); only the tick immediately
        // after a PnP event re-enumerates (~1ms). Total CPU at 30Hz under
        // normal conditions is negligible.
        public static bool IsDevicePathAlive(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) return false;
            try
            {
                foreach (var hid in DeviceList.Local.GetHidDevices(vendorID: SonyVendorId))
                {
                    if (string.Equals(hid.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
            catch
            {
                // Fail closed — if enumeration itself throws, skip the write
                // rather than fall through to HidStream.Write where the
                // failure mode is exactly the IOException we're trying to
                // avoid.
                return false;
            }
        }

        // Immediate-pass companion to Reconcile. Compares our currently-tracked
        // device paths to the live HID enumeration; for anything we still hold
        // open that no longer enumerates, suspend writes on its queue AND
        // refresh the alive-path snapshot UpdateQueues consult per frame.
        // Cheap (one HID enumeration + one set diff, no opens, no allocations
        // beyond the path set itself) and runs on whatever thread
        // DeviceList.Changed is raised from — keep it short.
        private void SuspendDeadDevices()
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
            catch
            {
                return;
            }

            // Publish the new snapshot atomically. UpdateQueues see the change
            // on the next trigger tick (volatile reference write).
            _alivePathsSnapshot = currentPaths;

            List<KeyValuePair<IRGBDevice, string>> snapshot;
            lock (_stateLock)
            {
                snapshot = _devicePaths.ToList();
            }

            foreach (var kvp in snapshot)
            {
                if (string.IsNullOrEmpty(kvp.Value)) continue;
                if (currentPaths.Contains(kvp.Value)) continue;

                // Mark as confirmed gone so when the debounced Reconcile
                // gets here it skips the off-frame write in RemoveDevice
                // (the device's queue is already suspended; the write
                // would have nowhere to land).
                lock (_stateLock) { _confirmedDisconnected.Add(kvp.Key); }

                switch (kvp.Key)
                {
                    case DualShock4Device ds4: ds4.SuspendWrites(); break;
                    case DualSenseDevice ds: ds.SuspendWrites(); break;
                }
            }
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
            // path can be re-added cleanly. Mark each device confirmed-gone
            // before calling RemoveDevice so the override skips the doomed
            // off-frame write to the stream.
            foreach (var kvp in snapshot)
            {
                if (string.IsNullOrEmpty(kvp.Value)) continue;
                if (!currentPaths.Contains(kvp.Value))
                {
                    lock (_stateLock) { _confirmedDisconnected.Add(kvp.Key); }
                    RemoveDevice(kvp.Key);
                }
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
                    // Refresh the alive-path snapshot BEFORE AddDevice so the
                    // first trigger tick after AddDevice already sees the new
                    // device's path. SuspendDeadDevices does the refresh as
                    // part of its work; the "suspend" half is a no-op here
                    // since the device we just opened is enumerated.
                    try { SuspendDeadDevices(); } catch { }

                    // AddDevice (inherited from AbstractRGBDeviceProvider) tracks
                    // it in InternalDevices and fires DevicesChanged.Added, which
                    // RGBController catches to attach the global + per-device
                    // brightness corrections + surface.Attach (hot-plug branch).
                    AddDevice(newDevice);

                    // Make sure our DeviceUpdateTrigger is actually running.
                    // AbstractRGBDeviceProvider.Initialize() calls Start() on
                    // every trigger in UpdateTriggerMapping at the end of
                    // initial load — but if no controllers were connected at
                    // launch, our trigger wasn't created until just now (via
                    // GetUpdateTrigger inside TryOpenAndCreateDevice). The
                    // initial Start() pass already ran, so without this
                    // explicit call the trigger sits idle and the queue's
                    // Update() is never invoked. Start() is idempotent
                    // (if (IsRunning) return;), safe to call repeatedly.
                    try { GetUpdateTrigger().Start(); } catch { }
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
            bool wasConfirmedGone;
            lock (_stateLock)
            {
                if (_openStreams.TryGetValue(device, out stream))
                    _openStreams.Remove(device);
                if (_devicePaths.TryGetValue(device, out path))
                    _devicePaths.Remove(device);
                wasConfirmedGone = _confirmedDisconnected.Remove(device);
            }

            // Send a final off-frame ONLY when removal is voluntary (user
            // toggled the provider off in Settings). Skip it when:
            //   - Reconcile confirmed the device is physically gone, or
            //   - We're inside Dispose (app close / provider teardown).
            // In both skip cases the write would throw IOException — the
            // catch handles it but the debugger breaks on first chance,
            // which is what the user actually sees.
            bool sendOffFrame = !wasConfirmedGone && !_disposing;
            try { (device as DualShock4Device)?.Shutdown(sendOffFrame); } catch { }
            try { (device as DualSenseDevice)?.Shutdown(sendOffFrame); } catch { }

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
                _disposing = true;

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

        // 12-char hex hash of an arbitrary string. Used to derive a stable
        // pseudo-serial from DevicePath when the controller's HID descriptor
        // doesn't expose a real serial — short enough to look reasonable in
        // the device name, long enough that two distinct USB instances of the
        // same product won't collide. Identity is the only requirement; we're
        // not relying on cryptographic strength.
        private static string ShortHashOf(string input)
        {
            byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(12);
            for (int i = 0; i < 6; i++) sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }
    }
}
