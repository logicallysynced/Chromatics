using AutoUpdaterDotNET;
using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Properties;
using MetroFramework.Components;
using MetroFramework.Controls;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.CoolerMaster;
using RGB.NET.Devices.Corsair;
using RGB.NET.Devices.Logitech;
using RGB.NET.Devices.Msi;
using RGB.NET.Devices.Novation;
using RGB.NET.Devices.Razer;
using RGB.NET.Devices.SteelSeries;
using RGB.NET.Devices.Wooting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

            tt_mappings.SetToolTip(this.chk_localcache, "Use saved cache for FFXIV data instead of online source. Default: Disabled");
            tt_mappings.SetToolTip(this.chk_winstart, "Start Chromatics when Windows starts. Default: Disabled");
            tt_mappings.SetToolTip(this.chk_minimizetray, "Minimise to system tray. Default: Enabled");
            tt_mappings.SetToolTip(this.chk_trayonstartup, "Minimise to system tray when Chromatics starts. Default: Disabled");
            tt_mappings.SetToolTip(this.btn_resetchromatics, @"Restore Chromatics to its default state. Requires application restart.");
            tt_mappings.SetToolTip(this.btn_clearcache, @"Clear local FFXIV cache. Requires application restart.");
            tt_mappings.SetToolTip(this.trackbar_lighting, @"Adjust global brightness for all devices.");
            tt_mappings.SetToolTip(this.chk_updatecheck, @"Enable checking for updates on Chromatics start. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_razer, @"Enable/disable Razer device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_logitech, @"Enable/disable Logitech device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_corsair, @"Enable/disable Corsair device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_coolermaster, @"Enable/disable Coolermaster device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_steelseries, @"Enable/disable SteelSeries device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_asus, @"Enable/disable ASUS device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_msi, @"Enable/disable MSI device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_wooting, @"Enable/disable Wooting device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_novation, @"Enable/disable Novation device library. Requires App Restart. Default: Enabled");
            tt_mappings.SetToolTip(mt_settings_hue, @"[BETA] Enable/disable Philips HUE device library. Requires App Restart. Default: Disabled");

            //Startup
            var settings = AppSettings.GetSettings();

            chk_localcache.Checked = settings.localcache;
            chk_winstart.Checked = settings.winstart;
            chk_minimizetray.Checked = settings.minimizetray;
            chk_trayonstartup.Checked = settings.trayonstartup;
            trackbar_lighting.Value = settings.globalbrightness;
            lbl_devicebrightpercent.Text = $"{settings.globalbrightness}%";
            chk_updatecheck.Checked = settings.checkupdates;

            mt_settings_razer.BackColor = settings.deviceRazerEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_logitech.BackColor = settings.deviceLogitechEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_corsair.BackColor = settings.deviceCorsairEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_coolermaster.BackColor = settings.deviceCoolermasterEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_steelseries.BackColor = settings.deviceSteelseriesEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_asus.BackColor = settings.deviceAsusEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_msi.BackColor = settings.deviceMsiEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_wooting.BackColor = settings.deviceWootingEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_novation.BackColor = settings.deviceNovationEnabled ? tilecol_enabled : tilecol_disabled;
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
                    @"Are you sure you wish to reset Chromatics? All settings, color palettes and layers will be reset.",
                    @"Reset Chromatics?", MessageBoxButtons.OKCancel);
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

                MessageBox.Show(@"Chromatics has been reset. Chromatics will now close.", @"Chromatics Reset", MessageBoxButtons.OK);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Unable to reset Chromatics. Are you running as Administrator? Error: " + ex.StackTrace, @"Unable to reset Chromatics", MessageBoxButtons.OK);
            }
        }

        private void btn_clearcache_Click(object sender, EventArgs e)
        {
            var cacheReset =
                MessageBox.Show(
                    @"Are you sure you wish to clear Chromatics cache?",
                    @"Clear Cache?", MessageBoxButtons.OKCancel);
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

                MessageBox.Show(@"Cache Cleared. Chromatics will now close.", @"Cache Cleared", MessageBoxButtons.OK);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Unable to clear cache. Are you running as Administrator? Error: " + ex.StackTrace, @"Unable to clear Cache", MessageBoxButtons.OK);
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

                RGBController.UnloadDeviceProvider(HueRGBDeviceProvider.Instance);
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;

                RGBController.LoadDeviceProvider(HueRGBDeviceProvider.Instance);
            }

            settings.deviceHueEnabled = device;
            AppSettings.SaveSettings(settings);
        }

        private void OnResize(object sender, EventArgs e)
        {
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
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }
    }
}
