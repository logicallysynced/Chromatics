using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Layers.BaseLayers;
using Chromatics.Layers.DynamicLayers;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public abstract class LayerProcessor
    {
        public bool _init;

        public abstract void Process(IMappingLayer layer);
    }

    public class NoneProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            //Static Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.BaseColor.Color);
            var _layergroupledcollections = new Dictionary<int, HashSet<Led>>();
            var _layergroups = RGBController.GetLiveLayerGroups();
            HashSet<Led> _layergroupledcollection;

            //loop through all LED's and assign to device layer
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);

            PublicListLedGroup layergroup;

            var ledArray = devices.SelectMany(d => d).Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

            if (_layergroupledcollections.ContainsKey(layer.layerID))
            {
                _layergroupledcollection = _layergroupledcollections[layer.layerID];
            }
            else
            {
                _layergroupledcollection = new HashSet<Led>();
                _layergroupledcollections.Add(layer.layerID, _layergroupledcollection);
            }

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;
                
            }
            else
            {
                layergroup = new PublicListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new PublicListLedGroup[] { layergroup };
                _layergroups.Add(layer.layerID, lg);
            }

            layergroup.Detach();
        }
    }

    public static class BaseLayerProcessorFactory
    {
        private static readonly Dictionary<BaseLayerType, LayerProcessor> layerProcessors = new Dictionary<BaseLayerType, LayerProcessor>
        {
            { BaseLayerType.Static, new StaticProcessor() },
            { BaseLayerType.ReactiveWeather, new ReactiveWeatherProcessor() },
            { BaseLayerType.BattleStance, new BaseBattleStanceProcessor() },
            { BaseLayerType.JobClasses, new JobClassesProcessor() },
            { BaseLayerType.StatusEffects, new StatusEffectsProcessor() },
            { BaseLayerType.ScreenCapture, new ScreenCaptureProcessor() }
        };

        public static Dictionary<BaseLayerType, LayerProcessor> GetProcessors()
        {
            return layerProcessors;
        }
    }

    public static class EffectLayerProcessorFactory
    {
        private static readonly Dictionary<EffectLayerType, LayerProcessor> layerProcessors = new Dictionary<EffectLayerType, LayerProcessor>
        {
            { EffectLayerType.None, new NoneProcessor() },
            { EffectLayerType.DutyFinderBell, new DutyFinderBellProcessor() },
            { EffectLayerType.DamageFlash, new DamageFlashProcessor() }
        };

        public static Dictionary<EffectLayerType, LayerProcessor> GetProcessors()
        {
            return layerProcessors;
        }
    }

    public static class DynamicLayerProcessorFactory
    {
        private static readonly Dictionary<DynamicLayerType, LayerProcessor> layerProcessors = new Dictionary<DynamicLayerType, LayerProcessor>
        {
            { DynamicLayerType.None, new NoneProcessor() },
            { DynamicLayerType.Highlight, new HighlightProcessor() },
            { DynamicLayerType.Keybinds, new KeybindsProcessor() },
            { DynamicLayerType.EnmityTracker, new EnmityTrackerProcessor() },
            { DynamicLayerType.TargetHP, new TargetHPProcessor() },
            { DynamicLayerType.TargetCastbar, new TargetCastbarProcessor() },
            { DynamicLayerType.HPTracker, new HPTrackerProcessor() },
            { DynamicLayerType.MPTracker, new MPTrackerProcessor() },
            { DynamicLayerType.JobGauge, new JobGaugeProcessor() },
            { DynamicLayerType.ExperienceTracker, new ExperienceTrackerProcessor() },
            { DynamicLayerType.BattleStance, new DynamicBattleStanceProcessor() },
            { DynamicLayerType.Castbar, new CastbarProcessor() },
            { DynamicLayerType.JobClassesHighlight, new JobClassesHighlightProcessor() },
            { DynamicLayerType.ReactiveWeatherHighlight, new ReactiveWeatherHighlightProcessor() },
            { DynamicLayerType.StatusEffectHighlight, new StatusEffectHighlightProcessor() }
        };

        public static Dictionary<DynamicLayerType, LayerProcessor> GetProcessors()
        {
            return layerProcessors;
        }
    }


}
