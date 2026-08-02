using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    public class NanoleafRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Singleton

        private static NanoleafRGBDeviceProvider _instance;
        public static NanoleafRGBDeviceProvider Instance => _instance ?? new NanoleafRGBDeviceProvider();

        public NanoleafRGBDeviceProvider()
        {
            if (_instance != null) Throw(new Exception($"There can be only one instance of type {nameof(NanoleafRGBDeviceProvider)}"));
            _instance = this;
        }

        #endregion

        // Update rate in Hz for every Nanoleaf device. Hydrated from the
        // hidden nanoleafUpdateRateHz setting before LoadDeviceProvider.
        public static double UpdateRateHz { get; set; } = 20.0;

        // Adopted controllers the user paired. Populated from settings before
        // LoadDeviceProvider; cleared on Dispose so a re-enable re-prompts.
        public List<NanoleafClientDefinition> ClientDefinitions { get; } = new();

        protected override void InitializeSDK()
        {
            // Open protocol, no SDK to initialise.
        }

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            return LoadDevicesAsync().GetAwaiter().GetResult();
        }

        private async Task<IEnumerable<IRGBDevice>> LoadDevicesAsync()
        {
            var devices = new List<IRGBDevice>();
            if (ClientDefinitions.Count == 0) return devices;

            bool panelOrderDirty = false;

            foreach (var def in ClientDefinitions)
            {
                if (def.Endpoint == null || string.IsNullOrEmpty(def.AuthToken))
                {
                    Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {def.Label}: not reachable or unpaired - skipping. Re-pair from Settings.");
                    continue;
                }

                try
                {
                    var rest = new NanoleafRestClient(def.Endpoint.Address.ToString(), def.Endpoint.Port, def.AuthToken);
                    var state = await rest.GetStateAsync().ConfigureAwait(false);
                    if (state == null)
                    {
                        Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] {def.Label}: unreachable at {def.Endpoint}; will retry on next enable.");
                        continue;
                    }

                    // Resolve the persisted slot table against the live
                    // layout: stored panels keep their LedId slot (even as
                    // tombstones while absent) so layer assignments survive
                    // physical changes to the wall; new panels append.
                    var livePanels = state.Panels.OrderBy(p => p.PanelId).ToList();
                    var liveIds = new HashSet<int>(livePanels.Select(p => p.PanelId));
                    bool hadStoredOrder = def.PanelOrder is { Count: > 0 };
                    var (slots, addedIds, missingIds) = NanoleafPanelSlots.Resolve(def.PanelOrder, liveIds);

                    if (hadStoredOrder && (addedIds.Count > 0 || missingIds.Count > 0))
                    {
                        Logger.WriteConsole(LoggerTypes.Devices,
                            $"[Nanoleaf] {def.Label}: panel layout changed since last session ({addedIds.Count} added, {missingIds.Count} missing). " +
                            "Existing panels keep their layer assignments; assign any new panels from the Mappings tab.");
                    }

                    if (!hadStoredOrder || addedIds.Count > 0)
                    {
                        def.PanelOrder = new List<int>(slots);
                        panelOrderDirty = true;
                    }

                    def.PanelCount = liveIds.Count;
                    if (!string.IsNullOrEmpty(state.Model)) def.Model = state.Model;
                    if (!string.IsNullOrEmpty(state.FirmwareVersion)) def.Firmware = state.FirmwareVersion;

                    var trigger = (NanoleafDeviceUpdateTrigger)GetUpdateTrigger();
                    var queue = new NanoleafUpdateQueue(trigger, def, slots, liveIds);
                    var info = new NanoleafDeviceInfo(def);
                    var dev = new NanoleafDevice(info, queue, def, livePanels, slots);

                    var deviceGuid = Chromatics.Helpers.DeviceHelper.GenerateDeviceGuid(info.DeviceName);
                    bool disabled = Chromatics.Layers.MappingLayers.IsDeviceDisabled(deviceGuid);
                    if (disabled) queue.SetPerDeviceDisabled(true);

                    await dev.CaptureAndStartAsync(turnOnIfOff: !disabled).ConfigureAwait(false);

                    devices.Add(dev);
                }
                catch (Exception ex)
                {
                    bool unreachable = Chromatics.Helpers.NetworkFailureHelper.IsUnreachable(ex);
                    Logger.WriteConsole(unreachable ? LoggerTypes.Devices : LoggerTypes.Error,
                        $"[Nanoleaf] failed to set up {def.Label}: {ex.Message}",
                        forwardToSentry: !unreachable);
                }
            }

            if (panelOrderDirty) PersistPanelOrders();

            return devices;
        }

        // Write resolved slot tables back to settings so the next launch
        // starts from the same panelId-to-LedId map. Matched by controller
        // Id; best-effort - a failed save just means re-resolving next time.
        private void PersistPanelOrders()
        {
            try
            {
                var cur = AppSettings.GetSettings();
                if (cur.deviceNanoleafAdoptedDevices == null) return;

                bool touched = false;
                foreach (var def in ClientDefinitions)
                {
                    var match = cur.deviceNanoleafAdoptedDevices.Find(a => a.Id == def.Id);
                    if (match == null) continue;
                    match.PanelOrder = new List<int>(def.PanelOrder);
                    match.PanelCount = def.PanelCount;
                    touched = true;
                }

                if (touched) AppSettings.SaveSettings(cur);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] could not persist panel layout: {ex.Message}", forwardToSentry: false);
            }
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
        {
            double hz = Math.Clamp(UpdateRateHz, 1.0, 60.0);
            return new NanoleafDeviceUpdateTrigger(1.0 / hz);
        }

        // Mirror of LIFX/Hue Dispose: gate every queue, restore each
        // controller to its pre-Chromatics state inside a bounded budget,
        // then clear the singleton so the next enable constructs fresh.
        // Restores are sequential with pacing so a multi-controller
        // household doesn't fire a burst of REST writes at once.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    var devices = Devices.OfType<NanoleafDevice>().ToList();
                    foreach (var d in devices) d.BeginShutdown();

                    if (devices.Count > 0)
                    {
                        // Per-controller budget covers retried REST writes
                        // plus the verify GET and a possible repair PUT.
                        int totalBudgetSec = Math.Min(30, 2 + devices.Count * 4);
                        Task.Run(async () =>
                        {
                            await Task.Delay(150).ConfigureAwait(false);
                            foreach (var d in devices)
                            {
                                try
                                {
                                    var restore = d.RestoreOriginalStateAsync();
                                    var done = await Task.WhenAny(restore, Task.Delay(5000)).ConfigureAwait(false);
                                    if (done != restore)
                                        Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] restore timed out for {d.DeviceInfo.DeviceName}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] restore failed for {d.DeviceInfo.DeviceName}: {ex.Message}");
                                }
                                await Task.Delay(200).ConfigureAwait(false);
                            }
                        }).Wait(TimeSpan.FromSeconds(totalBudgetSec));
                    }
                }
                catch { /* swallow during teardown */ }
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
