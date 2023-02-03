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

namespace Chromatics.Forms
{
    public partial class Fm_MainWindow : MetroForm
    {
        private MetroStyleManager metroStyleManager;
        //private Thread _ChromaticsThread;

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
            this.Size = new Size(1400, 885);

            
            //Load Settings
            AppSettings.Startup();


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
            RGBController.Setup();
            GameController.Setup();
        }

        private void OnResize(object sender, EventArgs e)
        {
            //Debug.WriteLine($"Form Resize: W: {this.Width} / H: {this.Height}");
        }

        
    }
}
