using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Models
{
    public class SettingsModel
    {
        public bool localcache { get; set; } = false;
        public bool winstart { get; set; } = false;
        public bool minimizetray { get; set; } = true;
        public bool trayonstartup { get; set; } = false;
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


    }
}
