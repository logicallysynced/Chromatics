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

        public bool DeviceOperationKeyboard = true;
        public bool DeviceOperationMouse = true;
        public bool DeviceOperationMousepad = true;
        public bool DeviceOperationHeadset = true;
        public bool DeviceOperationKeypad = true;
        public bool DeviceOperationCL = true;

        public bool KeysSingleKeyModeEnabled = false;
        public string KeySingleKeyMode = "Disabled";

        public string MouseZone1Mode = "DefaultColor";
        public string MouseZone2Mode = "EnmityTracker";
        public string MouseZone3Mode = "DefaultColor";
        public string MouseStrip1Mode = "HpTracker";
        public string MouseStrip2Mode = "MpTracker";

        public string PadZone1Mode = "HpTracker";
        public string PadZone2Mode = "TpTracker";
        public string PadZone3Mode = "MpTracker";

        public string CLZone1Mode = "DefaultColor";
        public string CLZone2Mode = "DefaultColor";
        public string CLZone3Mode = "DefaultColor";
        public string CLZone4Mode = "DefaultColor";
        public string CLZone5Mode = "DefaultColor";

        public string HeadsetZone1Mode = "DefaultColor";
        public string KeypadZone1Mode = "DefaultColor";

        public string LightbarMode = "TargetHp";
    }

    //Settings
    public class ChromaticsSettings
    {
        public string ChromaticsSettingsArxactip = "http://192.168.0.1:8085";
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
        public bool ChromaticsSettingsShowStats = true;
        public bool ChromaticsSettingsReactiveWeather = false;
        public int ChromaticsSettingsLanguage = 0;
        public int ChromaticsSettingsPreviousLanguage = 0;
        public KeyRegion ChromaticsSettingsQwertyMode = 0;
        public bool ChromaticsSettingsDebugOpt = true;

        public bool ChromaticsSettingsKeyHighlights = true;
        public bool ChromaticsSettingsLccAuto = false;
        public bool ChromaticsSettingsMemoryCache = false;
        public bool ChromaticsSettingsDesktopNotifications = true;
        public bool ChromaticsSettingsLcdEnabled = true;
        public string FinalFantasyXivVersion = "4.1.5";
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
        public string ColorMappingTargetHpClaimed = ColorTranslator.ToHtml(Color.Red);
        public string ColorMappingTargetHpEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingTargetHpIdle = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMappingTpEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMappingTpFull = ColorTranslator.ToHtml(Color.Yellow);
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
    }

    public static class DataStoreFunctions
    {
        public static string GetName<T>(T item) where T : class
        {
            return typeof(T).GetProperties()[0].Name;
        }
    }
}