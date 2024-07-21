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
        public static readonly string currentMappingLayerVersion = "1";

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
}
