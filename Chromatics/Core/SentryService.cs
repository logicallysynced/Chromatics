using Chromatics.Models;
using Sentry;
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
                // DSN not configured — skip silently. Runtime toggle in Settings
                // becomes a no-op rather than a crash.
                return;
            }

            // Under a debugger, do nothing. The Sentry SDK installs its own
            // AppDomain.UnhandledException + TaskScheduler.UnobservedTaskException
            // integrations that would intercept exceptions before the IDE's
            // normal break-on-unhandled flow. Skipping init keeps the IDE
            // experience identical to a clean (non-Sentry) debug session.
            if (Debugger.IsAttached) return;

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

                // Enable the Logs product (separate from Issues). Once on,
                // SentrySdk.Logger.LogInfo/LogWarning/LogError accept
                // structured log entries that show up in the dashboard's
                // Logs tab. Logger.cs forwards every WriteConsole line here.
                o.EnableLogs = true;

                // Honour the consent toggle by reading from the live _settings
                // reference, which ApplySettings swaps in once AppSettings has
                // loaded. Defaults are enableCrashReports=true, so early-startup
                // crashes are reported until the user has explicitly opted out.
                o.SetBeforeSend((evt, _) => _settings.enableCrashReports ? evt : null);
                o.SetBeforeBreadcrumb((b, _) => _settings.enableCrashReports ? b : null);
            });

            _initialized = true;
        }

        /// <summary>
        /// Layers real user settings onto the bootstrap init: applies consent,
        /// adds tags (beta/stable channel, language, admin elevation), and
        /// starts/ends the Release Health session based on the current consent.
        /// Safe to call multiple times if settings change at runtime.
        /// </summary>
        public static void ApplySettings(SettingsModel settings)
        {
            if (!_initialized) return;

            _settings = settings;

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
        /// Captures the exception and returns the Sentry event id so the
        /// caller can associate user feedback with the crash.
        /// </summary>
        public static SentryId CaptureCrash(Exception ex)
        {
            if (!IsActive) return SentryId.Empty;

            ex.Data["Chromatics.HandledByCrashDialog"] = true;
            return SentrySdk.CaptureException(ex);
        }

        /// <summary>
        /// Submits anonymous comments attached to a previously captured event
        /// id. The dialog deliberately does not collect name/email — comments
        /// are the only user-supplied field on the wire.
        /// </summary>
        public static void SubmitFeedback(SentryId eventId, string comments)
        {
            if (!IsActive || eventId == SentryId.Empty) return;
            if (string.IsNullOrWhiteSpace(comments)) return;

            try
            {
                var feedback = new SentryFeedback(
                    message: comments,
                    contactEmail: null,
                    name: null,
                    associatedEventId: eventId);

                SentrySdk.CaptureFeedback(feedback);
                SentrySdk.Flush(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Feedback submission must never throw — the user is in the
                // middle of a crash dialog already.
            }
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
}
