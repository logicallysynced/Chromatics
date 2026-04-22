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

        public static bool dutyComplete = false;
        public static bool raidEffectsRunning = false;
        // BGM the active raid effect was built for. Compared against the
        // current BGM so opt-in raid cases can rebuild their decorator on
        // phase transitions.
        public static uint currentRaidBgmId = 0;

        // Per-tick state update. Called by RaidEffectProcessor before any
        // raid case fires so duty-end and out-of-instance resets stay in
        // one place.
        public static void UpdateState(bool inInstance, uint currentBgmId)
        {
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
            }
        }
    }
}
