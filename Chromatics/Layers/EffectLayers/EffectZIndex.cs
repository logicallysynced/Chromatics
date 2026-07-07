namespace Chromatics.Layers
{
    // Fixed z-order for whole-device effects, top to bottom:
    // DamageFlash > DutyFinderBell > CastingSuccess > Cutscene >
    // StatusInflicted > TitleScreen > VegasMode > StartupAnimation.
    // Each effect paints its own ListLedGroup and idles on a transparent
    // brush (a Color+ no-op), so simultaneous effects composite by this
    // order instead of fighting over one shared brush.
    //
    // Everything here sits above the raid overlays (500 / 600, with a 700
    // intra-raid flash pin) and the user-draggable layer range.
    //
    // VegasMode has no constant on purpose: it REPLACES the base layer at
    // the base layer's own zindex so gauges and castbars keep rendering
    // over it. Its slot in the order above still holds everywhere it can
    // be observed - flash, bell and cutscene groups all sit far above the
    // base slot, and the title / startup animations only run while logged
    // out or disconnected, when Vegas cannot be active.
    public static class EffectZIndex
    {
        public const int DamageFlash      = 1500;
        public const int DutyFinderBell   = 1400;
        public const int CastingSuccess   = 1350;
        public const int Cutscene         = 1300;
        public const int StatusInflicted  = 1250;
        public const int TitleScreen      = 1200;
        public const int StartupAnimation = 1000;
    }
}
