using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chromatics.Layers
{
    public class AudioVisualizerBaseProcessor : LayerProcessor
    {
        private static AudioVisualizerBaseProcessor _instance;
        private static Dictionary<int, AudioVisualizerModel> layerProcessorModel = new();
        private bool _disposed;

        private AudioVisualizerBaseProcessor() { }

        public static AudioVisualizerBaseProcessor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AudioVisualizerBaseProcessor();
                return _instance;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var model in layerProcessorModel.Values)
                    {
                        model.Cleanup();
                        model._effectEnabled = false;
                    }
                    layerProcessorModel.Clear();
                }
                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }

        public override void CleanupLayer(int layerID)
        {
            if (layerProcessorModel.TryGetValue(layerID, out var model))
            {
                model.Cleanup();
                model._effectEnabled = false;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;
            if (RGBController.IsBaseLayerEffectRunning()) return;

            AudioVisualizerModel model;
            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new AudioVisualizerModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();

            ListLedGroup layergroup;
            var ledArray = GetLedArray(layer);

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
            }
            else
            {
                layergroup = new ListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new ListLedGroup[] { layergroup };
                _layergroups.Add(layer.layerID, lg);
                layergroup.Detach();
            }

            var enabled = layer.Enabled;

            if (!enabled || model._effectEnabled != enabled || layer.requestUpdate)
            {
                if (!enabled)
                {
                    model.Cleanup();
                    model._effectEnabled = false;

                    var baseCol = ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
                    layergroup.Brush = new SolidColorBrush(baseCol);
                    layergroup.RemoveAllDecorators();
                    layergroup.Attach(surface);

                    _init = true;
                    layer.requestUpdate = false;
                    return;
                }
            }

            if (!model._effectEnabled || layer.requestUpdate)
            {
                layergroup.RemoveAllDecorators();
                model.Cleanup();

                var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.AudioVisualizerBase.Color);
                var colors = new Color[]
                {
                    ColorHelper.ColorToRGBColor(_colorPalette.AudioVisualizerLow.Color),
                    ColorHelper.ColorToRGBColor(_colorPalette.AudioVisualizerMid.Color),
                    ColorHelper.ColorToRGBColor(_colorPalette.AudioVisualizerHigh.Color),
                };

                var effect = new AudioVisualizerEffect(layergroup, colors, surface, baseCol);
                layergroup.AddDecorator(effect);
                model._activeEffect = effect;
                model._group = layergroup;
                model._effectEnabled = true;
            }

            _init = true;
            layer.requestUpdate = false;
        }

        private class AudioVisualizerModel
        {
            public AudioVisualizerEffect _activeEffect;
            public ListLedGroup _group;
            public bool _effectEnabled;

            public void Cleanup()
            {
                if (_activeEffect != null && _group != null)
                {
                    try { _group.RemoveDecorator(_activeEffect); } catch { }
                }
                _activeEffect = null;
            }
        }
    }
}
