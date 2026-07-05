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
        //
        // The query sets the QU (unicast-response) bit, so controllers reply
        // directly to our ephemeral port. Without it, responders multicast
        // their answers to port 5353 - which the Windows mDNS service owns,
        // so we would never see a single reply.
        public static async Task<IReadOnlyList<NanoleafDiscoveredController>> DiscoverAsync(TimeSpan timeout, CancellationToken ct = default)
        {
            var found = new Dictionary<string, NanoleafDiscoveredController>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var udp = CreateDiscoverySocket();

                var query = BuildPtrQuery(ServiceType);
                var target = new IPEndPoint(MulticastAddr, MdnsPort);
                await udp.SendAsync(query, query.Length, target).ConfigureAwait(false);

                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                deadline.CancelAfter(timeout);
                int socketErrors = 0;

                while (!deadline.IsCancellationRequested)
                {
                    // 1s receive slices inside the overall deadline: a quiet
                    // slice re-sends the query (mDNS is lossy; repeat asks
                    // are standard) instead of blocking until the deadline.
                    using var slice = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
                    slice.CancelAfter(1000);

                    UdpReceiveResult res;
                    try
                    {
                        res = await udp.ReceiveAsync(slice.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        if (deadline.IsCancellationRequested) break;
                        try { await udp.SendAsync(query, query.Length, target).ConfigureAwait(false); }
                        catch (SocketException) { if (++socketErrors >= 5) break; }
                        continue;
                    }
                    catch (SocketException)
                    {
                        // Stray ICMP or transient stack error: skip the
                        // packet, but bail if the socket looks dead so a
                        // permanent fault can't spin the loop.
                        if (++socketErrors >= 5) break;
                        continue;
                    }

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

        // The discovery socket, with Windows ICMP-reset reporting turned
        // off. Windows converts ICMP "port unreachable" from any earlier
        // send into a SocketException on the NEXT receive, and keeps
        // rethrowing it on every receive after that - one unreachable host
        // turns the receive loop into a repeated-throw storm. The
        // SIO_UDP_CONNRESET ioctl disables that reporting for this socket.
        // Public so the test suite can prove the production config survives
        // an ICMP reset without throwing.
        public static UdpClient CreateDiscoverySocket()
        {
            var udp = new UdpClient(AddressFamily.InterNetwork);
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            const int SIO_UDP_CONNRESET = unchecked((int)0x9800000C);
            try { udp.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); }
            catch { /* unsupported off-Windows; the receive loop's catch still covers it */ }

            return udp;
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
            body.AddRange(new byte[] { 0, 12 });    // QTYPE PTR
            // QCLASS IN with the top (QU) bit set: asks responders to reply
            // unicast to our source port instead of multicasting to 5353.
            body.AddRange(new byte[] { 0x80, 1 });
            return body.ToArray();
        }

        // We don't fully parse the DNS response - stitching SRV/A across
        // compressed names is fiddly and error-prone. A response with
        // answers that carries the Nanoleaf service label is enough: the
        // responder's address is the controller, on the standard port. The
        // adoption pairing step confirms identity via REST.
        private static NanoleafDiscoveredController TryParseResponse(byte[] buffer, IPAddress source)
        {
            if (buffer == null || buffer.Length < 12) return null;
            // answer count in the header must be > 0 for a real response
            ushort ancount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(6, 2));
            if (ancount == 0) return null;
            if (!ContainsServiceLabel(buffer)) return null;

            return new NanoleafDiscoveredController
            {
                Label = source.ToString(),
                Endpoint = new IPEndPoint(source, 16021),
            };
        }

        // DNS name compression can only point backwards, so the first
        // occurrence of the service name is always spelled out - a literal
        // scan for the "_nanoleafapi" label is a reliable containment test.
        private static readonly byte[] ServiceLabel = Encoding.ASCII.GetBytes("_nanoleafapi");

        public static bool ContainsServiceLabel(byte[] buffer)
        {
            return buffer.AsSpan().IndexOf(ServiceLabel) >= 0;
        }
    }
}
