namespace Chromatics
{
    public enum ConsoleTypes
    {
        System,
        Ffxiv,
        Razer,
        Corsair,
        Logitech,
        Lifx,
        Hue,
        Arx,
        Steel,
        Coolermaster,
        Roccat,
        Wooting,
        Error
    }

    public enum BulbModeTypes
    {
        Disabled,
        Standby,
        DefaultColor,
        HighlightColor,
        EnmityTracker,
        TargetHp,
        HpTracker,
        MpTracker,
        TpTracker,
        Castbar,
        ChromaticsDefault,
        DutyFinder,
        StatusEffects,
        ACTTracker,
        Unknown
    }

    public enum DevModeTypes
    {
        Disabled,
        DefaultColor,
        HighlightColor,
        EnmityTracker,
        TargetHp,
        HpTracker,
        MpTracker,
        TpTracker,
        Castbar,
        DutyFinder,
        ACTTracker
    }

    public enum DevMultiModeTypes
    {
        Disabled,
        DefaultColor,
        HighlightColor,
        EnmityTracker,
        TargetHp,
        HpTracker,
        MpTracker,
        TpTracker,
        Castbar,
        DutyFinder,
        ReactiveWeather,
        StatusEffects,
        ACTTracker
    }

    public enum KeyRegion
    {
        QWERTY = 0,
        AZERTY = 1,
        QWERTZ = 2
    }

    public enum LightbarMode
    {
        Disabled,
        DefaultColor,
        HighlightColor,
        EnmityTracker,
        TargetHp,
        HpTracker,
        MpTracker,
        TpTracker,
        Castbar,
        DutyFinder,
        CurrentExp,
        JobGauge,
        PullCountdown,
        ACTTracker,
        ACTEnrage
    }

    public enum FKeyMode
    {
        Disabled,
        DefaultColor,
        HighlightColor,
        EnmityTracker,
        TargetHp,
        HpTracker,
        MpTracker,
        TpTracker,
        HpMpTp,
        CurrentExp,
        JobGauge,
        PullCountdown,
        ACTTracker,
        ACTEnrage
    }

    public enum ACTMode
    {
        DPS,
        HPS,
        GroupDPS,
        CritPrc,
        DHPrc,
        CritDHPrc,
        OverhealPrc,
        DamagePrc,
        Timer,
        CustomTrigger

    }
}