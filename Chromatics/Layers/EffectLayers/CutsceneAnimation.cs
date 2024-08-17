using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Chromatics.Extensions.Sharlayan;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures.Gradients;
using RGB.NET.Presets.Textures;
using System.Linq;

namespace Chromatics.Layers
{
    public class CutsceneAnimationProcessor : LayerProcessor
    {
        private static Dictionary<int, CutsceneAnimationEffectModel> layerProcessorModel = new Dictionary<int, CutsceneAnimationEffectModel>();
        private bool _disposed = false;

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            // Do not apply if layer/effect is disabled
            var effectSettings = RGBController.GetEffectsSettings();
            var runningEffects = RGBController.GetRunningEffects();

            CutsceneAnimationEffectModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new CutsceneAnimationEffectModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            // Cutscene Effect Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();

            ListLedGroup layergroup;
            var ledArray = GetLedArray(layer);

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
                layergroup.Detach();
            }

            if (!layer.Enabled || !effectSettings.effect_cutscenes)
            {
                layergroup.RemoveAllDecorators();

                if (runningEffects.Contains(layergroup))
                    runningEffects.Remove(layergroup);

                layergroup.Brush = new SolidColorBrush(Color.Transparent);
                layergroup.Detach();

                model._inCutscene = false;
                model.wasDisabled = true;
                return;
            }

            var baseColor = ColorHelper.ColorToRGBColor(_colorPalette.CutsceneBase.Color);
            var highlightColors = new Color[] {
                ColorHelper.ColorToRGBColor(_colorPalette.CutsceneHighlight1.Color),
                ColorHelper.ColorToRGBColor(_colorPalette.CutsceneHighlight2.Color),
                ColorHelper.ColorToRGBColor(_colorPalette.CutsceneHighlight3.Color)
            };

            var animationGradient = new LinearGradient(new GradientStop(0f, baseColor),
                new GradientStop(0.20f, highlightColors[0]),
                new GradientStop(0.35f, baseColor),
                new GradientStop(0.50f, highlightColors[1]),
                new GradientStop(0.65f, baseColor),
                new GradientStop(0.80f, highlightColors[2]),
                new GradientStop(1.00f, baseColor));

            var gradientMove = new MoveGradientDecorator(surface, 80, true);
            var animation = new StarfieldDecorator(layergroup, (layergroup.Count() / 4), 10, 500, highlightColors, surface, false, baseColor);

            // Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                DutyFinderBellExtension.CheckCache();

                if (model._inCutscene != getCurrentPlayer.Entity.InCutscene || model._inInstance != DutyFinderBellExtension.InInstance() || model.wasDisabled || layer.requestUpdate)
                {
                    if (getCurrentPlayer.Entity.InCutscene && !DutyFinderBellExtension.InInstance())
                    {
                        if (runningEffects.Contains(layergroup))
                        {
                            runningEffects.Remove(layergroup);
                        }

                        layergroup.RemoveAllDecorators();
                        animationGradient.WrapGradient = true;
                        animationGradient.AddDecorator(gradientMove);

                        layergroup.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), animationGradient));
                        layergroup.ZIndex = 1000;

                        runningEffects.Add(layergroup);
                    }
                    else
                    {
                        if (!model.wasDisabled && layergroup != null)
                        {
                            layergroup.RemoveAllDecorators();
                            layergroup.Brush = new SolidColorBrush(Color.Transparent);

                            if (runningEffects.Contains(layergroup))
                                runningEffects.Remove(layergroup);
                        }
                    }

                    model._inCutscene = getCurrentPlayer.Entity.InCutscene;
                    model._inInstance = DutyFinderBellExtension.InInstance();
                }

                model.wasDisabled = false;
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
                    // Dispose managed resources
                    foreach (var model in layerProcessorModel.Values)
                    {
                        model.activeBrush = null;
                    }
                    layerProcessorModel.Clear();
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

        private class CutsceneAnimationEffectModel
        {
            public bool _inCutscene { get; set; }
            public bool _inInstance { get; set; }
            public bool wasDisabled { get; set; }
            public SolidColorBrush activeBrush { get; set; }
            public bool init { get; set; }
        }
    }
}
