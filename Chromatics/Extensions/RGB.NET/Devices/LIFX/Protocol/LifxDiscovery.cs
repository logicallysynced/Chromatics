using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol
{
    // UDP-based LIFX discovery. Sends GetService broadcasts on port 56700 and
    // collects StateService responses, then per-device follow-up queries
    // (GetLabel, GetVersion, GetExtendedColorZones) so the adoption dialog has
    // enough info to render a meaningful list.
    //
    // Why a few rounds instead of one: UDP loss on consumer routers is non-trivial,
    // especially on 2.4GHz networks where most LIFX bulbs sit. Multiple GetService
    // sends spread across a 3s window catch devices that drop the first packet;
    // dedup by MAC is cheap.
    //
    // Why broadcast 255.255.255.255 AND per-interface directed broadcasts: some
    // dual-NIC machines (VPN, virtual switches) bind 255.255.255.255 to the wrong
    // interface, missing the wifi LAN where bulbs live. Per-interface broadcast
    // covers that case at the cost of a few extra packets.
    public static class LifxDiscovery
    {
        public const int LifxPort = 56700;

        public sealed class DiscoveredDevice
        {
            public string Mac { get; set; }
            public IPEndPoint Endpoint { get; set; }
            public string Label { get; set; }
            public uint VendorId { get; set; }
            public uint ProductId { get; set; }
            public ushort ZoneCount { get; set; }
        }

        // Three-second discovery window with progress callback for the dialog.
        // Returns devices seen at least once. Per-device follow-up queries
        // (label, version, zones) run in parallel as soon as a StateService
        // arrives, so the dialog can populate before the window closes.
        public static async Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(
            TimeSpan timeout,
            Action<DiscoveredDevice> onDeviceSeen = null,
            CancellationToken cancellationToken = default)
        {
            var devices = new ConcurrentDictionary<string, DiscoveredDevice>(StringComparer.OrdinalIgnoreCase);
            uint source = (uint)Random.Shared.Next(2, int.MaxValue);

            using var udp = new UdpClient(0)
            {
                EnableBroadcast = true,
            };
            udp.Client.ReceiveBufferSize = 64 * 1024;

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);

            // Cancellation primitive that completes without throwing — used to
            // race against the receive task and the inter-broadcast delay so
            // OperationCanceledException doesn't fire on every shutdown
            // (debuggers break on first-chance even when caught, and discovery
            // ends every adoption-dialog open).
            var cancelTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancelReg = deadline.Token.Register(() => cancelTcs.TrySetResult());

            // Receive loop runs concurrently with the broadcast loop.
            var followUpTasks = new ConcurrentBag<Task>();
            var receiver = Task.Run(async () =>
            {
                while (!deadline.IsCancellationRequested)
                {
                    try
                    {
                        // Race the next datagram against deadline cancellation.
                        // No CancellationToken on ReceiveAsync — passing one
                        // makes it throw on cancel; instead we let the socket
                        // close in the outer finally and ignore the orphan
                        // receive task here.
                        var receiveTask = udp.ReceiveAsync();
                        var winner = await Task.WhenAny(receiveTask, cancelTcs.Task).ConfigureAwait(false);
                        if (winner != receiveTask) break;
                        var result = receiveTask.Result;
                        if (!LifxPacket.TryReadHeader(result.Buffer, out var hdr)) continue;
                        if (hdr.Source != source) continue;

                        switch (hdr.MessageType)
                        {
                            case LifxMessageTypes.StateService:
                            {
                                var payload = LifxPacket.Payload(result.Buffer);
                                if (payload.Length < 5) break;
                                byte service = payload[0];
                                if (service != 1) break; // 1 = UDP
                                uint port = BitConverter.ToUInt32(payload.Slice(1, 4));
                                var ep = new IPEndPoint(result.RemoteEndPoint.Address, (int)port);
                                string mac = hdr.TargetMac;

                                bool added = false;
                                var dev = devices.GetOrAdd(mac, _ =>
                                {
                                    added = true;
                                    return new DiscoveredDevice
                                    {
                                        Mac = mac,
                                        Endpoint = ep,
                                        Label = $"LIFX ({mac})",
                                    };
                                });
                                if (added)
                                {
                                    onDeviceSeen?.Invoke(dev);
                                    followUpTasks.Add(QueryDeviceAsync(udp, source, dev, deadline.Token));
                                }
                                break;
                            }
                            case LifxMessageTypes.StateLabel:
                            {
                                if (devices.TryGetValue(hdr.TargetMac, out var dev))
                                {
                                    var payload = LifxPacket.Payload(result.Buffer);
                                    if (payload.Length >= 32)
                                        dev.Label = ReadLabel(payload[..32]);
                                }
                                break;
                            }
                            case LifxMessageTypes.StateVersion:
                            {
                                if (devices.TryGetValue(hdr.TargetMac, out var dev))
                                {
                                    var payload = LifxPacket.Payload(result.Buffer);
                                    if (payload.Length >= 8)
                                    {
                                        dev.VendorId = BitConverter.ToUInt32(payload.Slice(0, 4));
                                        dev.ProductId = BitConverter.ToUInt32(payload.Slice(4, 4));
                                    }
                                }
                                break;
                            }
                            case LifxMessageTypes.StateExtendedColorZones:
                            {
                                if (devices.TryGetValue(hdr.TargetMac, out var dev))
                                {
                                    var payload = LifxPacket.Payload(result.Buffer);
                                    // payload: zones_count(2) + zone_index(2) + colors_count(1) + 82 colors
                                    if (payload.Length >= 5)
                                        dev.ZoneCount = BitConverter.ToUInt16(payload.Slice(0, 2));
                                }
                                break;
                            }
                        }
                    }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception) { /* ignore malformed datagrams; keep listening */ }
                }
            });

            // Broadcast GetService a few times across the window. A bulb that
            // misses the first packet usually catches the second.
            byte[] broadcastPacket = LifxPacket.Build(
                LifxMessageTypes.GetService,
                target: null,
                source: source,
                sequence: 0,
                payload: ReadOnlySpan<byte>.Empty,
                tagged: true);

            var broadcastTargets = GetBroadcastTargets();

            try
            {
                int rounds = Math.Max(1, (int)(timeout.TotalMilliseconds / 600));
                for (int i = 0; i < rounds && !deadline.IsCancellationRequested; i++)
                {
                    foreach (var target in broadcastTargets)
                    {
                        // No CancellationToken — SendAsync(token) throws on
                        // cancellation, which would trip the debugger on
                        // every discovery completion. We check
                        // IsCancellationRequested between sends instead.
                        try { await udp.SendAsync(broadcastPacket, target).ConfigureAwait(false); }
                        catch { /* one bad NIC shouldn't kill the rest */ }
                        if (deadline.IsCancellationRequested) break;
                    }

                    // Race the inter-round delay against deadline cancellation
                    // so we exit cleanly instead of throwing TaskCanceled when
                    // the dialog closes mid-sweep.
                    var winner = await Task.WhenAny(Task.Delay(600), cancelTcs.Task).ConfigureAwait(false);
                    if (winner == cancelTcs.Task) break;
                }

                // Hold until the deadline elapses so in-flight responses arrive.
                await cancelTcs.Task.ConfigureAwait(false);
            }
            finally
            {
                udp.Close();
                try { await receiver.ConfigureAwait(false); } catch { }
                try { await Task.WhenAll(followUpTasks).ConfigureAwait(false); } catch { }
            }

            return new List<DiscoveredDevice>(devices.Values);
        }

        // Per-device follow-up queries fired the moment we first see the
        // device. No CancellationToken on the sends — these are 36-byte
        // packets, completing in microseconds; throwing on shutdown to save
        // microseconds of work isn't worth the first-chance exception break.
        private static async Task QueryDeviceAsync(UdpClient udp, uint source, DiscoveredDevice dev, CancellationToken ct)
        {
            byte[] target = LifxHeader.TargetFromMac(dev.Mac);

            byte[] getLabel = LifxPacket.Build(LifxMessageTypes.GetLabel, target, source, 1, ReadOnlySpan<byte>.Empty);
            byte[] getVersion = LifxPacket.Build(LifxMessageTypes.GetVersion, target, source, 2, ReadOnlySpan<byte>.Empty);
            byte[] getZones = LifxPacket.Build(LifxMessageTypes.GetExtendedColorZones, target, source, 3, ReadOnlySpan<byte>.Empty);

            try
            {
                if (ct.IsCancellationRequested) return;
                await udp.SendAsync(getLabel, dev.Endpoint).ConfigureAwait(false);
                if (ct.IsCancellationRequested) return;
                await udp.SendAsync(getVersion, dev.Endpoint).ConfigureAwait(false);
                if (ct.IsCancellationRequested) return;
                await udp.SendAsync(getZones, dev.Endpoint).ConfigureAwait(false);
            }
            catch { /* Best-effort; missing extra metadata is acceptable. */ }
        }

        // 255.255.255.255 plus the directed broadcast for every up IPv4
        // interface that's not a loopback. Covers VPN / multi-NIC setups
        // where the global broadcast doesn't make it onto the LAN.
        private static List<IPEndPoint> GetBroadcastTargets()
        {
            var targets = new List<IPEndPoint>
            {
                new IPEndPoint(IPAddress.Broadcast, LifxPort),
            };

            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    var ipProps = nic.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(addr.Address)) continue;
                        var bcast = ComputeDirectedBroadcast(addr.Address, addr.IPv4Mask);
                        if (bcast != null && !bcast.Equals(IPAddress.Broadcast))
                            targets.Add(new IPEndPoint(bcast, LifxPort));
                    }
                }
            }
            catch { /* fall back to limited broadcast */ }

            return targets;
        }

        private static IPAddress ComputeDirectedBroadcast(IPAddress ip, IPAddress mask)
        {
            if (ip == null || mask == null) return null;
            var ipBytes = ip.GetAddressBytes();
            var maskBytes = mask.GetAddressBytes();
            if (ipBytes.Length != 4 || maskBytes.Length != 4) return null;
            var bcast = new byte[4];
            for (int i = 0; i < 4; i++)
                bcast[i] = (byte)(ipBytes[i] | (~maskBytes[i] & 0xFF));
            return new IPAddress(bcast);
        }

        private static string ReadLabel(ReadOnlySpan<byte> bytes)
        {
            int n = 0;
            while (n < bytes.Length && bytes[n] != 0) n++;
            return Encoding.UTF8.GetString(bytes[..n]);
        }
    }
}
