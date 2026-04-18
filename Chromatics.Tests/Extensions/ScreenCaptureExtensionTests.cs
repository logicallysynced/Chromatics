using Chromatics.Extensions;

namespace Chromatics.Tests.Extensions;

public class ScreenCaptureExtensionTests
{
    [Fact]
    public void Dispose_WithoutStart_DoesNotThrow()
    {
        // Regression: previously the class had no IDisposable contract and
        // relied on GC.Collect to clean up leaked GDI handles. A never-started
        // instance must dispose cleanly.
        var capture = new ScreenCaptureExtension();
        var ex = Record.Exception(() => capture.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var capture = new ScreenCaptureExtension();
        capture.Dispose();
        var ex = Record.Exception(() => capture.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Stop_WithoutStart_IsNoOp()
    {
        var capture = new ScreenCaptureExtension();
        var ex = Record.Exception(() => capture.Stop());
        Assert.Null(ex);
    }

    [Fact]
    public void GetScreenColours_BeforeStart_ReturnsNull()
    {
        using var capture = new ScreenCaptureExtension();
        Assert.Null(capture.GetScreenColours());
    }

    [Fact]
    public void Start_AfterDispose_ThrowsObjectDisposedException()
    {
        var capture = new ScreenCaptureExtension();
        capture.Dispose();
        Assert.Throws<ObjectDisposedException>(() => capture.Start());
    }
}
