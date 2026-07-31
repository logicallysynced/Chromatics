using Chromatics.Core;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Layers
{
    // Dynamic layer that holds the assigned keys at black. HighlightProcessor
    // with the palette read removed: useful for masking keys that a lower
    // layer would otherwise light.
    public class BlackProcessor : LayerProcessor
    {
        private static BlackProcessor _instance;
        private bool _disposed = false;
        private readonly Dictionary<int, SolidColorBrush> _brushCache = new Dictionary<int, SolidColorBrush>();

        private static readonly Color BlackColor = new Color(0, 0, 0);

        private BlackProcessor() { }

        public static BlackProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BlackProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedArray(layer);
            ListLedGroup updatedLayerGroup;

            if (_layergroups.ContainsKey(layer.layerID))
            {
                updatedLayerGroup = _layergroups[layer.layerID].FirstOrDefault();

                if (layer.requestUpdate)
                {
                    updatedLayerGroup.RemoveLeds(updatedLayerGroup);
                    updatedLayerGroup.AddLeds(ledArray);
                }
            }
            else
            {
                updatedLayerGroup = new ListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new ListLedGroup[] { updatedLayerGroup };
                _layergroups[layer.layerID] = lg;
                updatedLayerGroup.Detach();
            }

            if (!layer.Enabled)
            {
                updatedLayerGroup.Detach();
                return;
            }

            foreach (var led in updatedLayerGroup)
            {
                if (led.Color != BlackColor)
                {
                    led.Color = BlackColor;
                }
            }

            if (!_brushCache.TryGetValue(layer.layerID, out var brush))
            {
                brush = new SolidColorBrush(BlackColor);
                _brushCache[layer.layerID] = brush;
            }
            updatedLayerGroup.Brush = brush;
            updatedLayerGroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _brushCache.Clear();
                    var _layergroups = RGBController.GetLiveLayerGroups();
                    if (_layergroups != null)
                    {
                        foreach (var layerGroupArray in _layergroups.Values)
                        {
                            foreach (var layerGroup in layerGroupArray)
                            {
                                layerGroup?.Detach();
                            }
                        }
                        _layergroups.Clear();
                    }
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }
    }
}
