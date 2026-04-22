using System;
using System.Collections.Generic;

namespace Chromatics.Layers
{
    // Shared raid-effect state. Lives outside ReactiveWeather processors so
    // RaidEffectProcessor / RaidEffectHighlightProcessor can run as
    // independent overlays regardless of which base / highlight layer the
    // user has chosen.
    public static class RaidEffectState
    {
        // FFXIV's Victory Fanfare BGM id. Detecting this is faster and more
        // reliable than scanning the chat log for boss-defeat / completion
        // messages, and it works for raids where the final boss never emits
        // a "You defeat ..." line.
        public const uint VictoryBgmId = 18;
        // BGM id used during silence (no music) — e.g. dramatic pauses before
        // a phase transition. RaidEffectProcessor blacks out the base layer
        // overlay and resets raidEffectsRunning so the decorator rebuilds when
        // music returns.
        public const uint SilenceBgmId = 1;

        public static bool dutyComplete = false;
        public static bool raidEffectsRunning = false;
        // BGM the active raid effect was built for. Compared against the
        // current BGM so opt-in raid cases can rebuild their decorator on
        // phase transitions.
        public static uint currentRaidBgmId = 0;
        // Most recent non-silence BGM observed during this duty. Used to
        // distinguish "silence after real music played" (a dramatic pause —
        // blackout) from "silence on zone entry / loading screen" (no music
        // yet — keep the effect running). Reset when leaving the instance.
        public static uint lastSeenBgmId = 0;
        // True while the base-layer raid overlay is in dramatic-pause blackout.
        // Held independently of raidEffectsRunning so the zone case doesn't
        // rebuild every tick during the pause; flipped off on the silence→music
        // transition by RaidEffectProcessor, which then resets raidEffectsRunning
        // exactly once to trigger a clean decorator rebuild.
        public static bool silenced = false;

        // Hold-black-then-start gate. RaidEffectProcessor cuts the base layer
        // to solid black either on raid-zone entry (no music yet) or on a mid-
        // fight BGM=silence transition, and paints black until this timestamp
        // passes — giving every raid effect a dramatic anticipation window
        // before kicking in.
        //   DateTime.MinValue → no hold, fall through to ApplyRaidEffect
        //   DateTime.MaxValue → cut to black, waiting for music. On the first
        //                       tick where music is observed, this is converted
        //                       to (now + pendingDelaySec).
        //   anything else     → hold black until that wall-clock instant
        public static DateTime delayedStartUntil = DateTime.MinValue;

        // Captured at trigger time so the MaxValue→finite conversion uses the
        // correct delay (start vs transition) regardless of how state has
        // shifted by the time music finally arrives.
        public static double pendingDelaySec = 0;

        // Anticipation window after a raid-zone entry, between the cut-to-black
        // and the first effect kicking in. Zone-keyed entries in
        // instanceStartDelayOverrides take precedence over this default.
        public static double instanceStartDelaySec = 2.0;

        // Anticipation window during a mid-fight phase transition, between the
        // cut-to-black and the next effect kicking in. Zone-keyed entries in
        // instanceTransitionDelayOverrides take precedence over this default.
        public static double instanceTransitionDelaySec = 0.5;

        // Per-zone overrides. Add an entry like
        //   instanceStartDelayOverrides["Containment Bay S1T7"] = 5.0;
        // from a zone case (or wherever per-fight tuning lives) to swap the
        // default for that fight only. Lookup helpers below fall back to the
        // defaults above when no entry exists.
        public static readonly Dictionary<string, double> instanceStartDelayOverrides = new();
        public static readonly Dictionary<string, double> instanceTransitionDelayOverrides = new();

        public static double GetInstanceStartDelay(string zone) =>
            !string.IsNullOrEmpty(zone) && instanceStartDelayOverrides.TryGetValue(zone, out var v)
                ? v
                : instanceStartDelaySec;

        public static double GetInstanceTransitionDelay(string zone) =>
            !string.IsNullOrEmpty(zone) && instanceTransitionDelayOverrides.TryGetValue(zone, out var v)
                ? v
                : instanceTransitionDelaySec;
        // Last zone name observed. Zone changes reset raid state so a lingering
        // lastSeenBgmId / raidEffectsRunning from a previous zone (e.g. the Mist
        // demo) can't make the silence blackout fire on the new zone's loading
        // screen before its own music has started.
        public static string lastZoneName = string.Empty;

        // Per-tick state update. Called by RaidEffectProcessor before any
        // raid case fires so duty-end and out-of-instance resets stay in
        // one place.
        public static void UpdateState(bool inInstance, uint currentBgmId, string currentZone)
        {
            if (!string.IsNullOrEmpty(currentZone) && currentZone != "???" && currentZone != lastZoneName)
            {
                raidEffectsRunning = false;
                currentRaidBgmId = 0;
                dutyComplete = false;
                lastSeenBgmId = 0;
                silenced = false;
                delayedStartUntil = DateTime.MinValue;
                pendingDelaySec = 0;
                lastZoneName = currentZone;
            }

            if (currentBgmId == VictoryBgmId && raidEffectsRunning)
            {
                dutyComplete = true;
                raidEffectsRunning = false;
                currentRaidBgmId = 0;
            }

            if (!inInstance)
            {
                raidEffectsRunning = false;
                currentRaidBgmId = 0;
                dutyComplete = false;
                lastSeenBgmId = 0;
                silenced = false;
                delayedStartUntil = DateTime.MinValue;
                pendingDelaySec = 0;
                lastZoneName = string.Empty;
            }

            if (currentBgmId != SilenceBgmId && currentBgmId != 0)
            {
                lastSeenBgmId = currentBgmId;
            }
        }
    }
}
