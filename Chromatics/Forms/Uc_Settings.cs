using AutoUpdaterDotNET;
using Chromatics.Core;
using MetroFramework.Components;
using MetroFramework.Controls;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
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

            //Startup
            var settings = AppSettings.GetSettings();

            chk_localcache.Checked = settings.localcache;
            chk_winstart.Checked = settings.winstart;
            chk_minimizetray.Checked = settings.minimizetray;
            chk_trayonstartup.Checked = settings.trayonstartup;
            trackbar_lighting.Value = settings.globalbrightness;
            lbl_devicebrightpercent.Text = $"{settings.globalbrightness}%";
            chk_updatecheck.Checked = settings.checkupdates;
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
    }
}
