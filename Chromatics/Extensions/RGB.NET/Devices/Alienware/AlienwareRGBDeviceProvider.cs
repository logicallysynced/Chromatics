using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware
{
    // Custom RGB.NET device provider for Alienware AlienFX hardware.
    // Pure managed implementation via HidSharp — no LightFX_SDK.dll, no
    // Dell AWCC dependency, no precompiled bridge. Three HID dialects
    // are dispatched from one provider:
    //
    //   - V4: 5-zone chassis (Aurora R7-R14 desktops, m15 zone laptops,
    //     m17R1, Dell G7/G5). VID 0x187C.
    //   - V5: per-key notebook keyboards (Area51m-R2, x17R2, m15R3+,
    //     m17R3). VID 0x0D62.
    //   - V8: per-key external keyboards (AW510K, AW920K, AW768, AW410K).
    //     VID 0x04F2.
    //
    // Auto-adopt on first enable mirrors the QMK pattern. The Mapping
    // tab handles per-device disable for users who don't want every
    // discovered Alienware device controlled.
    public class AlienwareRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Singleton

        private static AlienwareRGBDeviceProvider _instance;
        public static AlienwareRGBDeviceProvider Instance => _instance ?? new AlienwareRGBDeviceProvider();

        public AlienwareRGBDeviceProvider()
        {
            if (_instance != null) Throw(new Exception($"There can be only one instance of type {nameof(AlienwareRGBDeviceProvider)}"));
            _instance = this;
        }

        #endregion

        // Adopted devices the user opted to control. Hydrated by the
        // Settings layer / RGBController.Setup before LoadDeviceProvider
        // runs. Empty list short-circuits LoadDevices.
        public List<AlienwareClientDefinition> ClientDefinitions { get; } = new();

        // Track open HID streams so Dispose can shut them down cleanly.
        private readonly Dictionary<IRGBDevice, HidStream> _openStreams = new();

        // 30Hz cap. AlienFX hardware accepts sustained 30Hz reliably across
        // V4/V5/V8 in T-Troll's testing; faster than this risks the
        // firmware's internal queue dropping commands silently.
        private const double UpdateFrequencySeconds = 1.0 / 30.0;

        protected override void InitializeSDK()
        {
            // No SDK init — pure managed HID.
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
            => new AlienwareUpdateTrigger(UpdateFrequencySeconds);

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            var devices = new List<IRGBDevice>();
            if (ClientDefinitions.Count == 0) return devices;

            foreach (var def in ClientDefinitions)
            {
                try
                {
                    HidDevice hid = ResolveHidDevice(def);
                    if (hid == null)
                    {
                        Logger.WriteConsole(LoggerTypes.Devices,
                            $"[Alienware] {def.Product}: not present on this PC right now — skipping. Will retry on next enable.",
                            forwardToSentry: false);
                        continue;
                    }

                    if (!hid.TryOpen(out HidStream stream))
                    {
                        // Most common reason a TryOpen fails on AlienFX
                        // hardware: Alienware Command Center is running and
                        // holds the HID interface exclusively. Detect that
                        // and give the user a specific, actionable hint
                        // rather than the generic "another app may be
                        // holding it" message.
                        var awccProcess = DetectAwccConflict();
                        if (awccProcess != null)
                        {
                            Logger.WriteConsole(LoggerTypes.Error,
                                $"[Alienware] Could not open {def.Product} ({def.VendorId:X4}:{def.ProductId:X4}). Alienware Command Center ({awccProcess}) is currently running and holds the AlienFX HID interface exclusively. Quit AWCC from the system tray (right-click the AWCC icon → Exit), then re-enable the Alienware provider in Settings.",
                                forwardToSentry: false);
                        }
                        else
                        {
                            Logger.WriteConsole(LoggerTypes.Error,
                                $"[Alienware] Could not open {def.Product} ({def.VendorId:X4}:{def.ProductId:X4}). Another app may be holding the AlienFX HID interface exclusively. Common culprits: Alienware Command Center (AWCC), AlienFX Tools (T-Troll), and the older AlienFX Editor. Close any of these and try again.",
                                forwardToSentry: false);
                        }
                        continue;
                    }

                    var trigger = (AlienwareUpdateTrigger)GetUpdateTrigger();
                    var queue = new AlienwareUpdateQueue(trigger, def, stream);
                    var info = new AlienwareDeviceInfo(def);
                    var dev = new AlienwareDevice(info, queue, def);

                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[Alienware] Adopted {def.Product} ({def.ApiVersion}, {def.LightCount} addressable lights).",
                        forwardToSentry: false);

                    _openStreams[dev] = stream;
                    devices.Add(dev);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error,
                        $"[Alienware] failed to set up {def.Product}: {ex.Message}");
                }
            }

            return devices;
        }

        // Returns the friendly process name of a running Alienware Command
        // Center component if one is detected, or null if none are running.
        // AWCC ships as a multi-process suite — the main UI is `AWCC.exe`,
        // the background lighting service is `AlienFXService.exe` /
        // `LightingService.exe` depending on AWCC version, and the
        // legacy editor is `AlienFXEditor.exe`. Any of these holding the
        // HID interface is enough to lock us out.
        private static readonly string[] _awccProcessNames =
        {
            "AWCC",
            "AlienFXService",
            "LightingService",
            "AlienFXEditor",
            "AlienFusionUpdate",
            "AlienwareCommandCenter",
        };

        private static string DetectAwccConflict()
        {
            try
            {
                foreach (var name in _awccProcessNames)
                {
                    var procs = System.Diagnostics.Process.GetProcessesByName(name);
                    if (procs.Length == 0) continue;
                    foreach (var p in procs) try { p.Dispose(); } catch { /* ignore */ }
                    return $"{name}.exe";
                }
            }
            catch { /* process enumeration is best-effort */ }
            return null;
        }

        // Find the HidDevice on the bus that matches a client definition.
        // Match by VID+PID first; tie-break on DevicePath when the user has
        // multiple of the same model (rare for AlienFX hardware but
        // possible — e.g. two AW510Ks).
        private static HidDevice ResolveHidDevice(AlienwareClientDefinition def)
        {
            try
            {
                HidDevice fallback = null;
                foreach (var hid in DeviceList.Local.GetHidDevices())
                {
                    int vid = 0; int pid = 0;
                    try { vid = hid.VendorID; pid = hid.ProductID; } catch { continue; }
                    if (vid != def.VendorId || pid != def.ProductId) continue;

                    if (!string.IsNullOrEmpty(def.DevicePath)
                        && string.Equals(hid.DevicePath, def.DevicePath, StringComparison.OrdinalIgnoreCase))
                        return hid;

                    fallback ??= hid;
                }
                return fallback;
            }
            catch { return null; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    foreach (var dev in Devices.OfType<AlienwareDevice>())
                    {
                        try { dev.BeginShutdown(); } catch { /* ignore */ }
                    }
                    foreach (var s in _openStreams.Values)
                    {
                        try { s.Dispose(); } catch { /* ignore */ }
                    }
                    _openStreams.Clear();
                    ClientDefinitions.Clear();
                }
                catch { /* swallow during teardown */ }
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
