using Chromatics.Layers;

namespace Chromatics.Tests.Core;

public class ScreenCaptureProcessorLifetimeTests
{
    [Fact]
    public void Instance_IsRebuiltAfterDispose()
    {
        // DisposeAll runs every time the player returns to the title screen and
        // nulls the processor's surface. Handing the disposed object back out
        // left the Screen Capture base layer attaching to a dead surface on
        // every later tick, which killed lighting for the rest of the session.
        var first = ScreenCaptureProcessor.Instance;
        first.Dispose();

        var second = ScreenCaptureProcessor.Instance;

        Assert.NotSame(first, second);
    }
}
