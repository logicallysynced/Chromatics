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
        public bool effect_reactiveweather { get; set; } = true;
        public bool effect_cutscenes { get; set; } = true;
        public bool effect_vegasmode { get; set; } = false;
        public bool effect_titlescreen { get; set; } = true;
        public bool effect_startupanimation { get; set; } = true;

        //Specific Effect Settings
        public bool effect_damageflash_scaledamage { get; set; } = true;
        public double effect_damageflash_min_flash { get; set; } = 0.1;


        //Reactive Weather Effects
        public bool weather_marelametorum_animation { get; set; } = true;
        public bool weather_marelametorum_umbralwind_animation { get; set; } = true;
        public bool weather_ultimathule_animation { get; set; } = true;
        public bool weather_ultimathule_umbralwind_animation { get; set; } = true;
        public bool weather_rain_animation { get; set; } = true;
        public bool weather_showers_animation { get; set; } = true;
        public bool weather_wind_animation { get; set; } = true;
        public bool weather_gales_animation { get; set; } = true;
        public bool weather_thunder_animation { get; set; } = true;
        public bool weather_astromagneticstorm_animation { get; set; } = true;
        public bool weather_umbralwind_animation { get; set; } = true;
        public bool weather_umbralstatic_animation { get; set; } = true;
        public bool weather_snow_animation { get; set; } = true;
        public bool weather_blizzard_animation { get; set; } = true;
        public bool weather_sandstorms_animation { get; set; } = true;
        public bool weather_everlastinglight_animation { get; set; } = true;


    }
}
