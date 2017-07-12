using System.Drawing;

namespace Chromatics.Datastore
{
    //Device Managment
    public class DeviceDataStore
    {
        public bool DeviceOperation_CorsairHeadset = true;
        public bool DeviceOperation_CorsairKeyboard = true;
        public bool DeviceOperation_CorsairKeypad = false; //Not Implemented
        public bool DeviceOperation_CorsairMouse = true;
        public bool DeviceOperation_CorsairMousepad = true;
        public string DeviceOperation_HueDevices = "";
        public string DeviceOperation_LifxDevices = "";
        public bool DeviceOperation_LogitechHeadset = true;
        public bool DeviceOperation_LogitechKeyboard = true;
        public bool DeviceOperation_LogitechKeypad = true;
        public bool DeviceOperation_LogitechMouse = true;
        public bool DeviceOperation_LogitechMousepad = true;
        public int DeviceOperation_MouseToggle = 0;
        public bool DeviceOperation_RazerHeadset = true;
        public bool DeviceOperation_RazerKeyboard = true;
        public bool DeviceOperation_RazerKeypad = true;
        public bool DeviceOperation_RazerMouse = true;
        public bool DeviceOperation_RazerMousepad = true;
        public string DeviceOperation_HUEDefault = "";
    }

    //Settings
    public class ChromaticsSettings
    {
        public bool ChromaticsSettings_ARXToggle = true;
        public int ChromaticsSettings_ARXTheme = 0;
        public string ChromaticsSettings_ARXMode = "Player HUD";
        public string ChromaticsSettings_ARXACTIP = "http://192.168.0.1:8085";
        public string FinalFantasyXIVVersion = "4.0.0";
        public bool ChromaticsSettings_LCCAuto = false;
        public bool ChromaticsSettings_MemoryCache = false;
        public bool ChromaticsSettings_AZERTYMode = false;
        public bool ChromaticsSettings_CastToggle = true;
        public bool ChromaticsSettings_CastAnimate = true;
        public bool ChromaticsSettings_GCDCountdown = true;

        public bool ChromaticsSettings_KeyHighlights = true;
        public bool ChromaticsSettings_JobGaugeToggle = true;
        public bool ChromaticsSettings_KeybindToggle = true;
    }

    //Color Mapping
    public class FFXIVColorMappings
    {
        public string ColorMapping_Amnesia = ColorTranslator.ToHtml(Color.Snow);
        public string ColorMapping_BaseColor = ColorTranslator.ToHtml(Color.DodgerBlue);

        public string ColorMapping_Bind = ColorTranslator.ToHtml(Color.BlueViolet);
        public string ColorMapping_Bleed = ColorTranslator.ToHtml(Color.IndianRed);
        public string ColorMapping_Burns = ColorTranslator.ToHtml(Color.OrangeRed);
        public string ColorMapping_CastChargeEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_CastChargeFull = ColorTranslator.ToHtml(Color.White);
        public string ColorMapping_CPEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_CPFull = ColorTranslator.ToHtml(Color.Purple);
        public string ColorMapping_DamageDown = ColorTranslator.ToHtml(Color.PaleVioletRed);
        public string ColorMapping_Daze = ColorTranslator.ToHtml(Color.PaleVioletRed);
        public string ColorMapping_DeepFreeze = ColorTranslator.ToHtml(Color.RoyalBlue);
        public string ColorMapping_Dropsy = ColorTranslator.ToHtml(Color.DeepSkyBlue);
        public string ColorMapping_Emnity0 = ColorTranslator.ToHtml(Color.Green);
        public string ColorMapping_Emnity1 = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMapping_Emnity2 = ColorTranslator.ToHtml(Color.Gold);
        public string ColorMapping_Emnity3 = ColorTranslator.ToHtml(Color.Orange);
        public string ColorMapping_Emnity4 = ColorTranslator.ToHtml(Color.Red);
        public string ColorMapping_GPEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_GPFull = ColorTranslator.ToHtml(Color.SkyBlue);
        public string ColorMapping_Heavy = ColorTranslator.ToHtml(Color.DarkCyan);
        public string ColorMapping_HighlightColor = ColorTranslator.ToHtml(Color.Magenta);
        public string ColorMapping_HPCritical = ColorTranslator.ToHtml(Color.Red);
        public string ColorMapping_HPEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_HPFull = ColorTranslator.ToHtml(Color.Lime);
        public string ColorMapping_HPLoss = ColorTranslator.ToHtml(Color.Red);
        public string ColorMapping_Incapacitation = ColorTranslator.ToHtml(Color.DarkRed);
        public string ColorMapping_Infirmary = ColorTranslator.ToHtml(Color.PaleVioletRed);
        public string ColorMapping_Leaden = ColorTranslator.ToHtml(Color.DarkGray);
        public string ColorMapping_Misery = ColorTranslator.ToHtml(Color.MidnightBlue);
        public string ColorMapping_MPEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_MPFull = ColorTranslator.ToHtml(Color.Magenta);
        public string ColorMapping_NoEmnity = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_Paralysis = ColorTranslator.ToHtml(Color.PeachPuff);
        public string ColorMapping_Petrification = ColorTranslator.ToHtml(Color.SlateGray);
        public string ColorMapping_Poison = ColorTranslator.ToHtml(Color.DarkGreen);
        public string ColorMapping_Pollen = ColorTranslator.ToHtml(Color.Goldenrod);
        public string ColorMapping_Pox = ColorTranslator.ToHtml(Color.PaleVioletRed);
        public string ColorMapping_Silence = ColorTranslator.ToHtml(Color.DarkBlue);
        public string ColorMapping_Sleep = ColorTranslator.ToHtml(Color.GhostWhite);
        public string ColorMapping_Slow = ColorTranslator.ToHtml(Color.YellowGreen);
        public string ColorMapping_Stun = ColorTranslator.ToHtml(Color.Snow);
        public string ColorMapping_TargetCasting = ColorTranslator.ToHtml(Color.White);
        public string ColorMapping_TargetHPClaimed = ColorTranslator.ToHtml(Color.Red);
        public string ColorMapping_TargetHPEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_TargetHPIdle = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMapping_TPEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_TPFull = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMapping_GCDReady = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMapping_GCDHot = ColorTranslator.ToHtml(Color.Red);
        public string ColorMapping_GCDEmpty = ColorTranslator.ToHtml(Color.Black);
        public string ColorMapping_HotbarProc = ColorTranslator.ToHtml(Color.Yellow);
        public string ColorMapping_HotbarCD = ColorTranslator.ToHtml(Color.Red);
        public string ColorMapping_HotbarReady = ColorTranslator.ToHtml(Color.DodgerBlue);
        public string ColorMapping_HotbarOutRange = ColorTranslator.ToHtml(Color.Red);
        public string ColorMapping_HotbarNotAvailable = ColorTranslator.ToHtml(Color.Red);
    }

    public static class DataStoreFunctions
    {
        public static string GetName<T>(T item) where T : class
        {
            return typeof(T).GetProperties()[0].Name;
        }
    }
}