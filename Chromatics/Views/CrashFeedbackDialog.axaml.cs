using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Chromatics.Core;
using Sentry;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Chromatics.Views
{
    /// <summary>
    /// Crash feedback form shown after an unhandled exception (and only when a
    /// debugger is NOT attached — under a debugger we let the exception
    /// propagate so the developer can see it in the IDE).
    ///
    /// The dialog is intentionally self-contained: it can be shown from a
    /// dying app where Avalonia's normal lifecycle has already collapsed,
    /// which is why everything is wired through plain code-behind without an
    /// MVVM viewmodel.
    /// </summary>
    public partial class CrashFeedbackDialog : Window
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        private SentryId _eventId = SentryId.Empty;

        public CrashFeedbackDialog()
        {
            InitializeComponent();
        }

        public CrashFeedbackDialog(Exception ex) : this()
        {
            // Capture the exception immediately so that even if the user closes
            // the dialog with the X (no feedback), Sentry still gets the crash.
            // Feedback, if provided, is associated with this event id afterward.
            _eventId = SentryService.CaptureCrash(ex);

            ErrorSummary.Text = $"{ex.GetType().Name}: {ex.Message}";
        }

        /// <summary>
        /// Shows the crash dialog and blocks the calling thread until the
        /// user dismisses it. Safe to call from any thread, including from
        /// AppDomain.UnhandledException with no Avalonia loop running yet.
        /// Falls back to a Win32 MessageBox if Avalonia is not initialized
        /// (early-startup crash before BuildAvaloniaApp ran), so the process
        /// never hangs waiting on a dispatcher that doesn't exist.
        /// </summary>
        public static void ShowBlocking(Exception ex)
        {
            // Avalonia not booted yet — Dispatcher.UIThread.Post would queue
            // an action no one will ever pump, leaving the calling thread
            // blocked on done.Wait() and the process stuck in task manager
            // until the timeout (which is what was happening on
            // settings-load failures).
            if (Application.Current?.ApplicationLifetime == null)
            {
                ShowFallback(ex);
                return;
            }

            try
            {
                var done = new ManualResetEventSlim();

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var dlg = new CrashFeedbackDialog(ex);
                        dlg.Closed += (_, _) => done.Set();
                        dlg.Show();
                    }
                    catch
                    {
                        done.Set();
                    }
                });

                // Wait up to 2 minutes for the user to act, then bail so we
                // don't hang the process indefinitely if they walk away.
                if (!done.Wait(TimeSpan.FromMinutes(2)))
                    ShowFallback(ex);
            }
            catch
            {
                // The dispatcher itself may have torn down — fall back to
                // the message-box path so the user at least sees something.
                ShowFallback(ex);
            }
        }

        // Capture the crash, then surface a Win32 MessageBox so the user
        // knows the report was sent. Used when no Avalonia UI thread is
        // available (early startup crashes, or post-shutdown crashes).
        private static void ShowFallback(Exception ex)
        {
            try { SentryService.CaptureCrash(ex); } catch { }
            try { SentryService.Shutdown(); } catch { }
            try
            {
                MessageBoxW(IntPtr.Zero,
                    $"Chromatics encountered an unexpected error and could not continue:\n\n{ex.GetType().Name}: {ex.Message}\n\nA crash report has been sent automatically.",
                    "Chromatics Error",
                    0x10 /* MB_ICONERROR */);
            }
            catch { }
        }

        private void OnSend(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                // Comments only — name and email fields were removed so the
                // form cannot collect personally identifying information.
                SentryService.SubmitFeedback(_eventId, CommentsBox.Text ?? string.Empty);
            }
            catch { }
            Close();
        }

        private void OnDontSend(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
