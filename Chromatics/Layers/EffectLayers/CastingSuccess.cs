using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;

namespace Chromatics.Layers
{
    public class CastingSuccessProcessor : LayerProcessor
    {
        private static CastingSuccessProcessor _instance;
        private static Dictionary<int, CastingSuccessEffectModel> layerProcessorModel = new Dictionary<int, CastingSuccessEffectModel>();
        private bool _disposed = false;

        // 200ms game-loop granularity means the final casting sample lands
        // anywhere in the last fifth of the bar. Casts observed at or above
        // this fraction when IsCasting drops are treated as completed;
        // anything lower is an interrupt / slidecast-cancel.
        private const double SuccessThreshold = 0.85;

        private CastingSuccessProcessor() { }

        public static CastingSuccessProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CastingSuccessProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            var effectSettings = RGBController.GetEffectsSettings();

            CastingSuccessEffectModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new CastingSuccessEffectModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();

            // Own group pinned just under the Duty Finder Bell so a
            // simultaneous bell flash composites above the cast ripple.
            ListLedGroup layergroup = model.layergroup;
            bool registered = layergroup != null
                && _layergroups.TryGetValue(layer.layerID, out var registeredGroups)
                && Array.IndexOf(registeredGroups, layergroup) >= 0;
            if (!registered)
            {
                if (model.activeBrush != null)
                {
                    model.activeBrush.RemoveAllDecorators();
                    model.activeBrush = null;
                    model.activeRipple = null;
                }
                layergroup?.Detach();
                layergroup = new ListLedGroup(surface, GetLedArray(layer))
                {
                    ZIndex = EffectZIndex.CastingSuccess,
                    Brush = new SolidColorBrush(Color.Transparent),
                };
                layergroup.Detach();
                model.layergroup = layergroup;
                RGBController.RegisterLiveLayerGroup(layer.layerID, layergroup);
            }

            if (!layer.Enabled || !effectSettings.effect_castingsuccess || !MappingLayers.IsDeviceEffectsEnabled(layer.deviceGuid))
            {
                if (model.activeBrush != null)
                {
                    model.activeBrush.RemoveAllDecorators();
                    layergroup.Brush = new SolidColorBrush(Color.Transparent);
                    model.activeBrush = null;
                    model.activeRipple = null;
                }
                model.wasCasting = false;
                return;
            }

            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity != null)
                {
                    bool isCasting = getCurrentPlayer.Entity.IsCasting1;

                    if (isCasting)
                    {
                        model.lastCastPercentage = getCurrentPlayer.Entity.CastingPercentage;
                    }
                    else if (model.wasCasting && model.lastCastPercentage >= SuccessThreshold)
                    {
                        var ringColor = ColorHelper.ColorToRGBColor(_colorPalette.CastingSuccess.Color);

                        model.activeBrush?.RemoveAllDecorators();

                        var brush = new SolidColorBrush(Color.Transparent);
                        var ripple = new RippleBrushDecorator(layergroup, surface, LedId.Keyboard_J, 12.0, 2.0, ringColor)
                        {
                            IsEnabled = true,
                        };
                        brush.AddDecorator(ripple);

                        layergroup.Brush = brush;
                        model.activeBrush = brush;
                        model.activeRipple = ripple;
                    }

                    model.wasCasting = isCasting;
                    if (!isCasting && model.activeRipple == null) model.lastCastPercentage = 0;
                }
            }

            if (model.activeRipple != null && model.activeRipple.IsFinished)
            {
                model.activeBrush?.RemoveAllDecorators();
                layergroup.Brush = new SolidColorBrush(Color.Transparent);
                model.activeBrush = null;
                model.activeRipple = null;
                model.lastCastPercentage = 0;
            }

            if (layergroup.Decorators.Count == 0)
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
                    foreach (var model in layerProcessorModel.Values)
                    {
                        model.activeBrush?.RemoveAllDecorators();
                    }
                    layerProcessorModel.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }

        private class CastingSuccessEffectModel
        {
            public bool wasCasting { get; set; }
            public double lastCastPercentage { get; set; }
            public SolidColorBrush activeBrush { get; set; }
            public RippleBrushDecorator activeRipple { get; set; }
            public ListLedGroup layergroup { get; set; }
            public bool init { get; set; }
        }
    }
}
