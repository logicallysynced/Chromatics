using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using MetroFramework.Components;
using MetroFramework.Forms;
using Sharlayan.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using static Chromatics.Models.VirtualDevice;
using System.Reflection;
using System.Windows;
using Chromatics.Models;
using System.IO;

namespace Chromatics.Forms
{
    public partial class Fm_MainWindow : MetroForm
    {
        private MetroStyleManager metroStyleManager;
        private Form mainForm;
        private SettingsModel appSettings;

        public Fm_MainWindow()
        {
            //Correct for DPI settings
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font(Font.Name, 8.25f * 100f / CreateGraphics().DpiY, Font.Style, Font.Unit, Font.GdiCharSet, Font.GdiVerticalFont);

            //Start Form
            InitializeComponent();

            metroStyleManager = new MetroStyleManager(); 
            metroStyleManager.Owner = this;
            metroStyleManager.Theme = MetroFramework.MetroThemeStyle.Default;
            metroStyleManager.Style = MetroFramework.MetroColorStyle.Pink;

            this.Theme = metroStyleManager.Theme;
            this.Style = metroStyleManager.Style;
            this.Size = new System.Drawing.Size(1400, 885);

            mainForm = this;
            
            //Load Settings
            AppSettings.Startup();
            appSettings = AppSettings.GetSettings();
            

            //Initiate Tabs
            var uC_Console = new Uc_Console
            {
                Dock = DockStyle.Fill
            };

            var uC_Mappings = new Uc_Mappings
            {
                Dock = DockStyle.Fill
            };

            var uC_Palette = new Uc_Palette
            {
                Dock = DockStyle.Fill
            };

            var uC_Effects = new Uc_Effects
            {
                Dock = DockStyle.Fill
            };

            var uC_Settings = new Uc_Settings
            {
                Dock = DockStyle.Fill
            };

            tP_console.Controls.Add(uC_Console);
            tP_mappings.Controls.Add(uC_Mappings);
            tP_palette.Controls.Add(uC_Palette);
            tP_Effects.Controls.Add(uC_Effects);
            tP_Settings.Controls.Add(uC_Settings);

            uC_Mappings.TabManager = mT_TabManager;

            this.ResizeBegin += (s, e) => { this.SuspendLayout(); };
            this.ResizeEnd += (s, e) => { this.ResumeLayout(true); };

            contextMenuStrip_main.Items.Add(new ToolStripMenuItem(@"Show Window", null, new EventHandler(OnNotifyIconDoubleClick)));
            contextMenuStrip_main.Items.Add(new ToolStripMenuItem(@"Close", null, new EventHandler(OnNotifyClickClose)));
            notifyIcon_main.ContextMenuStrip = contextMenuStrip_main;

            if (appSettings.trayonstartup)
            {
                Hide();
                this.WindowState = FormWindowState.Minimized;
                this.Visible = false;
                this.ShowInTaskbar = false;
            }

            //Setup Defaults
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            if (!File.Exists(enviroment + @"/layers.chromatics3"))
            {
                File.Copy(enviroment + @"/defaults/layers.chromatics3", enviroment + @"/layers.chromatics3");
            }

            Logger.WriteConsole(LoggerTypes.System, @"Chromatics is starting up..");

        }

        private void OnLoad(object sender, EventArgs e)
        {
            //Load all tabs into memory on boot
            for (int i = 1; i < mT_TabManager.TabPages.Count; i++)
                mT_TabManager.SelectedIndex = i;

            mT_TabManager.SelectedIndex = 0;
            
            //Setup Chromatics
            SetupChromatics();

            
        }

        private void SetupChromatics()
        {
            //Check for updates
            Logger.WriteConsole(LoggerTypes.System, @"Checking for updates..");
            AutoUpdater.Start("https://chromaticsffxiv.com/chromatics3/update/update.xml");
            AutoUpdater.ShowSkipButton = false;

            var assembly = typeof(Program).Assembly;

            if (assembly.GetName().Version.Revision != 0)
            {
                Logger.WriteConsole(LoggerTypes.System, $"Chromatics {assembly.GetName().Version.Major}.{assembly.GetName().Version.Minor}.{assembly.GetName().Version.Build}.{assembly.GetName().Version.Revision} (BETA) has loaded");
                this.Text = $"Chromatics {assembly.GetName().Version.Major}.{assembly.GetName().Version.Minor}.{assembly.GetName().Version.Build}.{assembly.GetName().Version.Revision} (BETA)";
            }
            else
            {
                Logger.WriteConsole(LoggerTypes.System, $"Chromatics {assembly.GetName().Version.Major}.{assembly.GetName().Version.Minor} has loaded");
            }

            RunChromaticsThread();
        }

        private async void RunChromaticsThread()
        {
            //Start Chromatics
            await Task.Run(() => FileOperationsHelper.GetUpdatedWeatherData());

            KeyController.Setup();

            await Task.Run(() => RGBController.Setup());

            GameController.Setup();
        }

        protected void Dispose(bool disposing, bool isMainWindow)
        { 
            if( disposing ) 
            {
                this.notifyIcon_main.Dispose();
                this.contextMenuStrip_main.Dispose();
            }

            base.Dispose( disposing );
        }

        private void OnResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && appSettings.minimizetray)
            {
                Hide();
                ShowInTaskbar = false;
            }
        }

        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            if (!mainForm.Visible)
            {
                mainForm.Show();
                this.WindowState = FormWindowState.Normal;
                this.Visible = true;
                this.ShowInTaskbar = true;
            }
            else
            {
                if (appSettings.minimizetray)
                {
                    mainForm.Hide();
                    this.WindowState = FormWindowState.Minimized;
                    this.Visible = false;
                    this.ShowInTaskbar = false;
                }
            }
        }

        private void OnNotifyClickClose(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }


    }
}
