using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Localization;
using System.Collections.ObjectModel;

namespace Chromatics.ViewModels
{
    public sealed class EffectsViewModel : ViewModelBase
    {
        public ObservableCollection<EffectToggleItem> Toggles { get; } = new();

        public EffectsViewModel()
        {
            if (!RGBController.LoadEffectsSettings())
            {
                Logger.WriteConsole(LoggerTypes.System, @"No effects file found. Creating default effects..");
                RGBController.SaveEffectsSettings();
            }
            else
            {
                Logger.WriteConsole(LoggerTypes.System, "Loaded effects from effects.chromatics4");
            }

            var e = RGBController.GetEffectsSettings();

            var loc = LocalizationService.Instance;

            Toggles.Add(new EffectToggleItem(
                loc["Duty Finder Bell"],
                loc["Flash devices when Duty Finder pops."],
                "avares://Chromatics/Resources/notification.png",
                e.effect_dfbell,
                v => { e.effect_dfbell = v; RGBController.SaveEffectsSettings(); }));

            Toggles.Add(new EffectToggleItem(
                loc["Damage Flash"],
                loc["Flash devices when damage is taken."],
                "avares://Chromatics/Resources/sword.png",
                e.effect_damageflash,
                v => { e.effect_damageflash = v; RGBController.SaveEffectsSettings(); }));

            Toggles.Add(new EffectToggleItem(
                loc["Startup Animation"],
                loc["Displays rainbow animation on all devices when Chromatics starts but not connected to FFXIV."],
                "avares://Chromatics/Resources/keyboard.png",
                e.effect_startupanimation,
                v => { e.effect_startupanimation = v; RGBController.SaveEffectsSettings(); }));

            Toggles.Add(new EffectToggleItem(
                loc["Reactive Weather"],
                loc["Animated base layers for Reactive Weather."],
                "avares://Chromatics/Resources/cloudy.png",
                e.effect_reactiveweather,
                v => { e.effect_reactiveweather = v; RGBController.SaveEffectsSettings(); }));

            Toggles.Add(new EffectToggleItem(
                loc["Title Screen"],
                loc["Animation on devices when on title and character selection screen."],
                "avares://Chromatics/Resources/crystal.png",
                e.effect_titlescreen,
                v => { e.effect_titlescreen = v; RGBController.SaveEffectsSettings(); }));

            Toggles.Add(new EffectToggleItem(
                loc["Cutscenes"],
                loc["Animation on devices when cutscenes play."],
                "avares://Chromatics/Resources/video-clip.png",
                e.effect_cutscenes,
                v => { e.effect_cutscenes = v; RGBController.SaveEffectsSettings(); }));

            Toggles.Add(new EffectToggleItem(
                loc["Vegas Mode"],
                loc["Color cycle devices when in the Gold Saucer."],
                "avares://Chromatics/Resources/coin.png",
                e.effect_vegasmode,
                v => { e.effect_vegasmode = v; RGBController.SaveEffectsSettings(); }));

            Toggles.Add(new EffectToggleItem(
                loc["Raid Effects"],
                loc["Animated raid zone effects instead of reactive weather. Uses Reactive Weather layers."],
                "avares://Chromatics/Resources/raid.png",
                e.effect_raideffects,
                v => { e.effect_raideffects = v; RGBController.SaveEffectsSettings(); }));

        }
    }
}
