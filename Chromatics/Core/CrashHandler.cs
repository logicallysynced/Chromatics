using Chromatics.Views;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Chromatics.Core
{
    /// <summary>
    /// Single point of entry for "Chromatics is dying, do the dialog +
    /// telemetry + force-kill dance". Wired into every place an exception
    /// can get out of our control: AppDomain.UnhandledException,
    /// TaskScheduler.UnobservedTaskException, the Avalonia message-loop
    /// try/catch in Program.Main, and App.OnFrameworkInitializationCompleted.
    ///
    /// Re-entrant calls are short-circuited to a hard kill — without that, a
    /// secondary throw inside the dialog's own bootstrap (e.g. Avalonia
    /// service init failing) loops back into HandleCrash and never lets the
    /// process die.
    /// </summary>
    public static class CrashHandler
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        private static readonly System.Threading.Lock _gate = new();
        private static bool _alreadyHandling;

        /// <summary>
        /// Capture, prompt, kill. Returns only if a debugger is attached
        /// (so the IDE's exception flow continues to work normally).
        /// </summary>
        public static void HandleCrash(Exception ex)
        {
            if (ex == null) ex = new Exception("Unknown unhandled exception (non-CLR object thrown)");

            // Under a debugger, leave handling to the IDE — no Sentry, no
            // dialog, no kill. The break-on-unhandled flow takes over.
            if (Debugger.IsAttached) return;

            // Re-entrancy guard. A throw inside CrashFeedbackDialog/CrashApp
            // bootstrap would loop back here; on the second hit we just kill.
            lock (_gate)
            {
                if (_alreadyHandling)
                {
                    try { Process.GetCurrentProcess().Kill(); } catch { }
                    return;
                }
                _alreadyHandling = true;
            }

            try
            {
                CrashFeedbackDialog.ShowBlocking(ex);
            }
            catch
            {
                // Last-resort native MessageBox so the user sees *something*
                // even if every layer of our themed dialog has collapsed.
                try
                {
                    MessageBoxW(IntPtr.Zero,
                        $"Chromatics encountered an unexpected error and must close:\n\n{ex.GetType().Name}: {ex.Message}",
                        "Chromatics Error",
                        0x10);
                }
                catch { }
            }
            finally
            {
                // Process.Kill — Sentry's already been flushed inside the
                // dialog flow (CaptureCrash + SubmitFeedback both block on
                // Flush). Use the OS terminate so background threads
                // (Sentry.Profiling EventPipe, RGB.NET timer, Sharlayan
                // polling, Hue update trigger) can't keep the process alive
                // as a zombie.
                try { SentryService.Shutdown(); } catch { }
                try { Process.GetCurrentProcess().Kill(); } catch { }
            }
        }
    }
}
