using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class HotbarModifierParserTests
{
    // The exact strings the game ships, from Addon sheet rows 7915 / 7916 / 7917.
    [Theory]
    // en, and the same for ja / chs / tc / ko.
    [InlineData("Ctrl", true, false, false)]
    [InlineData("Alt", false, true, false)]
    [InlineData("Shift", false, false, true)]
    // de. The sheet abbreviates Shift to "Umsch", which an earlier guess of
    // "Umschalt" would have missed, leaving German keybinds dark.
    [InlineData("Strg", true, false, false)]
    [InlineData("Umsch", false, false, true)]
    // fr, including the trailing period the sheet carries.
    [InlineData("Maj.", false, false, true)]
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
        // "Strg+Umsch+1" on a German client.
        Assert.Equal((true, false, true), HotbarModifierParser.Parse(["Strg", "Umsch"], out _));

        // "Ctrl+Alt+Maj.+1" on a French client.
        Assert.Equal((true, true, true), HotbarModifierParser.Parse(["Ctrl", "Alt", "Maj."], out _));
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
