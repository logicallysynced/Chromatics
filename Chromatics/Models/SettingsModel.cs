using Chromatics.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Models
{
    public class SettingsModel
    {
        public string version { get; set; } = "1";
        public bool localcache { get; set; } = false;
        public bool winstart { get; set; } = false;
        public bool minimizetray { get; set; } = false;
        public bool trayonstartup { get; set; } = false;
        public bool checkupdates { get; set; } = true;
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
        public bool deviceHueEnabled { get; set; } = false;
        public string deviceHueBridgeIP { get; set; } = "127.0.0.1";
        public string deviceHueBridgeKey { get; set; } = "";
        public string deviceHueBridgeStreamingKey { get; set; } = "";
        public bool deviceRazerCheckSDKOverride { get; set; } = false;

    }
}
