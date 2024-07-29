using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Enums
{
    public enum Theme
    {
       System = 0,
       Light = 1,
       Dark = 2,
    }

    public enum Language
    {
        [Display(Name = "English")]
        English = 0,

        [Display(Name = "Japanese (日本語)")]
        Japanese = 1,

        [Display(Name = "French (Français)")]
        French = 2,

        [Display(Name = "German (Deutsch)")]
        German = 3,

        [Display(Name = "Korean (한국어)")]
        Korean = 4,

        [Display(Name = "Spanish (Español)")]
        Spanish = 5,

        [Display(Name = "Chinese (中文)")]
        Chinese = 6
    }
}
