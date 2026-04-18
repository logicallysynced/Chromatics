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

    // ColorInterpolator round-trip: interpolating at lambda=0 should return the first
    // endpoint and at lambda=1 the second, modulo rounding through HSL conversion.
    [Fact]
    public void ColorInterpolator_AtLambdaZero_ReturnsFirstColor()
    {
        var c1 = DrawingColor.FromArgb(200, 100, 50);
        var c2 = DrawingColor.FromArgb(10, 20, 30);
        var result = ColorInterpolator.InterpolateBetween(c1, c2, 0.0);
        // Allow ±2 per channel for float rounding through HSL.
        Assert.InRange(result.R, c1.R - 2, c1.R + 2);
        Assert.InRange(result.G, c1.G - 2, c1.G + 2);
        Assert.InRange(result.B, c1.B - 2, c1.B + 2);
    }

    [Fact]
    public void ColorInterpolator_AtLambdaOne_ReturnsSecondColor()
    {
        var c1 = DrawingColor.FromArgb(200, 100, 50);
        var c2 = DrawingColor.FromArgb(10, 20, 30);
        var result = ColorInterpolator.InterpolateBetween(c1, c2, 1.0);
        Assert.InRange(result.R, c2.R - 2, c2.R + 2);
        Assert.InRange(result.G, c2.G - 2, c2.G + 2);
        Assert.InRange(result.B, c2.B - 2, c2.B + 2);
    }

    [Fact]
    public void ColorInterpolator_Midpoint_IsNotAnEndpoint()
    {
        var c1 = DrawingColor.FromArgb(0, 0, 0);
        var c2 = DrawingColor.FromArgb(255, 255, 255);
        var result = ColorInterpolator.InterpolateBetween(c1, c2, 0.5);
        // Midpoint through HSL should not be pure black or pure white.
        Assert.NotEqual(c1, result);
        Assert.NotEqual(c2, result);
    }
}
