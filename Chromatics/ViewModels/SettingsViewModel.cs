using Avalonia.Controls.ApplicationLifetimes;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Localization;
using Chromatics.Extensions.RGB.NET.Devices;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Extensions.RGB.NET.Devices.LIFX;
using Chromatics.Extensions.RGB.NET.Devices.PlayStation;
using Chromatics.Extensions.RGB.NET.Devices.Alienware;
using Chromatics.Extensions.RGB.NET.Devices.DynamicLighting;
using Chromatics.Extensions.RGB.NET.Devices.QmkRawHid;
using Chromatics.Extensions.RGB.NET.Devices.Yeelight;
using Chromatics.Models;
using Chromatics.Helpers;
using Chromatics.Views;
using Chromatics.Views.Dialogs;
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
using System.Collections.Generic;
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
            _dynamicLightingBypassConflictCheck = s.dynamicLightingBypassConflictCheck;
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

            // Logitech is special - the LightSync SDK only loads while
            // Logitech G HUB is running, and RGB.NET surfaces that failure
            // with "Failed to initialize Logitech-SDK." Catch that path,
            // flip the toggle back off, and prompt the user to start G HUB.
            // Other Logitech failures (driver missing, permission, etc.)
            // fall through to the generic error log and the toggle stays on
            // so the user can re-toggle once they've fixed the underlying
            // issue.
            DeviceToggles.Add(new DeviceToggleItem(
                "Logitech",
                "Enable/disable Logitech device library. Requires Logitech G HUB to be running. Default: Enabled",
                s.deviceLogitechEnabled,
                async () =>
                {
                    RGBController.LoadDeviceProvider(LogitechDeviceProvider.Instance, out var loadError);
                    if (loadError != null
                        && loadError.Message.Contains("Failed to initialize Logitech-SDK", StringComparison.OrdinalIgnoreCase))
                    {
                        try { RGBController.UnloadDeviceProvider(LogitechDeviceProvider.Instance); } catch { /* best-effort */ }
                        var c = AppSettings.GetSettings();
                        c.deviceLogitechEnabled = false;
                        AppSettings.SaveSettings(c);
                        await DialogService.ShowAsync(
                            LocalizationService.Instance["Logitech G HUB not detected"],
                            LocalizationService.Instance["Chromatics couldn't load the Logitech LightSync SDK. The SDK only loads while Logitech G HUB is running. Open G HUB on this machine, then re-enable the Logitech provider. If G HUB is already open, try restarting it."]);
                        return false;
                    }
                    var cur = AppSettings.GetSettings();
                    cur.deviceLogitechEnabled = true;
                    AppSettings.SaveSettings(cur);
                    return true;
                },
                () =>
                {
                    RGBController.UnloadDeviceProvider(LogitechDeviceProvider.Instance);
                    var cur = AppSettings.GetSettings();
                    cur.deviceLogitechEnabled = false;
                    AppSettings.SaveSettings(cur);
                }));

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

            // OpenRGB is special — it can't enumerate devices until it has
            // connected to an SDK server. If the server isn't running the
            // provider throws on Load; if the server is running but has no
            // devices configured the provider loads cleanly with an empty
            // Devices list. Both modes need user-facing feedback (a generic
            // "toggle silently turned itself off" leaves the user staring
            // at a dead checkbox). Mirrors the LIFX / Yeelight / QMK
            // patterns: empty-result auto-untoggle + dialog, connection-
            // refused catch + dialog.
            DeviceToggles.Add(new DeviceToggleItem(
                "OpenRGB",
                "Enable/disable OpenRGB device library. Requires the OpenRGB SDK server to be running (OpenRGB -> Settings -> SDK Server -> Start at Application Start). Default: Disabled",
                s.deviceOpenRGBEnabled,
                async () =>
                {
                    var cur = AppSettings.GetSettings();
                    var ip = string.IsNullOrWhiteSpace(cur.openRgbServerIp)
                        ? "127.0.0.1"
                        : cur.openRgbServerIp.Trim();

                    try
                    {
                        // The Setup() path in RGBController only adds the
                        // server definition if the toggle was already on at
                        // startup. A first-time user-driven enable needs to
                        // add it here too. AddDeviceDefinition is idempotent
                        // enough for the common case where the user toggles
                        // on/off without changing the IP.
                        OpenRGBDeviceProvider.Instance.AddDeviceDefinition(new OpenRGBServerDefinition
                        {
                            Port = 6742,
                            Ip = ip,
                            ClientName = "Chromatics",
                        });

                        Logger.WriteConsole(LoggerTypes.Devices,
                            $"[OpenRGB] Connecting to SDK server at {ip}:6742...");

                        // Load runs synchronously on the UI thread and the
                        // OpenRGB.NET client probe is sub-second when the
                        // server is local; no Task.Run wrap needed.
                        RGBController.LoadDeviceProvider(OpenRGBDeviceProvider.Instance);

                        int count = OpenRGBDeviceProvider.Instance.Devices.Count();
                        if (count == 0)
                        {
                            // Server reachable but reports zero devices.
                            // Pull the provider back out and flip the toggle
                            // off so the user gets a clear "didn't take" UX
                            // instead of a checked toggle that paints nothing.
                            RGBController.UnloadDeviceProvider(OpenRGBDeviceProvider.Instance);
                            var cc = AppSettings.GetSettings();
                            cc.deviceOpenRGBEnabled = false;
                            AppSettings.SaveSettings(cc);
                            Logger.WriteConsole(LoggerTypes.Devices,
                                $"[OpenRGB] Connected to {ip}:6742 but no devices are configured in OpenRGB. Add devices in OpenRGB before enabling this provider.");
                            await DialogService.ShowAsync(
                                LocalizationService.Instance["No OpenRGB devices found"],
                                LocalizationService.Instance["Chromatics connected to the OpenRGB SDK server but it reports no devices. Open OpenRGB on the host machine, confirm at least one device shows up there, then enable this provider again."]);
                            return false;
                        }

                        Logger.WriteConsole(LoggerTypes.Devices,
                            $"[OpenRGB] Connected to {ip}:6742 - {count} device(s) adopted");
                        var c = AppSettings.GetSettings();
                        c.deviceOpenRGBEnabled = true;
                        AppSettings.SaveSettings(c);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Connection refused / timeout / DNS failure / etc.
                        // Log the full error to console for diagnosis and
                        // show a short user-facing dialog with the prereq.
                        Logger.WriteConsole(LoggerTypes.Error,
                            $"[OpenRGB] Connection to {ip}:6742 failed: {ex.Message}");
                        try { RGBController.UnloadDeviceProvider(OpenRGBDeviceProvider.Instance); } catch { /* best-effort */ }
                        var c = AppSettings.GetSettings();
                        c.deviceOpenRGBEnabled = false;
                        AppSettings.SaveSettings(c);
                        await DialogService.ShowAsync(
                            LocalizationService.Instance["OpenRGB SDK server unreachable"],
                            LocalizationService.Instance["Chromatics couldn't reach the OpenRGB SDK server. Open OpenRGB on the host machine, go to Settings -> SDK Server, and enable 'Start at Application Start'. To target a remote server, set openRgbServerIp in settings.chromatics4 (Chromatics %AppData% folder)."]);
                        return false;
                    }
                },
                () =>
                {
                    RGBController.UnloadDeviceProvider(OpenRGBDeviceProvider.Instance);
                    var cur = AppSettings.GetSettings();
                    cur.deviceOpenRGBEnabled = false;
                    AppSettings.SaveSettings(cur);
                    Logger.WriteConsole(LoggerTypes.Devices, "[OpenRGB] Provider disabled");
                }));

            DeviceToggles.Add(MakeDeviceToggle("PlayStation", "Enable/disable PlayStation controller lighting (DualShock 4 / DualSense over USB or Bluetooth). Default: Disabled",
                s.devicePlayStationEnabled,
                () => RGBController.LoadDeviceProvider(PlayStationControllerRGBDeviceProvider.Instance),
                () => RGBController.UnloadDeviceProvider(PlayStationControllerRGBDeviceProvider.Instance),
                v => { var cur = AppSettings.GetSettings(); cur.devicePlayStationEnabled = v; AppSettings.SaveSettings(cur); }));

            // Hue is special — enabling opens the bridge-pairing dialog (with
            // auto-discovery) and then the bulb-adoption dialog. Both must
            // succeed and result in a non-empty selection for the toggle to
            // stick. If either is cancelled, or the user adopts no bulbs,
            // the toggle reverts to off — same UX as the LIFX flow below.
            DeviceToggles.Add(new DeviceToggleItem(
                "Hue",
                "Enable/disable Philips HUE device library. Default: Disabled",
                s.deviceHueEnabled,
                async () =>
                {
                    var cur = AppSettings.GetSettings();
                    var owner = GetMainWindow();

                    // Step 1: bridge dialog (discovery + pair).
                    var bridgeDlg = new HueBridgeDialog(cur.deviceHueBridgeIP);
                    if (owner != null) await bridgeDlg.ShowDialog(owner);
                    else bridgeDlg.Show();

                    if (!bridgeDlg.BridgeConfigured) return false;

                    cur.deviceHueBridgeIP = bridgeDlg.BridgeIp;
                    if (!string.IsNullOrEmpty(bridgeDlg.BridgeKey))
                        cur.deviceHueBridgeClientKey = bridgeDlg.BridgeKey;

                    // Step 2: adoption dialog (pick which bulbs Chromatics
                    // controls). Pre-checks bulbs the user previously adopted.
                    var alreadyAdopted = (cur.deviceHueAdoptedDevices ?? new System.Collections.Generic.List<HueAdoptedDevice>())
                        .ToDictionary(d => d.LightId, d => d);

                    var adoptDlg = new HueAdoptionDialog(cur.deviceHueBridgeIP, cur.deviceHueBridgeClientKey, alreadyAdopted);
                    if (owner != null) await adoptDlg.ShowDialog(owner);
                    else adoptDlg.Show();

                    if (!adoptDlg.Saved) return false;
                    if (adoptDlg.SelectedDevices == null || adoptDlg.SelectedDevices.Count == 0) return false;

                    cur.deviceHueAdoptedDevices = adoptDlg.SelectedDevices;
                    cur.deviceHueEnabled = true;
                    AppSettings.SaveSettings(cur);

                    // LoadDeviceProvider runs LoadDevices synchronously, which
                    // for Hue includes the bulb fetch + GetService probes +
                    // CaptureOriginalStateAsync per bulb. Push to a background
                    // thread so the toggle returns immediately and the UI
                    // stays responsive while bulbs come online.
                    HueRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                    HueRGBDeviceProvider.Instance.ClientDefinitions.Add(
                        new HueClientDefinition(cur.deviceHueBridgeIP, "chromatics", ""));
                    _ = Task.Run(() => RGBController.LoadDeviceProvider(HueRGBDeviceProvider.Instance));
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
                "LIFX",
                "Enable/disable LIFX device library (LAN protocol). Default: Disabled",
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

                    // If discovery turned up nothing OR the user unchecked
                    // everything before saving, treat the enable as a no-op
                    // and leave the toggle off. Without this the provider
                    // would load with an empty ClientDefinitions list — no
                    // devices appear in the surface but the Settings tab
                    // shows the toggle as "on", which is misleading.
                    if (dlg.SelectedDevices == null || dlg.SelectedDevices.Count == 0)
                        return false;

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

            // QMK Raw HID — auto-adopts every QMK-compatible board on first
            // enable (no picker dialog yet; per-keyboard disable via the
            // Mapping tab covers the "I don't want this one" case for v1
            // Beta). Covers NovelKeys, KBDFans, Drop, GMMK, Glorious and any
            // other custom keyboard running QMK with Raw HID enabled.
            DeviceToggles.Add(new DeviceToggleItem(
                "QMK Keyboards (Beta)",
                "[BETA] Enable/disable QMK Raw HID keyboard support. Auto-adopts any QMK-compatible keyboard with Raw HID enabled (covers NovelKeys, KBDFans, Drop, GMMK, Glorious, and other custom QMK boards). Default: Disabled",
                s.deviceQmkRawHidEnabled,
                async () =>
                {
                    var cur = AppSettings.GetSettings();

                    Logger.WriteConsole(LoggerTypes.Devices,
                        "[QMK] Scanning for QMK-compatible keyboards on the USB bus...");

                    // Discovery + auto-adopt: run on a background thread to
                    // keep the Settings dialog responsive — per-device VIA
                    // handshakes can take 200-500ms each on a sluggish USB
                    // stack, and discovery + handshake of 5+ boards adds up.
                    bool result = await Task.Run(() =>
                    {
                        var discovered = Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol.QmkRawHidDiscovery.Discover();
                        if (discovered.Count == 0)
                        {
                            // No boards responded — leave the toggle off so
                            // the user sees the immediate "didn't take" UX
                            // rather than an empty-but-on provider.
                            return false;
                        }

                        var adopted = new System.Collections.Generic.List<QmkRawHidAdoptedDevice>();
                        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidRGBDeviceProvider.Instance.AdoptedDevices.Clear();
                        foreach (var c in discovered)
                        {
                            string mfg = ""; string prod = "";
                            try { mfg  = c.Hid.GetManufacturer() ?? ""; } catch { }
                            try { prod = c.Hid.GetProductName() ?? ""; } catch { }
                            var key = $"{c.Hid.VendorID:X4}:{c.Hid.ProductID:X4}:{mfg}:{prod}";
                            if (!seen.Add(key)) continue;

                            adopted.Add(new QmkRawHidAdoptedDevice
                            {
                                VendorId = c.Hid.VendorID,
                                ProductId = c.Hid.ProductID,
                                Manufacturer = mfg,
                                Product = prod,
                                LedCount = c.LedCount,
                                Protocol = c.Protocol.ToString(),
                                ViaKeymapKey = string.Empty,
                            });
                            Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidRGBDeviceProvider.Instance.AdoptedDevices.Add(
                                new Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidAdoptedDeviceFilter(
                                    c.Hid.VendorID, c.Hid.ProductID, mfg, prod));
                        }

                        cur.deviceQmkRawHidAdoptedDevices = adopted;
                        cur.deviceQmkRawHidEnabled = true;
                        AppSettings.SaveSettings(cur);

                        RGBController.LoadDeviceProvider(
                            Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidRGBDeviceProvider.Instance);
                        return true;
                    });

                    if (!result)
                    {
                        // The "no boards found" path. Logger.WriteConsole has
                        // already written the detailed enumeration breakdown
                        // (count of HID devices seen, candidates with the
                        // Raw HID interface, open-failures, etc.). Surface a
                        // short user-facing dialog too so it's obvious why
                        // the toggle didn't take — without this the toggle
                        // flashes on then back off with no visible feedback.
                        await DialogService.ShowAsync(
                            LocalizationService.Instance["No QMK Keyboards Found"],
                            LocalizationService.Instance["Chromatics didn't detect any QMK keyboards. Make sure your keyboard is plugged in over USB and that its firmware has Raw HID enabled (the default for any VIA-compatible build). If VIA, Vial, or OpenRGB is running, close it before enabling this provider - they hold the Raw HID interface exclusively. See the console for the full list of detected HID devices."]);
                    }

                    return result;
                },
                () =>
                {
                    var prov = Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidRGBDeviceProvider.Instance;
                    if (prov != null)
                    {
                        prov.AdoptedDevices.Clear();
                        RGBController.UnloadDeviceProvider(prov);
                        prov.Dispose();
                    }
                    var cur = AppSettings.GetSettings();
                    cur.deviceQmkRawHidEnabled = false;
                    AppSettings.SaveSettings(cur);
                }));

            // Yeelight — LAN-protocol bulbs, strips, lamps, ceiling lights.
            // Mirrors the LIFX flow: enabling the toggle pops the
            // YeelightAdoptionDialog which runs SSDP discovery and lets
            // the user pick which bulbs Chromatics drives. Empty
            // selection or no bulbs found leaves the toggle off.
            DeviceToggles.Add(new DeviceToggleItem(
                "Yeelight (Beta)",
                "[BETA] Enable/disable Yeelight LAN device support. Discovers Yeelight bulbs, light strips, lamps, and ceiling lights on your LAN (requires LAN Control enabled in the Yeelight / Mi Home app). Default: Disabled",
                s.deviceYeelightEnabled,
                async () =>
                {
                    var cur = AppSettings.GetSettings();
                    var owner = GetMainWindow();

                    var alreadyAdopted = (cur.deviceYeelightAdoptedDevices ?? new System.Collections.Generic.List<YeelightAdoptedDevice>())
                        .ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);

                    var dlg = new YeelightAdoptionDialog(alreadyAdopted);
                    if (owner != null)
                        await dlg.ShowDialog(owner);
                    else
                        dlg.Show();

                    if (!dlg.Saved) return false;

                    // Empty selection or discovery returning nothing → leave
                    // the toggle off so the user sees the immediate "didn't
                    // take" UX rather than an empty-but-on provider.
                    if (dlg.SelectedDevices == null || dlg.SelectedDevices.Count == 0)
                        return false;

                    cur.deviceYeelightAdoptedDevices = dlg.SelectedDevices;
                    cur.deviceYeelightEnabled = true;
                    AppSettings.SaveSettings(cur);

                    Chromatics.Extensions.RGB.NET.Devices.Yeelight.YeelightRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                    foreach (var d in cur.deviceYeelightAdoptedDevices)
                    {
                        System.Net.IPEndPoint ep = null;
                        if (!string.IsNullOrEmpty(d.LastIp) &&
                            System.Net.IPAddress.TryParse(d.LastIp, out var ip))
                        {
                            ep = new System.Net.IPEndPoint(ip, d.LastPort > 0 ? d.LastPort : 55443);
                        }
                        Chromatics.Extensions.RGB.NET.Devices.Yeelight.YeelightRGBDeviceProvider.Instance.ClientDefinitions.Add(
                            new Chromatics.Extensions.RGB.NET.Devices.Yeelight.YeelightClientDefinition(
                                d.Id, d.Label, ep, d.Model, d.FirmwareVersion, d.Support));
                    }

                    // LoadDeviceProvider runs LoadDevices synchronously which
                    // for Yeelight includes a discovery sweep + per-bulb TCP
                    // connect + Music Mode handshake (~1.5s per bulb on a
                    // healthy LAN). Push to a background thread so the
                    // toggle returns immediately and the UI stays responsive
                    // while bulbs come online.
                    _ = Task.Run(() => RGBController.LoadDeviceProvider(
                        Chromatics.Extensions.RGB.NET.Devices.Yeelight.YeelightRGBDeviceProvider.Instance));
                    return true;
                },
                () =>
                {
                    var prov = Chromatics.Extensions.RGB.NET.Devices.Yeelight.YeelightRGBDeviceProvider.Instance;
                    if (prov != null)
                    {
                        prov.ClientDefinitions.Clear();
                        RGBController.UnloadDeviceProvider(prov);
                        prov.Dispose();
                    }
                    var cur = AppSettings.GetSettings();
                    cur.deviceYeelightEnabled = false;
                    AppSettings.SaveSettings(cur);
                }));

            // Alienware AlienFX — pure managed HID via HidSharp, no native
            // DLL or Dell driver. Three HID dialects (V4 zone chassis, V5
            // notebook per-key, V8 external per-key) dispatched from one
            // provider; auto-adopt every AlienFX device discovered on
            // first enable. Mapping tab handles per-device disable.
            DeviceToggles.Add(new DeviceToggleItem(
                "Alienware (Beta)",
                "[BETA] Enable/disable Alienware AlienFX device support. Auto-adopts any AlienFX-capable Alienware or Dell G-series chassis, notebook keyboard, or external keyboard discovered on the HID bus. Default: Disabled",
                s.deviceAlienwareEnabled,
                async () =>
                {
                    var cur = AppSettings.GetSettings();
                    Logger.WriteConsole(LoggerTypes.Devices,
                        "[Alienware] Scanning for AlienFX devices on the HID bus...");

                    bool result = await Task.Run(() =>
                    {
                        var discovered = Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol.AlienwareDiscovery.Discover();
                        if (discovered.Count == 0) return false;

                        var adopted = new System.Collections.Generic.List<AlienwareAdoptedDevice>();
                        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareRGBDeviceProvider.Instance.ClientDefinitions.Clear();

                        foreach (var c in discovered)
                        {
                            string key = $"{c.Hid.VendorID:X4}:{c.Hid.ProductID:X4}:{c.Hid.DevicePath}";
                            if (!seen.Add(key)) continue;

                            adopted.Add(new AlienwareAdoptedDevice
                            {
                                VendorId = c.Hid.VendorID,
                                ProductId = c.Hid.ProductID,
                                Manufacturer = c.Manufacturer,
                                Product = c.Product,
                                DevicePath = c.Hid.DevicePath,
                                ApiVersion = c.ApiVersion.ToString(),
                                LightCount = c.LightCount,
                                ReportLength = c.ReportLength,
                            });

                            Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareRGBDeviceProvider.Instance.ClientDefinitions.Add(
                                new Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareClientDefinition(
                                    c.Hid.VendorID, c.Hid.ProductID, c.Manufacturer, c.Product,
                                    c.ApiVersion, c.LightCount, c.ReportLength, c.Hid.DevicePath));
                        }

                        cur.deviceAlienwareAdoptedDevices = adopted;
                        cur.deviceAlienwareEnabled = true;
                        AppSettings.SaveSettings(cur);

                        RGBController.LoadDeviceProvider(
                            Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareRGBDeviceProvider.Instance);
                        return true;
                    });

                    if (!result)
                    {
                        await DialogService.ShowAsync(
                            LocalizationService.Instance["No Alienware Devices Found"],
                            LocalizationService.Instance["Chromatics didn't detect any AlienFX hardware on this PC. Make sure you're on an Alienware (or Dell G-series) machine with AlienFX lighting. If Alienware Command Center or another AlienFX tool is running, close it before enabling this provider - they hold the HID interface exclusively."]);
                    }
                    return result;
                },
                () =>
                {
                    var prov = Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareRGBDeviceProvider.Instance;
                    if (prov != null)
                    {
                        prov.ClientDefinitions.Clear();
                        RGBController.UnloadDeviceProvider(prov);
                        prov.Dispose();
                    }
                    var cur = AppSettings.GetSettings();
                    cur.deviceAlienwareEnabled = false;
                    AppSettings.SaveSettings(cur);
                }));

            // Windows Dynamic Lighting (LampArray). Discovery is handled by
            // the Windows DeviceWatcher inside the provider so there's no
            // per-device adoption picker — every Dynamic-Lighting-capable
            // device the OS exposes shows up automatically.
            //
            // The sparse package registration is intentionally NOT torn down
            // when the user toggles this off. Identity is bound to the
            // process by the OS loader at CreateProcess time; if we
            // deregistered on disable, a subsequent re-enable in the same
            // process would not re-acquire identity and background lighting
            // would silently fall back to foreground-only until the user
            // restarted Chromatics. Settings → Reset (and the Velopack
            // uninstall callback) are the two paths that explicitly remove
            // the registration when the user actually wants it gone.
            //
            // Background lighting on Windows 10 isn't supported by the OS
            // (LampArray is a Windows 11 API), so the toggle is greyed out
            // there via isAvailable; the tooltip explains the requirement.
            bool isWin11OrLater = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
            DeviceToggles.Add(new DeviceToggleItem(
                "Windows Dynamic Lighting (Beta)",
                "[BETA] Enable/disable the Windows Dynamic Lighting provider. Requires Windows 11. Picks up any device Windows lists in Settings -> Personalization -> Dynamic Lighting (Razer, Logitech G LIGHTSYNC, ASUS ROG, HyperX, MSI, SteelSeries, HP/Omen). Default: Disabled",
                s.deviceDynamicLightingEnabled,
                async () =>
                {
                    // Conflict popup (vendor providers already enabled).
                    await ShowDynamicLightingOverlapPopupIfNeededAsync("Windows Dynamic Lighting (Beta)").ConfigureAwait(true);

                    // Register BEFORE loading the device provider so the
                    // package exists for AmbientLightingServer-side checks
                    // when the provider starts adopting devices. (Identity
                    // binding to the CURRENT process can only happen at
                    // process start, so the user may still need to restart
                    // for background to kick in on their very first enable —
                    // Program.cs handles that auto-restart for subsequent
                    // launches.)
                    if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                        await SparsePackageRegistrar.EnsureRegisteredAsync().ConfigureAwait(true);

                    // First-time hint dialog walking the user through the
                    // Settings → Personalization → Dynamic Lighting steps,
                    // including dragging Chromatics to the top of the
                    // Background light control list for gameplay-time writes.
                    var cur = AppSettings.GetSettings();
                    if (!cur.dynamicLightingHintShown)
                    {
                        try
                        {
                            var owner = GetMainWindow();
                            var hintDlg = new DynamicLightingHintDialog();
                            if (owner != null) await hintDlg.ShowDialog(owner).ConfigureAwait(true);
                            else hintDlg.Show();
                        }
                        catch { /* dialog failure shouldn't block enable */ }
                    }

                    RGBController.LoadDeviceProvider(DynamicLightingRGBDeviceProvider.Instance);

                    // Empty-result handling: if Windows enumerated zero
                    // compatible devices, unload the provider and leave the
                    // toggle off rather than ship an enabled-but-empty
                    // provider. Mirrors the LIFX / Yeelight pattern.
                    if (DynamicLightingRGBDeviceProvider.Instance.AdoptedDeviceCount == 0)
                    {
                        RGBController.UnloadDeviceProvider(DynamicLightingRGBDeviceProvider.Instance);
                        var cc = AppSettings.GetSettings();
                        cc.deviceDynamicLightingEnabled = false;
                        AppSettings.SaveSettings(cc);
                        await DialogService.ShowAsync(
                            LocalizationService.Instance["No Dynamic Lighting devices found"],
                            LocalizationService.Instance["Chromatics didn't detect any Dynamic Lighting devices on this PC. Open Settings -> Personalization -> Dynamic Lighting in Windows and check that at least one compatible device is listed there. If your hardware is listed but Chromatics still doesn't see it, it may be hidden by an enabled vendor provider conflict; Settings -> Advanced has a toggle to control that."]);
                        return false;
                    }

                    var c = AppSettings.GetSettings();
                    c.deviceDynamicLightingEnabled = true;
                    AppSettings.SaveSettings(c);
                    return true;
                },
                () =>
                {
                    RGBController.UnloadDeviceProvider(DynamicLightingRGBDeviceProvider.Instance);
                    var c = AppSettings.GetSettings();
                    c.deviceDynamicLightingEnabled = false;
                    AppSettings.SaveSettings(c);
                    // Intentionally NO DeregisterAsync — see block comment
                    // above. Keeping the registration keeps process identity
                    // valid for subsequent re-enables in the same session.
                },
                isAvailable: isWin11OrLater));
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
                async () =>
                {
                    // Surface the Dynamic Lighting overlap popup before
                    // we kick off the load. Either side (vendor → DL or
                    // DL → vendor) gets a one-shot warning naming the
                    // other provider so the user knows what to expect.
                    // The auto-dedup in DynamicLightingRGBDeviceProvider
                    // makes the actual conflict harmless (vendor SDK
                    // wins on overlapping devices), but we still tell
                    // the user up-front rather than make them figure it
                    // out from the device list.
                    await ShowDynamicLightingOverlapPopupIfNeededAsync(label).ConfigureAwait(true);

                    load();
                    saveFlag(true);
                    return true;
                },
                () =>
                {
                    unload();
                    saveFlag(false);
                });
        }

        // Shows a one-shot popup naming the other half of an overlap
        // (DL → enabled vendor providers, or vendor → DL when DL is
        // enabled). Returns immediately for any provider that doesn't
        // overlap with Dynamic Lighting (Corsair, Wooting, Coolermaster,
        // Novation, OpenRGB, etc.) so the helper is safe to call
        // unconditionally from MakeDeviceToggle.
        internal static async Task ShowDynamicLightingOverlapPopupIfNeededAsync(string enablingProviderLabel)
        {
            try
            {
                var settings = AppSettings.GetSettings();

                // Match both legacy "Dynamic Lighting (Beta)" and current
                // "Windows Dynamic Lighting (Beta)" labels — the rename to
                // include "Windows" broke a plain StartsWith check.
                bool isDl = enablingProviderLabel.Contains("Dynamic Lighting", StringComparison.OrdinalIgnoreCase);
                IReadOnlyList<string> conflicts;
                if (isDl)
                {
                    conflicts = Chromatics.Extensions.RGB.NET.Devices.DynamicLighting.DynamicLightingVendorOverlap
                        .GetEnabledOverlappingVendorNames(settings);
                    if (conflicts.Count == 0) return;
                }
                else
                {
                    if (!Chromatics.Extensions.RGB.NET.Devices.DynamicLighting.DynamicLightingVendorOverlap
                            .IsOverlappingVendorName(enablingProviderLabel)) return;
                    if (!settings.deviceDynamicLightingEnabled) return;
                    conflicts = new[] { "Dynamic Lighting" };
                }

                string title = LocalizationService.Instance["Provider conflict"];
                string template = LocalizationService.Instance["{0} overlaps with {1}. Both providers may try to control the same physical devices on this PC. To prevent flickering, the existing vendor SDK takes priority on overlapping devices and Dynamic Lighting silently skips them; you can override per device from the Mappings tab."];
                string body = string.Format(template, enablingProviderLabel, string.Join(", ", conflicts));
                await DialogService.ShowAsync(title, body).ConfigureAwait(true);
            }
            catch { /* popup is advisory; never block toggle on failure */ }
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

        private bool _dynamicLightingBypassConflictCheck;
        public bool DynamicLightingBypassConflictCheck
        {
            get => _dynamicLightingBypassConflictCheck;
            set
            {
                if (SetProperty(ref _dynamicLightingBypassConflictCheck, value))
                {
                    var s = AppSettings.GetSettings();
                    s.dynamicLightingBypassConflictCheck = value;
                    AppSettings.SaveSettings(s);
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

            // Reset is the user's "nuke everything Chromatics-related" path, so
            // also tear down the Dynamic Lighting sparse package registration —
            // otherwise Chromatics would keep appearing in Settings →
            // Personalization → Dynamic Lighting → Background light control
            // even after the user wiped local state. Fire-and-forget so the
            // ~1s RemovePackageAsync call doesn't block the UI; the OS
            // completes the removal even if we exit before it finishes.
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                _ = SparsePackageRegistrar.DeregisterAsync();
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
