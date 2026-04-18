using Avalonia;
using Chromatics.Core;
using Chromatics.Forms;
using Chromatics.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WinFormsApp = System.Windows.Forms.Application;

namespace Chromatics
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (!ThereCanOnlyBeOne())
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                return;
            }

            // WinForms still gets initialized: AutoUpdaterDotNET opens its prompt as
            // a WinForms dialog, so visual styles need to be set even though Avalonia
            // owns the main window.
            WinFormsApp.ThreadException += ThreadExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            WinFormsApp.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            WinFormsApp.EnableVisualStyles();
            WinFormsApp.SetCompatibleTextRenderingDefault(false);

            // Load settings and run the first-run wizard / expansion migration BEFORE
            // Avalonia starts. Fm_FirstRun is still a WinForms modal dialog — calling
            // it from inside Avalonia's dispatcher would freeze the main window during
            // first launch.
            AppSettings.Startup();
            var appSettings = AppSettings.GetSettings();

            if (appSettings.firstrun)
            {
                using var firstRunForm = new Fm_FirstRun();
                firstRunForm.ShowDialog();
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
