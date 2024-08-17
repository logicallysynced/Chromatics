using Chromatics.Core;
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
    public class TargetHPProcessor : LayerProcessor
    {
        private static Dictionary<int, TargetHPDynamicModel> layerProcessorModel = new Dictionary<int, TargetHPDynamicModel>();
        private bool _disposed = false;

        public override void Process(IMappingLayer layer)
        {
            TargetHPDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new TargetHPDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            // Target HP Tracker Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedSortedArray(layer);
            var countKeys = ledArray.Count();

            // Check if layer has been updated or if layer is disabled or if currently in Preview mode    
            if (model.init && (layer.requestUpdate || !layer.Enabled))
            {
                DetachAndClearGroups(model._localgroups);

                if (!layer.Enabled)
                    return;
            }

            // Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetTargetInfo())
            {
                var full_col = ColorHelper.ColorToRGBColor(_colorPalette.TargetHpClaimed.Color);
                var friendly_col = ColorHelper.ColorToRGBColor(_colorPalette.TargetHpFriendly.Color);
                var idle_col = ColorHelper.ColorToRGBColor(_colorPalette.TargetHpIdle.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.TargetHpEmpty.Color); // Bleed layer

                model.full_brush = model.full_brush ?? new SolidColorBrush(full_col);
                model.empty_brush = model.empty_brush ?? new SolidColorBrush(empty_col);

                model.empty_brush.Color = layer.allowBleed ? Color.Transparent : empty_col;

                var getTargetInfo = _memoryHandler.Reader.GetTargetInfo();
                if (getTargetInfo.TargetInfo == null) return;

                uint targetId = getTargetInfo.TargetInfo.CurrentTarget?.ID ?? 0;

                if (targetId != model._targetId)
                {
                    DetachAndClearGroups(model._localgroups);
                    model._targetReset = true;
                    model._targetId = targetId;
                }

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
                    if (getTargetInfo.TargetInfo.CurrentTarget != null)
                    {
                        var currentVal = getTargetInfo.TargetInfo.CurrentTarget.HPCurrent;
                        var maxVal = getTargetInfo.TargetInfo.CurrentTarget.HPMax;
                        var valPercentage = MathHelper.CalculatePercentage(currentVal, maxVal);

                        if (maxVal <= 0) maxVal = currentVal + 1;

                        model.full_brush.Color = getTargetInfo.TargetInfo.CurrentTarget.InCombat
                            ? (getTargetInfo.TargetInfo.CurrentTarget.IsAggressive ? full_col : friendly_col)
                            : idle_col;

                        if (model._currentMode != layer.layerModes)
                        {
                            DetachAndClearGroups(model._localgroups);
                            model._currentMode = layer.layerModes;
                        }

                        if (layer.layerModes == Enums.LayerModes.Interpolate)
                        {
                            var currentVal_Interpolate = LinearInterpolation.Interpolate(currentVal, 0, maxVal, 0, countKeys);
                            currentVal_Interpolate = Math.Max(0, Math.Min(countKeys, currentVal_Interpolate));

                            if (currentVal_Interpolate != model._interpolateValue || model._targetReset)
                            {
                                var ledGroups = new List<ListLedGroup>();

                                for (int i = 0; i < countKeys; i++)
                                {
                                    var ledGroup = new ListLedGroup(surface, ledArray[i])
                                    {
                                        ZIndex = layer.zindex,
                                    };

                                    ledGroup.Detach();

                                    ledGroup.Brush = i < currentVal_Interpolate ? model.full_brush : model.empty_brush;

                                    ledGroups.Add(ledGroup);
                                }

                                DetachAndClearGroups(model._localgroups);

                                model._localgroups = ledGroups;
                                model._interpolateValue = currentVal_Interpolate;
                            }
                        }
                        else if (layer.layerModes == Enums.LayerModes.Fade)
                        {
                            var currentVal_Fader = ColorHelper.GetInterpolatedColor(currentVal, 0, maxVal, model.empty_brush.Color, model.full_brush.Color);

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
                    }
                }

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
                    foreach (var model in layerProcessorModel.Values)
                    {
                        DetachAndClearGroups(model._localgroups);
                    }
                    layerProcessorModel.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        private void DetachAndClearGroups(List<ListLedGroup> groups)
        {
            foreach (var group in groups)
            {
                group?.Detach();
            }
            groups.Clear();
        }

        private class TargetHPDynamicModel
        {
            public List<ListLedGroup> _localgroups { get; set; } = new List<ListLedGroup>();
            public SolidColorBrush empty_brush { get; set; }
            public SolidColorBrush full_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; }
            public Color _faderValue { get; set; }
            public uint _targetId { get; set; }
            public bool _targetReset { get; set; }
            public bool init { get; set; }
        }
    }
}
