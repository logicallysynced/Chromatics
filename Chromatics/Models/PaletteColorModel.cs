using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Chromatics.Enums.Palette;

namespace Chromatics.Models
{
    public class PaletteColorModel
    {
        //Chromatics
        public ColorMapping BaseColor = new("Base Color", PaletteTypes.Chromatics, Color.DodgerBlue);
        public ColorMapping HighlightColor = new("Highlight Color", PaletteTypes.Chromatics, Color.Magenta);
        public ColorMapping DeviceDisabled = new("Device Disabled Color", PaletteTypes.Chromatics, Color.Black);
        public ColorMapping MenuBase = new("Main Menu Base Color", PaletteTypes.Chromatics, Color.OrangeRed);
        public ColorMapping MenuHighlight1 = new("Main Menu Animation Color 1", PaletteTypes.Chromatics, Color.Red);
        public ColorMapping MenuHighlight2 = new("Main Menu Animation Color 2", PaletteTypes.Chromatics, Color.Red);
        public ColorMapping MenuHighlight3 = new("Main Menu Animation Color 3", PaletteTypes.Chromatics, Color.Red);
        public ColorMapping CutsceneBase = new("Cutscene Base Color", PaletteTypes.Chromatics, Color.DeepSkyBlue);
        public ColorMapping CutsceneHighlight1 = new("Cutscene Animation Color 1", PaletteTypes.Chromatics, Color.White);
        public ColorMapping CutsceneHighlight2 = new("Cutscene Animation Color 2", PaletteTypes.Chromatics, Color.DeepSkyBlue);
        public ColorMapping CutsceneHighlight3 = new("Cutscene Animation Color 3", PaletteTypes.Chromatics, Color.White);

        //Player Stats
        public ColorMapping HpCritical = new("HP Critical", PaletteTypes.PlayerStats, Color.Red);
        public ColorMapping HpEmpty = new("HP Empty", PaletteTypes.PlayerStats, Color.Black);
        public ColorMapping HpFull = new("HP Full", PaletteTypes.PlayerStats, Color.Lime);
        public ColorMapping HpLoss = new("HP Loss", PaletteTypes.PlayerStats, Color.Red);
        public ColorMapping MpEmpty = new("MP Empty", PaletteTypes.PlayerStats, Color.Black);
        public ColorMapping MpFull = new("MP Full", PaletteTypes.PlayerStats, Color.Magenta);
        public ColorMapping CpEmpty = new("CP Empty", PaletteTypes.PlayerStats, Color.Black);
        public ColorMapping CpFull = new("CP Full", PaletteTypes.PlayerStats, Color.Purple);
        public ColorMapping GpEmpty = new("GP Empty", PaletteTypes.PlayerStats, Color.Black);
        public ColorMapping GpFull = new("GP Full", PaletteTypes.PlayerStats, Color.SkyBlue);
        public ColorMapping ExpEmpty = new("Experience Bar (Empty)", PaletteTypes.PlayerStats, Color.Black);
        public ColorMapping ExpFull = new("Experience Bar (Full)", PaletteTypes.PlayerStats, Color.Yellow);
        public ColorMapping ExpMax = new("Experience Bar (Level Cap)", PaletteTypes.PlayerStats, Color.Orange);
        public ColorMapping CastChargeEmpty = new("Cast Bar Charge Empty", PaletteTypes.PlayerStats, Color.Black);
        public ColorMapping CastChargeFull = new("Cast Bar Charge Build", PaletteTypes.PlayerStats, Color.White);
        public ColorMapping BattleEngaged = new("Battle Stance Engaged", PaletteTypes.PlayerStats, Color.Red);
        public ColorMapping BattleNotEngaged = new("Battle Stance Not Engaged", PaletteTypes.PlayerStats, Color.Black);

        //Enmity/Aggro
        public ColorMapping Emnity0 = new("Minimal Enmity", PaletteTypes.EnmityAggro, Color.Green);
        public ColorMapping Emnity1 = new("Low Enmity", PaletteTypes.EnmityAggro, Color.Yellow);
        public ColorMapping Emnity2 = new("Medium Enmity", PaletteTypes.EnmityAggro, Color.Gold);
        public ColorMapping Emnity3 = new("High Enmity", PaletteTypes.EnmityAggro, Color.Orange);
        public ColorMapping Emnity4 = new("Top Enmity", PaletteTypes.EnmityAggro, Color.Red);
        public ColorMapping NoEmnity = new("No Enmity", PaletteTypes.EnmityAggro, Color.Black);

        //Target/Enemy
        public ColorMapping TargetCasting = new("Target Casting", PaletteTypes.TargetEnemy, Color.White);
        public ColorMapping TargetHpFriendly = new("Target HP (Friendly)", PaletteTypes.TargetEnemy, Color.Lime);
        public ColorMapping TargetHpClaimed = new("Target HP (Claimed)", PaletteTypes.TargetEnemy, Color.Red);
        public ColorMapping TargetHpEmpty = new("Target HP (Empty)", PaletteTypes.TargetEnemy, Color.Black);
        public ColorMapping TargetHpIdle = new("Target HP (Idle)", PaletteTypes.TargetEnemy, Color.Yellow);

        //Status Effects
        public ColorMapping Amnesia = new("Amnesia", PaletteTypes.StatusEffects, Color.Snow);
        public ColorMapping Bind = new("Bind", PaletteTypes.StatusEffects, Color.BlueViolet);
        public ColorMapping VulnerabilityUp = new("Vulnerability Up", PaletteTypes.StatusEffects, Color.Orange);
        public ColorMapping Bleed = new("Bleed", PaletteTypes.StatusEffects, Color.IndianRed);
        public ColorMapping Burns = new("Burns", PaletteTypes.StatusEffects, Color.OrangeRed);
        public ColorMapping DamageDown = new("Damage Down", PaletteTypes.StatusEffects, Color.PaleVioletRed);
        public ColorMapping Daze = new("Daze", PaletteTypes.StatusEffects, Color.PaleVioletRed);
        public ColorMapping Old = new("Old", PaletteTypes.StatusEffects, Color.SlateGray);
        public ColorMapping DeepFreeze = new("Deep Freeze", PaletteTypes.StatusEffects, Color.RoyalBlue);
        public ColorMapping Dropsy = new("Dropsy", PaletteTypes.StatusEffects, Color.DeepSkyBlue);
        public ColorMapping Incapacitation = new("Incapacitation", PaletteTypes.StatusEffects, Color.DarkRed);
        public ColorMapping Infirmary = new("Infirmary", PaletteTypes.StatusEffects, Color.PaleVioletRed);
        public ColorMapping Leaden = new("Leaden", PaletteTypes.StatusEffects, Color.DarkGray);
        public ColorMapping Misery = new("Misery", PaletteTypes.StatusEffects, Color.MidnightBlue);
        public ColorMapping Paralysis = new("Paralysis", PaletteTypes.StatusEffects, Color.PeachPuff);
        public ColorMapping Petrification = new("Petrification", PaletteTypes.StatusEffects, Color.SlateGray);
        public ColorMapping Poison = new("Poison", PaletteTypes.StatusEffects, Color.DarkGreen);
        public ColorMapping Pollen = new("Pollen", PaletteTypes.StatusEffects, Color.Goldenrod);
        public ColorMapping Pox = new("Pox", PaletteTypes.StatusEffects, Color.PaleVioletRed);
        public ColorMapping Silence = new("Silence", PaletteTypes.StatusEffects, Color.DarkBlue);
        public ColorMapping Sleep = new("Sleep", PaletteTypes.StatusEffects, Color.GhostWhite);
        public ColorMapping Slow = new("Slow", PaletteTypes.StatusEffects, Color.YellowGreen);
        public ColorMapping Stun = new("Stun", PaletteTypes.StatusEffects, Color.Snow);
        public ColorMapping Heavy = new("Heavy", PaletteTypes.StatusEffects, Color.DarkCyan);
        
        //Cooldowns/Keybinds
        public ColorMapping HotbarCd = new("Keybind Cooldown", PaletteTypes.CooldownsKeybinds, Color.Red);
        public ColorMapping HotbarNotAvailable = new("Keybind Not Available", PaletteTypes.CooldownsKeybinds, Color.Red);
        public ColorMapping HotbarOutRange = new("Keybind Out of Range", PaletteTypes.CooldownsKeybinds, Color.Red);
        public ColorMapping HotbarProc = new("Keybind Proc", PaletteTypes.CooldownsKeybinds, Color.Yellow);
        public ColorMapping HotbarReady = new("Keybind Ready", PaletteTypes.CooldownsKeybinds, Color.DodgerBlue);
        public ColorMapping GcdReady = new("Global Cooldown Ready", PaletteTypes.CooldownsKeybinds, Color.DodgerBlue);
        public ColorMapping GcdEmpty = new("Global Cooldown Empty", PaletteTypes.CooldownsKeybinds, Color.Black);
        public ColorMapping GcdHot = new("Global Cooldown Hot", PaletteTypes.CooldownsKeybinds, Color.Red);
        public ColorMapping KeybindDisabled = new("Keybind Disabled", PaletteTypes.CooldownsKeybinds, Color.Black);
        public ColorMapping KeybindMap = new("Map Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindAetherCurrents = new("Aether Currents Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindSigns = new("Signs Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindWaymarks = new("Waymarks Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindRecordReadyCheck = new("Record Ready Check Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindReadyCheck = new("Ready Check Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindCountdown = new("Countdown Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindEmotes = new("Emotes Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindCrossWorldLS = new("Crossworld Linkshell Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindLinkshells = new("Linkshell Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindContacts = new("Contacts Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindSprint = new("Sprint Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindTeleport = new("Teleport Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindReturn = new("Return Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindLimitBreak = new("Limit Break Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindDutyAction = new("Duty Action Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindRepair = new("Repair Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindDig = new("Dig Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        public ColorMapping KeybindInventory = new("Inventory Keybind", PaletteTypes.CooldownsKeybinds, Color.OrangeRed);
        
        //Job Gauges
        public ColorMapping JobWARNegative = new("WAR: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobWARBeastGauge = new("WAR: Beast Gauge", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobWARBeastGaugeMax = new("WAR: Beast Gauge Max", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobWARDefiance = new("WAR: Defiance", PaletteTypes.JobGauges, Color.MediumVioletRed);
        public ColorMapping JobWARNonDefiance = new("WAR: No Defiance", PaletteTypes.JobGauges, Color.Blue);
        public ColorMapping JobPLDNegative = new("PLD: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobPLDOathGauge = new("PLD: Oath Gauge", PaletteTypes.JobGauges, Color.Khaki);
        public ColorMapping JobPLDSwordOath = new("PLD: Sword Oath", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobPLDIronWill = new("PLD: Iron Will", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobMNKNegative = new("MNK: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobMNKGreased = new("MNK: Greased Lightning", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobDRGNegative = new("DRG: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobDRGBloodDragon = new("DRG: Blood of the Dragon", PaletteTypes.JobGauges, Color.Aqua);
        public ColorMapping JobDRGDragonGauge1 = new("DRG: Dragon Gauge 1", PaletteTypes.JobGauges, Color.BlueViolet);
        public ColorMapping JobDRGDragonGauge2 = new("DRG: Dragon Gauge 2", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobDRGLifeOfTheDragon = new("DRG: Life of the Dragon", PaletteTypes.JobGauges, Color.MediumVioletRed);
        public ColorMapping JobBRDNegative = new("BRD: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobBRDRepertoire = new("BRD: Repertoire Stack", PaletteTypes.JobGauges, Color.GhostWhite);
        public ColorMapping JobBRDBallad = new("BRD: Mage's Ballad", PaletteTypes.JobGauges, Color.MediumSlateBlue);
        public ColorMapping JobBRDArmys = new("BRD: Army's Paeon", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobBRDMinuet = new("BRD: The Wanderers' Minuet", PaletteTypes.JobGauges, Color.MediumSpringGreen);
        public ColorMapping JobWHMNegative = new("WHM: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobWHMFlowerPetal = new("WHM: Flower", PaletteTypes.JobGauges, Color.MediumVioletRed);
        public ColorMapping JobWHMFlowerCharge = new("WHM: Flower Charge", PaletteTypes.JobGauges, Color.Aqua);
        public ColorMapping JobWHMBloodLily = new("WHM: Blood Lily", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobWHMFreecure = new("WHM: Freecure Proc", PaletteTypes.JobGauges, Color.LightSeaGreen);
        public ColorMapping JobBLMNegative = new("BLM: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobBLMAstralFire = new("BLM: Astral Fire", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobBLMUmbralIce = new("BLM: Umbral Ice", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobBLMUmbralHeart = new("BLM: Umbral Heart", PaletteTypes.JobGauges, Color.DeepPink);
        public ColorMapping JobBLMEnochianCountdown = new("BLM: Enochian Countdown", PaletteTypes.JobGauges, Color.MediumPurple);
        public ColorMapping JobBLMEnochianCharge = new("BLM: Enochian Charge", PaletteTypes.JobGauges, Color.MediumPurple);
        public ColorMapping JobBLMPolyglot = new("BLM: Polyglot", PaletteTypes.JobGauges, Color.Magenta);
        public ColorMapping JobSMNNegative = new("SMN: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobSMNAetherflow = new("SMN: Aetherflow", PaletteTypes.JobGauges, Color.Orchid);
        public ColorMapping JobSCHNegative = new("SCH: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobSCHAetherflow = new("SCH: Aetherflow", PaletteTypes.JobGauges, Color.Orchid);
        public ColorMapping JobSCHFaerieGauge = new("SCH: Faerie Gauge", PaletteTypes.JobGauges, Color.MediumSpringGreen);
        public ColorMapping JobNINNegative = new("NIN: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobNINHuton = new("NIN: Huton", PaletteTypes.JobGauges, Color.White);
        public ColorMapping JobNINNinkiGauge = new("NIN: Ninki Gauge", PaletteTypes.JobGauges, Color.Coral);
        public ColorMapping JobDRKNegative = new("DRK: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobDRKBloodGauge = new("DRK: Blood Gauge", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobDRKGrit = new("DRK: Grit", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobDRKDarkside = new("DRK: Darkside", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobASTNegative = new("AST: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobASTArrow = new("AST: Arrow Drawn", PaletteTypes.JobGauges, Color.Lime);
        public ColorMapping JobASTBalance = new("AST: Balance Drawn", PaletteTypes.JobGauges, Color.Crimson);
        public ColorMapping JobASTBole = new("AST: Bole Drawn", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobASTEwer = new("AST: Ewer Drawn", PaletteTypes.JobGauges, Color.MediumBlue);
        public ColorMapping JobASTSpear = new("AST: Spear Drawn", PaletteTypes.JobGauges, Color.Turquoise);
        public ColorMapping JobASTSpire = new("AST: Spire Drawn", PaletteTypes.JobGauges, Color.SlateBlue);
        public ColorMapping JobASTLady = new("AST: Lady of Crowns Drawn", PaletteTypes.JobGauges, Color.HotPink);
        public ColorMapping JobASTLord = new("AST: Lord of Crowns Drawn", PaletteTypes.JobGauges, Color.Magenta);
        public ColorMapping JobMCHNegative = new("MCH: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobMCHAmmo = new("MCH: Ammo", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobMCHHeatGauge = new("MCH: Heat Gauge", PaletteTypes.JobGauges, Color.DarkOrange);
        public ColorMapping JobMCHOverheat = new("MCH: Overheat", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobSAMNegative = new("SAM: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobSAMSetsu = new("SAM: Setsu", PaletteTypes.JobGauges, Color.Aquamarine);
        public ColorMapping JobSAMGetsu = new("SAM: Getsu", PaletteTypes.JobGauges, Color.Azure);
        public ColorMapping JobSAMKa = new("SAM: Ka", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobSAMKenki = new("SAM: Kenki Charge", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobRDMNegative = new("RDM: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobRDMBlackMana = new("RDM: Black Mana", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobRDMWhiteMana = new("RDM: White Mana", PaletteTypes.JobGauges, Color.White);
        public ColorMapping JobDNCNegative = new("DNC: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobDNCEntrechat = new("DNC: Entrechat", PaletteTypes.JobGauges, Color.Blue);
        public ColorMapping JobDNCPirouette = new("DNC: Pirouette", PaletteTypes.JobGauges, Color.Yellow);
        public ColorMapping JobDNCEmboite = new("DNC: Emboite", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobDNCJete = new("DNC: Jete", PaletteTypes.JobGauges, Color.Lime);
        public ColorMapping JobDNCStandardFinish = new("DNC: Standard Finish", PaletteTypes.JobGauges, Color.Aquamarine);
        public ColorMapping JobDNCTechnicalFinish = new("DNC: Technical Finish", PaletteTypes.JobGauges, Color.MediumVioletRed);
        public ColorMapping JobGNBNegative = new("GNB: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobGNBRoyalGuard = new("GNB: Royal Guard", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobSGENegative = new("SGE: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobSGEAddersgallStacks = new("SGE: Addersgall Stacks", PaletteTypes.JobGauges, Color.LightBlue);
        public ColorMapping JobSGEEukrasiaActive = new("SGE: Eukrasia Active", PaletteTypes.JobGauges, Color.DodgerBlue);
        public ColorMapping JobSGEAddersgallRecharge = new("SGE: Addersgall Recharge", PaletteTypes.JobGauges, Color.LightBlue);
        public ColorMapping JobRPRNegative = new("RPR: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobRPRSouls = new("RPR: Souls Collected", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobRPRShrouds = new("RPR: Shrouds Collected", PaletteTypes.JobGauges, Color.DodgerBlue);
        public ColorMapping JobCrafterNegative = new("Crafter: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobCrafterInnerquiet = new("Crafter: Inner Quiet Stacks", PaletteTypes.JobGauges, Color.BlueViolet);
        public ColorMapping JobCrafterCollectable = new("Crafter: Collectables", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobCrafterCrafter = new("Crafter: Crafting", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        
        //Reactive Weather
        public ColorMapping WeatherClearBase = new("Clear (Base)", PaletteTypes.ReactiveWeather, Color.SkyBlue);
        public ColorMapping WeatherClearHighlight = new("Clear (Highlight)", PaletteTypes.ReactiveWeather, Color.LightYellow);
        public ColorMapping WeatherFairBase = new("Fair (Base)", PaletteTypes.ReactiveWeather, Color.SkyBlue);
        public ColorMapping WeatherFairHighlight = new("Fair (Highlight)", PaletteTypes.ReactiveWeather, Color.LightYellow);
        public ColorMapping WeatherCloudsBase = new("Clouds (Base)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherCloudsHighlight = new("Clouds (Highlight)", PaletteTypes.ReactiveWeather, Color.Azure);
        public ColorMapping WeatherFogBase = new("Fog (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateGray);
        public ColorMapping WeatherFogHighlight = new("Fog (Highlight)", PaletteTypes.ReactiveWeather, Color.Azure);
        public ColorMapping WeatherWindBase = new("Wind (Base)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherWindHighlight = new("Wind (Highlight)", PaletteTypes.ReactiveWeather, Color.Aquamarine);
        public ColorMapping WeatherGalesBase = new("Gales (Base)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherGalesHighlight = new("Gales (Highlight)", PaletteTypes.ReactiveWeather, Color.Aquamarine);
        public ColorMapping WeatherRainBase = new("Rain (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherRainHighlight = new("Rain (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkBlue);
        public ColorMapping WeatherShowersBase = new("Showers (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherShowersHighlight = new("Showers (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkBlue);
        public ColorMapping WeatherThunderBase = new("Thunder (Base)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherThunderHighlight = new("Thunder (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherThunderstormsBase = new("Thunderstorms (Base)", PaletteTypes.ReactiveWeather, Color.DarkBlue);
        public ColorMapping WeatherThunderstormsHighlight = new("Thunderstorms (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherDustBase = new("Dust (Base)", PaletteTypes.ReactiveWeather, Color.DarkSalmon);
        public ColorMapping WeatherDustHighlight = new("Dust (Highlight)", PaletteTypes.ReactiveWeather, Color.Coral);
        public ColorMapping WeatherSandstormBase = new("Sandstorm (Base)", PaletteTypes.ReactiveWeather, Color.DarkSalmon);
        public ColorMapping WeatherSandstormHighlight = new("Sandstorm (Highlight)", PaletteTypes.ReactiveWeather, Color.OrangeRed);
        public ColorMapping WeatherHotspellBase = new("Hotspell (Base)", PaletteTypes.ReactiveWeather, Color.Orange);
        public ColorMapping WeatherHotspellHighlight = new("Hotspell (Highlight)", PaletteTypes.ReactiveWeather, Color.Red);
        public ColorMapping WeatherHeatwaveBase = new("Heatwave (Base)", PaletteTypes.ReactiveWeather, Color.Orange);
        public ColorMapping WeatherHeatwaveHighlight = new("Heatwave (Highlight)", PaletteTypes.ReactiveWeather, Color.Red);
        public ColorMapping WeatherSnowBase = new("Snow (Base)", PaletteTypes.ReactiveWeather, Color.Snow);
        public ColorMapping WeatherSnowHighlight = new("Snow (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherBlizzardsBase = new("Blizzards (Base)", PaletteTypes.ReactiveWeather, Color.Snow);
        public ColorMapping WeatherBlizzardsHighlight = new("Blizzards (Highlight)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherGloomBase = new("Gloom (Base)", PaletteTypes.ReactiveWeather, Color.Magenta);
        public ColorMapping WeatherGloomHighlight = new("Glooom (Highligh)", PaletteTypes.ReactiveWeather, Color.Azure);
        public ColorMapping WeatherAurorasBase = new("Auroras (Base)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherAurorasHighlight = new("Auroras (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherDarknessBase = new("Darkness (Base)", PaletteTypes.ReactiveWeather, Color.DarkBlue);
        public ColorMapping WeatherDarknessHighlight = new("Darkness (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkMagenta);
        public ColorMapping WeatherTensionBase = new("Tension (Base)", PaletteTypes.ReactiveWeather, Color.DarkOrchid);
        public ColorMapping WeatherTensionHighlight = new("Tension (Highlight)", PaletteTypes.ReactiveWeather, Color.Magenta);
        public ColorMapping WeatherStormcloudsBase = new("Stormclouds (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateGray);
        public ColorMapping WeatherStormcloudsHighlight = new("Stormclouds (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherRoughseasBase = new("Roughseas (Base)", PaletteTypes.ReactiveWeather, Color.CornflowerBlue);
        public ColorMapping WeatherRoughseasHighlight = new("Roughseas (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherLouringBase = new("Louring (Base)", PaletteTypes.ReactiveWeather, Color.Maroon);
        public ColorMapping WeatherLouringHighlight = new("Louring (Highligh)", PaletteTypes.ReactiveWeather, Color.Magenta);
        public ColorMapping WeatherEruptionsBase = new("Eruptions (Base)", PaletteTypes.ReactiveWeather, Color.DarkRed);
        public ColorMapping WeatherEruptionsHighlight = new("Eruptions (Highlight)", PaletteTypes.ReactiveWeather, Color.Red);
        public ColorMapping WeatherIrradianceBase = new("Irradiance (Base)", PaletteTypes.ReactiveWeather, Color.Gold);
        public ColorMapping WeatherIrradianceHighlight = new("Irradiance (Highlight)", PaletteTypes.ReactiveWeather, Color.Orange);
        public ColorMapping WeatherCoreradiationBase = new("Core Radiation (Base)", PaletteTypes.ReactiveWeather, Color.GreenYellow);
        public ColorMapping WeatherCoreradiationHighlight = new("Core Radiation (Highlight)", PaletteTypes.ReactiveWeather, Color.Gold);
        public ColorMapping WeatherShelfcloudsBase = new("Shelf Clouds (Base)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherShelfcloudsHighlight = new("Shelf Clouds (Highlight)", PaletteTypes.ReactiveWeather, Color.Azure);
        public ColorMapping WeatherOppressionBase = new("Oppression (Base)", PaletteTypes.ReactiveWeather, Color.DarkMagenta);
        public ColorMapping WeatherOppressionHighlight = new("Oppresion (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherUmbralwindBase = new("Umbral Wind (Base)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherUmbralwindHighlight = new("Umbral Wind (Highlight)", PaletteTypes.ReactiveWeather, Color.Azure);
        public ColorMapping WeatherUmbralstaticBase = new("Umbral Static (Base)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherUmbralstaticHighlight = new("Umbral Static (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherSmokeBase = new("Smoke (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateGray);
        public ColorMapping WeatherSmokeHighlight = new("Smoke (Highlight)", PaletteTypes.ReactiveWeather, Color.Azure);
        public ColorMapping WeatherRoyallevinBase = new("Royal Levin (Base)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherRoyallevinHighlight = new("Royal Levin (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkBlue);
        public ColorMapping WeatherHyperelectricityBase = new("Hyper Electricity (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherHyperelectricityHighlight = new("Hyper Electricity (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherMultiplicityBase = new("Multiplicity (Base)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherMultiplicityHighlight = new("Multiplicity (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkGray);
        public ColorMapping WeatherDragonstormBase = new("Dragonstorm (Base)", PaletteTypes.ReactiveWeather, Color.OrangeRed);
        public ColorMapping WeatherDragonstormHighlight = new("Dragonstorm (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherSubterrainBase = new("Subterrain (Base)", PaletteTypes.ReactiveWeather, Color.Goldenrod);
        public ColorMapping WeatherSubterrainHighlight = new("Subterrain (Highlight)", PaletteTypes.ReactiveWeather, Color.SandyBrown);
        public ColorMapping WeatherConcordanceBase = new("Concordance (Base)", PaletteTypes.ReactiveWeather, Color.Brown);
        public ColorMapping WeatherConcordanceHighlight = new("Concordance (Highlight)", PaletteTypes.ReactiveWeather, Color.SandyBrown);
        public ColorMapping WeatherBeyondtimeBase = new("Beyond Time (Base)", PaletteTypes.ReactiveWeather, Color.Gold);
        public ColorMapping WeatherBeyondtimeHighlight = new("Beyond Time (Highlight)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherDemonicinfinityBase = new("Demonic Infinity (Base)", PaletteTypes.ReactiveWeather, Color.DarkRed);
        public ColorMapping WeatherDemonicinfinityHighlight = new("Demonic Infinity (Highlight)", PaletteTypes.ReactiveWeather, Color.Red);
        public ColorMapping WeatherDimensionaldisruptionBase = new("Dimensional Disruption (Base)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherDimensionaldisruptionHighlight = new("Dimensional Disruption (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkMagenta);
        public ColorMapping WeatherRevelstormBase = new("Revel Storm (Base)", PaletteTypes.ReactiveWeather, Color.LightSkyBlue);
        public ColorMapping WeatherRevelstormHighlight = new("Revel Storm (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherEternalblissBase = new("Eternal Bliss (Base)", PaletteTypes.ReactiveWeather, Color.Gold);
        public ColorMapping WeatherEternalblissHighlight = new("Eternal Bliss (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherWyrmstormBase = new("Wyrmstorm (Base)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherWyrmstormHighlight = new("Wyrmstorm (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkSlateGray);
        public ColorMapping WeatherQuicklevinBase = new("Quick Levin (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherQuicklevinHighlight = new("Quick Levin (Highlight)", PaletteTypes.ReactiveWeather, Color.Blue);
        public ColorMapping WeatherWhitecycloneBase = new("White Cyclone (Base)", PaletteTypes.ReactiveWeather, Color.White);
        public ColorMapping WeatherWhitecycloneHighlight = new("White Cyclone (Highlight)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherGeostormsBase = new("Geostorms (Base)", PaletteTypes.ReactiveWeather, Color.Aquamarine);
        public ColorMapping WeatherGeostormsHighlight = new("Geostorms (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumPurple);
        public ColorMapping WeatherTrueblueBase = new("True Blue (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherTrueblueHighlight = new("True Blue (Highlight)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherUmbralturbulenceBase = new("Umbral Turbulence (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherUmbralturbulenceHighlight = new("Umbral Turbulence (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherEverlastinglightBase = new("Everlasting Light (Base)", PaletteTypes.ReactiveWeather, Color.Salmon);
        public ColorMapping WeatherEverlastinglightHighlight = new("Everlasting Light (Highlight)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherTerminationBase = new("Termination (Base)", PaletteTypes.ReactiveWeather, Color.DarkMagenta);
        public ColorMapping WeatherTerminationHighlight = new("Termination (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumPurple);

        //Notifications
        public ColorMapping DutyFinderBell = new("Duty Finder Bell", PaletteTypes.Notifications, Color.Red);
        public ColorMapping PullCountdownTick = new("Pull Countdown (Tick)", PaletteTypes.Notifications, Color.Turquoise);
        public ColorMapping PullCountdownEmpty = new("Pull Countdown (Empty)", PaletteTypes.Notifications, Color.Black);
        public ColorMapping PullCountdownEngage = new("Pull Countdown (Engage)", PaletteTypes.Notifications, Color.Lime);

        //Job Claases
        public ColorMapping JobPLDBase = new("PLD  (Base)", PaletteTypes.JobClasses, Color.DeepSkyBlue);
        public ColorMapping JobPLDHighlight = new("PLD (Highlight)", PaletteTypes.JobClasses, Color.Yellow);
        public ColorMapping JobMNKBase = new("MNK (Base)", PaletteTypes.JobClasses, Color.Orange);
        public ColorMapping JobMNKHighlight = new("MNK (Highlight)", PaletteTypes.JobClasses, Color.Brown);
        public ColorMapping JobWARBase = new("WAR (Base)", PaletteTypes.JobClasses, Color.Blue);
        public ColorMapping JobWARHighlight = new("WAR (Highlight)", PaletteTypes.JobClasses, Color.White);
        public ColorMapping JobDRGBase = new("DRG (Base)", PaletteTypes.JobClasses, Color.Maroon);
        public ColorMapping JobDRGHighlight = new("DRG (Highlight)", PaletteTypes.JobClasses, Color.Red);
        public ColorMapping JobBRDBase = new("BRD (Base)", PaletteTypes.JobClasses, Color.Orange);
        public ColorMapping JobBRDHighlight = new("BRD (Highlight)", PaletteTypes.JobClasses, Color.Lime);
        public ColorMapping JobWHMBase = new("WHM (Base)", PaletteTypes.JobClasses, Color.DeepSkyBlue);
        public ColorMapping JobWHMHighlight = new("WHM (Highlight)", PaletteTypes.JobClasses, Color.Snow);
        public ColorMapping JobBLMBase = new("BLM (Base)", PaletteTypes.JobClasses, Color.DarkMagenta);
        public ColorMapping JobBLMHighlight = new("BLM (Highlight)", PaletteTypes.JobClasses, Color.Orange);
        public ColorMapping JobSMNBase = new("SMN (Base)", PaletteTypes.JobClasses, Color.Yellow);
        public ColorMapping JobSMNHighlight = new("SMN (Highlight)", PaletteTypes.JobClasses, Color.Lime);
        public ColorMapping JobSCHBase = new("SCH (Base)", PaletteTypes.JobClasses, Color.MediumSpringGreen);
        public ColorMapping JobSCHHighlight = new("SCH (Highlight)", PaletteTypes.JobClasses, Color.DeepSkyBlue);
        public ColorMapping JobNINBase = new("NIN (Base)", PaletteTypes.JobClasses, Color.DarkMagenta);
        public ColorMapping JobNINHighlight = new("NIN (Highlight)", PaletteTypes.JobClasses, Color.RosyBrown);
        public ColorMapping JobMCHBase = new("MCH (Base)", PaletteTypes.JobClasses, Color.SaddleBrown);
        public ColorMapping JobMCHHighlight = new("MCH (Highlight)", PaletteTypes.JobClasses, Color.SandyBrown);
        public ColorMapping JobDRKBase = new("DRK (Base)", PaletteTypes.JobClasses, Color.Blue);
        public ColorMapping JobDRKHighlight = new("DRK (Highlight)", PaletteTypes.JobClasses, Color.Red);
        public ColorMapping JobASTBase = new("AST (Base)", PaletteTypes.JobClasses, Color.White);
        public ColorMapping JobASTHighlight = new("AST (Highlight)", PaletteTypes.JobClasses, Color.MediumSpringGreen);
        public ColorMapping JobSAMBase = new("SAM (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobSAMHighlight = new("SAM (Highlight)", PaletteTypes.JobClasses, Color.White);
        public ColorMapping JobRDMBase = new("RDM (Base)", PaletteTypes.JobClasses, Color.MediumVioletRed);
        public ColorMapping JobRDMHighlight = new("RDM (Highlight)", PaletteTypes.JobClasses, Color.White);
        public ColorMapping JobDNCBase = new("DNC (Base)", PaletteTypes.JobClasses, Color.BlueViolet);
        public ColorMapping JobDNCHighlight = new("DNC (Highlight)", PaletteTypes.JobClasses, Color.CornflowerBlue);
        public ColorMapping JobGNBBase = new("GNB (Base)", PaletteTypes.JobClasses, Color.DarkMagenta);
        public ColorMapping JobGNBHighlight = new("GNB (Highlight)", PaletteTypes.JobClasses, Color.Blue);
        public ColorMapping JobRPRBase = new("RPR (Base)", PaletteTypes.JobClasses, Color.DarkRed);
        public ColorMapping JobRPRHighlight = new("RPR (Highlight)", PaletteTypes.JobClasses, Color.Red);
        public ColorMapping JobSGEBase = new("SGE (Base)", PaletteTypes.JobClasses, Color.LimeGreen);
        public ColorMapping JobSGEHighlight = new("SGE (Highlight)", PaletteTypes.JobClasses, Color.DodgerBlue);
        public ColorMapping JobBLUBase = new("BLU (Base)", PaletteTypes.JobClasses, Color.DeepSkyBlue);
        public ColorMapping JobBLUHighlight = new("BLU (Highlight)", PaletteTypes.JobClasses, Color.Blue);
        public ColorMapping JobCPTBase = new("CPT (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobCPTHighlight = new("CPT (Highlight)", PaletteTypes.JobClasses, Color.Orchid);
        public ColorMapping JobBSMBase = new("BSM (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobBSMHighlight = new("BSM (Highlight)", PaletteTypes.JobClasses, Color.Orchid);
        public ColorMapping JobARMBase = new("ARM (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobARMHighlight = new("ARM (Highlight)", PaletteTypes.JobClasses, Color.Orchid);
        public ColorMapping JobGSMBase = new("GSM (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobGSMHighlight = new("GSM (Highlight)", PaletteTypes.JobClasses, Color.Orchid);
        public ColorMapping JobLTWBase = new("LTW (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobLTWHighlight = new("LTW (Highlight)", PaletteTypes.JobClasses, Color.Orchid);
        public ColorMapping JobWVRBase = new("WVR (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobWVRHighlight = new("WVR (Highlight)", PaletteTypes.JobClasses, Color.Orchid);
        public ColorMapping JobALCBase = new("ALC (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobALCHighlight = new("ALC (Highlight)", PaletteTypes.JobClasses, Color.Orchid);
        public ColorMapping JobCULBase = new("CUL (Base)", PaletteTypes.JobClasses, Color.DarkOrange);
        public ColorMapping JobCULHighlight = new("CUL (Highlight)", PaletteTypes.JobClasses, Color.Orchid);
        public ColorMapping JobMINBase = new("MIN (Base)", PaletteTypes.JobClasses, Color.Gray);
        public ColorMapping JobMINHighlight = new("MIN (Highlight)", PaletteTypes.JobClasses, Color.Blue);
        public ColorMapping JobBTNBase = new("BTN (Base)", PaletteTypes.JobClasses, Color.MediumSpringGreen);
        public ColorMapping JobBTNHighlight = new("BTN (Highlight)", PaletteTypes.JobClasses, Color.Yellow);
        public ColorMapping JobFSHBase = new("FSH (Base)", PaletteTypes.JobClasses, Color.DeepSkyBlue);
        public ColorMapping JobFSHHighlight = new("FSH (Highlight)", PaletteTypes.JobClasses, Color.White);
    }

    public class LegacyColorMappings
    {
        public string ColorMappingDeviceDisabled { get; set; }
        public string ColorMappingAmnesia { get; set; }
        public string ColorMappingBaseColor { get; set; }
        public string ColorMappingBind { get; set; }
        public string ColorMappingVulnerabilityUp { get; set; }
        public string ColorMappingBleed { get; set; }
        public string ColorMappingBurns { get; set; }
        public string ColorMappingCastChargeEmpty { get; set; }
        public string ColorMappingCastChargeFull { get; set; }
        public string ColorMappingCpEmpty { get; set; }
        public string ColorMappingCpFull { get; set; }
        public string ColorMappingDamageDown { get; set; }
        public string ColorMappingDaze { get; set; }
        public string ColorMappingOld { get; set; }
        public string ColorMappingDeepFreeze { get; set; }
        public string ColorMappingDropsy { get; set; }
        public string ColorMappingDutyFinderBell { get; set; }
        public string ColorMappingEmnity0 { get; set; }
        public string ColorMappingEmnity1 { get; set; }
        public string ColorMappingEmnity2 { get; set; }
        public string ColorMappingEmnity3 { get; set; }
        public string ColorMappingEmnity4 { get; set; }
        public string ColorMappingGcdEmpty { get; set; }
        public string ColorMappingGcdHot { get; set; }
        public string ColorMappingGcdReady { get; set; }
        public string ColorMappingGpEmpty { get; set; }
        public string ColorMappingGpFull { get; set; }
        public string ColorMappingHeavy { get; set; }
        public string ColorMappingHighlightColor { get; set; }
        public string ColorMappingHotbarCd { get; set; }
        public string ColorMappingHotbarNotAvailable { get; set; }
        public string ColorMappingHotbarOutRange { get; set; }
        public string ColorMappingHotbarProc { get; set; }
        public string ColorMappingHotbarReady { get; set; }
        public string ColorMappingKeybindDisabled { get; set; }
        public string ColorMappingKeybindMap { get; set; }
        public string ColorMappingKeybindAetherCurrents { get; set; }
        public string ColorMappingKeybindSigns { get; set; }
        public string ColorMappingKeybindWaymarks { get; set; }
        public string ColorMappingKeybindRecordReadyCheck { get; set; }
        public string ColorMappingKeybindReadyCheck { get; set; }
        public string ColorMappingKeybindCountdown { get; set; }
        public string ColorMappingKeybindEmotes { get; set; }
        public string ColorMappingKeybindCrossWorldLS { get; set; }
        public string ColorMappingKeybindLinkshells { get; set; }
        public string ColorMappingKeybindContacts { get; set; }
        public string ColorMappingKeybindSprint { get; set; }
        public string ColorMappingKeybindTeleport { get; set; }
        public string ColorMappingKeybindReturn { get; set; }
        public string ColorMappingKeybindLimitBreak { get; set; }
        public string ColorMappingKeybindDutyAction { get; set; }
        public string ColorMappingKeybindRepair { get; set; }
        public string ColorMappingKeybindDig { get; set; }
        public string ColorMappingKeybindInventory { get; set; }
        public string ColorMappingHpCritical { get; set; }
        public string ColorMappingHpEmpty { get; set; }
        public string ColorMappingHpFull { get; set; }
        public string ColorMappingHpLoss { get; set; }
        public string ColorMappingIncapacitation { get; set; }
        public string ColorMappingInfirmary { get; set; }
        public string ColorMappingLeaden { get; set; }
        public string ColorMappingMisery { get; set; }
        public string ColorMappingMpEmpty { get; set; }
        public string ColorMappingMpFull { get; set; }
        public string ColorMappingNoEmnity { get; set; }
        public string ColorMappingParalysis { get; set; }
        public string ColorMappingPetrification { get; set; }
        public string ColorMappingPoison { get; set; }
        public string ColorMappingPollen { get; set; }
        public string ColorMappingPox { get; set; }
        public string ColorMappingSilence { get; set; }
        public string ColorMappingSleep { get; set; }
        public string ColorMappingSlow { get; set; }
        public string ColorMappingStun { get; set; }
        public string ColorMappingTargetCasting { get; set; }
        public string ColorMappingTargetHpFriendly { get; set; }
        public string ColorMappingTargetHpClaimed { get; set; }
        public string ColorMappingTargetHpEmpty { get; set; }
        public string ColorMappingTargetHpIdle { get; set; }
        public string ColorMappingExpEmpty { get; set; }
        public string ColorMappingExpFull { get; set; }
        public string ColorMappingExpMax { get; set; }
        public string ColorMappingJobWARNegative { get; set; }
        public string ColorMappingJobWARBeastGauge { get; set; }
        public string ColorMappingJobWARBeastGaugeMax { get; set; }
        public string ColorMappingJobWARDefiance { get; set; }
        public string ColorMappingJobWARNonDefiance { get; set; }
        public string ColorMappingJobPLDNegative { get; set; }
        public string ColorMappingJobPLDOathGauge { get; set; }
        public string ColorMappingJobPLDSwordOath { get; set; }
        public string ColorMappingJobPLDIronWill { get; set; }
        public string ColorMappingJobMNKNegative { get; set; }
        public string ColorMappingJobMNKGreased { get; set; }
        public string ColorMappingJobDRGNegative { get; set; }
        public string ColorMappingJobDRGBloodDragon { get; set; }
        public string ColorMappingJobDRGDragonGauge1 { get; set; }
        public string ColorMappingJobDRGDragonGauge2 { get; set; }
        public string ColorMappingJobDRGLifeOfTheDragon { get; set; }
        public string ColorMappingJobBRDNegative { get; set; }
        public string ColorMappingJobBRDRepertoire { get; set; }
        public string ColorMappingJobBRDBallad { get; set; }
        public string ColorMappingJobBRDArmys { get; set; }
        public string ColorMappingJobBRDMinuet { get; set; }
        public string ColorMappingJobWHMNegative { get; set; }
        public string ColorMappingJobWHMFlowerPetal { get; set; }
        public string ColorMappingJobWHMFlowerCharge { get; set; }
        public string ColorMappingJobWHMBloodLily { get; set; }
        public string ColorMappingJobWHMFreecure { get; set; }
        public string ColorMappingJobBLMNegative { get; set; }
        public string ColorMappingJobBLMAstralFire { get; set; }
        public string ColorMappingJobBLMUmbralIce { get; set; }
        public string ColorMappingJobBLMUmbralHeart { get; set; }
        public string ColorMappingJobBLMEnochianCountdown { get; set; }
        public string ColorMappingJobBLMEnochianCharge { get; set; }
        public string ColorMappingJobBLMPolyglot { get; set; }
        public string ColorMappingJobSMNNegative { get; set; }
        public string ColorMappingJobSMNAetherflow { get; set; }
        public string ColorMappingJobSCHNegative { get; set; }
        public string ColorMappingJobSCHAetherflow { get; set; }
        public string ColorMappingJobSCHFaerieGauge { get; set; }
        public string ColorMappingJobNINNegative { get; set; }
        public string ColorMappingJobNINHuton { get; set; }
        public string ColorMappingJobNINNinkiGauge { get; set; }
        public string ColorMappingJobDRKNegative { get; set; }
        public string ColorMappingJobDRKBloodGauge { get; set; }
        public string ColorMappingJobDRKGrit { get; set; }
        public string ColorMappingJobDRKDarkside { get; set; }
        public string ColorMappingJobASTNegative { get; set; }
        public string ColorMappingJobASTArrow { get; set; }
        public string ColorMappingJobASTBalance { get; set; }
        public string ColorMappingJobASTBole { get; set; }
        public string ColorMappingJobASTEwer { get; set; }
        public string ColorMappingJobASTSpear { get; set; }
        public string ColorMappingJobASTSpire { get; set; }
        public string ColorMappingJobASTLady { get; set; }
        public string ColorMappingJobASTLord { get; set; }
        public string ColorMappingJobMCHNegative { get; set; }
        public string ColorMappingJobMCHAmmo { get; set; }
        public string ColorMappingJobMCHHeatGauge { get; set; }
        public string ColorMappingJobMCHOverheat { get; set; }
        public string ColorMappingJobSAMNegative { get; set; }
        public string ColorMappingJobSAMSetsu { get; set; }
        public string ColorMappingJobSAMGetsu { get; set; }
        public string ColorMappingJobSAMKa { get; set; }
        public string ColorMappingJobSAMKenki { get; set; }
        public string ColorMappingJobRDMNegative { get; set; }
        public string ColorMappingJobRDMBlackMana { get; set; }
        public string ColorMappingJobRDMWhiteMana { get; set; }
        public string ColorMappingJobDNCNegative { get; set; }
        public string ColorMappingJobDNCEntrechat { get; set; }
        public string ColorMappingJobDNCPirouette { get; set; }
        public string ColorMappingJobDNCEmboite { get; set; }
        public string ColorMappingJobDNCJete { get; set; }
        public string ColorMappingJobDNCStandardFinish { get; set; }
        public string ColorMappingJobDNCTechnicalFinish { get; set; }
        public string ColorMappingJobGNBNegative { get; set; }
        public string ColorMappingJobGNBRoyalGuard { get; set; }
        public string ColorMappingJobSGENegative { get; set; }
        public string ColorMappingJobSGEAddersgallStacks { get; set; }
        public string ColorMappingJobSGEEukrasiaActive { get; set; }
        public string ColorMappingJobSGEAddersgallRecharge { get; set; }
        public string ColorMappingJobRPRNegative { get; set; }
        public string ColorMappingJobRPRSouls { get; set; }
        public string ColorMappingJobRPRShrouds { get; set; }
        public string ColorMappingMenuBase { get; set; }
        public string ColorMappingJobCrafterNegative { get; set; }
        public string ColorMappingJobCrafterInnerquiet { get; set; }
        public string ColorMappingJobCrafterCollectable { get; set; }
        public string ColorMappingJobCrafterCrafter { get; set; }
        public string ColorMappingMenuHighlight1 { get; set; }
        public string ColorMappingMenuHighlight2 { get; set; }
        public string ColorMappingMenuHighlight3 { get; set; }
        public string ColorMappingCutsceneBase { get; set; }
        public string ColorMappingCutsceneHighlight1 { get; set; }
        public string ColorMappingCutsceneHighlight2 { get; set; }
        public string ColorMappingCutsceneHighlight3 { get; set; }
        public string ColorMappingWeatherClearBase { get; set; }
        public string ColorMappingWeatherClearHighlight { get; set; }
        public string ColorMappingWeatherFairBase { get; set; }
        public string ColorMappingWeatherFairHighlight { get; set; }
        public string ColorMappingWeatherCloudsBase { get; set; }
        public string ColorMappingWeatherCloudsHighlight { get; set; }
        public string ColorMappingWeatherFogBase { get; set; }
        public string ColorMappingWeatherFogHighlight { get; set; }
        public string ColorMappingWeatherWindBase { get; set; }
        public string ColorMappingWeatherWindHighlight { get; set; }
        public string ColorMappingWeatherGalesBase { get; set; }
        public string ColorMappingWeatherGalesHighlight { get; set; }
        public string ColorMappingWeatherRainBase { get; set; }
        public string ColorMappingWeatherRainHighlight { get; set; }
        public string ColorMappingWeatherShowersBase { get; set; }
        public string ColorMappingWeatherShowersHighlight { get; set; }
        public string ColorMappingWeatherThunderBase { get; set; }
        public string ColorMappingWeatherThunderHighlight { get; set; }
        public string ColorMappingWeatherThunderstormsBase { get; set; }
        public string ColorMappingWeatherThunderstormsHighlight { get; set; }
        public string ColorMappingWeatherDustBase { get; set; }
        public string ColorMappingWeatherDustHighlight { get; set; }
        public string ColorMappingWeatherSandstormBase { get; set; }
        public string ColorMappingWeatherSandstormHighlight { get; set; }
        public string ColorMappingWeatherHotspellBase { get; set; }
        public string ColorMappingWeatherHotspellHighlight { get; set; }
        public string ColorMappingWeatherHeatwaveBase { get; set; }
        public string ColorMappingWeatherHeatwaveHighlight { get; set; }
        public string ColorMappingWeatherSnowBase { get; set; }
        public string ColorMappingWeatherSnowHighlight { get; set; }
        public string ColorMappingWeatherBlizzardsBase { get; set; }
        public string ColorMappingWeatherBlizzardsHighlight { get; set; }
        public string ColorMappingWeatherGloomBase { get; set; }
        public string ColorMappingWeatherGloomHighlight { get; set; }
        public string ColorMappingWeatherAurorasBase { get; set; }
        public string ColorMappingWeatherAurorasHighlight { get; set; }
        public string ColorMappingWeatherDarknessBase { get; set; }
        public string ColorMappingWeatherDarknessHighlight { get; set; }
        public string ColorMappingWeatherTensionBase { get; set; }
        public string ColorMappingWeatherTensionHighlight { get; set; }
        public string ColorMappingWeatherStormcloudsBase { get; set; }
        public string ColorMappingWeatherStormcloudsHighlight { get; set; }
        public string ColorMappingWeatherRoughseasBase { get; set; }
        public string ColorMappingWeatherRoughseasHighlight { get; set; }
        public string ColorMappingWeatherLouringBase { get; set; }
        public string ColorMappingWeatherLouringHighlight { get; set; }
        public string ColorMappingWeatherEruptionsBase { get; set; }
        public string ColorMappingWeatherEruptionsHighlight { get; set; }
        public string ColorMappingWeatherIrradianceBase { get; set; }
        public string ColorMappingWeatherIrradianceHighlight { get; set; }
        public string ColorMappingWeatherCoreradiationBase { get; set; }
        public string ColorMappingWeatherCoreradiationHighlight { get; set; }
        public string ColorMappingWeatherShelfcloudsBase { get; set; }
        public string ColorMappingWeatherShelfcloudsHighlight { get; set; }
        public string ColorMappingWeatherOppressionBase { get; set; }
        public string ColorMappingWeatherOppressionHighlight { get; set; }
        public string ColorMappingWeatherUmbralwindBase { get; set; }
        public string ColorMappingWeatherUmbralwindHighlight { get; set; }
        public string ColorMappingWeatherUmbralstaticBase { get; set; }
        public string ColorMappingWeatherUmbralstaticHighlight { get; set; }
        public string ColorMappingWeatherSmokeBase { get; set; }
        public string ColorMappingWeatherSmokeHighlight { get; set; }
        public string ColorMappingWeatherRoyallevinBase { get; set; }
        public string ColorMappingWeatherRoyallevinHighlight { get; set; }
        public string ColorMappingWeatherHyperelectricityBase { get; set; }
        public string ColorMappingWeatherHyperelectricityHighlight { get; set; }
        public string ColorMappingWeatherMultiplicityBase { get; set; }
        public string ColorMappingWeatherMultiplicityHighlight { get; set; }
        public string ColorMappingWeatherDragonstormBase { get; set; }
        public string ColorMappingWeatherDragonstormHighlight { get; set; }
        public string ColorMappingWeatherSubterrainBase { get; set; }
        public string ColorMappingWeatherSubterrainHighlight { get; set; }
        public string ColorMappingWeatherConcordanceBase { get; set; }
        public string ColorMappingWeatherConcordanceHighlight { get; set; }
        public string ColorMappingWeatherBeyondtimeBase { get; set; }
        public string ColorMappingWeatherBeyondtimeHighlight { get; set; }
        public string ColorMappingWeatherDemonicinfinityBase { get; set; }
        public string ColorMappingWeatherDemonicinfinityHighlight { get; set; }
        public string ColorMappingWeatherDimensionaldisruptionBase { get; set; }
        public string ColorMappingWeatherDimensionaldisruptionHighlight { get; set; }
        public string ColorMappingWeatherRevelstormBase { get; set; }
        public string ColorMappingWeatherRevelstormHighlight { get; set; }
        public string ColorMappingWeatherEternalblissBase { get; set; }
        public string ColorMappingWeatherEternalblissHighlight { get; set; }
        public string ColorMappingWeatherWyrmstormBase { get; set; }
        public string ColorMappingWeatherWyrmstormHighlight { get; set; }
        public string ColorMappingWeatherQuicklevinBase { get; set; }
        public string ColorMappingWeatherQuicklevinHighlight { get; set; }
        public string ColorMappingWeatherWhitecycloneBase { get; set; }
        public string ColorMappingWeatherWhitecycloneHighlight { get; set; }
        public string ColorMappingWeatherGeostormsBase { get; set; }
        public string ColorMappingWeatherGeostormsHighlight { get; set; }
        public string ColorMappingWeatherTrueblueBase { get; set; }
        public string ColorMappingWeatherTrueblueHighlight { get; set; }
        public string ColorMappingWeatherUmbralturbulenceBase { get; set; }
        public string ColorMappingWeatherUmbralturbulenceHighlight { get; set; }
        public string ColorMappingWeatherEverlastinglightBase { get; set; }
        public string ColorMappingWeatherEverlastinglightHighlight { get; set; }
        public string ColorMappingWeatherTerminationBase { get; set; }
        public string ColorMappingWeatherTerminationHighlight { get; set; }
        public string ColorMappingPullCountdownTick { get; set; }
        public string ColorMappingPullCountdownEmpty { get; set; }
        public string ColorMappingPullCountdownEngage { get; set; }
        public string ColorMappingACTThresholdEmpty { get; set; }
        public string ColorMappingACTThresholdBuild { get; set; }
        public string ColorMappingACTThresholdSuccess { get; set; }
        public string ColorMappingACTThresholdFlash { get; set; }
        public string ColorMappingACTCustomTriggerIdle { get; set; }
        public string ColorMappingACTCustomTriggerBell { get; set; }
        public string ColorMappingACTTimerIdle { get; set; }
        public string ColorMappingACTTimerBuild { get; set; }
        public string ColorMappingACTTimerFlash { get; set; }
        public string ColorMappingACTEnrageEmpty { get; set; }
        public string ColorMappingACTEnrageCountdown { get; set; }
        public string ColorMappingACTEnrageWarning { get; set; }
        public string ColorMappingJobPLDBase { get; set; }
        public string ColorMappingJobPLDHighlight { get; set; }
        public string ColorMappingJobMNKBase { get; set; }
        public string ColorMappingJobMNKHighlight { get; set; }
        public string ColorMappingJobWARBase { get; set; }
        public string ColorMappingJobWARHighlight { get; set; }
        public string ColorMappingJobDRGBase { get; set; }
        public string ColorMappingJobDRGHighlight { get; set; }
        public string ColorMappingJobBRDBase { get; set; }
        public string ColorMappingJobBRDHighlight { get; set; }
        public string ColorMappingJobWHMBase { get; set; }
        public string ColorMappingJobWHMHighlight { get; set; }
        public string ColorMappingJobBLMBase { get; set; }
        public string ColorMappingJobBLMHighlight { get; set; }
        public string ColorMappingJobSMNBase { get; set; }
        public string ColorMappingJobSMNHighlight { get; set; }
        public string ColorMappingJobSCHBase { get; set; }
        public string ColorMappingJobSCHHighlight { get; set; }
        public string ColorMappingJobNINBase { get; set; }
        public string ColorMappingJobNINHighlight { get; set; }
        public string ColorMappingJobMCHBase { get; set; }
        public string ColorMappingJobMCHHighlight { get; set; }
        public string ColorMappingJobDRKBase { get; set; }
        public string ColorMappingJobDRKHighlight { get; set; }
        public string ColorMappingJobASTBase { get; set; }
        public string ColorMappingJobASTHighlight { get; set; }
        public string ColorMappingJobSAMBase { get; set; }
        public string ColorMappingJobSAMHighlight { get; set; }
        public string ColorMappingJobRDMBase { get; set; }
        public string ColorMappingJobRDMHighlight { get; set; }
        public string ColorMappingJobDNCBase { get; set; }
        public string ColorMappingJobDNCHighlight { get; set; }
        public string ColorMappingJobGNBBase { get; set; }
        public string ColorMappingJobGNBHighlight { get; set; }
        public string ColorMappingJobRPRBase { get; set; }
        public string ColorMappingJobRPRHighlight { get; set; }
        public string ColorMappingJobSGEBase { get; set; }
        public string ColorMappingJobSGEHighlight { get; set; }
        public string ColorMappingJobBLUBase { get; set; }
        public string ColorMappingJobBLUHighlight { get; set; }
        public string ColorMappingJobCPTBase { get; set; }
        public string ColorMappingJobCPTHighlight { get; set; }
        public string ColorMappingJobBSMBase { get; set; }
        public string ColorMappingJobBSMHighlight { get; set; }
        public string ColorMappingJobARMBase { get; set; }
        public string ColorMappingJobARMHighlight { get; set; }
        public string ColorMappingJobGSMBase { get; set; }
        public string ColorMappingJobGSMHighlight { get; set; }
        public string ColorMappingJobLTWBase { get; set; }
        public string ColorMappingJobLTWHighlight { get; set; }
        public string ColorMappingJobWVRBase { get; set; }
        public string ColorMappingJobWVRHighlight { get; set; }
        public string ColorMappingJobALCBase { get; set; }
        public string ColorMappingJobALCHighlight { get; set; }
        public string ColorMappingJobCULBase { get; set; }
        public string ColorMappingJobCULHighlight { get; set; }
        public string ColorMappingJobMINBase { get; set; }
        public string ColorMappingJobMINHighlight { get; set; }
        public string ColorMappingJobBTNBase { get; set; }
        public string ColorMappingJobBTNHighlight { get; set; }
        public string ColorMappingJobFSHBase { get; set; }
        public string ColorMappingJobFSHHighlight { get; set; }
    }

    public class ColorMapping
    {
        public string Name { get; set; }
        public PaletteTypes Type { get; set; }
        public Color Color { get; set; }

        public ColorMapping(string _name, PaletteTypes _type, Color _color)
        {
            Name = _name;
            Type = _type;
            Color = _color;
        }
    }
}
