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
                lastZoneName = string.Empty;
            }

            if (currentBgmId != SilenceBgmId && currentBgmId != 0)
            {
                lastSeenBgmId = currentBgmId;
            }
        }
    }
}
