using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.Yeelight.Protocol;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight
{
    public class YeelightRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Singleton

        private static YeelightRGBDeviceProvider _instance;
        public static YeelightRGBDeviceProvider Instance => _instance ?? new YeelightRGBDeviceProvider();

        public YeelightRGBDeviceProvider()
        {
            if (_instance != null) Throw(new Exception($"There can be only one instance of type {nameof(YeelightRGBDeviceProvider)}"));
            _instance = this;
        }

        #endregion

        // Bulbs the user has chosen to control. Hydrated from settings by
        // RGBController.Setup before LoadDeviceProvider runs; cleared on
        // Dispose so the next enable cycle can re-prompt with a fresh
        // discovery sweep.
        public List<YeelightClientDefinition> ClientDefinitions { get; } = new();

        // Discovery sweep timeout when no last-known endpoints are stored.
        // Yeelight bulbs respond within ~500ms typically; 2.5s gives slow
        // responders headroom on busy networks.
        private static readonly TimeSpan DiscoverySweepTimeout = TimeSpan.FromMilliseconds(2500);

        private readonly Dictionary<IRGBDevice, YeelightConnection> _openConnections = new();

        protected override void InitializeSDK()
        {
            // Yeelight LAN is a plain socket protocol — no SDK init required.
            // Connection establishment happens per-bulb in LoadDevices.
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
        {
            // 30Hz upper bound. Yeelight bulbs accept up to ~60Hz over Music
            // Mode in practice, but going above 30 doesn't translate to a
            // perceptible improvement and bumps lighter bulbs (Color 1S)
            // closer to thermal cutoff during long colour-cycling layers.
            return new YeelightDeviceUpdateTrigger(1.0 / 30.0);
        }

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            return LoadDevicesAsync().GetAwaiter().GetResult();
        }

        private async Task<IEnumerable<IRGBDevice>> LoadDevicesAsync()
        {
            var devices = new List<IRGBDevice>();
            if (ClientDefinitions.Count == 0) return devices;

            // Re-discover up-front so DHCP renewals don't strand us on a
            // stale IP. Bulbs reply with their Id, which is stable across
            // IP changes; we overlay the freshly-discovered endpoint onto
            // the existing definitions for any id we recognise.
            var resolved = await ResolveEndpointsAsync(ClientDefinitions, DiscoverySweepTimeout).ConfigureAwait(false);
            foreach (var def in ClientDefinitions)
            {
                if (resolved.TryGetValue(def.Id, out var ep))
                    def.Endpoint = ep;
            }

            foreach (var def in ClientDefinitions)
            {
                if (def.Endpoint == null)
                {
                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[Yeelight] {def.Label}: not reachable on the network — skipping. Will retry on next enable.");
                    continue;
                }

                try
                {
                    var connection = new YeelightConnection(def.Endpoint);
                    await connection.ConnectAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

                    // Try Music Mode. If the bulb supports it and our reverse
                    // TCP is reachable from the bulb's network position,
                    // we'll be uncapped on send rate. Falls back to outbound
                    // (with the per-minute rate cap silently enforced by
                    // the bulb) when Music Mode setup fails.
                    if (def.SupportsMusicMode)
                    {
                        var localIp = ResolveLocalIpReachableTo(def.Endpoint.Address);
                        if (localIp != null)
                        {
                            bool ok = await connection.EnterMusicModeAsync(localIp, TimeSpan.FromMilliseconds(1500)).ConfigureAwait(false);
                            if (!ok)
                            {
                                Logger.WriteConsole(LoggerTypes.Devices,
                                    $"[Yeelight] {def.Label}: Music Mode handshake failed (firewall blocking reverse TCP?); falling back to outbound (rate-capped).",
                                    forwardToSentry: false);
                            }
                        }
                    }

                    var trigger = (YeelightDeviceUpdateTrigger)GetUpdateTrigger();
                    var queue = new YeelightUpdateQueue(trigger, def, connection);
                    var info = new YeelightDeviceInfo(def);
                    var dev = new YeelightDevice(info, queue, def);

                    // Honour per-device disable from a prior session before
                    // the surface attaches. Same race window as LIFX/Hue:
                    // surface.Load attaches every device unconditionally,
                    // and a frame can drain between attach and our
                    // post-Load detach pass. Setting the gate flag here
                    // closes that window.
                    var deviceGuid = Chromatics.Helpers.DeviceHelper.GenerateDeviceGuid(info.DeviceName);
                    bool disabled = Chromatics.Layers.MappingLayers.IsDeviceDisabled(deviceGuid);
                    if (disabled) queue.SetPerDeviceDisabled(true);

                    _openConnections[dev] = connection;
                    devices.Add(dev);
                }
                catch (Exception ex)
                {
                    bool unreachable = Chromatics.Helpers.NetworkFailureHelper.IsUnreachable(ex);
                    Logger.WriteConsole(unreachable ? LoggerTypes.Devices : LoggerTypes.Error,
                        $"[Yeelight] failed to set up {def.Label}: {ex.Message}",
                        forwardToSentry: !unreachable);
                }
            }

            return devices;
        }

        // Overlay discovery results onto the existing definitions. Bulbs are
        // identified by their stable hex Id, which survives IP changes.
        private async Task<Dictionary<string, IPEndPoint>> ResolveEndpointsAsync(IList<YeelightClientDefinition> defs, TimeSpan timeout)
        {
            var result = new Dictionary<string, IPEndPoint>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in defs)
                if (d.Endpoint != null) result[d.Id] = d.Endpoint;

            try
            {
                var discovered = await YeelightDiscovery.DiscoverAsync(timeout).ConfigureAwait(false);
                foreach (var d in discovered)
                    if (!string.IsNullOrEmpty(d.Id) && d.Endpoint != null)
                        result[d.Id] = d.Endpoint;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"[Yeelight] discovery sweep failed: {ex.Message}");
            }
            return result;
        }

        // Pick the local IP address Windows would route to the bulb. We do
        // this by opening a UDP socket and asking it to "connect" to the
        // bulb's address — no packets are sent, but the OS resolves the
        // outbound interface and we can read its local IP. This handles
        // multi-NIC machines correctly without us having to enumerate
        // interfaces and guess.
        private static IPAddress ResolveLocalIpReachableTo(IPAddress remote)
        {
            try
            {
                using var probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                probe.Connect(remote, 55443);
                if (probe.LocalEndPoint is IPEndPoint ep)
                    return ep.Address;
            }
            catch { /* no reachable interface — caller falls back to no-music-mode */ }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    var devices = Devices.OfType<YeelightDevice>().ToList();
                    foreach (var d in devices) d.BeginShutdown();
                }
                catch { /* swallow during teardown */ }

                foreach (var conn in _openConnections.Values)
                {
                    try { conn.Dispose(); } catch { /* ignore */ }
                }
                _openConnections.Clear();
                ClientDefinitions.Clear();
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
