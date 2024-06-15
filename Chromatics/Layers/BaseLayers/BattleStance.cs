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
    public class BaseBattleStanceProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview() || RGBController.IsBaseLayerEffectRunning()) return;
            
            //Battle Stance Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var engaged_color = ColorHelper.ColorToRGBColor(_colorPalette.BattleEngaged.Color);
            var empty_color = ColorHelper.ColorToRGBColor(_colorPalette.BattleNotEngaged.Color);
            var _layergroupledcollections = new Dictionary<int, HashSet<Led>>();
            var _layergroups = RGBController.GetLiveLayerGroups();
            HashSet<Led> _layergroupledcollection;

            //loop through all LED's and assign to device layer (Order of LEDs is not important for a base layer)
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);

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

            if (!layer.Enabled)
            {
                engaged_color = ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
                //layergroup.Detach();
                //return;
            }
            else
            {
                //Process data from FFXIV
                var _memoryHandler = GameController.GetGameData();

                if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
                {
                    var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                    if (getCurrentPlayer.Entity == null) return;

                    var inCombat = getCurrentPlayer.Entity.InCombat;

                    Debug.WriteLine($"In Combat: {inCombat}");

                    if (!inCombat)
                    {
                        engaged_color = empty_color;
                    }

                    foreach (var led in layergroup)
                    {
                        if (!_layergroupledcollection.Contains(led))
                        {
                            _layergroupledcollection.Add(led);
                        }

                        if (led.Color != engaged_color)
                        {
                            led.Color = engaged_color;
                        }

                    }
                }
            }
            

            //Apply lighting
            var brush = new SolidColorBrush(engaged_color);
            layergroup.Brush = brush;
            _init = true;
            layer.requestUpdate = false;

        }
    }
}
