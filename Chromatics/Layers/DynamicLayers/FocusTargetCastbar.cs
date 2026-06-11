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
    public class FocusTargetCastbarProcessor : LayerProcessor
    {
        private static FocusTargetCastbarProcessor _instance;
        private static Dictionary<int, FocusTargetCastbarDynamicModel> layerProcessorModel = new Dictionary<int, FocusTargetCastbarDynamicModel>();
        private bool _disposed = false;

        private FocusTargetCastbarProcessor() { }

        public static FocusTargetCastbarProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FocusTargetCastbarProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            FocusTargetCastbarDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new FocusTargetCastbarDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            // Focus Target Castbar Layer Implementation
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
                var getTargetInfo = _memoryHandler.Reader.GetTargetInfo().TargetInfo;
                if (getTargetInfo == null) return;

                var focusTarget = getTargetInfo.FocusTarget;
                uint targetId = focusTarget?.ID ?? 0;

                var full_col = ColorHelper.ColorToRGBColor(_colorPalette.FocusTargetCastbar.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.FocusTargetCastbarEmpty.Color); // Bleed layer

                model.full_brush ??= new SolidColorBrush(full_col);
                model.full_brush.Color = full_col;

                model.empty_brush ??= new SolidColorBrush(layer.allowBleed ? Color.Transparent : empty_col);
                model.empty_brush.Color = layer.allowBleed ? Color.Transparent : empty_col;

                if (targetId != model._targetId)
                {
                    DetachAndClearGroups(model._localgroups);
                    model._targetReset = true;
                    model._targetId = targetId;
                }

                // Check if layer mode has changed
                if (model._currentMode != layer.layerModes)
                {
                    DetachAndClearGroups(model._localgroups);
                    model._currentMode = layer.layerModes;
                }

                // No focus target set: paint the empty bar instead of
                // returning, so the selected keys hold the negative colour
                // (or bleed) rather than freezing on the last cast state.
                var currentVal = targetId != 0 ? focusTarget.CastingPercentage : 0.0;
                var minVal = 0.0;
                var maxVal = 1.0;

                if (layer.layerModes == Enums.LayerModes.Interpolate)
                {
                    // Interpolate implementation
                    var currentVal_Interpolate = Convert.ToInt32(LinearInterpolation.Interpolate(currentVal, minVal, maxVal, 0, countKeys));
                    currentVal_Interpolate = Math.Max(0, Math.Min(countKeys, currentVal_Interpolate));

                    // Process Lighting
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
                    // Fade implementation
                    var currentVal_Fader = ColorHelper.GetInterpolatedColor(currentVal, minVal, maxVal, model.empty_brush.Color, model.full_brush.Color);

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

                // Send layers to _layergroups Dictionary to be tracked outside this method
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

        private class FocusTargetCastbarDynamicModel
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
