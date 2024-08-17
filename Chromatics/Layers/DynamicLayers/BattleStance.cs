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
    public class DynamicBattleStanceProcessor : LayerProcessor
    {
        private bool _disposed = false;

        public override void Process(IMappingLayer layer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DynamicBattleStanceProcessor));

            // Battle Stance Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var engaged_color = ColorHelper.ColorToRGBColor(_colorPalette.BattleEngaged.Color);
            var empty_color = ColorHelper.ColorToRGBColor(_colorPalette.BattleNotEngaged.Color); // bleed layer
            var _layergroups = RGBController.GetLiveLayerGroups();

            // Loop through all LEDs and assign to device layer (Order of LEDs is not important for a highlight layer)
            var ledArray = GetLedArray(layer);

            // Add new ListLedGroup to _layergroups with updated LEDs and create a new instance of it
            ListLedGroup updatedLayerGroup;

            if (_layergroups.ContainsKey(layer.layerID))
            {
                updatedLayerGroup = _layergroups[layer.layerID].FirstOrDefault();

                if (layer.requestUpdate)
                {
                    // Replace the existing LEDs
                    updatedLayerGroup?.RemoveLeds(updatedLayerGroup);
                    updatedLayerGroup?.AddLeds(ledArray);
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
                updatedLayerGroup?.Detach();
                return;
            }

            // Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();
            var brush = new SolidColorBrush(engaged_color);

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                var inCombat = getCurrentPlayer.Entity.InCombat;

                if (!inCombat)
                {
                    brush.Color = layer.allowBleed ? Color.Transparent : empty_color;
                }
            }

            // Apply lighting
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
