using Chromatics.Models;
using Sentry;
using Sentry.Profiling;
using System;
using System.Diagnostics;
using System.Linq;
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

        // Background heartbeat so Sentry has steady trace/profile data even
        // when the user isn't exercising instrumented paths. Fires every
        // HeartbeatIntervalSeconds and opens a short-lived transaction that
        // the profiler attaches to. Cheap — the transaction is a no-op body;
        // its purpose is to exist so ProfilesSampleRate has something to
        // sample against.
        private static System.Threading.Timer _heartbeatTimer;
        private const double HeartbeatIntervalSeconds = 300.0;
        // First-fire delay so a baseline tick lands on most sessions without
        // burning quota on transient one-second launches (Velopack lifecycle
        // probes, --help, etc.). 60s catches typical user sessions while
        // staying clear of the noisy early seconds.
        private const double HeartbeatInitialDelaySeconds = 60.0;

        // CPU usage is computed from deltas between heartbeats, so we keep
        // the last-sampled values here. Initial call returns 0% (no baseline
        // to diff against).
        private static TimeSpan _lastProcessorTime;
        private static DateTime _lastSampleTime;

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
                return;

            // Under a debugger, do nothing. The Sentry SDK installs its own
            // AppDomain.UnhandledException + TaskScheduler.UnobservedTaskException
            // integrations that would intercept exceptions before the IDE's
            // normal break-on-unhandled flow. Skipping init keeps the IDE
            // experience identical to a clean (non-Sentry) debug session.
            if (Debugger.IsAttached)
                return;

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

                // Tracing: capture 100% of started transactions. ProfilesSampleRate
                // multiplies on top of this, so TracesSampleRate=1.0 + ProfilesSampleRate=1.0
                // means every transaction we START is profiled. NOTE: this only
                // matters when we actually call SentrySdk.StartTransaction —
                // without explicit transactions, no profile data is collected
                // regardless of the sample rates. See RunInstrumented below.
                o.TracesSampleRate = 1.0;

                // Explicit per-transaction sampler that always returns 1.0.
                // Defends against any default heuristic the SDK might apply on
                // top of TracesSampleRate (e.g. backpressure throttling, or
                // transaction-context-based sampling that downweights short
                // background tasks). Returning 1.0 unconditionally guarantees
                // every StartTransaction we issue is sampled and queued for
                // send.
                o.TracesSampler = _ => 1.0;

                // Backpressure handling is enabled by default in Sentry .NET
                // 5.x+. If the SDK detects send issues (rate limit, transient
                // network failures), it AUTOMATICALLY downsamples traces to
                // try and recover. For an OSS desktop app emitting one
                // transaction per minute, that's the wrong tradeoff: the
                // user's heartbeats start silently dropping after any single
                // transient failure and never come back without a restart.
                // Disable it so our 1-per-minute cadence is preserved.
                o.EnableBackpressureHandling = false;

                // Profiling: requires the Sentry.Profiling integration package.
                // The TimeSpan argument is the startup-grace window — the SDK
                // waits up to this long during app launch so the profiler is
                // ready when the first transaction starts. 500ms matches
                // Sentry's recommended default.
                o.ProfilesSampleRate = 1.0;
                o.AddIntegration(new ProfilingIntegration(TimeSpan.FromMilliseconds(500)));

                // Strip personally identifiable information. We're an OSS app
                // and the user is shown a privacy notice in the README; keep
                // the data we send minimal.
                o.SendDefaultPii = false;
                o.AttachStacktrace = true;

                // Don't auto-capture WriteLine output — it's noisy and the
                // user-visible Console tab is already replayed via Logger.
                o.MaxBreadcrumbs = 100;

                // SDK internal diagnostics are disabled. When Debug is on, the
                // SDK emits the "Debug=true in production" warning, the
                // MergeDebugImagesInto-multiple-times warning, periodic
                // envelope queue/handoff lines, and full HTTP transport
                // payload dumps — together they fill verbose.log with
                // megabytes of noise that doesn't help end users. Transport
                // failures will surface server-side ("no events arrived")
                // rather than client-side; flip Debug back to true and
                // re-enable DiagnosticLogger when diagnosing send issues.
                o.Debug = false;

                // Enable the Logs product (separate from Issues). Once on,
                // SentrySdk.Logger.LogInfo/LogWarning/LogError accept
                // structured log entries that show up in the dashboard's
                // Logs tab. Logger.cs forwards every WriteConsole line here.
                o.EnableLogs = true;

                // Consent model: the opt-out toggle governs ONGOING telemetry
                // (breadcrumbs, non-crash error messages, performance data,
                // heartbeat). Crash reports bypass consent because the user
                // explicitly confirms by clicking "Send" in the post-crash
                // dialog — treating that click as a second, localised consent
                // to ship this one event.
                //
                // Crashes are tagged via Exception.Data["Chromatics.HandledByCrashDialog"]
                // (set by CaptureCrash). BeforeSend lets those through and
                // drops everything else when consent is off.
                o.SetBeforeSend((evt, _) =>
                {
                    // Drop unobserved-task SocketException noise (Hue/OpenRGB
                    // shutdown, transient network resets) before consent gating.
                    // These are auto-captured by Sentry's UnobservedTaskException
                    // integration and would otherwise spam the Issues tab; our
                    // own UnobservedTaskExceptionHandler already filters them
                    // from the crash flow on the same criteria.
                    if (evt.Exception != null && IsBenignBackgroundException(evt.Exception))
                        return null;

                    var s = _settings;
                    bool consent = s == null || s.enableCrashReports;
                    bool isCrash = evt.Exception?.Data.Contains("Chromatics.HandledByCrashDialog") == true
                                   || evt.Tags.TryGetValue("kind", out var k) && k == "user_feedback";
                    if (consent || isCrash) return evt;
                    return null;
                });

                // Transactions = performance / profiling. No bypass for these
                // — if the user opts out, no performance data ships.
                o.SetBeforeSendTransaction((tx, _) =>
                {
                    var s = _settings;
                    return (s == null || s.enableCrashReports) ? tx : null;
                });

                o.SetBeforeBreadcrumb((b, _) =>
                {
                    // Breadcrumbs are buffered locally and only egress as part
                    // of a captured event. Let them through unconditionally —
                    // BeforeSend decides whether the containing event ships.
                    return b;
                });
            });

            _initialized = true;

            StartHeartbeat();
        }

        private static void StartHeartbeat()
        {
            // Dev-time note: the heartbeat only emits under the consent gate
            // because BeforeSend drops events when enableCrashReports is off.
            // Interval is every 60s — enough to populate the Performance tab
            // and provide profile samples without eating quota.
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = new System.Threading.Timer(
                _ => _ = RunHeartbeatAsync(),
                null,
                TimeSpan.FromSeconds(HeartbeatInitialDelaySeconds),
                TimeSpan.FromSeconds(HeartbeatIntervalSeconds));
        }

        private static async System.Threading.Tasks.Task RunHeartbeatAsync()
        {
            try
            {
                if (!SentrySdk.IsEnabled) return;
                // Consent gate: no telemetry during normal runtime when
                // opted out. Transactions are also dropped by
                // BeforeSendTransaction, but skipping the work entirely
                // saves the allocation + profiler overhead.
                var s = _settings;
                if (s != null && !s.enableCrashReports) return;

                // NOTE: requires the Sentry project's "Filter out health
                // check transactions" inbound filter to be DISABLED.
                // That filter regex-matches transaction names containing
                // health / heart / ping / alive / ready and silently
                // drops the entire envelope server-side regardless of
                // transaction shape, scope binding, or measurements.
                // If app.heartbeat events stop arriving, check that
                // filter first (Project Settings → Inbound Filters).
                var tx = SentrySdk.StartTransaction("app.heartbeat", "task");
                tx.Description = "Periodic process-metrics heartbeat";

                // Bind to the active scope so the transaction inherits
                // the scope's tags (channel/language/admin from
                // ApplySettings) and so any breadcrumbs / spans
                // generated inside `work` automatically attach. Mirrors
                // RunInstrumented's pattern (which is what app.startup
                // uses).
                SentrySdk.ConfigureScope(scope => scope.Transaction = tx);

                var span = tx.StartChild("metrics.collect", "collect process metrics");
                try
                {
                    using var p = System.Diagnostics.Process.GetCurrentProcess();

                    long workingSetMb = p.WorkingSet64 / 1024 / 1024;
                    long privateMb = p.PrivateMemorySize64 / 1024 / 1024;
                    long managedHeapMb = GC.GetTotalMemory(forceFullCollection: false) / 1024 / 1024;
                    int gc0 = GC.CollectionCount(0);
                    int gc1 = GC.CollectionCount(1);
                    int gc2 = GC.CollectionCount(2);
                    int threads = p.Threads.Count;

                    // Measurements: numeric, unit-aware values that
                    // Sentry plots as time series in the transaction
                    // detail view and that are queryable via
                    // `measurements.<name>:>=<value>` in the Trace
                    // Explorer. Proper primitive for "stat that varies
                    // per transaction" — the ingest-side equivalent of
                    // a Prometheus gauge.
                    tx.SetMeasurement("working_set_mb", workingSetMb, MeasurementUnit.Information.Megabyte);
                    tx.SetMeasurement("private_memory_mb", privateMb, MeasurementUnit.Information.Megabyte);
                    tx.SetMeasurement("managed_heap_mb", managedHeapMb, MeasurementUnit.Information.Megabyte);
                    tx.SetMeasurement("gc_gen0", gc0, MeasurementUnit.None);
                    tx.SetMeasurement("gc_gen1", gc1, MeasurementUnit.None);
                    tx.SetMeasurement("gc_gen2", gc2, MeasurementUnit.None);
                    tx.SetMeasurement("thread_count", threads, MeasurementUnit.None);

                    // Span data: same numeric values mirrored on the
                    // child span so the event-detail "Additional Data"
                    // panel surfaces them in-place rather than only via
                    // the Measurements section.
                    span.SetData("working_set_mb", workingSetMb);
                    span.SetData("private_memory_mb", privateMb);
                    span.SetData("managed_heap_mb", managedHeapMb);
                    span.SetData("gc_gen0", gc0);
                    span.SetData("gc_gen1", gc1);
                    span.SetData("gc_gen2", gc2);
                    span.SetData("thread_count", threads);

                    // CPU usage: percent of a single core used since the
                    // last heartbeat. Divided by core count so 100% = one
                    // core fully saturated, capped across many-core
                    // machines. First sample is 0 (no baseline delta).
                    var now = DateTime.UtcNow;
                    var cpuTime = p.TotalProcessorTime;
                    if (_lastSampleTime != DateTime.MinValue)
                    {
                        var cpuDelta = (cpuTime - _lastProcessorTime).TotalMilliseconds;
                        var wallDelta = (now - _lastSampleTime).TotalMilliseconds;
                        if (wallDelta > 0)
                        {
                            var cores = Math.Max(1, Environment.ProcessorCount);
                            var cpuPct = Math.Round(Math.Clamp((cpuDelta / (wallDelta * cores)) * 100.0, 0, 100), 1);
                            tx.SetMeasurement("cpu_percent", cpuPct, MeasurementUnit.Fraction.Percent);
                            span.SetData("cpu_percent", cpuPct);
                        }
                    }
                    _lastProcessorTime = cpuTime;
                    _lastSampleTime = now;

                    // Keep the transaction span open ~1.5s so the profiler
                    // collects samples comparable to app.startup duration.
                    // Uses async delay so the thread-pool thread is released
                    // during the wait rather than blocked.
                    await System.Threading.Tasks.Task.Delay(1500).ConfigureAwait(false);
                }
                catch { /* non-critical */ }
                span.Finish(SpanStatus.Ok);
                tx.Finish(SpanStatus.Ok);

                // Detach the transaction from scope so subsequent
                // unrelated events don't inherit a stale trace context.
                SentrySdk.ConfigureScope(scope => scope.Transaction = null);

                // Force-flush so the envelope leaves the process now
                // rather than sitting in the SDK's background queue.
                // For a 1/min cadence on an app that the user might
                // close at any moment, an explicit flush guarantees
                // each tick reaches the backend.
                try { await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); } catch { }
            }
            catch { /* heartbeat must never throw */ }
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
                return;

            // Defensive: AppSettings.Startup can hand us null when settings
            // deserialization fails (malformed settings.chromatics4). The
            // previous version blindly assigned _settings = null and then
            // NRE'd on the next field access, which cascaded into BeforeSend
            // and CaptureCrash failures and broke the themed crash dialog.
            if (settings == null)
                return;

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
        /// Runs <paramref name="work"/> inside a Sentry transaction so the
        /// profiler (and traces) capture meaningful timing data. Without a
        /// transaction actively running, ProfilesSampleRate has nothing to
        /// attach to — Sentry does not auto-instrument background .NET code,
        /// so profiling + tracing appear empty until we manually open a
        /// transaction around operations we care about.
        ///
        /// Exceptions propagate; the transaction is marked errored and
        /// finished automatically via the `using` scope.
        ///
        /// Call sites: startup setup (one-shot, useful for observing launch
        /// perf), device provider load/unload (tells us how long hardware
        /// enumeration takes), and other infrequent high-value operations.
        /// Do NOT wrap per-tick render or game-state reads — that would
        /// saturate the SDK with 60Hz transactions.
        /// </summary>
        public static void RunInstrumented(string operation, string description, Action work)
        {
            if (!_initialized || work == null)
            {
                work?.Invoke();
                return;
            }

            // Consent gate — skip the transaction entirely when opted out.
            var s = _settings;
            if (s != null && !s.enableCrashReports)
            {
                work();
                return;
            }

            ITransactionTracer tx = null;
            try
            {
                tx = SentrySdk.StartTransaction(operation, description);
                SentrySdk.ConfigureScope(s => s.Transaction = tx);
                work();
                tx.Finish(SpanStatus.Ok);
            }
            catch (Exception ex)
            {
                tx?.Finish(ex);
                throw;
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
            // Crash capture IGNORES the consent toggle because the user sees
            // the CrashFeedbackDialog and has to click "Send" — the click is a
            // per-crash consent. The toggle only governs ongoing/silent
            // telemetry. BeforeSend detects these via ex.Data and lets them
            // through unconditionally.
            if (!_initialized || !SentrySdk.IsEnabled)
                return SentryId.Empty;

            ex.Data["Chromatics.HandledByCrashDialog"] = true;
            var id = SentrySdk.CaptureException(ex);

            try { SentrySdk.Flush(TimeSpan.FromSeconds(5)); } catch { /* best-effort flush */ }

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
            // Feedback submission IGNORES the consent toggle, same reasoning
            // as CaptureCrash — the Send click is the per-crash consent.
            // BeforeSend allows `kind=user_feedback` events through regardless.
            if (!_initialized || !SentrySdk.IsEnabled || eventId == SentryId.Empty) return;
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

            // Positional args only — SentryStructuredLogger uses String.Format, not Serilog templates.
            try
            {
                var evIdStr = eventId.ToString();
                SentrySdk.Logger.LogInfo(
                    log => { log.SetAttribute("associated_event_id", evIdStr); log.SetAttribute("kind", "user_feedback"); },
                    "User feedback: {0}",
                    new object[] { comments });
            }
            catch { }

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
            try { _heartbeatTimer?.Dispose(); _heartbeatTimer = null; } catch { }
            try { SentrySdk.Flush(TimeSpan.FromSeconds(3)); } catch { }
            _sdkHandle?.Dispose();
            _initialized = false;
        }

        // Mirror of Program.IsBenignBackgroundException — kept private to
        // SentryService so the BeforeSend filter doesn't take a hard dep on
        // Program. Both must agree on what's "benign" or events get dropped
        // here while the crash flow still kills the app, or vice versa.
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
                // Avalonia's WndProc raises ShutdownRequested a second time
                // (e.g. system logoff / WM_CLOSE on a transient window) after
                // our OnClosed already called desktop.Shutdown(). DoShutdown
                // throws because _isShuttingDown is already true. We Process.Kill
                // immediately after, so the throw never affects the user — but
                // Sentry's UnhandledException integration still captures it as
                // handled noise. Filter at source.
                InvalidOperationException ioe when ioe.Message.StartsWith("Application is already shutting down", StringComparison.Ordinal) => true,
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
    }

}
