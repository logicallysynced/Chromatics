using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Core
{
    public static class AppSettings
    {
        private static SettingsModel _settings = new SettingsModel();

        public static readonly string currentSettingsVersion = "2";
        public static readonly string currentEffectsVersion = "2";
        public static readonly string currentPalettesVersion = "1";
        public static readonly string currentMappingLayerVersion = "2";

        // Fires after the keyboard layout setting changes so visuals (virtual
        // keyboard in Mappings, effect grids) can rebuild on the UI thread.
        // The event args carry the transition so subscribers can remap stored
        // per-LedId state (e.g. layer key assignments) between layouts.
        public static event EventHandler<KeyboardLayoutChangedEventArgs> KeyboardLayoutChanged;

        public static void RaiseKeyboardLayoutChanged(KeyboardLocalization oldLayout, KeyboardLocalization newLayout)
        {
            KeyboardLayoutChanged?.Invoke(null, new KeyboardLayoutChangedEventArgs(oldLayout, newLayout));
        }

        public static void Startup()
        {
            //Load Settings
            if (LoadSettings())
            {
                Logger.WriteConsole(LoggerTypes.System, $"Loaded settings from settings.chromatics3");

            }
            else
            {
                Logger.WriteConsole(LoggerTypes.System, @"No settings file found. Creating default settings..");
                SaveSettings(_settings);
            }
        }

        public static SettingsModel GetSettings()
        {
            return _settings;
        }

        public static bool LoadSettings()
        {
            if (FileOperationsHelper.CheckSettingsExist())
            {
                _settings = FileOperationsHelper.LoadSettings();

                return true;
            }

            return false;
        }

        public static bool SaveSettings(SettingsModel settings)
        {
            _settings = settings;
            FileOperationsHelper.SaveSettings(_settings);
            return true;
        }
    }

    public sealed class KeyboardLayoutChangedEventArgs : EventArgs
    {
        public KeyboardLocalization OldLayout { get; }
        public KeyboardLocalization NewLayout { get; }

        public KeyboardLayoutChangedEventArgs(KeyboardLocalization oldLayout, KeyboardLocalization newLayout)
        {
            OldLayout = oldLayout;
            NewLayout = newLayout;
        }
    }
}
