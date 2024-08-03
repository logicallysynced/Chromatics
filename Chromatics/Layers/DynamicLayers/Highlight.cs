using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public class HighlightProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {
            //Static Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.HighlightColor.Color);
            var _layergroups = RGBController.GetLiveLayerGroups();

            //loop through all LED's and assign to device layer (Order of LEDs is not important for a highlight layer)
            
            
            var ledArray = GetLedArray(layer);
                       

            // Add new ListLedGroup to _layergroups with updated leds and create a new instance of it
            ListLedGroup updatedLayerGroup;

            if (_layergroups.ContainsKey(layer.layerID))
            {
                updatedLayerGroup = _layergroups[layer.layerID].FirstOrDefault();

                if (layer.requestUpdate)
                {
                    // Replace the existing leds
                    updatedLayerGroup.RemoveLeds(updatedLayerGroup);
                    updatedLayerGroup.AddLeds(ledArray);

                }
            } else {
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

            //Apply lighting
            var brush = new SolidColorBrush(highlight_col);
            updatedLayerGroup.Brush = brush;
            updatedLayerGroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;
        }
    }
}
