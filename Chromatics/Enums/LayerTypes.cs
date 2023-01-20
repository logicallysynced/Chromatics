using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Enums
{
    public enum LayerType
    {
        [Display(Name="Base Layer"), DefaultValue(typeof(Color), "DodgerBlue")]
        BaseLayer = 0,
        [Display(Name="Dynamic Layer"), DefaultValue(typeof(Color), "BlueViolet")]
        DynamicLayer = 1,
        [Display(Name="Effect Layer"), DefaultValue(typeof(Color), "Crimson")]
        EffectLayer = 2
    }
    public enum BaseLayerType
    {
        [Display(Name="Static")]
        Static = 0,
        [Display(Name="Reactive Weather")]
        ReactiveWeather = 1
    };
    public enum DynamicLayerType
    {
        [Display(Name="None")]
        None = 0,
        [Display(Name="Highlight")]
        Highlight = 1,
        [Display(Name="Keybinds")]
        Keybinds = 2
    };
    public enum EffectLayerType
    {
        [Display(Name="None")]
        None = 0,
        [Display(Name="Duty Finder Bell")]
        DutyFinderBell = 1,
        [Display(Name="Damage Flash")]
        DamageFlash = 2
    };
}
