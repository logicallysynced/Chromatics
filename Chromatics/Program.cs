using Avalonia;
using Chromatics.Core;
using Chromatics.Enums;
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

        // Sentinel argv flag set by RestartForPackageIdentity. Present on the
        // second-pass invocation so we don't re-enter the restart logic and
        // loop forever if identity still fails to bind.
        private const string PostRegisterRestartArg = "--chromatics-post-register-restart";

        [STAThread]
        static void Main(string[] args)
        {
            // Must be the very first call — Velopack intercepts lifecycle args
            // (--velopack-firstrun, --velopack-updated, etc.) and exits early
            // when acting on them. Also registers the global VelopackLocator
            // that UpdateService depends on, so this must run even under a
            // debugger. Lifecycle args are never passed during debug sessions,
            // so Run() just registers the locator and returns cleanly.
            //
            // OnBeforeUninstallFastCallback removes the Dynamic Lighting sparse
            // package registration before Velopack kills the process, so the
            // app stops appearing in Settings → Personalization → Dynamic
            // Lighting → Background light control after uninstall.
            VelopackApp.Build()
                .OnBeforeUninstallFastCallback(_ =>
                {
                    if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                        SparsePackageRegistrar.Deregister();
                })
                .Run();

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

            // Initialize Sentry BEFORE settings load and admin elevation so
            // early-startup crashes (malformed settings.chromatics4, missing
            // dependencies, etc.) get captured. The non-admin parent and the
            // elevated child both init their own SDK instance; that's fine
            // because each process has its own state and AdminElevationHelper.
            // RelaunchAsAdmin calls SentryService.Shutdown before Process.Kill,
            // so the parent's pending events flush before it dies.
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
            // are loaded. Captures from this point forward use these values.
            SentryService.ApplySettings(appSettings);

            if (!Debugger.IsAttached)
                AdminElevationHelper.CheckAndElevateIfNeeded(appSettings);

            // First-run device-provider wizard runs after Avalonia boots — see
            // App.axaml.cs / FirstRunDialog. The expansion migration is
            // independent of the wizard and always runs on cold start.
            RunExpansionMigrationIfNeeded(appSettings);
            AppSettings.SaveSettings(appSettings);

            // Re-register the Dynamic Lighting sparse package on startup if the
            // user already had the DL provider enabled. Catches the upgrade
            // path: a new Chromatics version ships a new sparse-package version,
            // so the OS-side registration needs to be refreshed to match.
            // Initial registration on first enable is driven from the Settings
            // toggle (see SettingsViewModel); this is just the keep-in-sync
            // pass on subsequent launches.
            if (appSettings.deviceDynamicLightingEnabled && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            {
                SparsePackageRegistrar.EnsureRegistered();

                // The OS loader binds fusion-manifest identity at CreateProcess
                // time — meaning the process that performs the FIRST registration
                // is forever without identity, because it started before the
                // package existed in the registry. Relaunch once (sentinel arg
                // prevents loops) so the new process picks up the binding and
                // background Dynamic Lighting works without the user having to
                // close-and-reopen Chromatics by hand.
                if (!args.Contains(PostRegisterRestartArg) && !SparsePackageRegistrar.HasPackageIdentity())
                {
                    Logger.WriteVerbose("[SparsePackage] Process started before package was registered; relaunching once to bind identity");
                    RestartForPackageIdentity(args);
                    return;
                }

                SparsePackageRegistrar.LogPackageIdentity();
            }

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                // Avalonia startup or message-loop crash. AppDomain handler
                // doesn't always fire for these because the runtime sometimes
                // unwinds out of Main first. Route through CrashHandler so
                // the dialog + Sentry + force-kill flow runs regardless of
                // which entry point caught the exception.
                if (Debugger.IsAttached)
                    throw;

                CrashHandler.HandleCrash(ex);
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

        // Relaunches the current Chromatics.exe with a sentinel argv flag so the
        // new process picks up package identity bound by the sparse-package
        // registration that just completed. The current process started before
        // the package existed, so its identity is forever absent — only a fresh
        // CreateProcess can pick up the new binding. Releases the single-instance
        // mutex first so the new process doesn't have to wait through the
        // ThereCanOnlyBeOne 3-second grace window before claiming it.
        private static void RestartForPackageIdentity(string[] originalArgs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = false,
            };
            foreach (var arg in originalArgs) psi.ArgumentList.Add(arg);
            psi.ArgumentList.Add(PostRegisterRestartArg);

            try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
            try { _singleInstanceMutex?.Dispose();   } catch { }
            _singleInstanceMutex = null;

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[SparsePackage] Relaunch failed: {ex.GetType().Name} — {ex.Message}. Continuing without identity (foreground DL only).");
                // Re-acquire the mutex so this surviving process still passes
                // single-instance checks downstream. Best-effort; if it fails
                // we soldier on rather than killing the surviving instance.
                try { _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out _); } catch { }
                return;
            }

            try { SentryService.Shutdown(); } catch { }
            Environment.Exit(0);
        }

        // Hard process termination via OS TerminateProcess. SentryService.Shutdown
        // does a 3-second sync flush first so pending events leave the wire,
        // then Process.Kill is uninterruptible — no risk of being held hostage
        // by Sentry's AppDomain.ProcessExit handler or Sentry.Profiling's
        // EventPipe teardown, both of which can stall Environment.Exit
        // indefinitely on a slow network.
        private static void ForceTerminate(int exitCode)
        {
            // Materialise any clipboard data the user has copied during the
            // session so it survives this process dying. Avalonia's clipboard
            // uses OLE delayed rendering on Windows, which means a Ctrl+C
            // inside the Console TextBox (or any Avalonia text control) only
            // leaves a "ask Chromatics for the bytes" pointer in Windows'
            // clipboard chain; once Process.Kill fires the pointer is dead
            // and the user's clipboard goes empty. OleFlushClipboard walks
            // the pending OLE formats and serialises them into the system
            // clipboard before we tear the process down.
            try { Helpers.ClipboardHelper.FlushOleClipboard(); } catch { }
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
            CrashHandler.HandleCrash(ex);
        }

        private static void UnobservedTaskExceptionHandler(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // .NET 5+ no longer terminates the process for unobserved task
            // exceptions, but for Chromatics these almost always represent
            // a fatal failure in a core background task (RGB.NET update tick,
            // Sharlayan polling, Hue update queue, etc.) that the user can't
            // recover from. Treat them as fatal: capture, show the dialog,
            // force-kill — same as any other unhandled exception. Without
            // this, the user previously saw "no dialog, but a zombie
            // Chromatics.exe in task manager" because Sentry's automatic
            // UnobservedTaskExceptionIntegration captured the event but our
            // code did nothing to surface or terminate.
            e.SetObserved();

            // Exception to that rule: socket aborts during shutdown (Hue
            // entertainment client, OpenRGB TCP client, AutoUpdater HTTPS)
            // surface as unobserved SocketException with WSAEINTR /
            // WSAECONNABORTED / OperationAborted. They're cosmetic — the
            // dependent task is already on its way out. Crashing the whole
            // app for them spams users with the crash dialog.
            if (IsBenignBackgroundException(e.Exception))
                return;

            CrashHandler.HandleCrash(e.Exception);
        }

        private static bool IsBenignBackgroundException(Exception ex)
        {
            if (ex is null) return false;
            if (ex is AggregateException agg)
            {
                var flat = agg.Flatten();
                return flat.InnerExceptions.Count > 0 && flat.InnerExceptions.All(IsBenignBackgroundException);
            }
            return ex switch
            {
                System.Net.Sockets.SocketException se => IsBenignSocketError(se.SocketErrorCode),
                System.IO.IOException io => io.InnerException is System.Net.Sockets.SocketException ise && IsBenignSocketError(ise.SocketErrorCode),
                ObjectDisposedException => true,
                OperationCanceledException => true,
                _ => false,
            };
        }

        private static bool IsBenignSocketError(System.Net.Sockets.SocketError code) => code switch
        {
            System.Net.Sockets.SocketError.OperationAborted => true,
            System.Net.Sockets.SocketError.ConnectionReset => true,
            System.Net.Sockets.SocketError.ConnectionAborted => true,
            System.Net.Sockets.SocketError.Interrupted => true,
            System.Net.Sockets.SocketError.Shutdown => true,
            System.Net.Sockets.SocketError.NetworkReset => true,
            _ => false,
        };

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
