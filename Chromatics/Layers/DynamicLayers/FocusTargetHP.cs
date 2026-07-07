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
    public class FocusTargetHPProcessor : LayerProcessor
    {
        private static FocusTargetHPProcessor _instance;
        private static Dictionary<int, FocusTargetHPDynamicModel> layerProcessorModel = new Dictionary<int, FocusTargetHPDynamicModel>();
        private bool _disposed = false;

        private FocusTargetHPProcessor() { }

        public static FocusTargetHPProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FocusTargetHPProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            FocusTargetHPDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new FocusTargetHPDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            // Focus Target HP Tracker Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedSortedArray(layer);
            var countKeys = ledArray.Count();

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
                var full_col = ColorHelper.ColorToRGBColor(_colorPalette.FocusTargetHpClaimed.Color);
                var friendly_col = ColorHelper.ColorToRGBColor(_colorPalette.FocusTargetHpFriendly.Color);
                var idle_col = ColorHelper.ColorToRGBColor(_colorPalette.FocusTargetHpIdle.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.FocusTargetHpEmpty.Color); // Bleed layer

                model.full_brush = model.full_brush ?? new SolidColorBrush(full_col);
                model.empty_brush = model.empty_brush ?? new SolidColorBrush(empty_col);

                model.empty_brush.Color = layer.allowBleed ? Color.Transparent : empty_col;

                var getTargetInfo = _memoryHandler.Reader.GetTargetInfo();
                if (getTargetInfo.TargetInfo == null) return;

                var focusTarget = getTargetInfo.TargetInfo.FocusTarget;
                uint targetId = focusTarget?.ID ?? 0;

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
                        model._localgroups.Add(ledGroup);
                    }
                }
                else
                {
                    var currentVal = focusTarget.HPCurrent;
                    var maxVal = focusTarget.HPMax;

                    if (maxVal <= 0) maxVal = currentVal + 1;

                    model.full_brush.Color = focusTarget.InCombat
                        ? (focusTarget.IsAggressive ? full_col : friendly_col)
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

                        if (currentVal_Interpolate != model._interpolateValue || model._targetReset || layer.requestUpdate)
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

                        if (currentVal_Fader != model._faderValue || model._targetReset || layer.requestUpdate)
                        {
                            var ledGroup = new ListLedGroup(surface, ledArray)
                            {
                                ZIndex = layer.zindex,
                                Brush = new SolidColorBrush(currentVal_Fader)
                            };

                            ledGroup.Detach();

                            DetachAndClearGroups(model._localgroups);
                            model._localgroups.Add(ledGroup);
                            model._faderValue = currentVal_Fader;
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
                    _layergroups[layer.layerID] = lg;
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
            _instance = null;
        }

        private void DetachAndClearGroups(List<ListLedGroup> groups)
        {
            foreach (var group in groups)
            {
                group?.Detach();
            }
            groups.Clear();
        }

        private class FocusTargetHPDynamicModel
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
