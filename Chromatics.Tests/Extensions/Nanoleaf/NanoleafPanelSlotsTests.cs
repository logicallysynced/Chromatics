using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf;

namespace Chromatics.Tests.Extensions.Nanoleaf;

// The slot table maps panelId -> LedId.Custom slot, and layer assignments
// reference LedIds, so the resolver's one job is keeping slots stable while
// panels come and go from the wall.
public class NanoleafPanelSlotsTests
{
    [Fact]
    public void FreshAdoption_AssignsSlotsInAscendingPanelIdOrder()
    {
        var (slots, added, missing) = NanoleafPanelSlots.Resolve(null, new[] { 30, 10, 20 });

        Assert.Equal(new[] { 10, 20, 30 }, slots);
        Assert.Equal(new[] { 10, 20, 30 }, added);
        Assert.Empty(missing);
    }

    [Fact]
    public void RemovedPanel_KeepsItsSlot_SoLaterPanelsDoNotShift()
    {
        var stored = new[] { 10, 20, 30 };

        var (slots, added, missing) = NanoleafPanelSlots.Resolve(stored, new[] { 10, 30 });

        // Panel 20 is gone but its slot survives as a tombstone: panel 30
        // stays at slot 2 (LedId.Custom3), keeping its layer assignments.
        Assert.Equal(new[] { 10, 20, 30 }, slots);
        Assert.Empty(added);
        Assert.Equal(new[] { 20 }, missing);
    }

    [Fact]
    public void AddedPanel_AppendsAtTheEnd()
    {
        var stored = new[] { 10, 20, 30 };

        var (slots, added, missing) = NanoleafPanelSlots.Resolve(stored, new[] { 10, 20, 30, 5, 40 });

        // New panels never displace existing slots, even when their
        // panelIds sort below existing ones.
        Assert.Equal(new[] { 10, 20, 30, 5, 40 }, slots);
        Assert.Equal(new[] { 5, 40 }, added);
        Assert.Empty(missing);
    }

    [Fact]
    public void ReturningPanel_LandsBackOnItsOldSlot()
    {
        var stored = new[] { 10, 20, 30 };

        // Session with panel 20 absent - tombstone holds slot 1.
        var (afterRemoval, _, _) = NanoleafPanelSlots.Resolve(stored, new[] { 10, 30 });
        // Panel 20 re-attached: no additions, no missing, same table.
        var (afterReturn, added, missing) = NanoleafPanelSlots.Resolve(afterRemoval, new[] { 10, 20, 30 });

        Assert.Equal(new[] { 10, 20, 30 }, afterReturn);
        Assert.Empty(added);
        Assert.Empty(missing);
    }
}
