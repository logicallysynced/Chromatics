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

        // v3: adds deviceDynamicLightingEnabled, dynamicLightingHintShown,
        // and dynamicLightingBypassConflictCheck (v4.2.6+).
        public static readonly string currentSettingsVersion = "3";
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
                Logger.WriteConsole(LoggerTypes.System, $"Loaded settings from settings.chromatics4");

            }
            else
            {
                Logger.WriteConsole(LoggerTypes.System, @"No settings file found. Creating default settings..");
                SaveSettings(_settings);
            }

            // Any user-editable IP-shaped string fields in settings.chromatics4
            // get validated here so a typo in the JSON doesn't blow up later
            // inside a provider's connection logic with a less-helpful error.
            // Validation runs against the in-memory _settings only - we don't
            // overwrite the file, so the user still sees the same warning on
            // every startup until they fix or remove the bad value.
            ValidateIpFields(_settings);

            Chromatics.Extensions.RGB.NET.ColorCorrections.GlobalBrightnessCorrection.Instance.BrightnessPercent = _settings.globalbrightness;
        }

        // settings.chromatics4 fields that hold an IP address. Anything the
        // user might hand-edit goes here; auto-populated nested lists (LIFX
        // adopted devices etc.) are validated by their owning providers when
        // the device gets re-attached, so they aren't in this list.
        private static void ValidateIpFields(SettingsModel settings)
        {
            ValidateIp(nameof(settings.openRgbServerIp), settings.openRgbServerIp, "127.0.0.1",
                v => settings.openRgbServerIp = v);
            ValidateIp(nameof(settings.deviceHueBridgeIP), settings.deviceHueBridgeIP, "127.0.0.1",
                v => settings.deviceHueBridgeIP = v);
        }

        // Logs + falls back when an IP-shaped settings field doesn't parse.
        // Empty / whitespace is treated as "unset" and left alone - that's
        // the legitimate state for fields like deviceHueBridgeIP before the
        // user has paired a bridge. Only a non-empty value that fails
        // IPAddress.TryParse triggers the warning.
        private static void ValidateIp(string fieldName, string current, string defaultValue, Action<string> setter)
        {
            if (string.IsNullOrWhiteSpace(current)) return;
            if (System.Net.IPAddress.TryParse(current.Trim(), out _)) return;
            Logger.WriteConsole(LoggerTypes.Error,
                $"[Settings] Invalid IP address in settings.chromatics4 for {fieldName}: '{current}'. Falling back to default '{defaultValue}' for this session. Edit the file to fix the value.");
            setter(defaultValue);
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
