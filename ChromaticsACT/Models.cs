using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaticsACT
{
    class Models
    {
        
    }

    public class ACTDataTemplate
    {
        public bool IsConnected { get; set; }
        public int Version { get; set; }

        public double PlayerCurrentDPS { get; set; }
        public double PlayerCurrentHPS { get; set; }
        public double PlayerCurrentGroupDPS { get; set; }
        public double PlayerCurrentCrit { get; set; }
        public double PlayerCurrentDH { get; set; }
        public double PlayerCurrentCritDH { get; set; }
        public double PlayerCurrentOverheal { get; set; }
        public int PlayerCurrentDamage { get; set; }
        public double CurrentEncounterTime { get; set; }
        public string CurrentEncounterName { get; set; }
        public bool CustomTriggerActive { get; set; }
        public bool TimerActive { get; set; }

        public ACTDataTemplate()
        {
            IsConnected = false;
            Version = 1;
            PlayerCurrentDPS = 0;
            PlayerCurrentHPS = 0;
            PlayerCurrentGroupDPS = 0;
            PlayerCurrentCrit = 0;
            PlayerCurrentDH = 0;
            PlayerCurrentCritDH = 0;
            PlayerCurrentOverheal = 0;
            PlayerCurrentDamage = 0;
            CurrentEncounterTime = 0;
            CurrentEncounterName = "";
            CustomTriggerActive = false;
            TimerActive = false;
        }
    }
}
