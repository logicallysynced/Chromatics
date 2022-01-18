using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Models
{
    public class EffectTypesModel
    {
        public bool effect_dfbell { get; set; } = true;
        public bool effect_damageflash { get; set; } = false;
        public bool effect_castcomplete { get; set; } = true;
        public bool effect_reactiveweather { get; set; } = true;
        public bool effect_statuseffects { get; set; } = true;
        public bool effect_cutscenes { get; set; } = true;
        public bool effect_vegasmode { get; set; } = false;


    }
}
