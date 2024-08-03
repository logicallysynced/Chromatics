using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using Sharlayan.Core.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public class JobClassesProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {
            if (RGBController.IsBaseLayerEffectRunning()) return;
            
            //Job Classes Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroupledcollections = new Dictionary<int, HashSet<Led>>();
            var _layergroups = RGBController.GetLiveLayerGroups();
            HashSet<Led> _layergroupledcollection;

            //loop through all LED's and assign to device layer (Order of LEDs is not important for a base layer)
            var surface = RGBController.GetLiveSurfaces();
            var device = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == layer.deviceGuid).Value;

            ListLedGroup layergroup;
            var ledArray = device.Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

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
                layergroup = new ListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new ListLedGroup[] { layergroup };
                _layergroups.Add(layer.layerID, lg);
            }

            var highlight_col = Color.Transparent;

            if (!layer.Enabled)
            {
                highlight_col = ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
            }
            else
            {
                //Process data from FFXIV
                var _memoryHandler = GameController.GetGameData();

                if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
                {
                    var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                    if (getCurrentPlayer.Entity == null) return;

                    var currentJob = getCurrentPlayer.Entity.Job;

                    highlight_col = GameHelper.GetJobClassColor(currentJob, _colorPalette, false);
                
                }
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
            _init = true;
            layer.requestUpdate = false;

        }
    }
}
