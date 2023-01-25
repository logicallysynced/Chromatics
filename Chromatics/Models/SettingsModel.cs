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
        public bool desktopnotify { get; set; } = false;
        public bool minimizetray { get; set; } = true;
        public bool trayonstartup { get; set; } = false;
        public bool releasedevices { get; set; } = false;
        public int globalbrightness { get; set; } = 100;
        public double criticalHpPercentage { get; set; } = 20.0;
        public bool deviceLogitechEnabled { get; set; } = false;
        public bool deviceCorsairEnabled { get; set; } = false;
        public bool deviceCoolermasterEnabled { get; set; } = false;
        public bool deviceRazerEnabled { get; set; } = true;
        public bool deviceAsusEnabled { get; set; } = false;
        public bool deviceMsiEnabled { get; set; } = false;
        public bool deviceSteelseriesEnabled { get; set; } = false;
        public bool deviceWootingEnabled { get; set; } = false;
        public bool deviceNovationEnabled { get; set; } = false;


    }
}
