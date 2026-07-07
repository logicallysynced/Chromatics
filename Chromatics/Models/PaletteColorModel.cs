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
        // v1 = pre-Dawntrail.
        // v2 = Dawntrail (7.x) job-gauge additions (BRD Radiant Finale codas, BLM Paradox/Astral Soul,
        //      DRK Living Shadow, DRG Firstminds, GNB Bloodfest, MNK Beast Chakra + Nadi,
        //      NIN Kazematoi, PLD Confiteor, RDM Mana Stacks, SAM Kaeshi, SCH Dismissed Fairy,
        //      AST Astral/Umbral Draw, VPR Reawakened + Serpent Combo). NIN Huton display renamed to Kazematoi.
        // v3 = full detrimental status catalogue, focus-target HP / castbar entries, Casting Success.
        //
        // A version mismatch on load makes MigratePaletteIfNeeded re-save the
        // file, which persists every field added since the file was written
        // (new fields arrive from the C# initialisers during deserialisation
        // and would otherwise live only in memory). Newer files opened by an
        // older build still load - unknown JSON members are ignored - the old
        // build just drops the extra entries on its next save and a newer
        // build restores them from the initialisers again.
        public const string CurrentVersion = "3";
        public string version { get; set; } = CurrentVersion;

        //Chromatics
        public ColorMapping BaseColor = new("Base Color", PaletteTypes.Chromatics, Color.DodgerBlue);
        public ColorMapping HighlightColor = new("Highlight Color", PaletteTypes.Chromatics, Color.Magenta);
        public ColorMapping DeviceDisabled = new("Device Disabled Color", PaletteTypes.Chromatics, Color.Black);
        public ColorMapping MenuBase = new("Main Menu Base Color", PaletteTypes.Chromatics, Color.FromArgb(255, 0, 206, 209));
        public ColorMapping MenuHighlight1 = new("Main Menu Animation Color 1", PaletteTypes.Chromatics, Color.Gold);
        public ColorMapping MenuHighlight2 = new("Main Menu Animation Color 2", PaletteTypes.Chromatics, Color.Yellow);
        public ColorMapping MenuHighlight3 = new("Main Menu Animation Color 3", PaletteTypes.Chromatics, Color.Gold);
        public ColorMapping CutsceneBase = new("Cutscene Base Color", PaletteTypes.Chromatics, Color.DeepSkyBlue);
        public ColorMapping CutsceneHighlight1 = new("Cutscene Animation Color 1", PaletteTypes.Chromatics, Color.White);
        public ColorMapping CutsceneHighlight2 = new("Cutscene Animation Color 2", PaletteTypes.Chromatics, Color.White);
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
        public ColorMapping DamageFlashAnimation = new("Damage Flash Effect Color", PaletteTypes.PlayerStats, Color.Red);

        //Enmity/Aggro
        public ColorMapping EmnityGreen = new("Minimal Enmity", PaletteTypes.EnmityAggro, Color.Green);
        public ColorMapping EmnityYellow = new("Low Enmity", PaletteTypes.EnmityAggro, Color.Yellow);
        public ColorMapping EmnityOrange = new("High Enmity", PaletteTypes.EnmityAggro, Color.Orange);
        public ColorMapping EmnityRed = new("Top Enmity", PaletteTypes.EnmityAggro, Color.Red);
        public ColorMapping NoEmnity = new("No Enmity", PaletteTypes.EnmityAggro, Color.Black);

        //Target/Enemy
        public ColorMapping TargetCastbar = new("Target Cast Bar Charge Build", PaletteTypes.TargetEnemy, Color.Orange);
        public ColorMapping TargetCastbarEmpty = new("Target Cast Bar Charge Empty", PaletteTypes.TargetEnemy, Color.Black);
        public ColorMapping TargetHpFriendly = new("Target HP (Friendly)", PaletteTypes.TargetEnemy, Color.Lime);
        public ColorMapping TargetHpClaimed = new("Target HP (Claimed)", PaletteTypes.TargetEnemy, Color.Red);
        public ColorMapping TargetHpEmpty = new("Target HP (Empty)", PaletteTypes.TargetEnemy, Color.Black);
        public ColorMapping TargetHpIdle = new("Target HP (Idle)", PaletteTypes.TargetEnemy, Color.Yellow);
        public ColorMapping FocusTargetCastbar = new("Focus Target Cast Bar Charge Build", PaletteTypes.TargetEnemy, Color.Orange);
        public ColorMapping FocusTargetCastbarEmpty = new("Focus Target Cast Bar Charge Empty", PaletteTypes.TargetEnemy, Color.Black);
        public ColorMapping FocusTargetHpFriendly = new("Focus Target HP (Friendly)", PaletteTypes.TargetEnemy, Color.Lime);
        public ColorMapping FocusTargetHpClaimed = new("Focus Target HP (Claimed)", PaletteTypes.TargetEnemy, Color.Red);
        public ColorMapping FocusTargetHpEmpty = new("Focus Target HP (Empty)", PaletteTypes.TargetEnemy, Color.Black);
        public ColorMapping FocusTargetHpIdle = new("Focus Target HP (Idle)", PaletteTypes.TargetEnemy, Color.Yellow);

        //Status Effects
        public ColorMapping Amnesia = new("Amnesia", PaletteTypes.StatusEffects, Color.Snow);
        public ColorMapping Bind = new("Bind", PaletteTypes.StatusEffects, Color.BlueViolet);
        public ColorMapping VulnerabilityUp = new("Vulnerability Up", PaletteTypes.StatusEffects, Color.Orange);
        // Display names double as the StatusNameEnglish match keys for the
        // Status Inflicted effect - keep them identical to the in-game
        // status names.
        public ColorMapping Bleed = new("Bleeding", PaletteTypes.StatusEffects, Color.IndianRed);
        public ColorMapping Burns = new("Burns", PaletteTypes.StatusEffects, Color.OrangeRed);
        public ColorMapping DamageDown = new("Damage Down", PaletteTypes.StatusEffects, Color.PaleVioletRed);
        public ColorMapping Daze = new("Daze", PaletteTypes.StatusEffects, Color.PaleVioletRed);
        public ColorMapping Old = new("Old", PaletteTypes.StatusEffects, Color.SlateGray);
        public ColorMapping DeepFreeze = new("Deep Freeze", PaletteTypes.StatusEffects, Color.RoyalBlue);
        public ColorMapping Dropsy = new("Dropsy", PaletteTypes.StatusEffects, Color.DeepSkyBlue);
        public ColorMapping Incapacitation = new("Incapacitation", PaletteTypes.StatusEffects, Color.DarkRed);
        public ColorMapping Infirmary = new("Infirmity", PaletteTypes.StatusEffects, Color.PaleVioletRed);
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
        public ColorMapping Doom = new("Doom", PaletteTypes.StatusEffects, Color.FromArgb(153, 17, 34));
        public ColorMapping Frostbite = new("Frostbite", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 187));
        public ColorMapping Windburn = new("Windburn", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 136));
        public ColorMapping Dehydration = new("Dehydration", PaletteTypes.StatusEffects, Color.SandyBrown);
        public ColorMapping Disease = new("Disease", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping Concussion = new("Concussion", PaletteTypes.StatusEffects, Color.FromArgb(18, 10, 20));
        public ColorMapping SustainedDamage = new("Sustained Damage", PaletteTypes.StatusEffects, Color.FromArgb(136, 101, 6));
        public ColorMapping MagicVulnerabilityUp = new("Magic Vulnerability Up", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 68));
        public ColorMapping PhysicalVulnerabilityUp = new("Physical Vulnerability Up", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 153));
        public ColorMapping HealingMagicDown = new("Healing Magic Down", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping Confused = new("Confused", PaletteTypes.StatusEffects, Color.FromArgb(68, 34, 68));
        public ColorMapping Hysteria = new("Hysteria", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 0));
        public ColorMapping Seduced = new("Seduced", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 0));
        public ColorMapping Fetters = new("Fetters", PaletteTypes.StatusEffects, Color.FromArgb(187, 238, 255));
        public ColorMapping Electrocution = new("Electrocution", PaletteTypes.StatusEffects, Color.FromArgb(187, 102, 153));
        public ColorMapping Throttle = new("Throttle", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping TemporaryMisdirection = new("Temporary Misdirection", PaletteTypes.StatusEffects, Color.FromArgb(170, 204, 255));
        public ColorMapping ForcedMarch = new("Forced March", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping DownForTheCount = new("Down for the Count", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 17));
        public ColorMapping BrinkOfDeath = new("Brink of Death", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping Weakness = new("Weakness", PaletteTypes.StatusEffects, Color.FromArgb(68, 79, 39));
        public ColorMapping Nausea = new("Nausea", PaletteTypes.StatusEffects, Color.FromArgb(187, 204, 221));
        public ColorMapping AccuracyDown = new("Accuracy Down", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping Blind = new("Blind", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 68));
        public ColorMapping BrushWithDeath = new("Brush with Death", PaletteTypes.StatusEffects, Color.FromArgb(187, 204, 204));
        public ColorMapping Charm = new("Charm", PaletteTypes.StatusEffects, Color.Orchid);
        public ColorMapping Seduce = new("Seduce", PaletteTypes.StatusEffects, Color.MediumVioletRed);
        public ColorMapping Pacification = new("Pacification", PaletteTypes.StatusEffects, Color.FromArgb(136, 119, 119));
        public ColorMapping ReducedImmunity = new("Reduced Immunity", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping Sludge = new("Sludge", PaletteTypes.StatusEffects, Color.FromArgb(170, 119, 34));
        public ColorMapping FireResistanceDown = new("Fire Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 17));
        public ColorMapping IceResistanceDown = new("Ice Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 187));
        public ColorMapping WindResistanceDown = new("Wind Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 51));
        public ColorMapping EarthResistanceDown = new("Earth Resistance Down", PaletteTypes.StatusEffects, Color.Sienna);
        public ColorMapping LightningResistanceDown = new("Lightning Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping WaterResistanceDown = new("Water Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 17));
        public ColorMapping BluntResistanceDown = new("Blunt Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 153));
        public ColorMapping PiercingResistanceDown = new("Piercing Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 153));
        public ColorMapping SlashingResistanceDown = new("Slashing Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 0));

        // Every remaining detrimental status from the game data (Status
        // sheet, StatusCategory 2), generated from XIVAPI and deduplicated
        // by name. Default colours are assigned per-name from a fixed
        // debuff colour set so each status keeps a stable colour across
        // builds. Display names double as StatusNameEnglish match keys for
        // the Status Inflicted effect - do not reword them.
        public ColorMapping StatusTPBleed = new("TP Bleed", PaletteTypes.StatusEffects, Color.FromArgb(119, 34, 34));
        public ColorMapping StatusHPPenalty = new("HP Penalty", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusMPPenalty = new("MP Penalty", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusAttackDown = new("Attack Down", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusDefenseDown = new("Defense Down", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 153));
        public ColorMapping StatusEvasionDown = new("Evasion Down", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusAttackMagicPotencyDown = new("Attack Magic Potency Down", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping StatusHealingPotencyDown = new("Healing Potency Down", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusMagicDefenseDown = new("Magic Defense Down", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 68));
        public ColorMapping StatusStrengthDown = new("Strength Down", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 0));
        public ColorMapping StatusVitalityDown = new("Vitality Down", PaletteTypes.StatusEffects, Color.FromArgb(153, 221, 17));
        public ColorMapping StatusPhysicalDamageDown = new("Physical Damage Down", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusMagicDamageDown = new("Magic Damage Down", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping StatusTerror = new("Terror", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 0));
        public ColorMapping StatusHolmgang = new("Holmgang", PaletteTypes.StatusEffects, Color.FromArgb(204, 102, 68));
        public ColorMapping StatusDragonKick = new("Dragon Kick", PaletteTypes.StatusEffects, Color.FromArgb(238, 0, 0));
        public ColorMapping StatusTouchOfDeath = new("Touch of Death", PaletteTypes.StatusEffects, Color.FromArgb(238, 187, 153));
        public ColorMapping StatusChaosThrust = new("Chaos Thrust", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 255));
        public ColorMapping StatusPhlebotomize = new("Phlebotomize", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 170));
        public ColorMapping StatusDisembowel = new("Disembowel", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 136));
        public ColorMapping StatusVenomousBite = new("Venomous Bite", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 153));
        public ColorMapping StatusWindbite = new("Windbite", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 204));
        public ColorMapping StatusFoeRequiem = new("Foe Requiem", PaletteTypes.StatusEffects, Color.FromArgb(102, 153, 153));
        public ColorMapping StatusAero = new("Aero", PaletteTypes.StatusEffects, Color.FromArgb(68, 102, 68));
        public ColorMapping StatusAeroII = new("Aero II", PaletteTypes.StatusEffects, Color.FromArgb(204, 221, 153));
        public ColorMapping StatusThunder = new("Thunder", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 136));
        public ColorMapping StatusThunderII = new("Thunder II", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusThunderIII = new("Thunder III", PaletteTypes.StatusEffects, Color.FromArgb(204, 221, 255));
        public ColorMapping StatusEkpyrosis = new("Ekpyrosis", PaletteTypes.StatusEffects, Color.FromArgb(34, 170, 255));
        public ColorMapping StatusBio = new("Bio", PaletteTypes.StatusEffects, Color.FromArgb(187, 102, 136));
        public ColorMapping StatusMiasma = new("Miasma", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 17));
        public ColorMapping StatusVirus = new("Virus", PaletteTypes.StatusEffects, Color.FromArgb(0, 17, 0));
        public ColorMapping StatusFever = new("Fever", PaletteTypes.StatusEffects, Color.FromArgb(0, 17, 0));
        public ColorMapping StatusEyeForAnEye = new("Eye for an Eye", PaletteTypes.StatusEffects, Color.FromArgb(69, 34, 20));
        public ColorMapping StatusMiasmaII = new("Miasma II", PaletteTypes.StatusEffects, Color.FromArgb(5, 0, 5));
        public ColorMapping StatusBioII = new("Bio II", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 85));
        public ColorMapping StatusMalady = new("Malady", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 255));
        public ColorMapping StatusTrueSight = new("True Sight", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusChocoBeak = new("Choco Beak", PaletteTypes.StatusEffects, Color.FromArgb(50, 25, 48));
        public ColorMapping StatusFracture = new("Fracture", PaletteTypes.StatusEffects, Color.FromArgb(187, 153, 153));
        public ColorMapping StatusDemolish = new("Demolish", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 0));
        public ColorMapping StatusRainOfDeath = new("Rain of Death", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 221));
        public ColorMapping StatusCircleOfScorn = new("Circle of Scorn", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusFleshWound = new("Flesh Wound", PaletteTypes.StatusEffects, Color.FromArgb(204, 102, 187));
        public ColorMapping StatusStabWound = new("Stab Wound", PaletteTypes.StatusEffects, Color.FromArgb(50, 25, 48));
        public ColorMapping StatusPoison1 = new("Poison +1", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 68));
        public ColorMapping StatusManaModulation = new("Mana Modulation", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping StatusGoldbile = new("Goldbile", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 204));
        public ColorMapping StatusGoldLung = new("Gold Lung", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 204));
        public ColorMapping StatusBurrs = new("Burrs", PaletteTypes.StatusEffects, Color.FromArgb(153, 34, 34));
        public ColorMapping StatusInferno = new("Inferno", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 204));
        public ColorMapping StatusGreenwrath = new("Greenwrath", PaletteTypes.StatusEffects, Color.FromArgb(85, 17, 34));
        public ColorMapping StatusAllaganRot = new("Allagan Rot", PaletteTypes.StatusEffects, Color.FromArgb(136, 68, 51));
        public ColorMapping StatusAllaganImmunity = new("Allagan Immunity", PaletteTypes.StatusEffects, Color.FromArgb(85, 85, 68));
        public ColorMapping StatusFirestream = new("Firestream", PaletteTypes.StatusEffects, Color.FromArgb(187, 68, 34));
        public ColorMapping StatusNeurolink = new("Neurolink", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusDisseminate = new("Disseminate", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusSpjot = new("Spjot", PaletteTypes.StatusEffects, Color.FromArgb(85, 17, 34));
        public ColorMapping StatusViscousAetheroplasm = new("Viscous Aetheroplasm", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 51));
        public ColorMapping StatusSirenSong = new("Siren Song", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 221));
        public ColorMapping StatusZombification = new("Zombification", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping StatusBlight = new("Blight", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 51));
        public ColorMapping StatusCorruptedCrystal = new("Corrupted Crystal", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 102));
        public ColorMapping StatusSuppuration = new("Suppuration", PaletteTypes.StatusEffects, Color.FromArgb(187, 17, 51));
        public ColorMapping StatusSearingWind = new("Searing Wind", PaletteTypes.StatusEffects, Color.FromArgb(255, 170, 119));
        public ColorMapping StatusInfernalFetters = new("Infernal Fetters", PaletteTypes.StatusEffects, Color.FromArgb(187, 119, 0));
        public ColorMapping StatusDeathThroes = new("Death Throes", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusThermalLow = new("Thermal Low", PaletteTypes.StatusEffects, Color.FromArgb(51, 68, 85));
        public ColorMapping StatusFoolSTumble = new("Fool's Tumble", PaletteTypes.StatusEffects, Color.FromArgb(170, 136, 119));
        public ColorMapping StatusSkewer = new("Skewer", PaletteTypes.StatusEffects, Color.FromArgb(51, 51, 68));
        public ColorMapping StatusAstralRealignment = new("Astral Realignment", PaletteTypes.StatusEffects, Color.FromArgb(103, 119, 121));
        public ColorMapping StatusCorporealReturn = new("Corporeal Return", PaletteTypes.StatusEffects, Color.FromArgb(153, 136, 17));
        public ColorMapping StatusCharge = new("Charge", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 187));
        public ColorMapping StatusSeized = new("Seized", PaletteTypes.StatusEffects, Color.FromArgb(102, 102, 68));
        public ColorMapping StatusThrownForALoop = new("Thrown for a Loop", PaletteTypes.StatusEffects, Color.FromArgb(238, 136, 136));
        public ColorMapping StatusBewildered = new("Bewildered", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusDustPoisoning = new("Dust Poisoning", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 68));
        public ColorMapping StatusStormSPath = new("Storm's Path", PaletteTypes.StatusEffects, Color.FromArgb(68, 153, 136));
        public ColorMapping StatusMistyVeil = new("Misty Veil", PaletteTypes.StatusEffects, Color.FromArgb(136, 102, 68));
        public ColorMapping StatusPrey = new("Prey", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusDevoured = new("Devoured", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 0));
        public ColorMapping StatusNightmare = new("Nightmare", PaletteTypes.StatusEffects, Color.FromArgb(153, 153, 136));
        public ColorMapping StatusDiabolicCurse = new("Diabolic Curse", PaletteTypes.StatusEffects, Color.FromArgb(119, 85, 102));
        public ColorMapping StatusEerieAir = new("Eerie Air", PaletteTypes.StatusEffects, Color.FromArgb(119, 17, 17));
        public ColorMapping StatusSlow = new("Slow+", PaletteTypes.StatusEffects, Color.FromArgb(68, 34, 34));
        public ColorMapping StatusScaleFlakes = new("Scale Flakes", PaletteTypes.StatusEffects, Color.FromArgb(136, 17, 119));
        public ColorMapping StatusBrinyMirror = new("Briny Mirror", PaletteTypes.StatusEffects, Color.FromArgb(187, 85, 85));
        public ColorMapping StatusBrinyVeil = new("Briny Veil", PaletteTypes.StatusEffects, Color.FromArgb(85, 153, 187));
        public ColorMapping StatusAbsoluteBind = new("Absolute Bind", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 221));
        public ColorMapping StatusDemonEye = new("Demon Eye", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping StatusBriar = new("Briar", PaletteTypes.StatusEffects, Color.FromArgb(170, 51, 34));
        public ColorMapping StatusStoneCurse = new("Stone Curse", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 187));
        public ColorMapping StatusMinimum = new("Minimum", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 17));
        public ColorMapping StatusToad = new("Toad", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusThornyVine = new("Thorny Vine", PaletteTypes.StatusEffects, Color.FromArgb(204, 170, 17));
        public ColorMapping StatusHoneyGlazed = new("Honey-glazed", PaletteTypes.StatusEffects, Color.FromArgb(170, 119, 34));
        public ColorMapping StatusPotentAcid = new("Potent Acid", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 204));
        public ColorMapping StatusSwarmed = new("Swarmed", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 221));
        public ColorMapping StatusStung = new("Stung", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 68));
        public ColorMapping StatusCursedVoice = new("Cursed Voice", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 17));
        public ColorMapping StatusCursedShriek = new("Cursed Shriek", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 17));
        public ColorMapping StatusAllaganVenom = new("Allagan Venom", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusAllaganField = new("Allagan Field", PaletteTypes.StatusEffects, Color.FromArgb(8, 7, 0));
        public ColorMapping StatusLanguishing = new("Languishing", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBind = new("Bind+", PaletteTypes.StatusEffects, Color.FromArgb(85, 17, 17));
        public ColorMapping StatusRavenBlight = new("Raven Blight", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 204));
        public ColorMapping StatusGarroteTwist = new("Garrote Twist", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 85));
        public ColorMapping StatusGarrote = new("Garrote", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 34));
        public ColorMapping StatusFirescorched = new("Firescorched", PaletteTypes.StatusEffects, Color.FromArgb(187, 68, 34));
        public ColorMapping StatusIcebitten = new("Icebitten", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 187));
        public ColorMapping StatusThunderstruck = new("Thunderstruck", PaletteTypes.StatusEffects, Color.FromArgb(221, 153, 187));
        public ColorMapping StatusVoidbound = new("Voidbound", PaletteTypes.StatusEffects, Color.FromArgb(170, 136, 170));
        public ColorMapping StatusMoglightResistanceDown = new("Moglight Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(187, 170, 170));
        public ColorMapping StatusMogdarkResistanceDown = new("Mogdark Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(221, 170, 153));
        public ColorMapping StatusBemoggled = new("Bemoggled", PaletteTypes.StatusEffects, Color.FromArgb(204, 85, 17));
        public ColorMapping StatusSlipperyPrey = new("Slippery Prey", PaletteTypes.StatusEffects, Color.FromArgb(136, 204, 255));
        public ColorMapping StatusGloam = new("Gloam", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 68));
        public ColorMapping StatusInk = new("Ink", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 85));
        public ColorMapping StatusWateryGrave = new("Watery Grave", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusDancingEdge = new("Dancing Edge", PaletteTypes.StatusEffects, Color.FromArgb(85, 17, 34));
        public ColorMapping StatusMutilation = new("Mutilation", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 0));
        public ColorMapping StatusDotonHeavy = new("Doton Heavy", PaletteTypes.StatusEffects, Color.FromArgb(121, 121, 116));
        public ColorMapping StatusVertigo = new("Vertigo", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 51));
        public ColorMapping StatusShadowFang = new("Shadow Fang", PaletteTypes.StatusEffects, Color.FromArgb(119, 255, 255));
        public ColorMapping StatusAetherochemicalBomb = new("Aetherochemical Bomb", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 17));
        public ColorMapping StatusFireToad = new("Fire Toad", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 170));
        public ColorMapping StatusElectroconductivity = new("Electroconductivity", PaletteTypes.StatusEffects, Color.FromArgb(221, 255, 255));
        public ColorMapping StatusStaticCondensation = new("Static Condensation", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 119));
        public ColorMapping StatusCausality = new("Causality", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 0));
        public ColorMapping StatusThunderclap = new("Thunderclap", PaletteTypes.StatusEffects, Color.FromArgb(170, 153, 17));
        public ColorMapping StatusChaos = new("Chaos", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusSurgeProtection = new("Surge Protection", PaletteTypes.StatusEffects, Color.FromArgb(68, 204, 255));
        public ColorMapping StatusEnervation = new("Enervation", PaletteTypes.StatusEffects, Color.FromArgb(238, 170, 0));
        public ColorMapping StatusSixFulmsUnder = new("Six Fulms Under", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping StatusSlime = new("Slime", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 170));
        public ColorMapping StatusErraticBlaster = new("Erratic Blaster", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusStaticCharge = new("Static Charge", PaletteTypes.StatusEffects, Color.FromArgb(221, 238, 238));
        public ColorMapping StatusInTheHeadlights = new("In the Headlights", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping StatusAetherochemicalNanospores = new("Aetherochemical Nanospores α", PaletteTypes.StatusEffects, Color.FromArgb(34, 102, 51));
        public ColorMapping StatusAetherochemicalNanospores2 = new("Aetherochemical Nanospores β", PaletteTypes.StatusEffects, Color.FromArgb(34, 119, 136));
        public ColorMapping StatusForkedLightning = new("Forked Lightning", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusRevelationResistanceDown = new("Revelation Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 34));
        public ColorMapping StatusChainOfPurgatory = new("Chain of Purgatory", PaletteTypes.StatusEffects, Color.FromArgb(17, 51, 0));
        public ColorMapping StatusArmOfPurgatory = new("Arm of Purgatory", PaletteTypes.StatusEffects, Color.FromArgb(102, 34, 85));
        public ColorMapping StatusBluefire = new("Bluefire", PaletteTypes.StatusEffects, Color.FromArgb(85, 153, 221));
        public ColorMapping StatusRiseOfThePhoenix = new("Rise of the Phoenix", PaletteTypes.StatusEffects, Color.FromArgb(221, 85, 34));
        public ColorMapping StatusCloakOfDeath = new("Cloak of Death", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 221));
        public ColorMapping StatusSuffocatedWill = new("Suffocated Will", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusFlareDampening = new("Flare Dampening", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusCurseOfTheMummy = new("Curse of the Mummy", PaletteTypes.StatusEffects, Color.FromArgb(136, 136, 119));
        public ColorMapping StatusMummification = new("Mummification", PaletteTypes.StatusEffects, Color.FromArgb(221, 136, 119));
        public ColorMapping StatusThinIce = new("Thin Ice", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 221));
        public ColorMapping StatusFrozen = new("Frozen", PaletteTypes.StatusEffects, Color.FromArgb(51, 85, 136));
        public ColorMapping StatusSnowball = new("Snowball", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 255));
        public ColorMapping StatusWetPlate = new("Wet Plate", PaletteTypes.StatusEffects, Color.FromArgb(119, 153, 68));
        public ColorMapping StatusImp = new("Imp", PaletteTypes.StatusEffects, Color.FromArgb(153, 187, 221));
        public ColorMapping StatusRottingLungs = new("Rotting Lungs", PaletteTypes.StatusEffects, Color.FromArgb(153, 119, 204));
        public ColorMapping StatusOutOfTheAction = new("Out of the Action", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 68));
        public ColorMapping StatusFrenzied = new("Frenzied", PaletteTypes.StatusEffects, Color.FromArgb(136, 17, 0));
        public ColorMapping StatusLamed = new("Lamed", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 0));
        public ColorMapping StatusSilenced = new("Silenced", PaletteTypes.StatusEffects, Color.FromArgb(119, 85, 85));
        public ColorMapping StatusDistracted = new("Distracted", PaletteTypes.StatusEffects, Color.FromArgb(136, 85, 85));
        public ColorMapping StatusBrandOfTheSullen = new("Brand of the Sullen", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 187));
        public ColorMapping StatusBrandOfTheIreful = new("Brand of the Ireful", PaletteTypes.StatusEffects, Color.FromArgb(221, 187, 187));
        public ColorMapping StatusPyretic = new("Pyretic", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 17));
        public ColorMapping StatusChicken = new("Chicken", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusDigesting = new("Digesting", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusAbandonment = new("Abandonment", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 0));
        public ColorMapping StatusAtrophy = new("Atrophy", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 187));
        public ColorMapping StatusExtend = new("Extend", PaletteTypes.StatusEffects, Color.FromArgb(51, 51, 51));
        public ColorMapping StatusRedLight = new("Red Light", PaletteTypes.StatusEffects, Color.FromArgb(153, 153, 187));
        public ColorMapping StatusNanoparticles = new("Nanoparticles", PaletteTypes.StatusEffects, Color.FromArgb(102, 102, 0));
        public ColorMapping StatusResin = new("Resin", PaletteTypes.StatusEffects, Color.FromArgb(121, 121, 117));
        public ColorMapping StatusConcentratedPoison = new("Concentrated Poison", PaletteTypes.StatusEffects, Color.FromArgb(85, 0, 17));
        public ColorMapping StatusMarkedForVulnerabilityUp = new("Marked for Vulnerability Up", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 51));
        public ColorMapping StatusMarkedForDamageDown = new("Marked for Damage Down", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 85));
        public ColorMapping StatusMarkedForHealingMagicDown = new("Marked for Healing Magic Down", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusHardMarked = new("Hard Marked", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusPositiveCharge = new("Positive Charge", PaletteTypes.StatusEffects, Color.FromArgb(187, 0, 0));
        public ColorMapping StatusNegativeCharge = new("Negative Charge", PaletteTypes.StatusEffects, Color.FromArgb(17, 136, 221));
        public ColorMapping StatusBattleEfficiencyDown = new("Battle Efficiency Down", PaletteTypes.StatusEffects, Color.SlateBlue);
        public ColorMapping StatusBloated = new("Bloated", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusDraconianGaze = new("Draconian Gaze", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusLuminousAetheroplasm = new("Luminous Aetheroplasm", PaletteTypes.StatusEffects, Color.FromArgb(221, 255, 255));
        public ColorMapping StatusDecreeNisiA = new("Decree Nisi A", PaletteTypes.StatusEffects, Color.FromArgb(187, 204, 221));
        public ColorMapping StatusDecreeNisiB = new("Decree Nisi B", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping StatusHeavyFeet = new("Heavy Feet", PaletteTypes.StatusEffects, Color.FromArgb(121, 121, 117));
        public ColorMapping StatusTemporaryInsanity = new("Temporary Insanity", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 68));
        public ColorMapping StatusSevereDamage = new("Severe Damage", PaletteTypes.StatusEffects, Color.FromArgb(17, 34, 51));
        public ColorMapping StatusStaggered = new("Staggered", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 17));
        public ColorMapping StatusTurbulence = new("Turbulence", PaletteTypes.StatusEffects, Color.FromArgb(51, 68, 85));
        public ColorMapping StatusWillOfTheWind = new("Will of the Wind", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping StatusWillOfTheWater = new("Will of the Water", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 255));
        public ColorMapping StatusGoringBlade = new("Goring Blade", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 221));
        public ColorMapping StatusScourge = new("Scourge", PaletteTypes.StatusEffects, Color.FromArgb(204, 34, 85));
        public ColorMapping StatusDelirium = new("Delirium", PaletteTypes.StatusEffects, Color.FromArgb(34, 85, 153));
        public ColorMapping StatusAnotherVictim = new("Another Victim", PaletteTypes.StatusEffects, Color.FromArgb(238, 153, 238));
        public ColorMapping StatusReprisal = new("Reprisal", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 170));
        public ColorMapping StatusInefficientHooking = new("Inefficient Hooking", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 136));
        public ColorMapping StatusBurningChains = new("Burning Chains", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 0));
        public ColorMapping StatusOutOfBody = new("Out of Body", PaletteTypes.StatusEffects, Color.FromArgb(153, 85, 102));
        public ColorMapping StatusAetherSickness = new("Aether Sickness", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 187));
        public ColorMapping StatusVoidblood = new("Voidblood", PaletteTypes.StatusEffects, Color.FromArgb(153, 17, 51));
        public ColorMapping StatusNymianPlague = new("Nymian Plague", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 51));
        public ColorMapping StatusAeroIII = new("Aero III", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping StatusWalkingDead = new("Walking Dead", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 204));
        public ColorMapping StatusCombust = new("Combust", PaletteTypes.StatusEffects, Color.FromArgb(119, 221, 255));
        public ColorMapping StatusCombustII = new("Combust II", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusDisabled = new("Disabled", PaletteTypes.StatusEffects, Color.FromArgb(238, 136, 102));
        public ColorMapping StatusLeadShot = new("Lead Shot", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 136));
        public ColorMapping StatusRentMind = new("Rent Mind", PaletteTypes.StatusEffects, Color.FromArgb(204, 102, 170));
        public ColorMapping StatusDismantled = new("Dismantled", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusWildfire = new("Wildfire", PaletteTypes.StatusEffects, Color.FromArgb(119, 119, 119));
        public ColorMapping StatusCarnalChill = new("Carnal Chill", PaletteTypes.StatusEffects, Color.FromArgb(51, 119, 153));
        public ColorMapping StatusTemporalDisplacement = new("Temporal Displacement", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 221));
        public ColorMapping StatusNectar = new("Nectar", PaletteTypes.StatusEffects, Color.FromArgb(170, 119, 34));
        public ColorMapping StatusQuarantine = new("Quarantine", PaletteTypes.StatusEffects, Color.FromArgb(102, 255, 255));
        public ColorMapping StatusUnwillingHost = new("Unwilling Host", PaletteTypes.StatusEffects, Color.FromArgb(136, 34, 51));
        public ColorMapping StatusDigestiveEnzymes = new("Digestive Enzymes", PaletteTypes.StatusEffects, Color.FromArgb(204, 102, 102));
        public ColorMapping StatusATKDown = new("ATK Down", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusDEFDown = new("DEF Down", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 153));
        public ColorMapping StatusSPDDown = new("SPD Down", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusCritterVulnerability = new("Critter Vulnerability", PaletteTypes.StatusEffects, Color.FromArgb(170, 204, 119));
        public ColorMapping StatusMonsterVulnerability = new("Monster Vulnerability", PaletteTypes.StatusEffects, Color.FromArgb(119, 51, 51));
        public ColorMapping StatusPoppetVulnerability = new("Poppet Vulnerability", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 102));
        public ColorMapping StatusForcedWithdrawal = new("Forced Withdrawal", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 170));
        public ColorMapping StatusDamageOverTime = new("Damage Over Time", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusMarkedForCulling = new("Marked for Culling", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusHeadache = new("Headache", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusGoblixerOvergulp = new("Goblixer Overgulp", PaletteTypes.StatusEffects, Color.FromArgb(255, 136, 136));
        public ColorMapping StatusGoblixerGrumblygut = new("Goblixer Grumblygut", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 170));
        public ColorMapping StatusAntiCoagulant = new("Anti-coagulant", PaletteTypes.StatusEffects, Color.FromArgb(153, 17, 34));
        public ColorMapping StatusForceAgainstMight = new("Force Against Might", PaletteTypes.StatusEffects, Color.FromArgb(153, 85, 0));
        public ColorMapping StatusForceAgainstMagic = new("Force Against Magic", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusRoadToToad = new("Road to Toad", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusLowArithmeticks = new("Low Arithmeticks", PaletteTypes.StatusEffects, Color.FromArgb(204, 85, 17));
        public ColorMapping StatusHighArithmeticks = new("High Arithmeticks", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusCompressedWater = new("Compressed Water", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 255));
        public ColorMapping StatusCompressedLightning = new("Compressed Lightning", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 204));
        public ColorMapping StatusWaterResistanceDownII = new("Water Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(170, 136, 170));
        public ColorMapping StatusLightningResistanceDownII = new("Lightning Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(221, 153, 187));
        public ColorMapping StatusFinalPunishment = new("Final Punishment", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 221));
        public ColorMapping StatusFinalDecreeNisiA = new("Final Decree Nisi A", PaletteTypes.StatusEffects, Color.FromArgb(204, 221, 221));
        public ColorMapping StatusFinalDecreeNisiB = new("Final Decree Nisi B", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 204));
        public ColorMapping StatusFinalJudgmentMaxHP = new("Final Judgment: Max HP", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 204));
        public ColorMapping StatusFinalJudgmentMinHP = new("Final Judgment: Min HP", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 204));
        public ColorMapping StatusFinalJudgmentPenaltyI = new("Final Judgment: Penalty I", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusFinalJudgmentPenaltyII = new("Final Judgment: Penalty II", PaletteTypes.StatusEffects, Color.FromArgb(170, 221, 153));
        public ColorMapping StatusFinalJudgmentPenaltyIII = new("Final Judgment: Penalty III", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusFinalJudgmentDecreeNisiA = new("Final Judgment: Decree Nisi A", PaletteTypes.StatusEffects, Color.FromArgb(204, 221, 221));
        public ColorMapping StatusFinalJudgmentDecreeNisiB = new("Final Judgment: Decree Nisi B", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 204));
        public ColorMapping StatusFinalFlight = new("Final Flight", PaletteTypes.StatusEffects, Color.FromArgb(153, 153, 102));
        public ColorMapping StatusGradualZombification = new("Gradual Zombification", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 17));
        public ColorMapping StatusWindResistanceDownII = new("Wind Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 136));
        public ColorMapping StatusEarthResistanceDownII = new("Earth Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(187, 136, 51));
        public ColorMapping StatusHeavyMedal = new("Heavy Medal", PaletteTypes.StatusEffects, Color.FromArgb(153, 119, 68));
        public ColorMapping StatusOffBalance = new("Off-balance", PaletteTypes.StatusEffects, Color.FromArgb(153, 136, 136));
        public ColorMapping StatusBrandOfTheFallen = new("Brand of the Fallen", PaletteTypes.StatusEffects, Color.FromArgb(170, 153, 102));
        public ColorMapping StatusBitterHate = new("Bitter Hate", PaletteTypes.StatusEffects, Color.FromArgb(170, 102, 0));
        public ColorMapping StatusDirtyVenom = new("Dirty Venom", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 68));
        public ColorMapping StatusAssimilation = new("Assimilation", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 170));
        public ColorMapping StatusAssimilated = new("Assimilated", PaletteTypes.StatusEffects, Color.FromArgb(85, 17, 17));
        public ColorMapping StatusAccelerationBomb = new("Acceleration Bomb", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusDigestiveFluid = new("Digestive Fluid", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 170));
        public ColorMapping StatusLightningChain = new("Lightning Chain", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusAccursedPox = new("Accursed Pox", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 204));
        public ColorMapping StatusItemPenalty = new("Item Penalty", PaletteTypes.StatusEffects, Color.FromArgb(136, 102, 85));
        public ColorMapping StatusSprintPenalty = new("Sprint Penalty", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 34));
        public ColorMapping StatusKnockbackPenalty = new("Knockback Penalty", PaletteTypes.StatusEffects, Color.FromArgb(153, 68, 51));
        public ColorMapping StatusAutoHealPenalty = new("Auto-heal Penalty", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 17));
        public ColorMapping StatusAetherialSurge = new("Aetherial Surge", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusPumpkin = new("Pumpkin", PaletteTypes.StatusEffects, Color.FromArgb(238, 119, 51));
        public ColorMapping StatusClawbound = new("Clawbound", PaletteTypes.StatusEffects, Color.FromArgb(119, 34, 51));
        public ColorMapping StatusFangbound = new("Fangbound", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 204));
        public ColorMapping StatusEternalDoom = new("Eternal Doom", PaletteTypes.StatusEffects, Color.FromArgb(85, 85, 85));
        public ColorMapping StatusDefamation = new("Defamation", PaletteTypes.StatusEffects, Color.FromArgb(119, 51, 170));
        public ColorMapping StatusAggravatedAssault = new("Aggravated Assault", PaletteTypes.StatusEffects, Color.FromArgb(187, 170, 119));
        public ColorMapping StatusSharedSentence = new("Shared Sentence", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 136));
        public ColorMapping StatusHouseArrest = new("House Arrest", PaletteTypes.StatusEffects, Color.FromArgb(102, 51, 0));
        public ColorMapping StatusRestrainingOrder = new("Restraining Order", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 204));
        public ColorMapping StatusMainHullReassembly = new("Main Hull Reassembly", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 187));
        public ColorMapping StatusRightArmReassembly = new("Right Arm Reassembly", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 187));
        public ColorMapping StatusLeftArmReassembly = new("Left Arm Reassembly", PaletteTypes.StatusEffects, Color.FromArgb(153, 153, 153));
        public ColorMapping StatusExtremeCaution = new("Extreme Caution", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusSunseal = new("Sunseal", PaletteTypes.StatusEffects, Color.FromArgb(238, 187, 68));
        public ColorMapping StatusMoonseal = new("Moonseal", PaletteTypes.StatusEffects, Color.FromArgb(170, 221, 255));
        public ColorMapping StatusFireResistanceDownII = new("Fire Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(187, 68, 34));
        public ColorMapping StatusInfiniteFire = new("Infinite Fire", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 187));
        public ColorMapping StatusInfiniteIce = new("Infinite Ice", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 255));
        public ColorMapping StatusShadowLinks = new("Shadow Links", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 17));
        public ColorMapping StatusInfiniteAnguish = new("Infinite Anguish", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusEnfeebled = new("Enfeebled", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 204));
        public ColorMapping StatusFeint = new("Feint", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 68));
        public ColorMapping StatusCausticBite = new("Caustic Bite", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 187));
        public ColorMapping StatusStormbite = new("Stormbite", PaletteTypes.StatusEffects, Color.FromArgb(187, 153, 136));
        public ColorMapping StatusAddle = new("Addle", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 34));
        public ColorMapping StatusThunderIV = new("Thunder IV", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBioIII = new("Bio III", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusMiasmaIII = new("Miasma III", PaletteTypes.StatusEffects, Color.FromArgb(68, 68, 68));
        public ColorMapping StatusLoadBearing = new("Load-bearing", PaletteTypes.StatusEffects, Color.FromArgb(255, 136, 34));
        public ColorMapping StatusChainStratagem = new("Chain Stratagem", PaletteTypes.StatusEffects, Color.FromArgb(68, 85, 136));
        public ColorMapping StatusYukikaze = new("Yukikaze", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusHiganbana = new("Higanbana", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 153));
        public ColorMapping StatusMonomachy = new("Monomachy", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 51));
        public ColorMapping StatusTurretReset = new("Turret Reset", PaletteTypes.StatusEffects, Color.FromArgb(28, 29, 34));
        public ColorMapping StatusWounded = new("Wounded", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 0));
        public ColorMapping StatusChurning = new("Churning", PaletteTypes.StatusEffects, Color.FromArgb(34, 85, 136));
        public ColorMapping StatusClashing = new("Clashing", PaletteTypes.StatusEffects, Color.FromArgb(238, 85, 102));
        public ColorMapping StatusSlashingResistanceDownII = new("Slashing Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(102, 17, 17));
        public ColorMapping StatusSinking = new("Sinking", PaletteTypes.StatusEffects, Color.FromArgb(68, 0, 51));
        public ColorMapping StatusBardamSPrice = new("Bardam's Price", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 0));
        public ColorMapping StatusRuination = new("Ruination", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusPiggy = new("Piggy", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusForwardMarch = new("Forward March", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping StatusAboutFace = new("About Face", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 187));
        public ColorMapping StatusLeftFace = new("Left Face", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping StatusRightFace = new("Right Face", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping StatusSoleSurvivor = new("Sole Survivor", PaletteTypes.StatusEffects, Color.FromArgb(238, 153, 238));
        public ColorMapping StatusAssassinated = new("Assassinated", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 0));
        public ColorMapping StatusWithering = new("Withering", PaletteTypes.StatusEffects, Color.FromArgb(34, 119, 136));
        public ColorMapping StatusGravityFlip = new("Gravity Flip", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 34));
        public ColorMapping StatusElevated = new("Elevated", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusGradualPetrification = new("Gradual Petrification", PaletteTypes.StatusEffects, Color.FromArgb(85, 85, 85));
        public ColorMapping StatusUnstableGravity = new("Unstable Gravity", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 119));
        public ColorMapping StatusRageOfHalone = new("Rage of Halone", PaletteTypes.StatusEffects, Color.FromArgb(33, 26, 21));
        public ColorMapping StatusButcherSBlock = new("Butcher's Block", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 187));
        public ColorMapping StatusPowerSlash = new("Power Slash", PaletteTypes.StatusEffects, Color.FromArgb(255, 119, 153));
        public ColorMapping StatusImmaterialized = new("Immaterialized", PaletteTypes.StatusEffects, Color.FromArgb(153, 170, 187));
        public ColorMapping StatusTargetRight = new("Target Right", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusTargetLeft = new("Target Left", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusLifeDrain = new("Life Drain", PaletteTypes.StatusEffects, Color.FromArgb(136, 85, 204));
        public ColorMapping StatusAlmagest = new("Almagest", PaletteTypes.StatusEffects, Color.FromArgb(34, 85, 102));
        public ColorMapping StatusWhiteWound = new("White Wound", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 238));
        public ColorMapping StatusBlackWound = new("Black Wound", PaletteTypes.StatusEffects, Color.FromArgb(187, 255, 255));
        public ColorMapping StatusBeyondDeath = new("Beyond Death", PaletteTypes.StatusEffects, Color.FromArgb(68, 34, 85));
        public ColorMapping StatusAshen = new("Ashen", PaletteTypes.StatusEffects, Color.FromArgb(68, 102, 119));
        public ColorMapping StatusLimp = new("Limp", PaletteTypes.StatusEffects, Color.FromArgb(187, 85, 85));
        public ColorMapping StatusCometeor = new("Cometeor", PaletteTypes.StatusEffects, Color.FromArgb(238, 153, 102));
        public ColorMapping StatusTerminalVelocity = new("Terminal Velocity", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 187));
        public ColorMapping StatusGrounded = new("Grounded", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 51));
        public ColorMapping StatusTheWormSCurse = new("The Worm's Curse", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 85));
        public ColorMapping StatusCraven = new("Craven", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusTransfiguration = new("Transfiguration", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusDivineCommandmentFlee = new("Divine Commandment: Flee", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 102));
        public ColorMapping StatusDivineCommandmentTurn = new("Divine Commandment: Turn", PaletteTypes.StatusEffects, Color.FromArgb(255, 51, 0));
        public ColorMapping StatusUnnerved = new("Unnerved", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping StatusDrenched = new("Drenched", PaletteTypes.StatusEffects, Color.FromArgb(51, 153, 255));
        public ColorMapping StatusBreathless = new("Breathless", PaletteTypes.StatusEffects, Color.FromArgb(182, 180, 187));
        public ColorMapping StatusManaHypersensitivity = new("Mana Hypersensitivity", PaletteTypes.StatusEffects, Color.FromArgb(153, 204, 170));
        public ColorMapping StatusPiercingResistanceDownII = new("Piercing Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 153));
        public ColorMapping StatusClockwork = new("Clockwork", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 187));
        public ColorMapping StatusLordOfCrowns = new("Lord of Crowns", PaletteTypes.StatusEffects, Color.FromArgb(170, 102, 119));
        public ColorMapping StatusFlamethrowerFlames = new("Flamethrower Flames", PaletteTypes.StatusEffects, Color.FromArgb(238, 34, 0));
        public ColorMapping StatusTimeSUp = new("Time's Up", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 204));
        public ColorMapping StatusLastKiss = new("Last Kiss", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 255));
        public ColorMapping StatusApathetic = new("Apathetic", PaletteTypes.StatusEffects, Color.FromArgb(97, 55, 61));
        public ColorMapping StatusCemented = new("Cemented", PaletteTypes.StatusEffects, Color.CadetBlue);
        public ColorMapping StatusFilthy = new("Filthy", PaletteTypes.StatusEffects, Color.FromArgb(170, 119, 34));
        public ColorMapping StatusAetherRot = new("Aether Rot", PaletteTypes.StatusEffects, Color.FromArgb(136, 68, 51));
        public ColorMapping StatusAetherRotImmunity = new("Aether Rot Immunity", PaletteTypes.StatusEffects, Color.FromArgb(85, 85, 68));
        public ColorMapping StatusConnectivity = new("Connectivity", PaletteTypes.StatusEffects, Color.FromArgb(238, 187, 17));
        public ColorMapping StatusFalling = new("Falling", PaletteTypes.StatusEffects, Color.FromArgb(119, 34, 34));
        public ColorMapping StatusOminousWind = new("Ominous Wind", PaletteTypes.StatusEffects, Color.FromArgb(63, 44, 46));
        public ColorMapping StatusIncurable = new("Incurable", PaletteTypes.StatusEffects, Color.FromArgb(102, 170, 255));
        public ColorMapping StatusScalebound = new("Scalebound", PaletteTypes.StatusEffects, Color.FromArgb(102, 34, 17));
        public ColorMapping StatusShocked = new("Shocked", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusDrubbed = new("Drubbed", PaletteTypes.StatusEffects, Color.FromArgb(18, 10, 20));
        public ColorMapping StatusAccursedFlame = new("Accursed Flame", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 0));
        public ColorMapping StatusAirbound = new("Airbound", PaletteTypes.StatusEffects, Color.FromArgb(76, 76, 60));
        public ColorMapping StatusMoonlit = new("Moonlit", PaletteTypes.StatusEffects, Color.FromArgb(136, 119, 34));
        public ColorMapping StatusMoonshadowed = new("Moonshadowed", PaletteTypes.StatusEffects, Color.FromArgb(85, 136, 221));
        public ColorMapping StatusOdder = new("Odder", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusUnmagicked = new("Unmagicked", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 34));
        public ColorMapping StatusMagneticLysis = new("Magnetic Lysis +", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 238));
        public ColorMapping StatusMagneticLysis2 = new("Magnetic Lysis -", PaletteTypes.StatusEffects, Color.FromArgb(221, 255, 255));
        public ColorMapping StatusMagneticLevitation = new("Magnetic Levitation", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 221));
        public ColorMapping StatusComputationError = new("Computation Error", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 221));
        public ColorMapping StatusGrudge = new("Grudge", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 17));
        public ColorMapping StatusEntropy = new("Entropy", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 34));
        public ColorMapping StatusDynamicFluid = new("Dynamic Fluid", PaletteTypes.StatusEffects, Color.FromArgb(22, 40, 49));
        public ColorMapping StatusHeadwind = new("Headwind", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 221));
        public ColorMapping StatusTailwind = new("Tailwind", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusAccretion = new("Accretion", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 0));
        public ColorMapping StatusPrimordialCrust = new("Primordial Crust", PaletteTypes.StatusEffects, Color.FromArgb(136, 85, 51));
        public ColorMapping StatusDefenseless = new("Defenseless", PaletteTypes.StatusEffects, Color.FromArgb(119, 204, 255));
        public ColorMapping StatusDeathFromAbove = new("Death from Above", PaletteTypes.StatusEffects, Color.FromArgb(68, 73, 73));
        public ColorMapping StatusDeathFromBelow = new("Death from Below", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusLooper = new("Looper", PaletteTypes.StatusEffects, Color.FromArgb(34, 102, 85));
        public ColorMapping StatusMemoryDegradation = new("Memory Degradation", PaletteTypes.StatusEffects, Color.FromArgb(17, 51, 85));
        public ColorMapping StatusMemoryLoss = new("Memory Loss", PaletteTypes.StatusEffects, Color.FromArgb(187, 255, 255));
        public ColorMapping StatusChainsOfMemory = new("Chains of Memory", PaletteTypes.StatusEffects, Color.FromArgb(68, 85, 102));
        public ColorMapping StatusKillCommand = new("Kill Command", PaletteTypes.StatusEffects, Color.FromArgb(34, 102, 85));
        public ColorMapping StatusPoisonL = new("Poison L", PaletteTypes.StatusEffects, Color.FromArgb(170, 17, 221));
        public ColorMapping StatusSpiritDartL = new("Spirit Dart L", PaletteTypes.StatusEffects, Color.FromArgb(102, 204, 119));
        public ColorMapping StatusBanishL = new("Banish L", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusPacketFilterM = new("Packet Filter M", PaletteTypes.StatusEffects, Color.FromArgb(68, 102, 136));
        public ColorMapping StatusPacketFilterF = new("Packet Filter F", PaletteTypes.StatusEffects, Color.FromArgb(85, 187, 255));
        public ColorMapping StatusCriticalSynchronizationBug = new("Critical Synchronization Bug", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusCriticalOverflowBug = new("Critical Overflow Bug", PaletteTypes.StatusEffects, Color.FromArgb(255, 119, 170));
        public ColorMapping StatusCriticalUnderflowBug = new("Critical Underflow Bug", PaletteTypes.StatusEffects, Color.FromArgb(221, 102, 204));
        public ColorMapping StatusSynchronizationDebugger = new("Synchronization Debugger", PaletteTypes.StatusEffects, Color.FromArgb(136, 136, 119));
        public ColorMapping StatusOverflowDebugger = new("Overflow Debugger", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 136));
        public ColorMapping StatusUnderflowDebugger = new("Underflow Debugger", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 136));
        public ColorMapping StatusLatentDefect = new("Latent Defect", PaletteTypes.StatusEffects, Color.FromArgb(85, 136, 136));
        public ColorMapping StatusCascadingLatentDefect = new("Cascading Latent Defect", PaletteTypes.StatusEffects, Color.FromArgb(255, 102, 238));
        public ColorMapping StatusLocalRegression = new("Local Regression", PaletteTypes.StatusEffects, Color.FromArgb(119, 102, 68));
        public ColorMapping StatusRemoteRegression = new("Remote Regression", PaletteTypes.StatusEffects, Color.FromArgb(63, 76, 78));
        public ColorMapping StatusSpellInWaitingReturnIV = new("Spell-in-Waiting: Return IV", PaletteTypes.StatusEffects, Color.FromArgb(153, 136, 170));
        public ColorMapping StatusSpellInWaitingFlare = new("Spell-in-Waiting: Flare", PaletteTypes.StatusEffects, Color.FromArgb(85, 170, 187));
        public ColorMapping StatusPayingThePiper = new("Paying the Piper", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping StatusPrimaryTarget = new("Primary Target", PaletteTypes.StatusEffects, Color.FromArgb(153, 0, 0));
        public ColorMapping StatusDrowning = new("Drowning", PaletteTypes.StatusEffects, Color.FromArgb(136, 34, 34));
        public ColorMapping StatusLoomingCrescendo = new("Looming Crescendo", PaletteTypes.StatusEffects, Color.FromArgb(85, 153, 170));
        public ColorMapping StatusBiohacked = new("Biohacked", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusRightUnseen = new("Right Unseen", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusLeftUnseen = new("Left Unseen", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusBackUnseen = new("Back Unseen", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMalodorous = new("Malodorous", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 153));
        public ColorMapping StatusOffGuard = new("Off-guard", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 17));
        public ColorMapping StatusPeculiarLight = new("Peculiar Light", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 68));
        public ColorMapping StatusCursekeeper = new("Cursekeeper", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 204));
        public ColorMapping StatusWaningNocturne = new("Waning Nocturne", PaletteTypes.StatusEffects, Color.FromArgb(204, 85, 153));
        public ColorMapping StatusContractualObligation = new("Contractual Obligation", PaletteTypes.StatusEffects, Color.FromArgb(170, 136, 136));
        public ColorMapping StatusSacrifice = new("Sacrifice", PaletteTypes.StatusEffects, Color.FromArgb(153, 17, 34));
        public ColorMapping StatusBlownAway = new("Blown Away", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 221));
        public ColorMapping StatusBloodSacrifice = new("Blood Sacrifice", PaletteTypes.StatusEffects, Color.FromArgb(153, 17, 51));
        public ColorMapping StatusResurrectionRestricted = new("Resurrection Restricted", PaletteTypes.StatusEffects, Color.FromArgb(34, 153, 255));
        public ColorMapping StatusAjisai = new("Ajisai", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 153));
        public ColorMapping StatusMarkOfMortality = new("Mark of Mortality", PaletteTypes.StatusEffects, Color.FromArgb(204, 34, 51));
        public ColorMapping StatusSpellInWaitingUnholyDarkness = new("Spell-in-Waiting: Unholy Darkness", PaletteTypes.StatusEffects, Color.FromArgb(170, 119, 17));
        public ColorMapping StatusSpellInWaitingDarkFireIII = new("Spell-in-Waiting: Dark Fire III", PaletteTypes.StatusEffects, Color.FromArgb(238, 136, 221));
        public ColorMapping StatusSpellInWaitingHellWind = new("Spell-in-Waiting: Hell Wind", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 170));
        public ColorMapping StatusSpellInWaitingShadoweye = new("Spell-in-Waiting: Shadoweye", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusMonstrous = new("Monstrous", PaletteTypes.StatusEffects, Color.FromArgb(119, 51, 0));
        public ColorMapping StatusSonicBreak = new("Sonic Break", PaletteTypes.StatusEffects, Color.FromArgb(221, 255, 238));
        public ColorMapping StatusBowShock = new("Bow Shock", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 119));
        public ColorMapping StatusCurseOfTheRonka = new("Curse of the Ronka", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 136));
        public ColorMapping StatusSurgingWaters = new("Surging Waters", PaletteTypes.StatusEffects, Color.FromArgb(221, 238, 255));
        public ColorMapping StatusSplashingWaters = new("Splashing Waters", PaletteTypes.StatusEffects, Color.FromArgb(204, 221, 255));
        public ColorMapping StatusSwirlingWaters = new("Swirling Waters", PaletteTypes.StatusEffects, Color.FromArgb(51, 119, 221));
        public ColorMapping StatusSmotheringWaters = new("Smothering Waters", PaletteTypes.StatusEffects, Color.FromArgb(51, 153, 255));
        public ColorMapping StatusSunderingWaters = new("Sundering Waters", PaletteTypes.StatusEffects, Color.FromArgb(204, 221, 255));
        public ColorMapping StatusSweepingWaters = new("Sweeping Waters", PaletteTypes.StatusEffects, Color.FromArgb(204, 221, 255));
        public ColorMapping StatusFadingFast = new("Fading Fast", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 119));
        public ColorMapping StatusVitalSign = new("Vital Sign", PaletteTypes.StatusEffects, Color.FromArgb(136, 119, 119));
        public ColorMapping StatusBioblaster = new("Bioblaster", PaletteTypes.StatusEffects, Color.FromArgb(68, 34, 51));
        public ColorMapping StatusDia = new("Dia", PaletteTypes.StatusEffects, Color.FromArgb(119, 170, 238));
        public ColorMapping StatusCombustIII = new("Combust III", PaletteTypes.StatusEffects, Color.FromArgb(204, 255, 255));
        public ColorMapping StatusScouringWaters = new("Scouring Waters", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 255));
        public ColorMapping StatusBiolysis = new("Biolysis", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 255));
        public ColorMapping StatusFadedOut = new("Faded Out", PaletteTypes.StatusEffects, Color.FromArgb(221, 187, 187));
        public ColorMapping StatusSoakingWet = new("Soaking Wet", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 187));
        public ColorMapping StatusBeckoned = new("Beckoned", PaletteTypes.StatusEffects, Color.FromArgb(121, 121, 116));
        public ColorMapping StatusChilledToTheBone = new("Chilled to the Bone", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 187));
        public ColorMapping StatusAccursedPoison = new("Accursed Poison", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusBehelmed = new("Behelmed", PaletteTypes.StatusEffects, Color.FromArgb(170, 136, 85));
        public ColorMapping StatusFullSwing = new("Full Swing", PaletteTypes.StatusEffects, Color.FromArgb(255, 51, 0));
        public ColorMapping StatusPhantomDart = new("Phantom Dart", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusTrickAttack = new("Trick Attack", PaletteTypes.StatusEffects, Color.FromArgb(255, 170, 0));
        public ColorMapping StatusFountainOfFire = new("Fountain of Fire", PaletteTypes.StatusEffects, Color.FromArgb(170, 34, 0));
        public ColorMapping StatusDrainedPower = new("Drained Power", PaletteTypes.StatusEffects, Color.FromArgb(85, 119, 170));
        public ColorMapping StatusDrainedFortitude = new("Drained Fortitude", PaletteTypes.StatusEffects, Color.FromArgb(170, 136, 85));
        public ColorMapping StatusAcidicBite = new("Acidic Bite", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 187));
        public ColorMapping StatusConfiteor = new("Confiteor", PaletteTypes.StatusEffects, Color.FromArgb(204, 153, 136));
        public ColorMapping StatusInnerChaos = new("Inner Chaos", PaletteTypes.StatusEffects, Color.FromArgb(238, 136, 85));
        public ColorMapping StatusChaoticCyclone = new("Chaotic Cyclone", PaletteTypes.StatusEffects, Color.FromArgb(221, 17, 0));
        public ColorMapping StatusEdgeOfShadow = new("Edge of Shadow", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusAncientCircle = new("Ancient Circle", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 119));
        public ColorMapping StatusConked = new("Conked", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 51));
        public ColorMapping StatusAstralAttenuation = new("Astral Attenuation", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 221));
        public ColorMapping StatusUmbralAttenuation = new("Umbral Attenuation", PaletteTypes.StatusEffects, Color.FromArgb(136, 170, 136));
        public ColorMapping StatusPhysicalAttenuation = new("Physical Attenuation", PaletteTypes.StatusEffects, Color.FromArgb(136, 102, 0));
        public ColorMapping StatusBurningBrand = new("Burning Brand", PaletteTypes.StatusEffects, Color.FromArgb(238, 136, 68));
        public ColorMapping StatusFreezingBrand = new("Freezing Brand", PaletteTypes.StatusEffects, Color.FromArgb(0, 85, 255));
        public ColorMapping StatusMortalFlame = new("Mortal Flame", PaletteTypes.StatusEffects, Color.FromArgb(136, 204, 238));
        public ColorMapping StatusFinalDecreeNisi = new("Final Decree Nisi γ", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 85));
        public ColorMapping StatusFinalDecreeNisi2 = new("Final Decree Nisi δ", PaletteTypes.StatusEffects, Color.FromArgb(51, 51, 17));
        public ColorMapping StatusFinalJudgmentDecreeNisi = new("Final Judgment: Decree Nisi γ", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 85));
        public ColorMapping StatusFinalJudgmentDecreeNisi2 = new("Final Judgment: Decree Nisi δ", PaletteTypes.StatusEffects, Color.FromArgb(51, 51, 17));
        public ColorMapping StatusFinalJudgmentPenaltyIV = new("Final Judgment: Penalty IV", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusContactProhibitionOrdained = new("Contact Prohibition Ordained", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 221));
        public ColorMapping StatusContactRegulationOrdained = new("Contact Regulation Ordained", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 170));
        public ColorMapping StatusEscapeProhibitionOrdained = new("Escape Prohibition Ordained", PaletteTypes.StatusEffects, Color.FromArgb(170, 153, 238));
        public ColorMapping StatusEscapeDetectionOrdained = new("Escape Detection Ordained", PaletteTypes.StatusEffects, Color.FromArgb(204, 102, 255));
        public ColorMapping StatusFinalWordContactProhibition = new("Final Word: Contact Prohibition", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 221));
        public ColorMapping StatusFinalWordContactRegulation = new("Final Word: Contact Regulation", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusFinalWordEscapeProhibition = new("Final Word: Escape Prohibition", PaletteTypes.StatusEffects, Color.FromArgb(170, 153, 238));
        public ColorMapping StatusFinalWordEscapeDetection = new("Final Word: Escape Detection", PaletteTypes.StatusEffects, Color.FromArgb(187, 85, 255));
        public ColorMapping StatusOil = new("Oil", PaletteTypes.StatusEffects, Color.FromArgb(170, 102, 17));
        public ColorMapping StatusFloodOfShadow = new("Flood of Shadow", PaletteTypes.StatusEffects, Color.FromArgb(119, 34, 119));
        public ColorMapping StatusArmSLength = new("Arm's Length", PaletteTypes.StatusEffects, Color.FromArgb(153, 85, 119));
        public ColorMapping StatusAethericBurst = new("Aetheric Burst", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 0));
        public ColorMapping StatusDeathBecomesYou = new("Death Becomes You", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 17));
        public ColorMapping StatusMortalPowderMark = new("Mortal Powder Mark", PaletteTypes.StatusEffects, Color.FromArgb(68, 68, 34));
        public ColorMapping StatusNormal = new("Normal", PaletteTypes.StatusEffects, Color.FromArgb(39, 39, 39));
        public ColorMapping StatusRunningHot1 = new("Running Hot: +1", PaletteTypes.StatusEffects, Color.FromArgb(221, 119, 17));
        public ColorMapping StatusAtlas = new("Atlas", PaletteTypes.StatusEffects, Color.FromArgb(221, 153, 85));
        public ColorMapping StatusPallOfRage = new("Pall of Rage", PaletteTypes.StatusEffects, Color.FromArgb(221, 68, 0));
        public ColorMapping StatusPallOfGrief = new("Pall of Grief", PaletteTypes.StatusEffects, Color.FromArgb(85, 187, 238));
        public ColorMapping StatusRunningHot2 = new("Running Hot: +2", PaletteTypes.StatusEffects, Color.FromArgb(221, 119, 17));
        public ColorMapping StatusFinalDecreeNisi3 = new("Final Decree Nisi α", PaletteTypes.StatusEffects, Color.FromArgb(153, 170, 170));
        public ColorMapping StatusFinalDecreeNisi4 = new("Final Decree Nisi β", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 34));
        public ColorMapping StatusFinalJudgmentDecreeNisi3 = new("Final Judgment: Decree Nisi α", PaletteTypes.StatusEffects, Color.FromArgb(153, 170, 170));
        public ColorMapping StatusFinalJudgmentDecreeNisi4 = new("Final Judgment: Decree Nisi β", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 34));
        public ColorMapping StatusSystemShock = new("System Shock", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 0));
        public ColorMapping StatusElectrified = new("Electrified", PaletteTypes.StatusEffects, Color.FromArgb(51, 85, 51));
        public ColorMapping StatusHatedOfTheVortex = new("Hated of the Vortex", PaletteTypes.StatusEffects, Color.FromArgb(136, 221, 204));
        public ColorMapping StatusHatedOfEmbers = new("Hated of Embers", PaletteTypes.StatusEffects, Color.FromArgb(221, 136, 102));
        public ColorMapping StatusIronsOfPurgatory = new("Irons of Purgatory", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 0));
        public ColorMapping StatusAstralEffect = new("Astral Effect", PaletteTypes.StatusEffects, Color.FromArgb(51, 153, 238));
        public ColorMapping StatusUmbralEffect = new("Umbral Effect", PaletteTypes.StatusEffects, Color.FromArgb(153, 0, 119));
        public ColorMapping StatusForwardWithThee = new("Forward with Thee", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 0));
        public ColorMapping StatusBackWithThee = new("Back with Thee", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 0));
        public ColorMapping StatusLeftWithThee = new("Left with Thee", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 0));
        public ColorMapping StatusRightWithThee = new("Right with Thee", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 0));
        public ColorMapping StatusHatedOfLevin = new("Hated of Levin", PaletteTypes.StatusEffects, Color.FromArgb(119, 0, 85));
        public ColorMapping StatusWaymark = new("Waymark", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusUnstable = new("Unstable", PaletteTypes.StatusEffects, Color.FromArgb(17, 119, 238));
        public ColorMapping StatusLightheaded = new("Lightheaded", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusFreezing = new("Freezing", PaletteTypes.StatusEffects, Color.FromArgb(34, 119, 170));
        public ColorMapping StatusRefulgentChain = new("Refulgent Chain", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 51));
        public ColorMapping StatusRefulgentFate = new("Refulgent Fate", PaletteTypes.StatusEffects, Color.FromArgb(136, 119, 0));
        public ColorMapping StatusLightsteeped = new("Lightsteeped", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 51));
        public ColorMapping StatusWyrmclaw = new("Wyrmclaw", PaletteTypes.StatusEffects, Color.FromArgb(238, 119, 119));
        public ColorMapping StatusWyrmfang = new("Wyrmfang", PaletteTypes.StatusEffects, Color.FromArgb(34, 68, 238));
        public ColorMapping StatusHatedOfFrost = new("Hated of Frost", PaletteTypes.StatusEffects, Color.FromArgb(17, 68, 85));
        public ColorMapping StatusHatedOfTheWyrm = new("Hated of the Wyrm", PaletteTypes.StatusEffects, Color.FromArgb(0, 85, 0));
        public ColorMapping StatusRunningCold1 = new("Running Cold: -1", PaletteTypes.StatusEffects, Color.FromArgb(17, 102, 204));
        public ColorMapping StatusPanic = new("Panic", PaletteTypes.StatusEffects, Color.FromArgb(187, 170, 170));
        public ColorMapping StatusRunningCold2 = new("Running Cold: -2", PaletteTypes.StatusEffects, Color.FromArgb(17, 102, 204));
        public ColorMapping StatusIntemperate = new("Intemperate", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 51));
        public ColorMapping StatusHotBrand1 = new("Hot Brand: +1", PaletteTypes.StatusEffects, Color.FromArgb(6, 4, 4));
        public ColorMapping StatusLightResistanceDown = new("Light Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(221, 170, 153));
        public ColorMapping StatusShieldProtocolA = new("Shield Protocol A", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 187));
        public ColorMapping StatusShieldProtocolB = new("Shield Protocol B", PaletteTypes.StatusEffects, Color.FromArgb(170, 153, 136));
        public ColorMapping StatusShieldProtocolC = new("Shield Protocol C", PaletteTypes.StatusEffects, Color.FromArgb(68, 68, 68));
        public ColorMapping StatusHotBrand2 = new("Hot Brand: +2", PaletteTypes.StatusEffects, Color.FromArgb(255, 85, 68));
        public ColorMapping StatusColdBrand1 = new("Cold Brand: -1", PaletteTypes.StatusEffects, Color.FromArgb(2, 3, 4));
        public ColorMapping StatusColdBrand2 = new("Cold Brand: -2", PaletteTypes.StatusEffects, Color.FromArgb(0, 153, 255));
        public ColorMapping StatusTenderAnaphylaxis = new("Tender Anaphylaxis", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusJealousAnaphylaxis = new("Jealous Anaphylaxis", PaletteTypes.StatusEffects, Color.FromArgb(102, 119, 238));
        public ColorMapping StatusTrueWalkingDead = new("True Walking Dead", PaletteTypes.StatusEffects, Color.FromArgb(136, 136, 170));
        public ColorMapping StatusFloatingFetters = new("Floating Fetters", PaletteTypes.StatusEffects, Color.FromArgb(51, 119, 170));
        public ColorMapping StatusSafetyLockPyreticBooster = new("Safety Lock: Pyretic Booster", PaletteTypes.StatusEffects, Color.FromArgb(102, 170, 255));
        public ColorMapping StatusSafetyLockAetherialAegis = new("Safety Lock: Aetherial Aegis", PaletteTypes.StatusEffects, Color.FromArgb(85, 170, 255));
        public ColorMapping StatusABitBerserk = new("A Bit Berserk", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 85));
        public ColorMapping StatusTrulyBerserk = new("Truly Berserk", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 68));
        public ColorMapping StatusLostBanish = new("Lost Banish", PaletteTypes.StatusEffects, Color.FromArgb(34, 119, 255));
        public ColorMapping StatusFullyAnalyzed = new("Fully Analyzed", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusPallOfDarkness = new("Pall of Darkness", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 68));
        public ColorMapping StatusPhysicalAversion = new("Physical Aversion", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 221));
        public ColorMapping StatusMagicalAversion = new("Magical Aversion", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 221));
        public ColorMapping StatusEPPenalty = new("EP Penalty", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 17));
        public ColorMapping StatusStygianTendrils = new("Stygian Tendrils", PaletteTypes.StatusEffects, Color.FromArgb(170, 51, 34));
        public ColorMapping StatusCurseOfDarkness = new("Curse of Darkness", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 0));
        public ColorMapping StatusPain = new("Pain", PaletteTypes.StatusEffects, Color.FromArgb(85, 17, 34));
        public ColorMapping StatusAtTheLimit = new("At the Limit", PaletteTypes.StatusEffects, Color.FromArgb(153, 204, 255));
        public ColorMapping StatusDutiesAsAssigned = new("Duties as Assigned", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusServantOfShadow = new("Servant of Shadow", PaletteTypes.StatusEffects, Color.FromArgb(119, 187, 204));
        public ColorMapping StatusShadowed = new("Shadowed", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 170));
        public ColorMapping StatusShackledApart = new("Shackled Apart", PaletteTypes.StatusEffects, Color.FromArgb(0, 17, 17));
        public ColorMapping StatusShackledTogether = new("Shackled Together", PaletteTypes.StatusEffects, Color.FromArgb(17, 51, 17));
        public ColorMapping StatusWandererSFate = new("Wanderer's Fate", PaletteTypes.StatusEffects, Color.FromArgb(153, 85, 153));
        public ColorMapping StatusSacrificeSFate = new("Sacrifice's Fate", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 0));
        public ColorMapping StatusLostFlareStar = new("Lost Flare Star", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusLostRendArmor = new("Lost Rend Armor", PaletteTypes.StatusEffects, Color.FromArgb(204, 153, 119));
        public ColorMapping StatusReversalOfForces = new("Reversal of Forces", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping StatusPowderMark = new("Powder Mark", PaletteTypes.StatusEffects, Color.FromArgb(51, 51, 17));
        public ColorMapping StatusReturn = new("Return", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 119));
        public ColorMapping StatusReturnIV = new("Return IV", PaletteTypes.StatusEffects, Color.FromArgb(238, 136, 238));
        public ColorMapping StatusSpellInWaitingDarkEruption = new("Spell-in-Waiting: Dark Eruption", PaletteTypes.StatusEffects, Color.FromArgb(204, 170, 255));
        public ColorMapping StatusSpellInWaitingDarkWaterIII = new("Spell-in-Waiting: Dark Water III", PaletteTypes.StatusEffects, Color.FromArgb(153, 153, 170));
        public ColorMapping StatusSpellInWaitingDarkBlizzardIII = new("Spell-in-Waiting: Dark Blizzard III", PaletteTypes.StatusEffects, Color.FromArgb(221, 255, 255));
        public ColorMapping StatusSpellInWaitingDarkAeroIII = new("Spell-in-Waiting: Dark Aero III", PaletteTypes.StatusEffects, Color.FromArgb(170, 255, 187));
        public ColorMapping StatusSpellInWaitingReturn = new("Spell-in-Waiting: Return", PaletteTypes.StatusEffects, Color.FromArgb(136, 136, 187));
        public ColorMapping StatusIceResistanceDownII = new("Ice Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 187));
        public ColorMapping StatusAetherialDepletion = new("Aetherial Depletion", PaletteTypes.StatusEffects, Color.FromArgb(68, 68, 51));
        public ColorMapping StatusMovementEdict2Squares = new("Movement Edict: 2 Squares", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 170));
        public ColorMapping StatusMovementEdict3Squares = new("Movement Edict: 3 Squares", PaletteTypes.StatusEffects, Color.FromArgb(136, 51, 170));
        public ColorMapping StatusMovementEdict4Squares = new("Movement Edict: 4 Squares", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 170));
        public ColorMapping StatusYourMove2Squares = new("Your Move: 2 Squares", PaletteTypes.StatusEffects, Color.FromArgb(110, 49, 23));
        public ColorMapping StatusYourMove3Squares = new("Your Move: 3 Squares", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 0));
        public ColorMapping StatusYourMove4Squares = new("Your Move: 4 Squares", PaletteTypes.StatusEffects, Color.FromArgb(170, 119, 119));
        public ColorMapping StatusTwiceComeRuin = new("Twice-come Ruin", PaletteTypes.StatusEffects, Color.FromArgb(170, 34, 255));
        public ColorMapping StatusSuckedIn = new("Sucked In", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 204));
        public ColorMapping StatusIncendiaryBurns = new("Incendiary Burns", PaletteTypes.StatusEffects, Color.FromArgb(187, 68, 34));
        public ColorMapping StatusHerbsona = new("Herbsona", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 204));
        public ColorMapping StatusBloodyRuin = new("Bloody Ruin", PaletteTypes.StatusEffects, Color.FromArgb(119, 17, 17));
        public ColorMapping StatusTorrentialRuin = new("Torrential Ruin", PaletteTypes.StatusEffects, Color.FromArgb(17, 51, 204));
        public ColorMapping StatusAvariciousRuin = new("Avaricious Ruin", PaletteTypes.StatusEffects, Color.FromArgb(17, 102, 17));
        public ColorMapping StatusSubtleRuin = new("Subtle Ruin", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusThriceComeRuin = new("Thrice-come Ruin", PaletteTypes.StatusEffects, Color.FromArgb(170, 34, 255));
        public ColorMapping StatusSpellInWaitingQuietus = new("Spell-in-Waiting: Quietus", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 238));
        public ColorMapping StatusCloyingCondensation = new("Cloying Condensation", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusDistorted = new("Distorted", PaletteTypes.StatusEffects, Color.FromArgb(204, 170, 153));
        public ColorMapping StatusFreezingUp = new("Freezing Up", PaletteTypes.StatusEffects, Color.FromArgb(51, 68, 68));
        public ColorMapping StatusDuelOrDie = new("Duel or Die", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 0));
        public ColorMapping StatusLostBurst = new("Lost Burst", PaletteTypes.StatusEffects, Color.FromArgb(119, 153, 255));
        public ColorMapping StatusLostRampage = new("Lost Rampage", PaletteTypes.StatusEffects, Color.FromArgb(204, 51, 17));
        public ColorMapping StatusBreak = new("Break", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 34));
        public ColorMapping StatusLightningRod = new("Lightning Rod", PaletteTypes.StatusEffects, Color.FromArgb(17, 221, 255));
        public ColorMapping StatusSystemLock = new("System Lock", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusDeathSDesign = new("Death's Design", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 0));
        public ColorMapping StatusHubris = new("Hubris", PaletteTypes.StatusEffects, Color.FromArgb(85, 221, 221));
        public ColorMapping StatusEukrasianDosis = new("Eukrasian Dosis", PaletteTypes.StatusEffects, Color.FromArgb(204, 34, 255));
        public ColorMapping StatusEukrasianDosisII = new("Eukrasian Dosis II", PaletteTypes.StatusEffects, Color.FromArgb(238, 153, 238));
        public ColorMapping StatusEukrasianDosisIII = new("Eukrasian Dosis III", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 255));
        public ColorMapping StatusDownAndOut = new("Down and Out", PaletteTypes.StatusEffects, Color.FromArgb(136, 119, 102));
        public ColorMapping StatusFrontUnseen = new("Front Unseen", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusLeveilleurDosisIII = new("Leveilleur Dosis III", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 255));
        public ColorMapping StatusManusyaBerserk = new("Manusya Berserk", PaletteTypes.StatusEffects, Color.FromArgb(85, 0, 0));
        public ColorMapping StatusManusyaConfuse = new("Manusya Confuse", PaletteTypes.StatusEffects, Color.FromArgb(0, 102, 0));
        public ColorMapping StatusManusyaStop = new("Manusya Stop", PaletteTypes.StatusEffects, Color.FromArgb(0, 17, 17));
        public ColorMapping StatusSkyblind = new("Skyblind", PaletteTypes.StatusEffects, Color.FromArgb(221, 153, 17));
        public ColorMapping StatusPlanarImprisonment = new("Planar Imprisonment", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 34));
        public ColorMapping StatusKatabasis = new("Katabasis", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 204));
        public ColorMapping StatusChaoticSpring = new("Chaotic Spring", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBladeOfValor = new("Blade of Valor", PaletteTypes.StatusEffects, Color.FromArgb(68, 119, 187));
        public ColorMapping StatusAnguish = new("Anguish", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusDiscomposed = new("Discomposed", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 51));
        public ColorMapping StatusToxicosis = new("Toxicosis", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 204));
        public ColorMapping StatusFadingConsciousness = new("Fading Consciousness", PaletteTypes.StatusEffects, Color.FromArgb(51, 51, 51));
        public ColorMapping StatusAtDeathSDoor = new("At Death's Door", PaletteTypes.StatusEffects, Color.FromArgb(170, 204, 204));
        public ColorMapping StatusColdSpell = new("Cold Spell", PaletteTypes.StatusEffects, Color.FromArgb(68, 204, 255));
        public ColorMapping StatusHotSpell = new("Hot Spell", PaletteTypes.StatusEffects, Color.FromArgb(221, 51, 51));
        public ColorMapping StatusShacklesOfTime = new("Shackles of Time", PaletteTypes.StatusEffects, Color.FromArgb(119, 153, 255));
        public ColorMapping StatusShacklesOfCompanionship = new("Shackles of Companionship", PaletteTypes.StatusEffects, Color.FromArgb(187, 204, 238));
        public ColorMapping StatusShacklesOfLoneliness = new("Shackles of Loneliness", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 34));
        public ColorMapping StatusInescapableCompanionship = new("Inescapable Companionship", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 255));
        public ColorMapping StatusInescapableLoneliness = new("Inescapable Loneliness", PaletteTypes.StatusEffects, Color.FromArgb(136, 136, 102));
        public ColorMapping StatusHighJumpTarget = new("High Jump Target", PaletteTypes.StatusEffects, Color.FromArgb(0, 51, 136));
        public ColorMapping StatusSpineshatterDiveTarget = new("Spineshatter Dive Target", PaletteTypes.StatusEffects, Color.FromArgb(153, 153, 136));
        public ColorMapping StatusElusiveJumpTarget = new("Elusive Jump Target", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 221));
        public ColorMapping StatusSpreadingFlames = new("Spreading Flames", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 153));
        public ColorMapping StatusEntangledFlames = new("Entangled Flames", PaletteTypes.StatusEffects, Color.FromArgb(34, 51, 136));
        public ColorMapping StatusDeathSToll = new("Death's Toll", PaletteTypes.StatusEffects, Color.FromArgb(0, 34, 119));
        public ColorMapping StatusSmileyFace = new("Smiley Face", PaletteTypes.StatusEffects, Color.FromArgb(17, 34, 51));
        public ColorMapping StatusFrownyFace = new("Frowny Face", PaletteTypes.StatusEffects, Color.FromArgb(204, 102, 85));
        public ColorMapping StatusMarkOfEasyPrey = new("Mark of Easy Prey", PaletteTypes.StatusEffects, Color.FromArgb(170, 153, 102));
        public ColorMapping StatusMarkOfTheTides = new("Mark of the Tides", PaletteTypes.StatusEffects, Color.FromArgb(22, 40, 49));
        public ColorMapping StatusMarkOfTheDepths = new("Mark of the Depths", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 255));
        public ColorMapping StatusForeMarkOfTheTides = new("Fore Mark of the Tides", PaletteTypes.StatusEffects, Color.FromArgb(0, 187, 238));
        public ColorMapping StatusRearMarkOfTheTides = new("Rear Mark of the Tides", PaletteTypes.StatusEffects, Color.FromArgb(0, 187, 238));
        public ColorMapping StatusLeftMarkOfTheTides = new("Left Mark of the Tides", PaletteTypes.StatusEffects, Color.FromArgb(0, 187, 238));
        public ColorMapping StatusRightMarkOfTheTides = new("Right Mark of the Tides", PaletteTypes.StatusEffects, Color.FromArgb(0, 187, 238));
        public ColorMapping StatusBoundAndDetermined = new("Bound and Determined", PaletteTypes.StatusEffects, Color.FromArgb(85, 119, 204));
        public ColorMapping StatusCircleOfClarity = new("Circle of Clarity", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusTrueReprisal = new("True Reprisal", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 170));
        public ColorMapping StatusElementalResistanceDown = new("Elemental Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 0));
        public ColorMapping StatusRoleCall = new("Role Call", PaletteTypes.StatusEffects, Color.FromArgb(34, 187, 51));
        public ColorMapping StatusMiscast = new("Miscast", PaletteTypes.StatusEffects, Color.FromArgb(0, 68, 17));
        public ColorMapping StatusThornpricked = new("Thornpricked", PaletteTypes.StatusEffects, Color.FromArgb(131, 14, 60));
        public ColorMapping StatusChorusAligned = new("Chorus Aligned", PaletteTypes.StatusEffects, Color.FromArgb(102, 136, 68));
        public ColorMapping StatusToTheDungeons = new("To the Dungeons", PaletteTypes.StatusEffects, Color.FromArgb(255, 187, 255));
        public ColorMapping StatusIncapacitated = new("Incapacitated", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 51));
        public ColorMapping StatusDeactivated = new("Deactivated", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusCreepingPoison = new("Creeping Poison", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 0));
        public ColorMapping StatusTenebrousGrasp = new("Tenebrous Grasp", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 238));
        public ColorMapping StatusWhisperedIncantation = new("Whispered Incantation", PaletteTypes.StatusEffects, Color.FromArgb(0, 102, 153));
        public ColorMapping StatusWhispersManifest = new("Whispers Manifest", PaletteTypes.StatusEffects, Color.FromArgb(153, 85, 221));
        public ColorMapping StatusMirroredIncantation = new("Mirrored Incantation", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 136));
        public ColorMapping StatusHPRecoveryDown = new("HP Recovery Down", PaletteTypes.StatusEffects, Color.FromArgb(153, 68, 187));
        public ColorMapping StatusPlagued = new("Plagued", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusMortalVow = new("Mortal Vow", PaletteTypes.StatusEffects, Color.FromArgb(56, 57, 34));
        public ColorMapping StatusMortalAtonement = new("Mortal Atonement", PaletteTypes.StatusEffects, Color.FromArgb(18, 23, 19));
        public ColorMapping StatusBoiling = new("Boiling", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 17));
        public ColorMapping StatusSustainedDarkDamage = new("Sustained Dark Damage", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 238));
        public ColorMapping StatusSustainedLightDamage = new("Sustained Light Damage", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 238));
        public ColorMapping StatusUmbralRays = new("Umbral Rays", PaletteTypes.StatusEffects, Color.FromArgb(136, 102, 153));
        public ColorMapping StatusActingDPS = new("Acting DPS", PaletteTypes.StatusEffects, Color.FromArgb(221, 153, 136));
        public ColorMapping StatusActingHealer = new("Acting Healer", PaletteTypes.StatusEffects, Color.FromArgb(85, 0, 17));
        public ColorMapping StatusActingTank = new("Acting Tank", PaletteTypes.StatusEffects, Color.FromArgb(119, 0, 34));
        public ColorMapping StatusWillToLive = new("Will to Live", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 0));
        public ColorMapping StatusTwistingViper = new("Twisting Viper", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 153));
        public ColorMapping StatusGnashingWolf = new("Gnashing Wolf", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 204));
        public ColorMapping StatusNecrosis = new("Necrosis", PaletteTypes.StatusEffects, Color.FromArgb(34, 85, 102));
        public ColorMapping StatusCravenCompanionship = new("Craven Companionship", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusCraniotomy = new("Craniotomy", PaletteTypes.StatusEffects, Color.FromArgb(97, 55, 61));
        public ColorMapping StatusSpinning = new("Spinning", PaletteTypes.StatusEffects, Color.FromArgb(221, 170, 238));
        public ColorMapping StatusDizzy = new("Dizzy", PaletteTypes.StatusEffects, Color.FromArgb(85, 85, 85));
        public ColorMapping StatusEchoesOfNausea = new("Echoes of Nausea", PaletteTypes.StatusEffects, Color.FromArgb(34, 68, 85));
        public ColorMapping StatusEchoesOfBefoulment = new("Echoes of Befoulment", PaletteTypes.StatusEffects, Color.FromArgb(85, 102, 68));
        public ColorMapping StatusEchoesOfTheFuture = new("Echoes of the Future", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusEchoesOfBenevolence = new("Echoes of Benevolence", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 68));
        public ColorMapping StatusGripOfDespair = new("Grip of Despair", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping StatusFirstInLine = new("First in Line", PaletteTypes.StatusEffects, Color.FromArgb(68, 102, 204));
        public ColorMapping StatusSecondInLine = new("Second in Line", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 85));
        public ColorMapping StatusThirdInLine = new("Third in Line", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 238));
        public ColorMapping StatusUnshakableLoyalty = new("Unshakable Loyalty", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusOnBalance = new("On Balance", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusUnguarded = new("Unguarded", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 221));
        public ColorMapping StatusHalfAsleep = new("Half-asleep", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 170));
        public ColorMapping StatusSacredClaim = new("Sacred Claim", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusOnslaught = new("Onslaught", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 0));
        public ColorMapping StatusSaltSBane = new("Salt's Bane", PaletteTypes.StatusEffects, Color.FromArgb(255, 119, 204));
        public ColorMapping StatusRelentlessShrapnel = new("Relentless Shrapnel", PaletteTypes.StatusEffects, Color.FromArgb(221, 68, 204));
        public ColorMapping StatusDotonCorruption = new("Doton Corruption", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 119));
        public ColorMapping StatusRhythmeticFever = new("Rhythmetic Fever", PaletteTypes.StatusEffects, Color.FromArgb(221, 255, 85));
        public ColorMapping StatusMiracleOfNature = new("Miracle of Nature", PaletteTypes.StatusEffects, Color.FromArgb(68, 102, 34));
        public ColorMapping StatusBiolytic = new("Biolytic", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 17));
        public ColorMapping StatusCelestialTide = new("Celestial Tide", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 34));
        public ColorMapping StatusToxikon = new("Toxikon", PaletteTypes.StatusEffects, Color.FromArgb(170, 34, 85));
        public ColorMapping StatusII = new("●トキシコンII", PaletteTypes.StatusEffects, Color.LightSlateGray);
        public ColorMapping StatusLype = new("Lype", PaletteTypes.StatusEffects, Color.FromArgb(238, 153, 0));
        public ColorMapping StatusFeatherbrained = new("Featherbrained", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 51));
        public ColorMapping StatusDevouringDark = new("Devouring Dark", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusDarkResistanceDown = new("Dark Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 238));
        public ColorMapping StatusChainSaw = new("Chain Saw", PaletteTypes.StatusEffects, Color.FromArgb(136, 85, 85));
        public ColorMapping StatusMortared = new("Mortared", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 51));
        public ColorMapping StatusPressurePoint = new("Pressure Point", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 0));
        public ColorMapping StatusMeteodrive = new("Meteodrive", PaletteTypes.StatusEffects, Color.FromArgb(204, 153, 119));
        public ColorMapping StatusHorridRoar = new("Horrid Roar", PaletteTypes.StatusEffects, Color.FromArgb(102, 102, 119));
        public ColorMapping StatusMug = new("Mug", PaletteTypes.StatusEffects, Color.FromArgb(68, 68, 34));
        public ColorMapping StatusGokaMekkyaku = new("Goka Mekkyaku", PaletteTypes.StatusEffects, Color.FromArgb(136, 34, 0));
        public ColorMapping StatusDeathLink = new("Death Link", PaletteTypes.StatusEffects, Color.FromArgb(187, 102, 68));
        public ColorMapping StatusSealedGokaMekkyaku = new("Sealed Goka Mekkyaku", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusSealedHyoshoRanryu = new("Sealed Hyosho Ranryu", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusSealedForkedRaiju = new("Sealed Forked Raiju", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusSealedHuton = new("Sealed Huton", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusSealedDoton = new("Sealed Doton", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusSealedMeisui = new("Sealed Meisui", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusKuzushi = new("Kuzushi", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusDeathWarrant = new("Death Warrant", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 0));
        public ColorMapping StatusTheUnforgotten = new("The Unforgotten", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 119));
        public ColorMapping StatusAstralWarmth = new("Astral Warmth", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 153));
        public ColorMapping StatusUmbralFreeze = new("Umbral Freeze", PaletteTypes.StatusEffects, Color.FromArgb(221, 238, 238));
        public ColorMapping StatusSlipping = new("Slipping", PaletteTypes.StatusEffects, Color.FromArgb(51, 170, 102));
        public ColorMapping StatusScarletFlame = new("Scarlet Flame", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 119));
        public ColorMapping StatusRevelation = new("Revelation", PaletteTypes.StatusEffects, Color.FromArgb(255, 187, 51));
        public ColorMapping StatusEnchantedRiposte = new("Enchanted Riposte", PaletteTypes.StatusEffects, Color.FromArgb(221, 85, 238));
        public ColorMapping StatusEnchantedZwerchhau = new("Enchanted Zwerchhau", PaletteTypes.StatusEffects, Color.FromArgb(255, 170, 204));
        public ColorMapping StatusEnchantedRedoublement = new("Enchanted Redoublement", PaletteTypes.StatusEffects, Color.FromArgb(136, 51, 85));
        public ColorMapping StatusFrazzle = new("Frazzle", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 0));
        public ColorMapping StatusOrogeny = new("Orogeny", PaletteTypes.StatusEffects, Color.FromArgb(153, 17, 34));
        public ColorMapping StatusLiftoff = new("Liftoff", PaletteTypes.StatusEffects, Color.FromArgb(17, 119, 238));
        public ColorMapping StatusFirstBrand = new("First Brand", PaletteTypes.StatusEffects, Color.FromArgb(136, 51, 0));
        public ColorMapping StatusSecondBrand = new("Second Brand", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 187));
        public ColorMapping StatusThirdBrand = new("Third Brand", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusFourthBrand = new("Fourth Brand", PaletteTypes.StatusEffects, Color.FromArgb(44, 32, 25));
        public ColorMapping StatusFirstFlame = new("First Flame", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 85));
        public ColorMapping StatusSecondFlame = new("Second Flame", PaletteTypes.StatusEffects, Color.FromArgb(119, 51, 34));
        public ColorMapping StatusThirdFlame = new("Third Flame", PaletteTypes.StatusEffects, Color.FromArgb(102, 17, 17));
        public ColorMapping StatusFourthFlame = new("Fourth Flame", PaletteTypes.StatusEffects, Color.FromArgb(119, 17, 17));
        public ColorMapping StatusCallOfThePortal = new("Call of the Portal", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusRiteOfPassage = new("Rite of Passage", PaletteTypes.StatusEffects, Color.FromArgb(102, 51, 34));
        public ColorMapping StatusForbiddenPassage = new("Forbidden Passage", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusBrainRot = new("Brain Rot", PaletteTypes.StatusEffects, Color.FromArgb(136, 85, 136));
        public ColorMapping StatusTangled = new("Tangled", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 102));
        public ColorMapping StatusEchoOfTheFallen = new("Echo of the Fallen", PaletteTypes.StatusEffects, Color.FromArgb(119, 34, 153));
        public ColorMapping StatusScreamOfTheFallen = new("Scream of the Fallen", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 136));
        public ColorMapping StatusLingeringEchoes = new("Lingering Echoes", PaletteTypes.StatusEffects, Color.FromArgb(34, 85, 238));
        public ColorMapping StatusThunderousEcho = new("Thunderous Echo", PaletteTypes.StatusEffects, Color.FromArgb(85, 85, 119));
        public ColorMapping StatusChainsOfResentment = new("Chains of Resentment", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 238));
        public ColorMapping StatusGildedFate = new("Gilded Fate", PaletteTypes.StatusEffects, Color.FromArgb(153, 119, 34));
        public ColorMapping StatusSilveredFate = new("Silvered Fate", PaletteTypes.StatusEffects, Color.FromArgb(119, 119, 153));
        public ColorMapping StatusInviolateWinds = new("Inviolate Winds", PaletteTypes.StatusEffects, Color.FromArgb(187, 238, 170));
        public ColorMapping StatusHolyBonds = new("Holy Bonds", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 238));
        public ColorMapping StatusPurgatoryWinds = new("Purgatory Winds", PaletteTypes.StatusEffects, Color.FromArgb(187, 238, 170));
        public ColorMapping StatusHolyPurgation = new("Holy Purgation", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 238));
        public ColorMapping StatusOverload = new("Overload", PaletteTypes.StatusEffects, Color.FromArgb(187, 102, 153));
        public ColorMapping StatusGlossomorph = new("Glossomorph", PaletteTypes.StatusEffects, Color.FromArgb(68, 34, 51));
        public ColorMapping StatusChelomorph = new("Chelomorph", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 85));
        public ColorMapping StatusOutOfControl = new("Out of Control", PaletteTypes.StatusEffects, Color.FromArgb(12, 29, 20));
        public ColorMapping StatusConsumption = new("Consumption", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 68));
        public ColorMapping StatusBodilyManipulation = new("Bodily Manipulation", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusGlossalResistanceDown = new("Glossal Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 17));
        public ColorMapping StatusChelicResistanceDown = new("Chelic Resistance Down", PaletteTypes.StatusEffects, Color.FromArgb(17, 51, 34));
        public ColorMapping StatusAetheronecrosis = new("Aetheronecrosis", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 119));
        public ColorMapping StatusDarkResistanceDownII = new("Dark Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 238));
        public ColorMapping StatusBloodOfTheGorgon = new("Blood of the Gorgon", PaletteTypes.StatusEffects, Color.FromArgb(17, 34, 17));
        public ColorMapping StatusBreathOfTheGorgon = new("Breath of the Gorgon", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 51));
        public ColorMapping StatusSpringTide = new("Spring Tide", PaletteTypes.StatusEffects, Color.FromArgb(119, 85, 136));
        public ColorMapping StatusNeapTide = new("Neap Tide", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 34));
        public ColorMapping StatusImperfectionAlpha = new("Imperfection: Alpha", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 204));
        public ColorMapping StatusImperfectionBeta = new("Imperfection: Beta", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 0));
        public ColorMapping StatusImperfectionGamma = new("Imperfection: Gamma", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusInconceivable = new("Inconceivable", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusFieryConception = new("Fiery Conception", PaletteTypes.StatusEffects, Color.FromArgb(136, 34, 17));
        public ColorMapping StatusToxicConception = new("Toxic Conception", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 34));
        public ColorMapping StatusGrowingConception = new("Growing Conception", PaletteTypes.StatusEffects, Color.FromArgb(153, 102, 34));
        public ColorMapping StatusSolosplice = new("Solosplice", PaletteTypes.StatusEffects, Color.FromArgb(153, 68, 85));
        public ColorMapping StatusMultisplice = new("Multisplice", PaletteTypes.StatusEffects, Color.FromArgb(204, 170, 68));
        public ColorMapping StatusSupersplice = new("Supersplice", PaletteTypes.StatusEffects, Color.FromArgb(102, 136, 204));
        public ColorMapping StatusTargeting = new("Targeting", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusInverseMagicks = new("Inverse Magicks", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusSoulStranded = new("Soul Stranded", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 34));
        public ColorMapping StatusEyeOfTheGorgon = new("Eye of the Gorgon", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 17));
        public ColorMapping StatusCrownOfTheGorgon = new("Crown of the Gorgon", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 17));
        public ColorMapping StatusArcaneAttraction = new("Arcane Attraction", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 170));
        public ColorMapping StatusAttractionReversed = new("Attraction Reversed", PaletteTypes.StatusEffects, Color.FromArgb(0, 119, 119));
        public ColorMapping StatusArcaneFever = new("Arcane Fever", PaletteTypes.StatusEffects, Color.FromArgb(221, 187, 68));
        public ColorMapping StatusFeverReversed = new("Fever Reversed", PaletteTypes.StatusEffects, Color.FromArgb(34, 119, 153));
        public ColorMapping StatusSoulOfFire = new("Soul of Fire", PaletteTypes.StatusEffects, Color.FromArgb(102, 0, 0));
        public ColorMapping StatusSoulOfIce = new("Soul of Ice", PaletteTypes.StatusEffects, Color.FromArgb(204, 238, 238));
        public ColorMapping StatusNaturalAlignment = new("Natural Alignment", PaletteTypes.StatusEffects, Color.FromArgb(68, 68, 204));
        public ColorMapping StatusGuidedMissileKyriosIncoming = new("Guided Missile Kyrios Incoming", PaletteTypes.StatusEffects, Color.FromArgb(102, 51, 51));
        public ColorMapping StatusSniperCannonFodder = new("Sniper Cannon Fodder", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusHighPoweredSniperCannonFodder = new("High-powered Sniper Cannon Fodder", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusMidGlitch = new("Mid Glitch", PaletteTypes.StatusEffects, Color.FromArgb(137, 136, 133));
        public ColorMapping StatusRemoteGlitch = new("Remote Glitch", PaletteTypes.StatusEffects, Color.FromArgb(70, 70, 70));
        public ColorMapping StatusCriticalPerformanceBug = new("Critical Performance Bug", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 255));
        public ColorMapping StatusPerformanceDebugger = new("Performance Debugger", PaletteTypes.StatusEffects, Color.FromArgb(136, 136, 136));
        public ColorMapping StatusLatentSynchronizationBug = new("Latent Synchronization Bug", PaletteTypes.StatusEffects, Color.FromArgb(102, 136, 119));
        public ColorMapping StatusLatentPerformanceDefect = new("Latent Performance Defect", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 255));
        public ColorMapping StatusSynchronizationCodeSmell = new("Synchronization Code Smell", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusOverflowCodeSmell = new("Overflow Code Smell", PaletteTypes.StatusEffects, Color.FromArgb(102, 102, 136));
        public ColorMapping StatusUnderflowCodeSmell = new("Underflow Code Smell", PaletteTypes.StatusEffects, Color.FromArgb(85, 102, 136));
        public ColorMapping StatusPerformanceCodeSmell = new("Performance Code Smell", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping StatusLocalCodeSmell = new("Local Code Smell", PaletteTypes.StatusEffects, Color.FromArgb(187, 170, 170));
        public ColorMapping StatusRemoteCodeSmell = new("Remote Code Smell", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 204));
        public ColorMapping StatusHelloNearWorld = new("Hello, Near World", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 17));
        public ColorMapping StatusHelloDistantWorld = new("Hello, Distant World", PaletteTypes.StatusEffects, Color.FromArgb(51, 136, 136));
        public ColorMapping StatusFourthInLine = new("Fourth in Line", PaletteTypes.StatusEffects, Color.FromArgb(221, 85, 187));
        public ColorMapping StatusOversampledWaveCannonLoading = new("Oversampled Wave Cannon Loading", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusVegetalVapours = new("Vegetal Vapours", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 170));
        public ColorMapping StatusPenance = new("Penance", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 204));
        public ColorMapping StatusPenitentSShackles = new("Penitent's Shackles", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusBloomingWelt = new("Blooming Welt", PaletteTypes.StatusEffects, Color.FromArgb(102, 17, 17));
        public ColorMapping StatusFuriousWelt = new("Furious Welt", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusStingingWelt = new("Stinging Welt", PaletteTypes.StatusEffects, Color.FromArgb(119, 34, 17));
        public ColorMapping StatusFlamespire = new("Flamespire", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 255));
        public ColorMapping StatusMarkOfTheHarvest = new("Mark of the Harvest", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 17));
        public ColorMapping StatusDemiclonePenalty = new("Demiclone Penalty", PaletteTypes.StatusEffects, Color.FromArgb(102, 51, 34));
        public ColorMapping StatusOwlet = new("Owlet", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusAncientFrost = new("Ancient Frost", PaletteTypes.StatusEffects, Color.FromArgb(119, 221, 255));
        public ColorMapping StatusCondensedWaveCannonKyrios = new("Condensed Wave Cannon Kyrios", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusGlassyEyed = new("Glassy-eyed", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusMini = new("Mini", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 17));
        public ColorMapping StatusMirroredSmoke = new("Mirrored Smoke", PaletteTypes.StatusEffects, Color.FromArgb(170, 153, 187));
        public ColorMapping StatusMagicNumber = new("Magic Number", PaletteTypes.StatusEffects, Color.FromArgb(0, 102, 119));
        public ColorMapping StatusDarkWhispers = new("Dark Whispers", PaletteTypes.StatusEffects, Color.FromArgb(204, 170, 255));
        public ColorMapping StatusBindingSoulSnare = new("Binding Soul Snare", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 68));
        public ColorMapping StatusHeavySoulSnare = new("Heavy Soul Snare", PaletteTypes.StatusEffects, Color.FromArgb(102, 102, 102));
        public ColorMapping StatusIncapacitatingSoulSnare = new("Incapacitating Soul Snare", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 68));
        public ColorMapping StatusDMoniacBonds = new("Dæmoniac Bonds", PaletteTypes.StatusEffects, Color.FromArgb(119, 85, 153));
        public ColorMapping StatusDuodMoniacBonds = new("Duodæmoniac Bonds", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 136));
        public ColorMapping StatusLightSAccord = new("Light's Accord", PaletteTypes.StatusEffects, Color.FromArgb(68, 238, 255));
        public ColorMapping StatusDarkSAccord = new("Dark's Accord", PaletteTypes.StatusEffects, Color.FromArgb(85, 238, 255));
        public ColorMapping StatusLightSDiscord = new("Light's Discord", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 119));
        public ColorMapping StatusDarkSDiscord = new("Dark's Discord", PaletteTypes.StatusEffects, Color.FromArgb(204, 255, 255));
        public ColorMapping StatusPolarizing = new("Polarizing", PaletteTypes.StatusEffects, Color.FromArgb(102, 170, 238));
        public ColorMapping StatusAlphaTarget = new("Alpha Target", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 204));
        public ColorMapping StatusBetaTarget = new("Beta Target", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusSystemError = new("System Error", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusScatteredWailing = new("Scattered Wailing", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 68));
        public ColorMapping StatusIntensifiedWailing = new("Intensified Wailing", PaletteTypes.StatusEffects, Color.FromArgb(136, 68, 187));
        public ColorMapping StatusWrathfulRevelation = new("Wrathful Revelation", PaletteTypes.StatusEffects, Color.FromArgb(255, 170, 68));
        public ColorMapping StatusDelightfulRevelation = new("Delightful Revelation", PaletteTypes.StatusEffects, Color.FromArgb(51, 85, 153));
        public ColorMapping StatusFlamesOfEventide = new("Flames of Eventide", PaletteTypes.StatusEffects, Color.FromArgb(119, 51, 136));
        public ColorMapping StatusUmbralTilt = new("Umbral Tilt", PaletteTypes.StatusEffects, Color.FromArgb(85, 153, 221));
        public ColorMapping StatusAstralTilt = new("Astral Tilt", PaletteTypes.StatusEffects, Color.FromArgb(136, 0, 0));
        public ColorMapping StatusHeavensflameSoul = new("Heavensflame Soul", PaletteTypes.StatusEffects, Color.FromArgb(153, 68, 68));
        public ColorMapping StatusUmbralbrightSoul = new("Umbralbright Soul", PaletteTypes.StatusEffects, Color.FromArgb(102, 153, 221));
        public ColorMapping StatusAstralbrightSoul = new("Astralbright Soul", PaletteTypes.StatusEffects, Color.FromArgb(102, 17, 17));
        public ColorMapping StatusUmbralstrongSoul = new("Umbralstrong Soul", PaletteTypes.StatusEffects, Color.FromArgb(221, 238, 255));
        public ColorMapping StatusAstralstrongSoul = new("Astralstrong Soul", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusQuarteredSoul = new("Quartered Soul", PaletteTypes.StatusEffects, Color.FromArgb(119, 255, 255));
        public ColorMapping StatusXMarkedSoul = new("X-marked Soul", PaletteTypes.StatusEffects, Color.FromArgb(170, 255, 255));
        public ColorMapping StatusEnchainedSoul = new("Enchained Soul", PaletteTypes.StatusEffects, Color.FromArgb(153, 153, 153));
        public ColorMapping StatusAscended = new("Ascended", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 68));
        public ColorMapping StatusMissingLink = new("Missing Link", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 170));
        public ColorMapping StatusCloseCaloric = new("Close Caloric", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 51));
        public ColorMapping StatusPyrefaction = new("Pyrefaction", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 136));
        public ColorMapping StatusAtmosfaction = new("Atmosfaction", PaletteTypes.StatusEffects, Color.FromArgb(34, 255, 238));
        public ColorMapping StatusEntropifaction = new("Entropifaction", PaletteTypes.StatusEffects, Color.FromArgb(187, 68, 34));
        public ColorMapping StatusUnstableFactor = new("Unstable Factor", PaletteTypes.StatusEffects, Color.FromArgb(34, 238, 238));
        public ColorMapping StatusCriticalFactor = new("Critical Factor", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 102));
        public ColorMapping StatusRodentialRebirth = new("Rodential Rebirth", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 187));
        public ColorMapping StatusOdderIncarnation = new("Odder Incarnation", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping StatusSquirrellyPrayer = new("Squirrelly Prayer", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusOdderPrayer = new("Odder Prayer", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 187));
        public ColorMapping StatusLiveBrazier = new("Live Brazier", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 0));
        public ColorMapping StatusLiveCandle = new("Live Candle", PaletteTypes.StatusEffects, Color.FromArgb(204, 34, 0));
        public ColorMapping StatusRatAndMouse = new("Rat and Mouse", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 51));
        public ColorMapping StatusVengefulFlame = new("Vengeful Flame", PaletteTypes.StatusEffects, Color.FromArgb(204, 34, 0));
        public ColorMapping StatusVengefulPyre = new("Vengeful Pyre", PaletteTypes.StatusEffects, Color.FromArgb(51, 0, 0));
        public ColorMapping StatusEntropyResistance = new("Entropy Resistance", PaletteTypes.StatusEffects, Color.FromArgb(119, 51, 34));
        public ColorMapping StatusStableSystem = new("Stable System", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusHoofingIt = new("Hoofing It", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusBegrimed = new("Begrimed", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 170));
        public ColorMapping StatusCandyCane = new("Candy Cane", PaletteTypes.StatusEffects, Color.FromArgb(51, 85, 0));
        public ColorMapping StatusNoxiousGnash = new("Noxious Gnash", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 68));
        public ColorMapping StatusSlipperyGround = new("Slippery Ground", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusFourfoldComeRuin = new("Fourfold-come Ruin", PaletteTypes.StatusEffects, Color.FromArgb(170, 34, 255));
        public ColorMapping StatusTetradMoniacBonds = new("Tetradæmoniac Bonds", PaletteTypes.StatusEffects, Color.FromArgb(119, 85, 153));
        public ColorMapping StatusSlipperySlope = new("Slippery Slope", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusBreathOfMagic = new("Breath of Magic", PaletteTypes.StatusEffects, Color.FromArgb(238, 204, 204));
        public ColorMapping StatusSticky = new("Sticky", PaletteTypes.StatusEffects, Color.FromArgb(204, 170, 102));
        public ColorMapping StatusTimesThree = new("Times Three", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 51));
        public ColorMapping StatusSurgeVector = new("Surge Vector", PaletteTypes.StatusEffects, Color.FromArgb(119, 119, 238));
        public ColorMapping StatusSubtractiveSuppressorAlpha = new("Subtractive Suppressor Alpha", PaletteTypes.StatusEffects, Color.FromArgb(238, 119, 34));
        public ColorMapping StatusSubtractiveSuppressorBeta = new("Subtractive Suppressor Beta", PaletteTypes.StatusEffects, Color.FromArgb(238, 119, 34));
        public ColorMapping StatusInscribed = new("Inscribed", PaletteTypes.StatusEffects, Color.FromArgb(136, 238, 68));
        public ColorMapping StatusBrightPulse = new("Bright Pulse", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 68));
        public ColorMapping StatusBullSEye = new("Bull's-eye", PaletteTypes.StatusEffects, Color.FromArgb(119, 187, 187));
        public ColorMapping StatusBubbleWeave = new("Bubble Weave", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusBubbleNet = new("Bubble Net", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 34));
        public ColorMapping StatusBubbleGaol = new("Bubble Gaol", PaletteTypes.StatusEffects, Color.FromArgb(51, 119, 170));
        public ColorMapping StatusHydrofallTarget = new("Hydrofall Target", PaletteTypes.StatusEffects, Color.FromArgb(51, 153, 255));
        public ColorMapping StatusHydrobulletTarget = new("Hydrobullet Target", PaletteTypes.StatusEffects, Color.FromArgb(204, 204, 255));
        public ColorMapping StatusHeatWave = new("Heat Wave", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBigBounce = new("Big Bounce", PaletteTypes.StatusEffects, Color.FromArgb(119, 17, 17));
        public ColorMapping StatusDivisiveDark = new("Divisive Dark", PaletteTypes.StatusEffects, Color.FromArgb(68, 34, 204));
        public ColorMapping StatusBeckoningDark = new("Beckoning Dark", PaletteTypes.StatusEffects, Color.FromArgb(102, 51, 136));
        public ColorMapping StatusDoubledDark = new("Doubled Dark", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 153));
        public ColorMapping StatusGloweringDark = new("Glowering Dark", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBindingDark = new("Binding Dark", PaletteTypes.StatusEffects, Color.FromArgb(187, 136, 255));
        public ColorMapping StatusBondsOfDarkness = new("Bonds of Darkness", PaletteTypes.StatusEffects, Color.FromArgb(136, 34, 238));
        public ColorMapping StatusGravitationalAnomaly = new("Gravitational Anomaly", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 187));
        public ColorMapping StatusFoamyFetters = new("Foamy Fetters", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 0));
        public ColorMapping StatusTimesFive = new("Times Five", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 51));
        public ColorMapping StatusTrauma = new("Trauma", PaletteTypes.StatusEffects, Color.FromArgb(153, 85, 17));
        public ColorMapping StatusDirectionalDisregard = new("Directional Disregard", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 34));
        public ColorMapping StatusSeedCrystals = new("Seed Crystals", PaletteTypes.StatusEffects, Color.FromArgb(85, 102, 153));
        public ColorMapping StatusCrystalBurden = new("Crystal Burden", PaletteTypes.StatusEffects, Color.FromArgb(85, 102, 153));
        public ColorMapping StatusCrystallized = new("Crystallized", PaletteTypes.StatusEffects, Color.FromArgb(85, 102, 153));
        public ColorMapping StatusAuthoritySGaze = new("Authority's Gaze", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping StatusCalamitySFlames = new("Calamity's Flames", PaletteTypes.StatusEffects, Color.FromArgb(204, 102, 0));
        public ColorMapping StatusCalamitySInferno = new("Calamity's Inferno", PaletteTypes.StatusEffects, Color.FromArgb(255, 68, 34));
        public ColorMapping StatusCalamitySEmbers = new("Calamity's Embers", PaletteTypes.StatusEffects, Color.FromArgb(94, 38, 23));
        public ColorMapping StatusCalamitySFrost = new("Calamity's Frost", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 255));
        public ColorMapping StatusCalamitySBite = new("Calamity's Bite", PaletteTypes.StatusEffects, Color.FromArgb(4, 85, 83));
        public ColorMapping StatusCalamitySChill = new("Calamity's Chill", PaletteTypes.StatusEffects, Color.FromArgb(51, 153, 187));
        public ColorMapping StatusCalamitySBolt = new("Calamity's Bolt", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusCalamitySFulgur = new("Calamity's Fulgur", PaletteTypes.StatusEffects, Color.FromArgb(76, 18, 79));
        public ColorMapping StatusGreatestCurse = new("Greatest Curse", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 17));
        public ColorMapping StatusDokumori = new("Dokumori", PaletteTypes.StatusEffects, Color.FromArgb(170, 153, 153));
        public ColorMapping StatusHighThunder = new("High Thunder", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 255));
        public ColorMapping StatusBanefulImpaction = new("Baneful Impaction", PaletteTypes.StatusEffects, Color.FromArgb(187, 204, 255));
        public ColorMapping StatusEukrasianDyskrasia = new("Eukrasian Dyskrasia", PaletteTypes.StatusEffects, Color.FromArgb(204, 102, 238));
        public ColorMapping StatusKunaiSBane = new("Kunai's Bane", PaletteTypes.StatusEffects, Color.FromArgb(51, 51, 68));
        public ColorMapping StatusInfatuated = new("Infatuated", PaletteTypes.StatusEffects, Color.FromArgb(44, 37, 37));
        public ColorMapping StatusHeadOverHeels = new("Head Over Heels", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 221));
        public ColorMapping StatusHopelessDevotion = new("Hopeless Devotion", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 221));
        public ColorMapping StatusFatalAttraction = new("Fatal Attraction", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 221));
        public ColorMapping StatusDrone = new("Drone", PaletteTypes.StatusEffects, Color.FromArgb(79, 0, 85));
        public ColorMapping StatusHoneyBeeMine = new("Honey Bee Mine", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 238));
        public ColorMapping StatusBeelovedVenom = new("Beeloved Venom: α", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 255));
        public ColorMapping StatusBeelovedVenom2 = new("Beeloved Venom: β", PaletteTypes.StatusEffects, Color.FromArgb(47, 91, 68));
        public ColorMapping StatusPoisonNPop = new("Poison 'n' Pop", PaletteTypes.StatusEffects, Color.FromArgb(103, 68, 65));
        public ColorMapping StatusPoisonResistanceDownII = new("Poison Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusDelayedNeurotoxicity = new("Delayed Neurotoxicity", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 0));
        public ColorMapping StatusThunderOfTheRroneek = new("Thunder of the Rroneek", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 170));
        public ColorMapping StatusBenoggined = new("Benoggined", PaletteTypes.StatusEffects, Color.FromArgb(17, 187, 204));
        public ColorMapping StatusNuisance = new("Nuisance", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 34));
        public ColorMapping StatusOvermedicated = new("Overmedicated", PaletteTypes.StatusEffects, Color.FromArgb(136, 119, 119));
        public ColorMapping StatusDelusions = new("Delusions", PaletteTypes.StatusEffects, Color.FromArgb(34, 51, 102));
        public ColorMapping StatusNumbingCurrent = new("Numbing Current", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 119));
        public ColorMapping StatusFrogtourageFan = new("Frogtourage Fan", PaletteTypes.StatusEffects, Color.FromArgb(17, 0, 17));
        public ColorMapping StatusElectricalCondenser = new("Electrical Condenser", PaletteTypes.StatusEffects, Color.FromArgb(102, 68, 187));
        public ColorMapping StatusPositron = new("Positron", PaletteTypes.StatusEffects, Color.FromArgb(255, 221, 136));
        public ColorMapping StatusNegatron = new("Negatron", PaletteTypes.StatusEffects, Color.FromArgb(102, 204, 238));
        public ColorMapping StatusRemoteCurrent = new("Remote Current", PaletteTypes.StatusEffects, Color.FromArgb(170, 221, 17));
        public ColorMapping StatusProximateCurrent = new("Proximate Current", PaletteTypes.StatusEffects, Color.FromArgb(0, 255, 255));
        public ColorMapping StatusSpinningConductor = new("Spinning Conductor", PaletteTypes.StatusEffects, Color.FromArgb(68, 187, 204));
        public ColorMapping StatusRoundhouseConductor = new("Roundhouse Conductor", PaletteTypes.StatusEffects, Color.FromArgb(85, 85, 204));
        public ColorMapping StatusColliderConductor = new("Collider Conductor", PaletteTypes.StatusEffects, Color.FromArgb(238, 187, 255));
        public ColorMapping StatusMustardBomb = new("Mustard Bomb", PaletteTypes.StatusEffects, Color.FromArgb(238, 170, 68));
        public ColorMapping StatusMustardBombproof = new("Mustard Bombproof", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 17));
        public ColorMapping StatusChainDeathmatch = new("Chain Deathmatch", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusBombarium = new("Bombarium", PaletteTypes.StatusEffects, Color.FromArgb(238, 0, 238));
        public ColorMapping StatusRightwardFracture = new("Rightward Fracture", PaletteTypes.StatusEffects, Color.FromArgb(85, 0, 0));
        public ColorMapping StatusLeftwardFracture = new("Leftward Fracture", PaletteTypes.StatusEffects, Color.FromArgb(85, 0, 0));
        public ColorMapping StatusBackwardFracture = new("Backward Fracture", PaletteTypes.StatusEffects, Color.FromArgb(85, 0, 0));
        public ColorMapping StatusProjection = new("Projection", PaletteTypes.StatusEffects, Color.FromArgb(221, 136, 119));
        public ColorMapping StatusTheNarwhalSLullaby = new("The Narwhal's Lullaby", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 204));
        public ColorMapping StatusClawedMuse = new("Clawed Muse", PaletteTypes.StatusEffects, Color.FromArgb(221, 153, 0));
        public ColorMapping StatusFangedMuse = new("Fanged Muse", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 153));
        public ColorMapping StatusPerpetualConflagration = new("Perpetual Conflagration", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 34));
        public ColorMapping StatusAuthoritySHold = new("Authority's Hold", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusHeatstroke = new("Heatstroke", PaletteTypes.StatusEffects, Color.FromArgb(34, 17, 17));
        public ColorMapping StatusColdSweats = new("Cold Sweats", PaletteTypes.StatusEffects, Color.FromArgb(34, 119, 170));
        public ColorMapping StatusChainsOfEverlastingLight = new("Chains of Everlasting Light", PaletteTypes.StatusEffects, Color.FromArgb(170, 170, 0));
        public ColorMapping StatusCurseOfEverlastingLight = new("Curse of Everlasting Light", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 0));
        public ColorMapping StatusTheWeightOfLight = new("The Weight of Light", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusLightSDesign = new("Light's Design", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 238));
        public ColorMapping StatusDarknessSDesign = new("Darkness's Design", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 102));
        public ColorMapping StatusLightResistanceDownII = new("Light Resistance Down II", PaletteTypes.StatusEffects, Color.FromArgb(221, 170, 153));
        public ColorMapping StatusFatedBurnMark = new("Fated Burn Mark", PaletteTypes.StatusEffects, Color.FromArgb(221, 170, 68));
        public ColorMapping StatusPowderMarkTrail = new("Powder Mark Trail", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 68));
        public ColorMapping StatusSpellInWaitingReturnII = new("Spell-in-Waiting: Return II", PaletteTypes.StatusEffects, Color.FromArgb(0, 17, 34));
        public ColorMapping StatusReturnII = new("Return II", PaletteTypes.StatusEffects, Color.FromArgb(255, 102, 255));
        public ColorMapping StatusAdamantScales = new("Adamant Scales", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 17));
        public ColorMapping StatusInnerDarkness = new("Inner Darkness", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 17));
        public ColorMapping StatusOuterDarkness = new("Outer Darkness", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 187));
        public ColorMapping StatusDeadlyEmbrace = new("Deadly Embrace", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 0));
        public ColorMapping StatusSpitefulFlames = new("Spiteful Flames", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 221));
        public ColorMapping StatusBondsOfLoathing = new("Bonds of Loathing", PaletteTypes.StatusEffects, Color.FromArgb(119, 187, 102));
        public ColorMapping StatusAuthoritySExpansion = new("Authority's Expansion", PaletteTypes.StatusEffects, Color.FromArgb(17, 68, 85));
        public ColorMapping StatusAuthoritySBoot = new("Authority's Boot", PaletteTypes.StatusEffects, Color.FromArgb(238, 238, 255));
        public ColorMapping StatusAuthoritySHeel = new("Authority's Heel", PaletteTypes.StatusEffects, Color.FromArgb(187, 187, 170));
        public ColorMapping StatusEastWindOfChange = new("East Wind of Change", PaletteTypes.StatusEffects, Color.FromArgb(170, 136, 85));
        public ColorMapping StatusWestWindOfChange = new("West Wind of Change", PaletteTypes.StatusEffects, Color.FromArgb(136, 170, 170));
        public ColorMapping StatusEpicHero = new("Epic Hero", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 204));
        public ColorMapping StatusFatedHero = new("Fated Hero", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 0));
        public ColorMapping StatusVauntedHero = new("Vaunted Hero", PaletteTypes.StatusEffects, Color.FromArgb(34, 102, 221));
        public ColorMapping StatusPlayingWithFire = new("Playing with Fire", PaletteTypes.StatusEffects, Color.FromArgb(221, 51, 51));
        public ColorMapping StatusPlayingWithIce = new("Playing with Ice", PaletteTypes.StatusEffects, Color.FromArgb(68, 204, 255));
        public ColorMapping StatusPyromania = new("Pyromania", PaletteTypes.StatusEffects, Color.FromArgb(221, 51, 51));
        public ColorMapping StatusCryomania = new("Cryomania", PaletteTypes.StatusEffects, Color.FromArgb(68, 204, 255));
        public ColorMapping StatusArcaneStop = new("Arcane Stop", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 221));
        public ColorMapping StatusHeroSLament = new("Hero's Lament", PaletteTypes.StatusEffects, Color.FromArgb(85, 68, 51));
        public ColorMapping StatusOccultMageMasher = new("Occult Mage Masher", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusResurrectionDenied = new("Resurrection Denied", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusSilverSickness = new("Silver Sickness", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 221));
        public ColorMapping StatusFalsePrediction = new("False Prediction", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusPhantomDoom = new("Phantom Doom", PaletteTypes.StatusEffects, Color.FromArgb(153, 17, 34));
        public ColorMapping StatusWeaponPilfered = new("Weapon Pilfered", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 51));
        public ColorMapping StatusShieldSmite = new("Shield Smite", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusDebana = new("Debana", PaletteTypes.StatusEffects, Color.FromArgb(204, 0, 0));
        public ColorMapping StatusScorch = new("Scorch", PaletteTypes.StatusEffects, Color.FromArgb(51, 68, 85));
        public ColorMapping StatusLethargy = new("Lethargy", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 34));
        public ColorMapping StatusWickedWater = new("Wicked Water", PaletteTypes.StatusEffects, Color.FromArgb(48, 39, 44));
        public ColorMapping StatusGelidGaol = new("Gelid Gaol", PaletteTypes.StatusEffects, Color.FromArgb(68, 153, 204));
        public ColorMapping StatusPreyLesserAxebit = new("Prey: Lesser Axebit", PaletteTypes.StatusEffects, Color.FromArgb(221, 187, 51));
        public ColorMapping StatusPreyGreaterAxebit = new("Prey: Greater Axebit", PaletteTypes.StatusEffects, Color.FromArgb(136, 102, 34));
        public ColorMapping StatusPreyLancepoint = new("Prey: Lancepoint", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBuyerSRemorse = new("Buyer's Remorse", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 51));
        public ColorMapping StatusCursedChange = new("Cursed Change", PaletteTypes.StatusEffects, Color.FromArgb(221, 187, 68));
        public ColorMapping StatusBuyBuyBuy = new("Buy! Buy! Buy!", PaletteTypes.StatusEffects, Color.FromArgb(119, 221, 34));
        public ColorMapping StatusDefyingGravity = new("Defying Gravity", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 221));
        public ColorMapping StatusCraterLater = new("Crater Later", PaletteTypes.StatusEffects, Color.FromArgb(153, 17, 17));
        public ColorMapping StatusValuedCustomer = new("Valued Customer", PaletteTypes.StatusEffects, Color.FromArgb(17, 34, 51));
        public ColorMapping StatusWindpack = new("Windpack", PaletteTypes.StatusEffects, Color.FromArgb(153, 204, 102));
        public ColorMapping StatusStonepack = new("Stonepack", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 34));
        public ColorMapping StatusEarthborneEnd = new("Earthborne End", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 102));
        public ColorMapping StatusWindborneEnd = new("Windborne End", PaletteTypes.StatusEffects, Color.FromArgb(136, 238, 119));
        public ColorMapping StatusTerrestrialChains = new("Terrestrial Chains", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 119));
        public ColorMapping StatusPatienceOfWind = new("Patience of Wind", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 170));
        public ColorMapping StatusPatienceOfStone = new("Patience of Stone", PaletteTypes.StatusEffects, Color.FromArgb(238, 170, 85));
        public ColorMapping StatusLamentOfTheClose = new("Lament of the Close", PaletteTypes.StatusEffects, Color.FromArgb(170, 238, 0));
        public ColorMapping StatusLamentOfTheDistant = new("Lament of the Distant", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 51));
        public ColorMapping StatusTritonicGravity = new("Tritonic Gravity", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 153));
        public ColorMapping StatusNereidicGravity = new("Nereidic Gravity", PaletteTypes.StatusEffects, Color.FromArgb(68, 0, 153));
        public ColorMapping StatusPhobosicGravity = new("Phobosic Gravity", PaletteTypes.StatusEffects, Color.FromArgb(204, 187, 170));
        public ColorMapping StatusNovaOoze = new("Nova Ooze", PaletteTypes.StatusEffects, Color.FromArgb(221, 102, 34));
        public ColorMapping StatusIceOoze = new("Ice Ooze", PaletteTypes.StatusEffects, Color.FromArgb(51, 102, 221));
        public ColorMapping StatusToxicMinerals = new("Toxic Minerals", PaletteTypes.StatusEffects, Color.FromArgb(85, 34, 0));
        public ColorMapping StatusShortFuse = new("Short Fuse", PaletteTypes.StatusEffects, Color.FromArgb(204, 153, 68));
        public ColorMapping StatusLongFuse = new("Long Fuse", PaletteTypes.StatusEffects, Color.FromArgb(85, 170, 85));
        public ColorMapping StatusBurningUp = new("Burning Up", PaletteTypes.StatusEffects, Color.FromArgb(204, 68, 0));
        public ColorMapping StatusSweltering = new("Sweltering", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 119));
        public ColorMapping StatusWingmark = new("Wingmark", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 221));
        public ColorMapping StatusWarmTint = new("Warm Tint", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 170));
        public ColorMapping StatusCoolTint = new("Cool Tint", PaletteTypes.StatusEffects, Color.FromArgb(34, 221, 255));
        public ColorMapping StatusMousseMine = new("Mousse Mine", PaletteTypes.StatusEffects, Color.FromArgb(136, 119, 0));
        public ColorMapping StatusHeatingUp = new("Heating Up", PaletteTypes.StatusEffects, Color.FromArgb(93, 57, 11));
        public ColorMapping StatusArmedToTheTeeth = new("Armed to the Teeth", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBurnBabyBurn = new("Burn Baby Burn", PaletteTypes.StatusEffects, Color.FromArgb(153, 153, 102));
        public ColorMapping StatusWavelength = new("Wavelength α", PaletteTypes.StatusEffects, Color.FromArgb(136, 119, 85));
        public ColorMapping StatusWavelength2 = new("Wavelength β", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 51));
        public ColorMapping StatusThornsOfDeathI = new("Thorns of Death I", PaletteTypes.StatusEffects, Color.FromArgb(119, 68, 68));
        public ColorMapping StatusThornsOfDeathII = new("Thorns of Death II", PaletteTypes.StatusEffects, Color.FromArgb(119, 85, 68));
        public ColorMapping StatusThornsOfDeathIII = new("Thorns of Death III", PaletteTypes.StatusEffects, Color.FromArgb(119, 85, 68));
        public ColorMapping StatusThornsOfDeathIV = new("Thorns of Death IV", PaletteTypes.StatusEffects, Color.FromArgb(170, 51, 51));
        public ColorMapping StatusInTheSpotlight = new("In the Spotlight", PaletteTypes.StatusEffects, Color.FromArgb(153, 85, 0));
        public ColorMapping StatusSpotlightless = new("Spotlightless", PaletteTypes.StatusEffects, Color.FromArgb(85, 51, 136));
        public ColorMapping StatusGravitationalStability = new("Gravitational Stability", PaletteTypes.StatusEffects, Color.FromArgb(255, 153, 51));
        public ColorMapping StatusRampage = new("Rampage", PaletteTypes.StatusEffects, Color.FromArgb(204, 51, 17));
        public ColorMapping StatusRust = new("Rust", PaletteTypes.StatusEffects, Color.FromArgb(85, 17, 153));
        public ColorMapping StatusDiabrosis = new("Diabrosis", PaletteTypes.StatusEffects, Color.FromArgb(34, 102, 255));
        public ColorMapping StatusShadowOfDeath = new("Shadow of Death", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusNowhereToRun = new("Nowhere to Run", PaletteTypes.StatusEffects, Color.FromArgb(17, 17, 17));
        public ColorMapping StatusCellBlock = new("Cell Block α", PaletteTypes.StatusEffects, Color.FromArgb(221, 204, 204));
        public ColorMapping StatusCellBlock2 = new("Cell Block β", PaletteTypes.StatusEffects, Color.FromArgb(68, 51, 0));
        public ColorMapping StatusCellBlock3 = new("Cell Block γ", PaletteTypes.StatusEffects, Color.FromArgb(34, 102, 221));
        public ColorMapping StatusCellBlock4 = new("Cell Block δ", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 238));
        public ColorMapping StatusInsensible = new("Insensible", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 51));
        public ColorMapping StatusDarkVengeance = new("Dark Vengeance", PaletteTypes.StatusEffects, Color.FromArgb(153, 51, 221));
        public ColorMapping StatusLightVengeance = new("Light Vengeance", PaletteTypes.StatusEffects, Color.FromArgb(187, 153, 34));
        public ColorMapping StatusChainsOfCondemnation = new("Chains of Condemnation", PaletteTypes.StatusEffects, Color.FromArgb(255, 238, 153));
        public ColorMapping StatusSearingChains = new("Searing Chains", PaletteTypes.StatusEffects, Color.FromArgb(238, 102, 17));
        public ColorMapping StatusShackledHealing = new("Shackled Healing", PaletteTypes.StatusEffects, Color.FromArgb(238, 136, 0));
        public ColorMapping StatusShackledAbilities = new("Shackled Abilities", PaletteTypes.StatusEffects, Color.FromArgb(136, 17, 17));
        public ColorMapping StatusHellishEarth = new("Hellish Earth", PaletteTypes.StatusEffects, Color.FromArgb(221, 102, 170));
        public ColorMapping StatusSinBearer = new("Sin Bearer", PaletteTypes.StatusEffects, Color.FromArgb(204, 221, 221));
        public ColorMapping StatusSumOfAllSins = new("Sum of All Sins", PaletteTypes.StatusEffects, Color.FromArgb(102, 85, 34));
        public ColorMapping StatusWithoutSin = new("Without Sin", PaletteTypes.StatusEffects, Color.FromArgb(102, 102, 187));
        public ColorMapping StatusGuardianWill = new("Guardian Will", PaletteTypes.StatusEffects, Color.FromArgb(102, 51, 68));
        public ColorMapping StatusProjectionLocus = new("Projection Locus", PaletteTypes.StatusEffects, Color.FromArgb(221, 17, 17));
        public ColorMapping StatusBeyondBeleafed = new("Beyond Beleafed", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 34));
        public ColorMapping StatusAwayWithTheFae = new("Away with the Fae", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 17));
        public ColorMapping StatusIncensePenalty = new("Incense Penalty", PaletteTypes.StatusEffects, Color.FromArgb(187, 102, 51));
        public ColorMapping StatusCurseOfSolitude = new("Curse of Solitude", PaletteTypes.StatusEffects, Color.FromArgb(153, 34, 170));
        public ColorMapping StatusCurseOfCompanionship = new("Curse of Companionship", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusCurseOfImmolation = new("Curse of Immolation", PaletteTypes.StatusEffects, Color.FromArgb(102, 17, 17));
        public ColorMapping StatusStandingFirm = new("Standing Firm", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBreakerOfWind = new("Breaker of Wind", PaletteTypes.StatusEffects, Color.FromArgb(153, 68, 51));
        public ColorMapping StatusDesignatedConductor = new("Designated Conductor", PaletteTypes.StatusEffects, Color.FromArgb(136, 34, 34));
        public ColorMapping StatusMorbidMeal = new("Morbid Meal", PaletteTypes.StatusEffects, Color.FromArgb(102, 119, 153));
        public ColorMapping StatusNearShoreShackles = new("Near Shore Shackles", PaletteTypes.StatusEffects, Color.FromArgb(0, 17, 17));
        public ColorMapping StatusFarShoreShackles = new("Far Shore Shackles", PaletteTypes.StatusEffects, Color.FromArgb(17, 51, 17));
        public ColorMapping StatusTidalspoutTarget = new("Tidalspout Target", PaletteTypes.StatusEffects, Color.FromArgb(51, 153, 255));
        public ColorMapping StatusCurseOfTheBombpyre = new("Curse of the Bombpyre", PaletteTypes.StatusEffects, Color.FromArgb(119, 0, 119));
        public ColorMapping StatusHellAwaits = new("Hell Awaits", PaletteTypes.StatusEffects, Color.FromArgb(187, 0, 34));
        public ColorMapping StatusHellInACell = new("Hell in a Cell", PaletteTypes.StatusEffects, Color.FromArgb(170, 34, 68));
        public ColorMapping StatusFleshForward = new("Flesh Forward", PaletteTypes.StatusEffects, Color.FromArgb(153, 170, 153));
        public ColorMapping StatusFleshBack = new("Flesh Back", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusBurstingGrotesquerie = new("Bursting Grotesquerie", PaletteTypes.StatusEffects, Color.FromArgb(187, 119, 0));
        public ColorMapping StatusSharedGrotesquerie = new("Shared Grotesquerie", PaletteTypes.StatusEffects, Color.FromArgb(255, 170, 119));
        public ColorMapping StatusDirectedGrotesquerie = new("Directed Grotesquerie", PaletteTypes.StatusEffects, Color.FromArgb(136, 17, 255));
        public ColorMapping StatusBondsOfFlesh = new("Bonds of Flesh α", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusUnbreakableFlesh = new("Unbreakable Flesh α", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 0));
        public ColorMapping StatusBondsOfFlesh2 = new("Bonds of Flesh β", PaletteTypes.StatusEffects, Color.FromArgb(34, 34, 0));
        public ColorMapping StatusUnbreakableFlesh2 = new("Unbreakable Flesh β", PaletteTypes.StatusEffects, Color.FromArgb(255, 187, 0));
        public ColorMapping StatusFantasticalRegeneration = new("Fantastical Regeneration", PaletteTypes.StatusEffects, Color.FromArgb(119, 187, 34));
        public ColorMapping StatusRottingFlesh = new("Rotting Flesh", PaletteTypes.StatusEffects, Color.FromArgb(187, 0, 204));
        public ColorMapping StatusMitoticPhase = new("Mitotic Phase", PaletteTypes.StatusEffects, Color.FromArgb(170, 187, 51));
        public ColorMapping StatusLindwurmSPortent = new("Lindwurm's Portent", PaletteTypes.StatusEffects, Color.FromArgb(153, 170, 68));
        public ColorMapping StatusFarawayPortent = new("Faraway Portent", PaletteTypes.StatusEffects, Color.FromArgb(85, 119, 238));
        public ColorMapping StatusNearbyPortent = new("Nearby Portent", PaletteTypes.StatusEffects, Color.FromArgb(255, 85, 102));
        public ColorMapping StatusHotBlooded = new("Hot-blooded", PaletteTypes.StatusEffects, Color.FromArgb(119, 51, 17));
        public ColorMapping StatusMutation = new("Mutation α", PaletteTypes.StatusEffects, Color.FromArgb(68, 68, 68));
        public ColorMapping StatusMutatingCells = new("Mutating Cells", PaletteTypes.StatusEffects, Color.FromArgb(170, 238, 255));
        public ColorMapping StatusMutation2 = new("Mutation β", PaletteTypes.StatusEffects, Color.FromArgb(34, 68, 102));
        public ColorMapping StatusFateOfTheWurm = new("Fate of the Wurm", PaletteTypes.StatusEffects, Color.FromArgb(221, 221, 119));
        public ColorMapping StatusMaleficE = new("Malefic E", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficW = new("Malefic W", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficEW = new("Malefic E-W", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficS = new("Malefic S", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficSE = new("Malefic S-E", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficSW = new("Malefic S-W", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficSEW = new("Malefic S-E-W", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficN = new("Malefic N", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficNE = new("Malefic N-E", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusMaleficNW = new("Malefic N-W", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficNEW = new("Malefic N-E-W", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusMaleficNS = new("Malefic N-S", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficNSE = new("Malefic N-S-E", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusMaleficNSW = new("Malefic N-S-W", PaletteTypes.StatusEffects, Color.FromArgb(51, 17, 0));
        public ColorMapping StatusMaleficNSEW = new("Malefic N-S-E-W", PaletteTypes.StatusEffects, Color.FromArgb(68, 17, 17));
        public ColorMapping StatusBlazingBane = new("Blazing Bane", PaletteTypes.StatusEffects, Color.FromArgb(170, 34, 0));
        public ColorMapping StatusEnamored = new("Enamored", PaletteTypes.StatusEffects, Color.FromArgb(255, 85, 153));
        public ColorMapping StatusMesmerized = new("Mesmerized", PaletteTypes.StatusEffects, Color.FromArgb(238, 221, 204));
        public ColorMapping StatusSynchronizedFiresnaking = new("Synchronized Firesnaking", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusSynchronizedWatersnaking = new("Synchronized Watersnaking", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 17));
        public ColorMapping StatusXtremeFiresnaking = new("Xtreme Firesnaking", PaletteTypes.StatusEffects, Color.FromArgb(255, 187, 85));
        public ColorMapping StatusXtremeWatersnaking = new("Xtreme Watersnaking", PaletteTypes.StatusEffects, Color.FromArgb(85, 187, 204));
        public ColorMapping StatusOutsider = new("Outsider", PaletteTypes.StatusEffects, Color.FromArgb(153, 119, 136));
        public ColorMapping StatusMagneticPull = new("Magnetic Pull", PaletteTypes.StatusEffects, Color.FromArgb(121, 121, 116));
        public ColorMapping StatusChainsOfPassion = new("Chains of Passion α", PaletteTypes.StatusEffects, Color.FromArgb(255, 204, 204));
        public ColorMapping StatusChainsOfPassion2 = new("Chains of Passion β", PaletteTypes.StatusEffects, Color.FromArgb(110, 119, 154));
        public ColorMapping StatusTelePortent = new("Tele-portent", PaletteTypes.StatusEffects, Color.FromArgb(34, 0, 102));
        public ColorMapping StatusUnbecoming = new("Unbecoming", PaletteTypes.StatusEffects, Color.FromArgb(17, 68, 119));
        public ColorMapping StatusQuantumNullification = new("Quantum Nullification", PaletteTypes.StatusEffects, Color.FromArgb(238, 255, 255));
        public ColorMapping StatusQuantumEntanglement = new("Quantum Entanglement", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 136));
        public ColorMapping StatusFiresnaking = new("Firesnaking", PaletteTypes.StatusEffects, Color.FromArgb(221, 34, 51));
        public ColorMapping StatusWatersnaking = new("Watersnaking", PaletteTypes.StatusEffects, Color.FromArgb(51, 34, 204));
        public ColorMapping StatusMotionTracker = new("Motion Tracker", PaletteTypes.StatusEffects, Color.FromArgb(187, 136, 136));
        public ColorMapping StatusCloakOfWaningLight = new("Cloak of Waning Light", PaletteTypes.StatusEffects, Color.FromArgb(170, 136, 34));
        public ColorMapping StatusCloakOfWaxingDark = new("Cloak of Waxing Dark", PaletteTypes.StatusEffects, Color.FromArgb(136, 68, 204));
        public ColorMapping StatusGauntletTaken = new("Gauntlet Taken", PaletteTypes.StatusEffects, Color.FromArgb(0, 0, 34));
        public ColorMapping StatusEasterlyWinds = new("Easterly Winds", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusWesterlyWinds = new("Westerly Winds", PaletteTypes.StatusEffects, Color.FromArgb(255, 255, 255));
        public ColorMapping StatusStrungUp = new("Strung Up", PaletteTypes.StatusEffects, Color.FromArgb(221, 255, 255));

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
        public ColorMapping JobPLDIronWill = new("PLD: Iron Will", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobPLDConfiteorTimer = new("PLD: Confiteor Combo Timer", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobPLDConfiteorStep = new("PLD: Confiteor Combo Step", PaletteTypes.JobGauges, Color.Yellow);
        public ColorMapping JobMNKNegative = new("MNK: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobMNKChakra = new("MNK: Chakras", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobMNKBeastChakra = new("MNK: Beast Chakra (Gauge)", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobMNKOpoOpo = new("MNK: Opo-opo Beast Chakra", PaletteTypes.JobGauges, Color.Yellow);
        public ColorMapping JobMNKRaptor = new("MNK: Raptor Beast Chakra", PaletteTypes.JobGauges, Color.LimeGreen);
        public ColorMapping JobMNKCoeurl = new("MNK: Coeurl Beast Chakra", PaletteTypes.JobGauges, Color.DodgerBlue);
        public ColorMapping JobMNKNadiLunar = new("MNK: Lunar Nadi", PaletteTypes.JobGauges, Color.MediumPurple);
        public ColorMapping JobMNKNadiSolar = new("MNK: Solar Nadi", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobDRGNegative = new("DRG: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobDRGBloodDragon = new("DRG: Dragon Gauge", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobDRGDragonGaze = new("DRG: Dragon Gaze", PaletteTypes.JobGauges, Color.BlueViolet);
        public ColorMapping JobDRGFirstminds = new("DRG: Firstminds' Focus", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobBRDNegative = new("BRD: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobBRDSoulVoice = new("BRD: Soul Voice", PaletteTypes.JobGauges, Color.GreenYellow);
        public ColorMapping JobBRDSoulVoiceThreshold = new("BRD: Soul Voice Threshold", PaletteTypes.JobGauges, Color.GhostWhite);
        public ColorMapping JobBRDRepertoire = new("BRD: Repertoire Stack", PaletteTypes.JobGauges, Color.GhostWhite);
        public ColorMapping JobBRDBallad = new("BRD: Mage's Ballad", PaletteTypes.JobGauges, Color.Purple);
        public ColorMapping JobBRDArmys = new("BRD: Army's Paeon", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobBRDMinuet = new("BRD: The Wanderers' Minuet", PaletteTypes.JobGauges, Color.MediumSpringGreen);
        public ColorMapping JobBRDRadiantFinaleBallad = new("BRD: Radiant Finale (Ballad Coda)", PaletteTypes.JobGauges, Color.Purple);
        public ColorMapping JobBRDRadiantFinalePaeon = new("BRD: Radiant Finale (Paeon Coda)", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobBRDRadiantFinaleMinuet = new("BRD: Radiant Finale (Minuet Coda)", PaletteTypes.JobGauges, Color.MediumSpringGreen);
        public ColorMapping JobWHMNegative = new("WHM: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobWHMFlowerPetal = new("WHM: Flower", PaletteTypes.JobGauges, Color.MediumVioletRed);
        public ColorMapping JobWHMFlowerCharge = new("WHM: Flower Charge", PaletteTypes.JobGauges, Color.Aqua);
        public ColorMapping JobWHMBloodLily = new("WHM: Blood Lily", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobWHMFreecure = new("WHM: Freecure Proc", PaletteTypes.JobGauges, Color.LightSeaGreen);
        public ColorMapping JobBLMNegative = new("BLM: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobBLMAstralFire = new("BLM: Astral Fire", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobBLMUmbralIce = new("BLM: Umbral Ice", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobBLMPolyglot = new("BLM: Polyglot", PaletteTypes.JobGauges, Color.Magenta);
        public ColorMapping JobBLMParadox = new("BLM: Paradox Proc", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobBLMAstralSoul = new("BLM: Astral Soul (Flare Star)", PaletteTypes.JobGauges, Color.Crimson);
        public ColorMapping JobSMNNegative = new("SMN: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobSMNCarbuncleTimer = new("SMN: Carbuncle Timer", PaletteTypes.JobGauges, Color.DodgerBlue);
        public ColorMapping JobSMNDreadwyrmTimer = new("SMN: Dreadwyrm Timer", PaletteTypes.JobGauges, Color.Yellow);
        public ColorMapping JobSMNBahamutTimer = new("SMN: Bahamut Timer", PaletteTypes.JobGauges, Color.MediumBlue);
        public ColorMapping JobSMNPhoenixTimer = new("SMN: Phoenix Timer", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobSMNIfrit = new("SMN: Summon Ifrit", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobSMNTitan = new("SMN: Summon Titan", PaletteTypes.JobGauges, Color.Yellow);
        public ColorMapping JobSMNGaruda = new("SMN: Summon Garuda", PaletteTypes.JobGauges, Color.Green);
        public ColorMapping JobSCHNegative = new("SCH: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobSCHAetherflow = new("SCH: Aetherflow", PaletteTypes.JobGauges, Color.Orchid);
        public ColorMapping JobSCHFaerieGauge = new("SCH: Faerie Gauge", PaletteTypes.JobGauges, Color.MediumSpringGreen);
        public ColorMapping JobSCHSeraph = new("SCH: Summon Seraph", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobSCHDismissedFairy = new("SCH: Fairy Dismissed", PaletteTypes.JobGauges, Color.DimGray);
        public ColorMapping JobNINNegative = new("NIN: Blank Key", PaletteTypes.JobGauges, Color.Black);
        // Legacy NIN Huton slot: Dawntrail replaced Huton with Kazematoi (same gauge-A role).
        // Field name kept for JSON deserialisation compatibility with pre-v2 palettes; display
        // name updated so the UI reflects the current ability.
        public ColorMapping JobNINHuton = new("NIN: Kazematoi", PaletteTypes.JobGauges, Color.Cornsilk);
        public ColorMapping JobNINNinkiGauge = new("NIN: Ninki Gauge", PaletteTypes.JobGauges, Color.Coral);
        public ColorMapping JobDRKNegative = new("DRK: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobDRKBloodGauge = new("DRK: Blood Gauge", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobDRKGrit = new("DRK: Grit", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobDRKDarkside = new("DRK: Darkside Timer", PaletteTypes.JobGauges, Color.MediumPurple);
        public ColorMapping JobDRKLivingShadow = new("DRK: Living Shadow Timer", PaletteTypes.JobGauges, Color.DarkMagenta);
        public ColorMapping JobASTNegative = new("AST: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobASTArrow = new("AST: Arrow Drawn", PaletteTypes.JobGauges, Color.Lime);
        public ColorMapping JobASTBalance = new("AST: Balance Drawn", PaletteTypes.JobGauges, Color.Crimson);
        public ColorMapping JobASTBole = new("AST: Bole Drawn", PaletteTypes.JobGauges, Color.Orange);
        public ColorMapping JobASTEwer = new("AST: Ewer Drawn", PaletteTypes.JobGauges, Color.MediumBlue);
        public ColorMapping JobASTSpear = new("AST: Spear Drawn", PaletteTypes.JobGauges, Color.Turquoise);
        public ColorMapping JobASTSpire = new("AST: Spire Drawn", PaletteTypes.JobGauges, Color.SlateBlue);
        public ColorMapping JobASTLady = new("AST: Lady of Crowns Drawn", PaletteTypes.JobGauges, Color.HotPink);
        public ColorMapping JobASTLord = new("AST: Lord of Crowns Drawn", PaletteTypes.JobGauges, Color.Magenta);
        public ColorMapping JobASTAstralDraw = new("AST: Astral Draw", PaletteTypes.JobGauges, Color.HotPink);
        public ColorMapping JobASTUmbralDraw = new("AST: Umbral Draw", PaletteTypes.JobGauges, Color.DarkSlateBlue);
        public ColorMapping JobMCHNegative = new("MCH: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobMCHBatteryGauge = new("MCH: Battery Gauge", PaletteTypes.JobGauges, Color.Cyan);
        public ColorMapping JobMCHHeatGauge = new("MCH: Heat Gauge", PaletteTypes.JobGauges, Color.DarkOrange);
        public ColorMapping JobMCHOverheat = new("MCH: Overheat", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobSAMNegative = new("SAM: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobSAMKenki = new("SAM: Kenki Charge", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobSAMMeditation = new("SAM: Meditation Stacks", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobSAMKaeshiReady = new("SAM: Kaeshi Ready", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobRDMNegative = new("RDM: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobRDMBlackMana = new("RDM: Black Mana", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobRDMWhiteMana = new("RDM: White Mana", PaletteTypes.JobGauges, Color.White);
        public ColorMapping JobRDMManaStacks = new("RDM: Mana Stacks", PaletteTypes.JobGauges, Color.MediumVioletRed);
        public ColorMapping JobDNCNegative = new("DNC: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobDNCFeathers = new("DNC: Fourfold Feathers", PaletteTypes.JobGauges, Color.GreenYellow);
        public ColorMapping JobDNCEspirit = new("DNC: Espirit Gauge", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobGNBNegative = new("GNB: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobGNBRoyalGuard = new("GNB: Royal Guard", PaletteTypes.JobGauges, Color.OrangeRed);
        public ColorMapping JobGNBCartridge = new("GNB: Cartridge", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobGNBBloodfestTimer = new("GNB: Bloodfest Timer", PaletteTypes.JobGauges, Color.DarkOrange);
        public ColorMapping JobSGENegative = new("SGE: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobSGEAddersgallStacks = new("SGE: Addersgall Stacks", PaletteTypes.JobGauges, Color.LightBlue);
        public ColorMapping JobSGEAdderstingStacks = new("SGE: Addersting Stacks", PaletteTypes.JobGauges, Color.MediumPurple);
        public ColorMapping JobSGEAddersgallRecharge = new("SGE: Addersgall Recharge", PaletteTypes.JobGauges, Color.LightBlue);
        public ColorMapping JobRPRNegative = new("RPR: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobRPRSouls = new("RPR: Souls Collected", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobRPRShrouds = new("RPR: Shrouds Collected", PaletteTypes.JobGauges, Color.DodgerBlue);
        public ColorMapping JobCrafterNegative = new("Crafter: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobCrafterInnerquiet = new("Crafter: Inner Quiet Stacks", PaletteTypes.JobGauges, Color.BlueViolet);
        public ColorMapping JobCrafterCollectable = new("Crafter: Collectables", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobCrafterCrafter = new("Crafter: Crafting", PaletteTypes.JobGauges, Color.DeepSkyBlue);
        public ColorMapping JobVPRVipersight = new("VPR: Vipersight Gauge", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobVPRSerpentOffering = new("VPR: Serpent Offering Gauge", PaletteTypes.JobGauges, Color.DodgerBlue);
        public ColorMapping JobVPRReawakened = new("VPR: Reawakened Timer", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobVPRSerpentCombo = new("VPR: Serpent Combo Ready", PaletteTypes.JobGauges, Color.YellowGreen);
        public ColorMapping JobVPRNegative = new("VPR: Blank Key", PaletteTypes.JobGauges, Color.Black);
        public ColorMapping JobPCTPalette = new("PCT: Palette Gauge", PaletteTypes.JobGauges, Color.DodgerBlue);
        public ColorMapping JobPCTLandscape = new("PCT: Canvas Landscape", PaletteTypes.JobGauges, Color.BlueViolet);
        public ColorMapping JobPCTMaw = new("PCT: Canvas Maw", PaletteTypes.JobGauges, Color.Purple);
        public ColorMapping JobPCTWeapon = new("PCT: Canvas Weapon", PaletteTypes.JobGauges, Color.Red);
        public ColorMapping JobPCTClaw = new("PCT: Canvas Claw", PaletteTypes.JobGauges, Color.Gold);
        public ColorMapping JobPCTWing = new("PCT: Canvas Wing", PaletteTypes.JobGauges, Color.Magenta);
        public ColorMapping JobPCTNegative = new("PCT: Blank Key", PaletteTypes.JobGauges, Color.Black);

        //Reactive Weather
        public ColorMapping WeatherClearSkiesBase = new("Clear Skies (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherClearSkiesHighlight = new("Clear Skies (Highlight)", PaletteTypes.ReactiveWeather, Color.Yellow);
        public ColorMapping WeatherFairSkiesBase = new("Fair Skies (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherFairSkiesHighlight = new("Fair Skies (Highlight)", PaletteTypes.ReactiveWeather, Color.Yellow);
        public ColorMapping WeatherCloudsBase = new("Clouds (Base)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherCloudsHighlight = new("Clouds (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherFogBase = new("Fog (Base)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherFogHighlight = new("Fog (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkBlue);
        public ColorMapping WeatherWindBase = new("Wind (Base)", PaletteTypes.ReactiveWeather, Color.RoyalBlue);
        public ColorMapping WeatherWindHighlight = new("Wind (Highlight)", PaletteTypes.ReactiveWeather, Color.GreenYellow);
        public ColorMapping WeatherWindAnimation = new("Wind (Animation Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x559C2F));
        public ColorMapping WeatherGalesBase = new("Gales (Base)", PaletteTypes.ReactiveWeather, Color.RoyalBlue);
        public ColorMapping WeatherGalesHighlight = new("Gales (Highlight)", PaletteTypes.ReactiveWeather, Color.GreenYellow);
        public ColorMapping WeatherGalesAnimation = new("Gales (Animation Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x559C2F));
        public ColorMapping WeatherRainBase = new("Rain (Base)", PaletteTypes.ReactiveWeather, Color.MediumBlue);
        public ColorMapping WeatherRainHighlight = new("Rain (Highlight)", PaletteTypes.ReactiveWeather, Color.SpringGreen);
        public ColorMapping WeatherRainAnimation = new("Rain (Animation Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x2529A5));
        public ColorMapping WeatherShowersBase = new("Showers (Base)", PaletteTypes.ReactiveWeather, Color.MediumBlue);
        public ColorMapping WeatherShowersHighlight = new("Showers (Highlight)", PaletteTypes.ReactiveWeather, Color.SpringGreen);
        public ColorMapping WeatherShowersAnimation = new("Showers (Animation Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x256FA5));
        public ColorMapping WeatherThunderBase = new("Thunder (Base)", PaletteTypes.ReactiveWeather, Color.BlueViolet);
        public ColorMapping WeatherThunderHighlight = new("Thunder (Highlight)", PaletteTypes.ReactiveWeather, Color.Pink);
        public ColorMapping WeatherThunderAnimation = new("Thunder (Animation Highlight)", PaletteTypes.ReactiveWeather, Color.White);
        public ColorMapping WeatherThunderstormsBase = new("Thunderstorms (Base)", PaletteTypes.ReactiveWeather, Color.Indigo);
        public ColorMapping WeatherThunderstormsHighlight = new("Thunderstorms (Highlight)", PaletteTypes.ReactiveWeather, Color.Plum);
        public ColorMapping WeatherDustStormsBase = new("Dust Storms (Base)", PaletteTypes.ReactiveWeather, Color.Sienna);
        public ColorMapping WeatherDustStormsHighlight = new("Dust Storms (Highlight)", PaletteTypes.ReactiveWeather, Color.PeachPuff);
        public ColorMapping WeatherSandstormsBase = new("Sandstorms (Base)", PaletteTypes.ReactiveWeather, Color.Peru);
        public ColorMapping WeatherSandstormsHighlight = new("Sandstorms (Highlight)", PaletteTypes.ReactiveWeather, Color.PapayaWhip);
        public ColorMapping WeatherSandstormsAnimationHighlight = new("Sandstorms Animation (Highlight)", PaletteTypes.ReactiveWeather, Color.Orange);
        public ColorMapping WeatherHotSpellsBase = new("Hot Spells (Base)", PaletteTypes.ReactiveWeather, Color.Orange);
        public ColorMapping WeatherHotSpellsHighlight = new("Hot Spells (Highlight)", PaletteTypes.ReactiveWeather, Color.OrangeRed);
        public ColorMapping WeatherHeatWavesBase = new("Heat Waves (Base)", PaletteTypes.ReactiveWeather, Color.OrangeRed);
        public ColorMapping WeatherHeatWavesHighlight = new("Heat Waves (Highlight)", PaletteTypes.ReactiveWeather, Color.Red);
        public ColorMapping WeatherSnowBase = new("Snow (Base)", PaletteTypes.ReactiveWeather, Color.SkyBlue);
        public ColorMapping WeatherSnowHighlight = new("Snow (Highlight)", PaletteTypes.ReactiveWeather, Color.Snow);
        public ColorMapping WeatherSnowAnimationHighlight = new("Snow Animation (Highlight)", PaletteTypes.ReactiveWeather, Color.Black);
        public ColorMapping WeatherBlizzardsBase = new("Blizzards (Base)", PaletteTypes.ReactiveWeather, Color.LightSlateGray);
        public ColorMapping WeatherBlizzardsHighlight = new("Blizzards (Highlight)", PaletteTypes.ReactiveWeather, Color.Snow);
        public ColorMapping WeatherBilzzardsAnimationHighlight = new("Blizzards Animation (Highlight)", PaletteTypes.ReactiveWeather, Color.Black);
        public ColorMapping WeatherGloomBase = new("Gloom (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateBlue);
        public ColorMapping WeatherGloomHighlight = new("Gloom (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumOrchid);
        public ColorMapping WeatherAurorasBase = new("Auroras (Base)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherAurorasHighlight = new("Auroras (Highlight)", PaletteTypes.ReactiveWeather, Color.Violet);
        public ColorMapping WeatherDarknessBase = new("Darkness (Base)", PaletteTypes.ReactiveWeather, Color.MidnightBlue);
        public ColorMapping WeatherDarknessHighlight = new("Darkness (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumVioletRed);
        public ColorMapping WeatherTensionBase = new("Tension (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateGray);
        public ColorMapping WeatherTensionHighlight = new("Tension (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumTurquoise);
        public ColorMapping WeatherStormCloudsBase = new("Storm Clouds (Base)", PaletteTypes.ReactiveWeather, Color.DarkBlue);
        public ColorMapping WeatherStormCloudsHighlight = new("Storm Clouds (Highlight)", PaletteTypes.ReactiveWeather, Color.CornflowerBlue);
        public ColorMapping WeatherRoughSeasBase = new("Rough Seas (Base)", PaletteTypes.ReactiveWeather, Color.RoyalBlue);
        public ColorMapping WeatherRoughSeasHighlight = new("Rough Seas (Highlight)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherLouringBase = new("Louring (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateGray);
        public ColorMapping WeatherLouringHighlight = new("Louring (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkOrange);
        public ColorMapping WeatherEruptionsBase = new("Eruptions (Base)", PaletteTypes.ReactiveWeather, Color.DarkRed);
        public ColorMapping WeatherEruptionsHighlight = new("Eruptions (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkOrange);
        public ColorMapping WeatherIrradianceBase = new("Irradiance (Base)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherIrradianceHighlight = new("Irradiance (Highlight)", PaletteTypes.ReactiveWeather, Color.Violet);
        public ColorMapping WeatherCoreRadiationBase = new("Core Radiation (Base)", PaletteTypes.ReactiveWeather, Color.MidnightBlue);
        public ColorMapping WeatherCoreRadiationHighlight = new("Core Radiation (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherShelfCloudsBase = new("Shelf Clouds (Base)", PaletteTypes.ReactiveWeather, Color.DarkOliveGreen);
        public ColorMapping WeatherShelfCloudsHighlight = new("Shelf Clouds (Highlight)", PaletteTypes.ReactiveWeather, Color.PaleGoldenrod);
        public ColorMapping WeatherOppressionBase = new("Oppression (Base)", PaletteTypes.ReactiveWeather, Color.LimeGreen);
        public ColorMapping WeatherOppressionHighlight = new("Oppression (Highlight)", PaletteTypes.ReactiveWeather, Color.PaleGreen);
        public ColorMapping WeatherUmbralWindBase = new("Umbral Wind (Base)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherUmbralWindHighlight = new("Umbral Wind (Highlight)", PaletteTypes.ReactiveWeather, Color.Azure);
        public ColorMapping WeatherUmbralWindAnimationBase = new("Umbral Wind Animation (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x0A1DC6));
        public ColorMapping WeatherUmbralWindAnimationHighlight = new("Umbral Wind Animation (Highlight)", PaletteTypes.ReactiveWeather, Color.SpringGreen);
        public ColorMapping WeatherUmbralStaticBase = new("Umbral Static (Base)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherUmbralStaticHighlight = new("Umbral Static (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherUmbralStaticAnimationBase = new("Umbral Static Animation (Base)", PaletteTypes.ReactiveWeather, Color.MediumBlue);
        public ColorMapping WeatherUmbralStaticAnimationHighlight = new("Umbral Static Animation (Highlight)", PaletteTypes.ReactiveWeather, Color.Cyan);
        public ColorMapping WeatherSmokeBase = new("Smoke (Base)", PaletteTypes.ReactiveWeather, Color.DarkRed);
        public ColorMapping WeatherSmokeHighlight = new("Smoke (Highlight)", PaletteTypes.ReactiveWeather, Color.OrangeRed);
        public ColorMapping WeatherRoyalLevinBase = new("Royal Levin (Base)", PaletteTypes.ReactiveWeather, Color.Goldenrod);
        public ColorMapping WeatherRoyalLevinHighlight = new("Royal Levin (Highlight)", PaletteTypes.ReactiveWeather, Color.Khaki);
        public ColorMapping WeatherHyperelectricityBase = new("Hyperelectricity (Base)", PaletteTypes.ReactiveWeather, Color.HotPink);
        public ColorMapping WeatherHyperelectricityHighlight = new("Hyperelectricity (Highlight)", PaletteTypes.ReactiveWeather, Color.Gold);
        public ColorMapping WeatherMultiplicityBase = new("Multiplicity (Base)", PaletteTypes.ReactiveWeather, Color.SlateGray);
        public ColorMapping WeatherMultiplicityHighlight = new("Multiplicity (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumTurquoise);
        public ColorMapping WeatherDragonstormsBase = new("Dragonstorms (Base)", PaletteTypes.ReactiveWeather, Color.Sienna);
        public ColorMapping WeatherDragonstormsHighlight = new("Dragonstorms (Highlight)", PaletteTypes.ReactiveWeather, Color.OrangeRed);
        public ColorMapping WeatherSubterrainBase = new("Subterrain (Base)", PaletteTypes.ReactiveWeather, Color.SaddleBrown);
        public ColorMapping WeatherSubterrainHighlight = new("Subterrain (Highlight)", PaletteTypes.ReactiveWeather, Color.Peru);
        public ColorMapping WeatherConcordanceBase = new("Concordance (Base)", PaletteTypes.ReactiveWeather, Color.DarkRed);
        public ColorMapping WeatherConcordanceHighlight = new("Concordance (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkOrange);
        public ColorMapping WeatherBeyondTimeBase = new("Beyond Time (Base)", PaletteTypes.ReactiveWeather, Color.LimeGreen);
        public ColorMapping WeatherBeyondTimeHighlight = new("Beyond Time (Highlight)", PaletteTypes.ReactiveWeather, Color.Gold);
        public ColorMapping WeatherDemonicInfinityBase = new("Demonic Infinity (Base)", PaletteTypes.ReactiveWeather, Color.DarkRed);
        public ColorMapping WeatherDemonicInfinityHighlight = new("Demonic Infinity (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkOrange);
        public ColorMapping WeatherDimensionalDisruptionBase = new("Dimensional Disruption (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateBlue);
        public ColorMapping WeatherDimensionalDisruptionHighlight = new("Dimensional Disruption (Highlight)", PaletteTypes.ReactiveWeather, Color.SkyBlue);
        public ColorMapping WeatherRevelstormsBase = new("Revelstorms (Base)", PaletteTypes.ReactiveWeather, Color.SteelBlue);
        public ColorMapping WeatherRevelstormsHighlight = new("Revelstorms (Highlight)", PaletteTypes.ReactiveWeather, Color.SkyBlue);
        public ColorMapping WeatherEternalBlissBase = new("Eternal Bliss (Base)", PaletteTypes.ReactiveWeather, Color.Pink);
        public ColorMapping WeatherEternalBlissHighlight = new("Eternal Bliss (Highlight)", PaletteTypes.ReactiveWeather, Color.PeachPuff);
        public ColorMapping WeatherWyrmstormsBase = new("Wyrmstorms (Base)", PaletteTypes.ReactiveWeather, Color.DarkMagenta);
        public ColorMapping WeatherWyrmstormsHighlight = new("Wyrmstorms (Highlight)", PaletteTypes.ReactiveWeather, Color.PaleGoldenrod);
        public ColorMapping WeatherQuicklevinBase = new("Quicklevin (Base)", PaletteTypes.ReactiveWeather, Color.Navy);
        public ColorMapping WeatherQuicklevinHighlight = new("Quicklevin (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherWhiteCyclonesBase = new("White Cyclones (Base)", PaletteTypes.ReactiveWeather, Color.MidnightBlue);
        public ColorMapping WeatherWhiteCyclonesHighlight = new("White Cyclones (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumTurquoise);
        public ColorMapping WeatherUltimaniaBase = new("Ultimania (Base)", PaletteTypes.ReactiveWeather, Color.RoyalBlue);
        public ColorMapping WeatherUltimaniaHighlight = new("Ultimania (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);
        public ColorMapping WeatherMoonlightBase = new("Moonlight (Base)", PaletteTypes.ReactiveWeather, Color.BlueViolet);
        public ColorMapping WeatherMoonlightHighlight = new("Moonlight (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkOrange);
        public ColorMapping WeatherRedMoonBase = new("Red Moon (Base)", PaletteTypes.ReactiveWeather, Color.MidnightBlue);
        public ColorMapping WeatherRedMoonHighlight = new("Red Moon (Highlight)", PaletteTypes.ReactiveWeather, Color.Crimson);
        public ColorMapping WeatherScarletBase = new("Scarlet (Base)", PaletteTypes.ReactiveWeather, Color.MidnightBlue);
        public ColorMapping WeatherScarletHighlight = new("Scarlet (Highlight)", PaletteTypes.ReactiveWeather, Color.Crimson);
        public ColorMapping WeatherFlamesBase = new("Flames (Base)", PaletteTypes.ReactiveWeather, Color.MidnightBlue);
        public ColorMapping WeatherFlamesHighlight = new("Flames (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkOrange);
        public ColorMapping WeatherTsunamisBase = new("Tsunamis (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateGray);
        public ColorMapping WeatherTsunamisHighlight = new("Tsunamis (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);

        public ColorMapping WeatherCyclonesBase = new("Cyclones (Base)", PaletteTypes.ReactiveWeather, Color.CadetBlue);
        public ColorMapping WeatherCyclonesHighlight = new("Cyclones (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumTurquoise);

        public ColorMapping WeatherGeostormsBase = new("Geostorms (Base)", PaletteTypes.ReactiveWeather, Color.Goldenrod);
        public ColorMapping WeatherGeostormsHighlight = new("Geostorms (Highlight)", PaletteTypes.ReactiveWeather, Color.NavajoWhite);

        public ColorMapping WeatherTrueBlueBase = new("True Blue (Base)", PaletteTypes.ReactiveWeather, Color.SlateBlue);
        public ColorMapping WeatherTrueBlueHighlight = new("True Blue (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkTurquoise);

        public ColorMapping WeatherUmbralTurbulenceBase = new("Umbral Turbulence (Base)", PaletteTypes.ReactiveWeather, Color.Firebrick);
        public ColorMapping WeatherUmbralTurbulenceHighlight = new("Umbral Turbulence (Highlight)", PaletteTypes.ReactiveWeather, Color.OrangeRed);

        public ColorMapping WeatherEverlastingLightBase = new("Everlasting Light (Base)", PaletteTypes.ReactiveWeather, Color.Cornsilk);
        public ColorMapping WeatherEverlastingLightHighlight = new("Everlasting Light (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x162CFB));
        public ColorMapping WeatherEverlastingLightAnimationHighlight = new("Everlasting Light Animation (Highlight)", PaletteTypes.ReactiveWeather, Color.Yellow);

        public ColorMapping WeatherTerminationBase = new("Termination (Base)", PaletteTypes.ReactiveWeather, Color.DarkOrchid);
        public ColorMapping WeatherTerminationHighlight = new("Termination (Highlight)", PaletteTypes.ReactiveWeather, Color.Plum);

        public ColorMapping WeatherDreamsBase = new("Dreams (Base)", PaletteTypes.ReactiveWeather, Color.LightSeaGreen);
        public ColorMapping WeatherDreamsHighlight = new("Dreams (Highlight)", PaletteTypes.ReactiveWeather, Color.Aquamarine);

        public ColorMapping WeatherBrillianceBase = new("Brilliance (Base)", PaletteTypes.ReactiveWeather, Color.Goldenrod);
        public ColorMapping WeatherBrillianceHighlight = new("Brilliance (Highlight)", PaletteTypes.ReactiveWeather, Color.Gold);

        public ColorMapping WeatherUmbralFlareBase = new("Umbral Flare (Base)", PaletteTypes.ReactiveWeather, Color.DarkRed);
        public ColorMapping WeatherUmbralFlareHighlight = new("Umbral Flare (Highlight)", PaletteTypes.ReactiveWeather, Color.OrangeRed);

        public ColorMapping WeatherUmbralDuststormBase = new("Umbral Duststorm (Base)", PaletteTypes.ReactiveWeather, Color.Sienna);
        public ColorMapping WeatherUmbralDuststormHighlight = new("Umbral Duststorm (Highlight)", PaletteTypes.ReactiveWeather, Color.SandyBrown);

        public ColorMapping WeatherUmbralLevinBase = new("Umbral Levin (Base)", PaletteTypes.ReactiveWeather, Color.Indigo);
        public ColorMapping WeatherUmbralLevinHighlight = new("Umbral Levin (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumOrchid);

        public ColorMapping WeatherUmbralTempestBase = new("Umbral Tempest (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateGray);
        public ColorMapping WeatherUmbralTempestHighlight = new("Umbral Tempest (Highlight)", PaletteTypes.ReactiveWeather, Color.Turquoise);

        public ColorMapping WeatherStarshowerBase = new("Starshower (Base)", PaletteTypes.ReactiveWeather, Color.SaddleBrown);
        public ColorMapping WeatherStarshowerHighlight = new("Starshower (Highlight)", PaletteTypes.ReactiveWeather, Color.Orange);

        public ColorMapping WeatherDeliriumBase = new("Delirium (Base)", PaletteTypes.ReactiveWeather, Color.PeachPuff);
        public ColorMapping WeatherDeliriumHighlight = new("Delirium (Highlight)", PaletteTypes.ReactiveWeather, Color.OldLace);

        public ColorMapping WeatherFirestormBase = new("Firestorm (Base)", PaletteTypes.ReactiveWeather, Color.OliveDrab);
        public ColorMapping WeatherFirestormHighlight = new("Firestorm (Highlight)", PaletteTypes.ReactiveWeather, Color.YellowGreen);

        public ColorMapping WeatherSpectralCurrentBase = new("Spectral Current (Base)", PaletteTypes.ReactiveWeather, Color.Turquoise);
        public ColorMapping WeatherSpectralCurrentHighlight = new("Spectral Current (Highlight)", PaletteTypes.ReactiveWeather, Color.Cyan);

        public ColorMapping WeatherClimacticBase = new("Climactic (Base)", PaletteTypes.ReactiveWeather, Color.DarkCyan);
        public ColorMapping WeatherClimacticHighlight = new("Climactic (Highlight)", PaletteTypes.ReactiveWeather, Color.DarkOrange);

        public ColorMapping WeatherMoonDustBase = new("Moon Dust (Base)", PaletteTypes.ReactiveWeather, Color.MediumBlue);
        public ColorMapping WeatherMoonDustHighlight = new("Moon Dust (Highlight)", PaletteTypes.ReactiveWeather, Color.White);
        public ColorMapping WeatherMoonDustAnimationBase = new("Mare Lamentorum (Animation Base)", PaletteTypes.ReactiveWeather, Color.Black);
        public ColorMapping WeatherMoonDustAnimationHighlight = new("Mare Lamentorum (Animation Highlight)", PaletteTypes.ReactiveWeather, Color.White);

        public ColorMapping WeatherAstromagneticStormBase = new("Astromagnetic Storms (Base)", PaletteTypes.ReactiveWeather, Color.Magenta);
        public ColorMapping WeatherAstromagneticStormHighlight = new("Astromagnetic Storms (Highlight)", PaletteTypes.ReactiveWeather, Color.Red);
        public ColorMapping WeatherAstromagneticStormHighlight1 = new("Astromagnetic Storms Animation (Highlight 1)", PaletteTypes.ReactiveWeather, Color.Magenta);
        public ColorMapping WeatherAstromagneticStormHighlight2 = new("Astromagnetic Storms (Highlight 2)", PaletteTypes.ReactiveWeather, Color.DeepPink);
        public ColorMapping WeatherAstromagneticStormHighlight3 = new("Astromagnetic Storms (Highlight 3)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);

        public ColorMapping WeatherUltimaThuleAnimationBase = new("Ultima Thule Animation (Base)", PaletteTypes.ReactiveWeather, Color.Magenta);
        public ColorMapping WeatherUltimaThuleAnimationHighlight = new("Ultima Thule Animation (Highlight)", PaletteTypes.ReactiveWeather, Color.Blue);
        public ColorMapping WeatherUltimaThuleAnimationHighlight1 = new("Ultima Thule Animation (Highlight 1)", PaletteTypes.ReactiveWeather, Color.MediumBlue);
        public ColorMapping WeatherUltimaThuleAnimationHighlight2 = new("Ultima Thule Animation (Highlight 2)", PaletteTypes.ReactiveWeather, Color.Cyan);
        public ColorMapping WeatherUltimaThuleAnimationHighlight3 = new("Ultima Thule Animation (Highlight 3)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherUltimaThuleAnimationHighlight4 = new("Ultima Thule Animation (Highlight 4)", PaletteTypes.ReactiveWeather, Color.White);

        public ColorMapping WeatherApocalypseBase = new("Apocalypse (Base)", PaletteTypes.ReactiveWeather, Color.DarkRed);
        public ColorMapping WeatherApocalypseHighlight = new("Apocalypse (Highlight)", PaletteTypes.ReactiveWeather, Color.OrangeRed);

        public ColorMapping WeatherPolarizationBase = new("Polarization (Base)", PaletteTypes.ReactiveWeather, Color.Navy);
        public ColorMapping WeatherPolarizationHighlight = new("Polarization (Highlight)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);

        public ColorMapping WeatherProjectionBase = new("Projection (Base)", PaletteTypes.ReactiveWeather, Color.MidnightBlue);
        public ColorMapping WeatherProjectionHighlight = new("Projection (Highlight)", PaletteTypes.ReactiveWeather, Color.ForestGreen);

        public ColorMapping WeatherPandæmoniumBase = new("Pandæmonium (Base)", PaletteTypes.ReactiveWeather, Color.Indigo);
        public ColorMapping WeatherPandæmoniumHighlight = new("Pandæmonium (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumSlateBlue);

        public ColorMapping WeatherUltimatumBase = new("Ultimatum (Base)", PaletteTypes.ReactiveWeather, Color.DarkSlateBlue);
        public ColorMapping WeatherUltimatumHighlight = new("Ultimatum (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumSlateBlue);

        public ColorMapping WeatherInevitabilityBase = new("Inevitability (Base)", PaletteTypes.ReactiveWeather, Color.Maroon);
        public ColorMapping WeatherInevitabilityHighlight = new("Inevitability (Highlight)", PaletteTypes.ReactiveWeather, Color.IndianRed);

        public ColorMapping WeatherTranscendenceBase = new("Transcendence (Base)", PaletteTypes.ReactiveWeather, Color.LightSkyBlue);
        public ColorMapping WeatherTranscendenceHighlight = new("Transcendence (Highlight)", PaletteTypes.ReactiveWeather, Color.LightYellow);

        public ColorMapping WeatherVacuityBase = new("Vacuity (Base)", PaletteTypes.ReactiveWeather, Color.Purple);
        public ColorMapping WeatherVacuityHighlight = new("Vacuity (Highlight)", PaletteTypes.ReactiveWeather, Color.Violet);

        public ColorMapping WeatherReminiscenceBase = new("Reminiscence (Base)", PaletteTypes.ReactiveWeather, Color.Gold);
        public ColorMapping WeatherReminiscenceHighlight = new("Reminiscence (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x00FF7E));

        public ColorMapping WeatherOminousCloudsBase = new("Ominous Clouds (Base)", PaletteTypes.ReactiveWeather, Color.Silver);
        public ColorMapping WeatherOminousCloudsHighlight = new("Ominous Clouds (Highlight)", PaletteTypes.ReactiveWeather, Color.RoyalBlue);

        public ColorMapping WeatherLiminalityBase = new("Liminality (Base)", PaletteTypes.ReactiveWeather, Color.RoyalBlue);
        public ColorMapping WeatherLiminalityHighlight = new("Liminality (Highlight)", PaletteTypes.ReactiveWeather, Color.MediumSlateBlue);

        //7.1+
        public ColorMapping WeatherAtmosphericPhantasmsBase = new("Atmospheric Phantasms (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x4BF0FC));
        public ColorMapping WeatherAtmosphericPhantasmsHighlight = new("Atmospheric Phantasms (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFFFFFF));
        public ColorMapping WeatherAtmosphericPhantasmsAnimationBase = new("Atmospheric Phantasms (Animation Bse)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x006F70));
        public ColorMapping WeatherAtmosphericPhantasmsHighlight1 = new("Atmospheric Phantasms (Highlight 1)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x002BFF));
        public ColorMapping WeatherAtmosphericPhantasmsHighlight2 = new("Atmospheric Phantasms (Highlight 2)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x00FFCA));
        public ColorMapping WeatherIllusoryDisturbancesBase = new("Illusory Disturbances (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xE3F9A6));
        public ColorMapping WeatherIllusoryDisturbancesHighlight = new("Illusory Disturbances (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFFFFFF));
        public ColorMapping WeatherIllusoryDisturbancesAnimationBase = new("Illusory Disturbances (Animation Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x1A1A1A));
        public ColorMapping WeatherIllusoryDisturbancesHighligh1 = new("Illusory Disturbances (Highlight 1)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x03FDFF));
        public ColorMapping WeatherIllusoryDisturbancesHighligh2 = new("Illusory Disturbances (Highlight 2)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xD16900));
        public ColorMapping WeatherIllusoryDisturbancesHighligh3 = new("Illusory Disturbances (Highlight 3)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFFEA00));
        public ColorMapping WeatherAuroralMiragesBase = new("Auroral Mirages (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFF8674));
        public ColorMapping WeatherAuroralMiragesHighlight = new("Auroral Mirages (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFF007F));
        public ColorMapping WeatherMeteorShowersBase = new("Meteor Showers (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x000000));
        public ColorMapping WeatherMeteorShowersHighlight = new("Meteor Showers (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x08FFF9));
        public ColorMapping WeatherMeteorShowersHighlight1 = new("Meteor Showers (Highlight 1)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFFFFFF));
        public ColorMapping WeatherMeteorShowersHighlight2 = new("Meteor Showers (Highlight 2)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFFCB01));
        public ColorMapping WeatherSporingMistBase = new("Sporing Mist (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x2DBF0D));
        public ColorMapping WeatherSporingMistHighlight = new("Sporing Mist (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xD462FF));
        public ColorMapping WeatherSporingMistHighlight1 = new("Sporing Mist (Highlight 1)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xD462FF));
        public ColorMapping WeatherSporingMistHighlight2 = new("Sporing Mist (Highlight 2)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xD462FF));
        public ColorMapping WeatherAnnealingWindsBase = new("Annealing Winds (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x16E2F5));
        public ColorMapping WeatherAnnealingWindsHighlight = new("Annealing Winds (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xF6358A));
        public ColorMapping WeatherAnnealingWindsHighlight1 = new("Annealing Winds (Highlight1)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x035CFF));
        public ColorMapping WeatherGlassStormsBase = new("Glass Storms (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFC6C85));
        public ColorMapping WeatherGlassStormsAnimationBase = new("Glass Storms (Animation Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x480048));
        public ColorMapping WeatherGlassStormsHighlight = new("Glass Storms (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFF8040));
        public ColorMapping WeatherGlassStormsHighlight1 = new("Glass Storms (Highlight 1)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFFFFFF));
        public ColorMapping WeatherGravitationalFluxBase = new("Gravitational Flux (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x191970));
        public ColorMapping WeatherGravitationalFluxHighlight = new("Gravitational Flux (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xEB5406));
        public ColorMapping WeatherGravitationalFluxAnimationBase = new("Gravitational Flux (Animation Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x010014));
        public ColorMapping WeatherGravitationalFluxHighlight1 = new("Gravitational Flux (Highlight 1)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x0800FF));
        public ColorMapping WeatherGravitationalFluxHighlight2 = new("Gravitational Flux (Highlight 2)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x00FFF5));
        public ColorMapping WeatherBubbleBloomBase = new("Bubble Bloom (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x0059FF));
        public ColorMapping WeatherBubbleBloomAnimationBase = new("Bubble Bloom (Animation Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x00033E));
        public ColorMapping WeatherBubbleBloomHighlight = new("Bubble Bloom (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFFFFFF));
        public ColorMapping WeatherBubbleBloomHighlight1 = new("Bubble Bloom (Highlight 1)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x00C8FF));
        public ColorMapping WeatherElectrostaticDustBase = new("Electrostatic Dust (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xFFAE42));
        public ColorMapping WeatherElectrostaticDustHighlight = new("Electrostatic Dust (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0xC45AEC));
        public ColorMapping WeatherDyingBreathBase = new("Dying Breath (Base)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x571B7E));
        public ColorMapping WeatherDyingBreathHighlight = new("Dying Breath (Highlight)", PaletteTypes.ReactiveWeather, Color.FromArgb(0x2916F5));
        public ColorMapping WeatherUnknownBase = new("Unknown Weather (Base)", PaletteTypes.ReactiveWeather, Color.DeepSkyBlue);
        public ColorMapping WeatherUnknownHighlight = new("Unknown Weather (Highlight)", PaletteTypes.ReactiveWeather, Color.Yellow);
        
        //Notifications
        public ColorMapping DutyFinderBell = new("Duty Finder Bell", PaletteTypes.Notifications, Color.Red);
        public ColorMapping CastingSuccess = new("Casting Success", PaletteTypes.Notifications, Color.White);
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
        public ColorMapping JobDRGBase = new("DRG (Base)", PaletteTypes.JobClasses, Color.DarkBlue);
        public ColorMapping JobDRGHighlight = new("DRG (Highlight)", PaletteTypes.JobClasses, Color.LightSkyBlue);
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
        public ColorMapping JobDRKBase = new("DRK (Base)", PaletteTypes.JobClasses, Color.DarkRed);
        public ColorMapping JobDRKHighlight = new("DRK (Highlight)", PaletteTypes.JobClasses, Color.DarkOrange);
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
        public ColorMapping JobRPRBase = new("RPR (Base)", PaletteTypes.JobClasses, Color.PaleVioletRed);
        public ColorMapping JobRPRHighlight = new("RPR (Highlight)", PaletteTypes.JobClasses, Color.DeepSkyBlue);
        public ColorMapping JobSGEBase = new("SGE (Base)", PaletteTypes.JobClasses, Color.LimeGreen);
        public ColorMapping JobSGEHighlight = new("SGE (Highlight)", PaletteTypes.JobClasses, Color.DodgerBlue);
        public ColorMapping JobVPRBase = new("VPR (Base)", PaletteTypes.JobClasses, Color.LimeGreen);
        public ColorMapping JobVPRHighlight = new("VPR (Highlight)", PaletteTypes.JobClasses, Color.YellowGreen);
        public ColorMapping JobPCTBase = new("PCT (Base)", PaletteTypes.JobClasses, Color.DodgerBlue);
        public ColorMapping JobPCTHighlight = new("PCT (Highlight)", PaletteTypes.JobClasses, Color.Magenta);
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

        //Raid Effects
        public ColorMapping RaidEffectEverkeepBase = new("Everkeep (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectEverkeepKeyHighlight = new("Everkeep (Key Highlights)", PaletteTypes.RaidEffects, Color.Red);
        public ColorMapping RaidEffectEverkeepHighlight1 = new("Everkeep (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0x34007E));
        public ColorMapping RaidEffectEverkeepHighlight2 = new("Everkeep (Highlight 2)", PaletteTypes.RaidEffects, Color.Blue);
        public ColorMapping RaidEffectEverkeepHighlight3 = new("Everkeep (Highlight 3)", PaletteTypes.RaidEffects, Color.Purple);
        public ColorMapping RaidEffectInterphosBase = new("Interphos (Base)", PaletteTypes.RaidEffects, Color.LimeGreen);
        public ColorMapping RaidEffectInterphosKeyHighlight = new("Interphos (Key Highlights)", PaletteTypes.RaidEffects, Color.DodgerBlue);
        public ColorMapping RaidEffectInterphosHighlight1 = new("Interphos (Highlight 1)", PaletteTypes.RaidEffects, Color.Green);
        public ColorMapping RaidEffectInterphosHighlight2 = new("Interphos (Highlight 2)", PaletteTypes.RaidEffects, Color.Lime);
        public ColorMapping RaidEffectInterphosHighlight3 = new("Interphos (Highlight 3)", PaletteTypes.RaidEffects, Color.Gold);
        public ColorMapping RaidEffectM1Base = new("Arcadion M1 (Base)", PaletteTypes.RaidEffects, Color.Magenta);
        public ColorMapping RaidEffectM1KeyHighlight = new("Arcadion M1 (Key Highlights)", PaletteTypes.RaidEffects, Color.Red);
        public ColorMapping RaidEffectM1Highlight1 = new("Arcadion M1 (Highlight 1)", PaletteTypes.RaidEffects, Color.DodgerBlue);
        public ColorMapping RaidEffectM1Highlight2 = new("Arcadion M1 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0x0A1DC6));
        public ColorMapping RaidEffectM1Highlight3 = new("Arcadion M1 (Highlight 3)", PaletteTypes.RaidEffects, Color.HotPink);
        public ColorMapping RaidEffectM2Base = new("Arcadion M2 (Base)", PaletteTypes.RaidEffects, Color.Yellow);
        public ColorMapping RaidEffectM2KeyHighlight = new("Arcadion M2 (Key Highlights)", PaletteTypes.RaidEffects, Color.Yellow);
        public ColorMapping RaidEffectM2Highlight1 = new("Arcadion M2 (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFFF00));
        public ColorMapping RaidEffectM2Highlight2 = new("Arcadion M2 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF1393));
        public ColorMapping RaidEffectM3Base = new("Arcadion M3 (Base)", PaletteTypes.RaidEffects, Color.Red);
        public ColorMapping RaidEffectM3KeyHighlight = new("Arcadion M3 (Key Highlights)", PaletteTypes.RaidEffects, Color.Lime);
        public ColorMapping RaidEffectM3Highlight1 = new("Arcadion M3 (Highlight 1)", PaletteTypes.RaidEffects, Color.DarkOrange);
        public ColorMapping RaidEffectM3Highlight2 = new("Arcadion M3 (Highlight 2)", PaletteTypes.RaidEffects, Color.Red);
        public ColorMapping RaidEffectM3Highlight3 = new("Arcadion M3 (Highlight 3)", PaletteTypes.RaidEffects, Color.DarkOrange);
        public ColorMapping RaidEffectM4Base = new("Arcadion M4 (Base)", PaletteTypes.RaidEffects, Color.Blue);
        public ColorMapping RaidEffectM4KeyHighlight = new("Arcadion M4 (Key Highlights)", PaletteTypes.RaidEffects, Color.Red);
        public ColorMapping RaidEffectM4Highlight1 = new("Arcadion M4 (Highlight 1)", PaletteTypes.RaidEffects, Color.Blue);
        public ColorMapping RaidEffectM4Highlight2 = new("Arcadion M4 (Highlight 2)", PaletteTypes.RaidEffects, Color.Magenta);
        public ColorMapping RaidEffectM4Highlight3 = new("Arcadion M4 (Highlight 3)", PaletteTypes.RaidEffects, Color.White);
        public ColorMapping RaidEffectM4Highlight4 = new("Arcadion M4 (Highlight 4)", PaletteTypes.RaidEffects, Color.FromArgb(0x00ACFF));
        public ColorMapping RaidEffectM5Base = new("Arcadion M5 (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectM5KeyHighlight = new("Arcadion M5 (Key Highlights)", PaletteTypes.RaidEffects, Color.LimeGreen);
        public ColorMapping RaidEffectM5Highlight1 = new("Arcadion M5 (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0x0096FF));
        public ColorMapping RaidEffectM5Highlight2 = new("Arcadion M5 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF32C8));
        public ColorMapping RaidEffectM5Highlight3 = new("Arcadion M5 (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFED00));
        public ColorMapping RaidEffectM6Base = new("Arcadion M6 (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectM6KeyHighlight = new("Arcadion M6 (Key Highlights)", PaletteTypes.RaidEffects, Color.White);
        public ColorMapping RaidEffectM6Highlight1 = new("Arcadion M6 (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF0064));
        public ColorMapping RaidEffectM6Highlight2 = new("Arcadion M6 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0x0064FF));
        public ColorMapping RaidEffectM6Highlight3 = new("Arcadion M6 (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFC800));
        public ColorMapping RaidEffectM6Highlight4 = new("Arcadion M6 (Highlight 4)", PaletteTypes.RaidEffects, Color.FromArgb(0x2BFF00));
        public ColorMapping RaidEffectM7Base = new("Arcadion M7 (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectM7KeyHighlight = new("Arcadion M7 (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0x1300FF));
        public ColorMapping RaidEffectM7Highlight1 = new("Arcadion M7 (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF0000));
        public ColorMapping RaidEffectM7Highlight2 = new("Arcadion M7 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFA308));
        public ColorMapping RaidEffectM7Highlight3 = new("Arcadion M7 (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0x6900FF));
        public ColorMapping RaidEffectM8Base = new("Arcadion M8 (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectM8KeyHighlight = new("Arcadion M8 (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFF500));
        public ColorMapping RaidEffectM8Highlight1 = new("Arcadion M8 (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0x00F2FF));
        public ColorMapping RaidEffectM8Highlight2 = new("Arcadion M8 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0x0004FF));
        public ColorMapping RaidEffectM8Highlight3 = new("Arcadion M8 (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFFFFF));
        public ColorMapping RaidEffectM8SBase = new("Arcadion M8 Savage Phase (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectM8SKeyHighlight = new("Arcadion M8 Savage Phase (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0x0027FF));
        public ColorMapping RaidEffectM8SHighlight1 = new("Arcadion M8 Savage Phase (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF0000));
        public ColorMapping RaidEffectM8SHighlight2 = new("Arcadion M8 Savage Phase (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFF501));
        public ColorMapping RaidEffectM8SHighlight3 = new("Arcadion M8 Savage Phase (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF8C00));
        public ColorMapping RaidEffectM9Base = new("Arcadion M9 (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectM9KeyHighlight = new("Arcadion M9 (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0xCB00A5));
        public ColorMapping RaidEffectM9KeyHighlight1 = new("Arcadion M9 (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF0000));
        public ColorMapping RaidEffectM9KeyHighlight2 = new("Arcadion M9 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF00F9));
        public ColorMapping RaidEffectM10Base = new("Arcadion M10 (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectM10KeyHighlight = new("Arcadion M10 (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFF505));
        public ColorMapping RaidEffectM10KeyHighlight1 = new("Arcadion M10 (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF0000));
        public ColorMapping RaidEffectM10KeyHighlight2 = new("Arcadion M10 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF00F9));
        public ColorMapping RaidEffectM11Base = new("Arcadion M10 (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectM11KeyHighlight = new("Arcadion M10 (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0x4E00FF));
        public ColorMapping RaidEffectM11KeyHighlight1 = new("Arcadion M10 (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xA70000));
        public ColorMapping RaidEffectM11KeyHighlight2 = new("Arcadion M10 (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0x60007E));
        public ColorMapping RaidEffectM11KeyHighlight3 = new("Arcadion M10 (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0xBD9F00));
        public ColorMapping RaidEffectM11KeyHighlight4 = new("Arcadion M10 (Highlight 4)", PaletteTypes.RaidEffects, Color.FromArgb(0x019DBB));
        public ColorMapping RaidEffectM12KeyHighlight = new("Arcadion M12 (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0x990000));
        public ColorMapping RaidEffectM12SKeyHighlight = new("Arcadion M12S Savage Phase (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0x00FF80));
        public ColorMapping RaidEffectCoDBase = new("The Cloud of Darkness (Base)", PaletteTypes.RaidEffects, Color.MediumPurple);
        public ColorMapping RaidEffectCoDKeyHighlight = new("The Cloud of Darkness (Key Highlights)", PaletteTypes.RaidEffects, Color.Purple);
        public ColorMapping RaidEffectCoDHighlight1 = new("The Cloud of Darkness (Highlight 1)", PaletteTypes.RaidEffects, Color.Magenta);
        public ColorMapping RaidEffectCoDHighlight2 = new("The Cloud of Darkness (Highlight 2)", PaletteTypes.RaidEffects, Color.MediumPurple);
        public ColorMapping RaidEffectCoDHighlight3 = new("The Cloud of Darkness (Highlight 3)", PaletteTypes.RaidEffects, Color.Purple);
        public ColorMapping RaidEffectHoRBase = new("Hell on Rails (Base)", PaletteTypes.RaidEffects, Color.FromArgb(0x0F002A));
        public ColorMapping RaidEffectHoRKeyHighlight = new("Hell on Rails (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0x0075FF));
        public ColorMapping RaidEffectHoRHighlight1 = new("Hell on Rails (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0x00FF64));
        public ColorMapping RaidEffectHoRHighlight2 = new("Hell on Rails (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xC800C1));
        public ColorMapping RaidEffectNecronBase = new("Necron's Embrace (Base)", PaletteTypes.RaidEffects, Color.FromArgb(0x0F002A));
        public ColorMapping RaidEffectNecronKeyHighlight = new("Necron's Embrace (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFFFFF));
        public ColorMapping RaidEffectNecronHighlight1 = new("Necron's Embrace (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0x00C8FF));
        public ColorMapping RaidEffectNecronHighlight2 = new("Necron's Embrace (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF4A00));
        public ColorMapping RaidEffectNecronHighlight3 = new("Necron's Embrace (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0x5D00FF));
        public ColorMapping RaidEffectNecronHighlight4 = new("Necron's Embrace (Highlight 4)", PaletteTypes.RaidEffects, Color.FromArgb(0x0400FF));
        public ColorMapping RaidEffectRecollectionBase = new("Recollection (Base)", PaletteTypes.RaidEffects, Color.FromArgb(0x3E00FF));
        public ColorMapping RaidEffectRecollectionKeyHighlight = new("Recollection (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0xFF0094));
        public ColorMapping RaidEffectRecollectionHighlight1 = new("Recollection (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFED00));
        public ColorMapping RaidEffectRecollectionHighlight2 = new("Recollection (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFA000));
        public ColorMapping RaidEffectRecollectionHighlight3 = new("Recollection (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0x3E00FF));
        public ColorMapping RaidEffectTheUnmakingBase = new("The Unmaking (Base)", PaletteTypes.RaidEffects, Color.Black);
        public ColorMapping RaidEffectTheUnmakingKeyHighlight = new("The Unmaking (Key Highlights)", PaletteTypes.RaidEffects, Color.FromArgb(0xC70000));
        public ColorMapping RaidEffectTheUnmakingHighlight1 = new("The Unmaking (Highlight 1)", PaletteTypes.RaidEffects, Color.FromArgb(0xFFFFFF));
        public ColorMapping RaidEffectTheUnmakingHighlight2 = new("The Unmaking (Highlight 2)", PaletteTypes.RaidEffects, Color.FromArgb(0x009CFF));
        public ColorMapping RaidEffectTheUnmakingHighlight3 = new("The Unmaking (Highlight 3)", PaletteTypes.RaidEffects, Color.FromArgb(0x8400FF));


        // Audio Visualizer
        public ColorMapping AudioVisualizerBase = new("Audio Visualizer (Base)", PaletteTypes.AudioVisualizer, Color.Black);
        public ColorMapping AudioVisualizerLow = new("Audio Visualizer (Low Freq)", PaletteTypes.AudioVisualizer, Color.FromArgb(unchecked((int)0xFF00CC00)));
        public ColorMapping AudioVisualizerMid = new("Audio Visualizer (Mid Freq)", PaletteTypes.AudioVisualizer, Color.Yellow);
        public ColorMapping AudioVisualizerHigh = new("Audio Visualizer (High Freq)", PaletteTypes.AudioVisualizer, Color.Red);
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
        public string ColorMappingJobVPRNegative { get; set; }
        public string ColorMappingJobPCTNegative { get; set; }

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
        public string ColorMappingJobVPRBase { get; set; }
        public string ColorMappingJobVPRHighlight { get; set; }
        public string ColorMappingJobPCTBase { get; set; }
        public string ColorMappingJobPCTHighlight { get; set; }
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
