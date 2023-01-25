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



    }
}
