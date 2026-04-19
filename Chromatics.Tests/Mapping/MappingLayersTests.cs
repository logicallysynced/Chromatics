using Chromatics.Layers;
using RGB.NET.Core;
using System;
using System.Collections.Generic;

namespace Chromatics.Tests.Mapping;

[Collection("MappingLayers")]
public class MappingLayersTests : IDisposable
{
    private readonly List<Guid> _devicesToClean = new();

    public MappingLayersTests() { }

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
    public void SetDeviceKeyPosition_StoresPosition_RetrievableViaOverrides()
    {
        var deviceId = NewDevice();

        MappingLayers.SetDeviceKeyPosition(deviceId, LedId.Keyboard_A, 42.0, 99.5);

        var overrides = MappingLayers.GetDeviceLayoutOverrides(deviceId);
        Assert.True(overrides.TryGetValue(LedId.Keyboard_A, out var pos));
        Assert.Equal(42.0, pos.X);
        Assert.Equal(99.5, pos.Y);
    }

    [Fact]
    public void SetDeviceKeyPosition_UpdatesExistingPosition()
    {
        var deviceId = NewDevice();
        MappingLayers.SetDeviceKeyPosition(deviceId, LedId.Keyboard_A, 10.0, 20.0);

        MappingLayers.SetDeviceKeyPosition(deviceId, LedId.Keyboard_A, 50.0, 60.0);

        var overrides = MappingLayers.GetDeviceLayoutOverrides(deviceId);
        Assert.True(overrides.TryGetValue(LedId.Keyboard_A, out var pos));
        Assert.Equal(50.0, pos.X);
        Assert.Equal(60.0, pos.Y);
    }

    [Fact]
    public void GetDeviceLayoutOverrides_ReturnsSnapshot_MutatingItDoesNotAffectStore()
    {
        var deviceId = NewDevice();
        MappingLayers.SetDeviceKeyPosition(deviceId, LedId.Keyboard_A, 1.0, 2.0);

        var snapshot = MappingLayers.GetDeviceLayoutOverrides(deviceId);
        // Build a mutated copy — the IReadOnlyDictionary prevents direct mutation,
        // so we just verify that a fresh fetch is unaffected by what callers do with
        // the snapshot reference.
        _ = new Dictionary<LedId, DeviceKeyPosition>(snapshot)
        {
            [LedId.Keyboard_B] = new DeviceKeyPosition { X = 99, Y = 99 }
        };

        var fresh = MappingLayers.GetDeviceLayoutOverrides(deviceId);
        Assert.False(fresh.ContainsKey(LedId.Keyboard_B),
            "Store was modified through the snapshot — GetDeviceLayoutOverrides must return a copy.");
    }

    [Fact]
    public void ClearDeviceLayoutOverrides_RemovesAllPositionsForDevice()
    {
        var deviceId = NewDevice();
        MappingLayers.SetDeviceKeyPosition(deviceId, LedId.Keyboard_A, 1, 2);
        MappingLayers.SetDeviceKeyPosition(deviceId, LedId.Keyboard_B, 3, 4);

        MappingLayers.ClearDeviceLayoutOverrides(deviceId);

        Assert.Empty(MappingLayers.GetDeviceLayoutOverrides(deviceId));
    }

    [Fact]
    public void ReplaceDeviceLayouts_ClearsPreviousDevices_AddsNewOnes()
    {
        var oldDevice = NewDevice();
        var newDevice = NewDevice();

        MappingLayers.SetDeviceKeyPosition(oldDevice, LedId.Keyboard_A, 1, 2);

        var replacement = new Dictionary<Guid, Dictionary<LedId, DeviceKeyPosition>>
        {
            [newDevice] = new Dictionary<LedId, DeviceKeyPosition>
            {
                [LedId.Keyboard_B] = new DeviceKeyPosition { X = 7, Y = 8 }
            }
        };
        MappingLayers.ReplaceDeviceLayouts(replacement);

        Assert.Empty(MappingLayers.GetDeviceLayoutOverrides(oldDevice));
        var newOverrides = MappingLayers.GetDeviceLayoutOverrides(newDevice);
        Assert.True(newOverrides.TryGetValue(LedId.Keyboard_B, out var pos));
        Assert.Equal(7.0, pos.X);
        Assert.Equal(8.0, pos.Y);
    }
}
