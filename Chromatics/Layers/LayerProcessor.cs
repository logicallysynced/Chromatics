using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Layers.DynamicLayers;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public abstract class LayerProcessor : IDisposable
    {
        internal bool _init;
        internal RGBSurface surface = RGBController.GetLiveSurfaces();
        private bool _disposed;

        // Declare _layergroups dictionary to store the layer groups
        protected Dictionary<int, ListLedGroup[]> _layergroups = new Dictionary<int, ListLedGroup[]>();

        internal Led[] GetLedArray(IMappingLayer layer)
        {
            var device = GetDevice(layer);

            if (device == null)
            {
                return Array.Empty<Led>();
            }

            return device.Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();
        }

        internal Led[] GetLedSortedArray(IMappingLayer layer)
        {
            var device = GetDevice(layer);

            if (device == null)
            {
                return Array.Empty<Led>();
            }

            return (from led in device.Select((led, index) => new { Index = index, Led = led })
                    join id in layer.deviceLeds.Values.Select((id, index) => new { Index = index, Id = id })
                    on led.Led.Id equals id.Id
                    orderby id.Index
                    select led.Led).ToArray();
        }

        internal Led[] GetLedBaseArray(IMappingLayer layer, Layer baseLayer)
        {
            var device = GetDevice(layer);

            if (device == null)
            {
                return Array.Empty<Led>();
            }

            return device.Where(led => baseLayer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();
        }

        internal IRGBDevice GetDevice(IMappingLayer layer)
        {
            return RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == layer.deviceGuid).Value;
        }

        public abstract void Process(IMappingLayer layer);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    surface = null;
                    _layergroups.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }


    public class NoneProcessor : LayerProcessor
    {
        private Dictionary<int, HashSet<Led>> _layergroupledcollections = new Dictionary<int, HashSet<Led>>();

        public override void Process(IMappingLayer layer)
        {
            // Avoid processing if the layer is not enabled
            if (!layer.Enabled) return;

            var ledArray = GetLedArray(layer);

            if (_layergroupledcollections.ContainsKey(layer.layerID))
            {
                // Reuse the existing LED collection
                var _layergroupledcollection = _layergroupledcollections[layer.layerID];
            }
            else
            {
                // Create a new collection if it doesn't exist
                var _layergroupledcollection = new HashSet<Led>(ledArray);
                _layergroupledcollections.Add(layer.layerID, _layergroupledcollection);
            }

            ListLedGroup layergroup;
            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;
            }
            else
            {
                layergroup = new ListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex
                };

                _layergroups.Add(layer.layerID, new[] { layergroup });
            }

            layergroup.Detach();

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Detach all groups and dispose of resources
                foreach (var layergroup in _layergroups.Values.SelectMany(lg => lg))
                {
                    layergroup?.Detach();
                }

                _layergroups.Clear();
                _layergroupledcollections.Clear();
            }

            base.Dispose(disposing);
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
            //{ BaseLayerType.ScreenCapture, new ScreenCaptureProcessor() }
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
            { EffectLayerType.DamageFlash, new DamageFlashProcessor() },
            { EffectLayerType.GoldSaucerVegas, new GoldSaucerVegasProcessor() },
            { EffectLayerType.CutsceneAnimation, new CutsceneAnimationProcessor() }
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
            { DynamicLayerType.JobGaugeA, new JobGaugeAProcessor() },
            { DynamicLayerType.JobGaugeB, new JobGaugeBProcessor() },
            { DynamicLayerType.ExperienceTracker, new ExperienceTrackerProcessor() },
            { DynamicLayerType.BattleStance, new DynamicBattleStanceProcessor() },
            { DynamicLayerType.Castbar, new CastbarProcessor() },
            { DynamicLayerType.JobClassesHighlight, new JobClassesHighlightProcessor() },
            { DynamicLayerType.ReactiveWeatherHighlight, new ReactiveWeatherHighlightProcessor() }
        };

        public static Dictionary<DynamicLayerType, LayerProcessor> GetProcessors()
        {
            return layerProcessors;
        }
    }


}
