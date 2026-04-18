using Chromatics.Enums;
using Chromatics.Layers;
using Chromatics.ViewModels.Mapping;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Tests.Mapping;

[Collection("MappingLayers")]
public class LayerItemViewModelTests : IDisposable
{
    public LayerItemViewModelTests() => ClearAllLayers();
    public void Dispose() => ClearAllLayers();

    private static void ClearAllLayers()
    {
        var store = MappingLayers.GetLayers();
        foreach (var id in store.Keys.ToList()) MappingLayers.RemoveLayer(id);
    }

    private static Layer SeedLayer(LayerType type = LayerType.DynamicLayer)
    {
        int id = MappingLayers.AddLayer(0, type, Guid.NewGuid(), RGBDeviceType.Keyboard,
            0, 1, false, new Dictionary<int, LedId>(), false, LayerModes.Interpolate);
        return MappingLayers.GetLayer(id);
    }

    [Fact]
    public void SettingIsEnabled_PersistsToMappingLayersStore()
    {
        var layer = SeedLayer();
        var vm = new LayerItemViewModel(layer, _ => { }, _ => { }, _ => { });

        vm.IsEnabled = true;

        var reloaded = MappingLayers.GetLayer(layer.layerID);
        Assert.True(reloaded.Enabled);
        Assert.True(reloaded.requestUpdate);
    }

    [Fact]
    public void SettingMode_PersistsToMappingLayersStore()
    {
        var layer = SeedLayer();
        var vm = new LayerItemViewModel(layer, _ => { }, _ => { }, _ => { });

        vm.Mode = LayerModes.Fade;

        Assert.Equal(LayerModes.Fade, MappingLayers.GetLayer(layer.layerID).layerModes);
    }

    [Fact]
    public void TypeOptions_BaseLayer_MatchesBaseLayerTypeEnum()
    {
        var layer = SeedLayer(LayerType.BaseLayer);
        var vm = new LayerItemViewModel(layer, _ => { }, _ => { }, _ => { });

        // BaseLayerType has Static, ReactiveWeather, BattleStance, JobClasses = 4 entries.
        Assert.Equal(Enum.GetValues<BaseLayerType>().Length, vm.TypeOptions.Count);
    }

    [Fact]
    public void DeleteCommand_InvokesCallbackWithLayerId()
    {
        var layer = SeedLayer();
        int? captured = null;
        var vm = new LayerItemViewModel(layer, _ => { }, _ => { }, id => captured = id);

        vm.DeleteCommand.Execute(null);

        Assert.Equal(layer.layerID, captured);
    }

    [Fact]
    public void Constructor_InitialMutationsInConstructor_DoNotTriggerMappingLayerUpdate()
    {
        var layer = SeedLayer();
        layer.requestUpdate = false;
        MappingLayers.UpdateLayer(layer);

        _ = new LayerItemViewModel(layer, _ => { }, _ => { }, _ => { });

        // Constructor pre-seeds the observable fields. It must not mark the
        // layer dirty, otherwise SaveMappings would churn on every refresh.
        Assert.False(MappingLayers.GetLayer(layer.layerID).requestUpdate);
    }
}
