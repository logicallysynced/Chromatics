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

            var ledSet = new HashSet<LedId>(layer.deviceLeds.Values);
            return device.Where(led => ledSet.Contains(led.Id)).ToArray();
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

            var ledSet = new HashSet<LedId>(baseLayer.deviceLeds.Values);
            return device.Where(led => ledSet.Contains(led.Id)).ToArray();
        }

        internal IRGBDevice GetDevice(IMappingLayer layer)
        {
            return RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == layer.deviceGuid).Value;
        }

        public abstract void Process(IMappingLayer layer);

        public virtual void CleanupLayer(int layerID) { }

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
        private static NoneProcessor _instance;
        private Dictionary<int, HashSet<Led>> _layergroupledcollections = new Dictionary<int, HashSet<Led>>();

        // Private constructor to prevent direct instantiation
        private NoneProcessor() { }

        // Singleton instance access
        public static NoneProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NoneProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (!layer.Enabled) return;

            var ledArray = GetLedArray(layer);

            if (_layergroupledcollections.ContainsKey(layer.layerID))
            {
                var _layergroupledcollection = _layergroupledcollections[layer.layerID];
            }
            else
            {
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
                foreach (var layergroup in _layergroups.Values.SelectMany(lg => lg))
                {
                    layergroup?.Detach();
                }

                _layergroups.Clear();
                _layergroupledcollections.Clear();
            }

            base.Dispose(disposing);
            _instance = null; // Clear the instance to allow re-creation if needed
        }
    }



    // The matching BaseLayerProcessorFactory and DynamicLayerProcessorFactory
    // maps were deleted: nothing called them, and their static initialisers
    // eagerly built every processor singleton while the entries drifted out
    // of date (missing AudioVisualizer, JobGaugeC, the focus-target layers).
    // GameController dispatches base + dynamic layers through
    // LayerProcessorFactory.GetProcessor instead. EffectLayerProcessorFactory
    // stays - GameController iterates its map for effect-layer dispatch.
    public static class EffectLayerProcessorFactory
    {
        private static readonly Dictionary<EffectLayerType, LayerProcessor> layerProcessors = new Dictionary<EffectLayerType, LayerProcessor>
        {
            { EffectLayerType.None, NoneProcessor.Instance },
            { EffectLayerType.DutyFinderBell, DutyFinderBellProcessor.Instance },
            { EffectLayerType.DamageFlash, DamageFlashProcessor.Instance },
            { EffectLayerType.GoldSaucerVegas, GoldSaucerVegasProcessor.Instance },
            { EffectLayerType.CutsceneAnimation, CutsceneAnimationProcessor.Instance }
        };

        public static Dictionary<EffectLayerType, LayerProcessor> GetProcessors()
        {
            return layerProcessors;
        }
    }


}
