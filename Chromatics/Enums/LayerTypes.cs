using Chromatics.Core;
using Chromatics.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        [LayerDisplay(Name="Static", Description = "Displays a single colour over the entire base layer.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        Static = 0,
        [LayerDisplay(Name="Reactive Weather", Description = "Base layer changes colour depending on the current zone's weather.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        ReactiveWeather = 1,
        [LayerDisplay(Name = "Battle Stance", Description = "Base layer changes colour depending on whether the character is engaged in battle or not.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        BattleStance = 2,
        [LayerDisplay(Name = "Job Classes", Description = "Base layer changes colour depending on the character's current class.\nUses base layer colours.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        JobClasses = 3,
        [LayerDisplay(Name = "Screen Capture", Description = "Draws a gradient as your base layer by polling the four corners of your game screen.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        ScreenCapture = 4,
        
    };
    public enum DynamicLayerType
    {
        [LayerDisplay(Name="None", Description = "Disables this layer.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        None = 0,
        [LayerDisplay(Name="Highlight", Description = "Displays the highlight colour over all selected keys.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        Highlight = 1,
        [LayerDisplay(Name="Keybinds", Description = "Displays different colours across selected keys based on keybind status of selected keys.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        Keybinds = 2,
        [LayerDisplay(Name = "Enmity Tracker", Description = "Displays different colours across selected keys depending on enmity status of target.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        EnmityTracker = 3,
        [LayerDisplay(Name = "Target HP", Description = "Shows current target's HP across selected keys.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        TargetHP = 4,
        [LayerDisplay(Name = "Target Castbar", Description = "Shows current target's cast progress across selected keys.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        TargetCastbar = 5,
        [LayerDisplay(Name = "HP Tracker", Description = "Shows character's HP across selected keys. Will switch to critical colour upon falling below {criticalHpPercentage} HP.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        HPTracker = 6,
        [LayerDisplay(Name = "MP Tracker", Description = "Shows character's MP/CP/GP across selected keys.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        MPTracker = 7,
        [LayerDisplay(Name = "Job Gauge A", Description = "Shows character's Job Gauge (A) across selected keys.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        JobGaugeA = 8,
        [LayerDisplay(Name = "Job Gauge B", Description = "Shows character's Job Gauge (B) across selected keys.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        JobGaugeB = 9, 
        [LayerDisplay(Name = "Experience Tracker", Description = "Shows character's EXP level progress across selected keys.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        ExperienceTracker = 10,
        [LayerDisplay(Name = "Battle Stance", Description = "Displays different colours across selected kyes depending on whether the character is engaged in battle or not.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        BattleStance = 11,
        [LayerDisplay(Name = "Castbar", Description = "Shows character's cast progress across selected keys.\n[Supported Modes: Interpolate, Fade]", LayerTypeCompatibility = new LayerModes[]{ LayerModes.Interpolate, LayerModes.Fade })]
        Castbar = 12,
        [LayerDisplay(Name = "Job Classes Highlight", Description = "Displays different colours across selected keys depending on the character's current class.\nUses highlight layer colours.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        JobClassesHighlight = 13,
        [LayerDisplay(Name = "Reactive Weather Highlight", Description = "Displays different colours across selected keys depending on current weather.\nUses highlight layer colours.", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        ReactiveWeatherHighlight = 14
    };
    public enum EffectLayerType
    {
        [LayerDisplay(Name="None", Description = "", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        None = 0,
        [LayerDisplay(Name="Duty Finder Bell", Description = "", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        DutyFinderBell = 1,
        [LayerDisplay(Name="Damage Flash", Description = "", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        DamageFlash = 2,
        [LayerDisplay(Name="Gold Saucer Vegas Mode", Description = "", LayerTypeCompatibility = new LayerModes[]{ LayerModes.None })]
        GoldSaucerVegas = 3
    };

    public enum LayerModes
    {
        [Display(Name="None")]
        None = 0,
        [Display(Name="Interpolate")]
        Interpolate = 1,
        [Display(Name="Fade")]
        Fade = 2
    };
}
