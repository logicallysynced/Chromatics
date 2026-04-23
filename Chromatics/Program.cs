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
using System.Threading;
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

        // Per-session mutex name. Stable GUID so future builds match. Held
        // for the lifetime of the process and released by the OS on exit
        // (whether clean shutdown, crash, or hard kill), which is why this
        // is more reliable than enumerating Process.GetProcesses(): a
        // lingering background thread can't keep us "alive" in the eyes of
        // the next launch, and elevated instances are visible to non-elevated
        // ones (and vice versa) without admin rights to enumerate.
        private const string SingleInstanceMutexName = "Chromatics-SingleInstance-{6E5F8A4D-2B4C-4F7E-9D1A-3E8B5C2F1A0D}";
        private static Mutex _singleInstanceMutex;

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

            // Only install our crash hooks when no debugger is attached. Under
            // a debugger we want the IDE's exception break / continue flow to
            // work exactly as if Sentry didn't exist — no MessageBox, no
            // Sentry capture, no feedback dialog.
            if (!Debugger.IsAttached)
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

            // Bootstrap Sentry BEFORE any code that can throw (settings load,
            // file migrations) so even those early failures are captured.
            // Real user settings (consent toggle, beta channel) are layered
            // on later via SentryService.ApplySettings once they've loaded.
            // No-op under a debugger.
            SentryService.Initialize();
            if (!Debugger.IsAttached)
                TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionHandler;

            // Relocate any user data files from the exe directory into
            // %AppData%\Chromatics first — the Velopack portable updater wipes
            // the install tree on every update, so exe-dir storage is unsafe.
            // Then run the legacy .chromatics3 → .chromatics4 migration inside
            // the now-canonical AppData location. Both are idempotent.
            FileOperationsHelper.MigrateExeDirDataToAppData();
            FileOperationsHelper.MigrateLegacyChromatics3Files();

            AppSettings.Startup();
            var appSettings = AppSettings.GetSettings();

            // Apply real consent / channel / language tags now that settings
            // are loaded. Captures from this point onward use these values.
            SentryService.ApplySettings(appSettings);

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
                if (Debugger.IsAttached)
                    throw;

                SentryService.CaptureCrash(ex);
                CrashFeedbackDialog.ShowBlocking(ex);
                ForceTerminate(1);
            }
            finally
            {
                SentryService.Shutdown();
            }

            // Normal exit path: Avalonia lifetime ended cleanly. Force-terminate
            // anyway because Sentry.Profiling's EventPipe session, the RGB.NET
            // update timer, the Sharlayan polling thread, and the Hue device
            // update trigger don't all exit when desktop.Shutdown() returns.
            // Without this, a "closed" Chromatics stays as a zombie process —
            // holding the single-instance mutex AND Sentry's envelope cache
            // lock, which manifests as both "Already running" prompts on the
            // next launch AND no events ever leaving subsequent Sentry inits.
            ForceTerminate(0);
        }

        // Hard process termination via OS TerminateProcess. SentryService.Shutdown
        // does a 3-second sync flush first so pending events leave the wire,
        // then Process.Kill is uninterruptible — no risk of being held hostage
        // by Sentry's AppDomain.ProcessExit handler or Sentry.Profiling's
        // EventPipe teardown, both of which can stall Environment.Exit
        // indefinitely on a slow network.
        private static void ForceTerminate(int exitCode)
        {
            try { SentryService.Shutdown(); } catch { }
            try { Process.GetCurrentProcess().Kill(); } catch { }
            // Should never reach here — Kill terminates synchronously.
            Environment.Exit(exitCode);
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
                ForceTerminate(1);
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
            // Try to claim the single-instance mutex. createdNew is true iff
            // we're the first process to ask for this name in the current
            // session; a stale mutex from a crashed previous instance is
            // released by the OS as soon as the previous process exits, so
            // there's no zombie state to clean up.
            _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
            if (createdNew) return true;

            // Mutex is held by another process. Two common cases where the
            // holder is about to die: (a) we're the elevated child of an
            // admin relaunch and the non-admin parent is in the process of
            // calling Process.Kill on itself, or (b) the previous launch
            // is finishing its shutdown teardown. Either way, wait briefly
            // for them to release before bothering the user with a prompt.
            try
            {
                if (_singleInstanceMutex.WaitOne(TimeSpan.FromSeconds(3)))
                    return true;
            }
            catch (AbandonedMutexException)
            {
                // Previous holder died without releasing — we now own it.
                return true;
            }

            int result = MessageBoxW(IntPtr.Zero,
                "Another instance of Chromatics is currently running, and only one can run at a time. Would you like to close the other instance and use this one?",
                "Already running", MB_YESNO | MB_ICONQUESTION);

            if (result != IDYES)
                return false;

            // User asked us to take over. Find any "Chromatics" process other
            // than us, kill it, and re-acquire the mutex once the OS has
            // released it. We only enumerate by name *after* the mutex check
            // confirms there really is another instance — the previous
            // implementation enumerated unconditionally and false-positived
            // on lingering background threads in our own process.
            var thisProcess = Process.GetCurrentProcess();
            var siblings = Process.GetProcesses()
                .Where(p => p.ProcessName == thisProcess.ProcessName)
                .Where(p => p.Id != thisProcess.Id)
                .ToList();

            foreach (var process in siblings)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch { /* may not have rights to kill an elevated sibling */ }
            }

            // Drop the previous handle and re-acquire — the OS only releases
            // the prior holder's mutex once that process has fully exited.
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out createdNew);
            return createdNew;
        }
    }
}
