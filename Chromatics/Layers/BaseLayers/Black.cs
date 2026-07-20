using Chromatics.Core;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Layers
{
    // Base layer that holds every key at black. StaticProcessor with the
    // palette read removed: black is the point of the layer, so the colour
    // never varies and the disabled state paints the same thing.
    public class BlackBaseProcessor : LayerProcessor
    {
        private static BlackBaseProcessor _instance;
        private bool _disposed = false;

        private static readonly Color BlackColor = new Color(0, 0, 0);

        private BlackBaseProcessor() { }

        public static BlackBaseProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BlackBaseProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            if (RGBController.IsBaseLayerEffectRunning()) return;

            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedArray(layer);

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
                    ZIndex = layer.zindex,
                };

                var lg = new ListLedGroup[] { layergroup };
                _layergroups[layer.layerID] = lg;
            }

            foreach (var led in layergroup)
            {
                if (led.Color != BlackColor)
                {
                    led.Color = BlackColor;
                }
            }

            var brush = new SolidColorBrush(BlackColor);
            layergroup.Brush = brush;
            _init = true;
            layer.requestUpdate = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    var _layergroups = RGBController.GetLiveLayerGroups();
                    foreach (var layergroup in _layergroups.Values.SelectMany(lg => lg))
                    {
                        layergroup?.Detach();
                    }
                    _layergroups.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }
    }
}
