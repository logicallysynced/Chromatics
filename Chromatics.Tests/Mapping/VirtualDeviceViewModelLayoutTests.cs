using Chromatics.Layers;
using Chromatics.Models;
using Chromatics.ViewModels.Mapping;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Tests.Mapping;

// Device-layout override tests for VirtualDeviceViewModel. Separated from the
// main VirtualDeviceViewModelTests class because these tests read and write
// MappingLayers._deviceLayouts and therefore need the shared [Collection]
// serialisation guard to avoid races with other MappingLayers tests.
[Collection("MappingLayers")]
public class VirtualDeviceViewModelLayoutTests : IDisposable
{
    private readonly List<Guid> _devicesToClean = new();

    public VirtualDeviceViewModelLayoutTests() { }

    public void Dispose()
    {
        foreach (var id in _devicesToClean)
            MappingLayers.ClearDeviceLayoutOverrides(id);
    }

    private Guid NewDevice()
    {
        var id = Guid.NewGuid();
        _devicesToClean.Add(id);
        return id;
    }

    [Fact]
    public void BuildFromKeys_NonKeyboard_AppliesDeviceLayoutOverride()
    {
        var deviceId = NewDevice();
        MappingLayers.SetDeviceKeyPosition(deviceId, LedId.Custom1, 200.0, 300.0);

        var keys = new[] { new KeyboardKey("Custom1", LedId.Custom1) };
        var vm = VirtualDeviceViewModel.BuildFromKeys(deviceId, "Mouse", RGBDeviceType.Mouse, keys);

        var keycap = vm.Keycaps.Single(k => k.LedType == LedId.Custom1);
        Assert.Equal(200.0, keycap.X);
        Assert.Equal(300.0, keycap.Y);
    }

    [Fact]
    public void BuildFromKeys_NonKeyboard_DefaultPositionPreservedWhenOverrideApplied()
    {
        var deviceId = NewDevice();
        MappingLayers.SetDeviceKeyPosition(deviceId, LedId.Custom1, 200.0, 300.0);

        var keys = new[] { new KeyboardKey("Custom1", LedId.Custom1) };
        var vm = VirtualDeviceViewModel.BuildFromKeys(deviceId, "Mouse", RGBDeviceType.Mouse, keys);

        var keycap = vm.Keycaps.Single(k => k.LedType == LedId.Custom1);
        // Override moved the key visually, but DefaultX/Y must stay at the grid position
        // so Reset can restore it without rebuilding the whole VM.
        Assert.Equal(0.0, keycap.DefaultX);
        Assert.Equal(0.0, keycap.DefaultY);
    }

    [Fact]
    public void BuildFromKeys_NonKeyboard_NoOverride_PositionEqualsDefault()
    {
        var deviceId = NewDevice(); // no overrides set

        var keys = new[] { new KeyboardKey("Custom1", LedId.Custom1) };
        var vm = VirtualDeviceViewModel.BuildFromKeys(deviceId, "Mouse", RGBDeviceType.Mouse, keys);

        var keycap = vm.Keycaps.Single(k => k.LedType == LedId.Custom1);
        Assert.Equal(keycap.DefaultX, keycap.X);
        Assert.Equal(keycap.DefaultY, keycap.Y);
    }
}
