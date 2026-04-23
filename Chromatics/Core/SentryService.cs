using Chromatics.Models;
using Sentry;
using Sentry.Extensibility;
using Sentry.Profiling;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Chromatics.Core
{
    /// <summary>
    /// Sentry SDK lifecycle. Initialize once at process start (Program.Main),
    /// then optionally enable/disable at runtime as the user toggles the
    /// "Send crash reports" setting. Disabling pauses the hub entirely so no
    /// network traffic is generated regardless of which other code paths
    /// (Logger forwarding, unhandled-exception handlers) call into it.
    ///
    /// Chromatics is open source, so the DSN below is publicly visible. This
    /// is by design — Sentry DSNs are write-only ingestion keys, not secrets,
    /// and exposing them is the documented OSS pattern.
    /// </summary>
    public static class SentryService
    {
        // DSN: public ingestion key for the Chromatics project (ID 4511267934437376,
        // slug "chromatics"). Sentry DSNs are not secrets — they are write-only
        // event-ingest tokens that must be embedded in client builds. Exposing
        // them in OSS source is the documented Sentry pattern.
        private const string Dsn = "https://281bff0891576e4f10d8722ce3bf6837@o4511267928735744.ingest.us.sentry.io/4511267934437376";

        private static IDisposable _sdkHandle;
        private static bool _initialized;

        // Mutable so ApplySettings can swap it in after AppSettings.Startup
        // succeeds. The BeforeSend / BeforeBreadcrumb callbacks below close
        // over a getter that returns the current value, so consent changes
        // take effect immediately on the next captured event.
        private static SettingsModel _settings = new SettingsModel();

        public static bool IsActive => _initialized && SentrySdk.IsEnabled;

        /// <summary>
        /// Bootstraps Sentry as early as possible — BEFORE AppSettings.Startup
        /// runs — so that a crash in settings deserialization itself can still
        /// be captured. Uses defaults from a fresh SettingsModel for the
        /// init-locked options (environment, release). Real user settings are
        /// layered on after AppSettings loads via ApplySettings().
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            if (string.IsNullOrWhiteSpace(Dsn))
            {
                Logger.WriteVerbose("[Sentry] Initialize skipped: DSN not configured");
                return;
            }

            // Under a debugger, do nothing. The Sentry SDK installs its own
            // AppDomain.UnhandledException + TaskScheduler.UnobservedTaskException
            // integrations that would intercept exceptions before the IDE's
            // normal break-on-unhandled flow. Skipping init keeps the IDE
            // experience identical to a clean (non-Sentry) debug session.
            if (Debugger.IsAttached)
            {
                Logger.WriteVerbose("[Sentry] Initialize skipped: debugger attached");
                return;
            }

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

            _sdkHandle = SentrySdk.Init(o =>
            {
                o.Dsn = Dsn;

                // Release health: ties events, sessions, and crash-free rates to
                // a specific build. Format "<slug>@<version>" is the Sentry
                // convention and lets the dashboard group beta vs stable.
                o.Release = $"chromatics@{version}";

                // Environment can only be set at init time. Default to
                // production here; ApplySettings switches the per-event
                // "channel" tag to "beta" if the user is on the beta channel,
                // which is what the dashboard groups by once we filter on it.
                o.Environment = "production";

                // Auto session tracking is what powers the Release Health UI
                // (sessions, crash-free users, adoption). On by default in v5.x
                // but set explicitly so future SDK changes don't disable it.
                o.AutoSessionTracking = true;

                // Tracing: 20% sample rate keeps quota usage bounded on the
                // free tier while still surfacing slow update loops.
                o.TracesSampleRate = 0.2;

                // Profiling: piggybacks on tracing sample rate. Requires the
                // Sentry.Profiling integration package, which we've added.
                o.ProfilesSampleRate = 1.0;
                o.AddIntegration(new ProfilingIntegration());

                // Strip personally identifiable information. We're an OSS app
                // and the user is shown a privacy notice in the README; keep
                // the data we send minimal.
                o.SendDefaultPii = false;
                o.AttachStacktrace = true;

                // Don't auto-capture WriteLine output — it's noisy and the
                // user-visible Console tab is already replayed via Logger.
                o.MaxBreadcrumbs = 100;

                // Route Sentry SDK's internal diagnostics to verbose.log so
                // any send/queue/flush failure surfaces something searchable.
                // Without this, transport errors (DNS failure, TLS reject,
                // 4xx/5xx responses) are silently swallowed and the user
                // just observes "no events arrived".
                o.Debug = true;
                o.DiagnosticLevel = SentryLevel.Debug;
                o.DiagnosticLogger = new SentryToVerboseLogLogger();

                // Enable the Logs product (separate from Issues). Once on,
                // SentrySdk.Logger.LogInfo/LogWarning/LogError accept
                // structured log entries that show up in the dashboard's
                // Logs tab. Logger.cs forwards every WriteConsole line here.
                o.EnableLogs = true;

                // Honour the consent toggle by reading from the live _settings
                // reference, which ApplySettings swaps in once AppSettings has
                // loaded. Defaults are enableCrashReports=true, so early-startup
                // crashes are reported until the user has explicitly opted out.
                // Capture _settings to a local — it can be transiently null if
                // ApplySettings was passed null (e.g. AppSettings.Startup failed
                // to parse settings.chromatics4) and we'd NRE inside the SDK's
                // hot path otherwise.
                o.SetBeforeSend((evt, _) =>
                {
                    var s = _settings;
                    if (s == null || s.enableCrashReports) return evt;
                    Logger.WriteVerbose($"[Sentry] BeforeSend dropped event {evt.EventId} — consent disabled");
                    return null;
                });
                o.SetBeforeBreadcrumb((b, _) =>
                {
                    var s = _settings;
                    return (s == null || s.enableCrashReports) ? b : null;
                });
            });

            _initialized = true;
            Logger.WriteVerbose($"[Sentry] Initialize complete: enabled={SentrySdk.IsEnabled}, defaultConsent={_settings?.enableCrashReports ?? true}");
        }

        /// <summary>
        /// Layers real user settings onto the bootstrap init: applies consent,
        /// adds tags (beta/stable channel, language, admin elevation), and
        /// starts/ends the Release Health session based on the current consent.
        /// Safe to call multiple times if settings change at runtime.
        /// </summary>
        public static void ApplySettings(SettingsModel settings)
        {
            if (!_initialized)
            {
                Logger.WriteVerbose("[Sentry] ApplySettings skipped: SDK not initialized");
                return;
            }

            // Defensive: AppSettings.Startup can hand us null when settings
            // deserialization fails (malformed settings.chromatics4). The
            // previous version blindly assigned _settings = null and then
            // NRE'd on the next field access, which cascaded into BeforeSend
            // and CaptureCrash failures and broke the themed crash dialog.
            if (settings == null)
            {
                Logger.WriteVerbose("[Sentry] ApplySettings skipped: settings is null — keeping defaults");
                return;
            }

            _settings = settings;
            Logger.WriteVerbose($"[Sentry] ApplySettings: consent={settings.enableCrashReports}, channel={(settings.betaChannel ? "beta" : "stable")}");

            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("channel", settings.betaChannel ? "beta" : "stable");
                scope.SetTag("language", settings.systemLanguage.ToString());
                scope.SetTag("admin", settings.alwaysRunAsAdmin ? "true" : "false");
                scope.SetExtra("processId", Environment.ProcessId);
            });

            ApplyConsent(settings.enableCrashReports);
        }

        /// <summary>
        /// Reflects the user's "Send crash reports" toggle. Pausing closes the
        /// transport and stops session/crash tracking; resuming restarts a
        /// fresh session so release-health stats stay coherent.
        /// </summary>
        public static void ApplyConsent(bool enabled)
        {
            if (!_initialized) return;

            if (enabled)
            {
                SentrySdk.StartSession();
            }
            else
            {
                SentrySdk.EndSession();
            }
        }

        /// <summary>
        /// Captures the exception, force-flushes the SDK so the event leaves
        /// the process before the caller continues, and returns the Sentry
        /// event id so the caller can associate user feedback with the crash.
        ///
        /// The synchronous flush is critical: CaptureException is normally
        /// queued for an async background worker. In the early-startup-crash
        /// path we then bootstrap a fresh Avalonia lifetime (CrashApp), and
        /// the post-dialog code path calls Environment.Exit(1) — both of
        /// which can drop the queued event before the worker has a chance to
        /// transmit it. Flushing inline costs a few seconds in the crash
        /// flow but makes "the dialog appeared but no event arrived" stop
        /// being a possible outcome.
        /// </summary>
        public static SentryId CaptureCrash(Exception ex)
        {
            if (!IsActive)
            {
                Logger.WriteVerbose($"[Sentry] CaptureCrash skipped: IsActive=false (initialized={_initialized}, sdkEnabled={SentrySdk.IsEnabled})");
                return SentryId.Empty;
            }

            ex.Data["Chromatics.HandledByCrashDialog"] = true;
            var id = SentrySdk.CaptureException(ex);
            // Defensive: _settings can be null transiently if ApplySettings
            // has not yet run (early-startup crashes); use ?? so the log line
            // doesn't NRE — which would then propagate up out of CaptureCrash
            // and break the CrashApp dialog bootstrap (caller catches it
            // and falls back to the unthemed Win32 MessageBox).
            var consent = _settings?.enableCrashReports ?? true;
            Logger.WriteVerbose($"[Sentry] CaptureCrash captured id={id}, consent={consent}, type={ex.GetType().Name}");

            try { SentrySdk.Flush(TimeSpan.FromSeconds(5)); } catch (Exception flushEx) { Logger.WriteVerbose($"[Sentry] CaptureCrash flush threw: {flushEx.Message}"); }
            Logger.WriteVerbose($"[Sentry] CaptureCrash flush complete for id={id}");

            return id;
        }

        /// <summary>
        /// Submits anonymous comments attached to a previously captured event
        /// id. The dialog deliberately does not collect name/email — comments
        /// are the only user-supplied field on the wire.
        ///
        /// We send the comment through THREE channels so it's visible no
        /// matter where in the dashboard the maintainer is looking:
        ///   1. SentrySdk.CaptureFeedback — User Feedback product (separate
        ///      sidebar entry; shows up on the event detail page too).
        ///   2. SentrySdk.Logger.LogInfo with the associated event id as a
        ///      structured property — appears in the Logs tab, filterable
        ///      by event id.
        ///   3. SentrySdk.CaptureMessage with the event id as a tag — shows
        ///      up as its own entry in the Issues list, linking back to the
        ///      crash. This is the most visible place for free-tier users
        ///      who haven't customised their dashboard.
        /// </summary>
        public static void SubmitFeedback(SentryId eventId, string comments)
        {
            if (!IsActive || eventId == SentryId.Empty) return;
            if (_settings != null && !_settings.enableCrashReports) return;
            if (string.IsNullOrWhiteSpace(comments)) return;

            try
            {
                var feedback = new SentryFeedback(
                    message: comments,
                    contactEmail: null,
                    name: null,
                    associatedEventId: eventId);
                SentrySdk.CaptureFeedback(feedback);
            }
            catch { }

            try { SentrySdk.Logger.LogInfo("User feedback for {EventId}: {Comment}", eventId.ToString(), comments); } catch { }

            try
            {
                SentrySdk.CaptureMessage(
                    $"User feedback: {comments}",
                    scope =>
                    {
                        scope.SetTag("associated_event_id", eventId.ToString());
                        scope.SetTag("kind", "user_feedback");
                        scope.SetExtra("crash_event_id", eventId.ToString());
                    },
                    SentryLevel.Info);
            }
            catch { }

            // Block long enough for all three to leave the process. The
            // dialog is about to close and the process about to exit, so a
            // 5s flush is the difference between feedback arriving or being
            // dropped on shutdown.
            try { SentrySdk.Flush(TimeSpan.FromSeconds(5)); } catch { }
        }

        /// <summary>
        /// Forces a flush of pending events. Called before shutdown so the
        /// last few breadcrumbs/exceptions actually leave the process.
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            try { SentrySdk.Flush(TimeSpan.FromSeconds(3)); } catch { }
            _sdkHandle?.Dispose();
            _initialized = false;
        }
    }

    /// <summary>
    /// Bridges Sentry's IDiagnosticLogger into Chromatics's verbose.log.
    /// Captures everything the SDK normally writes to its own debug stream:
    /// envelope queueing, transport HTTP responses, BeforeSend invocations,
    /// rate limit handling. Lets us diagnose "no events arriving" without
    /// attaching a network sniffer.
    /// </summary>
    internal sealed class SentryToVerboseLogLogger : IDiagnosticLogger
    {
        public bool IsEnabled(SentryLevel level) => true;

        public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
        {
            try
            {
                var formatted = (args == null || args.Length == 0) ? message : string.Format(message, args);
                var line = $"[Sentry SDK {logLevel}] {formatted}";
                if (exception != null)
                    line += $" — {exception.GetType().Name}: {exception.Message}";
                Logger.WriteVerbose(line);
            }
            catch { /* never let diagnostic logging throw */ }
        }
    }
}
