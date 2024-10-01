using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.Sharlayan;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    public class HPTrackerProcessor : LayerProcessor
    {
        private static HPTrackerProcessor _instance;
        private static Dictionary<int, HPTrackerDynamicModel> layerProcessorModel = new Dictionary<int, HPTrackerDynamicModel>();
        private bool _disposed = false;

        // Private constructor to prevent direct instantiation
        private HPTrackerProcessor() { }

        // Singleton instance access
        public static HPTrackerProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HPTrackerProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HPTrackerProcessor));

            HPTrackerDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new HPTrackerDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedSortedArray(layer);
            var countKeys = ledArray.Count();

            if (model.init && (layer.requestUpdate || !layer.Enabled))
            {
                DetachAndClearGroups(model._localgroups);
                if (!layer.Enabled) return;
            }

            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                var currentVal = getCurrentPlayer.Entity.HPCurrent;
                var minVal = 0;
                var maxVal = getCurrentPlayer.Entity.HPMax;
                var valPercentage = MathHelper.CalculatePercentage(currentVal, maxVal);

                var full_col = ColorHelper.ColorToRGBColor(_colorPalette.HpFull.Color);
                var critical_col = ColorHelper.ColorToRGBColor(_colorPalette.HpCritical.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.HpEmpty.Color);

                if (maxVal <= 0) maxVal = currentVal + 1;

                if (model.critical_brush == null || model.critical_brush.Color != critical_col) model.critical_brush = new SolidColorBrush(critical_col);
                if (model.full_brush == null || model.full_brush.Color != full_col) model.full_brush = new SolidColorBrush(full_col);

                var criticalHpPercentage = AppSettings.GetSettings().criticalHpPercentage;
                if (criticalHpPercentage < 0) criticalHpPercentage = 0;
                if (criticalHpPercentage > maxVal) criticalHpPercentage = maxVal;

                if (valPercentage < criticalHpPercentage)
                {
                    model.full_brush = model.critical_brush;
                }
                else
                {
                    model.full_brush.Color = full_col;
                }

                if (layer.allowBleed)
                {
                    model.empty_brush = new SolidColorBrush(Color.Transparent);
                }
                else
                {
                    model.empty_brush = new SolidColorBrush(empty_col);
                }

                if (model._currentMode != layer.layerModes)
                {
                    DetachAndClearGroups(model._localgroups);
                    model._currentMode = layer.layerModes;
                }

                if (layer.layerModes == Enums.LayerModes.Interpolate)
                {
                    var currentVal_Interpolate = LinearInterpolation.Interpolate(currentVal, minVal, maxVal, 0, countKeys);
                    currentVal_Interpolate = Math.Max(0, Math.Min(currentVal_Interpolate, countKeys));

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

                            ledGroup.Brush = (i < currentVal_Interpolate) ? model.full_brush : model.empty_brush;
                            ledGroups.Add(ledGroup);
                        }

                        DetachAndClearGroups(model._localgroups);
                        model._localgroups = ledGroups;
                        model._interpolateValue = currentVal_Interpolate;
                    }
                }
                else if (layer.layerModes == Enums.LayerModes.Fade)
                {
                    var currentVal_Fader = ColorHelper.GetInterpolatedColor(currentVal, minVal, maxVal, model.empty_brush.Color, model.full_brush.Color);
                    if (currentVal_Fader != model._faderValue || layer.requestUpdate)
                    {
                        if (valPercentage < criticalHpPercentage)
                        {
                            model.full_brush.Color = full_col;
                            model.empty_brush.Color = critical_col;
                        }
                        else
                        {
                            model.empty_brush.Color = empty_col;
                        }

                        var ledGroup = new ListLedGroup(surface, ledArray)
                        {
                            ZIndex = layer.zindex,
                            Brush = new SolidColorBrush(currentVal_Fader)
                        };

                        ledGroup.Detach();
                        model._localgroups.Add(ledGroup);
                        model._faderValue = currentVal_Fader;
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

            foreach (var layergroup in model._localgroups)
            {
                layergroup.Attach(surface);
            }

            model.init = true;
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
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }

        private class HPTrackerDynamicModel
        {
            public List<ListLedGroup> _localgroups { get; set; } = new List<ListLedGroup>();
            public SolidColorBrush critical_brush { get; set; }
            public SolidColorBrush empty_brush { get; set; }
            public SolidColorBrush full_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; }
            public Color _faderValue { get; set; }
            public bool init { get; set; }
        }
    }
}
