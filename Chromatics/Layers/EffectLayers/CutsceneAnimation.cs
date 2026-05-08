using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures.Gradients;
using RGB.NET.Presets.Textures;
using System.Linq;

namespace Chromatics.Layers
{
    public class CutsceneAnimationProcessor : LayerProcessor
    {
        private static CutsceneAnimationProcessor _instance;
        private static Dictionary<int, CutsceneAnimationEffectModel> layerProcessorModel = new Dictionary<int, CutsceneAnimationEffectModel>();
        private bool _disposed = false;

        // Private constructor to prevent direct instantiation
        private CutsceneAnimationProcessor() { }

        // Singleton instance access
        public static CutsceneAnimationProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CutsceneAnimationProcessor();
                }
                return _instance;
            }
        }

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

            // Raid-effect override: when a raid effect is active (or held in
            // entry / phase blackout) we suppress the cutscene animation
            // without touching the user's cutscene toggle in settings. Raid
            // effects own the base layer for the duration of the encounter
            // and cutscene animations firing on top would clobber the overlay.
            // Evaluated every tick so the override lifts automatically once
            // the raid effect resets raidEffectsRunning.
            //
            // Also suppress while the Victory Fanfare BGM is playing — the
            // end-of-duty victory cutscene shouldn't get the cutscene
            // gradient animation at all, since it's a celebratory moment
            // belonging to the raid encounter itself.
            bool raidEffectActive = RaidEffectState.raidEffectsRunning
                || RaidEffectState.delayedStartUntil != DateTime.MinValue;

            var handler = GameController.GetGameData();
            uint currentBgmId = 0;
            try { currentBgmId = handler?.Reader?.GetGameState().CurrentBgmId ?? 0; }
            catch { /* memory read can transiently fail; treat as no BGM */ }
            bool isVictoryFanfare = currentBgmId == RaidEffectState.VictoryBgmId;

            if (!layer.Enabled || !effectSettings.effect_cutscenes || raidEffectActive || isVictoryFanfare || !MappingLayers.IsDeviceEffectsEnabled(layer.deviceGuid))
            {
                // GameController dispatches every effect processor on every
                // EffectLayer (DF Bell, Damage Flash, Vegas, Cutscene all
                // share the same layergroup). If we wipe the layergroup
                // unconditionally here, we clobber whatever DF Bell or
                // Damage Flash has just painted on the same tick. Only do
                // the wipe when this processor was actually painting (i.e.
                // a cutscene was running) — that's a one-shot teardown of
                // OUR contribution. Outside a cutscene, leave the
                // layergroup alone so other processors' work survives.
                //
                // Tear down the active gradient too: the MoveGradientDecorator
                // is attached to the gradient object (not the layergroup), so
                // layergroup.RemoveAllDecorators() alone wouldn't unsubscribe
                // it from surface.Updating. Without the explicit removal the
                // decorator keeps firing (invisibly, since the brush is no
                // longer rendered) and accumulates wasted CPU per cancellation.
                if (model._inCutscene)
                {
                    layergroup.RemoveAllDecorators();
                    model.activeGradient?.RemoveAllDecorators();
                    model.activeGradient = null;

                    if (runningEffects.Contains(layergroup))
                        runningEffects.Remove(layergroup);

                    layergroup.Brush = new SolidColorBrush(Color.Transparent);
                    layergroup.Detach();

                    model._inCutscene = false;
                }

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
                var reader = _memoryHandler.Reader;
                var getCurrentPlayer = reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                var gameStateResult = reader.GetGameState();
                bool inCutscene = gameStateResult.WatchingCutscene && !gameStateResult.IsTeleporting;
                bool inInstance = gameStateResult.InInstance;

                if (model._inCutscene != inCutscene || model._inInstance != inInstance || model.wasDisabled || layer.requestUpdate)
                {
                    if (inCutscene && !inInstance)
                    {
                        if (runningEffects.Contains(layergroup))
                        {
                            runningEffects.Remove(layergroup);
                        }

                        layergroup.RemoveAllDecorators();
                        // Tear down any previous gradient's decorator chain
                        // before swapping in a new gradient — without this,
                        // each cutscene toggle left the prior gradient's
                        // MoveGradientDecorator subscribed to surface.Updating.
                        model.activeGradient?.RemoveAllDecorators();

                        animationGradient.WrapGradient = true;
                        animationGradient.AddDecorator(gradientMove);
                        model.activeGradient = animationGradient;

                        layergroup.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), animationGradient));
                        layergroup.ZIndex = 1000;

                        runningEffects.Add(layergroup);
                    }
                    else
                    {
                        if (!model.wasDisabled && layergroup != null)
                        {
                            layergroup.RemoveAllDecorators();
                            model.activeGradient?.RemoveAllDecorators();
                            model.activeGradient = null;
                            layergroup.Brush = new SolidColorBrush(Color.Transparent);

                            if (runningEffects.Contains(layergroup))
                                runningEffects.Remove(layergroup);
                        }
                    }

                    model._inCutscene = inCutscene;
                    model._inInstance = inInstance;
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
            _instance = null;
        }

        private class CutsceneAnimationEffectModel
        {
            public bool _inCutscene { get; set; }
            public bool _inInstance { get; set; }
            public bool wasDisabled { get; set; }
            public SolidColorBrush activeBrush { get; set; }
            public bool init { get; set; }
            // Tracks the currently-painting gradient so cleanup paths
            // (raid-effect override, natural-exit from cutscene, shutdown)
            // can unsubscribe its decorator from surface.Updating. Without
            // this, each cutscene-start left the previous cycle's gradient
            // decorator orphaned but subscribed — wastes CPU per iteration.
            public RGB.NET.Presets.Textures.Gradients.LinearGradient activeGradient { get; set; }
        }
    }
}
