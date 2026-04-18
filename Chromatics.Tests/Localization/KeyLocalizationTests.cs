using Chromatics.Enums;
using Chromatics.Localization;

namespace Chromatics.Tests.Localization;

public class KeyLocalizationTests
{
    [Theory]
    [InlineData(KeyboardLocalization.qwerty)]
    [InlineData(KeyboardLocalization.qwertz)]
    [InlineData(KeyboardLocalization.azerty)]
    public void GetLocalizedKeys_AllDefinedLocales_ReturnNonEmptyLayout(KeyboardLocalization locale)
    {
        var keys = KeyLocalization.GetLocalizedKeys(locale);

        Assert.NotNull(keys);
        Assert.NotEmpty(keys);
    }

    [Fact]
    public void GetLocalizedKeys_UnknownLocale_FallsBackToQwerty()
    {
        var qwerty = KeyLocalization.GetLocalizedKeys(KeyboardLocalization.qwerty);
        var fallback = KeyLocalization.GetLocalizedKeys((KeyboardLocalization)999);

        Assert.Same(qwerty, fallback);
    }

    [Fact]
    public void GetLocalizedKeys_Qwerty_ContainsNoDuplicateLedIds()
    {
        // Regression guard: a duplicate LedId in the layout would mean two keycaps
        // light the same LED, which would look "stuck" during input-driven effects.
        var keys = KeyLocalization.GetLocalizedKeys(KeyboardLocalization.qwerty);
        var ledIds = keys.Select(k => k.LedType).ToList();

        Assert.Equal(ledIds.Count, ledIds.Distinct().Count());
    }
}
