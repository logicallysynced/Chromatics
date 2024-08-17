using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Layers
{
    public class StaticProcessor : LayerProcessor
    {
        private bool _disposed = false;
        private Dictionary<int, HashSet<Led>> _layergroupledcollections = new Dictionary<int, HashSet<Led>>();

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            if (RGBController.IsBaseLayerEffectRunning()) return;

            // Static Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.BaseColor.Color);
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
                highlight_col = ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
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
            var brush = new SolidColorBrush(highlight_col);
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
        }
    }
}
