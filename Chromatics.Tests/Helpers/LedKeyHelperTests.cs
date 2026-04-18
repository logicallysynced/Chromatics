using Chromatics.Helpers;
using RGB.NET.Core;

namespace Chromatics.Tests.Helpers;

public class LedKeyHelperTests
{
    [Theory]
    [InlineData(LedId.Keyboard_Escape, "Escape")]
    [InlineData(LedId.Keyboard_A, "A")]
    [InlineData(LedId.Keyboard_Z, "Z")]
    [InlineData(LedId.Keyboard_Space, "Space")]
    [InlineData(LedId.Keyboard_F1, "F1")]
    [InlineData(LedId.Keyboard_NumLock, "Num Lock")]
    [InlineData(LedId.Keyboard_ArrowUp, "↑")]
    [InlineData(LedId.Keyboard_Macro1, "M1")]
    public void LedIdToHotbarKeyConverter_KnownKeys_ReturnsExpected(LedId led, string expected)
    {
        Assert.Equal(expected, LedKeyHelper.LedIdToHotbarKeyConverter(led));
    }

    [Fact]
    public void LedIdToHotbarKeyConverter_UnknownLed_ReturnsUnknown()
    {
        Assert.Equal("Unknown", LedKeyHelper.LedIdToHotbarKeyConverter(LedId.Invalid));
    }

    [Theory]
    [InlineData("Escape", LedId.Keyboard_Escape)]
    [InlineData("A", LedId.Keyboard_A)]
    [InlineData("Z", LedId.Keyboard_Z)]
    [InlineData("Space", LedId.Keyboard_Space)]
    [InlineData("F1", LedId.Keyboard_F1)]
    [InlineData("Num Lock", LedId.Keyboard_NumLock)]
    [InlineData("↑", LedId.Keyboard_ArrowUp)]
    [InlineData("M1", LedId.Keyboard_Macro1)]
    public void HotbarKeyToLedIdConverter_KnownKeys_ReturnsExpected(string key, LedId expected)
    {
        Assert.Equal(expected, LedKeyHelper.HotbarKeyToLedIdConverter(key));
    }

    [Fact]
    public void HotbarKeyToLedIdConverter_UnknownString_ReturnsUnknown1()
    {
        Assert.Equal(LedId.Unknown1, LedKeyHelper.HotbarKeyToLedIdConverter("not_a_key"));
    }

    [Fact]
    public void RoundTrip_AllKnownKeys_AreInvertible()
    {
        // Every key in the forward map that produces a unique display name
        // should round-trip back to itself via the reverse map.
        var forward = new[]
        {
            LedId.Keyboard_Escape, LedId.Keyboard_F1, LedId.Keyboard_F12,
            LedId.Keyboard_Q, LedId.Keyboard_W, LedId.Keyboard_A,
            LedId.Keyboard_Z, LedId.Keyboard_Space, LedId.Keyboard_Enter,
            LedId.Keyboard_Backspace, LedId.Keyboard_LeftShift,
            LedId.Keyboard_LeftCtrl, LedId.Keyboard_Macro1,
        };

        foreach (var led in forward)
        {
            var name = LedKeyHelper.LedIdToHotbarKeyConverter(led);
            var roundTripped = LedKeyHelper.HotbarKeyToLedIdConverter(name);
            Assert.Equal(led, roundTripped);
        }
    }

    [Fact]
    public void GraveAccent_IsCanonicalForBacktick()
    {
        // GraveAccentAndTilde and NonUsTilde both map to "`".
        // The reverse should resolve to GraveAccentAndTilde (canonical first entry).
        Assert.Equal("`", LedKeyHelper.LedIdToHotbarKeyConverter(LedId.Keyboard_GraveAccentAndTilde));
        Assert.Equal("`", LedKeyHelper.LedIdToHotbarKeyConverter(LedId.Keyboard_NonUsTilde));
        Assert.Equal(LedId.Keyboard_GraveAccentAndTilde, LedKeyHelper.HotbarKeyToLedIdConverter("`"));
    }

    [Fact]
    public void GetAllKeysForDevice_Keyboard_ReturnsNonEmpty()
    {
        var keys = LedKeyHelper.GetAllKeysForDevice(RGBDeviceType.Keyboard);
        Assert.NotNull(keys);
        Assert.NotEmpty(keys);
        Assert.Contains(LedId.Keyboard_A, keys.Values);
    }

    [Fact]
    public void GetAllKeysForDevice_None_ReturnsNull()
    {
        Assert.Null(LedKeyHelper.GetAllKeysForDevice(RGBDeviceType.None));
    }

    [Fact]
    public void GetAllKeysForDevice_Mouse_ReturnsNonEmpty()
    {
        var keys = LedKeyHelper.GetAllKeysForDevice(RGBDeviceType.Mouse);
        Assert.NotNull(keys);
        Assert.NotEmpty(keys);
    }
}
