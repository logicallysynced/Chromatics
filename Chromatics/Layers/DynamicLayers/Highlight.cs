using Chromatics.Core;
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
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            //Static Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.HighlightColor.Color);
            var _layergroups = RGBController.GetLiveLayerGroups();

            //loop through all LED's and assign to device layer
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);
            var ledArray = devices.SelectMany(d => d).Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

            // Add new ListLedGroup to _layergroups with updated leds and create a new instance of it

            if (_layergroups.TryGetValue(layer.layerID, out ListLedGroup updatedLayerGroup))
            {
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

                _layergroups.Add(layer.layerID, updatedLayerGroup);
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
                    Debug.WriteLine(@"1: Layer ID: " + layer.layerID + @". LED: " + led.Id + @". Col: " + led.Color);
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
