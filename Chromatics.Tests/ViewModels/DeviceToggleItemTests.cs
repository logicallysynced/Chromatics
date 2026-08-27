using System;
using System.IO;
using System.Threading.Tasks;
using Chromatics.ViewModels;

namespace Chromatics.Tests.ViewModels;

public class DeviceToggleItemTests
{
    // App Control blocking a vendor DLL threw out of the fire-and-forget task
    // the property setter starts, so the finalizer rethrew it as a crash
    // (CHROMATICS-1G / 1H). The toggle has to absorb it and show the real state.
    private static Exception AppControlBlock()
        => new FileLoadException("An Application Control policy has blocked this file. (0x800711C7)");

    [Fact]
    public void EnableFailure_RevertsToOff()
    {
        var item = new DeviceToggleItem("Razer", "tooltip", false,
            () => throw AppControlBlock(),
            () => { });

        item.IsEnabled = true;

        Assert.False(item.IsEnabled);
    }

    [Fact]
    public void DisableFailure_LeavesToggleOn()
    {
        var item = new DeviceToggleItem("OpenRGB", "tooltip", true,
            () => Task.FromResult(true),
            () => throw AppControlBlock());

        item.IsEnabled = false;

        Assert.True(item.IsEnabled);
    }

    [Fact]
    public void EnableVeto_LeavesToggleOff()
    {
        var item = new DeviceToggleItem("Hue", "tooltip", false,
            () => Task.FromResult(false),
            () => { });

        item.IsEnabled = true;

        Assert.False(item.IsEnabled);
    }

    [Fact]
    public void SuccessfulEnable_TurnsToggleOn()
    {
        var item = new DeviceToggleItem("LIFX", "tooltip", false,
            () => Task.FromResult(true),
            () => { });

        item.IsEnabled = true;

        Assert.True(item.IsEnabled);
    }
}
