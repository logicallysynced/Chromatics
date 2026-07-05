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
}
