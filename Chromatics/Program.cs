using Avalonia;
using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Models;
using Chromatics.Views;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

            // Initialize Sentry as early as possible after settings are loaded
            // so it can capture exceptions during the rest of startup. The
            // service honours the user's enableCrashReports toggle internally
            // and respects beta-vs-stable for release-health bucketing.
            SentryService.Initialize(appSettings);
            TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;

            if (!Debugger.IsAttached)
                AdminElevationHelper.CheckAndElevateIfNeeded(appSettings);

            // First-run device-provider wizard runs after Avalonia boots — see
            // App.axaml.cs / FirstRunDialog. The expansion migration is
            // independent of the wizard and always runs on cold start.
            RunExpansionMigrationIfNeeded(appSettings);
            AppSettings.SaveSettings(appSettings);

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                // Avalonia startup or message-loop crash. AppDomain handler
                // doesn't always fire for these because the runtime sometimes
                // unwinds out of Main first. Report and show feedback dialog
                // (unless we're in a debug session — let it propagate then).
                if (!Debugger.IsAttached)
                {
                    SentryService.CaptureCrash(ex);
                    CrashFeedbackDialog.ShowBlocking(ex);
                }
                throw;
            }
            finally
            {
                SentryService.Shutdown();
            }
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
            var ex = e.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception (non-CLR object thrown)");

            // Under a debugger, let the IDE handle the exception so the dev can
            // inspect it. Otherwise capture, then show the user feedback form
            // so they can submit context alongside the crash report.
            if (Debugger.IsAttached)
            {
                MessageBoxW(IntPtr.Zero, "Unhandled exception caught: " + ex.Message, "Chromatics Error", MB_OK | MB_ICONERROR);
                return;
            }

            try
            {
                CrashFeedbackDialog.ShowBlocking(ex);
            }
            catch
            {
                // Last-resort fallback if Avalonia is too dead to spin up the dialog.
                try { SentryService.CaptureCrash(ex); } catch { }
                MessageBoxW(IntPtr.Zero, "Unhandled exception caught: " + ex.Message, "Chromatics Error", MB_OK | MB_ICONERROR);
            }
            finally
            {
                SentryService.Shutdown();
            }
        }

        private static void UnobservedTaskExceptionHandler(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // These don't terminate the process by default in modern .NET, so
            // we capture without showing the dialog. Marking observed prevents
            // the legacy "rethrow on finalize" behaviour from kicking in.
            try { SentryService.CaptureCrash(e.Exception); } catch { }
            e.SetObserved();
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
