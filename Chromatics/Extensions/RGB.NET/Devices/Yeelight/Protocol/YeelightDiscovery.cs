using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight.Protocol
{
    // SSDP-based LAN discovery for Yeelight bulbs. Yeelight bulbs respond
    // to an `M-SEARCH * HTTP/1.1` UDP datagram sent to 239.255.255.250:1982
    // (note: not the standard SSDP port 1900) with `ST: wifi_bulb`, and
    // also emit unsolicited NOTIFY announces on the same multicast group
    // when they come online.
    //
    // The user has to enable "LAN Control" in the Yeelight / Mi Home app
    // before a bulb will respond. There's no host-side way to check for
    // that — bulbs with LAN control disabled simply don't appear in
    // discovery, indistinguishable from "no bulbs on the network". We
    // surface a hint in the empty-result case from the provider.
    //
    // Response is a small HTTP-shaped block with one header per line.
    // Relevant fields we parse:
    //
    //   Location: yeelight://<ip>:<port>     — control endpoint (port is always 55443)
    //   id: 0x000000000d2a4b1c                — stable 64-bit bulb id (hex)
    //   model: color | stripe | color1 | ...  — bulb model family
    //   support: get_prop set_default ...     — space-separated capability list
    //   power: on | off                       — current power state
    //   bright: 1..100                        — current brightness
    //   rgb: 16777215                         — packed RGB value (current)
    //   name: <user-set name>                 — friendly name (often empty)
    internal static class YeelightDiscovery
    {
        public const int SsdpPort = 1982;
        public const string SsdpMulticastAddress = "239.255.255.250";

        public const string ControlPort = "55443"; // every Yeelight LAN bulb uses this

        public sealed class DiscoveredBulb
        {
            public string Id { get; set; }            // hex id, lowercase, e.g. "0x000000000d2a4b1c"
            public IPEndPoint Endpoint { get; set; }  // ip + 55443
            public string Model { get; set; }
            public string Name { get; set; }
            public string FirmwareVersion { get; set; }
            public IReadOnlyList<string> Support { get; set; } = Array.Empty<string>();
            public string PowerState { get; set; }    // "on" / "off"
            public int Brightness { get; set; }       // 1..100, 0 if unknown
            public uint Rgb { get; set; }             // packed 24-bit RGB

            // Most user-visible identifier the Mapping tab will show. Falls
            // back through Name → Model → Id when the user hasn't bothered
            // setting a friendly name on the bulb.
            public string DisplayLabel => !string.IsNullOrWhiteSpace(Name)
                ? Name
                : !string.IsNullOrWhiteSpace(Model)
                    ? $"Yeelight {Model}"
                    : Id ?? "Yeelight bulb";
        }

        // Active discovery. Sends one M-SEARCH and listens for replies up
        // to `timeout`. Multiple bulbs answer in parallel; we de-duplicate
        // by Id so a bulb that replies twice during the window only shows
        // up once.
        public static async Task<IReadOnlyList<DiscoveredBulb>> DiscoverAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, DiscoveredBulb>(StringComparer.OrdinalIgnoreCase);

            using var udp = new UdpClient(0)
            {
                EnableBroadcast = true,
            };
            // Multicast TTL of 2 hops — bulbs are almost always on the same
            // subnet, but some home routers put IoT VLANs one router-hop
            // away and we want to reach those. Going higher than 2 risks
            // discovery leaking into neighbouring networks for users on
            // misconfigured routers.
            udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);

            var multicast = new IPEndPoint(IPAddress.Parse(SsdpMulticastAddress), SsdpPort);

            // The M-SEARCH packet Yeelight bulbs expect. Note the `\r\n`
            // line endings and `ST: wifi_bulb` — that's the Yeelight-specific
            // service type, not the generic UPnP `ssdp:all`. The bulb
            // firmware filters by this value.
            string mSearch =
                "M-SEARCH * HTTP/1.1\r\n" +
                $"HOST: {SsdpMulticastAddress}:{SsdpPort}\r\n" +
                "MAN: \"ssdp:discover\"\r\n" +
                "ST: wifi_bulb\r\n";
            byte[] payload = Encoding.UTF8.GetBytes(mSearch);

            try { await udp.SendAsync(payload, payload.Length, multicast).ConfigureAwait(false); }
            catch { return Array.Empty<DiscoveredBulb>(); }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                while (!timeoutCts.IsCancellationRequested)
                {
                    UdpReceiveResult resp;
                    try { resp = await udp.ReceiveAsync(timeoutCts.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                    catch { continue; }

                    var bulb = ParseResponse(resp.Buffer);
                    if (bulb == null || string.IsNullOrEmpty(bulb.Id)) continue;
                    if (bulb.Endpoint == null)
                    {
                        // Location header was malformed or missing; fall back
                        // to the sender's address with the standard port so
                        // the bulb is still addressable.
                        bulb.Endpoint = new IPEndPoint(resp.RemoteEndPoint.Address, 55443);
                    }
                    results[bulb.Id] = bulb; // last response wins (most recent state)
                }
            }
            catch { /* swallow — partial results are still useful */ }

            return new List<DiscoveredBulb>(results.Values);
        }

        // Parse one SSDP response into a DiscoveredBulb. Returns null on
        // a clearly-malformed payload. Tolerant of header order and case
        // — Yeelight firmware versions vary on both.
        private static DiscoveredBulb ParseResponse(byte[] buffer)
        {
            string text;
            try { text = Encoding.UTF8.GetString(buffer); }
            catch { return null; }

            var bulb = new DiscoveredBulb();
            string[] lines = text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in lines)
            {
                int colon = raw.IndexOf(':');
                if (colon <= 0 || colon >= raw.Length - 1) continue;
                string key = raw.Substring(0, colon).Trim().ToLowerInvariant();
                string value = raw.Substring(colon + 1).Trim();

                switch (key)
                {
                    case "location":
                        bulb.Endpoint = ParseYeelightUri(value);
                        break;
                    case "id":
                        bulb.Id = value;
                        break;
                    case "model":
                        bulb.Model = value;
                        break;
                    case "name":
                        bulb.Name = value;
                        break;
                    case "fw_ver":
                        bulb.FirmwareVersion = value;
                        break;
                    case "support":
                        bulb.Support = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        break;
                    case "power":
                        bulb.PowerState = value;
                        break;
                    case "bright":
                        if (int.TryParse(value, out int b)) bulb.Brightness = b;
                        break;
                    case "rgb":
                        if (uint.TryParse(value, out uint c)) bulb.Rgb = c;
                        break;
                }
            }
            return bulb;
        }

        // `yeelight://192.168.1.42:55443` → IPEndPoint(192.168.1.42, 55443).
        // Returns null on any parse failure so the caller can fall back to
        // the SSDP sender address.
        private static IPEndPoint ParseYeelightUri(string uri)
        {
            const string prefix = "yeelight://";
            if (string.IsNullOrEmpty(uri) || !uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
            string body = uri.Substring(prefix.Length);
            int colon = body.LastIndexOf(':');
            if (colon <= 0) return null;
            string ipPart = body.Substring(0, colon);
            string portPart = body.Substring(colon + 1);
            if (!IPAddress.TryParse(ipPart, out var ip)) return null;
            if (!int.TryParse(portPart, out int port)) return null;
            return new IPEndPoint(ip, port);
        }
    }
}
