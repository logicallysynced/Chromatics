﻿using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    public class CastbarProcessor : LayerProcessor
    {
        private static CastbarProcessor _instance;
        private static Dictionary<int, CastbarDynamicModel> layerProcessorModel = new Dictionary<int, CastbarDynamicModel>();
        private bool _disposed = false;

        // Private constructor to prevent direct instantiation
        private CastbarProcessor() { }

        // Singleton instance access
        public static CastbarProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CastbarProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CastbarProcessor));

            CastbarDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new CastbarDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            // Castbar Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            // loop through all LEDs and assign to device layer (must maintain order of LEDs)
            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedSortedArray(layer);
            var countKeys = ledArray.Count();

            // Check if layer has been updated or if layer is disabled or if currently in Preview mode    
            if (model.init && (layer.requestUpdate || !layer.Enabled))
            {
                foreach (var layergroup in model._localgroups)
                {
                    layergroup?.Detach();
                }

                model._localgroups.Clear();

                if (!layer.Enabled)
                    return;
            }

            // Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                var currentVal = getCurrentPlayer.Entity.CastingPercentage;

                var minVal = 0.0;
                var maxVal = 1.0;

                if (currentVal > maxVal) currentVal = maxVal;
                if (currentVal < minVal) currentVal = minVal;

                var full_col = ColorHelper.ColorToRGBColor(_colorPalette.CastChargeFull.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.CastChargeEmpty.Color); // Bleed layer

                if (model.full_brush == null || model.full_brush.Color != full_col)
                    model.full_brush = new SolidColorBrush(full_col);

                if (layer.allowBleed)
                {
                    // Allow bleeding of other layers
                    model.empty_brush = new SolidColorBrush(Color.Transparent);
                }
                else
                {
                    model.empty_brush = new SolidColorBrush(empty_col);
                }

                // Check if layer mode has changed
                if (model._currentMode != layer.layerModes)
                {
                    foreach (var layergroup in model._localgroups)
                    {
                        layergroup?.Detach();
                    }

                    model._localgroups.Clear();
                    model._currentMode = layer.layerModes;
                }

                if (layer.layerModes == Enums.LayerModes.Interpolate)
                {
                    // Interpolate implementation
                    var currentVal_Interpolate = Convert.ToInt32(LinearInterpolation.Interpolate(currentVal, minVal, maxVal, 0, countKeys));
                    currentVal_Interpolate = MathHelper.Clamp(currentVal_Interpolate, 0, countKeys);

                    // Process Lighting
                    if (currentVal_Interpolate != model._interpolateValue || layer.requestUpdate)
                    {
                        var ledGroups = new List<ListLedGroup>();

                        for (int i = 0; i < countKeys; i++)
                        {
                            var ledGroup = new ListLedGroup(surface, ledArray[i])
                            {
                                ZIndex = layer.zindex,
                            };

                            ledGroup.Detach();

                            ledGroup.Brush = i < currentVal_Interpolate
                                ? model.full_brush
                                : model.empty_brush;

                            ledGroups.Add(ledGroup);
                        }

                        foreach (var layergroup in model._localgroups)
                        {
                            layergroup.Detach();
                        }

                        model._localgroups = ledGroups;
                        model._interpolateValue = currentVal_Interpolate;
                    }
                }
                else if (layer.layerModes == Enums.LayerModes.Fade)
                {
                    // Fade implementation
                    var currentVal_Fader = ColorHelper.GetInterpolatedColor(currentVal, minVal, maxVal, model.empty_brush.Color, model.full_brush.Color);
                    if (currentVal_Fader != model._faderValue || layer.requestUpdate)
                    {
                        var ledGroup = new ListLedGroup(surface, ledArray)
                        {
                            ZIndex = layer.zindex,
                            Brush = new SolidColorBrush(currentVal_Fader)
                        };

                        ledGroup.Detach();

                        if (!model._localgroups.Contains(ledGroup))
                            model._localgroups.Add(ledGroup);

                        model._faderValue = currentVal_Fader;
                    }
                }

                // Send layers to _layergroups Dictionary to be tracked outside this method
                var lg = model._localgroups.ToArray();

                if (_layergroups.ContainsKey(layer.layerID))
                {
                    _layergroups[layer.layerID] = lg;
                }
                else
                {
                    _layergroups.Add(layer.layerID, lg);
                }
            }

            // Apply lighting
            foreach (var layergroup in model._localgroups)
            {
                layergroup.Attach(surface);
            }

            model.init = true;
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

                    layerProcessorModel.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }

        private class CastbarDynamicModel
        {
            public List<ListLedGroup> _localgroups { get; set; } = new List<ListLedGroup>();
            public SolidColorBrush empty_brush { get; set; }
            public SolidColorBrush full_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; }
            public Color _faderValue { get; set; }
            public bool init { get; set; }
        }
    }
}
