using Avalonia.Controls;
using Avalonia.Threading;
using Chromatics.Core;
using Sentry;
using System;
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
        /// Blocks the calling thread on a fresh STA dispatcher so the dialog
        /// can run after the main Avalonia message loop has died (e.g. from a
        /// startup crash or an UnhandledException). Returns when the user
        /// closes the dialog. Safe to call from any thread.
        /// </summary>
        public static void ShowBlocking(Exception ex)
        {
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
                done.Wait(TimeSpan.FromMinutes(2));
            }
            catch
            {
                // The dispatcher itself may have torn down — fall back to
                // capturing the exception silently and exiting.
                try { SentryService.CaptureCrash(ex); } catch { }
            }
        }

        private void OnSend(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                SentryService.SubmitFeedback(
                    _eventId,
                    CommentsBox.Text ?? string.Empty,
                    EmailBox.Text ?? string.Empty,
                    NameBox.Text ?? string.Empty);
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
