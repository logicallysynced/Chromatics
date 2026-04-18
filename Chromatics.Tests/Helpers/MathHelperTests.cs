using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class MathHelperTests
{
    [Theory]
    [InlineData(5, 0, 10, 5)]
    [InlineData(-5, 0, 10, 0)]
    [InlineData(15, 0, 10, 10)]
    [InlineData(0, 0, 10, 0)]
    [InlineData(10, 0, 10, 10)]
    public void Clamp_IntegersWithinAndOutsideBounds_ReturnsExpected(int value, int min, int max, int expected)
    {
        Assert.Equal(expected, MathHelper.Clamp(value, min, max));
    }

    [Fact]
    public void Clamp_DoubleWithinBounds_ReturnsValue()
    {
        Assert.Equal(1.5, MathHelper.Clamp(1.5, 0.0, 5.0));
    }

    [Theory]
    [InlineData(50, 100, 50.0)]
    [InlineData(25, 100, 25.0)]
    [InlineData(0, 100, 0.0)]
    [InlineData(100, 100, 100.0)]
    public void CalculatePercentage_ReturnsExpectedRatio(int current, int max, double expected)
    {
        Assert.Equal(expected, MathHelper.CalculatePercentage(current, max), 6);
    }

    [Fact]
    public void LinearInterpolation_MidPoint_ReturnsMidTarget()
    {
        // Halfway between min and max on [0, 100] should map to halfway between target range [0, 200].
        var result = MathHelper.LinearInterpolation.Interpolate(50, 0, 100, 0, 200);
        Assert.Equal(100, result);
    }

    [Fact]
    public void LinearInterpolation_AtLowEndpoint_ReturnsTargetLow()
    {
        var result = MathHelper.LinearInterpolation.Interpolate(0, 0, 100, 10, 20);
        Assert.Equal(10, result);
    }

    [Fact]
    public void LinearInterpolation_AtHighEndpoint_ReturnsTargetHigh()
    {
        var result = MathHelper.LinearInterpolation.Interpolate(100, 0, 100, 10, 20);
        Assert.Equal(20, result);
    }
}
