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
    public class EnmityTrackerProcessor : LayerProcessor
    {
        private static EnmityTrackerProcessor _instance;
        private static Dictionary<int, EnmityDynamicModel> layerProcessorModel = new Dictionary<int, EnmityDynamicModel>();
        private bool _disposed = false;

        // Private constructor to prevent direct instantiation
        private EnmityTrackerProcessor() { }

        // Singleton instance access
        public static EnmityTrackerProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EnmityTrackerProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EnmityTrackerProcessor));

            EnmityDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new EnmityDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            // Enmity Tracker Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

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

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetTargetInfo() && _memoryHandler.Reader.CanGetActors())
            {
                var enmity_top_col = ColorHelper.ColorToRGBColor(_colorPalette.EmnityRed.Color);
                var enmity_high_col = ColorHelper.ColorToRGBColor(_colorPalette.EmnityOrange.Color);
                var enmity_med_col = ColorHelper.ColorToRGBColor(_colorPalette.EmnityYellow.Color);
                var enmity_low_col = ColorHelper.ColorToRGBColor(_colorPalette.EmnityGreen.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.NoEmnity.Color); // Bleed layer

                if (model.enmity_brush == null || model.enmity_brush.Color != enmity_low_col)
                    model.enmity_brush = new SolidColorBrush(enmity_low_col);

                if (model.empty_brush == null || model.empty_brush.Color != empty_col)
                    model.empty_brush = new SolidColorBrush(empty_col);

                if (layer.allowBleed)
                {
                    // Allow bleeding of other layers
                    model.empty_brush.Color = Color.Transparent;
                }
                else
                {
                    model.empty_brush.Color = empty_col;
                }

                var getTargetInfo = _memoryHandler.Reader.GetTargetInfo();
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();

                if (getTargetInfo.TargetInfo == null || getCurrentPlayer.Entity == null) return;

                uint targetId = 0;

                if (getTargetInfo.TargetInfo.CurrentTarget != null)
                {
                    targetId = getTargetInfo.TargetInfo.CurrentTarget.ID;
                }

                if (targetId != model._targetId)
                {
                    foreach (var layergroup in model._localgroups)
                    {
                        layergroup?.Detach();
                    }

                    model._localgroups.Clear();
                    model._targetReset = true;
                    model._targetId = targetId;
                }

                // No target found
                if (targetId == 0 || !model.init)
                {
                    if (model._targetReset || !model.init)
                    {
                        var ledGroup = new ListLedGroup(surface, ledArray)
                        {
                            ZIndex = layer.zindex,
                            Brush = model.empty_brush
                        };

                        ledGroup.Detach();

                        if (!model._localgroups.Contains(ledGroup))
                            model._localgroups.Add(ledGroup);
                    }
                }
                else
                {
                    if (getTargetInfo.TargetInfo.CurrentTarget != null && getTargetInfo.TargetInfo.EnmityItems != null && getTargetInfo.TargetInfo.EnmityItems.Count > 0)
                    {
                        var enmityList = getTargetInfo.TargetInfo.EnmityItems;
                        var enmityProfile = enmityList.FirstOrDefault(item => item.ID == targetId);

                        if (enmityProfile == null) return;

                        var enmityPosition = enmityProfile.Enmity;

                        var currentVal = (int)enmityPosition;
                        var minVal = 0;
                        var maxVal = 100;

                        if (enmityPosition != model._enmityPosition || model._targetReset)
                        {
                            if (enmityPosition == 100)
                            {
                                //Full Aggro
                                model.enmity_brush.Color = enmity_top_col;
                            }
                            else if (enmityPosition >= 80 && enmityPosition < 100)
                            {
                                //High Aggro
                                model.enmity_brush.Color = enmity_high_col;
                            }
                            else if (enmityPosition >= 50 && model._enmityPosition < 80)
                            {
                                //Moderate Aggro
                                model.enmity_brush.Color = enmity_med_col;
                            }
                            else if (enmityPosition < 50) //&& _enmityPosition <= 8)
                            {
                                //Low Aggro
                                model.enmity_brush.Color = enmity_low_col;
                            }
                            else
                            {
                                //Not Engaged & No Aggro
                                model.enmity_brush = model.empty_brush;
                            }

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
                            var currentVal_Interpolate = LinearInterpolation.Interpolate(currentVal, minVal, maxVal, 0, countKeys);
                            currentVal_Interpolate = MathHelper.Clamp(currentVal_Interpolate, 0, countKeys);

                            if (currentVal_Interpolate != model._interpolateValue || model._targetReset)
                            {
                                // Process Lighting
                                var ledGroups = new List<ListLedGroup>();

                                for (int i = 0; i < countKeys; i++)
                                {
                                    var ledGroup = new ListLedGroup(surface, ledArray[i])
                                    {
                                        ZIndex = layer.zindex,
                                    };

                                    ledGroup.Detach();

                                    ledGroup.Brush = i < currentVal_Interpolate
                                        ? model.enmity_brush
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
                            var currentVal_Fader = ColorHelper.GetInterpolatedColor(currentVal, minVal, maxVal, model.empty_brush.Color, model.enmity_brush.Color);
                            if (currentVal_Fader != model._faderValue || model._targetReset)
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

                        model._enmityPosition = enmityPosition;
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
            model._targetReset = false;
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

        private class EnmityDynamicModel
        {
            public List<ListLedGroup> _localgroups { get; set; } = new List<ListLedGroup>();
            public SolidColorBrush empty_brush { get; set; }
            public SolidColorBrush enmity_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; }
            public Color _faderValue { get; set; }
            public uint _enmityPosition { get; set; }
            public uint _targetId { get; set; }
            public bool _targetReset { get; set; }
            public bool init { get; set; }
        }
    }
}
