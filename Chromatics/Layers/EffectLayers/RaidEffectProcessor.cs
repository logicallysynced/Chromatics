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

            // Time from phase1Start to the scripted full-device red flash.
            // Three values because the music timeline differs depending
            // on the gate's clear path:
            //   - Phase1FlashAtSec: clean entry — no cutscene observed,
            //     gate cleared via the standard GateDelaySec settle.
            //   - Phase1FlashAtSecCutsceneEventScene: scene 0 (Event)
            //     started playing the raid music inside the cutscene.
            //   - Phase1FlashAtSecCutsceneEnd: CUTSCENE_END_BYPASS path,
            //     where the cutscene ended and the duty track cross-fades
            //     in afterwards.
            // Tune each independently if the music timeline shifts
            // between paths.
            public const double Phase1FlashAtSec = 11.0;
            public const double Phase1FlashAtSecCutsceneEventScene = 11.0;
            public const double Phase1FlashAtSecCutsceneEnd = 9.5;
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

            // The flash time selected at gate-clear based on the bypass
            // path. Set once when phase1Start is captured and read by
            // the per-tick choreography to fire the flash.
            public static double activePhase1FlashAtSec = Phase1FlashAtSec;

            public static void Reset()
            {
                phase1Start = DateTime.MinValue;
                layersFlashed.Clear();
                flashEndByLayer.Clear();
                layersPostFlashApplied.Clear();
                activePhase1FlashAtSec = Phase1FlashAtSec;
            }
        }

        // BGM-trigger gate. FFXIV's BGM id transitions to the raid track
        // id at the moment the player enters the instance. Capture the
        // first tick we see the expected BGM and apply a generic settle
        // delay (GateDelaySec) before allowing the downstream effect to
        // build. Re-detection on the same (zone, bgm) is a no-op until
        // reset via Reset() (called when the player leaves the instance).
        private static class BgmTriggerGate
        {
            // Generic settle delay (seconds) applied between the FIRST
            // tick we see the target BGM and the moment IsReady returns
            // true. Used when no cutscene was ever observed for this
            // (zone, bgm).
            public const double GateDelaySec = 1.25;

            // Settle delay applied specifically after CUTSCENE_END_BYPASS
            // — the moment WatchingCutscene flips false on a key that was
            // latched. Independent from GateDelaySec because the audio
            // transition shape post-cutscene differs from a clean entry
            // (the game cross-fades from cutscene audio into the duty
            // track), so this can be tuned separately.
            // CUTSCENE_EVENT_SCENE_BYPASS still fires immediately — the
            // raid music is already audible inside the cutscene, no
            // settle needed.
            public const double CutsceneEndBypassDelaySec = 0.0;

            // Keyed by "{zone}:{bgmId}" so each phase transition within
            // a multi-phase fight (Arcadia P1 vs P2) gets its own capture.
            private static readonly Dictionary<string, DateTime> _startBy = new();

            // WatchingCutscene goes true even on clean entry (loading-screen
            // blip during zone-in), so we can't latch the moment we see it.
            // Instead: when WC first goes true for a key, record the start
            // time. Only treat it as a real cutscene once it's stayed true
            // for >= CutsceneObservationThresholdSec. Below threshold a brief
            // flip-true→false is treated as a clean entry.
            public const double CutsceneObservationThresholdSec = 4.0;

            // Per-key state. _cutsceneStart is the moment WC first went
            // true (cleared when WC goes false WITHOUT having latched, so a
            // future cutscene can re-arm the timer). _cutsceneObserved is
            // the latched "this is a real cutscene" flag — set only after
            // crossing the threshold, never cleared except by Reset().
            // _cutsceneEndBypassCaptured is a one-shot per-key guard so
            // the END_BYPASS branch only writes _startBy[key] once;
            // without it every subsequent WC=false tick would push the
            // start time forward and the gate would never clear.
            private static readonly Dictionary<string, DateTime> _cutsceneStart = new();
            private static readonly HashSet<string> _cutsceneObserved = new();
            private static readonly HashSet<string> _cutsceneEndBypassCaptured = new();

            // Variant of IsReady that also gates on cutscene state. The
            // latch behaviour is threshold-based to avoid misclassifying
            // the brief WatchingCutscene blips that occur during loading
            // screens on a clean entry:
            //
            //   - WC stays false: clean entry → IsReady (GateDelaySec).
            //   - WC goes true & timer < CutsceneObservationThresholdSec:
            //     hold black, decision deferred — could still be a brief
            //     loading-screen blip.
            //   - WC stays true past the threshold: latch as a real
            //     cutscene. Now scene-0 bypass is checked (raid music
            //     playing inside the cutscene → CUTSCENE_EVENT_SCENE_BYPASS,
            //     fire immediately).
            //   - WC went true → false BEFORE threshold: that was a blip.
            //     Clear the timer, treat as clean entry → IsReady.
            //   - WC went true → false AFTER latching: cutscene was
            //     skipped or completed → CUTSCENE_END_BYPASS (uses
            //     CutsceneEndBypassDelaySec).
            //
            // cutsceneViewerTestMode short-circuits the threshold (testers
            // iterating inside the Cutscene Viewer want the cutscene
            // confirmed immediately).
            public static bool IsReadyWithCutsceneBypass(string zone, uint bgmId, bool watchingCutscene, ushort eventScenePlayingBgmId, bool cutsceneViewerTestMode)
            {
                var key = zone + ":" + bgmId;

                if (watchingCutscene)
                {
                    // Suspend any in-flight GateDelaySec capture while WC
                    // is true. Clean-entry timing must not run in parallel
                    // with cutscene observation. If this turns out to be a
                    // sub-threshold blip, GateDelaySec gets re-captured
                    // fresh from the moment WC flips false (via IsReady's
                    // lazy capture on the WC=false path below). If the
                    // cutscene is confirmed, END_BYPASS captures
                    // CutsceneEndBypassDelaySec instead.
                    _startBy.Remove(key);

                    if (!_cutsceneStart.TryGetValue(key, out var cutsceneStart))
                    {
                        cutsceneStart = DateTime.UtcNow;
                        _cutsceneStart[key] = cutsceneStart;
                    }

                    var observedFor = (DateTime.UtcNow - cutsceneStart).TotalSeconds;
                    bool aboveThreshold = cutsceneViewerTestMode || observedFor >= CutsceneObservationThresholdSec;

                    if (aboveThreshold)
                        _cutsceneObserved.Add(key);

                    if (_cutsceneObserved.Contains(key) && eventScenePlayingBgmId == bgmId)
                        return true;

                    return false;
                }

                // WC is false now.
                if (_cutsceneObserved.Contains(key))
                {
                    // Confirmed cutscene that ended → END_BYPASS path.
                    // Overwrite the GateDelaySec capture with the
                    // cutscene-specific delay. Guarded by
                    // _cutsceneEndBypassCaptured so subsequent ticks don't keep
                    // pushing the start time forward.
                    if (_cutsceneEndBypassCaptured.Add(key))
                    {
                        _startBy[key] = DateTime.UtcNow.AddSeconds(CutsceneEndBypassDelaySec);
                    }
                    return IsReady(zone, bgmId);
                }

                // WC was either never true, or was true but ended under
                // threshold (a loading-screen blip). Either way → clean
                // entry. GateDelaySec was NOT running during any WC=true
                // window (suspended at the top of this method), so
                // IsReady's lazy capture starts the timer fresh from the
                // moment of this WC=false call. Clear the under-threshold
                // start so a later real cutscene on the same key can re-
                // arm the timer.
                _cutsceneStart.Remove(key);

                return IsReady(zone, bgmId);
            }

            public static bool IsReady(string zone, uint bgmId)
            {
                var key = zone + ":" + bgmId;
                if (!_startBy.TryGetValue(key, out var startTime))
                {
                    startTime = DateTime.UtcNow.AddSeconds(GateDelaySec);
                    _startBy[key] = startTime;
                }
                return DateTime.UtcNow >= startTime;
            }

            // True if this (zone, bgm) was latched as having a cutscene
            // observed during its wait — i.e. the gate cleared (or will
            // clear) via a CUTSCENE_* path rather than a clean entry.
            // Callers use this at the gate-clear site to pick between
            // clean-entry and post-cutscene timeline values.
            public static bool WasCutsceneObserved(string zone, uint bgmId)
            {
                return _cutsceneObserved.Contains(zone + ":" + bgmId);
            }

            public static void Reset()
            {
                _startBy.Clear();
                _cutsceneStart.Clear();
                _cutsceneObserved.Clear();
                _cutsceneEndBypassCaptured.Clear();
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
            // Raw cutscene flag (before the CutsceneViewerTestMode override
            // to inInstance below). Threaded into ApplyRaidEffect so the
            // Arcadia case can hold black while the player is mid-cutscene
            // even after the BGM id has flipped to Phase1BgmId.
            bool watchingCutscene = gameState.WatchingCutscene;
            // Scene 0 (Event) PlayingBgmId — used by IsReadyWithCutsceneBypass
            // to detect "cutscene is audibly playing the raid music" mid-
            // cutscene (user didn't skip). 0 if BgmScenes hasn't resolved.
            ushort eventScenePlayingBgmId = (gameState.BgmScenes != null && gameState.BgmScenes.Count > 0)
                ? gameState.BgmScenes[0].PlayingBgmId
                : (ushort)0;

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
                // Duty finished or zone unresolved — wipe per-encounter
                // choreography state so the next instance starts clean.
                ArcadiaState.Reset();
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
                // Player left the instance — clear cached BGM-trigger
                // captures so the next instance's first tick re-captures
                // scene/resume cleanly. Also wipe per-encounter
                // choreography state so the next encounter starts at t=0
                // rather than resuming with a stale phase1Start.
                BgmTriggerGate.Reset();
                ArcadiaState.Reset();
                return;
            }

            var runningEffects = RGBController.GetRunningEffects();

            bool applied = ApplyRaidEffect(overlay, zone, palette, currentBgmId, watchingCutscene, eventScenePlayingBgmId, runningEffects, layer);
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
        private bool ApplyRaidEffect(ListLedGroup layer, string zone, PaletteColorModel _colorPalette, uint currentBgmId, bool watchingCutscene, ushort eventScenePlayingBgmId, List<ListLedGroup> runningEffects, IMappingLayer masterlayer)
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
                //USED FOR TESTING
                /*
                case "The Interdimensional Rift":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning || currentBgmId != RaidEffectState.currentRaidBgmId)
                    {
                        if (!BgmTriggerGate.IsReadyWithCutsceneBypass(zone, currentBgmId, watchingCutscene, eventScenePlayingBgmId, CutsceneViewerTestMode))
                        {
                            layer.RemoveAllDecorators();
                            layer.Brush = new SolidColorBrush(new Color((byte)255, (byte)0, (byte)0, (byte)0));
                            layer.ZIndex = masterlayer.zindex;
                            return true;
                        }

                        switch (currentBgmId)
                        {
                            case 587: //20149 //587
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
                        // BGM-trigger scene gate — first tick of this
                        // (zone, bgm) captures scene + fade-in, computes
                        // start time. Hold black until ready. Don't set
                        // raidEffectsRunning so freshStart re-checks
                        // each tick.
                        if (!BgmTriggerGate.IsReady(zone, currentBgmId))
                        {
                            layer.RemoveAllDecorators();
                            layer.Brush = new SolidColorBrush(new Color((byte)255, (byte)0, (byte)0, (byte)0));
                            layer.ZIndex = masterlayer.zindex;
                            return true;
                        }

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
                case "Ring Noir":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM9Base.Color);
                        var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM9KeyHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM9KeyHighlight2.Color) };
                        var heartbeat = new BPMHeartbeatEffect(layer, 47, 2, colors, surface, baseCol);

                        SetEffect(heartbeat, layer, runningEffects);

                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;
                case "The X-Ring":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM10Base.Color);
                        var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM10KeyHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM10KeyHighlight2.Color) };
                        var pulse = new BPMCircularPulseEffect(layer, 180, 4, 6, 2, colors, surface, baseCol);

                        SetEffect(pulse, layer, runningEffects);

                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;
                case "The Crown":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM11Base.Color);
                        var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM11KeyHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM11KeyHighlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM11KeyHighlight3.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM11KeyHighlight4.Color) };
                        var spinner = new BPMSpinnerEffect(layer, 135, 4, 180, colors, surface, baseCol);

                        SetEffect(spinner, layer, runningEffects);

                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;
                case "Hell on Rails":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectHoRBase.Color);
                        var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectHoRHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectHoRHighlight2.Color) };
                        var matrix = new BPMMatrixEffect(layer, 110, 2, 0.5, 8, colors, surface, MatrixEffect.MatrixDirection.Left, baseCol);

                        SetEffect(matrix, layer, runningEffects);

                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;
                case "The Ageless Necropolis":
                case "The Lightless Abyss":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
                    {
                        var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectNecronBase.Color);
                        var colors = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectNecronHighlight1.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectNecronHighlight2.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectNecronHighlight3.Color), ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectNecronHighlight4.Color) };
                        var laser = new BPMLaserEffect(layer, 160, 8, 3.5, colors, surface, LaserEffect.LaserDirection.RandomDiagonal, baseCol);

                        SetEffect(laser, layer, runningEffects);

                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;
                case "Recollection":
                    if (layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning)
                    {
                        var animationCol1 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectRecollectionHighlight1.Color);
                        var animationCol2 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectRecollectionHighlight2.Color);
                        var animationCol3 = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectRecollectionHighlight3.Color);

                        var animationGradient = new LinearGradient(
                        new GradientStop(0f, animationCol1), new GradientStop(0.33f, animationCol2), new GradientStop(0.66f, animationCol3));

                        var gradientMove = new MoveBPMDiagonalGradientDecorator(surface, 27, DiagonalDirection.Random);

                        SetRadialGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, masterlayer.layerID);

                        RaidEffectState.raidEffectsRunning = true;
                        return true;
                    }
                    return RaidEffectState.raidEffectsRunning;
                //M12/M12S
                case ArcadiaState.ZoneName:
                //case "Private Mansion - Mist":
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
                        // Reset overlay ZIndex in case the previous tick left
                        // it at FlashPriorityZIndex (e.g. zone-out mid-flash).
                        layer.ZIndex = RaidOverlayZIndex;


                        switch (currentBgmId)
                        {
                            case ArcadiaState.Phase1BgmId:
                            {
                                // Cutscene-aware start gate. While
                                // WatchingCutscene is true we hold black
                                // and BgmTriggerGate latches the key. Once
                                // WatchingCutscene flips false the latch
                                // bypasses the timing delay entirely
                                // (cutscene end IS the music-start moment).
                                // If no cutscene was ever observed, falls
                                // back to standard scene/fade-in gating.
                                // CutsceneViewerTestMode skips the cutscene
                                // path so the choreography fires from the
                                // Cutscene Viewer for iteration. phase1Start
                                // captures at the moment the gate opens, so
                                // the 11.5s flash timer anchors correctly
                                // under either path.
                                if (!BgmTriggerGate.IsReadyWithCutsceneBypass(zone, currentBgmId, watchingCutscene, eventScenePlayingBgmId, CutsceneViewerTestMode))
                                {
                                    layer.RemoveAllDecorators();
                                    layer.Brush = new SolidColorBrush(new Color((byte)255, (byte)0, (byte)0, (byte)0));
                                    layer.ZIndex = masterlayer.zindex;
                                    return true;
                                }

                                if (ArcadiaState.phase1Start != DateTime.MinValue)
                                {
                                    // RESUME path: phase1Start was captured
                                    // by a prior run that got disabled mid-
                                    // encounter. Rebuild based on elapsed
                                    // time so the choreography picks up
                                    // where it left off instead of
                                    // restarting the opening timer.
                                    // ArcadiaState (phase1Start,
                                    // activePhase1FlashAtSec, per-layer
                                    // sets) was preserved through the
                                    // disable, so don't Reset() here.
                                    var elapsed = (DateTime.UtcNow - ArcadiaState.phase1Start).TotalSeconds;
                                    var flashAt = ArcadiaState.activePhase1FlashAtSec;
                                    int lid = masterlayer.layerID;

                                    if (elapsed < flashAt)
                                    {
                                        // Pre-flash: rebuild Effect 1.
                                        // Per-tick branch will fire the
                                        // flash on schedule against the
                                        // preserved phase1Start.
                                        var rBaseCol = new Color(0, 0, 0);
                                        var rAnimCol = new Color[] { new Color(255, 255, 255) };
                                        var rStarfield = new BPMStarfieldDecorator(layer, 6, 360, 2000, rAnimCol, surface, 1, false, rBaseCol);
                                        layer.Brush = new SolidColorBrush(rBaseCol);
                                        SetEffect(rStarfield, layer, runningEffects);
                                    }
                                    else if (elapsed < flashAt + ArcadiaState.FlashDurationSec)
                                    {
                                        // Inside the flash window: re-
                                        // paint red and mark per-layer
                                        // state. flashEndByLayer is set
                                        // relative to the original
                                        // phase1Start so the natural
                                        // window expiry still occurs at
                                        // the right wall-clock moment;
                                        // the per-tick flash-hold branch
                                        // takes over from there.
                                        layer.RemoveAllDecorators();
                                        layer.Brush = new SolidColorBrush(new Color((byte)255, (byte)255, (byte)0, (byte)0));
                                        layer.ZIndex = FlashPriorityZIndex;
                                        layer.Attach(surface);
                                        ArcadiaState.flashEndByLayer[lid] = ArcadiaState.phase1Start.AddSeconds(flashAt + ArcadiaState.FlashDurationSec);
                                        ArcadiaState.layersFlashed.Add(lid);
                                    }
                                    else
                                    {
                                        // Past flash window: rebuild
                                        // Effect 2 directly. Skip the
                                        // flash visual — replaying it
                                        // would be jarring on resume.
                                        var rBaseCol = new Color(1, 40, 5);
                                        var rColors = new Color[] { new Color(255, 220, 180), new Color(0, 255, 43) };
                                        var rStrike = new BPMThunderstrikeEffect(layer, 360, 4, 0.6, rColors, surface, rBaseCol);
                                        layer.Brush = new SolidColorBrush(rBaseCol);
                                        SetEffect(rStrike, layer, runningEffects);
                                        ArcadiaState.layersFlashed.Add(lid);
                                        ArcadiaState.layersPostFlashApplied.Add(lid);
                                    }
                                    break;
                                }

                                // FRESH ENTRY: never seen this encounter
                                // before in this app session, OR a prior
                                // encounter was cleaned up by zone-out /
                                // duty-complete. Initialise from t=0.
                                ArcadiaState.Reset();
                                ArcadiaState.phase1Start = DateTime.UtcNow;
                                // Pick the flash anchor based on which
                                // gate path cleared. The three paths
                                // differ in how the music timeline lines
                                // up with phase1Start, so each has its
                                // own constant.
                                bool wasCutsceneObserved = BgmTriggerGate.WasCutsceneObserved(zone, currentBgmId);
                                if (!wasCutsceneObserved)
                                {
                                    ArcadiaState.activePhase1FlashAtSec = ArcadiaState.Phase1FlashAtSec;
                                }
                                else if (watchingCutscene)
                                {
                                    // Latched AND still watching → cleared via CUTSCENE_EVENT_SCENE_BYPASS.
                                    ArcadiaState.activePhase1FlashAtSec = ArcadiaState.Phase1FlashAtSecCutsceneEventScene;
                                }
                                else
                                {
                                    // Latched AND cutscene flipped off → cleared via CUTSCENE_END_BYPASS.
                                    ArcadiaState.activePhase1FlashAtSec = ArcadiaState.Phase1FlashAtSecCutsceneEnd;
                                }

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
                                // Entering Phase 2 invalidates Phase 1
                                // choreography state — clear it so a
                                // future return to Phase 1 (shouldn't
                                // happen but defensive) starts fresh.
                                ArcadiaState.Reset();

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

                        if (!thisLayerFlashed && elapsed >= ArcadiaState.activePhase1FlashAtSec)
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
                                // attached above on this layer's flash tick,
                                // EXCEPT when we resumed mid-flash via
                                // disable/enable: midChoreography blocks
                                // freshStart so the resume branch above
                                // never ran, and the overlay is detached.
                                // Re-paint and re-attach in that case.
                                if (layer.Surface == null)
                                {
                                    layer.RemoveAllDecorators();
                                    layer.Brush = new SolidColorBrush(new Color((byte)255, (byte)255, (byte)0, (byte)0));
                                    layer.ZIndex = FlashPriorityZIndex;
                                    layer.Attach(surface);
                                }
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
