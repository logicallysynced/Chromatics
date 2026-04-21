using Avalonia;
using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Velopack;

namespace Chromatics
{
    static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        private const uint MB_OK           = 0x0;
        private const uint MB_YESNO        = 0x4;
        private const uint MB_ICONERROR    = 0x10;
        private const uint MB_ICONQUESTION = 0x20;
        private const int  IDYES           = 6;

        [STAThread]
        static void Main(string[] args)
        {
            // Must be the very first call — Velopack intercepts lifecycle args
            // (--velopack-firstrun, --velopack-updated, etc.) and exits early
            // when acting on them. Also registers the global VelopackLocator
            // that UpdateService depends on, so this must run even under a
            // debugger. Lifecycle args are never passed during debug sessions,
            // so Run() just registers the locator and returns cleanly.
            VelopackApp.Build().Run();

            if (!ThereCanOnlyBeOne())
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                return;
            }

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            // The Velopack installer declares this prerequisite via --framework and
            // installs it before first run, so Setup.exe users never see this warning.
            // Users launching the ZIP-portable build directly skip that path — this
            // probe catches the gap so devices don't fail later with a cryptic DLL error.
            RuntimePrerequisiteCheck.WarnIfMissing();

            // Point the verbose log at the AppData directory before anything
            // else logs — this ensures startup messages (including migrations
            // below) are captured even if they never reach the Console tab.
            Logger.SetLogDirectory(FileOperationsHelper.GetConfigDirectory());

            // Relocate any user data files from the exe directory into
            // %AppData%\Chromatics first — the Velopack portable updater wipes
            // the install tree on every update, so exe-dir storage is unsafe.
            // Then run the legacy .chromatics3 → .chromatics4 migration inside
            // the now-canonical AppData location. Both are idempotent.
            FileOperationsHelper.MigrateExeDirDataToAppData();
            FileOperationsHelper.MigrateLegacyChromatics3Files();

            AppSettings.Startup();
            var appSettings = AppSettings.GetSettings();

            if (!Debugger.IsAttached)
                AdminElevationHelper.CheckAndElevateIfNeeded(appSettings);

            // First-run device-provider wizard runs after Avalonia boots — see
            // App.axaml.cs / FirstRunDialog. The expansion migration is
            // independent of the wizard and always runs on cold start.
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
            var ex = (Exception)e.ExceptionObject;
            MessageBoxW(IntPtr.Zero, "Unhandled exception caught: " + ex.Message, "Chromatics Error", MB_OK | MB_ICONERROR);
        }

        private static bool ThereCanOnlyBeOne()
        {
            var thisprocessname = Process.GetCurrentProcess().ProcessName;
            var otherProcesses = Process.GetProcesses()
                .Where(p => p.ProcessName == thisprocessname)
                .Where(p => p.Id != Process.GetCurrentProcess().Id)
                .ToList();

            if (!otherProcesses.Any()) return true;

            int result = MessageBoxW(IntPtr.Zero,
                "Another instance of Chromatics is currently running, and only one can run at a time. Would you like to close the other instance and use this one?",
                "Already running", MB_YESNO | MB_ICONQUESTION);

            if (result == IDYES)
            {
                foreach (var process in otherProcesses)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                return true;
            }

            return false;
        }
    }
}
