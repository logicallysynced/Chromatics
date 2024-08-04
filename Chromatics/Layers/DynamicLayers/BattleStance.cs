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
    public class DynamicBattleStanceProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {

            //Battle Stance Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var engaged_color = ColorHelper.ColorToRGBColor(_colorPalette.BattleEngaged.Color);
            var empty_color = ColorHelper.ColorToRGBColor(_colorPalette.BattleNotEngaged.Color); //bleed layer
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


            //Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();
            var brush = new SolidColorBrush(engaged_color);

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                var inCombat = getCurrentPlayer.Entity.InCombat;
                
                if (!inCombat)
                {
                    if (layer.allowBleed)
                    {
                        brush.Color = Color.Transparent;
                    }
                    else
                    {
                        brush.Color = empty_color;
                    }
                    
                }

            }
                        

            //Apply lighting
            updatedLayerGroup.Brush = brush;
            updatedLayerGroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;
        }
    }
}
