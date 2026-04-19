using Avalonia;
using Chromatics.Core;
using Chromatics.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Velopack;
using WinFormsApp = System.Windows.Forms.Application;

namespace Chromatics
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Must be the very first call — Velopack intercepts lifecycle args
            // (--velopack-firstrun, --velopack-updated, etc.) and exits early
            // when acting on them. Nothing else may run before this.
            VelopackApp.Build().Run();

            if (!ThereCanOnlyBeOne())
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                return;
            }

            WinFormsApp.ThreadException += ThreadExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            WinFormsApp.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            WinFormsApp.EnableVisualStyles();
            WinFormsApp.SetCompatibleTextRenderingDefault(false);

            AppSettings.Startup();
            var appSettings = AppSettings.GetSettings();

            if (appSettings.firstrun)
            {
                // Pass 4 retired the WinForms Fm_FirstRun wizard. Device providers
                // now default to disabled on first run so Chromatics doesn't spin
                // up every SDK at once; users enable the ones they own from
                // Settings → Device Providers.
                appSettings.deviceRazerEnabled        = false;
                appSettings.deviceLogitechEnabled     = false;
                appSettings.deviceCorsairEnabled      = false;
                appSettings.deviceCoolermasterEnabled = false;
                appSettings.deviceSteelseriesEnabled  = false;
                appSettings.deviceAsusEnabled         = false;
                appSettings.deviceMsiEnabled          = false;
                appSettings.deviceWootingEnabled      = false;
                appSettings.deviceNovationEnabled     = false;
                appSettings.deviceOpenRGBEnabled      = false;
                appSettings.deviceHueEnabled          = false;
                appSettings.firstrun = false;
            }

            RunExpansionMigrationIfNeeded(appSettings);
            AppSettings.SaveSettings(appSettings);

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Carried over from the old Fm_MainWindow ctor: users upgrading from a build
        // prior to the 7.0 palette refresh need their menu-animation colours migrated
        // to the new PaletteColorModel defaults.
        private static void RunExpansionMigrationIfNeeded(SettingsModel appSettings)
        {
            if (appSettings.ffxivExpansion.HasValue && appSettings.ffxivExpansion >= 7.0)
                return;

            appSettings.ffxivExpansion = 7.0;

            var active = RGBController.GetActivePalette();
            var defaults = new PaletteColorModel();

            foreach (var p in typeof(PaletteColorModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.Name != "MenuBase" && p.Name != "MenuHighlight1" &&
                    p.Name != "MenuHighlight2" && p.Name != "MenuHighlight3") continue;

                var mapping = (ColorMapping)p.GetValue(defaults);
                var newMapping = new ColorMapping(mapping.Name, mapping.Type, mapping.Color);
                p.SetValue(active, newMapping);
            }

            RGBController.SaveColorPalette();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            MessageBox.Show("Unhandled exception caught: " + ex.Message);
        }

        private static void ThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("Unhandled exception caught: " + e.Exception.Message);
        }

        private static bool ThereCanOnlyBeOne()
        {
            var thisprocessname = Process.GetCurrentProcess().ProcessName;
            var otherProcesses = Process.GetProcesses()
                .Where(p => p.ProcessName == thisprocessname)
                .Where(p => p.Id != Process.GetCurrentProcess().Id);

            var enumerable = otherProcesses.ToList();
            if (enumerable.Any())
            {
                if (MessageBox.Show(@"Another instance of Chromatics is currently running, and only one can run at a time. Would you like to close the other instance and use this one?", @"Already running", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (var process in enumerable)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
