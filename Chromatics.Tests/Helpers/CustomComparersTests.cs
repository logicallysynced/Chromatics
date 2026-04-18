using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class CustomComparersTests
{
    [Theory]
    [InlineData(1, 2, -1)]
    [InlineData(2, 1, 1)]
    [InlineData(5, 5, 0)]
    [InlineData(-1, 0, -1)]
    public void LayerComparer_Compare_FollowsIntegerOrdering(int x, int y, int expected)
    {
        var comparer = new CustomComparers.LayerComparer();
        Assert.Equal(expected, comparer.Compare(x, y));
    }
}
