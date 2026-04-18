using Chromatics.Helpers;
using DrawingColor = System.Drawing.Color;
using RgbNetColor = RGB.NET.Core.Color;

namespace Chromatics.Tests.Helpers;

public class ColorHelperTests
{
    [Fact]
    public void ColorRoundTrip_PreservesAllChannels()
    {
        // The conversion uses byte channels on both sides, so any input that fits
        // in a System.Drawing.Color should round-trip exactly through RGB.NET.
        var original = DrawingColor.FromArgb(255, 128, 64, 32);
        var roundTripped = ColorHelper.RGBColorToColor(ColorHelper.ColorToRGBColor(original));

        Assert.Equal(original.A, roundTripped.A);
        Assert.Equal(original.R, roundTripped.R);
        Assert.Equal(original.G, roundTripped.G);
        Assert.Equal(original.B, roundTripped.B);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(127, 63, 191)]
    [InlineData(10, 20, 30)]
    public void ColorRoundTrip_VariousChannels_ExactMatch(int r, int g, int b)
    {
        var original = DrawingColor.FromArgb(r, g, b);
        var roundTripped = ColorHelper.RGBColorToColor(ColorHelper.ColorToRGBColor(original));

        Assert.Equal(original.R, roundTripped.R);
        Assert.Equal(original.G, roundTripped.G);
        Assert.Equal(original.B, roundTripped.B);
    }

    [Fact]
    public void ColorToRGBColor_BlackConvertsToBlack()
    {
        var black = ColorHelper.ColorToRGBColor(DrawingColor.Black);
        Assert.Equal(DrawingColor.Black.R, ColorHelper.RGBColorToColor(black).R);
        Assert.Equal(DrawingColor.Black.G, ColorHelper.RGBColorToColor(black).G);
        Assert.Equal(DrawingColor.Black.B, ColorHelper.RGBColorToColor(black).B);
    }
}
