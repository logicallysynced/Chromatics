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
        BaseMode,
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
        ReactiveWeather,
        BattleStance,
        JobClass,
        Unknown
    }

    public enum DevModeTypes
    {
        Disabled,
        BaseMode,
        HighlightColor,
        EnmityTracker,
        TargetHp,
        HpTracker,
        MpTracker,
        Castbar,
        DutyFinder,
        ACTTracker,
        ReactiveWeather,
        BattleStance,
        JobClass
    }

    public enum DevMultiModeTypes
    {
        Disabled,
        BaseMode,
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
        AZERTY2 = 4
    }

    public enum LightbarMode
    {
        Disabled,
        BaseMode,
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
        ACTEnrage,
        ReactiveWeather,
        BattleStance,
        JobClass
    }

    public enum FKeyMode
    {
        Disabled,
        BaseMode,
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
        ACTEnrage,
        ReactiveWeather,
        BattleStance,
        JobClass
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