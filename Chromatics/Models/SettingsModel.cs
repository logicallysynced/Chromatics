using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.Alienware;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Extensions.RGB.NET.Devices.LIFX;
using Chromatics.Extensions.RGB.NET.Devices.QmkRawHid;
using Chromatics.Extensions.RGB.NET.Devices.Yeelight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Chromatics.Models
{
    public class SettingsModel
    {
        public string version { get; set; } = "2";
        public double? ffxivExpansion { get; set; } = 6.0;
        public bool firstrun { get; set; } = true;
        public bool winstart { get; set; } = false;
        public bool minimizetray { get; set; } = false;
        public bool trayonstartup { get; set; } = false;
        public bool checkupdates { get; set; } = true;
        public bool betaChannel { get; set; } = false;
        public bool alwaysRunAsAdmin { get; set; } = false;
        public bool closeWithGame { get; set; } = false;
        public bool showDeviceErrors { get; set; } = true;
        public bool showEmulatorDevices { get; set; } = false;
        public KeyboardLocalization keyboardLayout { get; set; } = KeyboardLocalization.qwerty;
        public double rgbRefreshRate { get; set; } = 0.05;
        public int globalbrightness { get; set; } = 100;
        public double criticalHpPercentage { get; set; } = 20.0;
        public int screenCaptureTopLeftOffsetX { get; set; } = 0;
        public int screenCaptureTopLeftOffsetY { get; set; } = 0;
        public int screenCaptureBottomLeftOffsetX { get; set; } = 0;
        public int screenCaptureBottomLeftOffsetY { get; set; } = 0;
        public int screenCaptureTopRightOffsetX { get; set; } = 0;
        public int screenCaptureTopRightOffsetY { get; set; } = 0;
        public int screenCaptureBottomRightOffsetX { get; set; } = 0;
        public int screenCaptureBottomRightOffsetY { get; set; } = 0;
        public bool deviceLogitechEnabled { get; set; } = true;
        public bool deviceCorsairEnabled { get; set; } = true;
        public bool deviceCoolermasterEnabled { get; set; } = true;
        public bool deviceRazerEnabled { get; set; } = true;
        public bool deviceAsusEnabled { get; set; } = true;
        public bool deviceMsiEnabled { get; set; } = true;
        public bool deviceSteelseriesEnabled { get; set; } = true;
        public bool deviceWootingEnabled { get; set; } = true;
        public bool deviceNovationEnabled { get; set; } = true;
        public bool deviceOpenRGBEnabled { get; set; } = false;
        // Hidden setting — not exposed in the Settings UI. Edit
        // settings.chromatics4 manually to point Chromatics at a remote
        // OpenRGB SDK server (e.g. an OpenRGB instance running on another
        // machine on the LAN). Default 127.0.0.1 covers the common case
        // of the SDK server running locally. Port stays hardcoded at 6742.
        public string openRgbServerIp { get; set; } = "127.0.0.1";
        public bool deviceHueEnabled { get; set; } = false;
        public bool devicePlayStationEnabled { get; set; } = false;
        public bool deviceLifxEnabled { get; set; } = false;
        public List<LifxAdoptedDevice> deviceLifxAdoptedDevices { get; set; } = new();
        public List<HueAdoptedDevice> deviceHueAdoptedDevices { get; set; } = new();
        public bool deviceQmkRawHidEnabled { get; set; } = false;
        public List<QmkRawHidAdoptedDevice> deviceQmkRawHidAdoptedDevices { get; set; } = new();
        // True once we've shown the QmkOpenRgbHintDialog on the user's
        // first QMK enable. The dialog explains that stock QMK firmware
        // limits us to whole-board solid colour via VIA; per-key control
        // needs a custom QMK build with the OpenRGB-QMK plugin compiled
        // in. One-shot to avoid nagging on every Settings toggle.
        public bool qmkOpenRgbHintShown { get; set; } = false;
        public bool deviceYeelightEnabled { get; set; } = false;
        public List<YeelightAdoptedDevice> deviceYeelightAdoptedDevices { get; set; } = new();
        public bool deviceAlienwareEnabled { get; set; } = false;
        public List<AlienwareAdoptedDevice> deviceAlienwareAdoptedDevices { get; set; } = new();
        public bool deviceDynamicLightingEnabled { get; set; } = false;
        public bool dynamicLightingHintShown { get; set; } = false;
        // Default: false (bypass off, conservative dedup is on).
        // Dynamic Lighting silently skips devices that a Chromatics
        // vendor provider could also drive — the vendor SDK retains
        // exclusive control of overlapping hardware. Users who want
        // Dynamic Lighting to claim every device Windows enumerates
        // (and are willing to accept the risk of two providers writing
        // to the same device simultaneously) can tick this in
        // Settings -> Advanced to opt out of the dedup behaviour.
        public bool dynamicLightingBypassConflictCheck { get; set; } = false;
        public string deviceHueBridgeIP { get; set; } = "127.0.0.1";
        public string deviceHueBridgeClientKey { get; set; } = "";
        public double deviceHueBridgeBrightness { get; set; } = -1;
        public bool deviceRazerCheckSDKOverride { get; set; } = false;
        public bool enableCrashReports { get; set; } = true;
        public Theme systemTheme { get; set; } = Theme.System;
        public Language systemLanguage { get; set; } = Language.English;

    }
}
