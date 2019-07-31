using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Chromatics.Controllers;
using Chromatics.Datastore;
using Chromatics.DeviceInterfaces;
using Chromatics.FFXIVInterfaces;
using Chromatics.LCDInterfaces;
using Cyotek.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace Chromatics
{
    partial class Chromatics : ILogWrite
    {
        private readonly DataGridViewComboBoxColumn _dGmode = new DataGridViewComboBoxColumn();

        private readonly Dictionary<BulbModeTypes, string> _bulbModes = new Dictionary<BulbModeTypes, string>
        {
            //Keys
            {BulbModeTypes.Disabled, "Disabled"},
            {BulbModeTypes.Standby, "Standby"},
            {BulbModeTypes.BaseMode, "Base Mode"},
            {BulbModeTypes.HighlightColor, "Highlight Color"},
            {BulbModeTypes.EnmityTracker, "Enmity Tracker"},
            {BulbModeTypes.TargetHp, "Target HP"},
            {BulbModeTypes.StatusEffects, "Status Effects"},
            {BulbModeTypes.HpTracker, "HP Tracker"},
            {BulbModeTypes.MpTracker, "MP Tracker"},
            {BulbModeTypes.Castbar, "Castbar"},
            {BulbModeTypes.DutyFinder, "Duty Finder Bell"},
            {BulbModeTypes.ACTTracker, "ACT Tracker"},
            {BulbModeTypes.ChromaticsDefault, "Chromatics Default"},
            {BulbModeTypes.ReactiveWeather, "Reactive Weather"},
            {BulbModeTypes.BattleStance, "Battle Stance"},
            {BulbModeTypes.JobClass, "Job Class"}
        };

        private readonly Dictionary<DevModeTypes, string> _devModesA = new Dictionary<DevModeTypes, string>
        {
            //Keys
            {DevModeTypes.Disabled, "Disabled"},
            //{DevModeTypes.Standby, "Standby"},
            {DevModeTypes.BaseMode, "Base Mode"},
            {DevModeTypes.HighlightColor, "Highlight Color"},
            {DevModeTypes.EnmityTracker, "Enmity Tracker"},
            {DevModeTypes.TargetHp, "Target HP"},
            {DevModeTypes.HpTracker, "HP Tracker"},
            {DevModeTypes.MpTracker, "MP Tracker"},
            {DevModeTypes.Castbar, "Castbar"},
            {DevModeTypes.DutyFinder, "Duty Finder Bell"},
            {DevModeTypes.ACTTracker, "ACT Tracker"},
            {DevModeTypes.ReactiveWeather, "Reactive Weather"},
            {DevModeTypes.BattleStance, "Battle Stance"},
            {DevModeTypes.JobClass, "Job Class"}
        };

        private readonly Dictionary<DevMultiModeTypes, string> _devModesMulti = new Dictionary<DevMultiModeTypes, string>
        {
            //Keys
            {DevMultiModeTypes.Disabled, "Disabled"},
            //{DevMultiModeTypes.Standby, "Standby"},
            {DevMultiModeTypes.BaseMode, "Base Mode"},
            {DevMultiModeTypes.HighlightColor, "Highlight Color"},
            {DevMultiModeTypes.EnmityTracker, "Enmity Tracker"},
            {DevMultiModeTypes.TargetHp, "Target HP"},
            {DevMultiModeTypes.HpTracker, "HP Tracker"},
            {DevMultiModeTypes.MpTracker, "MP Tracker"},
            {DevMultiModeTypes.Castbar, "Castbar"},
            {DevMultiModeTypes.DutyFinder, "Duty Finder Bell"},
            {DevMultiModeTypes.ReactiveWeather, "Reactive Weather"},
            {DevMultiModeTypes.StatusEffects, "Status Effects"},
            {DevMultiModeTypes.ACTTracker, "ACT Tracker"}
        };

        private readonly Dictionary<LightbarMode, string> _lightbarModes = new Dictionary<LightbarMode, string>
        {
            //Keys
            {LightbarMode.Disabled, "Disabled"},
            {LightbarMode.BaseMode, "Base Mode"}, //
            {LightbarMode.HighlightColor, "Highlight Color"}, //
            {LightbarMode.EnmityTracker, "Enmity Tracker"}, //
            {LightbarMode.TargetHp, "Target HP"}, //
            {LightbarMode.TargetCastbar, "Target Castbar"},
            {LightbarMode.HpTracker, "HP Tracker"}, //
            {LightbarMode.MpTracker, "MP Tracker"}, //
            {LightbarMode.Castbar, "Castbar"}, //
            {LightbarMode.DutyFinder, "Duty Finder Bell"},
            {LightbarMode.CurrentExp, "Experience Tracker"},
            {LightbarMode.JobGauge, "Job Gauge"},
            {LightbarMode.PullCountdown, "Pull Countdown"},
            {LightbarMode.ACTTracker, "ACT Tracker"},
            {LightbarMode.ACTEnrage, "ACT Enrage"},
            {LightbarMode.ReactiveWeather, "Reactive Weather"},
            {LightbarMode.BattleStance, "Battle Stance"},
            {LightbarMode.JobClass, "Job Class"}
        };

        private readonly Dictionary<FKeyMode, string> _fkeyModes = new Dictionary<FKeyMode, string>
        {
            //Keys
            {FKeyMode.Disabled, "Disabled"},
            {FKeyMode.BaseMode, "Base Mode"}, //
            {FKeyMode.HighlightColor, "Highlight Color"}, //
            {FKeyMode.EnmityTracker, "Enmity Tracker"}, //
            {FKeyMode.TargetHp, "Target HP"}, //
            {FKeyMode.TargetCastbar, "Target Castbar"},
            {FKeyMode.HpTracker, "HP Tracker"}, //
            {FKeyMode.MpTracker, "MP Tracker"}, //
            {FKeyMode.HpMp, "HP/MP"}, //
            {FKeyMode.HpJobMp, "Hp/Job Gauge/MP" },
            {FKeyMode.CurrentExp, "Experience Tracker"},
            {FKeyMode.JobGauge, "Job Gauge"},
            {FKeyMode.PullCountdown, "Pull Countdown"},
            {FKeyMode.ACTTracker, "ACT Tracker"},
            {FKeyMode.ACTEnrage, "ACT Enrage"},
            {FKeyMode.ReactiveWeather, "Reactive Weather"},
            {FKeyMode.BattleStance, "Battle Stance"},
            {FKeyMode.JobClass, "Job Class"}
        };

        private readonly Dictionary<ACTMode, string> _actModes = new Dictionary<ACTMode, string>
        {
            //Keys
            {ACTMode.DPS, "DPS"},
            {ACTMode.HPS, "HPS"}, //
            {ACTMode.GroupDPS, "Group DPS"}, //
            {ACTMode.CritPrc, "Crit %"}, //
            {ACTMode.DHPrc, "DH %"}, //
            {ACTMode.CritDHPrc, "Crit DH %"}, //
            {ACTMode.OverhealPrc, "Overheal %"}, //
            {ACTMode.DamagePrc, "Damage %"}, //
            {ACTMode.Timer, "Timer" },
            {ACTMode.CustomTrigger, "Custom Trigger" },
        };

        private readonly Dictionary<int, string> _actJobs = new Dictionary<int, string>
        {
            {0, "Paladin"}, //PLD
            {1, "Warrior"}, //WAR
            {2, "Dark Knight"}, //DRK
            {3, "White Mage"}, //WHM
            {4, "Scholar"}, //SCH
            {5, "Astrologian"}, //AST
            {6, "Bard"}, //BRD
            {7, "Machinist"}, //MCH
            {8, "Black Mage"}, //BLM
            {9, "Summoner"}, //SMN
            {10, "Monk"}, //MNK
            {11, "Samurai"}, //SAM
            {12, "Ninja"}, //NIN
            {13, "Dragoon"}, //DRG
            {14, "Archer"}, //ARC
            {15, "Gladiator"}, //GLD
            {16, "Lancer"}, //LNC
            {17, "Marauder"}, //MRD
            {18, "Pugilist"}, //PUG
            {19, "Rouge"}, //ROG
            {20, "Arcanist"}, //ARC
            {21, "Conjurer"}, //CNJ
            {22, "Thaumaturge"}, //THM
            {23, "Red Mage" }, //RDM
            {24, "Blue Mage" }, //BLU
            {25, "Dancer" }, //DNC
            {26, "Gunbreaker" }, //GNB
        };

        private readonly Dictionary<string, string[]> _mappingPalette = new Dictionary<string, string[]>
        {
            //Keys
            {"ColorMappingBaseColor", new[] {"Base Color", "1", "Black", "White"}},
            {"ColorMappingHighlightColor", new[] {"Highlight Color", "1", "Black", "White"}},
            {"ColorMappingDeviceDisabled", new[] {"Device Disabled Color", "1", "Black", "White"}},
            {"ColorMappingMenuBase", new[] {"Menu Base Color", "1", "Black", "White"}},
            {"ColorMappingMenuHighlight1", new[] { "Menu Highlight Color 1", "1", "Black", "White"}},
            {"ColorMappingMenuHighlight2", new[] { "Menu Highlight Color 2", "1", "Black", "White"}},
            {"ColorMappingMenuHighlight3", new[] { "Menu Highlight Color 3", "1", "Black", "White"}},
            {"ColorMappingCutsceneBase", new[] {"Cutscene Base Color", "1", "Black", "White"}},
            {"ColorMappingCutsceneHighlight1", new[] {"Cutscene Highlight Color 1", "1", "Black", "White"}},
            {"ColorMappingCutsceneHighlight2", new[] { "Cutscene Highlight Color 2", "1", "Black", "White"}},
            {"ColorMappingCutsceneHighlight3", new[] { "Cutscene Highlight Color 3", "1", "Black", "White"}},
            {"ColorMappingHpFull", new[] {"HP Full", "2", "Black", "White"}},
            {"ColorMappingHpEmpty", new[] {"HP Empty", "2", "Black", "White"}},
            {"ColorMappingHpCritical", new[] {"HP Critical", "2", "Black", "White"}},
            {"ColorMappingHpLoss", new[] {"HP Loss", "2", "Black", "White"}},
            {"ColorMappingMpFull", new[] {"MP Full", "2", "Black", "White"}},
            {"ColorMappingMpEmpty", new[] {"MP Empty", "2", "Black", "White"}},
            {"ColorMappingGpFull", new[] {"GP Full", "2", "Black", "White"}},
            {"ColorMappingGpEmpty", new[] {"GP Empty", "2", "Black", "White"}},
            {"ColorMappingCpFull", new[] {"CP Full", "2", "Black", "White"}},
            {"ColorMappingCpEmpty", new[] {"CP Empty", "2", "Black", "White"}},
            {"ColorMappingExpEmpty", new[] {"Experience Bar (Empty)", "2", "Black", "White"}},
            {"ColorMappingExpFull", new[] { "Experience Bar (Full)", "2", "Black", "White"}},
            {"ColorMappingExpMax", new[] { "Experience Bar (Level Cap)", "2", "Black", "White"}},
            {"ColorMappingCastChargeFull", new[] {"Cast Charge", "3", "Black", "White"}},
            {"ColorMappingCastChargeEmpty", new[] {"Cast Empty", "3", "Black", "White"}},
            {"ColorMappingEmnity0", new[] {"Minimal Enmity", "4", "Black", "White"}},
            {"ColorMappingEmnity1", new[] {"Low Enmity", "4", "Black", "White"}},
            {"ColorMappingEmnity2", new[] {"Medium Enmity", "4", "Black", "White"}},
            {"ColorMappingEmnity3", new[] {"High Enmity", "4", "Black", "White"}},
            {"ColorMappingEmnity4", new[] {"Max Enmity", "4", "Black", "White"}},
            {"ColorMappingNoEmnity", new[] {"No Enmity", "4", "Black", "White"}},
            {"ColorMappingTargetHpClaimed", new[] {"Target HP - Claimed", "5", "Black", "White"}},
            {"ColorMappingTargetHpFriendly", new[] {"Target HP - Claimed", "5", "Black", "White"}},
            {"ColorMappingTargetHpIdle", new[] {"Target HP - Idle", "5", "Black", "White"}},
            {"ColorMappingTargetHpEmpty", new[] {"Target HP - Empty", "5", "Black", "White"}},
            {"ColorMappingTargetCasting", new[] {"Target Casting", "5", "Black", "White"}},
            {"ColorMappingBind", new[] {"Bind", "6", "Black", "White"}},
            {"ColorMappingVulnerabilityUp", new[] {"Vulnerability Up", "6", "Black", "White"}},
            {"ColorMappingPetrification", new[] {"Petrification", "6", "Black", "White"}},
            {"ColorMappingSlow", new[] {"Slow", "6", "Black", "White"}},
            {"ColorMappingStun", new[] {"Stun", "6", "Black", "White"}},
            {"ColorMappingSilence", new[] {"Silence", "6", "Black", "White"}},
            {"ColorMappingPoison", new[] {"Poison", "6", "Black", "White"}},
            {"ColorMappingPollen", new[] {"Pollen", "6", "Black", "White"}},
            {"ColorMappingPox", new[] {"Pox", "6", "Black", "White"}},
            {"ColorMappingParalysis", new[] {"Paralysis", "6", "Black", "White"}},
            {"ColorMappingLeaden", new[] {"Leaden", "6", "Black", "White"}},
            {"ColorMappingIncapacitation", new[] {"Incapacitation", "6", "Black", "White"}},
            {"ColorMappingDropsy", new[] {"Dropsy", "6", "Black", "White"}},
            {"ColorMappingOld", new[] {"Old", "6", "Black", "White"}},
            {"ColorMappingAmnesia", new[] {"Amnesia", "6", "Black", "White"}},
            {"ColorMappingBleed", new[] {"Bleed", "6", "Black", "White"}},
            {"ColorMappingMisery", new[] {"Misery", "6", "Black", "White"}},
            {"ColorMappingSleep", new[] {"Sleep", "6", "Black", "White"}},
            {"ColorMappingDaze", new[] {"Daze", "6", "Black", "White"}},
            {"ColorMappingHeavy", new[] {"Heavy", "6", "Black", "White"}},
            {"ColorMappingInfirmary", new[] {"Infirmary", "6", "Black", "White"}},
            {"ColorMappingBurns", new[] {"Burns", "6", "Black", "White"}},
            {"ColorMappingDeepFreeze", new[] {"Deep Freeze", "6", "Black", "White"}},
            {"ColorMappingDamageDown", new[] {"Damage Down", "6", "Black", "White"}},
            {"ColorMappingGcdHot", new[] {"GCD Countdown Hot", "7", "Black", "White"}},
            {"ColorMappingGcdReady", new[] {"GCD Countdown Ready", "7", "Black", "White"}},
            {"ColorMappingGcdEmpty", new[] {"GCD Countdown Empty", "7", "Black", "White"}},
            {"ColorMappingHotbarProc", new[] {"Keybind Proc/Combo", "7", "Black", "White"}},
            {"ColorMappingHotbarCd", new[] {"Keybind Cooldown", "7", "Black", "White"}},
            {"ColorMappingHotbarReady", new[] {"Keybind Ready", "7", "Black", "White"}},
            {"ColorMappingHotbarOutRange", new[] {"Keybind Out of Range", "7", "Black", "White"}},
            {"ColorMappingHotbarNotAvailable", new[] {"Keybind Not Available", "7", "Black", "White"}},
            {"ColorMappingKeybindDisabled", new[] {"Keybind Disabled", "7", "Black", "White"}},
            {"ColorMappingKeybindMap", new[] {"Map Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindAetherCurrents", new[] { "Aether Currents Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindSigns", new[] { "Signs Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindWaymarks", new[] { "Waymarks Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindRecordReadyCheck", new[] { "Record Ready Check Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindReadyCheck", new[] { "Ready Check Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindCountdown", new[] { "Countdown Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindEmotes", new[] { "Emotes Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindCrossWorldLS", new[] { "Cross-World Linkshells Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindLinkshells", new[] { "Linkshells Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindContacts", new[] { "Contacts Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindSprint", new[] { "Sprint Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindTeleport", new[] { "Teleport Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindReturn", new[] { "Return Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindLimitBreak", new[] { "Limit Break Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindDutyAction", new[] { "Duty Action Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindRepair", new[] { "Repair Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindDig", new[] { "Dig Keybind", "7", "Black", "White"}},
            {"ColorMappingKeybindInventory", new[] { "Inventory Keybind", "7", "Black", "White"}},
            {"ColorMappingDutyFinderBell", new[] {"Duty Finder Bell", "8", "Black", "White"}},
            {"ColorMappingPullCountdownTick", new[] { "Pull Countdown Tick", "8", "Black", "White"}},
            {"ColorMappingPullCountdownEmpty", new[] { "Pull Countdown Empty", "8", "Black", "White"}},
            {"ColorMappingPullCountdownEngage", new[] { "Pull Countdown Engage", "8", "Black", "White"}},
            {"ColorMappingJobWARNegative", new[] {"WAR: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobWARBeastGauge", new[] { "WAR: Beast Gauge", "9", "Black", "White"}},
            {"ColorMappingJobWARDefiance", new[] { "WAR: Defiance", "9", "Black", "White"}},
            {"ColorMappingJobWARNonDefiance", new[] { "WAR: Non-Defiance", "9", "Black", "White"}},
            {"ColorMappingJobWARBeastGaugeMax", new[] { "WAR: Beast Gauge (Full)", "9", "Black", "White"}},
            {"ColorMappingJobPLDNegative", new[] {"PLD: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobPLDOathGauge", new[] {"PLD: Oath Gauge", "9", "Black", "White"}},
            {"ColorMappingJobPLDIronWill", new[] {"PLD: Iron Will", "9", "Black", "White"}}, 
            {"ColorMappingJobPLDSwordOath", new[] { "PLD: Sword Oath", "9", "Black", "White"}},
            {"ColorMappingJobMNKNegative", new[] {"MNK: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobMNKGreased", new[] { "MNK: Greased Lighting", "9", "Black", "White"}},
            {"ColorMappingJobDRGNegative", new[] {"DRG: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobDRGBloodDragon", new[] {"DRG: Blood of the Dragon", "9", "Black", "White"}},
            {"ColorMappingJobDRGDragonGauge1", new[] {"DRG: Dragon Gauge 1", "9", "Black", "White"}},
            {"ColorMappingJobDRGDragonGauge2", new[] {"DRG: Dragon Gauge 2", "9", "Black", "White"}},
            {"ColorMappingJobDRGLifeOfTheDragon", new[] {"DRG: Life of the Dragon", "9", "Black", "White"}},
            {"ColorMappingJobBRDNegative", new[] {"BRD: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobBRDRepertoire", new[] { "BRD: Repertoire Stack", "9", "Black", "White"}},
            {"ColorMappingJobBRDBallad", new[] { "BRD: Mage's Ballad", "9", "Black", "White"}},
            {"ColorMappingJobBRDArmys", new[] { "BRD: Army's Paeon", "9", "Black", "White"}},
            {"ColorMappingJobBRDMinuet", new[] { "BRD: The Wanderers' Minuet", "9", "Black", "White"}},
            {"ColorMappingJobWHMNegative", new[] {"WHM: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobWHMFlowerPetal", new[] {"WHM: Flower", "9", "Black", "White"}},
            {"ColorMappingJobWHMFreecure", new[] {"WHM: Freecure Proc", "9", "Black", "White"}},
            {"ColorMappingJobWHMFlowerCharge", new[] {"WHM: Flower Charge", "9", "Black", "White"}},
            {"ColorMappingJobWHMBloodLily", new[] {"WHM: Blood Lily", "9", "Black", "White"}},
            {"ColorMappingJobBLMNegative", new[] {"BLM: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobBLMAstralFire", new[] { "BLM: Astral Fire", "9", "Black", "White"}},
            {"ColorMappingJobBLMUmbralIce", new[] { "BLM: Umbral Ice", "9", "Black", "White"}},
            {"ColorMappingJobBLMUmbralHeart", new[] { "BLM: Umbral Heart", "9", "Black", "White"}},
            {"ColorMappingJobBLMEnochianCountdown", new[] { "BLM: Enochian Countdown", "9", "Black", "White"}},
            {"ColorMappingJobBLMEnochianCharge", new[] { "BLM: Enochian Charge", "9", "Black", "White"}},
            {"ColorMappingJobBLMPolyglot", new[] { "BLM: Polyglot", "9", "Black", "White"}},
            {"ColorMappingJobSMNNegative", new[] {"SMN: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobSMNAetherflow", new[] {"SMN: Aetherflow", "9", "Black", "White"}},
            {"ColorMappingJobSCHNegative", new[] {"SCH: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobSCHAetherflow", new[] {"SCH: Aetherflow", "9", "Black", "White"}},
            {"ColorMappingJobSCHFaerieGauge", new[] {"SCH: Faerie Gauge", "9", "Black", "White"}},
            {"ColorMappingJobNINNegative", new[] {"NIN: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobNINHuton", new[] {"NIN: Huton", "9", "Black", "White"}},
            {"ColorMappingJobNINNinkiGauge", new[] {"NIN: Ninki Gauge", "9", "Black", "White"}},
            {"ColorMappingJobDRKNegative", new[] {"DRK: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobDRKBloodGauge", new[] {"DRK: Blood Gauge", "9", "Black", "White"}},
            {"ColorMappingJobDRKGrit", new[] {"DRK: Grit", "9", "Black", "White"}},
            {"ColorMappingJobDRKDarkside", new[] {"DRK: Darkside", "9", "Black", "White"}},
            {"ColorMappingJobASTNegative", new[] {"AST: Blank Key" , "9", "Black", "White"}},
            {"ColorMappingJobASTArrow", new[] { "AST: Arrow Drawn", "9", "Black", "White"}},
            {"ColorMappingJobASTBalance", new[] { "AST: Balance Drawn", "9", "Black", "White"}},
            {"ColorMappingJobASTBole", new[] { "AST: Bole Drawn", "9", "Black", "White"}},
            {"ColorMappingJobASTEwer", new[] { "AST: Ewer Drawn", "9", "Black", "White"}},
            {"ColorMappingJobASTSpear", new[] { "AST: Spear Drawn", "9", "Black", "White"}},
            {"ColorMappingJobASTSpire", new[] { "AST: Spire Drawn", "9", "Black", "White"}},
            {"ColorMappingJobASTLady", new[] { "AST: Lady of Crowns Drawn", "9", "Black", "White"}},
            {"ColorMappingJobASTLord", new[] { "AST: Lord of Crowns Drawn", "9", "Black", "White"}},
            {"ColorMappingJobMCHNegative", new[] {"MCH: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobMCHAmmo", new[] { "MCH: Ammo", "9", "Black", "White"}},
            {"ColorMappingJobMCHHeatGauge", new[] { "MCH: Heat Gauge", "9", "Black", "White"}},
            {"ColorMappingJobMCHOverheat", new[] { "MCH: Overheat", "9", "Black", "White"}},
            {"ColorMappingJobSAMNegative", new[] {"SAM: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobSAMSetsu", new[] { "SAM: Setsu", "9", "Black", "White"}},
            {"ColorMappingJobSAMGetsu", new[] { "SAM: Getsu", "9", "Black", "White"}},
            {"ColorMappingJobSAMKa", new[] { "SAM: Ka", "9", "Black", "White"}},
            {"ColorMappingJobSAMKenki", new[] { "SAM: Kenki Charge", "9", "Black", "White"}},
            {"ColorMappingJobRDMNegative", new[] {"RDM: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobRDMBlackMana", new[] { "RDM: Black Mana", "9", "Black", "White"}},
            {"ColorMappingJobRDMWhiteMana", new[] { "RDM: White Mana", "9", "Black", "White"}},
            {"ColorMappingJobDNCNegative", new[] {"DNC: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobDNCEntrechat", new[] { "DNC: Entrechat", "9", "Black", "White"}},
            {"ColorMappingJobDNCPirouette", new[] { "DNC: Pirouette", "9", "Black", "White"}},
            {"ColorMappingJobDNCEmboite", new[] { "DNC: Emboite", "9", "Black", "White"}},
            {"ColorMappingJobDNCJete", new[] { "DNC: Jete", "9", "Black", "White"}},
            {"ColorMappingJobDNCStandardFinish", new[] { "DNC: Standard Finish", "9", "Black", "White"}},
            {"ColorMappingJobDNCTechnicalFinish", new[] { "DNC: Technical Finish", "9", "Black", "White"}},
            {"ColorMappingJobGNBNegative", new[] { "GNB: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobGNBRoyalGuard", new[] { "GNB: Royal Guard", "9", "Black", "White"}},
            {"ColorMappingJobCrafterNegative", new[] { "Crafter: Blank Key", "9", "Black", "White"}},
            {"ColorMappingJobCrafterInnerquiet", new[] { "Crafter: Inner Quiet Stacks", "9", "Black", "White"}},
            {"ColorMappingJobCrafterCollectable", new[] { "Crafter: Collectables", "9", "Black", "White"}},
            {"ColorMappingJobCrafterCrafter", new[] { "Crafter: Crafting", "9", "Black", "White"}},
            {"ColorMappingWeatherClearBase", new[] { "Clear Skies (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherClearHighlight", new[] { "Clear Skies (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherFairBase", new[] { "Fair Skies (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherFairHighlight", new[] { "Fair Skies (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherCloudsBase", new[] { "Clouds (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherCloudsHighlight", new[] { "Clouds (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherFogBase", new[] { "Fog (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherFogHighlight", new[] { "Fog (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherWindBase", new[] { "Wind (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherWindHighlight", new[] { "Wind (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherGalesBase", new[] { "Gales (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherGalesHighlight", new[] { "Gales (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherRainBase", new[] { "Rain (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherRainHighlight", new[] { "Rain (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherShowersBase", new[] { "Showers (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherShowersHighlight", new[] { "Showers (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherThunderBase", new[] { "Thunder (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherThunderHighlight", new[] { "Thunder (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherThunderstormsBase", new[] { "Thunderstorms (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherThunderstormsHighlight", new[] { "Thunderstorms (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherDustBase", new[] { "Dust Storms (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherDustHighlight", new[] { "Dust Storms (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherSandstormBase", new[] { "Sandstorms (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherSandstormHighlight", new[] { "Sandstorms (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherHotspellBase", new[] { "Hot Spells (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherHotspellHighlight", new[] { "Hot Spells (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherHeatwaveBase", new[] { "Heat Waves (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherHeatwaveHighlight", new[] { "Heat Waves (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherSnowBase", new[] { "Snow (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherSnowHighlight", new[] { "Snow (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherBlizzardsBase", new[] { "Blizzards (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherBlizzardsHighlight", new[] { "Blizzards (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherGloomBase", new[] { "Gloom (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherGloomHighlight", new[] { "Gloom (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherAurorasBase", new[] { "Auroras (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherAurorasHighlight", new[] { "Auroras (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherDarknessBase", new[] { "Darkness (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherDarknessHighlight", new[] { "Darkness (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherTensionBase", new[] { "Tension (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherTensionHighlight", new[] { "Tension (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherStormcloudsBase", new[] { "Storm Clouds (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherStormcloudsHighlight", new[] { "Storm Clouds (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherRoughseasBase", new[] { "Rough Seas (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherRoughseasHighlight", new[] { "Rough Seas (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherLouringBase", new[] { "Louring (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherLouringHighlight", new[] { "Louring (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherEruptionsBase", new[] { "Eruptions (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherEruptionsHighlight", new[] { "Eruptions (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherIrradianceBase", new[] { "Irradiance (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherIrradianceHighlight", new[] { "Irradiance (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherCoreradiationBase", new[] { "Core Radiation (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherCoreradiationHighlight", new[] { "Core Radiation (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherShelfcloudsBase", new[] { "Shelf Clouds (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherShelfcloudsHighlight", new[] { "Shelf Clouds (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherOppressionBase", new[] { "Oppression (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherOppressionHighlight", new[] { "Oppression (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherUmbralwindBase", new[] { "Umbral Wind (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherUmbralwindHighlight", new[] { "Umbral Wind (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherUmbralstaticBase", new[] { "Umbral Static (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherUmbralstaticHighlight", new[] { "Umbral Static (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherSmokeBase", new[] { "Smoke (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherSmokeHighlight", new[] { "Smoke (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherRoyallevinBase", new[] { "Royal Levin (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherRoyallevinHighlight", new[] { "Royal Levin (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherHyperelectricityBase", new[] { "Hyperelectricity (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherHyperelectricityHighlight", new[] { "Hyperelectricity (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherMultiplicityBase", new[] { "Multiplicity (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherMultiplicityHighlight", new[] { "Multiplicity (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherDragonstormBase", new[] { "Dragonstorm (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherDragonstormHighlight", new[] { "Dragonstorm (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherSubterrainBase", new[] { "Subterrain (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherSubterrainHighlight", new[] { "Subterrain (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherConcordanceBase", new[] { "Concordance (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherConcordanceHighlight", new[] { "Concordance (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherBeyondtimeBase", new[] { "Beyond Time (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherBeyondtimeHighlight", new[] { "Beyond Time (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherDemonicinfinityBase", new[] { "Demonic Infinity (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherDemonicinfinityHighlight", new[] { "Demonic Infinity (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherDimensionaldisruptionBase", new[] { "Dimensional Disruption (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherDimensionaldisruptionHighlight", new[] { "Dimensional Disruption (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherRevelstormBase", new[] { "Revelstorm (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherRevelstormHighlight", new[] { "Revelstorm (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherEternalblissBase", new[] { "Eternal Bliss (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherEternalblissHighlight", new[] { "Eternal Bliss (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherWyrmstormBase", new[] { "Wyrmstorm (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherWyrmstormHighlight", new[] { "Wyrmstorm (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherQuicklevinBase", new[] { "Quicklevin (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherQuicklevinHighlight", new[] { "Quicklevin (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherWhitecycloneBase", new[] { "White Cyclone (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherWhitecycloneHighlight", new[] { "White Cyclone (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherGeostormsBase", new[] { "Geostorms (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherGeostormsHighlight", new[] { "Geostorms (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherTrueblueBase", new[] { "True Blue (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherTrueblueHighlight", new[] { "True Blue (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherUmbralturbulenceBase", new[] { "Umbral Turbulence (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherUmbralturbulenceHighlight", new[] { "Umbral Turbulence (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherEverlastinglightBase", new[] { "Everlasting Light (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherEverlastinglightHighlight", new[] { "Everlasting Light (Highlight)", "10", "Black", "White"}},
            {"ColorMappingWeatherTerminationBase", new[] { "Termination (Base)", "10", "Black", "White"}},
            {"ColorMappingWeatherTerminationHighlight", new[] { "Termination (Highlight)", "10", "Black", "White"}},
            {"ColorMappingACTThresholdEmpty", new[] { "ACT Threshold Empty", "11", "Black", "White"}},
            {"ColorMappingACTThresholdBuild", new[] { "ACT Threshold Build", "11", "Black", "White"}},
            {"ColorMappingACTThresholdSuccess", new[] { "ACT Threshold Success", "11", "Black", "White"}},
            {"ColorMappingACTThresholdFlash", new[] { "ACT Threshold Flash", "11", "Black", "White"}},
            {"ColorMappingACTCustomTriggerIdle", new[] { "Custom Trigger Idle", "11", "Black", "White"}},
            {"ColorMappingACTCustomTriggerBell", new[] { "Custom Trigger Bell", "11", "Black", "White"}},
            {"ColorMappingACTTimerIdle", new[] { "Timer Idle", "11", "Black", "White"}},
            {"ColorMappingACTTimerBuild", new[] { "Timer Build", "11", "Black", "White"}},
            {"ColorMappingACTTimerFlash", new[] { "Timer Flash", "11", "Black", "White"}},
            {"ColorMappingACTEnrageEmpty", new[] { "Enrage Empty", "11", "Black", "White"}},
            {"ColorMappingACTEnrageCountdown", new[] { "Enrage Countdown", "11", "Black", "White"}},
            {"ColorMappingACTEnrageWarning", new[] { "Enrage Warning", "11", "Black", "White"}},
            {"ColorMappingJobPLDBase", new[] { "PLD (Base)", "12", "Black", "White"}},
            {"ColorMappingJobPLDHighlight", new[] { "PLD (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobMNKBase", new[] { "MNK (Base)", "12", "Black", "White"}},
            {"ColorMappingJobMNKHighlight", new[] { "MNK (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobWARBase", new[] { "WAR (Base)", "12", "Black", "White"}},
            {"ColorMappingJobWARHighlight", new[] { "WAR (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobDRGBase", new[] { "DRG (Base)", "12", "Black", "White"}},
            {"ColorMappingJobDRGHighlight", new[] { "DRG (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobBRDBase", new[] { "BRD (Base)", "12", "Black", "White"}},
            {"ColorMappingJobBRDHighlight", new[] { "BRD (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobWHMBase", new[] { "WHM (Base)", "12", "Black", "White"}},
            {"ColorMappingJobWHMHighlight", new[] { "WHM (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobBLMBase", new[] { "BLM (Base)", "12", "Black", "White"}},
            {"ColorMappingJobBLMHighlight", new[] { "BLM (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobSMNBase", new[] { "SMN (Base)", "12", "Black", "White"}},
            {"ColorMappingJobSMNHighlight", new[] { "SMN (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobSCHBase", new[] { "SCH (Base)", "12", "Black", "White"}},
            {"ColorMappingJobSCHHighlight", new[] { "SCH (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobNINBase", new[] { "NIN (Base)", "12", "Black", "White"}},
            {"ColorMappingJobNINHighlight", new[] { "NIN (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobMCHBase", new[] { "MCH (Base)", "12", "Black", "White"}},
            {"ColorMappingJobMCHHighlight", new[] { "MCH (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobDRKBase", new[] { "DRK (Base)", "12", "Black", "White"}},
            {"ColorMappingJobDRKHighlight", new[] { "DRK (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobASTBase", new[] { "AST (Base)", "12", "Black", "White"}},
            {"ColorMappingJobASTHighlight", new[] { "AST (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobSAMBase", new[] { "SAM (Base)", "12", "Black", "White"}},
            {"ColorMappingJobSAMHighlight", new[] { "SAM (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobRDMBase", new[] { "RDM (Base)", "12", "Black", "White"}},
            {"ColorMappingJobRDMHighlight", new[] { "RDM (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobDNCBase", new[] { "DNC (Base)", "12", "Black", "White"}},
            {"ColorMappingJobDNCHighlight", new[] { "DNC (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobGNBBase", new[] { "GNB (Base)", "12", "Black", "White"}},
            {"ColorMappingJobGNBHighlight", new[] { "GNB (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobBLUBase", new[] { "BLU (Base)", "12", "Black", "White"}},
            {"ColorMappingJobBLUHighlight", new[] { "BLU (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobCPTBase", new[] { "CPT (Base)", "12", "Black", "White"}},
            {"ColorMappingJobCPTHighlight", new[] { "CPT (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobBSMBase", new[] { "BSM (Base)", "12", "Black", "White"}},
            {"ColorMappingJobBSMHighlight", new[] { "BSM (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobARMBase", new[] { "ARM (Base)", "12", "Black", "White"}},
            {"ColorMappingJobARMHighlight", new[] { "ARM (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobGSMBase", new[] { "GSM (Base)", "12", "Black", "White"}},
            {"ColorMappingJobGSMHighlight", new[] { "GSM (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobLTWBase", new[] { "LTW (Base)", "12", "Black", "White"}},
            {"ColorMappingJobLTWHighlight", new[] { "LTW (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobWVRBase", new[] { "WVR (Base)", "12", "Black", "White"}},
            {"ColorMappingJobWVRHighlight", new[] { "WVR (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobALCBase", new[] { "ALC (Base)", "12", "Black", "White"}},
            {"ColorMappingJobALCHighlight", new[] { "ALC (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobCULBase", new[] { "CUL (Base)", "12", "Black", "White"}},
            {"ColorMappingJobCULHighlight", new[] { "CUL (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobMINBase", new[] { "MIN (Base)", "12", "Black", "White"}},
            {"ColorMappingJobMINHighlight", new[] { "MIN (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobBTNBase", new[] { "BTN (Base)", "12", "Black", "White"}},
            {"ColorMappingJobBTNHighlight", new[] { "BTN (Highlight)", "12", "Black", "White"}},
            {"ColorMappingJobFSHBase", new[] { "FSH (Base)", "12", "Black", "White"}},
            {"ColorMappingJobFSHHighlight", new[] { "FSH (Highlight)", "12", "Black", "White"}},
        };


        private readonly Dictionary<int, string> _paletteCategories = new Dictionary<int, string>
        {
            //Keys
            {0, "All"},
            {1, "Chromatics"},
            {2, "Player Stats"},
            {3, "Abilities"},
            {4, "Enmity/Aggro"},
            {5, "Target/Enemy"},
            {6, "Status Effects"},
            {7, "Cooldowns/Keybinds"},
            {8, "Notifications"},
            {9, "Job Gauges" },
            {10, "Reactive Weather" },
            {11, "ACT"},
            {12, "Job Classes"},
        };

        public void SetFormName(string text)
        {
            if (InvokeRequired)
            {
                void Del()
                {
                    Text = text;
                }

                Invoke((SetFormNameDelegate) Del);
            }
            else
            {
                Text = text;
            }
        }

        public void ResetDeviceDataGrid()
        {
            if (InvokeRequired)
            {
                ResetGridDelegate del = ResetDeviceDataGrid;
                Invoke(del);
            }
            else
            {
                SetupDeviceDataGrid();
            }
        }

        public void ResetMappingsDataGrid()
        {
            if (InvokeRequired)
            {
                ResetMappingsDelegate del = ResetMappingsDataGrid;
                Invoke(del);
            }
            else
            {
                SetupMappingsDataGrid();
            }
        }

        private void InitColorMappingGrid()
        {
            foreach (var c in _paletteCategories)
                cb_palette_categories.Items.Add(c.Value);

            cb_palette_categories.SelectedIndex = 0;
            ToggleMappingControls(false);

            ResetMappingsDataGrid();

            dgv_iftttgrid.Rows.Add("0", "Duty Finder Bell", "Chromatics_DFBell");
            dgv_iftttgrid.Rows.Add("1", "Eorzea Time Alarm", "Chromatics_Alarm");
            dgv_iftttgrid.Rows.Add("2", "S Rank Alert", "Chromatics_SRankAlert");
            dgv_iftttgrid.Rows.Add("3", "Ready Check Alert", "Chromatics_ReadyCheck");
        }

        private void ToggleMappingControls(bool toggle)
        {
            if (toggle)
            {
                mapping_colorEditorManager.ColorEditor.Enabled = true;
                mapping_colorEditorManager.ColorGrid.Enabled = true;
                mapping_colorEditorManager.ColorWheel.Enabled = true;
                mapping_colorEditorManager.LightnessColorSlider.Enabled = true;
                mapping_colorEditorManager.ScreenColorPicker.Enabled = true;
                loadPaletteButton.Enabled = true;
            }
            else
            {
                mapping_colorEditorManager.ColorEditor.Enabled = false;
                mapping_colorEditorManager.ColorGrid.Enabled = false;
                mapping_colorEditorManager.ColorWheel.Enabled = false;
                mapping_colorEditorManager.LightnessColorSlider.Enabled = false;
                mapping_colorEditorManager.ScreenColorPicker.Enabled = false;
                loadPaletteButton.Enabled = false;
            }
        }

        private void cb_palette_categories_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MappingGridStartup)
            {
                var filter = cb_palette_categories.SelectedIndex.ToString();

                if (filter == "0")
                    foreach (DataGridViewRow row in dG_mappings.Rows)
                        row.Visible = true;
                else
                    foreach (DataGridViewRow row in dG_mappings.Rows)
                        if ((string)row.Cells[dG_mappings.Columns["mappings_col_cat"].Index].Value == filter)
                            row.Visible = true;
                        else
                            row.Visible = false;
            }
        }

        private void SetupMappingsDataGrid()
        {
            MappingGridStartup = false;
            dG_mappings.AllowUserToAddRows = true;
            dG_mappings.Rows.Clear();

            DrawMappingsDict();
            var i = 0;
            DataGridViewRow[] dgV = new DataGridViewRow[_mappingPalette.Count];

            foreach (var palette in _mappingPalette)
            {
                var paletteItem = (DataGridViewRow)dG_mappings.Rows[0].Clone();
                paletteItem.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value = palette.Key;
                paletteItem.Cells[dG_mappings.Columns["mappings_col_cat"].Index].Value = palette.Value[1];
                paletteItem.Cells[dG_mappings.Columns["mappings_col_type"].Index].Value = palette.Value[0];

                var paletteBtn = new DataGridViewTextBoxCell();
                paletteBtn.Style.BackColor = ColorTranslator.FromHtml(palette.Value[2]);
                paletteBtn.Style.SelectionBackColor = ColorTranslator.FromHtml(palette.Value[2]);

                paletteBtn.Value = "";

                paletteItem.Cells[dG_mappings.Columns["mappings_col_color"].Index] = paletteBtn;

                //dG_mappings.Rows.Add(paletteItem);
                dgV[i] = paletteItem;
                i++;
                paletteBtn.ReadOnly = true;
            }

            dG_mappings.Rows.AddRange(dgV);

            dG_mappings.AllowUserToAddRows = false;
            MappingGridStartup = true;
        }

        private void DrawMappingsDict()
        {
            //PropertyInfo[] _FFXIVColorMappings = typeof(FFXIVColorMappings).GetProperties();

            //ColorMappings
            foreach (var p in typeof(FfxivColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var var = p.Name;
                var color = (string)p.GetValue(ColorMappings);
                var _data = _mappingPalette[var];
                string[] data = { _data[0], _data[1], color, _data[3] };
                _mappingPalette[var] = data;
            }
        }

        private void InitDeviceDataGrid()
        {
            //
        }

        private async void SetupDeviceDataGrid()
        {
            try
            {
                DeviceGridStartup = false;
                dG_devices.AllowUserToAddRows = true;
                dG_devices.Rows.Clear();
                
                if (LifxSdkCalled == 1)
                {
                    DataGridViewRow[] dgV = new DataGridViewRow[_lifx.LifxBulbsDat.ToList().Count];
                    var i = 0;

                    if (_lifx.LifxBulbs > 0)
                        foreach (var d in _lifx.LifxBulbsDat.ToList())
                        {
                            Thread.Sleep(1);
                            
                            //LIFX Device
                            var state = await _lifx.GetLightStateAsync(d.Key);
                            var device = await _lifx.GetDeviceVersionAsync(d.Key);

                            _dGmode.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                            _dGmode.DisplayStyleForCurrentCellOnly = true;
                            var lState = _lifx.LifxStateMemory[d.Key.MacAddressName];

                            var devname = _lifx.LifXproductids[0];

                            if (_lifx.LifXproductids.ContainsKey(device.Product))
                            {
                                devname = _lifx.LifXproductids[device.Product];
                            }

                            var lifxdGDevice = (DataGridViewRow) dG_devices.Rows[0].Clone();
                            lifxdGDevice.Cells[dG_devices.Columns["col_devicename"].Index].Value = state.Label + " (" +
                                                                                                   d.Key
                                                                                                       .MacAddressName +
                                                                                                   ")";
                            lifxdGDevice.Cells[dG_devices.Columns["col_devicetype"].Index].Value =
                                devname + " (Version " + device.Version + ")";
                            lifxdGDevice.Cells[dG_devices.Columns["col_status"].Index].Value = lState == 0
                                ? "Disabled"
                                : "Enabled";
                            lifxdGDevice.Cells[dG_devices.Columns["col_state"].Index].Value = lState != 0;
                            lifxdGDevice.Cells[dG_devices.Columns["col_dattype"].Index].Value = "LIFX";
                            lifxdGDevice.Cells[dG_devices.Columns["col_ID"].Index].Value = d.Key.MacAddressName;

                            var lifxdGDeviceDgc = new DataGridViewComboBoxCell();

                            foreach (var x in _bulbModes.ToList())
                            {
                                if (d.Value != 0 && x.Key == BulbModeTypes.Disabled) continue;
                                if (x.Key == BulbModeTypes.ChromaticsDefault) continue;
                                lifxdGDeviceDgc.Items.Add(x.Value);
                            }

                            lifxdGDeviceDgc.Value = d.Value == BulbModeTypes.ChromaticsDefault
                                ? _bulbModes[BulbModeTypes.Standby]
                                : _bulbModes[d.Value];
                            lifxdGDeviceDgc.DisplayStyleForCurrentCellOnly = true;
                            lifxdGDeviceDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;

                            lifxdGDevice.Cells[dG_devices.Columns["col_mode"].Index] = lifxdGDeviceDgc;
                            
                            //Check for duplicates

                            /*
                            var duplicate = false;

                            if (DeviceGridStartup)
                            {
                                foreach (DataGridViewRow row in dG_devices.Rows)
                                {
                                    if (row.Cells[dG_devices.Columns["col_ID"].Index].Value.ToString().Contains(d.Key.MacAddressName))
                                    {
                                        duplicate = true;
                                        break;
                                    }
                                    
                                }
                            }

                            if (duplicate) continue;
                            */

                            //dG_devices.Rows.Add(lifxdGDevice);
                            dgV[i] = lifxdGDevice;
                            i++;
                            lifxdGDeviceDgc.ReadOnly = d.Value == 0 ? true : false;
                        }

                    dG_devices.Rows.AddRange(dgV);
                        
                }
                
                DeviceGridStartup = true;
                dG_devices.AllowUserToAddRows = false;
                

                
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, @"EX: " + ex.Message);
                WriteConsole(ConsoleTypes.Error, @"EX: " + ex.StackTrace);
            }
        }

        void dG_devices_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (this.dG_devices.IsCurrentCellDirty)
            {
                dG_devices.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dG_devices_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (DeviceGridStartup)
            {
                GlobalStopCycleEffects();

                var editedCell = dG_devices.Rows[e.RowIndex].Cells[e.ColumnIndex];
                var dattype = (string)dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_dattype"].Index]
                    .Value;

                if (dG_devices.Columns[e.ColumnIndex].Name == "col_state")
                {
                    var _switch = (bool)editedCell.Value;
                    var modeX = (string)dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_mode"].Index].Value;
                    var key = _bulbModes.FirstOrDefault(x => x.Value == modeX).Key;
                    var change = dG_devices.CurrentRow.Cells;

                    if (dattype == "LIFX")
                    {
                        var id = (string)dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_ID"].Index].Value;
                        if (LifxSdkCalled == 1 && id != null)
                        {
                            var bulb = _lifx.LifxDevices[id];

                            if (_switch)
                            {
                                _lifx.LifxBulbsDat[bulb] = _lifx.LifxModeMemory[id];
                                _lifx.LifxStateMemory[bulb.MacAddressName] = 1;
                                WriteConsole(ConsoleTypes.Lifx, @"Enabled LIFX Bulb " + id + @" on mode " + _lifx.LifxModeMemory[id]);

                                switch (_lifx.LifxModeMemory[id])
                                {
                                    case BulbModeTypes.Standby:
                                        var state = _lifx.LifxBulbsRestore[bulb];
                                        _lifx.SetColorAsync(bulb, state.Hue, state.Saturation, state.Brightness,
                                            state.Kelvin,
                                            TimeSpan.FromMilliseconds(1000));
                                        WriteConsole(ConsoleTypes.Lifx, @"Restoring LIFX Bulb " + state.Label);
                                        break;
                                    case BulbModeTypes.BaseMode:
                                    case BulbModeTypes.HighlightColor:
                                        SetKeysbase = false;
                                        break;
                                }
                            }
                            else
                            {
                                _lifx.LifxModeMemory[id] = key;
                                _lifx.LifxStateMemory[bulb.MacAddressName] = 0;
                                _lifx.LifxBulbsDat[bulb] = 0;
                                var state = _lifx.LifxBulbsRestore[bulb];
                                WriteConsole(ConsoleTypes.Lifx, @"Disabled LIFX Bulb " + id);
                                _lifx.SetColorAsync(bulb, state.Hue, state.Saturation, state.Brightness, state.Kelvin,
                                    TimeSpan.FromMilliseconds(1000));
                                WriteConsole(ConsoleTypes.Lifx, @"Restoring LIFX Bulb " + state.Label);
                            }

                            change[dG_devices.Columns["col_status"].Index].Value = _switch ? "Enabled" : "Disabled";
                        }
                    }
                }

                if (dG_devices.Columns[e.ColumnIndex].Name == "col_mode")
                {
                    var mode = (string)editedCell.Value;
                    var id = (string)dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_ID"].Index].Value;

                    if (LifxSdkCalled == 1 && id != null && dattype == "LIFX")
                        if (_bulbModes.ContainsValue(mode))
                        {
                            var key = _bulbModes.FirstOrDefault(x => x.Value == mode).Key;
                            var bulb = _lifx.LifxDevices[id];
                            _lifx.LifxBulbsDat[bulb] = key;
                            _lifx.LifxModeMemory[bulb.MacAddressName] = key;

                            WriteConsole(ConsoleTypes.Lifx, @"Updated Mode of LIFX Bulb " + id + " to " + key);

                            switch (_lifx.LifxModeMemory[id])
                            {
                                case BulbModeTypes.Standby:
                                    var state = _lifx.LifxBulbsRestore[bulb];
                                    _lifx.SetColorAsync(bulb, state.Hue, state.Saturation, state.Brightness,
                                        state.Kelvin,
                                        TimeSpan.FromMilliseconds(1000));
                                    WriteConsole(ConsoleTypes.Lifx, @"Restoring LIFX Bulb " + state.Label);
                                    break;
                                case BulbModeTypes.BaseMode:
                                case BulbModeTypes.HighlightColor:
                                    SetKeysbase = false;
                                    break;
                            }
                            
                        }
                }

                SaveDevices();
                //ResetDeviceDataGrid();

                /*
                if (RazerSdkCalled == 1)
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                */
            }
        }

        private void CenterPictureBox(PictureBox picBox, Image picImage)
        {
            /*
            picBox.Image = picImage;
            picBox.Location = new Point((picBox.Parent.ClientSize.Width / 2) - (picImage.Width / 2),
                                        (picBox.Parent.ClientSize.Height / 2) - (picImage.Height / 2));
            picBox.Refresh();
            */
            var xpos = picBox.Parent.Width / 2 - picImage.Width / 2;
            var ypos = picBox.Parent.Height / 2 - picImage.Height / 2;
            picBox.Location = new Point(xpos, ypos);
        }

        private void dG_mappings_SelectionChanged(object sender, EventArgs e)
        {
            var color = dG_mappings.CurrentRow.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor;
            ToggleMappingControls(true);
            mapping_colorEditorManager.Color = color;
            previewPanel.BackColor = color;
            //PaletteMappingCurrentSelect = dG_mappings.CurrentRow.Index;
        }

        private void dG_mappings_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            e.PaintParts &= ~DataGridViewPaintParts.Focus;
        }

        private void mapping_colorEditorManager_ColorChanged(object sender, EventArgs e)
        {
            var pmcs = dG_mappings.CurrentRow;
            var pcmsid = (string)pmcs.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value;
            var pcmsColor = pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor;

            previewPanel.BackColor = mapping_colorEditorManager.Color;

            if (pcmsColor != mapping_colorEditorManager.Color)
            {
                var _data = _mappingPalette[pcmsid];
                string[] data = { _data[0], _data[1], _data[2], ColorTranslator.ToHtml(pcmsColor) };
                _mappingPalette[pcmsid] = data;

                foreach (var p in typeof(FfxivColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (p.Name == pcmsid)
                        p.SetValue(ColorMappings, ColorTranslator.ToHtml(mapping_colorEditorManager.Color));

                DrawMappingsDict();
                pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor =
                    mapping_colorEditorManager.Color;
                pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.SelectionBackColor =
                    mapping_colorEditorManager.Color;

                SetKeysbase = false;
                SetMousebase = false;
                SetPadbase = false;
                SaveColorMappings(0);
            }
        }

        private void btn_palette_undo_Click(object sender, EventArgs e)
        {
            var pmcs = dG_mappings.CurrentRow;
            var pcmsid = (string)pmcs.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value;

            var cm = new FfxivColorMappings();
            var _restore = ColorTranslator.ToHtml(Color.Black);

            foreach (var p in typeof(FfxivColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (p.Name == pcmsid)
                    _restore = (string)p.GetValue(cm);

            var restore = ColorTranslator.FromHtml(_restore);
            mapping_colorEditorManager.Color = restore;
        }

        private void loadPaletteButton_Click(object sender, EventArgs e)
        {
            using (FileDialog dialog = new OpenFileDialog
            {
                Filter = PaletteSerializer.DefaultOpenFilter,
                DefaultExt = "pal",
                Title = @"Open Palette File"
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    try
                    {
                        IPaletteSerializer serializer;

                        serializer = PaletteSerializer.GetSerializer(dialog.FileName);
                        if (serializer != null)
                        {
                            ColorCollection palette;

                            if (!serializer.CanRead)
                                throw new InvalidOperationException("Serializer does not support reading palettes.");

                            using (var file = File.OpenRead(dialog.FileName))
                            {
                                palette = serializer.Deserialize(file);
                            }

                            if (palette != null)
                            {
                                // we can only display 96 colors in the color grid due to it's size, so if there's more, bin them
                                while (palette.Count > 96)
                                    palette.RemoveAt(palette.Count - 1);

                                // or if we have less, fill in the blanks
                                while (palette.Count < 96)
                                    palette.Add(Color.White);

                                colorGrid1.Colors = palette;
                            }
                        }
                        else
                        {
                            MessageBox.Show(
                                @"Sorry, unable to open palette, the file format is not supported or is not recognized.",
                                @"Load Palette", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format(@"Sorry, unable to open palette. {0}", ex.GetBaseException().Message),
                            @"Load Palette", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
            }
        }

        private void btn_importChromatics_Click(object sender, EventArgs e)
        {
            ImportColorMappings();
        }

        private void btn_exportChromatics_Click(object sender, EventArgs e)
        {
            ExportColorMappings();
        }

        private void SetupTooltips()
        {
            var ttBtnImport = new ToolTip();
            ttBtnImport.SetToolTip(btn_importChromatics, "Import Chromatics Color Palette");

            var ttBtnExport = new ToolTip();
            ttBtnExport.SetToolTip(btn_exportChromatics, "Export Chromatics Color Palette");

            var ttBtnLoadpalette = new ToolTip();
            ttBtnLoadpalette.SetToolTip(loadPaletteButton, "Import External Color Swatches");

            var ttBtnPaletteUndo = new ToolTip();
            ttBtnPaletteUndo.SetToolTip(btn_palette_undo, "Restore to Default");
        }

        private void InitSettingsGui()
        {
            chk_arxtoggle.Checked = ChromaticsSettings.ChromaticsSettingsArxToggle;
            cb_arx_theme.SelectedIndex = ChromaticsSettings.ChromaticsSettingsArxTheme;
            mi_arxenable.Checked = ChromaticsSettings.ChromaticsSettingsArxToggle;
            chk_lccauto.Checked = ChromaticsSettings.ChromaticsSettingsLccAuto;
            chk_memorycache.Checked = ChromaticsSettings.ChromaticsSettingsMemoryCache;
            chk_desktopnotify.Checked = ChromaticsSettings.ChromaticsSettingsDesktopNotifications;
            chk_cutscenes.Checked = ChromaticsSettings.ChromaticsSettingsCutsceneAnimation;
            chk_vegasmode.Checked = ChromaticsSettings.ChromaticsSettingsVegasMode;
            chk_quickclose.Checked = ChromaticsSettings.ChromaticsSettingsQuickClose;

            cb_qwerty.SelectedIndex = (int)ChromaticsSettings.ChromaticsSettingsQwertyMode;

            chk_castanimatetoggle.Checked = ChromaticsSettings.ChromaticsSettingsCastAnimate;
            chk_castchargetoggle.Checked = ChromaticsSettings.ChromaticsSettingsCastToggle;
            chk_gcdcounttoggle.Checked = ChromaticsSettings.ChromaticsSettingsGcdCountdown;

            chk_keybindtoggle.Checked = ChromaticsSettings.ChromaticsSettingsKeybindToggle;
            chk_jobgaugetoggle.Checked = ChromaticsSettings.ChromaticsSettingsJobGaugeToggle;
            chk_highlighttoggle.Checked = ChromaticsSettings.ChromaticsSettingsKeyHighlights;
            chk_impactflashtog.Checked = ChromaticsSettings.ChromaticsSettingsImpactToggle;
            chk_dfbelltoggle.Checked = ChromaticsSettings.ChromaticsSettingsDfBellToggle;
            chk_showstatuseffects.Checked = ChromaticsSettings.ChromaticsSettingsStatusEffectToggle;
            chk_enablebulbextraeffect.Checked = ChromaticsSettings.ChromaticsSettingsExtraBulbEffects;
            chk_releasedevices.Checked = ChromaticsSettings.ChromaticsSettingsReleaseDevices;

            chk_lcdtoggle.Checked = ChromaticsSettings.ChromaticsSettingsLcdEnabled;
            cb_lang.SelectedIndex = ChromaticsSettings.ChromaticsSettingsLanguage;

            chk_debugopt.Checked = ChromaticsSettings.ChromaticsSettingsDebugOpt;

            nm_criticalhp.Value = ChromaticsSettings.ChromaticsSettingsCriticalHP;
            chk_actflash.Checked = ChromaticsSettings.ChromaticsSettingsACTFlash;
            chk_actflashtrigger.Checked = ChromaticsSettings.ChromaticsSettingsACTFlashCustomTrigger;
            chk_actflashtimer.Checked = ChromaticsSettings.ChromaticsSettingsACTFlashTimer;

            chk_enablecast.Checked = ChromaticsSettings.ChromaticsSettingsCastEnabled;
            chk_castdfbell.Checked = ChromaticsSettings.ChromaticsSettingsCastDFBell;
            chk_enabletimebell.Checked = ChromaticsSettings.ChromaticsSettingsCastAlarmBell;
            cb_alarmclock.SelectedIndex = cb_alarmclock.FindStringExact(ChromaticsSettings.ChromaticsSettingsCastAlarmTime);
            chk_castsrank.Checked = ChromaticsSettings.ChromaticsSettingsCastSRankAlert;
            chk_castreadycheck.Checked = ChromaticsSettings.ChromaticsSettingsCastReadyCheckAlert;
            cb_bcm.SelectedIndex = cb_bcm.FindStringExact(ChromaticsSettings.ChromaticsSettingsBaseMode);

            nm_ledcount_z1.Value = _ChromaLinkLEDCountZ1;
            nm_ledcount_z2.Value = _ChromaLinkLEDCountZ2;
            nm_ledcount_z3.Value = _ChromaLinkLEDCountZ3;
            nm_ledcount_z4.Value = _ChromaLinkLEDCountZ4;
            nm_ledcount_z5.Value = _ChromaLinkLEDCountZ5;
            nm_ledcount_z6.Value = _ChromaLinkLEDCountZ6;

            nm_keymulti_led.Value = _KeyMultiLEDCount;
            chk_keymulti_rev.Checked = _KeyMultiReverse;

            chk_sdk_razer.Checked = _SDKRazer;
            chk_sdk_logi.Checked = _SDKLogitech;
            chk_sdk_corsair.Checked = _SDKCorsair;
            chk_sdk_cooler.Checked = _SDKCooler;
            chk_sdk_steel.Checked = _SDKSteelSeries;
            chk_sdk_wooting.Checked = _SDKWooting;
            chk_sdk_asus.Checked = _SDKAsus;
            chk_sdk_lifx.Checked = _SDKLifx;
            chk_sdk_mystic.Checked = _SDKMystic;

            if (ChromaticsSettings.ChromaticsSettingsCastEnabled)
            {
                chk_castdfbell.Enabled = true;
                cb_castdevlist.Enabled = true;
                lbl_chromecastdev.Enabled = true;
                chk_castsrank.Enabled = true;
                chk_castreadycheck.Checked = true;

                if (cb_castdevlist.Items.Count > 0)
                {
                    btn_casttest.Enabled = true;
                }
            }
            else
            {
                chk_castdfbell.Enabled = false;
                cb_castdevlist.Enabled = false;
                lbl_chromecastdev.Enabled = false;
                btn_casttest.Enabled = false;
                chk_castsrank.Enabled = false;
                chk_castreadycheck.Checked = false;
            }

            chk_enableifttt.Checked = ChromaticsSettings.ChromaticsSettingsIFTTTEnable;

            if (ChromaticsSettings.ChromaticsSettingsIFTTTEnable)
            {
                lbl_IFTTTcode.Enabled = true;
                txt_iftttmakerurl.Enabled = true;
                lbl_iftttmakerurlexample.Enabled = true;
                btn_ifttthelp.Enabled = true;
                dgv_iftttgrid.Enabled = true;
            }
            else
            {
                lbl_IFTTTcode.Enabled = false;
                txt_iftttmakerurl.Enabled = false;
                lbl_iftttmakerurlexample.Enabled = false;
                btn_ifttthelp.Enabled = false;
                dgv_iftttgrid.Enabled = false;
            }

            txt_iftttmakerurl.Text = ChromaticsSettings.ChromaticsSettingsIFTTTURL;

            chk_dev_keyboard.Checked = _deviceKeyboard;
            chk_dev_mouse.Checked = _deviceMouse;
            chk_dev_mousepad.Checked = _deviceMousepad;
            chk_dev_headset.Checked = _deviceHeadset;
            chk_dev_keypad.Checked = _deviceKeypad;
            chk_dev_chromalink.Checked = _deviceCL;

            cb_pollingint.SelectedIndex = cb_pollingint.FindStringExact(ChromaticsSettings.ChromaticsSettingsPollingInterval + @"ms");

            foreach (var item in _devModesA)
            {
                cb_mouse_z1.Items.Add(item.Value);
                cb_mouse_z2.Items.Add(item.Value);
                cb_mouse_z3.Items.Add(item.Value);
                cb_mouse_zs1.Items.Add(item.Value);
                cb_mouse_zs2.Items.Add(item.Value);
                cb_pad_zs1.Items.Add(item.Value);
                cb_pad_zs2.Items.Add(item.Value);
                cb_pad_zs3.Items.Add(item.Value);
                cb_headset_z1.Items.Add(item.Value);
                cb_headset_z2.Items.Add(item.Value);
                cb_chromalink_z1.Items.Add(item.Value);
                cb_chromalink_z2.Items.Add(item.Value);
                cb_chromalink_z3.Items.Add(item.Value);
                cb_chromalink_z4.Items.Add(item.Value);
                cb_chromalink_z5.Items.Add(item.Value);
                cb_chromalink_z6.Items.Add(item.Value);
            }

            foreach (var item in _devModesA)
            {
                if (item.Key == DevModeTypes.ReactiveWeather || item.Key == DevModeTypes.BattleStance || item.Key == DevModeTypes.JobClass)
                {
                    continue;
                }

                cb_singlezonemode.Items.Add(item.Value);
            }

            chk_keypad_binds.Checked = _EnableKeypadBinds;

            foreach (var item in _devModesMulti)
            {
                if (item.Key == DevMultiModeTypes.ReactiveWeather) continue;
                if (item.Key == DevMultiModeTypes.StatusEffects) continue;
                
                cb_multizonemode.Items.Add(item.Value);
            }

            foreach (var item in _devModesMulti)
            {
                if (item.Key == DevMultiModeTypes.StatusEffects) continue;

                cb_keypad_z1.Items.Add(item.Value);
            }

            foreach (var item in _lightbarModes)
            {
                cb_lightbarmode.Items.Add(item.Value);
            }

            foreach (var item in _fkeyModes)
            {
                cb_fkeymode.Items.Add(item.Value);
            }

            foreach (var item in _actModes)
            {
                cb_actmode.Items.Add(item.Value);
            }

            foreach (var item in _actJobs)
            {
                cb_actjobclass.Items.Add(item.Value);
            }

            cb_singlezonemode.SelectedItem = Helpers.ConvertDevModeToCB(_KeysSingleKeyMode);
            cb_multizonemode.SelectedItem = Helpers.ConvertDevMultiModeToCB(_KeysMultiKeyMode);

            cb_lightbarmode.SelectedItem = Helpers.ConvertLightbarModeToCB(_LightbarMode);
            cb_fkeymode.SelectedItem = Helpers.ConvertFKeyModeToCB(_FKeyMode);


            cb_actmode.SelectedItem = ChromaticsSettings.ChromaticsSettingsACTMode;
            cb_actjobclass.SelectedIndex = 0;


            cb_mouse_z1.SelectedItem = Helpers.ConvertDevModeToCB(_MouseZone1Mode);
            cb_mouse_z2.SelectedItem = Helpers.ConvertDevModeToCB(_MouseZone2Mode);
            cb_mouse_z3.SelectedItem = Helpers.ConvertDevModeToCB(_MouseZone3Mode);
            cb_mouse_zs1.SelectedItem = Helpers.ConvertDevModeToCB(_MouseStrip1Mode);
            cb_mouse_zs2.SelectedItem = Helpers.ConvertDevModeToCB(_MouseStrip2Mode);
            cb_pad_zs1.SelectedItem = Helpers.ConvertDevModeToCB(_PadZone1Mode);
            cb_pad_zs2.SelectedItem = Helpers.ConvertDevModeToCB(_PadZone2Mode);
            cb_pad_zs3.SelectedItem = Helpers.ConvertDevModeToCB(_PadZone3Mode);
            cb_headset_z1.SelectedItem = Helpers.ConvertDevModeToCB(_HeadsetZone1Mode);
            cb_headset_z2.SelectedItem = Helpers.ConvertDevModeToCB(_HeadsetZone2Mode);
            cb_keypad_z1.SelectedItem = Helpers.ConvertDevMultiModeToCB(_KeypadZone1Mode);
            cb_chromalink_z1.SelectedItem = Helpers.ConvertDevModeToCB(_CLZone1Mode);
            cb_chromalink_z2.SelectedItem = Helpers.ConvertDevModeToCB(_CLZone2Mode);
            cb_chromalink_z3.SelectedItem = Helpers.ConvertDevModeToCB(_CLZone3Mode);
            cb_chromalink_z4.SelectedItem = Helpers.ConvertDevModeToCB(_CLZone4Mode);
            cb_chromalink_z5.SelectedItem = Helpers.ConvertDevModeToCB(_CLZone5Mode);
            cb_chromalink_z6.SelectedItem = Helpers.ConvertDevModeToCB(_CLZone6Mode);

            chk_keys_singlemode.Checked = _KeysSingleKeyModeEnabled;
            cb_singlezonemode.Enabled = _KeysSingleKeyModeEnabled;

            chk_keys_multimode.Checked = _KeysMultiKeyModeEnabled;
            cb_multizonemode.Enabled = _KeysMultiKeyModeEnabled;
            lbl_multiledcnt.Enabled = _KeysMultiKeyModeEnabled;
            nm_keymulti_led.Enabled = _KeysMultiKeyModeEnabled;
            chk_keymulti_rev.Enabled = _KeysMultiKeyModeEnabled;

            chk_other_interp.Checked = _OtherInterpolateEffects;
            chk_other_interpreverse.Checked = _ReverseInterpolateEffects;

            //Setup Keypad Keybinds

            if (_KeyBindMap.ContainsKey(1))
            {
                dgv_keypad_binds.Rows.Add("0", "1", _KZ1Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[1]), _KeyBindMap[1]);
            }

            if (_KeyBindMap.ContainsKey(2))
            {
                dgv_keypad_binds.Rows.Add("1", "2", _KZ2Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[2]), _KeyBindMap[2]);
            }


            if (_KeyBindMap.ContainsKey(3))
            {
                dgv_keypad_binds.Rows.Add("2", "3", _KZ3Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[3]), _KeyBindMap[3]);
            }

            if (_KeyBindMap.ContainsKey(4))
            {
                dgv_keypad_binds.Rows.Add("3", "4", _KZ4Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[4]), _KeyBindMap[4]);
            }

            if (_KeyBindMap.ContainsKey(5))
            {
                dgv_keypad_binds.Rows.Add("4", "5", _KZ5Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[5]), _KeyBindMap[5]);
            }

            if (_KeyBindMap.ContainsKey(6))
            {
                dgv_keypad_binds.Rows.Add("5", "6", _KZ6Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[6]), _KeyBindMap[6]);
            }

            if (_KeyBindMap.ContainsKey(7))
            {
                dgv_keypad_binds.Rows.Add("6", "7", _KZ7Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[7]), _KeyBindMap[7]);
            }

            if (_KeyBindMap.ContainsKey(8))
            {
                dgv_keypad_binds.Rows.Add("7", "8", _KZ8Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[8]), _KeyBindMap[8]);
            }

            if (_KeyBindMap.ContainsKey(9))
            {
                dgv_keypad_binds.Rows.Add("8", "9", _KZ9Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[9]), _KeyBindMap[9]);
            }

            if (_KeyBindMap.ContainsKey(10))
            {
                dgv_keypad_binds.Rows.Add("9", "10", _KZ10Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[10]), _KeyBindMap[10]);
            }

            if (_KeyBindMap.ContainsKey(11))
            {
                dgv_keypad_binds.Rows.Add("10", "11", _KZ11Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[11]), _KeyBindMap[11]);
            }

            if (_KeyBindMap.ContainsKey(12))
            {
                dgv_keypad_binds.Rows.Add("11", "12", _KZ12Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[12]), _KeyBindMap[12]);
            }

            if (_KeyBindMap.ContainsKey(13))
            {
                dgv_keypad_binds.Rows.Add("12", "13", _KZ13Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[13]), _KeyBindMap[13]);
            }

            if (_KeyBindMap.ContainsKey(14))
            {
                dgv_keypad_binds.Rows.Add("13", "14", _KZ14Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[14]), _KeyBindMap[14]);
            }

            if (_KeyBindMap.ContainsKey(15))
            {
                dgv_keypad_binds.Rows.Add("14", "15", _KZ15Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[15]), _KeyBindMap[15]);
            }

            if (_KeyBindMap.ContainsKey(16))
            {
                dgv_keypad_binds.Rows.Add("15", "16", _KZ16Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[16]), _KeyBindMap[16]);
            }

            if (_KeyBindMap.ContainsKey(17))
            {
                dgv_keypad_binds.Rows.Add("16", "17", _KZ17Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[17]), _KeyBindMap[17]);
            }

            if (_KeyBindMap.ContainsKey(18))
            {
                dgv_keypad_binds.Rows.Add("17", "18", _KZ18Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[18]), _KeyBindMap[18]);
            }

            if (_KeyBindMap.ContainsKey(19))
            {
                dgv_keypad_binds.Rows.Add("18", "19", _KZ19Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[19]), _KeyBindMap[19]);
            }

            if (_KeyBindMap.ContainsKey(20))
            {
                dgv_keypad_binds.Rows.Add("19", "20", _KZ20Enabled, Helpers.ConvertModifiersToCB(_KeyBindModMap[20]), _KeyBindMap[20]);
            }

            if (!_EnableKeypadBinds)
            {
                dgv_keypad_binds.Enabled = false;
            }

            //Check for Depreciated Values
            
            if (_KeypadZone1Mode == DevMultiModeTypes.ReactiveWeather)
            {
                _KeypadZone1Mode = DevMultiModeTypes.BaseMode;
                cb_keypad_z1.SelectedItem = Helpers.ConvertDevMultiModeToString(DevMultiModeTypes.BaseMode);
            }

            if (_KeypadZone1Mode == DevMultiModeTypes.StatusEffects)
            {
                _KeypadZone1Mode = DevMultiModeTypes.BaseMode;
                cb_keypad_z1.SelectedItem = Helpers.ConvertDevMultiModeToString(DevMultiModeTypes.BaseMode);
            }

            if (_KeysMultiKeyMode == DevMultiModeTypes.ReactiveWeather)
            {
                _KeysMultiKeyMode = DevMultiModeTypes.BaseMode;
                cb_multizonemode.SelectedItem = Helpers.ConvertDevMultiModeToString(DevMultiModeTypes.BaseMode);
            }

            if (_KeysMultiKeyMode == DevMultiModeTypes.StatusEffects)
            {
                _KeysMultiKeyMode = DevMultiModeTypes.BaseMode;
                cb_multizonemode.SelectedItem = Helpers.ConvertDevMultiModeToString(DevMultiModeTypes.BaseMode);
            }
            
        }

        private void InitDevicesGui()
        {
            if (RazerSdkCalled == 1)
            {

                _razerDeviceKeyboard = _deviceKeyboard;
                _razerDeviceMouse = _deviceMouse;
                _razerDeviceMousepad = _deviceMousepad;
                _razerDeviceKeypad = _deviceKeypad;
                _razerDeviceHeadset = _deviceHeadset;
                _razerDeviceChromaLink = _deviceCL;

                /*
                gP_ChromaLink.Enabled = true;
                chk_dev_chromalink.Enabled = true;
                cb_chromalink_z1.Enabled = true;
                cb_chromalink_z2.Enabled = true;
                cb_chromalink_z3.Enabled = true;
                cb_chromalink_z4.Enabled = true;
                cb_chromalink_z5.Enabled = true;
                */
            }
            else
            {
                _razerDeviceKeyboard = false;
                _razerDeviceMouse = false;
                _razerDeviceMousepad = false;
                _razerDeviceKeypad = false;
                _razerDeviceHeadset = false;
                _razerDeviceChromaLink = false;

                /*
                chk_dev_chromalink.Enabled = false;
                cb_chromalink_z1.Enabled = false;
                cb_chromalink_z2.Enabled = false;
                cb_chromalink_z3.Enabled = false;
                cb_chromalink_z4.Enabled = false;
                cb_chromalink_z5.Enabled = false;
                gP_ChromaLink.Enabled = false;
                */
            }

            if (LogitechSdkCalled == 1)
            {
                _logitechDeviceKeyboard = _deviceKeyboard;
                //_logitechDeviceMouse = true;
                //_logitechDeviceMousepad = true;
                //_logitechDeviceKeypad = true;
                //_logitechDeviceHeadset = true;
            }
            else
            {
                _logitechDeviceKeyboard = false;
                //_logitechDeviceMouse = false;
                //_logitechDeviceMousepad = false;
                //_logitechDeviceKeypad = false;
                //_logitechDeviceHeadset = false;

                chk_lcdtoggle.Enabled = false;
            }

            if (CorsairSdkCalled == 1)
            {
                _corsairDeviceKeyboard = _deviceKeyboard;
                _corsairDeviceMouse = _deviceMouse;
                _corsairDeviceMousepad = _deviceMousepad;
                _corsairDeviceKeypad = _deviceKeypad;
                _corsairDeviceHeadset = _deviceHeadset;
            }
            else
            {
                _corsairDeviceKeyboard = false;
                _corsairDeviceMouse = false;
                _corsairDeviceMousepad = false;
                _corsairDeviceKeypad = false;
                _corsairDeviceHeadset = false;
            }

            if (CoolermasterSdkCalled == 1)
            {
                _coolermasterDeviceKeyboard = _deviceKeyboard;
                _coolermasterDeviceMouse = _deviceMouse;
                //_coolermasterDeviceMousepad = true;
                //_coolermasterDeviceKeypad = true;
                //_coolermasterDeviceHeadset = true;
            }
            else
            {
                _coolermasterDeviceKeyboard = false;
                _coolermasterDeviceMouse = false;
                //_coolermasterDeviceMousepad = false;
                //_coolermasterDeviceKeypad = false;
                //_coolermasterDeviceHeadset = false;
            }

            if (SteelSdkCalled == 1)
            {
                _steelDeviceKeyboard = _deviceKeyboard;
                _steelDeviceMouse = _deviceMouse;
                //_steelDeviceMousepad = true;
                //_steelDeviceKeypad = true;
                _steelDeviceHeadset = true;
            }
            else
            {
                _steelDeviceKeyboard = false;
                _steelDeviceMouse = false;
                //_steelDeviceMousepad = false;
                //_steelDeviceKeypad = false;
                _steelDeviceHeadset = false;
            }

            if (WootingSdkCalled == 1)
            {
                _wootingDeviceKeyboard = _deviceKeyboard;
            }
            else
            {
                _wootingDeviceKeyboard = false;
            }

            if (RoccatSdkCalled == 1)
            {
                _roccatDeviceKeyboard = _deviceKeyboard;
                _roccatDeviceMouse = _deviceMouse;
                //_roccatDeviceMousepad = true;
                //_roccatDeviceKeypad = true;
                //_roccatDeviceHeadset = true;
            }
            else
            {
                _roccatDeviceKeyboard = false;
                _roccatDeviceMouse = false;
                //_roccatDeviceMousepad = false;
                //_roccatDeviceKeypad = false;
                //_roccatDeviceHeadset = false;
            }

            GlobalResetDevices();
        }

        private void InitSettingsArxGui()
        {
            if (cb_arx_mode.Items.Contains(ChromaticsSettings.ChromaticsSettingsArxMode))
            {
                cb_arx_mode.SelectedItem = ChromaticsSettings.ChromaticsSettingsArxMode;
            }
            else
            {
                var chromaticsSettings = new ChromaticsSettings();
                cb_arx_mode.SelectedItem = chromaticsSettings.ChromaticsSettingsArxMode;
            }

            txt_arx_actip.Text = ChromaticsSettings.ChromaticsSettingsArxactip;
        }

        private void chk_arxtoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsArxToggle = chk_arxtoggle.Checked;
            SaveChromaticsSettings(1);

            if (chk_arxtoggle.Checked)
            {
                ArxToggle = true;
                cb_arx_theme.Enabled = true;
                cb_arx_mode.Enabled = true;

                if (ArxSdkCalled == 0)
                {
                    _arx = LogitechArxInterface.InitializeArxSdk();
                    if (_arx != null)
                    {
                        ArxSdk = true;
                        ArxSdkCalled = 1;
                        ArxState = 0;

                        WriteConsole(ConsoleTypes.Arx, @"ARX SDK Enabled");

                        LoadArxPlugins();

                        var theme = "light";
                        var themeid = cb_arx_theme.SelectedIndex;

                        switch (themeid)
                        {
                            case 0:
                                theme = "light";
                                break;
                            case 1:
                                theme = "dark";
                                break;
                            case 2:
                                theme = "grey";
                                break;
                            case 3:
                                theme = "black";
                                break;
                            case 4:
                                theme = "cycle";
                                break;
                        }

                        _arx.ArxUpdateTheme(theme);
                    }
                }
            }
            else
            {
                ArxToggle = false;
                cb_arx_theme.Enabled = false;
                cb_arx_mode.Enabled = false;

                if (Plugs.Count > 0)
                    foreach (var plugin in Plugs)
                        if (cb_arx_mode.Items.Contains(plugin))
                            cb_arx_mode.Items.Remove(plugin);

                if (ArxSdkCalled == 1)
                {
                    _arx.ShutdownArx();
                    ArxSdkCalled = 0;
                    WriteConsole(ConsoleTypes.Arx, @"ARX SDK Disabled");
                }
            }
        }

        private void cb_arx_theme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ArxSdkCalled != 1) return;
            if (Startup == false) return;
            var theme = "light";
            var themeid = cb_arx_theme.SelectedIndex;
            ChromaticsSettings.ChromaticsSettingsArxTheme = cb_arx_theme.SelectedIndex;
            SaveChromaticsSettings(1);

            switch (themeid)
            {
                case 0:
                    theme = "light";
                    break;
                case 1:
                    theme = "dark";
                    break;
                case 2:
                    theme = "grey";
                    break;
                case 3:
                    theme = "black";
                    break;
                case 4:
                    theme = "cycle";
                    break;
            }

            _arx.ArxUpdateTheme(theme);
        }

        private void cb_arx_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ArxSdkCalled != 1) return;
            if (Startup == false) return;
            
            if (cb_arx_mode.SelectedIndex < 3)
            {
                ArxState = cb_arx_mode.SelectedIndex + 1;
                _arx.SetArxCurrentID(ArxState);

                switch (ArxState)
                {
                    case 1:
                        _arx.ArxSetIndex("playerhud.html");
                        break;
                    case 2:
                        _arx.ArxSetIndex("partylist.html");
                        break;
                    case 3:
                        _arx.ArxSetIndex("act.html");

                        var changed = txt_arx_actip.Text;
                        if (changed.EndsWith("/"))
                        {
                            changed = changed.Substring(0, changed.Length - 1);
                        }

                        _arx.ArxSendActInfo(changed, 8085);
                        break;
                }
            }
            else
            {
                var getPlugin = cb_arx_mode.SelectedItem + ".html";
                ArxState = 100;
                _arx.ArxSetIndex(getPlugin);
            }
            

            WriteConsole(ConsoleTypes.Arx, @"ARX Template Changed: " + cb_arx_mode.SelectedItem);

            ChromaticsSettings.ChromaticsSettingsArxMode = cb_arx_mode.SelectedItem.ToString();
            SaveChromaticsSettings(1);
        }

        private void rtb_debug_TextChanged(object sender, EventArgs e)
        {
            rtb_debug.SelectionStart = rtb_debug.Text.Length;
            rtb_debug.ScrollToCaret();
        }

        private void showwindow_Click(object sender, EventArgs e)
        {
            if (!_allowVisible)
            {
                _allowVisible = true;
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        private void mi_updatecheck_Click(object sender, EventArgs e)
        {
            CheckUpdates(1);
        }

        private void mi_winstart_Click(object sender, EventArgs e)
        {
            if (mi_winstart.Checked)
                _rkApp.SetValue("Chromatics", Application.ExecutablePath);
            else
                _rkApp.DeleteValue("Chromatics", false);

            chk_startupenable.Checked = mi_winstart.Checked;
        }

        private void enableeffects_Click(object sender, EventArgs e)
        {
            //Enable effects
            if (mi_effectsenable.Checked)
            {
                HoldReader = true;
            }
            else
            {
                HoldReader = false;
            }

            ResetDeviceDataGrid();
        }

        private void enablearx_Click(object sender, EventArgs e)
        {
            if (mi_arxenable.Checked)
                chk_arxtoggle.Checked = true;
            else
                chk_arxtoggle.Checked = false;
        }

        private void notify_master_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!_allowVisible)
            {
                _allowVisible = true;
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        private void txt_arx_actip_TextChanged(object sender, EventArgs e)
        {
            if (Startup)
            {
                var changed = txt_arx_actip.Text;
                if (changed.EndsWith("/"))
                    changed = changed.Substring(0, changed.Length - 1);

                txt_arx_actip.Text = changed;
                _arx.ArxSendActInfo(changed, 8085);

                ChromaticsSettings.ChromaticsSettingsArxactip = txt_arx_actip.Text;
                SaveChromaticsSettings(1);
            }
        }

        private void chk_lccenable_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_lccenable.Checked)
                ToggleLccMode(true);
            else
                ToggleLccMode(false);
        }

        private void chk_lccauto_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsLccAuto = chk_lccauto.Checked;
            SaveChromaticsSettings(1);
        }

        private void btn_lccrestore_Click(object sender, EventArgs e)
        {
            if (LogitechSdkCalled == 0)
                return;

            var lccrestoreCheck =
                MessageBox.Show(
                    @"Are you sure you wish to restore LGS to its default settings? This should only be done as a last resort.",
                    @"Restore LGS Settings to Default", MessageBoxButtons.OKCancel);
            if (lccrestoreCheck == DialogResult.OK)
                try
                {
                    while (Process.GetProcessesByName("ffxiv_dx11").Length > 0)
                    {
                        var lccrestoreWarning =
                            MessageBox.Show(@"You must close Final Fantasy XIV before using restore.",
                                @"Please close Final Fantasy XIV", MessageBoxButtons.RetryCancel);
                        if (lccrestoreWarning == DialogResult.Cancel)
                            return;
                    }

                    if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                        File.Delete(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll");

                    if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled"))
                        File.Delete(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled");

                    var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    var path = enviroment + @"/LogitechLed.dll";

                    File.Copy(path, _lgsInstall + @"\SDK\LED\x64\LogitechLed.dll", true);
                    WriteConsole(ConsoleTypes.Logitech, @"LGS has been restored to its default settings.");

                    chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                    chk_lccenable.Checked = false;
                    LCCStatus = false;
                    chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error,
                        "An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                }
            else if (lccrestoreCheck == DialogResult.Cancel)
                return;
        }

        private void ToggleLccMode([Optional] bool force, [Optional] bool antilog)
        {
            if (LogitechSdkCalled == 0)
                return;

            var _force = false;

            if (!force)
                _force = true;


            if ((!LCCStatus || force) && !_force)
            {
                //Enable LCC

                if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                {
                    try
                    {
                        //File.Copy(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll", LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", true);
                        //File.Delete(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll");
                        FileSystem.RenameFile(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll", "LogitechLed.dll.disabled");
                        LCCStatus = true;
                    }
                    catch (Exception ex)
                    {
                        if (!antilog)
                            WriteConsole(ConsoleTypes.Error,
                                @"An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                        return;
                    }

                    if (!antilog)
                        WriteConsole(ConsoleTypes.Logitech, @"Logitech Conflict Mode Enabled.");
                }
                else
                {
                    if (!antilog)
                        WriteConsole(ConsoleTypes.Error,
                            @"An Error occurred trying to enable Logitech Conflict Mode. Error: LGS SDK Library not found (A).");
                }
            }
            else
            {
                //Disable LCC
                if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled"))
                {
                    try
                    {
                        //File.Copy(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", LgsInstall + @"\SDK\LED\x64\LogitechLed.dll", true);
                        //File.Delete(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled");
                        FileSystem.RenameFile(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", "LogitechLed.dll");
                        LCCStatus = false;
                    }
                    catch (Exception ex)
                    {
                        if (!antilog)
                            WriteConsole(ConsoleTypes.Error,
                                @"An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                        return;
                    }

                    if (!antilog)
                        WriteConsole(ConsoleTypes.Logitech, @"Logitech Conflict Mode Disabled.");
                }
                else
                {
                    if (!antilog)
                        WriteConsole(ConsoleTypes.Error,
                            @"An Error occurred trying to disable Logitech Conflict Mode. Error: LGS SDK Library not found (B).");
                }
            }
        }

        private void chk_memorycache_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsMemoryCache = chk_memorycache.Checked;
            SaveChromaticsSettings(1);
        }
        
        private void cb_qwerty_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsQwertyMode = (KeyRegion)cb_qwerty.SelectedIndex;
            Localization.SetKeyRegion(ChromaticsSettings.ChromaticsSettingsQwertyMode);

            SetKeysbase = true;
            SaveChromaticsSettings(1);
        }
        
        private void chk_startupenable_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_startupenable.Checked)
                _rkApp.SetValue("Chromatics", Application.ExecutablePath);
            else
                _rkApp.DeleteValue("Chromatics", false);

            mi_winstart.Checked = chk_startupenable.Checked;
        }

        private void chk_castchargetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsCastToggle = chk_castchargetoggle.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_castanimatetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsCastAnimate = chk_castanimatetoggle.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_gcdcounttoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsGcdCountdown = chk_gcdcounttoggle.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_highlighttoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsKeyHighlights = chk_highlighttoggle.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_jobgaugetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;
            
            ChromaticsSettings.ChromaticsSettingsJobGaugeToggle = chk_jobgaugetoggle.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
            
        }

        private void chk_keybindtoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsKeybindToggle = chk_keybindtoggle.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_impactflashtog_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsImpactToggle = chk_impactflashtog.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_dfbelltoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsDfBellToggle = chk_dfbelltoggle.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_dev_keyboard_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_dev_keyboard.Checked)
            {
                _deviceKeyboard = true;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceKeyboard = true;
                }

                if (LogitechSdkCalled == 1)
                {
                    _logitechDeviceKeyboard = true;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceKeyboard = true;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterDeviceKeyboard = true;
                }

                if (SteelSdkCalled == 1)
                {
                    _steelDeviceKeyboard = true;
                }

                if (WootingSdkCalled == 1)
                {
                    _wootingDeviceKeyboard = true;
                }

                if (RoccatSdkCalled == 1)
                {
                    _roccatDeviceKeyboard = true;
                }
                
            }
            else
            {
                _deviceKeyboard = false;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceKeyboard = false;
                }

                if (LogitechSdkCalled == 1)
                {
                    _logitechDeviceKeyboard = false;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceKeyboard = false;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterDeviceKeyboard = false;
                }

                if (SteelSdkCalled == 1)
                {
                    _steelDeviceKeyboard = false;
                }

                if (WootingSdkCalled == 1)
                {
                    _wootingDeviceKeyboard = false;
                }

                if (RoccatSdkCalled == 1)
                {
                    _roccatDeviceKeyboard = false;
                }
            }

            GlobalResetDevices();
            SaveDevices();
        }

        private void chk_dev_mouse_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;
            
            if (chk_dev_mouse.Checked)
            {
                _deviceMouse = true;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceMouse = true;

                    /*
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                        */
                }

                if (LogitechSdkCalled == 1)
                {
                    //_logitechDeviceMouse = true;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceMouse = true;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterDeviceMouse = true;
                }

                if (SteelSdkCalled == 1)
                {
                    _steelDeviceMouse = true;
                }

                if (RoccatSdkCalled == 1)
                {
                    _roccatDeviceMouse = true;
                }

            }
            else
            {
                _deviceMouse = false;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceMouse = false;

                    /*
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                        */
                }

                if (LogitechSdkCalled == 1)
                {
                    //_logitechDeviceMouse = false;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceMouse = false;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterDeviceMouse = false;
                }

                if (SteelSdkCalled == 1)
                {
                    _steelDeviceMouse = false;
                }

                if (RoccatSdkCalled == 1)
                {
                    _roccatDeviceMouse = false;
                }
            }

            GlobalResetDevices();
            SaveDevices();
        }

        private void chk_dev_mousepad_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_dev_mousepad.Checked)
            {
                _deviceMousepad = true;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceMousepad = true;

                    /*
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                        */
                }

                if (LogitechSdkCalled == 1)
                {
                    //_logitechDeviceMousepad = true;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceMousepad = true;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //_coolermasterDeviceMousepad = true;
                }

                if (RoccatSdkCalled == 1)
                {
                    //_roccatDeviceMousepad = true;
                }

            }
            else
            {

                _deviceMousepad = false;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceMousepad = false;

                    /*
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                        */
                }

                if (LogitechSdkCalled == 1)
                {
                    //_logitechDeviceMousepad = false;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceMousepad = false;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //_coolermasterDeviceMousepad = false;
                }

                if (RoccatSdkCalled == 1)
                {
                    //_roccatDeviceMousepad = false;
                }
            }

            GlobalResetDevices();
            SaveDevices();
        }

        private void chk_dev_headset_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_dev_headset.Checked)
            {
                _deviceHeadset = true;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceHeadset = true;

                    /*
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                        */
                }

                if (LogitechSdkCalled == 1)
                {
                    //_logitechDeviceHeadset = true;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceHeadset = true;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //_coolermasterDeviceHeadset = true;
                }

                if (SteelSdkCalled == 1)
                {
                    _steelDeviceHeadset = true;
                }

                if (RoccatSdkCalled == 1)
                {
                    //_roccatDeviceHeadset = true;
                }

            }
            else
            {
                _deviceHeadset = false;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceHeadset = false;

                    /*
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                        */
                }

                if (LogitechSdkCalled == 1)
                {
                    //_logitechDeviceHeadset = false;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceHeadset = false;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //_coolermasterDeviceHeadset = false;
                }

                if (SteelSdkCalled == 1)
                {
                    _steelDeviceHeadset = false;
                }

                if (RoccatSdkCalled == 1)
                {
                    //_roccatDeviceHeadset = false;
                }
            }

            GlobalResetDevices();
            SaveDevices();
        }

        private void chk_dev_keypad_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_dev_keypad.Checked)
            {
                _deviceKeypad = true;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceKeypad = true;

                    /*
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                        */
                }

                if (LogitechSdkCalled == 1)
                {
                    //_logitechDeviceKeypad = true;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceKeypad = true;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //_coolermasterDeviceKeypad = true;
                }

                if (RoccatSdkCalled == 1)
                {
                    //_roccatDeviceKeypad = true;
                }

            }
            else
            {
                _deviceKeypad = false;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceKeypad = false;

                    /*
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                        */
                }

                if (LogitechSdkCalled == 1)
                {
                    //_logitechDeviceKeypad = false;
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceKeypad = false;
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //_coolermasterDeviceKeypad = false;
                }

                if (RoccatSdkCalled == 1)
                {
                    //_roccatDeviceKeypad = false;
                }
            }

            GlobalResetDevices();
            SaveDevices();
        }
        
        private void chk_keys_singlemode_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_keys_singlemode.Checked)
            {
                _KeysSingleKeyModeEnabled = true;
                cb_singlezonemode.Enabled = true;

                _KeysMultiKeyModeEnabled = false;
                chk_keys_multimode.Enabled = false;
                cb_multizonemode.Enabled = false;
                lbl_multiledcnt.Enabled = false;
                nm_keymulti_led.Enabled = false;
                chk_keymulti_rev.Enabled = false;
            }
            else
            {
                _KeysSingleKeyModeEnabled = false;
                cb_singlezonemode.Enabled = false;

                chk_keys_multimode.Enabled = true;
                cb_multizonemode.Enabled = true;
                lbl_multiledcnt.Enabled = true;
                nm_keymulti_led.Enabled = true;
                chk_keymulti_rev.Enabled = true;
            }

            SetKeysbase = false;
            GlobalStopCycleEffects();
            SaveDevices();
        }

        private void chk_keys_multimode_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_keys_multimode.Checked)
            {
                _KeysMultiKeyModeEnabled = true;
                cb_multizonemode.Enabled = true;
                lbl_multiledcnt.Enabled = true;
                nm_keymulti_led.Enabled = true;
                chk_keymulti_rev.Enabled = true;

                _KeysSingleKeyModeEnabled = false;
                chk_keys_singlemode.Enabled = false;
                cb_singlezonemode.Enabled = false;
            }
            else
            {
                _KeysMultiKeyModeEnabled = false;
                cb_multizonemode.Enabled = false;
                lbl_multiledcnt.Enabled = false;
                nm_keymulti_led.Enabled = false;
                chk_keymulti_rev.Enabled = false;

                chk_keys_singlemode.Enabled = true;
                cb_singlezonemode.Enabled = true;
            }

            SetKeysbase = false;
            GlobalStopCycleEffects();
            SaveDevices();
        }

        private void cb_pollingint_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            var selected = cb_pollingint.GetItemText(cb_pollingint.SelectedItem).Replace("ms", "");
            var poll = 0;

            int.TryParse(selected, out poll);
            ChromaticsSettings.ChromaticsSettingsPollingInterval = poll;

            WriteConsole(ConsoleTypes.Ffxiv, @"Changing polling interval to " + (string)cb_pollingint.SelectedItem + @".");
            SaveChromaticsSettings(1);
        }

        private void cb_multizonemode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesMulti)
            {
                if ((string)cb_multizonemode.SelectedItem != item.Value) continue;
                _KeysMultiKeyMode = Helpers.ConvertCBToDevMultiMode(item.Value);
                break;
            }

            SetKeysbase = false;
            SaveDevices();
        }

        private void cb_lightbarmode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _lightbarModes)
            {
                if ((string)cb_lightbarmode.SelectedItem != item.Value) continue;
                _LightbarMode = Helpers.ConvertCBToLightbarMode(item.Value);
                break;
            }

            SetKeysbase = false;
            SaveDevices();
        }

        private void cb_fkeymode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _fkeyModes)
            {
                if ((string)cb_fkeymode.SelectedItem != item.Value) continue;
                _FKeyMode = Helpers.ConvertCBToFKeyMode(item.Value);
                break;
            }

            SetKeysbase = false;
            SaveDevices();
        }

        private void cb_actmode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _actModes)
            {
                if ((string)cb_actmode.SelectedItem != item.Value) continue;
                ChromaticsSettings.ChromaticsSettingsACTMode = (string)cb_actmode.SelectedItem;
                break;
            }
            
            SaveChromaticsSettings(1);
        }

        private void chk_actflash_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTFlash = chk_actflash.Checked;

            SaveChromaticsSettings(1);
        }
        
        private void chk_actflashtrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTFlashCustomTrigger = chk_actflashtrigger.Checked;

            SaveChromaticsSettings(1);
        }

        private void chk_actflashtimer_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTFlashTimer = chk_actflashtimer.Checked;

            SaveChromaticsSettings(1);
        }

        private void cb_actjobclass_SelectedIndexChanged(object sender, EventArgs e)
        {
            nm_acttargetdps.Value = ChromaticsSettings.ChromaticsSettingsACTDPS[cb_actjobclass.SelectedIndex][0];
            nm_acttargethps.Value = ChromaticsSettings.ChromaticsSettingsACTHPS[cb_actjobclass.SelectedIndex][0];
            nm_actgroupdps.Value = ChromaticsSettings.ChromaticsSettingsACTGroupDPS[cb_actjobclass.SelectedIndex][0];
            nm_actcritprc.Value = ChromaticsSettings.ChromaticsSettingsACTTargetCrit[cb_actjobclass.SelectedIndex][0];
            nm_actdhprc.Value = ChromaticsSettings.ChromaticsSettingsACTTargetDH[cb_actjobclass.SelectedIndex][0];
            nm_actcritdhprc.Value = ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[cb_actjobclass.SelectedIndex][0];
            nm_actoverhealprc.Value = ChromaticsSettings.ChromaticsSettingsACTOverheal[cb_actjobclass.SelectedIndex][0];
            nm_actdmgprc.Value = ChromaticsSettings.ChromaticsSettingsACTDamage[cb_actjobclass.SelectedIndex][0];
        }

        public string GetACTJob()
        {
            var ret = "";
            if (InvokeRequired)
            {
                void Del()
                {
                    ret = (string)cb_actjobclass.SelectedItem;
                }

                Invoke((GetJobDelegate)Del);
            }
            else
            {
                ret = (string)cb_actjobclass.SelectedItem;
            }

            return ret;
        }
        
        public void SwitchACTJob(string job)
        {
            if (!_actJobs.ContainsValue(job)) return;

            if (InvokeRequired)
            {
                void Del()
                {
                    //cb_actjobclass.
                    cb_actjobclass.Enabled = true;
                    cb_actjobclass.SelectedIndex = _actJobs.FirstOrDefault(x => x.Value == job).Key;
                    cb_actjobclass.Enabled = false;
                }

                Invoke((ChangeJobDelegate)Del);
            }
            else
            {
                cb_actjobclass.Enabled = true;
                cb_actjobclass.SelectedIndex = _actJobs.FirstOrDefault(x => x.Value == job).Key;
                cb_actjobclass.Enabled = false;
            }
        }

        private void nm_acttargetdps_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTDPS[cb_actjobclass.SelectedIndex][0] = Convert.ToInt32(nm_acttargetdps.Value);

            SaveChromaticsSettings(1);
        }

        private void nm_acttargethps_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTHPS[cb_actjobclass.SelectedIndex][0] = Convert.ToInt32(nm_acttargethps.Value);

            SaveChromaticsSettings(1);
        }

        private void nm_actgroupdps_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTGroupDPS[cb_actjobclass.SelectedIndex][0] = Convert.ToInt32(nm_actgroupdps.Value);

            SaveChromaticsSettings(1);
        }

        private void nm_actcritprc_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTTargetCrit[cb_actjobclass.SelectedIndex][0] = Convert.ToInt32(nm_actcritprc.Value);

            SaveChromaticsSettings(1);
        }

        private void nm_actdhprc_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTTargetDH[cb_actjobclass.SelectedIndex][0] = Convert.ToInt32(nm_actdhprc.Value);

            SaveChromaticsSettings(1);
        }

        private void nm_actcritdhprc_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[cb_actjobclass.SelectedIndex][0] = Convert.ToInt32(nm_actcritdhprc.Value);

            SaveChromaticsSettings(1);
        }

        private void nm_actoverhealprc_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTOverheal[cb_actjobclass.SelectedIndex][0] = Convert.ToInt32(nm_actoverhealprc.Value);

            SaveChromaticsSettings(1);
        }

        private void nm_actdmgprc_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsACTDamage[cb_actjobclass.SelectedIndex][0] = Convert.ToInt32(nm_actdmgprc.Value);

            SaveChromaticsSettings(1);
        }

        private void Chk_enablebulbextraeffect_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsExtraBulbEffects = chk_enablebulbextraeffect.Checked;

            SaveChromaticsSettings(1);
        }

        private void Chk_releasedevices_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsReleaseDevices = chk_releasedevices.Checked;

            SaveChromaticsSettings(1);
        }

        private void cb_singlezonemode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string) cb_singlezonemode.SelectedItem != item.Value) continue;
                _KeysSingleKeyMode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetKeysbase = false;
            SaveDevices();
        }

        private void cb_mouse_z1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_mouse_z1.SelectedItem != item.Value) continue;
                _MouseZone1Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetMousebase = false;
            SaveDevices();
        }

        private void cb_mouse_z2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_mouse_z2.SelectedItem != item.Value) continue;
                _MouseZone2Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetMousebase = false;
            SaveDevices();
        }

        private void cb_mouse_z3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_mouse_z3.SelectedItem != item.Value) continue;
                _MouseZone3Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetMousebase = false;
            SaveDevices();
        }

        private void cb_mouse_zs1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_mouse_zs1.SelectedItem != item.Value) continue;
                _MouseStrip1Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetMousebase = false;
            SaveDevices();
        }

        private void cb_mouse_zs2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_mouse_zs2.SelectedItem != item.Value) continue;
                _MouseStrip2Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetMousebase = false;
            SaveDevices();
        }

        private void cb_pad_zs1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_pad_zs1.SelectedItem != item.Value) continue;
                _PadZone1Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetPadbase = false;
            SaveDevices();
        }

        private void cb_pad_zs2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_pad_zs2.SelectedItem != item.Value) continue;
                _PadZone2Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetPadbase = false;
            SaveDevices();
        }

        private void cb_pad_zs3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_pad_zs3.SelectedItem != item.Value) continue;
                _PadZone3Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetPadbase = false;
            SaveDevices();
        }

        private void cb_headset_z1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_headset_z1.SelectedItem != item.Value) continue;
                _HeadsetZone1Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetHeadsetbase = false;
            SaveDevices();
        }

        private void cb_headset_z2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_headset_z2.SelectedItem != item.Value) continue;
                _HeadsetZone2Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetHeadsetbase = false;
            SaveDevices();
        }

        private void cb_keypad_z1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesMulti)
            {
                if ((string)cb_keypad_z1.SelectedItem != item.Value) continue;
                _KeypadZone1Mode = Helpers.ConvertCBToDevMultiMode(item.Value);
                break;
            }

            SetKeypadbase = false;
            SaveDevices();
        }

        private void chk_dev_chromalink_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_dev_chromalink.Checked)
            {
                
                _deviceCL = true;
                SetCLbase = false;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceChromaLink = true;

                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceOther = true;

                    _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                        _corsairDeviceMousepad, _corsairDeviceHeadset, _corsairDeviceOther,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                }

                if (AsusSdkCalled == 1)
                {
                    _asusDeviceOther = true;

                    _asus.ResetAsusDevices(_asusDeviceKeyboard, _asusDeviceMouse,
                         _asusDeviceHeadset, _asusDeviceOther,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                }
            }
            else
            {
                
                _deviceCL = false;

                if (RazerSdkCalled == 1)
                {
                    _razerDeviceChromaLink = false;

                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset, _razerDeviceChromaLink,
                        Color.Black);
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairDeviceOther = false;

                    _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                        _corsairDeviceMousepad, _corsairDeviceHeadset, _corsairDeviceOther,
                        Color.Black);
                }

                if (AsusSdkCalled == 1)
                {
                    _asusDeviceOther = false;

                    _asus.ResetAsusDevices(_asusDeviceKeyboard, _asusDeviceMouse,
                        _asusDeviceHeadset, _asusDeviceOther,
                        Color.Black);
                }
            }

            SaveDevices();
        }

        private void cb_chromalink_z1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_chromalink_z1.SelectedItem != item.Value) continue;
                _CLZone1Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetCLbase = false;
            SaveDevices();
        }

        private void cb_chromalink_z2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_chromalink_z2.SelectedItem != item.Value) continue;
                _CLZone2Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetCLbase = false;
            SaveDevices();
        }

        private void cb_chromalink_z3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_chromalink_z3.SelectedItem != item.Value) continue;
                _CLZone3Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetCLbase = false;
            SaveDevices();
        }

        private void cb_chromalink_z4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_chromalink_z4.SelectedItem != item.Value) continue;
                _CLZone4Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetCLbase = false;
            SaveDevices();
        }

        private void cb_chromalink_z5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_chromalink_z5.SelectedItem != item.Value) continue;
                _CLZone5Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetCLbase = false;
            SaveDevices();
        }

        private void cb_chromalink_z6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            foreach (var item in _devModesA)
            {
                if ((string)cb_chromalink_z6.SelectedItem != item.Value) continue;
                _CLZone6Mode = Helpers.ConvertCBToDevMode(item.Value);
                break;
            }

            SetCLbase = false;
            SaveDevices();
        }

        private void nm_criticalhp_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (nm_criticalhp.Value < 0) nm_criticalhp.Value = 0;
            if (nm_criticalhp.Value > 100) nm_criticalhp.Value = 100;

            ChromaticsSettings.ChromaticsSettingsCriticalHP = (int)nm_criticalhp.Value;
            SaveChromaticsSettings(1);
        }

        private void btn_help_Click(object sender, EventArgs e)
        {
            if (Startup == false) return;

            Process.Start("https://discord.gg/sK47yFE");
        }

        private void btn_doc_Click(object sender, EventArgs e)
        {
            if (Startup == false) return;

            Process.Start("https://docs.chromaticsffxiv.com/chromatics");
        }

        private void btn_web_Click(object sender, EventArgs e)
        {
            if (Startup == false) return;

            Process.Start("https://chromaticsffxiv.com/");
        }

        private void Btn_dumplog_Click(object sender, EventArgs e)
        {
            if (Startup == false) return;

            var log = rtb_debug.Text.Replace("\n", Environment.NewLine);
            ExportDebugLog(log);
        }

        private void btn_acthelp_Click(object sender, EventArgs e)
        {
            if (Startup == false) return;

            Process.Start("https://docs.chromaticsffxiv.com/chromatics/integrations/advanced-combat-tracker");
        }

        private void chk_desktopnotify_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsDesktopNotifications = chk_desktopnotify.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_cutscenes_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsCutsceneAnimation = chk_cutscenes.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_vegasmode_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsVegasMode = chk_vegasmode.Checked;
            SetKeysbase = false;

            SaveChromaticsSettings(1);
        }

        private void chk_debugopt_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsDebugOpt = chk_desktopnotify.Checked;
            SaveChromaticsSettings(1);
        }

        private void Chk_keymulti_rev_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _KeyMultiReverse = chk_keymulti_rev.Checked;

            SaveDevices();
        }

        private void chk_lcdtoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (LogitechSdkCalled == 1)
            {
                if (!chk_lcdtoggle.Checked)
                {
                    if (LcdSdkCalled == 1)
                    {
                        _lcd.ShutdownLcd();
                        _lcd = null;
                        LcdSdkCalled = 0;
                    }
                }
                else
                {
                    _lcd = LogitechLcdInterface.InitializeLcdSdk();

                    if (_lcd != null)
                    {
                        LcdSdk = true;
                        LcdSdkCalled = 1;
                        WriteConsole(ConsoleTypes.Logitech, @"LCD SDK Enabled");
                    }
                }
            }

            ChromaticsSettings.ChromaticsSettingsLcdEnabled = chk_lcdtoggle.Checked;
            SaveChromaticsSettings(1);
        }


        private void chk_showstatuseffects_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsStatusEffectToggle = chk_showstatuseffects.Checked;
            SetKeysbase = false;
            SaveChromaticsSettings(1);
        }
        
        private void btn_ffxivcachereset_Click(object sender, EventArgs e)
        {
            var cacheReset =
                MessageBox.Show(
                    @"Are you sure you wish to clear Chromatics cache?",
                    @"Clear Cache?", MessageBoxButtons.OKCancel);
            if (cacheReset != DialogResult.OK) return;

            try
            {
                string enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

                if (File.Exists(enviroment + @"/signatures-x64.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/signatures-x64.json");
                }

                if (File.Exists(enviroment + @"/structures-x64.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/structures-x64.json");
                }

                if (File.Exists(enviroment + @"/actions.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/actions.json");
                }

                if (File.Exists(enviroment + @"/statuses.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/statuses.json");
                }

                if (File.Exists(enviroment + @"/zones.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/zones.json");
                }

                MessageBox.Show(@"Cache Cleared. Please restart Chromatics.", @"Cache Cleared", MessageBoxButtons.OK);
                Thread.Sleep(1);
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Unable to clear cache. Are you running as Administrator? Error: " + ex.StackTrace, @"Unable to clear Cache", MessageBoxButtons.OK);
            }
        }

        private async void chk_enablecast_CheckedChanged(object sender, EventArgs e)
        {
            //if (Startup == false) return;

            if (chk_enablecast.Checked)
            {
                
                cb_castdevlist.Items.Clear();
                cb_castdevlist.Enabled = true;
                chk_castdfbell.Enabled = true;
                lbl_chromecastdev.Enabled = true;
                chk_castsrank.Enabled = true;
                chk_castreadycheck.Checked = true;

                if (cb_castdevlist.Items.Count > 0)
                {
                    btn_casttest.Enabled = true;
                }

                await SharpcastController.InitSharpcastAsync();
                var casts = SharpcastController.ReturnActiveChromecasts();

                var mem = 0;
                var i = 0;

                foreach (var cast in casts)
                {
                    cb_castdevlist.Items.Add(cast.Value.FriendlyName);

                    if (cast.Value.Id == ChromaticsSettings.ChromaticsSettingsCastDevice)
                    {
                        mem = i;
                    }

                    i++;
                }
                
                if (cb_castdevlist.Items.Count > 0)
                {
                    cb_castdevlist.SelectedIndex = mem;
                }
            }
            else
            {
                cb_castdevlist.Enabled = false;
                chk_castdfbell.Enabled = false;
                btn_casttest.Enabled = false;
                lbl_chromecastdev.Enabled = false;
                chk_castsrank.Enabled = false;
                chk_castreadycheck.Checked = false;

                SharpcastController.EndSharpcaster();
            }

            ChromaticsSettings.ChromaticsSettingsCastEnabled = chk_enablecast.Checked;
            SaveChromaticsSettings(1);
        }

        private void cb_castdevlist_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            var casts = SharpcastController.ReturnActiveChromecasts();
            var lookup = casts.FirstOrDefault(x => x.Value.FriendlyName == (string)cb_castdevlist.SelectedItem).Key;

            if (casts.ContainsKey(lookup))
            {
                ChromaticsSettings.ChromaticsSettingsCastDevice = lookup;
                Console.WriteLine(@"Setting default cast device to " + casts[lookup].FriendlyName);
                SharpcastController.SetActiveDevice(lookup);
                btn_casttest.Enabled = true;

                SaveChromaticsSettings(1);
            }
        }

        private void Cb_bcm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsBaseMode = (string)cb_bcm.SelectedItem;

            SetKeysbase = false;
            SaveChromaticsSettings(1);
        }
        
        private void Chk_castsrank_CheckedChanged(object sender, EventArgs e)
        {
           if (Startup == false) return;
           ChromaticsSettings.ChromaticsSettingsCastSRankAlert = chk_castsrank.Checked;

            SaveChromaticsSettings(1);
        }
        
        private void chk_castdfbell_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsCastDFBell = chk_castdfbell.Checked;
            SaveChromaticsSettings(1);
        }

        private void Chk_enabletimebell_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_enabletimebell.Checked)
            {
                cb_alarmclock.Enabled = true;
            }
            else
            {
                cb_alarmclock.Enabled = false;
            }

            ChromaticsSettings.ChromaticsSettingsCastAlarmBell = chk_enabletimebell.Checked;
            SaveChromaticsSettings(1);
        }

        private void Chk_castreadycheck_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_castreadycheck.Checked)
            {
                chk_castreadycheck.Enabled = true;
            }
            else
            {
                chk_castreadycheck.Enabled = false;
            }

            ChromaticsSettings.ChromaticsSettingsCastReadyCheckAlert = chk_castreadycheck.Checked;
            SaveChromaticsSettings(1);
        }

        private void Cb_alarmclock_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsCastAlarmTime = this.cb_alarmclock.GetItemText(this.cb_alarmclock.SelectedItem);
            SaveChromaticsSettings(1);
        }

        private void Btn_casttest_Click(object sender, EventArgs e)
        {
            if (Startup == false) return;
            SharpcastController.CastMedia("dfpop_notify.mp3");
        }

        private void chk_enableifttt_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_enableifttt.Checked)
            {
                lbl_IFTTTcode.Enabled = true;
                txt_iftttmakerurl.Enabled = true;
                lbl_iftttmakerurlexample.Enabled = true;
                btn_ifttthelp.Enabled = true;
                dgv_iftttgrid.Enabled = true;
            }
            else
            {
                lbl_IFTTTcode.Enabled = false;
                txt_iftttmakerurl.Enabled = false;
                lbl_iftttmakerurlexample.Enabled = false;
                btn_ifttthelp.Enabled = false;
                dgv_iftttgrid.Enabled = false;
            }

            ChromaticsSettings.ChromaticsSettingsIFTTTEnable = chk_enableifttt.Checked;
            SaveChromaticsSettings(1);
        }

        private void txt_iftttmakerurl_TextChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            var result = Uri.TryCreate(txt_iftttmakerurl.Text, UriKind.Absolute, out var uriResult) && uriResult.Scheme == Uri.UriSchemeHttps;

            if (!result)
            {
                MessageBox.Show(@"The IFTTT Maker URL you provided is invalid, please check the link and try again.", @"Invalid IFTTT Maker URL", MessageBoxButtons.OK);
                txt_iftttmakerurl.Text = "";
                return;
            }

            ChromaticsSettings.ChromaticsSettingsIFTTTURL = txt_iftttmakerurl.Text;
            SaveChromaticsSettings(1);
        }

        private void btn_ifttthelp_Click(object sender, EventArgs e)
        {
            if (Startup == false) return;

            Process.Start("https://docs.chromaticsffxiv.com/chromatics/integrations/ifttt");
        }

        private void Chk_sdk_razer_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKRazer = chk_sdk_razer.Checked;
            SaveDevices();
        }

        private void Chk_sdk_logi_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKLogitech = chk_sdk_logi.Checked;
            SaveDevices();
        }

        private void Chk_sdk_corsair_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKCorsair = chk_sdk_corsair.Checked;
            SaveDevices();
        }

        private void Chk_sdk_cooler_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKCooler = chk_sdk_cooler.Checked;
            SaveDevices();
        }

        private void Chk_sdk_steel_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKSteelSeries = chk_sdk_steel.Checked;
            SaveDevices();
        }

        private void Chk_sdk_wooting_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKWooting = chk_sdk_wooting.Checked;
            SaveDevices();
        }

        private void Chk_sdk_asus_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKAsus = chk_sdk_asus.Checked;
            SaveDevices();
        }

        private void Chk_sdk_mystic_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKMystic = chk_sdk_mystic.Checked;
            SaveDevices();
        }

        private void Chk_sdk_lifx_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _SDKLifx = chk_sdk_lifx.Checked;
            SaveDevices();
        }

        private void Nm_keymulti_led_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _KeyMultiLEDCount = Convert.ToInt32(nm_keymulti_led.Value);

            SaveDevices();
        }

        private void Nm_ledcount_z1_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _ChromaLinkLEDCountZ1 = Convert.ToInt32(nm_ledcount_z1.Value);

            SaveDevices();
        }

        private void Nm_ledcount_z2_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _ChromaLinkLEDCountZ2 = Convert.ToInt32(nm_ledcount_z2.Value);

            SaveDevices();
        }

        private void Nm_ledcount_z3_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _ChromaLinkLEDCountZ3 = Convert.ToInt32(nm_ledcount_z3.Value);

            SaveDevices();
        }

        private void Nm_ledcount_z4_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _ChromaLinkLEDCountZ4 = Convert.ToInt32(nm_ledcount_z4.Value);

            SaveDevices();
        }

        private void Nm_ledcount_z5_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _ChromaLinkLEDCountZ5 = Convert.ToInt32(nm_ledcount_z5.Value);

            SaveDevices();
        }

        private void Nm_ledcount_z6_ValueChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _ChromaLinkLEDCountZ6 = (int)nm_ledcount_z6.Value;

            SaveDevices();
        }

        private void Chk_other_interp_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _OtherInterpolateEffects = chk_other_interp.Checked;

            SaveDevices();
        }

        private void Chk_other_interpreverse_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _ReverseInterpolateEffects = chk_other_interpreverse.Checked;

            SaveDevices();
        }

        private void Chk_keypad_binds_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            _EnableKeypadBinds = chk_keypad_binds.Checked;

            if (_EnableKeypadBinds)
            {
                dgv_keypad_binds.Enabled = true;
            }
            else
            {
                dgv_keypad_binds.Enabled = false;
            }

            SaveDevices();
        }

        private void OnKeypadBindChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (Startup == false || !_EnableKeypadBinds) return;

            _KeyBindMap[1] = (string)dgv_keypad_binds.Rows[0].Cells[4].Value;
            _KeyBindMap[2] = (string)dgv_keypad_binds.Rows[1].Cells[4].Value;
            _KeyBindMap[3] = (string)dgv_keypad_binds.Rows[2].Cells[4].Value;
            _KeyBindMap[4] = (string)dgv_keypad_binds.Rows[3].Cells[4].Value;
            _KeyBindMap[5] = (string)dgv_keypad_binds.Rows[4].Cells[4].Value;
            _KeyBindMap[6] = (string)dgv_keypad_binds.Rows[5].Cells[4].Value;
            _KeyBindMap[7] = (string)dgv_keypad_binds.Rows[6].Cells[4].Value;
            _KeyBindMap[8] = (string)dgv_keypad_binds.Rows[7].Cells[4].Value;
            _KeyBindMap[9] = (string)dgv_keypad_binds.Rows[8].Cells[4].Value;
            _KeyBindMap[10] = (string)dgv_keypad_binds.Rows[9].Cells[4].Value;
            _KeyBindMap[11] = (string)dgv_keypad_binds.Rows[10].Cells[4].Value;
            _KeyBindMap[12] = (string)dgv_keypad_binds.Rows[11].Cells[4].Value;
            _KeyBindMap[13] = (string)dgv_keypad_binds.Rows[12].Cells[4].Value;
            _KeyBindMap[14] = (string)dgv_keypad_binds.Rows[13].Cells[4].Value;
            _KeyBindMap[15] = (string)dgv_keypad_binds.Rows[14].Cells[4].Value;
            _KeyBindMap[16] = (string)dgv_keypad_binds.Rows[15].Cells[4].Value;
            _KeyBindMap[17] = (string)dgv_keypad_binds.Rows[16].Cells[4].Value;
            _KeyBindMap[18] = (string)dgv_keypad_binds.Rows[17].Cells[4].Value;
            _KeyBindMap[19] = (string)dgv_keypad_binds.Rows[18].Cells[4].Value;
            _KeyBindMap[20] = (string)dgv_keypad_binds.Rows[19].Cells[4].Value;

            _KZ1Enabled = (bool)dgv_keypad_binds.Rows[0].Cells[2].Value;
            _KZ2Enabled = (bool)dgv_keypad_binds.Rows[1].Cells[2].Value;
            _KZ3Enabled = (bool)dgv_keypad_binds.Rows[2].Cells[2].Value;
            _KZ4Enabled = (bool)dgv_keypad_binds.Rows[3].Cells[2].Value;
            _KZ5Enabled = (bool)dgv_keypad_binds.Rows[4].Cells[2].Value;
            _KZ6Enabled = (bool)dgv_keypad_binds.Rows[5].Cells[2].Value;
            _KZ7Enabled = (bool)dgv_keypad_binds.Rows[6].Cells[2].Value;
            _KZ8Enabled = (bool)dgv_keypad_binds.Rows[7].Cells[2].Value;
            _KZ9Enabled = (bool)dgv_keypad_binds.Rows[8].Cells[2].Value;
            _KZ10Enabled = (bool)dgv_keypad_binds.Rows[9].Cells[2].Value;
            _KZ11Enabled = (bool)dgv_keypad_binds.Rows[10].Cells[2].Value;
            _KZ12Enabled = (bool)dgv_keypad_binds.Rows[11].Cells[2].Value;
            _KZ13Enabled = (bool)dgv_keypad_binds.Rows[12].Cells[2].Value;
            _KZ14Enabled = (bool)dgv_keypad_binds.Rows[13].Cells[2].Value;
            _KZ15Enabled = (bool)dgv_keypad_binds.Rows[14].Cells[2].Value;
            _KZ16Enabled = (bool)dgv_keypad_binds.Rows[15].Cells[2].Value;
            _KZ17Enabled = (bool)dgv_keypad_binds.Rows[16].Cells[2].Value;
            _KZ18Enabled = (bool)dgv_keypad_binds.Rows[17].Cells[2].Value;
            _KZ19Enabled = (bool)dgv_keypad_binds.Rows[18].Cells[2].Value;
            _KZ20Enabled = (bool)dgv_keypad_binds.Rows[19].Cells[2].Value;

            _KeyBindModMap[1] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[0].Cells[3].Value);
            _KeyBindModMap[2] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[1].Cells[3].Value);
            _KeyBindModMap[3] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[2].Cells[3].Value);
            _KeyBindModMap[4] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[3].Cells[3].Value);
            _KeyBindModMap[5] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[4].Cells[3].Value);
            _KeyBindModMap[6] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[5].Cells[3].Value);
            _KeyBindModMap[7] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[6].Cells[3].Value);
            _KeyBindModMap[8] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[7].Cells[3].Value);
            _KeyBindModMap[9] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[8].Cells[3].Value);
            _KeyBindModMap[10] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[9].Cells[3].Value);
            _KeyBindModMap[11] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[10].Cells[3].Value);
            _KeyBindModMap[12] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[11].Cells[3].Value);
            _KeyBindModMap[13] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[12].Cells[3].Value);
            _KeyBindModMap[14] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[13].Cells[3].Value);
            _KeyBindModMap[15] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[14].Cells[3].Value);
            _KeyBindModMap[16] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[15].Cells[3].Value);
            _KeyBindModMap[17] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[16].Cells[3].Value);
            _KeyBindModMap[18] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[17].Cells[3].Value);
            _KeyBindModMap[19] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[18].Cells[3].Value);
            _KeyBindModMap[20] = Helpers.ConvertCBToModifiers((string)dgv_keypad_binds.Rows[19].Cells[3].Value);

            _KeyBindMap[1] = _KeyBindMap[1].ToUpper();
            _KeyBindMap[2] = _KeyBindMap[2].ToUpper();
            _KeyBindMap[3] = _KeyBindMap[3].ToUpper();
            _KeyBindMap[4] = _KeyBindMap[4].ToUpper();
            _KeyBindMap[5] = _KeyBindMap[5].ToUpper();
            _KeyBindMap[6] = _KeyBindMap[6].ToUpper();
            _KeyBindMap[7] = _KeyBindMap[7].ToUpper();
            _KeyBindMap[8] = _KeyBindMap[8].ToUpper();
            _KeyBindMap[9] = _KeyBindMap[9].ToUpper();
            _KeyBindMap[10] = _KeyBindMap[10].ToUpper();
            _KeyBindMap[11] = _KeyBindMap[11].ToUpper();
            _KeyBindMap[12] = _KeyBindMap[12].ToUpper();
            _KeyBindMap[13] = _KeyBindMap[13].ToUpper();
            _KeyBindMap[14] = _KeyBindMap[14].ToUpper();
            _KeyBindMap[15] = _KeyBindMap[15].ToUpper();
            _KeyBindMap[16] = _KeyBindMap[16].ToUpper();
            _KeyBindMap[17] = _KeyBindMap[17].ToUpper();
            _KeyBindMap[18] = _KeyBindMap[18].ToUpper();
            _KeyBindMap[19] = _KeyBindMap[19].ToUpper();
            _KeyBindMap[20] = _KeyBindMap[20].ToUpper();

            dgv_keypad_binds.Rows[0].Cells[4].Value = _KeyBindMap[1];
            dgv_keypad_binds.Rows[1].Cells[4].Value = _KeyBindMap[2];
            dgv_keypad_binds.Rows[2].Cells[4].Value = _KeyBindMap[3];
            dgv_keypad_binds.Rows[3].Cells[4].Value = _KeyBindMap[4];
            dgv_keypad_binds.Rows[4].Cells[4].Value = _KeyBindMap[5];
            dgv_keypad_binds.Rows[5].Cells[4].Value = _KeyBindMap[6];
            dgv_keypad_binds.Rows[6].Cells[4].Value = _KeyBindMap[7];
            dgv_keypad_binds.Rows[7].Cells[4].Value = _KeyBindMap[8];
            dgv_keypad_binds.Rows[8].Cells[4].Value = _KeyBindMap[9];
            dgv_keypad_binds.Rows[9].Cells[4].Value = _KeyBindMap[10];
            dgv_keypad_binds.Rows[10].Cells[4].Value = _KeyBindMap[11];
            dgv_keypad_binds.Rows[11].Cells[4].Value = _KeyBindMap[12];
            dgv_keypad_binds.Rows[12].Cells[4].Value = _KeyBindMap[13];
            dgv_keypad_binds.Rows[13].Cells[4].Value = _KeyBindMap[14];
            dgv_keypad_binds.Rows[14].Cells[4].Value = _KeyBindMap[15];
            dgv_keypad_binds.Rows[15].Cells[4].Value = _KeyBindMap[16];
            dgv_keypad_binds.Rows[16].Cells[4].Value = _KeyBindMap[17];
            dgv_keypad_binds.Rows[17].Cells[4].Value = _KeyBindMap[18];
            dgv_keypad_binds.Rows[18].Cells[4].Value = _KeyBindMap[19];
            dgv_keypad_binds.Rows[19].Cells[4].Value = _KeyBindMap[20];
            
            SaveDevices();
        }

        private void Chk_quickclose_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsQuickClose = chk_quickclose.Checked;
            
            SaveChromaticsSettings(1);
        }

        private void cb_lang_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsLanguage = cb_lang.SelectedIndex;
            SetKeysbase = false;
            SaveChromaticsSettings(1);

            /*
            if (IsAdministrator())
            {
                var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

                if (File.Exists(enviroment + @"/signatures-x64.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/signatures-x64.json");
                }

                if (File.Exists(enviroment + @"/structures-x64.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/structures-x64.json");
                }

                if (File.Exists(enviroment + @"/actions.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/actions.json");
                }

                if (File.Exists(enviroment + @"/statuses.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/statuses.json");
                }

                if (File.Exists(enviroment + @"/zones.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/zones.json");
                }
            }

            WriteConsole(ConsoleTypes.Ffxiv, @"Language change detected. Clearing Cache..");
            */
        }

        private void SetupAboutText()
        {
            var tb = rtb_about;

            tb.SelectionFont = new Font(tb.Font, FontStyle.Bold);
            tb.AppendText(@"Developed by:" + Environment.NewLine);
            tb.SelectionFont = new Font(tb.Font, FontStyle.Regular);
            tb.AppendText(@"Roxas Keyheart" + Environment.NewLine);
            tb.AppendText(Environment.NewLine);
            tb.SelectionFont = new Font(tb.Font, FontStyle.Bold);
            tb.AppendText(@"Contributors:" + Environment.NewLine);
            tb.SelectionFont = new Font(tb.Font, FontStyle.Regular);
            tb.AppendText(@"Sharlayan Dev Team" + Environment.NewLine);
            tb.AppendText(@"Colore Dev Team" + Environment.NewLine);
            tb.AppendText(@"CUE.NET Dev Team" + Environment.NewLine);
            tb.AppendText(@"Daniel Rouleau (Zealotus)" + Environment.NewLine);
            tb.AppendText(@"Elestriel" + Environment.NewLine);
            tb.AppendText(@"Sidewinder94" + Environment.NewLine);
            tb.AppendText(@"C.J. Manca" + Environment.NewLine);
            tb.AppendText(@"Liam Dawson" + Environment.NewLine);
            tb.AppendText(@"jesterfraud" + Environment.NewLine);
            tb.AppendText(@"Paladinleeds" + Environment.NewLine);
            tb.AppendText(@"Ayiana Arch" + Environment.NewLine);
            tb.AppendText(@"GeT_ReKt" + Environment.NewLine);
            tb.AppendText(@"Timewarrener" + Environment.NewLine);
            tb.AppendText(Environment.NewLine);
            tb.SelectionFont = new Font(tb.Font, FontStyle.Bold);
            tb.AppendText(@"Big thanks to Chromatics' Donators/Supporters:" + Environment.NewLine);
            tb.SelectionFont = new Font(tb.Font, FontStyle.Regular);
            tb.AppendText(@"RAZER™" + Environment.NewLine);
            tb.AppendText(@"AliceD" + Environment.NewLine);
            tb.AppendText(@"TeoDaTank" + Environment.NewLine);
            tb.AppendText(@"dwighte" + Environment.NewLine);
            tb.AppendText(@"Robyn" + Environment.NewLine);
            tb.AppendText(@"Naxterra" + Environment.NewLine);
            tb.AppendText(Environment.NewLine);
            tb.SelectionFont = new Font(tb.Font, FontStyle.Bold);
            tb.AppendText(@"Disclaimer" + Environment.NewLine);
            tb.SelectionFont = new Font(tb.Font, FontStyle.Regular);
            tb.AppendText(@"Chromatics is not in anyway affiliated with Square Enix or FINAL FANTASY. All rights to their respected owners." + Environment.NewLine);
            tb.AppendText(Environment.NewLine);
            tb.AppendText(@"© 2010-2019 SQUARE ENIX CO., LTD. All Rights Reserved. A REALM REBORN, HEAVENSWARD, STORMBLOOD, SHADOWBRINGERS is a registered trademark or trademark of Square Enix Co., Ltd. FINAL FANTASY, SQUARE ENIX and the SQUARE ENIX logo are registered trademarks or trademarks of Square Enix Holdings Co., Ltd." + Environment.NewLine);
        }

        private delegate void ResetGridDelegate();
        private delegate void SetFormNameDelegate();
        private delegate void ResetMappingsDelegate();
        private delegate void ChangeJobDelegate();
        private delegate void GetJobDelegate();
    }
}