using Chromatics.Core;
using Chromatics.Enums;
using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    // Custom RGB.NET device provider for Windows Dynamic Lighting.
    //
    // Talks to the Windows.Devices.Lights.LampArray WinRT API. Devices
    // that expose the standard HID Lighting and Illumination usage page
    // (0x59, HUTRR84) are picked up by Windows automatically; any
    // device Windows exposes via LampArray.GetDeviceSelector() is a
    // candidate for adoption here.
    //
    // Discovery uses Windows' DeviceWatcher so hot-plug works without
    // any per-frame polling on our side. The watcher fires Added /
    // Removed events on a thread-pool worker; we resolve each Added
    // event into a LampArray instance via FromIdAsync and surface
    // them to RGB.NET through AddDevice / RemoveDevice.
    public class DynamicLightingRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Singleton

        private static DynamicLightingRGBDeviceProvider _instance;
        public static DynamicLightingRGBDeviceProvider Instance => _instance ?? new DynamicLightingRGBDeviceProvider();

        public DynamicLightingRGBDeviceProvider()
        {
            if (_instance != null) Throw(new Exception($"There can be only one instance of type {nameof(DynamicLightingRGBDeviceProvider)}"));
            _instance = this;
        }

        #endregion

        // 30Hz update cap. LampArray devices report MinUpdateInterval
        // ranging from 4ms to 33ms across the shipping device set;
        // 30Hz sits comfortably inside that envelope and matches the
        // cadence the QMK / Yeelight / Alienware providers use.
        private const double UpdateFrequencySeconds = 1.0 / 30.0;

        private DeviceWatcher _watcher;
        private readonly ConcurrentDictionary<string, DynamicLightingDevice> _devicesById = new(StringComparer.OrdinalIgnoreCase);

        // Count of devices currently adopted by this provider. Polled by
        // SettingsViewModel's toggle handler after LoadDeviceProvider so
        // it can flip the toggle back off (and surface a dialog) when
        // Windows enumerated zero compatible devices.
        public int AdoptedDeviceCount => _devicesById.Count;

        protected override void InitializeSDK()
        {
            // No SDK init required; LampArray is part of the Windows
            // platform. We just need to start the device watcher
            // (which happens in LoadDevices below).
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
            => new DynamicLightingUpdateTrigger(UpdateFrequencySeconds);

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            // Synchronously enumerate the initial device set. The
            // DeviceWatcher gives us hot-plug for the rest of the
            // session lifetime.
            var initialDevices = LoadInitialDevicesAsync().GetAwaiter().GetResult();
            StartWatcher();
            return initialDevices;
        }

        private async Task<IEnumerable<IRGBDevice>> LoadInitialDevicesAsync()
        {
            var devices = new List<IRGBDevice>();
            DeviceInformationCollection found;
            try
            {
                string selector = LampArray.GetDeviceSelector();
                found = await DeviceInformation.FindAllAsync(selector).AsTask().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"[DynamicLighting] Initial enumeration failed: {ex.Message}");
                return devices;
            }

            foreach (var info in found)
            {
                var dev = await TryAdoptAsync(info).ConfigureAwait(false);
                if (dev != null) devices.Add(dev);
            }

            if (devices.Count == 0)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    "[DynamicLighting] No Dynamic Lighting devices detected. Compatible hardware (Razer, Logitech G LIGHTSYNC, ASUS ROG, HyperX, MSI, SteelSeries, HP/Omen) shows up here when its firmware enables the Dynamic Lighting HID profile and Settings -> Personalization -> Dynamic Lighting is turned on in Windows.",
                    forwardToSentry: false);
            }
            return devices;
        }

        // Build the RGB.NET device wrapper for one OS-supplied
        // DeviceInformation. Returns null on any failure (LampArray
        // construction failed, device disappeared between enumeration
        // and adoption, etc.) so the caller can keep going.
        private async Task<DynamicLightingDevice> TryAdoptAsync(DeviceInformation info)
        {
            try
            {
                var lampArray = await LampArray.FromIdAsync(info.Id).AsTask().ConfigureAwait(false);
                if (lampArray == null || lampArray.LampCount <= 0) return null;

                // Conflict check: when the user has opted into the
                // conservative conflict-handling behaviour (Settings ->
                // Advanced -> "Block Dynamic Lighting on devices already
                // covered by a vendor provider"), skip adoption of any
                // device whose OEM has a Chromatics vendor provider
                // currently enabled. The vendor SDK retains exclusive
                // control of the device.
                //
                // Default is the bypass path (adopt every device Windows
                // exposes regardless of overlap), since most users
                // running Dynamic Lighting want it to work on every
                // supported device. The opt-in conservative path is
                // for users who see flickering from both providers
                // writing to the same hardware.
                try
                {
                    var settings = AppSettings.GetSettings();
                    if (!settings.dynamicLightingBypassConflictCheck)
                    {
                        string overlapVendor = DynamicLightingVendorOverlap.TryGetEnabledVendorOwner(
                            lampArray.HardwareVendorId, settings);
                        if (overlapVendor != null)
                        {
                            Logger.WriteConsole(LoggerTypes.Devices,
                                $"[DynamicLighting] Skipped '{info.Name}' (VID 0x{lampArray.HardwareVendorId:X4}); the {overlapVendor} provider is enabled and owns this device. Settings -> Advanced controls this behaviour.",
                                forwardToSentry: false);
                            return null;
                        }
                    }
                }
                catch { /* conflict check is best-effort; never block adoption on a check failure */ }

                var def = new DynamicLightingClientDefinition(info.Id, info.Name, lampArray);
                var trigger = (DynamicLightingUpdateTrigger)GetUpdateTrigger();
                var queue = new DynamicLightingUpdateQueue(trigger, def);
                var rgbInfo = new DynamicLightingDeviceInfo(def);
                var dev = new DynamicLightingDevice(rgbInfo, queue, def);
                _devicesById[info.Id] = dev;

                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[DynamicLighting] Adopted '{info.Name}' ({lampArray.LampArrayKind}, {lampArray.LampCount} LEDs).",
                    forwardToSentry: false);
                return dev;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error,
                    $"[DynamicLighting] Failed to adopt '{info?.Name ?? info?.Id ?? "unknown"}': {ex.Message}",
                    forwardToSentry: false);
                return null;
            }
        }

        // ── DeviceWatcher hot-plug ────────────────────────────────────

        private void StartWatcher()
        {
            if (_watcher != null) return;
            try
            {
                _watcher = DeviceInformation.CreateWatcher(LampArray.GetDeviceSelector());
                _watcher.Added += OnDeviceAdded;
                _watcher.Removed += OnDeviceRemoved;
                _watcher.Start();
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"[DynamicLighting] DeviceWatcher start failed: {ex.Message}");
            }
        }

        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation info)
        {
            if (info == null || string.IsNullOrEmpty(info.Id)) return;
            if (_devicesById.ContainsKey(info.Id)) return; // already adopted at startup

            var dev = await TryAdoptAsync(info).ConfigureAwait(false);
            if (dev != null)
            {
                try { AddDevice(dev); }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error, $"[DynamicLighting] AddDevice failed for '{info.Name}': {ex.Message}");
                }
            }
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            if (update == null || string.IsNullOrEmpty(update.Id)) return;
            if (!_devicesById.TryRemove(update.Id, out var dev)) return;
            try
            {
                dev.BeginShutdown();
                RemoveDevice(dev);
                Logger.WriteConsole(LoggerTypes.Devices, $"[DynamicLighting] Removed '{dev.DeviceInfo.DeviceName}'.", forwardToSentry: false);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"[DynamicLighting] RemoveDevice failed: {ex.Message}");
            }
        }

        // ── Dispose ───────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_watcher != null)
                {
                    try { _watcher.Stop(); } catch { /* ignore */ }
                    try
                    {
                        _watcher.Added -= OnDeviceAdded;
                        _watcher.Removed -= OnDeviceRemoved;
                    }
                    catch { /* ignore */ }
                    _watcher = null;
                }

                foreach (var dev in _devicesById.Values)
                {
                    try { dev.BeginShutdown(); } catch { /* ignore */ }
                }
                _devicesById.Clear();
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
