using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
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

        public CrashFeedbackDialog(Exception ex, SentryId preCapturedEventId = default) : this()
        {
            // If the caller already captured the exception (early-startup
            // crash path), reuse that event id to avoid double-billing the
            // same crash. Otherwise capture it now so that even if the user
            // dismisses the dialog with no feedback, Sentry still gets the
            // crash. Feedback, when provided, is associated with this id.
            _eventId = preCapturedEventId != SentryId.Empty
                ? preCapturedEventId
                : SentryService.CaptureCrash(ex);

            ErrorSummary.Text = $"{ex.GetType().Name}: {ex.Message}";

            // Show the Sentry event id so the user can paste it into a
            // support thread / GitHub issue. SentryId is a 32-char hex GUID
            // without dashes — present it in a readable form.
            ReferenceIdText.Text = _eventId == SentryId.Empty
                ? "(unavailable — Sentry was not initialised)"
                : _eventId.ToString();
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

            // If we're already on the UI thread (e.g. App.OnFrameworkInitializationCompleted
            // catching a MainWindow ctor failure), Dispatcher.UIThread.Post + .Wait()
            // is a guaranteed deadlock: the post can't be processed because we're
            // blocking the very thread that would process it. Manifests as "zombie
            // Chromatics.exe in task manager, no dialog ever appears". Show the
            // dialog inline and run a nested dispatcher message loop until the
            // dialog closes.
            if (Dispatcher.UIThread.CheckAccess())
            {
                try
                {
                    var dlg = new CrashFeedbackDialog(ex);
                    var cts = new CancellationTokenSource();
                    dlg.Closed += (_, _) => cts.Cancel();
                    dlg.Show();
                    Dispatcher.UIThread.MainLoop(cts.Token);
                }
                catch
                {
                    ShowFallback(ex);
                }
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

        // Bootstrap a minimal Avalonia lifetime (CrashApp) just to show the
        // themed crash dialog when the main app's Avalonia hasn't started.
        // StartWithClassicDesktopLifetime blocks until the dialog window is
        // closed (CrashApp uses ShutdownMode.OnMainWindowClose). If that
        // bootstrap itself fails (rare), fall back to the unthemed
        // Win32 MessageBox so the user at least sees something.
        private static void ShowFallback(Exception ex)
        {
            // Capture once up front and pass the event id through to the
            // dialog so it doesn't capture again. Without this, both this
            // path and the dialog's constructor would fire CaptureCrash on
            // the same exception, producing two events in the same Issue —
            // and any feedback would be associated with the second event,
            // making it look like comments "didn't arrive" because users
            // were viewing the first one in the Issues list.
            SentryId preCapturedEventId = SentryId.Empty;
            try { preCapturedEventId = SentryService.CaptureCrash(ex); } catch { }

            bool crashAppShown = false;
            try
            {
                CrashApp.PendingException = ex;
                CrashApp.PendingEventId = preCapturedEventId;
                AppBuilder.Configure<CrashApp>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .StartWithClassicDesktopLifetime(Array.Empty<string>());
                crashAppShown = true;
            }
            catch (Exception bootstrapEx)
            {
                // Verbose-log the actual exception so a user reporting "the
                // crash dialog is the basic Windows one, not the themed one"
                // produces enough context to diagnose without running a
                // debugger. CrashApp bootstrap failures (XAML resource not
                // found, Avalonia init failure, etc.) silently fell back to
                // MessageBox before, hiding the root cause.
                Logger.WriteVerbose($"[CrashDialog] CrashApp bootstrap failed: {bootstrapEx.GetType().Name}: {bootstrapEx.Message}");
                Logger.WriteVerbose($"[CrashDialog] Stack: {bootstrapEx.StackTrace}");
            }

            if (!crashAppShown)
            {
                try
                {
                    MessageBoxW(IntPtr.Zero,
                        $"Chromatics encountered an unexpected error and could not continue:\n\n{ex.GetType().Name}: {ex.Message}\n\nA crash report has been sent automatically.",
                        "Chromatics Error",
                        0x10 /* MB_ICONERROR */);
                }
                catch { }
            }

            try { SentryService.Shutdown(); } catch { }
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

        private async void OnCopyReference(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_eventId == SentryId.Empty) return;
            try
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(_eventId.ToString());
            }
            catch
            {
                // Copy is convenience-only — failures are silent (the user
                // can still triple-click the SelectableTextBlock to copy).
            }
        }
    }
}
