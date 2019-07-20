using System.Drawing;

namespace Chromatics.Datastore
{
    //Device Managment
    public class DeviceDataStore
    {
        public bool DeviceOperationCoolermasterKeyboard = true;
        public bool DeviceOperationCoolermasterMouse = true;
        public bool DeviceOperationCorsairHeadset = true;
        public bool DeviceOperationCorsairKeyboard = true;
        public bool DeviceOperationCorsairKeypad = false; //Not Implemented
        public bool DeviceOperationCorsairMouse = true;
        public bool DeviceOperationCorsairMousepad = true;
        public string DeviceOperationHueDefault = "";
        public string DeviceOperationHueDevices = "";
        public string DeviceOperationLifxDevices = "";
        public bool DeviceOperationLogitechHeadset = true;
        public bool DeviceOperationLogitechKeyboard = true;
        public bool DeviceOperationLogitechKeypad = true;
        public bool DeviceOperationLogitechMouse = true;
        public bool DeviceOperationLogitechMousepad = true;
        public int DeviceOperationMouseToggle = 0;
        public bool DeviceOperationRazerHeadset = true;
        public bool DeviceOperationRazerKeyboard = true;
        public bool DeviceOperationRazerKeypad = true;
        public bool DeviceOperationRazerMouse = true;
        public bool DeviceOperationRazerMousepad = true;
        public bool DeviceOperationRazerChromaLink = true;
        public bool DeviceOperationRoccatKeyboard = true;
        public bool DeviceOperationRoccatMouse = true;
        public bool DeviceOperationSteelHeadset = true;
        public bool DeviceOperationSteelKeyboard = true;
        public bool DeviceOperationSteelMouse = true;
        public bool DeviceOperationWootingKeyboard = true;

        public bool DeviceOperationKeyboard = true;
        public bool DeviceOperationMouse = true;
        public bool DeviceOperationMousepad = true;
        public bool DeviceOperationHeadset = true;
        public bool DeviceOperationKeypad = true;
        public bool DeviceOperationCL = true;

        public bool KeysSingleKeyModeEnabled = false;
        public bool KeysMultiKeyModeEnabled = false;
        public string KeySingleKeyMode = "Disabled";
        public string KeyMultiKeyMode = "Disabled";

        public string MouseZone1Mode = "DefaultColor";
        public string MouseZone2Mode = "EnmityTracker";
        public string MouseZone3Mode = "DefaultColor";
        public string MouseStrip1Mode = "HpTracker";
        public string MouseStrip2Mode = "MpTracker";

        public string PadZone1Mode = "HpTracker";
        public string PadZone2Mode = "MpTracker";
        public string PadZone3Mode = "MpTracker";

        public string CLZone1Mode = "DefaultColor";
        public string CLZone2Mode = "DefaultColor";
        public string CLZone3Mode = "DefaultColor";
        public string CLZone4Mode = "DefaultColor";
        public string CLZone5Mode = "DefaultColor";
        public string CLZone6Mode = "DefaultColor";

        public string HeadsetZone1Mode = "DefaultColor";
        public string HeadsetZone2Mode = "HighlightColor";
        public string KeypadZone1Mode = "DefaultColor";

        public string LightbarMode = "TargetHp";
        public string FKeyMode = "HpMpTp";

        public int ChromaLinkLEDCountZ1 = 10;
        public int ChromaLinkLEDCountZ2 = 10;
        public int ChromaLinkLEDCountZ3 = 10;
        public int ChromaLinkLEDCountZ4 = 10;
        public int ChromaLinkLEDCountZ5 = 10;
        public int ChromaLinkLEDCountZ6 = 10;

        public bool SDKRazer = true;
        public bool SDKLogitech = true;
        public bool SDKCorsair = true;
        public bool SDKCooler = true;
        public bool SDKSteelSeries = true;
        public bool SDKWooting = true;
        public bool SDKAsus = true;
        public bool SDKMystic = true;
        public bool SDKLifx = true;

        public bool EnableKeypadBinds = false;
        public string KeypadZ1Bind = "1";
        public string KeypadZ2Bind = "2";
        public string KeypadZ3Bind = "3";
        public string KeypadZ4Bind = "4";
        public string KeypadZ5Bind = "5";
        public string KeypadZ6Bind = "6";
        public string KeypadZ7Bind = "7";
        public string KeypadZ8Bind = "8";
        public string KeypadZ9Bind = "9";
        public string KeypadZ10Bind = "10";
        public string KeypadZ11Bind = "11";
        public string KeypadZ12Bind = "12";
        public string KeypadZ13Bind = "13";
        public string KeypadZ14Bind = "14";
        public string KeypadZ15Bind = "15";
        public string KeypadZ16Bind = "16";
        public string KeypadZ17Bind = "17";
        public string KeypadZ18Bind = "18";
        public string KeypadZ19Bind = "19";
        public string KeypadZ20Bind = "20";
        public bool KZ1Enabled = true;
        public bool KZ2Enabled = true;
        public bool KZ3Enabled = true;
        public bool KZ4Enabled = true;
        public bool KZ5Enabled = true;
        public bool KZ6Enabled = true;
        public bool KZ7Enabled = true;
        public bool KZ8Enabled = true;
        public bool KZ9Enabled = true;
        public bool KZ10Enabled = true;
        public bool KZ11Enabled = true;
        public bool KZ12Enabled = true;
        public bool KZ13Enabled = true;
        public bool KZ14Enabled = true;
        public bool KZ15Enabled = true;
        public bool KZ16Enabled = true;
        public bool KZ17Enabled = true;
        public bool KZ18Enabled = true;
        public bool KZ19Enabled = true;
        public bool KZ20Enabled = true;

        public bool OtherInterpolateEffects = false;
        public bool ReverseInterpolateEffects = false;
    }

    //Settings
    public class ChromaticsSettings
    {
        public string ChromaticsSettingsArxactip = "http://127.0.0.1:8085";
        public string ChromaticsSettingsArxMode = "Player HUD";
        public int ChromaticsSettingsArxTheme = 0;
        public bool ChromaticsSettingsArxToggle = true;
        public bool ChromaticsSettingsAzertyMode = false;
        public bool ChromaticsSettingsCastAnimate = true;
        public bool ChromaticsSettingsCastToggle = true;
        public bool ChromaticsSettingsDfBellToggle = true;
        public bool ChromaticsSettingsGcdCountdown = true;
        public bool ChromaticsSettingsImpactToggle = false;
        public bool ChromaticsSettingsJobGaugeToggle = true;
        public bool ChromaticsSettingsKeybindToggle = true;
        public bool ChromaticsSettingsStatusEffectToggle = false;
        public string ChromaticsSettingsBaseMode = "Static Colors";
        public int ChromaticsSettingsLanguage = 0;
        public int ChromaticsSettingsPreviousLanguage = 0;
        public KeyRegion ChromaticsSettingsQwertyMode = 0;
        public bool ChromaticsSettingsDebugOpt = true;
        public bool ChromaticsSettingsCutsceneAnimation = true;
        public bool ChromaticsSettingsVegasMode = true;
        public int ChromaticsSettingsCriticalHP = 10;
        public string ChromaticsSettingsACTMode = "DPS";
        public bool ChromaticsSettingsACTFlash = false;
        public bool ChromaticsSettingsACTFlashCustomTrigger = false;
        public bool ChromaticsSettingsACTFlashTimer = false;
        public bool ChromaticsSettingsKeyHighlights = true;
        public bool ChromaticsSettingsLccAuto = false;
        public bool ChromaticsSettingsMemoryCache = false;
        public bool ChromaticsSettingsDesktopNotifications = true;
        public bool ChromaticsSettingsLcdEnabled = false;
        public string FinalFantasyXivVersion = "5.0";
        public bool FirstRun = false;
        public bool ChromaticsSettingsCastEnabled = false;
        public string ChromaticsSettingsCastDevice = "";
        public bool ChromaticsSettingsCastDFBell = false;
        public bool ChromaticsSettingsCastAlarmBell = false;
        public string ChromaticsSettingsCastAlarmTime = "12:00 AM";
        public bool ChromaticsSettingsCastSRankAlert = false;
        public bool ChromaticsSettingsCastReadyCheckAlert = false;
        public bool ChromaticsSettingsIFTTTEnable = false;
        public string ChromaticsSettingsIFTTTURL = "";
        public int ChromaticsSettingsPollingInterval = 200;
        public bool ChromaticsSettingsExtraBulbEffects = false;
        public bool ChromaticsSettingsReleaseDevices = false;

        public int[][] ChromaticsSettingsACTDPS =
        {
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 }
        };

        public int[][] ChromaticsSettingsACTHPS =
        {
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 },
            new[] { 5000 }
        };

        public int[][] ChromaticsSettingsACTGroupDPS =
        {
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 },
            new[] { 25000 }
        };

        public int[][] ChromaticsSettingsACTTargetCrit =
        {
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 }
        };

        public int[][] ChromaticsSettingsACTTargetDH =
        {
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 }
        };

        public int[][] ChromaticsSettingsACTTargetCritDH =
        {
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 }
        };

        public int[][] ChromaticsSettingsACTOverheal =
        {
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 }
        };

        public int[][] ChromaticsSettingsACTDamage =
        {
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 },
            new[] { 10 }
        };
    }

    //Color Mapping
    public class FfxivColorMappings
    {
        public string ColorMappingAmnesia = ColorTranslator.ToHtml(Color.Snow);
        public string ColorMappingBaseColor = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingBind = ColorTranslator.ToHtml(Color.BlueViolet);
        public string ColorMappingBleed = ColorTranslator.ToHtml(Color.IndianRed);
        public string ColorMappingBurns = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingCastChargeEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingCastChargeFull = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingCpEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingCpFull = ColorTranslator.ToHtml(Color.Purple);
        public string ColorMappingDamageDown = ColorTranslator.ToHtml(Color.PaleVioletRed);
        public string ColorMappingDaze = ColorTranslator.ToHtml(Color.PaleVioletRed);
        public string ColorMappingDeepFreeze = ColorTranslator.ToHtml(Color.RoyalBlue);
        public string ColorMappingDropsy = ColorTranslator.ToHtml(Color.DeepSkyBlue);
        public string ColorMappingDutyFinderBell = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingEmnity0 = ColorTranslator.ToHtml(Color.Green);
        public string ColorMappingEmnity1 = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingEmnity2 = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMappingEmnity3 = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingEmnity4 = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingGcdEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingGcdHot = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingGcdReady = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingGpEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingGpFull = ColorTranslator.ToHtml(Color.SkyBlue);
        public string ColorMappingHeavy = ColorTranslator.ToHtml(Color.DarkCyan);
        public string ColorMappingHighlightColor = ColorTranslator.ToHtml(Color.Magenta);
        public string ColorMappingHotbarCd = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingHotbarNotAvailable = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingHotbarOutRange = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingHotbarProc = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingHotbarReady = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingKeybindMap = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindAetherCurrents = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindSigns = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindWaymarks = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindRecordReadyCheck = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindReadyCheck = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindCountdown = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindEmotes = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindCrossWorldLS = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindLinkshells = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindContacts = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindSprint = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindTeleport = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindReturn = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindLimitBreak = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindDutyAction = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindRepair = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindDig = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingKeybindInventory = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingHpCritical = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingHpEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingHpFull = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingHpLoss = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingIncapacitation = ColorTranslator.ToHtml(Color.DarkRed);
        public string ColorMappingInfirmary = ColorTranslator.ToHtml(Color.PaleVioletRed);
        public string ColorMappingLeaden = ColorTranslator.ToHtml(Color.DarkGray);
        public string ColorMappingMisery = ColorTranslator.ToHtml(Color.MidnightBlue);
        public string ColorMappingMpEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingMpFull = ColorTranslator.ToHtml(Color.Magenta);
        public string ColorMappingNoEmnity = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingParalysis = ColorTranslator.ToHtml(Color.PeachPuff);
        public string ColorMappingPetrification = ColorTranslator.ToHtml(Color.SlateGray);
        public string ColorMappingPoison = ColorTranslator.ToHtml(Color.DarkGreen);
        public string ColorMappingPollen = ColorTranslator.ToHtml(Color.Goldenrod);
        public string ColorMappingPox = ColorTranslator.ToHtml(Color.PaleVioletRed);
        public string ColorMappingSilence = ColorTranslator.ToHtml(Color.DarkBlue);
        public string ColorMappingSleep = ColorTranslator.ToHtml(Color.GhostWhite);
        public string ColorMappingSlow = ColorTranslator.ToHtml(Color.YellowGreen);
        public string ColorMappingStun = ColorTranslator.ToHtml(Color.Snow);
        public string ColorMappingTargetCasting = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingTargetHpFriendly = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingTargetHpClaimed = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingTargetHpEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingTargetHpIdle = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingExpEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingExpFull = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingJobWARNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobWARWrathBurst = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingJobWARWrathMax = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobPLDNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobPLDShieldOath = ColorTranslator.ToHtml(Color.Khaki);
        public string ColorMappingJobPLDSwordOath = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingJobMNKNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobMNKGreased = ColorTranslator.ToHtml(Color.Aqua);
        public string ColorMappingJobDRGNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobDRGBloodDragon = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobBRDNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobBRDRepertoire = ColorTranslator.ToHtml(Color.GhostWhite);
        public string ColorMappingJobBRDBallad = ColorTranslator.ToHtml(Color.MediumSlateBlue);
        public string ColorMappingJobBRDArmys = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingJobBRDMinuet = ColorTranslator.ToHtml(Color.MediumSpringGreen);
        public string ColorMappingJobWHMNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobWHMFlowerPetal = ColorTranslator.ToHtml(Color.MediumVioletRed);
        public string ColorMappingJobWHMFreecure = ColorTranslator.ToHtml(Color.LightSeaGreen);
        public string ColorMappingJobBLMNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobBLMAstralFire = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingJobBLMUmbralIce = ColorTranslator.ToHtml(Color.DeepSkyBlue);
        public string ColorMappingJobBLMUmbralHeart = ColorTranslator.ToHtml(Color.DeepPink);
        public string ColorMappingJobBLMEnochian = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingJobSMNNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobSMNAetherflow = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobSCHNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobSCHAetherflow = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobNINNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobNINHuton = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingJobDRKNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobDRKBloodGauge = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobDRKGrit = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingJobDRKDarkside = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingJobASTNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobASTArrow = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingJobASTBalance = ColorTranslator.ToHtml(Color.Crimson);
        public string ColorMappingJobASTBole = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingJobASTEwer = ColorTranslator.ToHtml(Color.MediumBlue);
        public string ColorMappingJobASTSpear = ColorTranslator.ToHtml(Color.Turquoise);
        public string ColorMappingJobASTSpire = ColorTranslator.ToHtml(Color.SlateBlue);
        public string ColorMappingJobMCHNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobMCHAmmo = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMappingJobMCHHeatGauge = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobMCHOverheat = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingJobSAMNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobSAMSetsu = ColorTranslator.ToHtml(Color.Aquamarine);
        public string ColorMappingJobSAMGetsu = ColorTranslator.ToHtml(Color.Azure);
        public string ColorMappingJobSAMKa = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingJobSAMKenki = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobRDMNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobRDMBlackMana = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobRDMWhiteMana = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingJobDNCNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobDNCEntrechat = ColorTranslator.ToHtml(Color.Blue);
        public string ColorMappingJobDNCPirouette = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingJobDNCEmboite = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobDNCJete = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingJobDNCStandardFinish = ColorTranslator.ToHtml(Color.Aquamarine);
        public string ColorMappingJobDNCTechnicalFinish = ColorTranslator.ToHtml(Color.MediumVioletRed);
        public string ColorMappingJobGNBNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobGNBRoyalGuard = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingMenuBase = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingJobCrafterNegative = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingJobCrafterInnerquiet = ColorTranslator.ToHtml(Color.BlueViolet);
        public string ColorMappingJobCrafterCollectable = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMappingJobCrafterCrafter = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingMenuHighlight1 = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingMenuHighlight2 = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingMenuHighlight3 = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingCutsceneBase = ColorTranslator.ToHtml(Color.DeepSkyBlue);
        public string ColorMappingCutsceneHighlight1 = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingCutsceneHighlight2 = ColorTranslator.ToHtml(Color.DeepSkyBlue);
        public string ColorMappingCutsceneHighlight3 = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingWeatherClearBase = ColorTranslator.ToHtml(Color.SkyBlue);
        public string ColorMappingWeatherClearHighlight = ColorTranslator.ToHtml(Color.LightYellow);
        public string ColorMappingWeatherFairBase = ColorTranslator.ToHtml(Color.SkyBlue);
        public string ColorMappingWeatherFairHighlight = ColorTranslator.ToHtml(Color.LightYellow);
        public string ColorMappingWeatherCloudsBase = ColorTranslator.ToHtml(Color.LightSlateGray);
        public string ColorMappingWeatherCloudsHighlight = ColorTranslator.ToHtml(Color.Azure);
        public string ColorMappingWeatherFogBase = ColorTranslator.ToHtml(Color.DarkSlateGray);
        public string ColorMappingWeatherFogHighlight = ColorTranslator.ToHtml(Color.Azure);
        public string ColorMappingWeatherWindBase = ColorTranslator.ToHtml(Color.LightSlateGray);
        public string ColorMappingWeatherWindHighlight = ColorTranslator.ToHtml(Color.Aquamarine);
        public string ColorMappingWeatherGalesBase = ColorTranslator.ToHtml(Color.LightSlateGray);
        public string ColorMappingWeatherGalesHighlight = ColorTranslator.ToHtml(Color.Aquamarine);
        public string ColorMappingWeatherRainBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherRainHighlight = ColorTranslator.ToHtml(Color.DarkBlue);
        public string ColorMappingWeatherShowersBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherShowersHighlight = ColorTranslator.ToHtml(Color.DarkBlue);
        public string ColorMappingWeatherThunderBase = ColorTranslator.ToHtml(Color.LightSlateGray);
        public string ColorMappingWeatherThunderHighlight = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherThunderstormsBase = ColorTranslator.ToHtml(Color.DarkBlue);
        public string ColorMappingWeatherThunderstormsHighlight = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherDustBase = ColorTranslator.ToHtml(Color.DarkSalmon);
        public string ColorMappingWeatherDustHighlight = ColorTranslator.ToHtml(Color.Coral);
        public string ColorMappingWeatherSandstormBase = ColorTranslator.ToHtml(Color.DarkSalmon);
        public string ColorMappingWeatherSandstormHighlight = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingWeatherHotspellBase = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingWeatherHotspellHighlight = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingWeatherHeatwaveBase = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingWeatherHeatwaveHighlight = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingWeatherSnowBase = ColorTranslator.ToHtml(Color.Snow);
        public string ColorMappingWeatherSnowHighlight = ColorTranslator.ToHtml(Color.DarkTurquoise);
        public string ColorMappingWeatherBlizzardsBase = ColorTranslator.ToHtml(Color.Snow);
        public string ColorMappingWeatherBlizzardsHighlight = ColorTranslator.ToHtml(Color.LightSlateGray);
        public string ColorMappingWeatherGloomBase = ColorTranslator.ToHtml(Color.Magenta);
        public string ColorMappingWeatherGloomHighlight = ColorTranslator.ToHtml(Color.Azure);
        public string ColorMappingWeatherAurorasBase = ColorTranslator.ToHtml(Color.Turquoise);
        public string ColorMappingWeatherAurorasHighlight = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherDarknessBase = ColorTranslator.ToHtml(Color.DarkBlue);
        public string ColorMappingWeatherDarknessHighlight = ColorTranslator.ToHtml(Color.DarkMagenta);
        public string ColorMappingWeatherTensionBase = ColorTranslator.ToHtml(Color.DarkOrchid);
        public string ColorMappingWeatherTensionHighlight = ColorTranslator.ToHtml(Color.Magenta);
        public string ColorMappingWeatherStormcloudsBase = ColorTranslator.ToHtml(Color.DarkSlateGray);
        public string ColorMappingWeatherStormcloudsHighlight = ColorTranslator.ToHtml(Color.DarkTurquoise);
        public string ColorMappingWeatherRoughseasBase = ColorTranslator.ToHtml(Color.CornflowerBlue);
        public string ColorMappingWeatherRoughseasHighlight = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherLouringBase = ColorTranslator.ToHtml(Color.Maroon);
        public string ColorMappingWeatherLouringHighlight = ColorTranslator.ToHtml(Color.Magenta);
        public string ColorMappingWeatherEruptionsBase = ColorTranslator.ToHtml(Color.DarkRed);
        public string ColorMappingWeatherEruptionsHighlight = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingWeatherIrradianceBase = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMappingWeatherIrradianceHighlight = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingWeatherCoreradiationBase = ColorTranslator.ToHtml(Color.GreenYellow);
        public string ColorMappingWeatherCoreradiationHighlight = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMappingWeatherShelfcloudsBase = ColorTranslator.ToHtml(Color.LightSlateGray);
        public string ColorMappingWeatherShelfcloudsHighlight = ColorTranslator.ToHtml(Color.Azure);
        public string ColorMappingWeatherOppressionBase = ColorTranslator.ToHtml(Color.DarkMagenta);
        public string ColorMappingWeatherOppressionHighlight = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherUmbralwindBase = ColorTranslator.ToHtml(Color.DarkTurquoise);
        public string ColorMappingWeatherUmbralwindHighlight = ColorTranslator.ToHtml(Color.Azure);
        public string ColorMappingWeatherUmbralstaticBase = ColorTranslator.ToHtml(Color.DarkTurquoise);
        public string ColorMappingWeatherUmbralstaticHighlight = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherSmokeBase = ColorTranslator.ToHtml(Color.DarkSlateGray);
        public string ColorMappingWeatherSmokeHighlight = ColorTranslator.ToHtml(Color.Azure);
        public string ColorMappingWeatherRoyallevinBase = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherRoyallevinHighlight = ColorTranslator.ToHtml(Color.DarkBlue);
        public string ColorMappingWeatherHyperelectricityBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherHyperelectricityHighlight = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherMultiplicityBase = ColorTranslator.ToHtml(Color.DarkTurquoise);
        public string ColorMappingWeatherMultiplicityHighlight = ColorTranslator.ToHtml(Color.DarkGray);
        public string ColorMappingWeatherDragonstormBase = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMappingWeatherDragonstormHighlight = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherSubterrainBase = ColorTranslator.ToHtml(Color.Goldenrod);
        public string ColorMappingWeatherSubterrainHighlight = ColorTranslator.ToHtml(Color.SandyBrown);
        public string ColorMappingWeatherConcordanceBase = ColorTranslator.ToHtml(Color.Brown);
        public string ColorMappingWeatherConcordanceHighlight = ColorTranslator.ToHtml(Color.SandyBrown);
        public string ColorMappingWeatherBeyondtimeBase = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMappingWeatherBeyondtimeHighlight = ColorTranslator.ToHtml(Color.Turquoise);
        public string ColorMappingWeatherDemonicinfinityBase = ColorTranslator.ToHtml(Color.DarkRed);
        public string ColorMappingWeatherDemonicinfinityHighlight = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingWeatherDimensionaldisruptionBase = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherDimensionaldisruptionHighlight = ColorTranslator.ToHtml(Color.DarkMagenta);
        public string ColorMappingWeatherRevelstormBase = ColorTranslator.ToHtml(Color.LightSkyBlue);
        public string ColorMappingWeatherRevelstormHighlight = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherEternalblissBase = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMappingWeatherEternalblissHighlight = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherWyrmstormBase = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherWyrmstormHighlight = ColorTranslator.ToHtml(Color.DarkSlateGray);
        public string ColorMappingWeatherQuicklevinBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherQuicklevinHighlight = ColorTranslator.ToHtml(Color.Blue);
        public string ColorMappingWeatherWhitecycloneBase = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingWeatherWhitecycloneHighlight = ColorTranslator.ToHtml(Color.Turquoise);
        public string ColorMappingWeatherGeostormsBase = ColorTranslator.ToHtml(Color.Aquamarine);
        public string ColorMappingWeatherGeostormsHighlight = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingWeatherTrueblueBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherTrueblueHighlight = ColorTranslator.ToHtml(Color.Turquoise);
        public string ColorMappingWeatherUmbralturbulenceBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingWeatherUmbralturbulenceHighlight = ColorTranslator.ToHtml(Color.DarkTurquoise);
        public string ColorMappingWeatherEverlastinglightBase = ColorTranslator.ToHtml(Color.Salmon);
        public string ColorMappingWeatherEverlastinglightHighlight = ColorTranslator.ToHtml(Color.Turquoise);
        public string ColorMappingWeatherTerminationBase = ColorTranslator.ToHtml(Color.DarkMagenta);
        public string ColorMappingWeatherTerminationHighlight = ColorTranslator.ToHtml(Color.MediumPurple);
        public string ColorMappingPullCountdownTick = ColorTranslator.ToHtml(Color.Turquoise);
        public string ColorMappingPullCountdownEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingPullCountdownEngage = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingACTThresholdEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingACTThresholdBuild = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingACTThresholdSuccess = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingACTThresholdFlash = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingACTCustomTriggerIdle = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingACTCustomTriggerBell = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMappingACTTimerIdle = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingACTTimerBuild = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingACTTimerFlash = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingACTEnrageEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingACTEnrageCountdown = ColorTranslator.ToHtml(Color.DarkTurquoise);
        public string ColorMappingACTEnrageWarning = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobPLDBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingJobPLDHighlight = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingJobMNKBase = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingJobMNKHighlight = ColorTranslator.ToHtml(Color.Brown);
        public string ColorMappingJobWARBase = ColorTranslator.ToHtml(Color.Blue);
        public string ColorMappingJobWARHighlight = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingJobDRGBase = ColorTranslator.ToHtml(Color.Maroon);
        public string ColorMappingJobDRGHighlight = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobBRDBase = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingJobBRDHighlight = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingJobWHMBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingJobWHMHighlight = ColorTranslator.ToHtml(Color.Snow);
        public string ColorMappingJobBLMBase = ColorTranslator.ToHtml(Color.DarkMagenta);
        public string ColorMappingJobBLMHighlight = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMappingJobSMNBase = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingJobSMNHighlight = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMappingJobSCHBase = ColorTranslator.ToHtml(Color.MediumSpringGreen);
        public string ColorMappingJobSCHHighlight = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingJobNINBase = ColorTranslator.ToHtml(Color.DarkMagenta);
        public string ColorMappingJobNINHighlight = ColorTranslator.ToHtml(Color.RosyBrown);
        public string ColorMappingJobMCHBase = ColorTranslator.ToHtml(Color.SaddleBrown);
        public string ColorMappingJobMCHHighlight = ColorTranslator.ToHtml(Color.SandyBrown);
        public string ColorMappingJobDRKBase = ColorTranslator.ToHtml(Color.Blue);
        public string ColorMappingJobDRKHighlight = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingJobASTBase = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingJobASTHighlight = ColorTranslator.ToHtml(Color.MediumSpringGreen);
        public string ColorMappingJobSAMBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobSAMHighlight = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingJobRDMBase = ColorTranslator.ToHtml(Color.MediumVioletRed);
        public string ColorMappingJobRDMHighlight = ColorTranslator.ToHtml(Color.White);
        public string ColorMappingJobDNCBase = ColorTranslator.ToHtml(Color.BlueViolet);
        public string ColorMappingJobDNCHighlight = ColorTranslator.ToHtml(Color.CornflowerBlue);
        public string ColorMappingJobGNBBase = ColorTranslator.ToHtml(Color.DarkMagenta);
        public string ColorMappingJobGNBHighlight = ColorTranslator.ToHtml(Color.Blue);
        public string ColorMappingJobBLUBase = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMappingJobBLUHighlight = ColorTranslator.ToHtml(Color.Blue);
        public string ColorMappingJobCPTBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobCPTHighlight = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobBSMBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobBSMHighlight = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobARMBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobARMHighlight = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobGSMBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobGSMHighlight = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobLTWBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobLTWHighlight = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobWVRBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobWVRHighlight = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobALCBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobALCHighlight = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobCULBase = ColorTranslator.ToHtml(Color.DarkOrange);
        public string ColorMappingJobCULHighlight = ColorTranslator.ToHtml(Color.Orchid);
        public string ColorMappingJobMINBase = ColorTranslator.ToHtml(Color.Gray);
        public string ColorMappingJobMINHighlight = ColorTranslator.ToHtml(Color.Blue);
        public string ColorMappingJobBTNBase = ColorTranslator.ToHtml(Color.MediumSpringGreen);
        public string ColorMappingJobBTNHighlight = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingJobFSHBase = ColorTranslator.ToHtml(Color.DeepSkyBlue);
        public string ColorMappingJobFSHHighlight = ColorTranslator.ToHtml(Color.White);
    }

    public static class DataStoreFunctions
    {
        public static string GetName<T>(T item) where T : class
        {
            return typeof(T).GetProperties()[0].Name;
        }
    }
}