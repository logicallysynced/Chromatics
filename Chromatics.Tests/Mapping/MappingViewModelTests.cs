using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.ViewModels.Mapping;
using RGB.NET.Core;
using System;
using System.IO;
using System.Linq;

namespace Chromatics.Tests.Mapping;

// MappingLayers is a process-global static store. These tests wipe it at
// construction so they can run in any order without interference, but the
// class as a whole is [Collection("MappingLayers")] so xUnit serializes
// across test classes that also touch it.
[Collection("MappingLayers")]
public class MappingViewModelTests : IDisposable
{
    public MappingViewModelTests()
    {
        ClearAllLayers();
    }

    public void Dispose()
    {
        ClearAllLayers();
    }

    private static void ClearAllLayers()
    {
        var store = MappingLayers.GetLayers();
        foreach (var id in store.Keys.ToList()) MappingLayers.RemoveLayer(id);
    }

    private static MappingViewModel NewVmWithSelectedDevice(Guid deviceId)
    {
        var vm = new MappingViewModel();
        vm.Devices.Add(new DeviceOptionItem(deviceId, "Test Device", RGBDeviceType.Keyboard));
        vm.SelectedDevice = vm.Devices[0];
        return vm;
    }

    [Fact]
    public void AddDynamicLayer_WithSelectedDevice_AppendsLayerAndBumpsZIndex()
    {
        var deviceId = Guid.NewGuid();
        using var vm = NewVmWithSelectedDevice(deviceId);

        int firstId  = vm.AddDynamicLayer();
        int secondId = vm.AddDynamicLayer();

        Assert.NotEqual(-1, firstId);
        Assert.NotEqual(-1, secondId);
        Assert.Equal(2, vm.Layers.Count);
        // Top of list = highest zindex. Newest layer was inserted above the
        // (absent) Base pin, so it ends up at index 0 with the larger z.
        Assert.Equal(2, vm.Layers[0].ZIndex);
        Assert.Equal(1, vm.Layers[1].ZIndex);
        Assert.All(vm.Layers, l => Assert.Equal(LayerType.DynamicLayer, l.RootLayerType));
    }

    [Fact]
    public void AddDynamicLayer_WithoutSelectedDevice_ReturnsMinusOneAndDoesNothing()
    {
        using var vm = new MappingViewModel();

        int result = vm.AddDynamicLayer();

        Assert.Equal(-1, result);
        Assert.Empty(vm.Layers);
    }

    [Fact]
    public void RemoveLayer_RemovesFromCollectionAndStoreAndRenumbers()
    {
        var deviceId = Guid.NewGuid();
        using var vm = NewVmWithSelectedDevice(deviceId);
        int a = vm.AddDynamicLayer();
        int b = vm.AddDynamicLayer();
        int c = vm.AddDynamicLayer();

        vm.RemoveLayer(b);

        Assert.Equal(2, vm.Layers.Count);
        Assert.DoesNotContain(vm.Layers, l => l.LayerId == b);
        Assert.Null(MappingLayers.GetLayer(b));

        // Remaining layers get renumbered so zindex stays contiguous, with the
        // top-of-list slot holding the highest z.
        Assert.Equal(2, vm.Layers[0].ZIndex);
        Assert.Equal(1, vm.Layers[1].ZIndex);
    }

    [Fact]
    public void MoveLayer_ReordersCollectionAndRewritesZIndexInStore()
    {
        var deviceId = Guid.NewGuid();
        using var vm = NewVmWithSelectedDevice(deviceId);
        int a = vm.AddDynamicLayer();
        int b = vm.AddDynamicLayer();
        int c = vm.AddDynamicLayer();

        // After the three inserts the list reads top→bottom as [c, b, a]
        // (newest on top). Drag the top entry down to the bottom.
        vm.MoveLayer(0, 2);

        Assert.Equal(new[] { b, a, c },
            vm.Layers.Select(l => l.LayerId).ToArray());

        // Top of list holds the highest z; persistence mirrors the display.
        Assert.Equal(3, MappingLayers.GetLayer(b).zindex);
        Assert.Equal(2, MappingLayers.GetLayer(a).zindex);
        Assert.Equal(1, MappingLayers.GetLayer(c).zindex);
    }

    [Fact]
    public void MoveLayer_WithOutOfRangeIndex_IsNoOp()
    {
        var deviceId = Guid.NewGuid();
        using var vm = NewVmWithSelectedDevice(deviceId);
        vm.AddDynamicLayer();
        vm.AddDynamicLayer();

        vm.MoveLayer(-1, 0);
        vm.MoveLayer(0, 99);

        Assert.Equal(2, vm.Layers[0].ZIndex);
        Assert.Equal(1, vm.Layers[1].ZIndex);
    }

    [Fact]
    public void DuplicateLayer_CopiesLedsIntoIndependentDictionary()
    {
        var deviceId = Guid.NewGuid();
        using var vm = NewVmWithSelectedDevice(deviceId);
        int id = vm.AddDynamicLayer();
        var source = MappingLayers.GetLayer(id);
        source.deviceLeds[0] = LedId.Keyboard_A;
        source.deviceLeds[1] = LedId.Keyboard_B;
        MappingLayers.UpdateLayer(source);

        int copyId = vm.DuplicateLayer(id);
        var copy = MappingLayers.GetLayer(copyId);

        Assert.NotEqual(id, copyId);
        Assert.Equal(2, copy.deviceLeds.Count);
        Assert.NotSame(source.deviceLeds, copy.deviceLeds);

        copy.deviceLeds[2] = LedId.Keyboard_C;
        Assert.DoesNotContain(2, source.deviceLeds.Keys);
    }

    [Fact]
    public void SelectedDevice_ChangingScopesLayersToThatDevice()
    {
        var deviceA = Guid.NewGuid();
        var deviceB = Guid.NewGuid();

        using var vm = NewVmWithSelectedDevice(deviceA);
        vm.AddDynamicLayer();
        vm.AddDynamicLayer();

        var optionB = new DeviceOptionItem(deviceB, "Other", RGBDeviceType.Keyboard);
        vm.Devices.Add(optionB);
        vm.SelectedDevice = optionB;

        // No layers yet for device B.
        Assert.Empty(vm.Layers);

        vm.AddDynamicLayer();
        Assert.Single(vm.Layers);

        // Flip back — the original two layers are still there.
        vm.SelectedDevice = vm.Devices[0];
        Assert.Equal(2, vm.Layers.Count);
    }

    [Fact]
    public void ApplyKeyboardLayoutChange_QwertyToQwertz_SwapsYAndZLeds()
    {
        var deviceId = Guid.NewGuid();
        using var vm = NewVmWithSelectedDevice(deviceId);
        int id = vm.AddDynamicLayer();

        var layer = MappingLayers.GetLayer(id);
        layer.deviceLeds[0] = LedId.Keyboard_Y;
        layer.deviceLeds[1] = LedId.Keyboard_A;
        MappingLayers.UpdateLayer(layer);

        vm.ApplyKeyboardLayoutChange(KeyboardLocalization.qwerty, KeyboardLocalization.qwertz);

        var after = MappingLayers.GetLayer(id);
        Assert.Equal(LedId.Keyboard_Z, after.deviceLeds[0]);
        Assert.Equal(LedId.Keyboard_A, after.deviceLeds[1]);
    }

    [Fact]
    public void RemoveLayer_BaseLayerType_IsNoOp()
    {
        var deviceId = Guid.NewGuid();
        int baseId = MappingLayers.AddLayer(0, LayerType.BaseLayer, deviceId, RGBDeviceType.Keyboard,
            0, 1, true, new System.Collections.Generic.Dictionary<int, LedId>(), false, LayerModes.None);

        using var vm = new MappingViewModel();
        vm.Devices.Add(new DeviceOptionItem(deviceId, "Test", RGBDeviceType.Keyboard));
        vm.SelectedDevice = vm.Devices[0];

        int countBefore = vm.Layers.Count;
        vm.RemoveLayer(baseId);

        Assert.Equal(countBefore, vm.Layers.Count);
        Assert.NotNull(MappingLayers.GetLayer(baseId));
    }

    [Fact]
    public void RemoveLayer_EffectLayerType_IsNoOp()
    {
        var deviceId = Guid.NewGuid();
        int effectId = MappingLayers.AddLayer(0, LayerType.EffectLayer, deviceId, RGBDeviceType.Keyboard,
            0, 2, true, new System.Collections.Generic.Dictionary<int, LedId>(), false, LayerModes.None);

        using var vm = new MappingViewModel();
        vm.Devices.Add(new DeviceOptionItem(deviceId, "Test", RGBDeviceType.Keyboard));
        vm.SelectedDevice = vm.Devices[0];

        int countBefore = vm.Layers.Count;
        vm.RemoveLayer(effectId);

        Assert.Equal(countBefore, vm.Layers.Count);
        Assert.NotNull(MappingLayers.GetLayer(effectId));
    }

    [Fact]
    public void DuplicateLayer_LayerBelongsToOtherDevice_NotInsertedIntoActiveList()
    {
        var deviceA = Guid.NewGuid();
        var deviceB = Guid.NewGuid();

        int layerOnA = MappingLayers.AddLayer(0, LayerType.DynamicLayer, deviceA, RGBDeviceType.Keyboard,
            0, 1, true, new System.Collections.Generic.Dictionary<int, LedId>(), false, LayerModes.Interpolate);

        using var vm = new MappingViewModel();
        vm.Devices.Add(new DeviceOptionItem(deviceA, "Device A", RGBDeviceType.Keyboard));
        vm.Devices.Add(new DeviceOptionItem(deviceB, "Device B", RGBDeviceType.Keyboard));
        vm.SelectedDevice = vm.Devices[1]; // select device B — empty

        int copyId = vm.DuplicateLayer(layerOnA);

        // Duplicate belongs to device A; active list shows device B's layers only.
        Assert.Empty(vm.Layers);
        Assert.NotEqual(-1, copyId);
        Assert.NotNull(MappingLayers.GetLayer(copyId));
        Assert.Equal(deviceA, MappingLayers.GetLayer(copyId).deviceGuid);
    }
}

// Redirects FileOperationsHelper.GetConfigDirectory() to a temp path for the
// lifetime of the test collection. Without this, SaveMappings() calls inside
// MappingViewModel tests write to the real %AppData%\Chromatics folder —
// clobbering the developer's actual layer/palette files whenever `dotnet test`
// runs (e.g. triggering default-layer re-seeding on the next app launch).
public sealed class ConfigDirectoryRedirectFixture : IDisposable
{
    public string TempDir { get; }

    public ConfigDirectoryRedirectFixture()
    {
        TempDir = Path.Combine(Path.GetTempPath(), "chromatics-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(TempDir);
        FileOperationsHelper.SetConfigDirectoryOverride(TempDir);
    }

    public void Dispose()
    {
        FileOperationsHelper.SetConfigDirectoryOverride(null);
        try { if (Directory.Exists(TempDir)) Directory.Delete(TempDir, recursive: true); } catch { }
    }
}

[CollectionDefinition("MappingLayers")]
public class MappingLayersCollection : ICollectionFixture<ConfigDirectoryRedirectFixture> { }
