using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Layers
{
    public class HighlightProcessor : LayerProcessor
    {
        private bool _disposed = false;

        public override void Process(IMappingLayer layer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HighlightProcessor));

            var _colorPalette = RGBController.GetActivePalette();
            var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.HighlightColor.Color);
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
                _layergroups.Add(layer.layerID, lg);
                updatedLayerGroup.Detach();
            }

            if (!layer.Enabled)
            {
                updatedLayerGroup.Detach();
                return;
            }

            foreach (var led in updatedLayerGroup)
            {
                if (led.Color != highlight_col)
                {
                    led.Color = highlight_col;
                }
            }

            var brush = new SolidColorBrush(highlight_col);
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
        }
    }
}
