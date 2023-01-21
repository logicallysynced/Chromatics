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
            var _layergroupledcollections = new Dictionary<int, HashSet<Led>>();
            var _layergroups = RGBController.GetLiveLayerGroups();
            HashSet<Led> _layergroupledcollection;

            //loop through all LED's and assign to device layer
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);

            //ListLedGroup layergroup;
            ListLedGroup layergroup;
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
                layergroup = _layergroups[layer.layerID];
                layergroup.ZIndex = layer.zindex;
            }
            else
            {

                layergroup = new ListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };


                _layergroups.Add(layer.layerID, layergroup);
            }           
            
            //Debug.WriteLine(@"Layer ID: " + layer.layerID + " Device Type: " + layer.deviceType);

            if (!layer.Enabled)
            {
                layergroup.Detach();
                Debug.WriteLine(@"Detached from " + layer.layerID);
                return;
            }
             
            if (!_init)
            {
                layergroup.Detach();
            }

            foreach (var led in layergroup)
            {
                if (!_layergroupledcollection.Contains(led))
                {
                    _layergroupledcollection.Add(led);
                }

                if (led.Color != highlight_col)
                {
                    led.Color = highlight_col;
                }

            }


            //Apply lighting
            var brush = new SolidColorBrush(highlight_col);
            layergroup.Brush = brush;
            layergroup.Attach(surface);
            _init = true;
        }
    }
}
