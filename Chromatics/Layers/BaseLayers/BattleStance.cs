using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chromatics.Layers
{
    public class BaseBattleStanceProcessor : LayerProcessor
    {
        private static BaseBattleStanceProcessor _instance;
        private bool _disposed = false;
        private Dictionary<int, HashSet<Led>> _layergroupledcollections = new Dictionary<int, HashSet<Led>>();

        // Private constructor to prevent direct instantiation
        private BaseBattleStanceProcessor() { }

        // Singleton instance access
        public static BaseBattleStanceProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BaseBattleStanceProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            if (RGBController.IsBaseLayerEffectRunning()) return;

            // Battle Stance Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var engaged_color = ColorHelper.ColorToRGBColor(_colorPalette.BattleEngaged.Color);
            var empty_color = ColorHelper.ColorToRGBColor(_colorPalette.BattleNotEngaged.Color);
            var _layergroups = RGBController.GetLiveLayerGroups();
            HashSet<Led> _layergroupledcollection;

            ListLedGroup layergroup;
            var ledArray = GetLedArray(layer);

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
            }
            else
            {
                // Process data from FFXIV
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

            // Apply lighting
            var brush = new SolidColorBrush(engaged_color);
            layergroup.Brush = brush;
            _init = true;
            layer.requestUpdate = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _layergroupledcollections.Clear();
                    var _layergroups = RGBController.GetLiveLayerGroups();
                    foreach (var layergroup in _layergroups.Values.SelectMany(lg => lg))
                    {
                        layergroup?.Detach();
                    }
                    _layergroups.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }
    }
}
