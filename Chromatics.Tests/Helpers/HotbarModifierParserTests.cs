using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class HotbarModifierParserTests
{
    [Theory]
    // English client.
    [InlineData("Ctrl", true, false, false)]
    [InlineData("Alt", false, true, false)]
    [InlineData("Shift", false, false, true)]
    // German: Strg for Ctrl, Umschalt for Shift.
    [InlineData("Strg", true, false, false)]
    [InlineData("Umschalt", false, false, true)]
    // French: Maj for Shift.
    [InlineData("Maj", false, false, true)]
    public void EachClientSpellingResolves(string name, bool ctrl, bool alt, bool shift)
    {
        var parsed = HotbarModifierParser.Parse([name], out var unrecognised);

        Assert.Equal((ctrl, alt, shift), parsed);
        Assert.Empty(unrecognised);
    }

    [Fact]
    public void FullWidthNamesResolve()
    {
        // A Japanese client can render the hint in full-width characters.
        var parsed = HotbarModifierParser.Parse(["Ｃｔｒｌ", "Ｓｈｉｆｔ"], out var unrecognised);

        Assert.Equal((true, false, true), parsed);
        Assert.Empty(unrecognised);
    }

    [Fact]
    public void CaseAndStraySpacingDoNotMatter()
    {
        var parsed = HotbarModifierParser.Parse([" ctrl ", "ALT", "Maj."], out var unrecognised);

        Assert.Equal((true, true, true), parsed);
        Assert.Empty(unrecognised);
    }

    [Fact]
    public void CombinationsResolveTogether()
    {
        var parsed = HotbarModifierParser.Parse(["Strg", "Umschalt"], out _);

        Assert.Equal((true, false, true), parsed);
    }

    [Fact]
    public void AnUnknownNameIsReportedRatherThanGuessed()
    {
        var parsed = HotbarModifierParser.Parse(["Ctrl", "Kontrol"], out var unrecognised);

        Assert.Equal((true, false, false), parsed);
        Assert.Equal("Kontrol", Assert.Single(unrecognised));
    }

    [Fact]
    public void NothingToParseIsNotAFailure()
    {
        Assert.Equal((false, false, false), HotbarModifierParser.Parse([], out var none));
        Assert.Empty(none);

        Assert.Equal((false, false, false), HotbarModifierParser.Parse(null, out var stillNone));
        Assert.Empty(stillNone);

        Assert.Equal((false, false, false), HotbarModifierParser.Parse(["", "   "], out var blank));
        Assert.Empty(blank);
    }
}
