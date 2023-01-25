using Chromatics.Core;
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
        [Display(Name="Static", Description = "Displays a single colour over the entire base layer.")]
        Static = 0,
        [Display(Name="Reactive Weather", Description = "Base layer changes colour depending on the current zone's weather.")]
        ReactiveWeather = 1,
        [Display(Name = "Battle Stance", Description = "Base layer changes colour depending on whether the character is engaged in battle or not.")]
        BattleStance = 2,
        [Display(Name = "Job Classes", Description = "Base layer changes colour depending on the character's current class.\nUses base layer colours.")]
        JobClasses = 3,
        [Display(Name = "Screen Capture", Description = "Draws a gradient as your base layer by polling the four corners of your game screen.")]
        ScreenCapture = 4,
    };
    public enum DynamicLayerType
    {
        [Display(Name="None", Description = "Disables this layer.")]
        None = 0,
        [Display(Name="Highlight", Description = "Displays the highlight colour over all selected keys.")]
        Highlight = 1,
        [Display(Name="Keybinds", Description = "Displays different colours across selected keys based on keybind status of selected keys.")]
        Keybinds = 2,
        [Display(Name = "Enmity Tracker", Description = "Displays different colours across selected keys depending on enmity status of target.")]
        EnmityTracker = 3,
        [Display(Name = "Target HP", Description = "Shows current target's HP across selected keys.\n[Supported Modes: Interpolate, Fade]")]
        TargetHP = 4,
        [Display(Name = "Target Castbar", Description = "Shows current target's cast progress across selected keys.\n[Supported Modes: Interpolate, Fade]")]
        TargetCastbar = 5,
        [Display(Name = "HP Tracker", Description = "Shows character's HP across selected keys. Will switch to critical colour upon falling below {criticalHpPercentage} HP.\n[Supported Modes: Interpolate, Fade]")]
        HPTracker = 6,
        [Display(Name = "MP Tracker", Description = "Shows character's MP/CP/GP across selected keys.\n[Supported Modes: Interpolate, Fade]")]
        MPTracker = 7,
        [Display(Name = "Job Gauge", Description = "Shows character's Job Gauge across selected keys.\n[Supported Modes: Interpolate, Fade]")]
        JobGauge = 8,
        [Display(Name = "Experience Tracker", Description = "Shows character's EXP level progress across selected keys.\n[Supported Modes: Interpolate, Fade]")]
        ExperienceTracker = 9,
        [Display(Name = "Battle Stance", Description = "Displays different colours across selected kyes depending on whether the character is engaged in battle or not.")]
        BattleStance = 10,
        [Display(Name = "Castbar", Description = "Shows character's cast progress across selected keys.\n[Supported Modes: Interpolate, Fade]")]
        Castbar = 11,
        [Display(Name = "Job Class", Description = "Displays different colours across selected keys depending on the character's current class.\nUses highlight layer colours.")]
        JobClass = 12,
    };
    public enum EffectLayerType
    {
        [Display(Name="None", Description = "")]
        None = 0,
        [Display(Name="Duty Finder Bell", Description = "")]
        DutyFinderBell = 1,
        [Display(Name="Damage Flash", Description = "")]
        DamageFlash = 2
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
