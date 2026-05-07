using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Devices.PlayStation;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private static PlayStationControllerRGBDeviceProvider _instance;
        public static PlayStationControllerRGBDeviceProvider Instance =>
            _instance ?? new PlayStationControllerRGBDeviceProvider();

        // Track open streams so Dispose can flush a final off-frame and release
        // handles cleanly. Keyed by IRGBDevice so we can match a teardown back to
        // the right stream/queue.
        private readonly Dictionary<IRGBDevice, HidStream> _openStreams = new();
        private readonly List<DualShock4Device> _ds4Devices = new();
        private readonly List<DualSenseDevice> _dsDevices = new();

        public PlayStationControllerRGBDeviceProvider()
        {
            if (_instance != null)
                Throw(new Exception($"There can be only one instance of {nameof(PlayStationControllerRGBDeviceProvider)}"), true);
            _instance = this;
        }

        protected override void InitializeSDK()
        {
            // Nothing to initialise — HidSharp's DeviceList.Local is process-wide
            // and lazily populated. LoadDevices does the actual enumeration.
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
                try
                {
                    if (TryOpenDevice(hid, pid, out var device, out var stream))
                    {
                        devices.Add(device);
                        _openStreams[device] = stream;
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error,
                        $"[PlayStation] Failed to open controller (VID 0x{hid.VendorID:X4} PID 0x{pid:X4}): {ex.Message}",
                        forwardToSentry: false);
                }
            }

            if (candidateCount == 0)
            {
                // No matching devices. Most common reasons: nothing connected,
                // or HidHide hiding the controllers from non-allow-listed apps.
                Logger.WriteConsole(Enums.LoggerTypes.Devices,
                    "[PlayStation] No PlayStation controllers detected. " +
                    "If one is connected, ensure it isn't hidden by HidHide and isn't bound to DS4Windows / reWASD in exclusive mode.");
            }
            else if (devices.Count == 0)
            {
                // At least one matching HID device existed but every open
                // attempt failed. Per-device exception was already logged
                // above; this summary makes the "exclusive-mode" cause
                // discoverable in the console without scanning earlier lines.
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

        private bool TryOpenDevice(HidDevice hid, int pid, out IRGBDevice device, out HidStream stream)
        {
            device = null;
            stream = null;

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

            var controllerType = pid switch
            {
                Pid_DualSense => PlayStationControllerType.DualSense,
                Pid_DualSenseEdge => PlayStationControllerType.DualSenseEdge,
                _ => PlayStationControllerType.DualShock4,
            };

            var info = new PlayStationDeviceInfo(controllerType, transport, serial);

            try
            {
                if (controllerType == PlayStationControllerType.DualShock4)
                {
                    var queue = new DualShock4UpdateQueue(GetUpdateTrigger(), opened, transport);
                    var ds4 = new DualShock4Device(info, queue);
                    _ds4Devices.Add(ds4);
                    device = ds4;
                }
                else
                {
                    var queue = new DualSenseUpdateQueue(GetUpdateTrigger(), opened, transport);
                    var ds = new DualSenseDevice(info, queue);
                    _dsDevices.Add(ds);
                    device = ds;
                }

                stream = opened;
                Logger.WriteConsole(Enums.LoggerTypes.Devices,
                    $"[PlayStation] Connected {info.DeviceName}{(string.IsNullOrEmpty(serial) ? "" : $" S/N {serial}")}.");
                return true;
            }
            catch
            {
                try { opened.Dispose(); } catch { }
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Send a final off-frame and close streams so the controller doesn't
                // sit on our last-painted colour after the provider is unloaded.
                // The firmware restores its own indicator (battery/charge state on
                // DS5; player number on DS4) shortly after we stop writing, but
                // black-out makes the transition crisp instead of a stale flash.
                foreach (var d in _ds4Devices)
                {
                    try { d.Shutdown(); } catch { }
                }
                foreach (var d in _dsDevices)
                {
                    try { d.Shutdown(); } catch { }
                }

                foreach (var stream in _openStreams.Values)
                {
                    try { stream.Dispose(); } catch { }
                }
                _openStreams.Clear();
                _ds4Devices.Clear();
                _dsDevices.Clear();
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
