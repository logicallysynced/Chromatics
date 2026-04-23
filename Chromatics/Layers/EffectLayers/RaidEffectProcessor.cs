using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Chromatics.Layers
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
        // Layer IDs whose user-side base groups must stay detached while the
        // raid overlay is active. Detached on every surface.Updating tick (not
        // just every game-loop tick) so the base processor's ~5fps re-attach
        // can't slip through the ~50-100Hz surface render before the next
        // game-loop suppress fires. Without this hook the user briefly sees
        // their normal base layer between game-loop ticks.
        private readonly HashSet<int> _suppressedLayerIds = new();
        private readonly Lock _suppressLock = new();
        private bool _hookedUpdating;
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
                // Reset run state so re-enabling the toggle mid-fight triggers
                // a fresh ApplyRaidEffect rebuild instead of seeing raidEffectsRunning=true
                // from a prior session and skipping the zone case's init guard.
                RaidEffectState.raidEffectsRunning = false;
                RaidEffectState.currentRaidBgmId = 0;
                RaidEffectState.delayedStartUntil = DateTime.MinValue;
                RaidEffectState.pendingDelaySec = 0;
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
            RaidEffectState.UpdateState(inInstance, currentBgmId, zone);

            if (RaidEffectState.dutyComplete || string.IsNullOrEmpty(zone) || zone == "???")
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var palette = RGBController.GetActivePalette();
            var ledArray = GetLedArray(layer);
            var overlay = GetOrCreateOverlay(layer.layerID, ledArray);

            bool hasMusic = currentBgmId != 0 && currentBgmId != RaidEffectState.SilenceBgmId;

            // Phase trigger (global): mid-fight BGM dropped to silence with a raid
            // effect already running and real music previously observed. Cut the
            // running effect to black instantly, then hold until music returns +
            // the configured transition delay. Reset raidEffectsRunning so the
            // post-hold ApplyRaidEffect rebuilds the decorator fresh.
            if (RaidEffectState.delayedStartUntil == DateTime.MinValue
                && RaidEffectState.raidEffectsRunning
                && !hasMusic
                && RaidEffectState.lastSeenBgmId != 0)
            {
                RaidEffectState.delayedStartUntil = DateTime.MaxValue;
                RaidEffectState.pendingDelaySec = RaidEffectState.GetInstanceTransitionDelay(zone);
                RaidEffectState.raidEffectsRunning = false;
            }

            // Entry trigger (global): zoned into an instance with no music yet and no
            // effect running. Cut the base layer to black instantly, then hold until
            // music starts + the configured start delay. Fires for any instance; if
            // ApplyRaidEffect returns false for this zone the hold expires and the
            // normal base layer resumes.
            //
            // This also doubles as a stability gate on zone-in: BGM often blips to
            // silence briefly during loading finalization. Without the hold-black,
            // ApplyRaidEffect would start the effect on tick 1, then the phase
            // trigger would fire on the silence blip and rebuild, causing a visible
            // restart. Holding black until the first stable music tick avoids that.
            if (RaidEffectState.delayedStartUntil == DateTime.MinValue
                && inInstance
                && !RaidEffectState.raidEffectsRunning
                && !hasMusic)
            {
                RaidEffectState.delayedStartUntil = DateTime.MaxValue;
                RaidEffectState.pendingDelaySec = RaidEffectState.GetInstanceStartDelay(zone);
            }

            // Music arrived while we were holding black indefinitely. Convert to a
            // finite hold using the delay captured at trigger time.
            if (RaidEffectState.delayedStartUntil == DateTime.MaxValue && hasMusic)
            {
                RaidEffectState.delayedStartUntil = DateTime.UtcNow.AddSeconds(RaidEffectState.pendingDelaySec);
            }

            // Hold-black gate: paint solid black, suppress the user's base layer
            // group, but slot our overlay at the SAME ZIndex as the user's base
            // layer (not the high RaidOverlayZIndex). That way any dynamic layers
            // the user has stacked above the base — gauges, castbars, raid
            // highlights, etc. — keep rendering over our black instead of being
            // hidden by it. Only the base layer slot is blacked out.
            if (RaidEffectState.delayedStartUntil != DateTime.MinValue
                && (RaidEffectState.delayedStartUntil == DateTime.MaxValue
                    || DateTime.UtcNow < RaidEffectState.delayedStartUntil))
            {
                overlay.RemoveAllDecorators();
                overlay.ZIndex = layer.zindex;
                overlay.Brush = new SolidColorBrush(new Color((byte)255, (byte)0, (byte)0, (byte)0));
                overlay.Attach(surface);
                SuppressBaseLayerGroups(layer.layerID);
                return;
            }
            if (RaidEffectState.delayedStartUntil != DateTime.MinValue)
            {
                RaidEffectState.delayedStartUntil = DateTime.MinValue;
                RaidEffectState.pendingDelaySec = 0;
            }

            // Don't apply raid effects outside an instance. During a loading-screen
            // transition the zone name can briefly still read as the raid zone while
            // inInstance is already false; UpdateState has reset raidEffectsRunning=false
            // at that point, so ApplyRaidEffect would rebuild the decorator and produce
            // a one-tick artifact of the previous raid effect before the overlay detaches.
            if (!inInstance)
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var runningEffects = RGBController.GetRunningEffects();

            bool applied = ApplyRaidEffect(overlay, zone, palette, currentBgmId, runningEffects, layer);
            if (applied)
            {
                SuppressBaseLayerGroups(layer.layerID);

                if (overlay.Decorators.Count > 0)
                {
                    // Decorator effect: BPMRipple/BPMChase/etc. write LEDs directly via
                    // surface.Updating. Their OnAttached already detached the overlay so
                    // brushes don't overwrite the writes. Don't re-attach — a transparent
                    // brush at ZIndex 500 would override every lower layer (including
                    // DamageFlash) with (0,0,0,0). Matches ReactiveWeather's pattern of
                    // leaving the decorator-driven group detached.
                }
                else
                {
                    // Gradient: decorator lives on the LinearGradient, not the overlay.
                    // TextureBrush is already set. Render at the base layer's ZIndex so
                    // dynamic layers paint over it — same behaviour as ReactiveWeather.
                    overlay.ZIndex = layer.zindex;
                    overlay.Attach(surface);
                }
            }
            else
            {
                DetachOverlay(layer.layerID);
            }
        }

        public override void CleanupLayer(int layerID) => DetachOverlay(layerID);

        // Mark layerID as suppressed and detach its base-layer groups now. The
        // surface.Updating hook re-detaches every render tick so the base
        // processor's re-attach (game-loop ~5fps) can't briefly leak between
        // suppress fires (~50-100Hz surface render). DetachOverlay clears the
        // mark when the overlay tears down.
        private void SuppressBaseLayerGroups(int layerID)
        {
            EnsureUpdatingHook();
            lock (_suppressLock)
            {
                _suppressedLayerIds.Add(layerID);
            }
            var liveGroups = RGBController.GetLiveLayerGroups();
            if (!liveGroups.TryGetValue(layerID, out var groups)) return;
            foreach (var g in groups)
            {
                g?.Detach();
            }
        }

        private void EnsureUpdatingHook()
        {
            if (_hookedUpdating || surface == null) return;
            surface.Updating += OnSurfaceUpdating;
            _hookedUpdating = true;
        }

        private void OnSurfaceUpdating(UpdatingEventArgs args)
        {
            var liveGroups = RGBController.GetLiveLayerGroups();
            lock (_suppressLock)
            {
                foreach (var id in _suppressedLayerIds)
                {
                    if (liveGroups.TryGetValue(id, out var groups))
                    {
                        foreach (var g in groups) g?.Detach();
                    }
                }
            }
        }

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
            lock (_suppressLock)
            {
                _suppressedLayerIds.Remove(layerID);
            }

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

                        layer.Brush = new SolidColorBrush(baseCol);
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

                        layer.Brush = new SolidColorBrush(baseCol);
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

                        layer.Brush = new SolidColorBrush(baseCol);
                        SetEffect(ripple, layer, runningEffects);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "Hunter's Ring":
                case "Hunting Ground":
                //case "Akh Afah Amphitheatre":
                    if (!RaidEffectState.raidEffectsRunning || currentBgmId != RaidEffectState.currentRaidBgmId)
                    {
                        layer.Brush = new SolidColorBrush(baseCol);
                        switch (currentBgmId)
                        {
                            case 20150:
                            {
                                var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8SBase.Color);
                                var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8SHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8SHighlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8SHighlight3.Color) };
                                var pulse = new BPMCircularPulseEffect(layer, 164, 4, 12, 2, colors, surface, baseCol);

                                SetEffect(pulse, layer, runningEffects);
                                break;
                            }
                            default: //20149
                            {
                                var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8Base.Color);
                                var animationCol = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8Highlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8Highlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8Highlight3.Color) };
                                var starfield = new BPMStarfieldDecorator(layer, layer.Count() / 6, 272, 500, animationCol, surface, 2, false, baseCol);

                                layer.Brush = new SolidColorBrush(baseCol);
                                SetEffect(starfield, layer, runningEffects);
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
                    if (_hookedUpdating && surface != null)
                    {
                        surface.Updating -= OnSurfaceUpdating;
                        _hookedUpdating = false;
                    }
                    lock (_suppressLock)
                    {
                        _suppressedLayerIds.Clear();
                    }
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
