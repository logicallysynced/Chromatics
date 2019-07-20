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
        Arx,
        Steel,
        Coolermaster,
        Roccat,
        Wooting,
        Asus,
        Mystic,
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
        QWERTZ = 2,
        ESDF = 3,
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
        HpMp,
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

    public enum Modifiers
    {
        Null,
        None,
        CTRL,
        ALT,
        SHIFT,
        CTRL_ALT,
        CTRL_SHIFT,
        ALT_SHIFT,
        CTRL_ALT_SHIFT
    }
}