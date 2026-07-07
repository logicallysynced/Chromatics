using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol;
using System.Net;
using System.Net.Sockets;

namespace Chromatics.Tests.Extensions.Nanoleaf;

// Pins the root cause of the discovery crash and proves the fix.
//
// Mechanism: Windows queues an ICMP "port unreachable" received for any
// earlier send as a SocketException on the socket's NEXT receive, and keeps
// rethrowing it on every receive after that. The original discovery loop
// only caught OperationCanceledException, so one ICMP reset became a
// repeated throw from DiscoverAsync - the continuous crash reported when
// testing the provider. The fix disables that reporting per-socket via the
// SIO_UDP_CONNRESET ioctl (CreateDiscoverySocket) and handles
// SocketException per-iteration as a backstop.
public class NanoleafDiscoverySocketTests
{
    private static int GetClosedUdpPort()
    {
        using var probe = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)probe.Client.LocalEndPoint!).Port;
        // disposed on return - the port is closed again
    }

    // Documents the OS mechanism the bug rode on: a default-configured
    // socket that sent to a closed port throws SocketException on receive.
    // If this ever stops failing on a future Windows build, the ioctl is
    // redundant but harmless.
    [Fact]
    public async Task DefaultSocket_ReceiveAfterIcmpReset_ThrowsSocketException()
    {
        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        bool gotReset = false;
        for (int i = 0; i < 5 && !gotReset; i++)
        {
            // Fresh closed port per attempt: another process grabbing the
            // probed port between attempts must not starve the test of ICMP.
            var closedTarget = new IPEndPoint(IPAddress.Loopback, GetClosedUdpPort());
            await udp.SendAsync(new byte[] { 0 }, 1, closedTarget);
            try
            {
                using var cts = new CancellationTokenSource(200);
                await udp.ReceiveAsync(cts.Token);
            }
            catch (SocketException) { gotReset = true; }
            catch (OperationCanceledException) { /* ICMP not queued yet; send again */ }
        }

        Assert.True(gotReset, "Expected Windows to surface the ICMP reset as a SocketException on receive.");
    }

    // The fix: the production discovery socket takes the same ICMP resets
    // without throwing, and still receives real datagrams afterwards.
    [Fact]
    public async Task DiscoverySocket_SurvivesIcmpReset_AndStillReceives()
    {
        using var udp = NanoleafDiscovery.CreateDiscoverySocket();
        int ourPort = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
        var closedTarget = new IPEndPoint(IPAddress.Loopback, GetClosedUdpPort());

        // Provoke the resets. A SocketException here fails the test - that
        // is the point.
        for (int i = 0; i < 5; i++)
        {
            await udp.SendAsync(new byte[] { 0 }, 1, closedTarget);
            try
            {
                using var cts = new CancellationTokenSource(100);
                await udp.ReceiveAsync(cts.Token);
            }
            catch (OperationCanceledException) { /* quiet is the expected outcome */ }
        }

        // The socket must still be usable for real traffic.
        var payload = new byte[] { 0xAB, 0xCD };
        using var sender = new UdpClient(AddressFamily.InterNetwork);
        await sender.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Loopback, ourPort));

        using var recvCts = new CancellationTokenSource(2000);
        var result = await udp.ReceiveAsync(recvCts.Token);
        Assert.Equal(payload, result.Buffer);
    }
}
