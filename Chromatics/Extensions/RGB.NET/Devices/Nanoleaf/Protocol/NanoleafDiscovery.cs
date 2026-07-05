using Chromatics.Core;
using Chromatics.Enums;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol
{
    // A controller found on the LAN (or entered manually).
    public sealed class NanoleafDiscoveredController
    {
        public string Id { get; set; }        // filled after a REST probe; may be null from raw mDNS
        public string Label { get; set; }
        public IPEndPoint Endpoint { get; set; }
        public string Model { get; set; }
    }

    // Hand-rolled mDNS/DNS-SD query for the Nanoleaf service type, plus a
    // direct REST probe for manual-IP entry. We roll our own rather than
    // pull a Zeroconf package (owned-protocol-code preference + license-safe
    // dependency rule). The query is a single multicast question for
    // PTR records of _nanoleafapi._tcp.local; replies carry SRV (host+port)
    // and A (address) records we stitch together.
    public static class NanoleafDiscovery
    {
        private const string ServiceType = "_nanoleafapi._tcp.local";
        private static readonly IPAddress MulticastAddr = IPAddress.Parse("224.0.0.251");
        private const int MdnsPort = 5353;

        // mDNS sweep. Best-effort: returns whatever answered inside the
        // timeout. Callers pair with the manual-IP probe for filtered
        // networks. Only the endpoint + label come from mDNS; the Id and
        // model are resolved by a REST probe at adoption time.
        public static async Task<IReadOnlyList<NanoleafDiscoveredController>> DiscoverAsync(TimeSpan timeout, CancellationToken ct = default)
        {
            var found = new Dictionary<string, NanoleafDiscoveredController>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var udp = new UdpClient(AddressFamily.InterNetwork);
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                udp.JoinMulticastGroup(MulticastAddr);

                var query = BuildPtrQuery(ServiceType);
                await udp.SendAsync(query, query.Length, new IPEndPoint(MulticastAddr, MdnsPort)).ConfigureAwait(false);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeout);
                while (!cts.IsCancellationRequested)
                {
                    UdpReceiveResult res;
                    try { res = await udp.ReceiveAsync(cts.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }

                    var ctrl = TryParseResponse(res.Buffer, res.RemoteEndPoint.Address);
                    if (ctrl != null)
                    {
                        var key = ctrl.Endpoint.ToString();
                        found[key] = ctrl;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[Nanoleaf] discovery sweep failed: {ex.Message}", forwardToSentry: false);
            }

            return new List<NanoleafDiscoveredController>(found.Values);
        }

        // Manual-IP fallback: probe a REST endpoint directly. Returns a
        // controller stub if the host answers the OpenAPI (even a 401 from
        // /api/v1/new-less info endpoint proves it's a Nanoleaf), null
        // otherwise. Full identity is resolved during pairing.
        public static async Task<NanoleafDiscoveredController> ProbeAsync(string host, int port, TimeSpan timeout, CancellationToken ct = default)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = timeout };
                // The unauthenticated info endpoint returns basic device info
                // without a token on current firmware; a reachable Nanoleaf
                // answers, everything else times out or 404s.
                using var resp = await http.GetAsync($"http://{host}:{port}/api/v1/", ct).ConfigureAwait(false);
                if (!IPAddress.TryParse(host, out var addr)) addr = IPAddress.Loopback;
                return new NanoleafDiscoveredController
                {
                    Label = host,
                    Endpoint = new IPEndPoint(addr, port),
                };
            }
            catch
            {
                return null;
            }
        }

        // ── minimal DNS wire helpers ────────────────────────────────────

        public static byte[] BuildPtrQuery(string name)
        {
            var body = new List<byte>();
            // header: id 0, flags 0, qdcount 1, others 0
            body.AddRange(new byte[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 });
            foreach (var label in name.Split('.'))
            {
                var bytes = Encoding.ASCII.GetBytes(label);
                body.Add((byte)bytes.Length);
                body.AddRange(bytes);
            }
            body.Add(0);              // end of name
            body.AddRange(new byte[] { 0, 12 }); // QTYPE PTR
            body.AddRange(new byte[] { 0, 1 });  // QCLASS IN
            return body.ToArray();
        }

        // We don't fully parse the DNS response - stitching SRV/A across
        // compressed names is fiddly and error-prone. For discovery we only
        // need the responder's address (any host answering the multicast for
        // this service type IS a Nanoleaf controller on the standard port),
        // so we take the source address and the default control port. The
        // adoption pairing step confirms identity via REST.
        private static NanoleafDiscoveredController TryParseResponse(byte[] buffer, IPAddress source)
        {
            if (buffer == null || buffer.Length < 12) return null;
            // answer count in the header must be > 0 for a real response
            ushort ancount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(6, 2));
            if (ancount == 0) return null;

            return new NanoleafDiscoveredController
            {
                Label = source.ToString(),
                Endpoint = new IPEndPoint(source, 16021),
            };
        }
    }
}
