using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Chromatics.Enums
{
    public class Palette
    {
        public enum PaletteTypes
        {
            [Display(Name = "All")]
            All = 0,
            [Display(Name = "Chromatics")]
            Chromatics = 1,
            [Display(Name = "Player Stats")]
            PlayerStats = 2,
            [Display(Name = "Abilities")]
            Abilities = 3,
            [Display(Name = "Enmity/Aggro")]
            EnmityAggro = 4,
            [Display(Name = "Target/Enemy")]
            TargetEnemy = 5,
            [Display(Name = "Status Effects")]
            StatusEffects = 6,
            [Display(Name = "Cooldowns/Keybinds")]
            CooldownsKeybinds = 7,
            [Display(Name = "Notifications")]
            Notifications = 8,
            [Display(Name = "Job Gauges")]
            JobGauges = 9,
            [Display(Name = "Reactive Weather")]
            ReactiveWeather = 10,
            [Display(Name = "Job Classes")]
            JobClasses = 11,
            [Display(Name = "Raid Zone Effects")]
            RaidEffects = 12
        }

        public static int TypeCount = 12;
    }
}
