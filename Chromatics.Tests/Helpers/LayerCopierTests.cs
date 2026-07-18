using Chromatics.Helpers;
using RGB.NET.Core;

namespace Chromatics.Tests.Helpers;

// Pins the default LedId mapping used when copying layers from a device
// that is disabled or no longer connected: the layer's own LedId set
// stands in for the missing device, so these rules decide where each key
// lands on the destination before the user touches the override dropdowns.
public class LayerCopierTests
{
    [Fact]
    public void KeyboardPair_MapsIdentity_OnlyWhereDestinationHasTheKey()
    {
        var used = new[] { LedId.Keyboard_A, LedId.Keyboard_B, LedId.Keyboard_NumLock };
        var destIds = new List<LedId> { LedId.Keyboard_A, LedId.Keyboard_B }; // no numpad

        var map = LayerCopier.ComputeDefaultMappingForLayer(
            used, RGBDeviceType.Keyboard, RGBDeviceType.Keyboard, destIds);

        Assert.Equal(LedId.Keyboard_A, map[LedId.Keyboard_A]);
        Assert.Equal(LedId.Keyboard_B, map[LedId.Keyboard_B]);
        // Keyboard pairs never fall back positionally: a missing key is
        // dropped so it can't land on an unrelated physical key.
        Assert.False(map.ContainsKey(LedId.Keyboard_NumLock));
    }

    [Fact]
    public void NonKeyboard_ExactIdMatchWins()
    {
        var used = new[] { LedId.Custom1, LedId.Custom2 };
        var destIds = new List<LedId> { LedId.Custom1, LedId.Custom2, LedId.Custom3 };

        var map = LayerCopier.ComputeDefaultMappingForLayer(
            used, RGBDeviceType.LedStripe, RGBDeviceType.LedStripe, destIds);

        Assert.Equal(LedId.Custom1, map[LedId.Custom1]);
        Assert.Equal(LedId.Custom2, map[LedId.Custom2]);
    }

    [Fact]
    public void NonKeyboard_OrdinalFallback_WhenIdsDoNotOverlap()
    {
        // Source painted Custom5..Custom7; destination only has Custom1..Custom3.
        var used = new[] { LedId.Custom5, LedId.Custom6, LedId.Custom7 };
        var destIds = new List<LedId> { LedId.Custom1, LedId.Custom2, LedId.Custom3 };

        var map = LayerCopier.ComputeDefaultMappingForLayer(
            used, RGBDeviceType.LedStripe, RGBDeviceType.Mouse, destIds);

        Assert.Equal(LedId.Custom1, map[LedId.Custom5]);
        Assert.Equal(LedId.Custom2, map[LedId.Custom6]);
        Assert.Equal(LedId.Custom3, map[LedId.Custom7]);
    }

    [Fact]
    public void NonKeyboard_SourceLargerThanDestination_DropsTheTail()
    {
        var used = new[] { LedId.Custom5, LedId.Custom6, LedId.Custom7 };
        var destIds = new List<LedId> { LedId.Custom1 };

        var map = LayerCopier.ComputeDefaultMappingForLayer(
            used, RGBDeviceType.LedStripe, RGBDeviceType.LedStripe, destIds);

        Assert.Equal(LedId.Custom1, map[LedId.Custom5]);
        Assert.False(map.ContainsKey(LedId.Custom6));
        Assert.False(map.ContainsKey(LedId.Custom7));
    }

    [Fact]
    public void EmptyOrMissingInputs_ReturnEmptyMap()
    {
        Assert.Empty(LayerCopier.ComputeDefaultMappingForLayer(
            Array.Empty<LedId>(), RGBDeviceType.Keyboard, RGBDeviceType.Keyboard, new List<LedId> { LedId.Keyboard_A }));
        Assert.Empty(LayerCopier.ComputeDefaultMappingForLayer(
            null, RGBDeviceType.Keyboard, RGBDeviceType.Keyboard, new List<LedId>()));
        Assert.Empty(LayerCopier.ComputeDefaultMappingForLayer(
            new[] { LedId.Keyboard_A }, RGBDeviceType.Keyboard, RGBDeviceType.Keyboard, null));
    }
}
