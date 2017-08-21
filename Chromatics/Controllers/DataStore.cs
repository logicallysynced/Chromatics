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
        public bool DeviceOperationRoccatKeyboard = true;
        public bool DeviceOperationRoccatMouse = true;

        public bool DeviceOperationKeyboard = true;
        public bool DeviceOperationMouse = true;
        public bool DeviceOperationMousepad = true;
        public bool DeviceOperationHeadset = true;
        public bool DeviceOperationKeypad = true;

        public bool KeysSingleKeyModeEnabled = false;
        public string KeySingleKeyMode = "Disabled";
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

        public bool ChromaticsSettingsKeyHighlights = true;
        public bool ChromaticsSettingsLccAuto = false;
        public bool ChromaticsSettingsMemoryCache = false;
        public string FinalFantasyXivVersion = "4.0.5";
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
    }

    public static class DataStoreFunctions
    {
        public static string GetName<T>(T item) where T : class
        {
            return typeof(T).GetProperties()[0].Name;
        }
    }
}