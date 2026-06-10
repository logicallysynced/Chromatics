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
    public class JobClassesHighlightProcessor : LayerProcessor
    {
        private static JobClassesHighlightProcessor _instance;
        private bool _disposed = false;
        private readonly Dictionary<int, SolidColorBrush> _brushCache = new Dictionary<int, SolidColorBrush>();

        // Private constructor to prevent direct instantiation
        private JobClassesHighlightProcessor() { }

        // Singleton instance access
        public static JobClassesHighlightProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new JobClassesHighlightProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            // Job Classes Highlight Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var highlight_col = Color.Transparent;
            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedArray(layer);

            // Add new ListLedGroup to _layergroups with updated LEDs and create a new instance of it
            ListLedGroup updatedLayerGroup;

            if (_layergroups.ContainsKey(layer.layerID))
            {
                updatedLayerGroup = _layergroups[layer.layerID].FirstOrDefault();

                if (layer.requestUpdate)
                {
                    // Replace the existing LEDs
                    updatedLayerGroup.RemoveLeds(updatedLayerGroup);
                    updatedLayerGroup.AddLeds(ledArray);
                }
            }
            else
            {
                updatedLayerGroup = new ListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new ListLedGroup[] { updatedLayerGroup };
                _layergroups[layer.layerID] = lg;
                updatedLayerGroup.Detach();
            }

            if (!layer.Enabled)
            {
                updatedLayerGroup.Detach();
                return;
            }

            // Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                var currentJob = getCurrentPlayer.Entity.Job;
                highlight_col = GameHelper.GetJobClassColor(currentJob, _colorPalette, true);

                foreach (var led in updatedLayerGroup)
                {
                    if (led.Color != highlight_col)
                    {
                        led.Color = highlight_col;
                    }
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
            updatedLayerGroup.Brush = brush;
            updatedLayerGroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;
        }

        private void DetachAndClearGroups(List<ListLedGroup> groups)
        {
            foreach (var group in groups)
            {
                group?.Detach();
            }
            groups.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _brushCache.Clear();
                    var _layergroups = RGBController.GetLiveLayerGroups();
                    if (_layergroups != null && _layergroups.Values != null)
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
            _instance = null;
        }
    }
}
