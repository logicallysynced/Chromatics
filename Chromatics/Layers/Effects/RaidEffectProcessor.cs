using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Layers.Effects
{
    // Standalone raid-effect overlay for base layers. Runs once per active
    // BaseLayer (after the user-selected base processor) and creates a
    // high-ZIndex ListLedGroup that overlays whatever the base layer is
    // painting. When no raid effect should be active for the current zone /
    // duty / BGM, the overlay is detached and the base layer renders as
    // normal.
    //
    // Overlay groups are kept in a private dictionary keyed by the source
    // base layer's layerID. They live independently of
    // RGBController.GetLiveLayerGroups() so requestUpdate / type-switch
    // cleanup in GameController doesn't tear them down.
    public sealed class RaidEffectProcessor : LayerProcessor
    {
        private static RaidEffectProcessor _instance;
        private readonly Dictionary<int, ListLedGroup> _overlays = new();
        private readonly Dictionary<int, HashSet<LinearGradient>> _gradientEffects = new();
        private bool _disposed;

        // Higher than user layer ZIndexes (typically 1-10) so the raid
        // effect visibly overrides them. Lower than 1000 (CutsceneAnimation)
        // so cutscenes still take priority over gameplay raid effects.
        private const int RaidOverlayZIndex = 500;

        private RaidEffectProcessor() { }

        public static RaidEffectProcessor Instance => _instance ??= new RaidEffectProcessor();

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            var effectSettings = RGBController.GetEffectsSettings();

            if (!layer.Enabled || !effectSettings.effect_raideffects)
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var handler = GameController.GetGameData();
            if (handler?.Reader == null || !handler.Reader.CanGetActors())
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var player = handler.Reader.GetCurrentPlayer();
            if (player.Entity == null)
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var zone = GameHelper.GetZoneNameById(player.Entity.MapTerritory);
            var gameState = handler.Reader.GetGameState();
            bool inInstance = gameState.InInstance;
            uint currentBgmId = gameState.CurrentBgmId;

            // Single per-tick state update. Multiple BaseLayers (one per
            // device) call this same method but the result is idempotent.
            RaidEffectState.UpdateState(inInstance, currentBgmId);

            if (RaidEffectState.dutyComplete || string.IsNullOrEmpty(zone) || zone == "???")
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var palette = RGBController.GetActivePalette();
            var ledArray = GetLedArray(layer);
            var overlay = GetOrCreateOverlay(layer.layerID, ledArray);
            var runningEffects = RGBController.GetRunningEffects();

            bool applied = ApplyRaidEffect(overlay, zone, palette, currentBgmId, runningEffects, layer);
            if (applied)
            {
                overlay.Attach(surface);
            }
            else
            {
                DetachOverlay(layer.layerID);
            }
        }

        public override void CleanupLayer(int layerID) => DetachOverlay(layerID);

        private ListLedGroup GetOrCreateOverlay(int layerID, Led[] ledArray)
        {
            if (_overlays.TryGetValue(layerID, out var existing))
            {
                // LED set may have changed (device swap, key rebind). Replace.
                if (!existing.SequenceEqual(ledArray))
                {
                    existing.RemoveLeds(existing);
                    existing.AddLeds(ledArray);
                }
                return existing;
            }

            var group = new ListLedGroup(surface, ledArray) { ZIndex = RaidOverlayZIndex };
            group.Detach();
            _overlays[layerID] = group;
            _gradientEffects[layerID] = new HashSet<LinearGradient>();
            return group;
        }

        private void DetachOverlay(int layerID)
        {
            if (!_overlays.TryGetValue(layerID, out var overlay)) return;

            if (_gradientEffects.TryGetValue(layerID, out var grads))
            {
                foreach (var g in grads) g.RemoveAllDecorators();
                grads.Clear();
            }

            overlay.RemoveAllDecorators();
            overlay.Brush = new SolidColorBrush(Color.Transparent);
            overlay.Detach();
        }

        private static void SetEffect(ILedGroupDecorator effect, ListLedGroup layer, List<ListLedGroup> runningEffects)
        {
            if (runningEffects.Contains(layer)) runningEffects.Remove(layer);

            layer.RemoveAllDecorators();
            layer.AddDecorator(effect);

            runningEffects.Add(layer);
        }

        private void SetLinearGradientEffect(LinearGradient gradient, IGradientDecorator effect, ListLedGroup layer, Size boundry, List<ListLedGroup> runningEffects, int layerID)
        {
            if (runningEffects.Contains(layer)) runningEffects.Remove(layer);

            layer.RemoveAllDecorators();
            gradient.WrapGradient = true;
            gradient.AddDecorator(effect);

            layer.Brush = new TextureBrush(new LinearGradientTexture(boundry, gradient));

            runningEffects.Add(layer);
            _gradientEffects[layerID].Add(gradient);
        }

        private void SetRadialGradientEffect(LinearGradient gradient, IGradientDecorator effect, ListLedGroup layer, Size boundry, List<ListLedGroup> runningEffects, int layerID)
        {
            if (runningEffects.Contains(layer)) runningEffects.Remove(layer);

            layer.RemoveAllDecorators();
            gradient.WrapGradient = true;
            gradient.AddDecorator(effect);

            layer.Brush = new TextureBrush(new ConicalGradientTexture(boundry, gradient));

            runningEffects.Add(layer);
            _gradientEffects[layerID].Add(gradient);
        }

        // Applies the per-zone raid decorator. Returns true if a raid effect
        // was applied (and the overlay should be attached), false otherwise.
        // Cases moved verbatim from ReactiveWeatherProcessor.SetReactiveWeather
        // so existing in-game behaviour is preserved.
        private bool ApplyRaidEffect(ListLedGroup layer, string zone, PaletteColorModel _colorPalette, uint currentBgmId, List<ListLedGroup> runningEffects, IMappingLayer masterlayer)
        {
            switch (zone)
            {
                case "Summit of Everkeep":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectEverkeepBase.Color);
                        var animationCol = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectEverkeepHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectEverkeepHighlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectEverkeepHighlight3.Color) };
                        var starfield = new BPMFastStarfieldDecorator(layer, layer.Count() / 6, 198, 80, animationCol, surface, 2.0, false, baseCol);

                        layer.Brush = new SolidColorBrush(baseCol);
                        SetEffect(starfield, layer, runningEffects);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Interphos":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectInterphosBase.Color);
                        var animationCol1 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectInterphosHighlight1.Color);
                        var animationCol2 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectInterphosHighlight2.Color);
                        var animationCol3 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectInterphosHighlight3.Color);

                        var animationGradient = new LinearGradient(new GradientStop(0f, baseCol),
                            new GradientStop(0.20f, animationCol1),
                            new GradientStop(0.35f, animationCol2),
                            new GradientStop(0.50f, animationCol3),
                            new GradientStop(0.65f, animationCol1),
                            new GradientStop(0.80f, animationCol2),
                            new GradientStop(0.95f, animationCol3));

                        var gradientMove = new MoveGradientDecorator(surface, 180, true);
                        SetRadialGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, masterlayer.layerID);
                        runningEffects.Add(layer);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Scratching Ring":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM1Base.Color);
                        var animationCol1 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM1Highlight1.Color);
                        var animationCol2 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM1Highlight2.Color);
                        var animationCol3 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM1Highlight3.Color);

                        var animationGradient = new LinearGradient(new GradientStop(0f, baseCol),
                            new GradientStop(0.20f, animationCol1),
                            new GradientStop(0.35f, animationCol2),
                            new GradientStop(0.50f, animationCol3),
                            new GradientStop(0.65f, animationCol1),
                            new GradientStop(0.80f, animationCol2),
                            new GradientStop(0.95f, animationCol3));

                        var gradientMove = new MoveBPMGradientDecorator(surface, 125 / 4, true);
                        SetRadialGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, masterlayer.layerID);
                        runningEffects.Add(layer);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Lovely Lovering":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM2Base.Color);
                        var animationCol = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM2Highlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM2Highlight2.Color) };
                        var arenaLightShow = new ArenaLightShowDecorator(layer, 20, 3.0, 1.0, animationCol, surface, false, baseCol);

                        layer.Brush = new SolidColorBrush(baseCol);
                        SetEffect(arenaLightShow, layer, runningEffects);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Blasting Ring":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM3Base.Color);
                        var animationCol1 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM3Highlight1.Color);
                        var animationCol2 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM3Highlight2.Color);
                        var animationCol3 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM3Highlight3.Color);

                        var animationGradient = new LinearGradient(new GradientStop(0f, baseCol),
                            new GradientStop(0.20f, animationCol1),
                            new GradientStop(0.35f, animationCol2),
                            new GradientStop(0.50f, animationCol3),
                            new GradientStop(0.65f, animationCol1),
                            new GradientStop(0.80f, animationCol2),
                            new GradientStop(0.95f, animationCol3));

                        var gradientMove = new MoveBPMDiagonalGradientDecorator(surface, 110 / 4, DiagonalDirection.TopLeftToBottomRight);
                        SetLinearGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, masterlayer.layerID);
                        runningEffects.Add(layer);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "The Thundering":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM4Base.Color);
                        var animationCol = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM4Highlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM4Highlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM4Highlight3.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM4Highlight4.Color) };
                        var bpmArenaLightShow = new BPMPWMDecorator(layer, 162, 1.0, animationCol, 0.10, 4, surface, false, baseCol);

                        layer.Brush = new SolidColorBrush(baseCol);
                        SetEffect(bpmArenaLightShow, layer, runningEffects);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Sphere of Naught":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectCoDBase.Color);
                        var animationCol = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectCoDHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectCoDHighlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectCoDHighlight3.Color) };
                        var arenaLightShow = new ArenaLightShowDecorator(layer, 20, 3.0, 1.0, animationCol, surface, false, baseCol);

                        layer.Brush = new SolidColorBrush(baseCol);
                        SetEffect(arenaLightShow, layer, runningEffects);
                        RaidEffectState.raidEffectsRunning = true;
                        masterlayer.requestUpdate = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Groovy Ring":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM5Base.Color);
                        var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM5Highlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM5Highlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM5Highlight3.Color) };
                        var ripple = new BPMRippleDecorator(layer, 60, 2, 5, colors, surface, baseCol);

                        SetEffect(ripple, layer, runningEffects);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Rebel Ring":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM6Base.Color);
                        var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM6Highlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM6Highlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM6Highlight3.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM6Highlight4.Color) };
                        var chase = new BPMChaseDecorator(layer, 160, 5, colors, surface, baseCol);

                        SetEffect(chase, layer, runningEffects);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Demolition Site":
                    if (!RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Base.Color);
                        var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Highlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Highlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Highlight3.Color) };
                        var ripple = new BPMRippleDecorator(layer, 178, 2, 2, colors, surface, baseCol);

                        SetEffect(ripple, layer, runningEffects);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                // BGM-based phase switching demo. Test zones (Mist / Limsa) let
                // the effect fire in open-world for visual development.
                case "Hunter's Ring":
                case "Hunting Ground":
                case "Mist":
                case "Limsa Lominsa Lower Decks":
                    if (!RaidEffectState.raidEffectsRunning ||
                        (currentBgmId != 0 && currentBgmId != RaidEffectState.currentRaidBgmId))
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Base.Color);
                        var colors = new Color[]
                        {
                            ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Highlight1.Color),
                            ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Highlight2.Color),
                            ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Highlight3.Color)
                        };

                        switch (currentBgmId)
                        {
                            case 186u: // TODO: replace with real phase-2 BGM ID
                            {
                                var chase = new BPMChaseDecorator(layer, 178, 2, colors, surface, baseCol);
                                SetEffect(chase, layer, runningEffects);
                                break;
                            }
                            default:
                            {
                                var ripple = new BPMRippleDecorator(layer, 178, 2, 2, colors, surface, baseCol);
                                SetEffect(ripple, layer, runningEffects);
                                break;
                            }
                        }

                        RaidEffectState.raidEffectsRunning = true;
                        RaidEffectState.currentRaidBgmId = currentBgmId;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;
            }

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var overlay in _overlays.Values)
                    {
                        overlay.RemoveAllDecorators();
                        overlay.Detach();
                    }
                    _overlays.Clear();
                    foreach (var grads in _gradientEffects.Values)
                    {
                        foreach (var g in grads) g.RemoveAllDecorators();
                        grads.Clear();
                    }
                    _gradientEffects.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }
    }
}
