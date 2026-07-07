using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol;
using System.Runtime.ExceptionServices;

namespace Chromatics.Tests.Extensions.Nanoleaf;

// Pins the discovery loop's no-throw contract. The first shipped loop used
// 1-second cancellation slices, which threw OperationCanceledException every
// quiet second as control flow. The exceptions were caught, but a debugger
// with break-on-thrown enabled halts on each one - which presents to the
// developer as the app crashing seconds after discovery starts. A healthy
// sweep must not throw at all.
public class NanoleafDiscoveryLoopTests
{
    [Fact]
    public async Task DiscoverAsync_QuietSweep_ThrowsNoCancellationExceptions()
    {
        int cancellationThrows = 0;
        EventHandler<FirstChanceExceptionEventArgs> recorder = (_, e) =>
        {
            // Match the production class only: test classes in this suite
            // carry "NanoleafDiscovery" in their names and throw their own
            // cancellations on parallel workers, so a loose substring counts
            // their noise as ours.
            if (e.Exception is OperationCanceledException &&
                Environment.StackTrace.Contains("Protocol.NanoleafDiscovery"))
            {
                Interlocked.Increment(ref cancellationThrows);
            }
        };

        AppDomain.CurrentDomain.FirstChanceException += recorder;
        try
        {
            await NanoleafDiscovery.DiscoverAsync(TimeSpan.FromMilliseconds(2500));
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= recorder;
        }

        Assert.Equal(0, cancellationThrows);
    }

    // A failed manual-IP probe is the expected outcome for a wrong address,
    // so it must report null without throwing anything - the first version
    // threw TaskCanceledException from HttpClient.Timeout as its "nothing
    // there" signal.
    [Fact]
    public async Task Probe_UnreachableOrWrongIp_ReturnsNullWithoutThrowing()
    {
        int throwsInProbe = 0;
        EventHandler<FirstChanceExceptionEventArgs> recorder = (_, e) =>
        {
            // "<ProbeAsync>" matches only the production state machine; this
            // test's own frames render as "<Probe_UnreachableOrWrongIp...>".
            if (Environment.StackTrace.Contains("<ProbeAsync>"))
                Interlocked.Increment(ref throwsInProbe);
        };

        AppDomain.CurrentDomain.FirstChanceException += recorder;
        try
        {
            // Refused: loopback port with no listener answers immediately.
            var refused = await NanoleafDiscovery.ProbeAsync("127.0.0.1", GetClosedTcpPort(), TimeSpan.FromSeconds(2));
            // Blackhole: TEST-NET-1 is reserved, so the connect times out.
            var timedOut = await NanoleafDiscovery.ProbeAsync("192.0.2.1", 16021, TimeSpan.FromMilliseconds(500));
            // Not an IP at all.
            var invalid = await NanoleafDiscovery.ProbeAsync("not-an-ip", 16021, TimeSpan.FromMilliseconds(100));

            Assert.Null(refused);
            Assert.Null(timedOut);
            Assert.Null(invalid);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= recorder;
        }

        Assert.Equal(0, throwsInProbe);
    }

    private static int GetClosedTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
