using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using Sharlayan.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Layers
{
    public class JobClassesProcessor : LayerProcessor
    {
        private static JobClassesProcessor _instance;
        private bool _disposed = false;
        private Dictionary<int, HashSet<Led>> _layergroupledcollections = new Dictionary<int, HashSet<Led>>();
        private readonly Dictionary<int, SolidColorBrush> _brushCache = new Dictionary<int, SolidColorBrush>();

        // Private constructor to prevent direct instantiation
        private JobClassesProcessor() { }

        // Singleton instance access
        public static JobClassesProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new JobClassesProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            if (RGBController.IsBaseLayerEffectRunning()) return;

            // Job Classes Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
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
                _layergroups[layer.layerID] = lg;
            }

            var highlight_col = Color.Transparent;

            if (!layer.Enabled)
            {
                highlight_col = ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
            }
            else
            {
                // Process data from FFXIV
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

            // Apply lighting
            if (!_brushCache.TryGetValue(layer.layerID, out var brush))
            {
                brush = new SolidColorBrush(highlight_col);
                _brushCache[layer.layerID] = brush;
            }
            else
            {
                brush.Color = highlight_col;
            }
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
                    _brushCache.Clear();
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
