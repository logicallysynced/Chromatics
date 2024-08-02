using AutoUpdaterDotNET;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Helpers;
using Chromatics.Localization;
using Chromatics.Properties;
using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Controls;
using MetroFramework.Forms;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Org.BouncyCastle.Utilities.Net;
using Q42.HueApi;
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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

namespace Chromatics.Forms
{
    public partial class Uc_Settings : UserControl
    {
        private MetroToolTip tt_mappings;
        private readonly RegistryKey _rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        private readonly Color tilecol_enabled = Color.DeepSkyBlue;
        private readonly Color tilecol_disabled = Color.DarkRed;

        public Uc_Settings()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            //Add tooltips
            tt_mappings = new MetroToolTip();

            tt_mappings.SetToolTip(this.chk_localcache, LocalizationManager.GetLocalizedText("Use saved cache for FFXIV data instead of online source. Default: Disabled"));
            tt_mappings.SetToolTip(this.chk_winstart, LocalizationManager.GetLocalizedText("Start Chromatics when Windows starts. Default: Disabled"));
            tt_mappings.SetToolTip(this.chk_minimizetray, LocalizationManager.GetLocalizedText("Minimise to system tray. Default: Enabled"));
            tt_mappings.SetToolTip(this.chk_trayonstartup, LocalizationManager.GetLocalizedText("Minimise to system tray when Chromatics starts. Default: Disabled"));
            tt_mappings.SetToolTip(this.btn_resetchromatics, LocalizationManager.GetLocalizedText("Restore Chromatics to its default state. Requires application restart."));
            tt_mappings.SetToolTip(this.btn_clearcache, LocalizationManager.GetLocalizedText("Clear local FFXIV cache. Requires application restart."));
            tt_mappings.SetToolTip(this.trackbar_lighting, LocalizationManager.GetLocalizedText("Adjust global brightness for all devices."));
            tt_mappings.SetToolTip(this.chk_updatecheck, LocalizationManager.GetLocalizedText("Enable checking for updates on Chromatics start. Default: Enabled"));
            tt_mappings.SetToolTip(this.cb_theme, LocalizationManager.GetLocalizedText("Change the interface theme. Default: System"));
            tt_mappings.SetToolTip(this.cb_language, LocalizationManager.GetLocalizedText("Change Chromatics' language. Default: English"));
            tt_mappings.SetToolTip(mt_settings_razer, LocalizationManager.GetLocalizedText("Enable/disable Razer device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_logitech, LocalizationManager.GetLocalizedText("Enable/disable Logitech device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_corsair, LocalizationManager.GetLocalizedText("Enable/disable Corsair device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_coolermaster, LocalizationManager.GetLocalizedText("Enable/disable Coolermaster device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_steelseries, LocalizationManager.GetLocalizedText("Enable/disable SteelSeries device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_asus, LocalizationManager.GetLocalizedText("Enable/disable ASUS device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_msi, LocalizationManager.GetLocalizedText("Enable/disable MSI device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_wooting, LocalizationManager.GetLocalizedText("Enable/disable Wooting device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_novation, LocalizationManager.GetLocalizedText("Enable/disable Novation device library. Default: Enabled"));
            tt_mappings.SetToolTip(mt_settings_openrgb, LocalizationManager.GetLocalizedText("Enable/disable OpenRGB device library. Default: Disabled"));
            tt_mappings.SetToolTip(mt_settings_hue, LocalizationManager.GetLocalizedText("[BETA] Enable/disable Philips HUE device library. Default: Disabled"));


            //Startup
            var settings = AppSettings.GetSettings();

            // Populate the ComboBox for Theme
            cb_theme.Items.AddRange(Enum.GetValues(typeof(Theme))
                .Cast<Theme>()
                .Select(t => new ComboBoxItem<Theme>(t, t.ToString()))
                .ToArray());

            // Populate the ComboBox for Language
            cb_language.Items.AddRange(Enum.GetValues(typeof(Language))
            .Cast<Language>()
            .Select(l => new ComboBoxItem<Language>(l, l.GetDisplayName()))
            .ToArray());


            chk_localcache.Checked = settings.localcache;
            chk_winstart.Checked = settings.winstart;
            chk_minimizetray.Checked = settings.minimizetray;
            chk_trayonstartup.Checked = settings.trayonstartup;
            trackbar_lighting.Value = settings.globalbrightness;
            lbl_devicebrightpercent.Text = $"{settings.globalbrightness}%";
            chk_updatecheck.Checked = settings.checkupdates;
            cb_theme.SelectedIndex = (int)settings.systemTheme;
            cb_language.SelectedIndex = (int)settings.systemLanguage;

            mt_settings_razer.BackColor = settings.deviceRazerEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_logitech.BackColor = settings.deviceLogitechEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_corsair.BackColor = settings.deviceCorsairEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_coolermaster.BackColor = settings.deviceCoolermasterEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_steelseries.BackColor = settings.deviceSteelseriesEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_asus.BackColor = settings.deviceAsusEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_msi.BackColor = settings.deviceMsiEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_wooting.BackColor = settings.deviceWootingEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_novation.BackColor = settings.deviceNovationEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_openrgb.BackColor = settings.deviceOpenRGBEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_hue.BackColor = settings.deviceHueEnabled ? tilecol_enabled : tilecol_disabled;
        }

        private void chk_localcache_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckBox)sender;
            var settings = AppSettings.GetSettings();
            settings.localcache = checkbox.Checked;

            AppSettings.SaveSettings(settings);
        }

        private void chk_winstart_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckBox)sender;
            var settings = AppSettings.GetSettings();
            settings.winstart = checkbox.Checked;

            AppSettings.SaveSettings(settings);
        }

        private void chk_minimizetray_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckBox)sender;
            var settings = AppSettings.GetSettings();
            settings.minimizetray = checkbox.Checked;

            AppSettings.SaveSettings(settings);
        }

        private void chk_trayonstartup_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckBox)sender;
            var settings = AppSettings.GetSettings();
            settings.trayonstartup = checkbox.Checked;

            if (settings.trayonstartup)
            {
                _rkApp.SetValue("Chromatics3", Application.ExecutablePath);
            }
            else
            {
                _rkApp.DeleteValue("Chromatics3", false);
            }

            AppSettings.SaveSettings(settings);
        }

        private void btn_resetchromatics_Click(object sender, EventArgs e)
        {
            var cacheReset =
                MessageBox.Show(
                    LocalizationManager.GetLocalizedText("Are you sure you wish to reset Chromatics? All settings, color palettes and layers will be reset."),
                    LocalizationManager.GetLocalizedText("Reset Chromatics?"), MessageBoxButtons.OKCancel);
            if (cacheReset != DialogResult.OK) return;

            try
            {
                string enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

                if (File.Exists(enviroment + @"/layers.chromatics3"))
                {
                    FileSystem.DeleteFile(enviroment + @"/layers.chromatics3");
                }

                if (File.Exists(enviroment + @"/palette.chromatics3"))
                {
                    FileSystem.DeleteFile(enviroment + @"/palette.chromatics3");
                }

                if (File.Exists(enviroment + @"/effects.chromatics3"))
                {
                    FileSystem.DeleteFile(enviroment + @"/effects.chromatics3");
                }

                if (File.Exists(enviroment + @"/settings.chromatics3"))
                {
                    FileSystem.DeleteFile(enviroment + @"/settings.chromatics3");
                }

                MessageBox.Show(LocalizationManager.GetLocalizedText("Chromatics has been reset. Chromatics will now close."), LocalizationManager.GetLocalizedText("Chromatics Reset"), MessageBoxButtons.OK);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.GetLocalizedText("Unable to reset Chromatics. Are you running as Administrator? Error: ") + ex.StackTrace, LocalizationManager.GetLocalizedText("Unable to reset Chromatics"), MessageBoxButtons.OK);
            }
        }

        private void btn_clearcache_Click(object sender, EventArgs e)
        {
            var cacheReset =
                MessageBox.Show(
                    LocalizationManager.GetLocalizedText("Are you sure you wish to clear Chromatics cache?"),
                    LocalizationManager.GetLocalizedText("Clear Cache?"), MessageBoxButtons.OKCancel);
            if (cacheReset != DialogResult.OK) return;

            try
            {
                string enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

                if (File.Exists(enviroment + @"/signatures-global-latest.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/signatures-global-latest.json");
                }

                if (File.Exists(enviroment + @"/structures-global-latest.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/structures-global-latest.json");
                }

                if (File.Exists(enviroment + @"/actions-latest.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/actions-latest.json");
                }

                if (File.Exists(enviroment + @"/statuses-latest.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/statuses-latest.json");
                }

                if (File.Exists(enviroment + @"/zones-latest.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/zones-latest.json");
                }

                if (File.Exists(enviroment + @"/terriTypes.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/terriTypes.json");
                }

                if (File.Exists(enviroment + @"/weatherKinds.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/weatherKinds.json");
                }

                if (File.Exists(enviroment + @"/weatherRateIndices.json"))
                {
                    FileSystem.DeleteFile(enviroment + @"/weatherRateIndices.json");
                }

                if (File.Exists(enviroment + @"/ParamGrow.csv"))
                {
                    FileSystem.DeleteFile(enviroment + @"/ParamGrow.csv");
                }

                MessageBox.Show(LocalizationManager.GetLocalizedText("Cache Cleared. Chromatics will now close."), LocalizationManager.GetLocalizedText("Cache Cleared"), MessageBoxButtons.OK);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.GetLocalizedText("Unable to clear cache. Are you running as Administrator? Error: ") + ex.StackTrace, LocalizationManager.GetLocalizedText("Unable to clear Cache"), MessageBoxButtons.OK);
            }
        }

        private void trackbar_lighting_Scroll(object sender, ScrollEventArgs e)
        {
            var trackbar = (MetroTrackBar)sender;
            var settings = AppSettings.GetSettings();

            settings.globalbrightness = trackbar.Value;
            lbl_devicebrightpercent.Text = $"{trackbar_lighting.Value}%";

            AppSettings.SaveSettings(settings);
        }

        private void chk_updatecheck_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckBox)sender;
            var settings = AppSettings.GetSettings();
            settings.checkupdates = checkbox.Checked;

            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_razer_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceRazerEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(RazerDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(RazerDeviceProvider.Instance);
            }

            settings.deviceRazerEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_logitech_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceLogitechEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(LogitechDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(LogitechDeviceProvider.Instance);
            }

            settings.deviceLogitechEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_corsair_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceCorsairEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(CorsairDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(CorsairDeviceProvider.Instance);
            }

            settings.deviceCorsairEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_steelseries_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceSteelseriesEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(SteelSeriesDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(SteelSeriesDeviceProvider.Instance);
            }

            settings.deviceSteelseriesEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_coolermaster_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceCoolermasterEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(CoolerMasterDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(CoolerMasterDeviceProvider.Instance);
            }

            settings.deviceCoolermasterEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_asus_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceAsusEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(AsusDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(AsusDeviceProvider.Instance);
            }

            settings.deviceAsusEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_wooting_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceWootingEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(WootingDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(WootingDeviceProvider.Instance);
            }

            settings.deviceWootingEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_msi_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceMsiEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(MsiDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(MsiDeviceProvider.Instance);
            }

            settings.deviceMsiEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void mt_settings_novation_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceNovationEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(NovationDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(NovationDeviceProvider.Instance);
            }

            settings.deviceNovationEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        

        private void mt_settings_hue_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceHueEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                if (HueRGBDeviceProvider.Instance != null)
                {
                    HueRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                    RGBController.UnloadDeviceProvider(HueRGBDeviceProvider.Instance);
                    HueRGBDeviceProvider.Instance.Dispose();
                }

                settings.deviceHueEnabled = false;
            }
            else
            {
                ShowHueSettingsForm(settings, tile);
            }

            
            AppSettings.SaveSettings(settings);
        }

        private void ShowHueSettingsForm(Models.SettingsModel settings, MetroTile tile)
        {
            MetroForm hueSettingsForm = new MetroForm();
            hueSettingsForm.Text = LocalizationManager.GetLocalizedText("Enter Hue Bridge Details");
            hueSettingsForm.MinimizeBox = false;
            hueSettingsForm.MaximizeBox = false;
            hueSettingsForm.Width = 400;
            hueSettingsForm.Height = 300;
            hueSettingsForm.Theme = SystemHelpers.IsDarkModeEnabled() ? MetroThemeStyle.Dark : MetroThemeStyle.Light;
            hueSettingsForm.KeyPreview = true; // To capture key events

            int padding = 80;

            MetroLabel labelIP = new MetroLabel { Text = LocalizationManager.GetLocalizedText("Bridge IP:"), Left = 10, Top = padding, Width = 100 };
            MetroTextBox textBoxIP = new MetroTextBox { Left = 120, Top = padding, Width = 200, Text = settings.deviceHueBridgeIP };

            MetroButton btnSubmit = new MetroButton { Text = LocalizationManager.GetLocalizedText("Submit"), Left = 120, Top = padding + 120, Width = 100 };

            // Create a Timer for the timeout
            Timer timeoutTimer = new Timer { Interval = 10000 }; // 10 seconds

            // Event handler for submit action
            async void SubmitForm()
            {
                var hueProvider = new HueRGBDeviceProvider();
                try
                {
                    if (IsValidIPAddress(textBoxIP.Text) && !string.IsNullOrEmpty(textBoxIP.Text))
                    {
                        btnSubmit.Text = LocalizationManager.GetLocalizedText("Connecting..");

                        hueProvider.ClientDefinitions.Clear();
                        HueClientDefinition hueBridge = null;

                        settings.deviceHueBridgeIP = textBoxIP.Text;
                        settings.deviceHueEnabled = true;
                        tile.BackColor = tilecol_enabled;

                        AppSettings.SaveSettings(settings);

                        hueBridge = new HueClientDefinition(settings.deviceHueBridgeIP, "chromatics", "pvpGWu0ets21cUUZGOHqd63Eb28i2QEx");
                        hueProvider.ClientDefinitions.Add(hueBridge);

                        var task = Task.Run(() =>
                        {
                            return RGBController.LoadDeviceProvider(hueProvider);
                        });

                        if (await Task.WhenAny(task, Task.Delay(10000)) == task)
                        {
                            if (!task.Result)
                            {
                                Logger.WriteConsole(Enums.LoggerTypes.Error, $"[Hue] LoadDeviceProvider Error.");
                                MessageBox.Show($"Unable to connect to Hue Bridge at IP address {settings.deviceHueBridgeIP}. Please check the IP address and try again.");
                                btnSubmit.Text = LocalizationManager.GetLocalizedText("Submit");

                                tile.BackColor = tilecol_disabled;

                                settings.deviceHueEnabled = false;

                                AppSettings.SaveSettings(settings);
                            }

                            hueSettingsForm.Close();
                        }
                        else
                        {
                            MessageBox.Show("Operation timed out. The form will now close.");
                            hueSettingsForm.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show(LocalizationManager.GetLocalizedText("Invalid or empty field. Please check the entered values."));
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"[Hue] LoadDeviceProvider Error: {ex.Message}");
                    MessageBox.Show($"Unable to connect to Hue Bridge at IP address {settings.deviceHueBridgeIP}. Please check the IP address and try again.");

                    tile.BackColor = tilecol_disabled;

                    if (HueRGBDeviceProvider.Instance != null)
                    {
                        HueRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                        RGBController.UnloadDeviceProvider(HueRGBDeviceProvider.Instance);
                        HueRGBDeviceProvider.Instance.Dispose();
                    }

                    if (hueProvider != null)
                    {
                        hueProvider.ClientDefinitions.Clear();
                        RGBController.UnloadDeviceProvider(hueProvider);
                        hueProvider.Dispose();
                    }

                    settings.deviceHueEnabled = false;

                    AppSettings.SaveSettings(settings);
                }
            }

            btnSubmit.Click += (s, e) => SubmitForm();

            hueSettingsForm.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    hueSettingsForm.Close();
                }
                else if (e.KeyCode == Keys.Return)
                {
                    SubmitForm();
                }
            };

            hueSettingsForm.Controls.Add(labelIP);
            hueSettingsForm.Controls.Add(textBoxIP);
            hueSettingsForm.Controls.Add(btnSubmit);

            hueSettingsForm.ShowDialog();
        }


        private bool IsValidIPAddress(string ipAddress)
            {
                return System.Net.IPAddress.TryParse(ipAddress, out _);
            }
            private void mt_settings_openRGB_Click(object sender, EventArgs e)
            {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceOpenRGBEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;

                RGBController.UnloadDeviceProvider(OpenRGBDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(OpenRGBDeviceProvider.Instance);
            }

            settings.deviceOpenRGBEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void OnResize(object sender, EventArgs e)
        {
            // Check if the form is minimized
            if (Fm_MainWindow.GetForm().WindowState == FormWindowState.Minimized)
            {
                return;
            }

            foreach (var control in tlp_devices.Controls)
            {
                if (control.GetType() != typeof(MetroTile)) continue;
                var tile = (MetroTile)control;

                var previmg = tile.TileImage;
                var newimg = ScaleImage(previmg, tile.Width / 2, tile.Height / 2);

                tile.TileImage = newimg;
                tile.TileImageAlign = ContentAlignment.MiddleCenter;
                tile.Invalidate();
            }
        }

        private static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image), "The image parameter cannot be null.");

            if (maxWidth <= 0 || maxHeight <= 0)
                throw new ArgumentException("maxWidth and maxHeight must be greater than zero.");

            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            if (newWidth <= 0 || newHeight <= 0)
                throw new ArgumentException("Calculated width and height must be greater than zero.");

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }


        private void cb_theme_SelectedIndexChanged(object sender, EventArgs e)
        {
            var settings = AppSettings.GetSettings();

            if ((Theme)cb_theme.SelectedIndex != settings.systemTheme)
            {
                settings.systemTheme = ((ComboBoxItem<Theme>)cb_theme.SelectedItem).Value;
                AppSettings.SaveSettings(settings);


                if (settings.systemTheme == Enums.Theme.System)
                {
                    if (SystemHelpers.IsDarkModeEnabled())
                    {
                        Fm_MainWindow.SetDarkMode(true);
                    }
                    else
                    {
                        Fm_MainWindow.SetDarkMode(false);
                    }
                }
                else if (settings.systemTheme == Enums.Theme.Dark)
                {
                    Fm_MainWindow.SetDarkMode(true);
                }
                else if (settings.systemTheme == Enums.Theme.Light)
                {
                    Fm_MainWindow.SetDarkMode(false);
                }


            }


        }

        private void cb_language_SelectedIndexChanged(object sender, EventArgs e)
        {
            var settings = AppSettings.GetSettings();
            settings.systemLanguage = ((ComboBoxItem<Language>)cb_language.SelectedItem).Value;

            AppSettings.SaveSettings(settings);

            Fm_MainWindow.TranslateForm();

        }

        public class ComboBoxItem<T>
        {
            public T Value { get; }
            public string DisplayName { get; }

            public ComboBoxItem(T value, string displayName)
            {
                Value = value;
                DisplayName = displayName;
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }

    }
}
