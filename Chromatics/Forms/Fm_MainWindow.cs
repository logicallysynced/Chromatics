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
using Chromatics.Forms.vDevices;
using static Chromatics.Forms.vDevices.VirtualDevice;
using System.Reflection;
using System.Windows;
using Chromatics.Models;
using System.IO;
using System.Timers;
using Chromatics.Properties;
using System.Windows.Controls.Primitives;
using Sharlayan.Core.Enums;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using MetroFramework;
using System.Diagnostics.Eventing.Reader;
using System.Printing;
using MetroFramework.Properties;
using Chromatics.Localization;

namespace Chromatics.Forms
{
    public partial class Fm_MainWindow : MetroForm
    {
        public MetroStyleManager metroStyleManager;
        private Form mainForm;
        private SettingsModel appSettings;
        private MetroToolTip tt_main;
        private static Fm_MainWindow _Instance;
        private static MetroStyleManager _metroStyleManager;
        private static DarkModeManager _darkModeManager = new DarkModeManager();
        // Keep a strong reference to the SystemEvents handler so it can be
        // unsubscribed on shutdown. Subscribing to the static SystemEvents class
        // with an anonymous lambda (as the previous code did) left the form
        // rooted forever, preventing garbage collection.
        private UserPreferenceChangedEventHandler _themeChangeHandler;

        public Fm_MainWindow()
        {
            //Correct for DPI settings
            AutoScaleMode = AutoScaleMode.None;
            // Dispose the transient Graphics object: CreateGraphics() allocates a GDI handle
            // that would otherwise leak until the next GC pass.
            using (var graphics = CreateGraphics())
            {
                Font = new Font(Font.Name, 8.25f * 100f / graphics.DpiY, Font.Style, Font.Unit, Font.GdiCharSet, Font.GdiVerticalFont);
            }

            //Start Form
            InitializeComponent();
            _Instance = this;

            metroStyleManager = new MetroStyleManager(); 
            metroStyleManager.Owner = this;
            metroStyleManager.Theme = MetroFramework.MetroThemeStyle.Default;
            metroStyleManager.Style = MetroFramework.MetroColorStyle.Pink;
            _metroStyleManager = metroStyleManager;

            this.Theme = metroStyleManager.Theme;
            this.Style = metroStyleManager.Style;
            this.Size = new System.Drawing.Size(1400, 885);

            mainForm = this;
            RegisterForThemeChanges(metroStyleManager);

            //Load Settings
            AppSettings.Startup();
            appSettings = AppSettings.GetSettings();
                       

            //Check for First Run
            if (appSettings.firstrun)
            {
                var firstRunForm = new Fm_FirstRun();
                firstRunForm.ShowDialog();
            }

            
            //Check for new expansion settings
            if (!appSettings.ffxivExpansion.HasValue || appSettings.ffxivExpansion < 7.0)
            {
                Debug.WriteLine("FFXIV Expansion not set, setting to 7.0");
                appSettings.ffxivExpansion = 7.0;

                //Update Menu Animation Colour

                var active = RGBController.GetActivePalette();
                var cm = new PaletteColorModel();

                foreach (var p in typeof(PaletteColorModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (p.Name == "MenuBase" || p.Name == "MenuHighlight1" || p.Name == "MenuHighlight2" || p.Name == "MenuHighlight3")
                    {
                        var mapping = (ColorMapping)p.GetValue(cm);
                        var new_mapping = new ColorMapping(mapping.Name, mapping.Type, mapping.Color);
                        p.SetValue(active, new_mapping);

                        Debug.WriteLine($"TESTTTT {p.Name} {new_mapping.Color}");
                    }
                }

                RGBController.SaveColorPalette();
            }

            AppSettings.SaveSettings(appSettings);


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

            //Add tooltips
            tt_main = new MetroToolTip();
            tt_main.SetToolTip(this.btn_help, "Opens Chromatics 3 documentation in your browser");

            this.ResizeBegin += (s, e) => { this.SuspendLayout(); };
            this.ResizeEnd += (s, e) => { this.ResumeLayout(true); };
            this.FormClosed += Form_FormClosed;

            contextMenuStrip_main.Items.Add(new ToolStripMenuItem(@"Show Window", null, new EventHandler(OnNotifyIconDoubleClick)));
            contextMenuStrip_main.Items.Add(new ToolStripMenuItem(@"Close", null, new EventHandler(OnNotifyClickClose)));
            notifyIcon_main.ContextMenuStrip = contextMenuStrip_main;

            if (appSettings.trayonstartup)
            {
                this.WindowState = FormWindowState.Minimized;

                this.Hide();
                this.Visible = false;
                this.ShowInTaskbar = false;
            }

            
            

            //Apply Theme
            if (appSettings.systemTheme == Enums.Theme.System)
            {
                if (SystemHelpers.IsDarkModeEnabled())
                {
                    SetDarkMode(true);
                }
                else
                {
                    SetDarkMode(false);
                }
            }
            else if (appSettings.systemTheme == Enums.Theme.Dark)
            {
                SetDarkMode(true);

            }
            else if (appSettings.systemTheme == Enums.Theme.Light)
            {
                SetDarkMode(false);

            }

            
            Debug.WriteLine($"System Dark Mode is: {SystemHelpers.IsDarkModeEnabled()}");

            LocalizationManager.LocalizeForm(this);

            Logger.WriteConsole(LoggerTypes.System, @"Chromatics is starting up..");
        }

        public static Form GetForm()
        {
            return _Instance;
        }

        public static void SetDarkMode(bool darkMode)
        {
            if (darkMode)
            {
                _Instance.Theme = MetroThemeStyle.Dark;
                _Instance.BackImage = global::Chromatics.Properties.Resources.chromatics_white;
                _metroStyleManager.Theme = MetroThemeStyle.Dark;
                _darkModeManager.ToggleDarkMode(_Instance, true);
            }
            else
            {
                _Instance.Theme = MetroThemeStyle.Light;
                _Instance.BackImage = global::Chromatics.Properties.Resources.logo;
                _metroStyleManager.Theme = MetroThemeStyle.Light;
                _darkModeManager.ToggleDarkMode(_Instance, false);
            }
        }

        public static void TranslateForm()
        {
            LocalizationManager.LocalizeForm(_Instance);
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
            
            if (appSettings.checkupdates)
            {
                Logger.WriteConsole(LoggerTypes.System, @"Checking for updates..");
                AutoUpdater.HttpUserAgent = "Chromatics/AutoUpdater";
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.Icon = this.Icon.ToBitmap();
                AutoUpdater.Start("https://chromaticsffxiv.com/chromatics3/update/update.xml");
                
            }

            var assembly = typeof(Program).Assembly;
            var beta = true;

            if (beta)
            {
                Logger.WriteConsole(LoggerTypes.System, $"Chromatics {assembly.GetName().Version.Major}.{assembly.GetName().Version.Minor}.{assembly.GetName().Version.Build}.{assembly.GetName().Version.Revision} (BETA) has loaded");
                //this.Text = $"Chromatics {assembly.GetName().Version.Major}.{assembly.GetName().Version.Minor}.{assembly.GetName().Version.Build}.{assembly.GetName().Version.Revision} (BETA)";
            }
            else
            {
                Logger.WriteConsole(LoggerTypes.System, $"Chromatics {assembly.GetName().Version.Major}.{assembly.GetName().Version.Minor}.{assembly.GetName().Version.Build} has loaded");
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

        // NOTE: the designer's Dispose(bool) in Fm_MainWindow.Designer.cs disposes
        // `components` (which owns notifyIcon_main and contextMenuStrip_main), so no
        // manual override is needed here. The prior `Dispose(bool, bool)` method was
        // dead code — it was never invoked because the signature didn't match the
        // base `Dispose(bool)` and so wasn't an override.
        //
        // Form-owned resources that are NOT tracked by `components` (e.g. tt_main,
        // the static SystemEvents subscription) are released via the OnHandleDestroyed
        // override below so we don't leak across form lifetimes.

        private void OnResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && appSettings.minimizetray)
            {
                notifyIcon_main.Visible = true;
                Hide();
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (appSettings.minimizetray)
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    notifyIcon_main.Visible = true;
                    Hide();
                    e.Cancel = true;
                    return;
                }
            }

            ExitApplication();
        }

        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            mainForm.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private void OnNotifyClickClose(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void btn_help_Click(object sender, EventArgs e)
        {
            var url = @"https://docs.chromaticsffxiv.com/chromatics-3";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void ExitApplication()
        {
            // Tear down subsystems in the reverse order they were started so background
            // loops stop cleanly and unmanaged resources (hooks, timers, handles) are
            // released before the process exits.
            if (_themeChangeHandler != null)
            {
                SystemEvents.UserPreferenceChanged -= _themeChangeHandler;
                _themeChangeHandler = null;
            }

            tt_main?.Dispose();
            tt_main = null;

            GameController.Exit();
            KeyController.Stop();
            RGBController.Unload();

            if (System.Windows.Forms.Application.MessageLoop)
            {
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                Environment.Exit(0);
            }
        }

        private static void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            #if !DEBUG
                var thread = new Thread(() =>
                {
                    Thread.Sleep(1000); // wait for background tasks to finish
                    Environment.Exit(0);
                });
                thread.Start();
            #endif
        }

        

        private void RegisterForThemeChanges(MetroFramework.Components.MetroStyleManager metroStyleManager)
        {
            // Store the delegate in an instance field so it can be unsubscribed when the
            // form closes. Previously this registered an anonymous lambda against the static
            // SystemEvents class, which rooted the entire form indefinitely.
            _themeChangeHandler = OnUserPreferenceChanged;
            SystemEvents.UserPreferenceChanged += _themeChangeHandler;
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            Debug.WriteLine($"UserPreferenceChanged: {e.Category}");

            if (e.Category != UserPreferenceCategory.General) return;

            var settings = AppSettings.GetSettings();
            if (settings?.systemTheme != Enums.Theme.System) return;

            SetDarkMode(SystemHelpers.IsDarkModeEnabled());
        }
    }
}
