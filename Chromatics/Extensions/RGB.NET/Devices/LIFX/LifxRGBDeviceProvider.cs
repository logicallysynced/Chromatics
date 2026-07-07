using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX
{
    public class LifxRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Singleton

        private static LifxRGBDeviceProvider _instance;
        public static LifxRGBDeviceProvider Instance => _instance ?? new LifxRGBDeviceProvider();

        public LifxRGBDeviceProvider()
        {
            if (_instance != null) Throw(new Exception($"There can be only one instance of type {nameof(LifxRGBDeviceProvider)}"));
            _instance = this;
        }

        #endregion

        // Adopted bulbs the user has chosen to control. Populated by the
        // settings layer before LoadDeviceProvider; cleared on Dispose so the
        // next enable cycle can re-prompt with a fresh list.
        public List<LifxClientDefinition> ClientDefinitions { get; } = new();

        // Shared UDP socket used for sending colour updates. Discovery and
        // original-state capture each use their own short-lived sockets to
        // avoid colliding with the trigger thread.
        private UdpClient _udp;
        private uint _source;

        protected override void InitializeSDK()
        {
            // The protocol is open — no SDK init needed. We allocate a
            // sending socket here so it lives as long as the provider does.
            _udp = new UdpClient(0)
            {
                EnableBroadcast = true,
            };
            _source = (uint)Random.Shared.Next(2, int.MaxValue);
        }

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            return LoadDevicesAsync().GetAwaiter().GetResult();
        }

        // For each adopted device:
        //   1) ping its last-known endpoint with GetService; if no reply in
        //      ~500ms run a short discovery sweep to refresh the IP (devices
        //      change IP on DHCP renewal),
        //   2) construct the RGB.NET device,
        //   3) capture original power+colour state for restore on disable.
        private async Task<IEnumerable<IRGBDevice>> LoadDevicesAsync()
        {
            var devices = new List<IRGBDevice>();
            if (ClientDefinitions.Count == 0) return devices;

            // Refresh stale IPs in one discovery sweep up front rather than
            // per-device. Cheaper and avoids repeated broadcast traffic.
            var resolved = await ResolveEndpointsAsync(ClientDefinitions, TimeSpan.FromMilliseconds(2500)).ConfigureAwait(false);
            foreach (var def in ClientDefinitions)
            {
                if (resolved.TryGetValue(def.Mac, out var ep))
                    def.Endpoint = ep;
            }

            foreach (var def in ClientDefinitions)
            {
                if (def.Endpoint == null)
                {
                    Logger.WriteConsole(LoggerTypes.Devices, $"[LIFX] {def.Label}: not reachable on the network — skipping. Will retry on next enable.");
                    continue;
                }

                try
                {
                    // Refresh the zone count before constructing the device —
                    // discovery's GetExtendedColorZones reply may have been
                    // dropped, leaving ZoneCount=0 in stored settings. A
                    // multizone strip with ZoneCount=0 would otherwise show
                    // up as a single-LED bulb because LifxDevice's layout
                    // gates on `_def.ZoneCount > 1`.
                    ushort liveZones = await LifxUpdateQueue.ProbeZoneCountAsync(def.Endpoint, def.Mac, TimeSpan.FromMilliseconds(800)).ConfigureAwait(false);
                    if (liveZones > 0) def.ZoneCount = liveZones;

                    var trigger = (LifxDeviceUpdateTrigger)GetUpdateTrigger();
                    var queue = new LifxUpdateQueue(trigger, def, _udp, _source);
                    var info = new LifxDeviceInfo(def);
                    var dev = new LifxDevice(info, queue, def);

                    // Devices the user disabled in the Mapping tab in a
                    // previous session must not be touched at startup. Two
                    // things we'd otherwise do are wrong here:
                    //   1) CaptureOriginalStateAsync's "turn on if off"
                    //      branch would silently power the bulb back up
                    //      every launch — and the next capture cycle would
                    //      then observe Powered=true and poison _original
                    //      so subsequent disables stop turning the bulb
                    //      back off. Pass turnOnIfOff: false to skip it.
                    //   2) The brief surface.Load → post-Load detach pass
                    //      window in RGBController could let the queue's
                    //      trigger drain a buffered LED frame and send a
                    //      paint UDP packet before the per-device disable
                    //      flag is set. Setting it here, before the device
                    //      is added to the surface, closes that window.
                    var deviceGuid = Chromatics.Helpers.DeviceHelper.GenerateDeviceGuid(info.DeviceName);
                    bool disabled = Chromatics.Layers.MappingLayers.IsDeviceDisabled(deviceGuid);

                    if (disabled)
                        queue.SetPerDeviceDisabled(true);

                    await dev.CaptureOriginalStateAsync(turnOnIfOff: !disabled).ConfigureAwait(false);

                    devices.Add(dev);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Error, $"[LIFX] failed to set up {def.Label}: {ex.Message}");
                }
            }

            return devices;
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
        {
            // 50ms = 20Hz cap per device. LIFX bulbs accept sustained 20Hz
            // without dropping packets, but going faster risks UDP queue
            // overflow on smaller LIFX MCUs (especially older Color 1000).
            return new LifxDeviceUpdateTrigger(0.05);
        }

        // Run discovery and overlay any responses onto the existing definitions.
        // Returns mac→endpoint map so the caller can update each definition.
        private async Task<Dictionary<string, IPEndPoint>> ResolveEndpointsAsync(IList<LifxClientDefinition> defs, TimeSpan timeout)
        {
            var result = new Dictionary<string, IPEndPoint>(StringComparer.OrdinalIgnoreCase);

            // Use existing endpoints first — most adopted devices will still
            // be where we left them, and starting them up before the
            // broadcast lets the surface paint a frame faster on warm start.
            foreach (var d in defs)
            {
                if (d.Endpoint != null) result[d.Mac] = d.Endpoint;
            }

            try
            {
                var discovered = await LifxDiscovery.DiscoverAsync(timeout).ConfigureAwait(false);
                foreach (var d in discovered)
                    result[d.Mac] = d.Endpoint;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"[LIFX] discovery sweep failed: {ex.Message}");
            }

            return result;
        }

        // Mirror of HueRGBDeviceProvider.Dispose — gate every queue, capture
        // a teardown budget, restore each bulb to the state we saw at first
        // attach, then unhook the surface and clear our singleton so the
        // next Instance access constructs fresh.
        //
        // Sequential restore with 80ms pacing — the LIFX UDP stack handles
        // bursts but we want bulbs to actually transition cleanly, so giving
        // each one its own send window keeps them from racing.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    var devices = Devices.OfType<LifxDevice>().ToList();
                    foreach (var d in devices) d.BeginShutdown();

                    if (devices.Count > 0)
                    {
                        // Budget derived from the loop's own constants so
                        // they can't drift apart. The per-bulb worst case
                        // (timeout + pacing) only bites when bulbs are
                        // unreachable - exactly when restore matters most.
                        // The 30s cap matches the global shutdown ceiling in
                        // RGBController.Unload, which means fleets of 9+
                        // all-unreachable bulbs can still lose the tail of
                        // the list; reachable bulbs restore in well under a
                        // second each, so real setups fit comfortably.
                        const int perBulbTimeoutMs = 3000;
                        const int pacingMs = 80;
                        int worstCaseMs = pacingMs + devices.Count * (perBulbTimeoutMs + pacingMs);
                        int totalBudgetSec = Math.Min(30, 2 + (worstCaseMs + 999) / 1000);
                        Task.Run(async () =>
                        {
                            await Task.Delay(pacingMs).ConfigureAwait(false);
                            foreach (var d in devices)
                            {
                                try
                                {
                                    var t = d.RestoreOriginalStateAsync();
                                    var done = await Task.WhenAny(t, Task.Delay(perBulbTimeoutMs)).ConfigureAwait(false);
                                    if (done != t)
                                        Logger.WriteConsole(LoggerTypes.Devices, $"[LIFX] restore timed out for {d.DeviceInfo.DeviceName}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteConsole(LoggerTypes.Devices, $"[LIFX] restore failed for {d.DeviceInfo.DeviceName}: {ex.Message}");
                                }
                                await Task.Delay(pacingMs).ConfigureAwait(false);
                            }
                        }).Wait(TimeSpan.FromSeconds(totalBudgetSec));
                    }
                }
                catch { /* swallow during teardown */ }

                try { _udp?.Close(); } catch { }
                _udp?.Dispose();
                _udp = null;
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
