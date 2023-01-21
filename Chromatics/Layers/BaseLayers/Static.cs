using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public class StaticProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            //Static Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroupledcollection = RGBController.GetLiveLayerGroupCollection();
            var _layergroups = RGBController.GetLiveLayerGroups();

            //Loop through all LED's and assign to device layer
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);

            var layergroup = new ListLedGroup(surface)
            {
                ZIndex = layer.zindex,
            };

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID];
            }
            else
            {
                _layergroups.Add(layer.layerID, layergroup);
            }

            var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.BaseColor.Color);

            foreach (var device in devices)
            {
                if (!RGBController.GetLiveDevices().Contains(device)) continue;

                foreach (var led in device)
                {
                    if (!layer.deviceLeds.Any(v => v.Value.Equals(led.Id)))
                    {
                        layergroup.RemoveLed(led);
                        _layergroupledcollection.Remove(led);
                        continue;
                    }

                    if (!layer.Enabled)
                    {
                        highlight_col = ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
                    }
                    
                    if (led.Color != highlight_col)
                    {
                        layergroup.RemoveLed(led);
                        led.Color = highlight_col;
                    }

                    if (!_layergroupledcollection.Contains(led))
                    {
                        _layergroupledcollection.Add(led);
                    }

                    layergroup.AddLed(led);

                }

            }

            //Apply lighting
            var brush = new SolidColorBrush(highlight_col);
            layergroup.Brush = brush;

            //Debug.WriteLine(@"Applying LED");
        }
    }
}
