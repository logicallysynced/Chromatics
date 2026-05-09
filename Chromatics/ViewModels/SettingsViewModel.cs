using Avalonia.Controls.ApplicationLifetimes;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Localization;
using Chromatics.Extensions.RGB.NET.Devices;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Extensions.RGB.NET.Devices.LIFX;
using Chromatics.Models;
using Chromatics.Helpers;
using Chromatics.Views;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.CoolerMaster;
using RGB.NET.Devices.Corsair;
using RGB.NET.Devices.Logitech;
using RGB.NET.Devices.Msi;
using RGB.NET.Devices.Novation;
using RGB.NET.Devices.OpenRGB;
using RGB.NET.Devices.Razer;
using RGB.NET.Devices.SteelSeries;
using RGB.NET.Devices.Wooting;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Chromatics.ViewModels
{
    public sealed class SettingsViewModel : ViewModelBase
    {
        private readonly RegistryKey _runKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        public ObservableCollection<DeviceToggleItem> DeviceToggles { get; } = new();
        public ObservableCollection<ThemeOption> ThemeOptions { get; } = new();
        public ObservableCollection<LanguageOption> LanguageOptions { get; } = new();
        public ObservableCollection<KeyboardLayoutOption> KeyboardLayoutOptions { get; } = new();

        public SettingsViewModel()
        {
            var s = AppSettings.GetSettings();

            _winStart = s.winstart;
            _minimizeTray = s.minimizetray;
            _trayOnStartup = s.trayonstartup;
            _checkUpdates = s.checkupdates;
            _betaChannel = s.betaChannel;
            _alwaysRunAsAdmin = s.alwaysRunAsAdmin;
            _enableCrashReports = s.enableCrashReports;
            _closeWithGame = s.closeWithGame;
            _globalBrightness = s.globalbrightness;

            foreach (var t in Enum.GetValues(typeof(Theme)).Cast<Theme>())
                ThemeOptions.Add(new ThemeOption(t, t.ToString()));
            _selectedTheme = ThemeOptions.First(o => o.Value == s.systemTheme);

            foreach (var l in Enum.GetValues(typeof(Language)).Cast<Language>())
                LanguageOptions.Add(new LanguageOption(l, l.GetDisplayName()));
            _selectedLanguage = LanguageOptions.First(o => o.Value == s.systemLanguage);

            foreach (var k in Enum.GetValues(typeof(KeyboardLocalization)).Cast<KeyboardLocalization>())
                KeyboardLayoutOptions.Add(new KeyboardLayoutOption(k, k.ToString().ToUpper()));
            _selectedKeyboardLayout = KeyboardLayoutOptions.First(o => o.Value == s.keyboardLayout);

            BuildDeviceToggles(s);
        }

        private void BuildDeviceToggles(Models.SettingsModel s)
        {
            DeviceToggles.Add(MakeDeviceToggle("Razer", "Enable/disable Razer device library. Default: Enabled",
                s.deviceRazerEnabled,
                () => RGBController.LoadDeviceProvider(RazerDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(RazerDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceRazerEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("Logitech", "Enable/disable Logitech device library. Default: Enabled",
                s.deviceLogitechEnabled,
                () => RGBController.LoadDeviceProvider(LogitechDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(LogitechDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceLogitechEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("Corsair", "Enable/disable Corsair device library. Default: Enabled",
                s.deviceCorsairEnabled,
                () => RGBController.LoadDeviceProvider(CorsairDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(CorsairDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceCorsairEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("CoolerMaster", "Enable/disable Coolermaster device library. Default: Enabled",
                s.deviceCoolermasterEnabled,
                () => RGBController.LoadDeviceProvider(CoolerMasterDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(CoolerMasterDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceCoolermasterEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("SteelSeries", "Enable/disable SteelSeries device library. Default: Enabled",
                s.deviceSteelseriesEnabled,
                () => RGBController.LoadDeviceProvider(SteelSeriesDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(SteelSeriesDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceSteelseriesEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("ASUS", "Enable/disable ASUS device library. Default: Enabled",
                s.deviceAsusEnabled,
                () => RGBController.LoadDeviceProvider(AsusDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(AsusDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceAsusEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("MSI", "Enable/disable MSI device library. Default: Enabled",
                s.deviceMsiEnabled,
                () => RGBController.LoadDeviceProvider(MsiDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(MsiDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceMsiEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("Wooting", "Enable/disable Wooting device library. Default: Enabled",
                s.deviceWootingEnabled,
                () => RGBController.LoadDeviceProvider(WootingDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(WootingDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceWootingEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("Novation", "Enable/disable Novation device library. Default: Enabled",
                s.deviceNovationEnabled,
                () => RGBController.LoadDeviceProvider(NovationDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(NovationDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceNovationEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("OpenRGB", "Enable/disable OpenRGB device library. Default: Disabled",
                s.deviceOpenRGBEnabled,
                () => RGBController.LoadDeviceProvider(OpenRGBDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(OpenRGBDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.deviceOpenRGBEnabled = v; AppSettings.SaveSettings(cur); }));

            DeviceToggles.Add(MakeDeviceToggle("PlayStation (Beta)", "[BETA] Enable/disable PlayStation controller lighting (DualShock 4 / DualSense over USB or Bluetooth). Default: Disabled",
                s.devicePlayStationEnabled,
                () => RGBController.LoadDeviceProvider(PlayStationControllerRGBDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(PlayStationControllerRGBDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.devicePlayStationEnabled = v; AppSettings.SaveSettings(cur); }));

            // Hue is special — enabling opens the bridge-pairing dialog first.
            DeviceToggles.Add(new DeviceToggleItem(
                "Hue (Beta)",
                "[BETA] Enable/disable Philips HUE device library. Default: Disabled",
                s.deviceHueEnabled,
                async () =>
                {
                    var cur = AppSettings.GetSettings();
                    var owner = GetMainWindow();
                    var dlg = new HueBridgeDialog(cur.deviceHueBridgeIP);
                    if (owner != null)
                    {
                        await dlg.ShowDialog(owner);
                    }
                    else
                    {
                        dlg.Show();
                    }

                    if (!dlg.BridgeConfigured) return false;

                    cur.deviceHueBridgeIP = dlg.BridgeIp;
                    cur.deviceHueEnabled = true;
                    AppSettings.SaveSettings(cur);
                    return true;
                },
                () =>
                {
                    if (HueRGBDeviceProvider.Instance != null)
                    {
                        HueRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                        RGBController.UnloadDeviceProvider(HueRGBDeviceProvider.Instance);
                        HueRGBDeviceProvider.Instance.Dispose();
                    }
                    var cur = AppSettings.GetSettings();
                    cur.deviceHueEnabled = false;
                    AppSettings.SaveSettings(cur);
                }));

            // LIFX is special — enabling runs network discovery + opens an
            // adoption dialog so the user picks which bulbs Chromatics drives.
            // Re-enabling re-prompts with existing adoptions pre-checked
            // (matches the user spec: see SettingsModel.deviceLifxAdoptedDevices).
            DeviceToggles.Add(new DeviceToggleItem(
                "LIFX (Beta)",
                "[BETA] Enable/disable LIFX device library (LAN protocol). Default: Disabled",
                s.deviceLifxEnabled,
                async () =>
                {
                    var cur = AppSettings.GetSettings();
                    var owner = GetMainWindow();

                    var alreadyAdopted = (cur.deviceLifxAdoptedDevices ?? new System.Collections.Generic.List<LifxAdoptedDevice>())
                        .ToDictionary(d => d.Mac, d => d, StringComparer.OrdinalIgnoreCase);

                    var dlg = new LifxAdoptionDialog(alreadyAdopted);
                    if (owner != null)
                        await dlg.ShowDialog(owner);
                    else
                        dlg.Show();

                    if (!dlg.Saved) return false;

                    cur.deviceLifxAdoptedDevices = dlg.SelectedDevices;
                    cur.deviceLifxEnabled = true;
                    AppSettings.SaveSettings(cur);

                    LifxRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                    foreach (var d in cur.deviceLifxAdoptedDevices)
                    {
                        System.Net.IPEndPoint ep = null;
                        if (!string.IsNullOrEmpty(d.LastIp) && System.Net.IPAddress.TryParse(d.LastIp, out var ip))
                            ep = new System.Net.IPEndPoint(ip, Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol.LifxDiscovery.LifxPort);
                        LifxRGBDeviceProvider.Instance.ClientDefinitions.Add(
                            new LifxClientDefinition(d.Mac, d.Label, ep, d.ProductId, d.ZoneCount));
                    }

                    // LoadDeviceProvider runs LoadDevices synchronously, which
                    // for LIFX includes a 2.5s discovery sweep + per-device
                    // probes (~800ms) and original-state captures (~500ms).
                    // On the UI thread that adds up to multi-second hangs
                    // after the dialog closes. Push the load to a background
                    // thread so the toggle returns immediately and the UI
                    // stays responsive while devices come online.
                    _ = Task.Run(() => RGBController.LoadDeviceProvider(LifxRGBDeviceProvider.Instance));
                    return true;
                },
                () =>
                {
                    if (LifxRGBDeviceProvider.Instance != null)
                    {
                        LifxRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                        RGBController.UnloadDeviceProvider(LifxRGBDeviceProvider.Instance);
                        LifxRGBDeviceProvider.Instance.Dispose();
                    }
                    var cur = AppSettings.GetSettings();
                    cur.deviceLifxEnabled = false;
                    AppSettings.SaveSettings(cur);
                }));
        }

        private static Avalonia.Controls.Window GetMainWindow()
        {
            return Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        }

        private static DeviceToggleItem MakeDeviceToggle(
            string label,
            string tooltip,
            bool initial,
            Action load,
            Action unload,
            Action<bool> saveFlag)
        {
            return new DeviceToggleItem(label, tooltip, initial,
                () =>
                {
                    load();
                    saveFlag(true);
                    return Task.FromResult(true);
                },
                () =>
                {
                    unload();
                    saveFlag(false);
                });
        }

        private bool _winStart;
        public bool WinStart
        {
            get => _winStart;
            set
            {
                if (SetProperty(ref _winStart, value))
                {
                    var s = AppSettings.GetSettings();
                    s.winstart = value;
                    AppSettings.SaveSettings(s);
                }
            }
        }

        private bool _minimizeTray;
        public bool MinimizeTray
        {
            get => _minimizeTray;
            set
            {
                if (SetProperty(ref _minimizeTray, value))
                {
                    var s = AppSettings.GetSettings();
                    s.minimizetray = value;
                    AppSettings.SaveSettings(s);
                }
            }
        }

        private bool _trayOnStartup;
        public bool TrayOnStartup
        {
            get => _trayOnStartup;
            set
            {
                if (SetProperty(ref _trayOnStartup, value))
                {
                    var s = AppSettings.GetSettings();
                    s.trayonstartup = value;

                    try
                    {
                        if (value)
                            _runKey?.SetValue("Chromatics4", Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location);
                        else
                            _runKey?.DeleteValue("Chromatics4", false);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(LoggerTypes.Error, $"Failed to update Windows startup registry: {ex.Message}");
                    }

                    AppSettings.SaveSettings(s);
                }
            }
        }

        private bool _checkUpdates;
        public bool CheckUpdates
        {
            get => _checkUpdates;
            set
            {
                if (SetProperty(ref _checkUpdates, value))
                {
                    var s = AppSettings.GetSettings();
                    s.checkupdates = value;
                    AppSettings.SaveSettings(s);
                }
            }
        }

        private bool _betaChannel;
        public bool BetaChannel
        {
            get => _betaChannel;
            set
            {
                if (SetProperty(ref _betaChannel, value))
                {
                    var s = AppSettings.GetSettings();
                    s.betaChannel = value;
                    AppSettings.SaveSettings(s);
                }
            }
        }

        private bool _alwaysRunAsAdmin;
        public bool AlwaysRunAsAdmin
        {
            get => _alwaysRunAsAdmin;
            set
            {
                if (SetProperty(ref _alwaysRunAsAdmin, value))
                {
                    var s = AppSettings.GetSettings();
                    s.alwaysRunAsAdmin = value;
                    AppSettings.SaveSettings(s);
                }
            }
        }

        private bool _enableCrashReports;
        public bool EnableCrashReports
        {
            get => _enableCrashReports;
            set
            {
                if (SetProperty(ref _enableCrashReports, value))
                {
                    var s = AppSettings.GetSettings();
                    s.enableCrashReports = value;
                    AppSettings.SaveSettings(s);
                    Chromatics.Core.SentryService.ApplyConsent(value);
                }
            }
        }

        private bool _closeWithGame;
        public bool CloseWithGame
        {
            get => _closeWithGame;
            set
            {
                if (SetProperty(ref _closeWithGame, value))
                {
                    var s = AppSettings.GetSettings();
                    s.closeWithGame = value;
                    AppSettings.SaveSettings(s);
                }
            }
        }

        private int _globalBrightness;
        public int GlobalBrightness
        {
            get => _globalBrightness;
            set
            {
                if (SetProperty(ref _globalBrightness, value))
                {
                    OnPropertyChanged(nameof(GlobalBrightnessLabel));
                    var s = AppSettings.GetSettings();
                    s.globalbrightness = value;
                    AppSettings.SaveSettings(s);
                    Chromatics.Extensions.RGB.NET.ColorCorrections.GlobalBrightnessCorrection.Instance.BrightnessPercent = value;
                }
            }
        }

        public string GlobalBrightnessLabel => $"{_globalBrightness}%";

        private ThemeOption _selectedTheme;
        public ThemeOption SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value) && value != null)
                {
                    var s = AppSettings.GetSettings();
                    s.systemTheme = value.Value;
                    AppSettings.SaveSettings(s);

                    if (Avalonia.Application.Current is App app)
                    {
                        app.RefreshTheme();
                    }
                }
            }
        }

        private LanguageOption _selectedLanguage;
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value) && value != null)
                {
                    var s = AppSettings.GetSettings();
                    s.systemLanguage = value.Value;
                    AppSettings.SaveSettings(s);
                    LocalizationService.Instance.SetLanguage(value.Value);
                }
            }
        }

        private KeyboardLayoutOption _selectedKeyboardLayout;
        public KeyboardLayoutOption SelectedKeyboardLayout
        {
            get => _selectedKeyboardLayout;
            set
            {
                if (SetProperty(ref _selectedKeyboardLayout, value) && value != null)
                {
                    var s = AppSettings.GetSettings();
                    var oldLayout = s.keyboardLayout;
                    if (oldLayout == value.Value) return;

                    s.keyboardLayout = value.Value;
                    AppSettings.SaveSettings(s);
                    AppSettings.RaiseKeyboardLayoutChanged(oldLayout, value.Value);
                }
            }
        }

        public void ResetChromatics()
        {
            try
            {
                var env = FileOperationsHelper.GetConfigDirectory();

                // Covers both the Chromatics-4 data files and any leftover
                // .chromatics3 / .chromatics3.migrated files from a prior install
                // so "Reset" truly clears everything.
                var names = new[]
                {
                    "layers.chromatics4", "palette.chromatics4", "effects.chromatics4", "settings.chromatics4",
                    "layers.chromatics3", "palette.chromatics3", "effects.chromatics3", "settings.chromatics3",
                    "layers.chromatics3.migrated", "palette.chromatics3.migrated",
                    "effects.chromatics3.migrated", "settings.chromatics3.migrated",
                };

                foreach (var f in names)
                {
                    var path = Path.Combine(env, f);
                    if (File.Exists(path)) FileSystem.DeleteFile(path);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"Unable to reset Chromatics: {ex.Message}");
            }
        }

        public sealed class ThemeOption
        {
            public Theme Value { get; }
            public string DisplayName { get; }
            public ThemeOption(Theme value, string displayName) { Value = value; DisplayName = displayName; }
            public override string ToString() => DisplayName;
        }

        public sealed class LanguageOption
        {
            public Language Value { get; }
            public string DisplayName { get; }
            public LanguageOption(Language value, string displayName) { Value = value; DisplayName = displayName; }
            public override string ToString() => DisplayName;
        }

        public sealed class KeyboardLayoutOption
        {
            public KeyboardLocalization Value { get; }
            public string DisplayName { get; }
            public KeyboardLayoutOption(KeyboardLocalization value, string displayName) { Value = value; DisplayName = displayName; }
            public override string ToString() => DisplayName;
        }
    }
}
