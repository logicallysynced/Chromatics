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

        // Mirrors DamageFlash.FlashPriorityZIndex (private over there). Used
        // by the Arcadia case to bump the overlay above every other layer
        // for the duration of its scripted red flash.
        private const int FlashPriorityZIndex = 700;

        // Dev-only switch: when true, the Cutscene Viewer in Private Mansion
        // - Mist is treated as an in-instance state for the Arcadia case so
        // the opening choreography can be iterated without zoning into the
        // actual encounter. Keep false for production.
        private const bool CutsceneViewerTestMode = false;

        // One-off choreography state for the Arcadia raid encounter.
        // Phase 1 (BGM 20241): Effect 1 plays for 11.5s, then a 0.5s
        // full-device red flash, then Effect 2 for the rest of the phase.
        // Phase 2 (BGM 20242) uses the standard phase-change rebuild path.
        // Static lifetime intentional — there's only ever one in-game
        // encounter active at a time, and zone changes reset via the
        // freshStart branch of the case body.
        private static class ArcadiaState
        {
            public const string ZoneName = "Arcadia";
            public const uint Phase1BgmId = 20241;
            public const uint Phase2BgmId = 20242;
            public const double Phase1FlashAtSec = 10.8;
            public const double FlashDurationSec = 0.4;

            // phase1Start is genuinely global — the in-game timer is one
            // countdown shared across every device in the choreography. The
            // flash and post-flash transitions, however, are PER-LAYER:
            // each device's overlay progresses through its own
            // Effect 1 → Flash → Effect 2 pipeline. Using global flags
            // caused the first layer's transition to "consume" the state
            // so subsequent layers skipped both branches and stayed
            // stuck on Effect 1.
            public static DateTime phase1Start = DateTime.MinValue;
            public static readonly HashSet<int> layersFlashed = [];
            public static readonly Dictionary<int, DateTime> flashEndByLayer = [];
            public static readonly HashSet<int> layersPostFlashApplied = [];

            public static void Reset()
            {
                phase1Start = DateTime.MinValue;
                layersFlashed.Clear();
                flashEndByLayer.Clear();
                layersPostFlashApplied.Clear();
            }
        }

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

            // Cutscene-viewer test mode (off by default for production).
            // When true, watching a cutscene in Private Mansion - Mist is
            // treated as if the player were in an instance, which lets the
            // Arcadia opening choreography fire from the Cutscene Viewer
            // for iterating without zoning into the actual encounter. The
            // Cutscene Viewer (Mist, MapId 284) is not normally an instance
            // so the standard Arcadia path skips it. CutsceneAnimation's
            // painter is also gated on `inCutscene && !inInstance`, so the
            // override also stops it from competing with the raid overlay.
            // Set to false before shipping.
            if (CutsceneViewerTestMode
                && gameState.WatchingCutscene
                && zone == "Private Mansion - Mist")
            {
                inInstance = true;
            }

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
                    //
                    // Skip the attach + ZIndex reset if the case body has already
                    // attached the overlay itself (e.g. Arcadia's flash window
                    // pins it at FlashPriorityZIndex). Otherwise this branch
                    // would clobber the elevated ZIndex back down to layer.zindex.
                    if (overlay.Surface == null)
                    {
                        overlay.ZIndex = layer.zindex;
                        overlay.Attach(surface);
                    }
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
            // Each base layer has its own per-device overlay (`layer`), so
            // every case-body's "should I build?" check is per-overlay
            // (Decorators.Count == 0) rather than the global
            // raidEffectsRunning flag. That lets every device build its
            // own decorator independently — keyboards, mice, headsets,
            // Hue bulbs all get the raid effect on their own overlay.
            // The global flag is still set/cleared so Process's entry
            // and phase-transition gates know whether anything is active,
            // but it no longer gates per-layer rebuilds.

            switch (zone)
            {
                case "Summit of Everkeep":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
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
                    // Gradient effects: the decorator lives on the LinearGradient,
                    // not on the overlay `layer` — so layer.Decorators.Count is
                    // ALWAYS 0 here. Gating on that alone rebuilds the gradient +
                    // decorator every tick, which produces flicker + runaway
                    // speed. Gate on the presence of the TextureBrush instead:
                    // once SetRadialGradientEffect installs it, this overlay is
                    // already painting and doesn't need rebuilding until
                    // raidEffectsRunning drops.
                    if (layer.Brush is not TextureBrush || !RaidEffectState.raidEffectsRunning)
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
                    // Gradient effect — see Interphos for rationale on
                    // gating by brush type instead of Decorators.Count.
                    if (layer.Brush is not TextureBrush || !RaidEffectState.raidEffectsRunning)
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
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
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
                    // Gradient effect — see Interphos for rationale on
                    // gating by brush type instead of Decorators.Count.
                    if (layer.Brush is not TextureBrush || !RaidEffectState.raidEffectsRunning)
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

                        var gradientMove = new MoveBPMDiagonalGradientDecorator(surface, 27, DiagonalDirection.TopLeftToBottomRight);
                        SetLinearGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, masterlayer.layerID);
                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;

                case "The Thundering":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
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
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
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
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
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
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
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
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
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
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning || currentBgmId != RaidEffectState.currentRaidBgmId)
                    {
                        switch (currentBgmId)
                        {
                            case 20150:
                            {
                                var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8SBase.Color);
                                var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8SHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8SHighlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM8SHighlight3.Color) };
                                var pulse = new BPMCircularPulseEffect(layer, 164, 4, 12, 2, colors, surface, baseCol);

                                layer.Brush = new SolidColorBrush(baseCol);
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
                
                //USED FOR TESTING
                /*
                case "Akh Afah Amphitheatre":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning || currentBgmId != RaidEffectState.currentRaidBgmId)
                    {
                        switch (currentBgmId)
                        {
                            default: //20149
                            {
                                var baseCol = new Color(0, 0, 0);
                                var animationCol = new Color[] { new Color(255, 255, 255) };
                                var starfield = new BPMStarfieldDecorator(layer, 6, 360, 2000, animationCol, surface, 1, false, baseCol);

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
                */
                //M12/M12S
                case ArcadiaState.ZoneName:
                {
                    // Custom multi-stage choreography. Phase 1 (BGM 20241):
                    // Effect 1 plays for 11.5s, then a 0.5s full-device red
                    // flash, then Effect 2 for the rest of phase 1. Phase 2
                    // (BGM 20242): single Effect uses the standard
                    // raidEffectsRunning rebuild path. Both effects are
                    // placeholders pending final design.
                    // Suppress freshStart while this layer is mid-
                    // choreography (flashed but Effect 2 not yet built).
                    // During the flash window we RemoveAllDecorators and
                    // hold a red brush, so Decorators.Count == 0 — without
                    // this guard the next tick would treat that as "needs
                    // rebuild", wipe ArcadiaState, and restart Effect 1
                    // instead of letting the per-tick branch finish the
                    // flash → Effect 2 transition.
                    int lidForFresh = masterlayer.layerID;
                    bool midChoreography = ArcadiaState.layersFlashed.Contains(lidForFresh)
                        && !ArcadiaState.layersPostFlashApplied.Contains(lidForFresh);

                    bool freshStart = !midChoreography
                        && (layer.Decorators.Count == 0
                            || !RaidEffectState.raidEffectsRunning
                            || currentBgmId != RaidEffectState.currentRaidBgmId);

                    if (freshStart)
                    {
                        ArcadiaState.Reset();
                        // Reset overlay ZIndex in case the previous tick left
                        // it at FlashPriorityZIndex (e.g. zone-out mid-flash).
                        layer.ZIndex = RaidOverlayZIndex;

                        switch (currentBgmId)
                        {
                            case ArcadiaState.Phase1BgmId:
                            {
                                ArcadiaState.phase1Start = DateTime.UtcNow;

                                // Phase 1 Effect 1 - Intro

                                var baseCol = new Color(0, 0, 0);
                                var animationCol = new Color[] { new Color(255, 255, 255) };
                                var starfield = new BPMStarfieldDecorator(layer, 6, 360, 2000, animationCol, surface, 1, false, baseCol);

                                layer.Brush = new SolidColorBrush(baseCol);
                                SetEffect(starfield, layer, runningEffects);
                                break;
                            }
                            case ArcadiaState.Phase2BgmId:
                            {
                                // Phase 2 effect.
                                var baseCol = new Color(0, 0, 0);
                                var colors = new Color[] { new Color(255, 0, 4), new Color(93, 0, 255), new Color(255, 117, 0), new Color(249, 255, 0) };
                                var pulse = new BPMCircularPulseEffect(layer, 165, 4, 12, 1, colors, surface, baseCol);

                                layer.Brush = new SolidColorBrush(baseCol);
                                SetEffect(pulse, layer, runningEffects);

                                
                                break;
                            }
                            default:
                            {
                                // Unknown BGM in Arcadia — e.g. ambient pre-pull
                                // music, or the cutscene-viewer ticks before the
                                // encounter track starts. Paint the overlay
                                // solid black at the base layer ZIndex so the
                                // user's normal base layer doesn't poke through;
                                // the choreography is scripted to begin at the
                                // exact moment Phase1BgmId hits, not whatever
                                // ambient cue is playing beforehand.
                                //
                                // We DON'T set raidEffectsRunning=true so that
                                // freshStart stays true and the switch is re-
                                // entered every tick — that lets a later
                                // transition to Phase1BgmId / Phase2BgmId fire
                                // the choreography normally.
                                //
                                // Duty complete (Victory Fanfare BGM 18) is
                                // handled at the top of Process via
                                // dutyComplete → DetachOverlay → base layer
                                // renders, so this black hold ends naturally
                                // when the fight finishes.
                                layer.RemoveAllDecorators();
                                layer.Brush = new SolidColorBrush(new Color((byte)255, (byte)0, (byte)0, (byte)0));
                                layer.ZIndex = masterlayer.zindex;
                                return true;
                            }
                        }

                        RaidEffectState.raidEffectsRunning = true;
                        RaidEffectState.currentRaidBgmId = currentBgmId;
                        return true;
                    }

                    // Per-tick choreography during phase 1: timer, flash,
                    // then Effect 2. Per-layer state so each device's
                    // overlay (keyboard / mouse / Hue / etc.) walks
                    // through the pipeline independently.
                    if (currentBgmId == ArcadiaState.Phase1BgmId
                        && ArcadiaState.phase1Start != DateTime.MinValue)
                    {
                        var elapsed = (DateTime.UtcNow - ArcadiaState.phase1Start).TotalSeconds;
                        int lid = masterlayer.layerID;
                        bool thisLayerFlashed = ArcadiaState.layersFlashed.Contains(lid);
                        bool thisLayerPostFlash = ArcadiaState.layersPostFlashApplied.Contains(lid);

                        if (!thisLayerFlashed && elapsed >= ArcadiaState.Phase1FlashAtSec)
                        {
                            // Stop Effect 1 on THIS overlay and start its
                            // red flash. Bumped above DamageFlash so the
                            // red visibly covers every dynamic / highlight
                            // layer. Manual Attach because the standard
                            // post-Apply path would otherwise reset ZIndex.
                            layer.RemoveAllDecorators();
                            layer.Brush = new SolidColorBrush(new Color((byte)255, (byte)255, (byte)0, (byte)0));
                            layer.ZIndex = FlashPriorityZIndex;
                            layer.Attach(surface);

                            ArcadiaState.flashEndByLayer[lid] = DateTime.UtcNow.AddSeconds(ArcadiaState.FlashDurationSec);
                            ArcadiaState.layersFlashed.Add(lid);
                            return true;
                        }

                        if (thisLayerFlashed && !thisLayerPostFlash)
                        {
                            if (ArcadiaState.flashEndByLayer.TryGetValue(lid, out var end)
                                && DateTime.UtcNow < end)
                            {
                                // Hold the red brush on THIS overlay for
                                // the rest of its flash window. Already
                                // attached above on this layer's flash tick.
                                return true;
                            }

                            // Flash done for this layer — drop ZIndex back
                            // and build Effect 2 on this overlay.
                            layer.ZIndex = RaidOverlayZIndex;

                            // Phase 1 Effect 2
                            var baseCol = new Color(1, 40, 5);
                            var colors = new Color[] { new Color(255, 220, 180), new Color(0, 255, 43) };
                            var strike = new BPMThunderstrikeEffect(layer, 360, 4, 0.6, colors, surface, baseCol);

                            layer.Brush = new SolidColorBrush(baseCol);
                            SetEffect(strike, layer, runningEffects);

                            ArcadiaState.layersPostFlashApplied.Add(lid);
                            return true;
                        }
                    }

                    return RaidEffectState.raidEffectsRunning;
                }
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
