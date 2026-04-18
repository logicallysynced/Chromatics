using System.Drawing;
using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class SizeHelperTests
{
    [Fact]
    public void ResizeKeepAspect_ShrinksLargerImage_PreservesAspect()
    {
        var src = new Size(1920, 1080);
        var result = src.ResizeKeepAspect(960, 540);

        Assert.Equal(960, result.Width);
        Assert.Equal(540, result.Height);
    }

    [Fact]
    public void ResizeKeepAspect_FitsWithinNonMatchingAspect()
    {
        // 1920x1080 into a 960x1000 box should scale by width (ratio 0.5) to 960x540.
        var src = new Size(1920, 1080);
        var result = src.ResizeKeepAspect(960, 1000);

        Assert.Equal(960, result.Width);
        Assert.Equal(540, result.Height);
    }

    [Fact]
    public void ResizeKeepAspect_WithoutEnlarge_DoesNotUpscale()
    {
        // Default enlarge=false: a 100x100 image fed "max 500x500" should stay 100x100.
        var src = new Size(100, 100);
        var result = src.ResizeKeepAspect(500, 500);

        Assert.Equal(100, result.Width);
        Assert.Equal(100, result.Height);
    }

    [Fact]
    public void ResizeKeepAspect_WithEnlarge_Upscales()
    {
        var src = new Size(100, 100);
        var result = src.ResizeKeepAspect(500, 500, enlarge: true);

        Assert.Equal(500, result.Width);
        Assert.Equal(500, result.Height);
    }

    [Fact]
    public void ResizeKeepAspect_AsymmetricBounds_TakesTighterAxis()
    {
        // Tall image, narrow bound — height becomes limiting factor on the *source*
        // since enlarge=false clamps maxHeight to src.Height=200.
        var src = new Size(400, 200);
        var result = src.ResizeKeepAspect(100, 200);

        Assert.Equal(100, result.Width);
        Assert.Equal(50, result.Height);
    }
}
