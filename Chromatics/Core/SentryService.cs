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

        // Background heartbeat so Sentry has steady trace/profile data even
        // when the user isn't exercising instrumented paths. Fires every
        // HeartbeatIntervalSeconds and opens a short-lived transaction that
        // the profiler attaches to. Cheap — the transaction is a no-op body;
        // its purpose is to exist so ProfilesSampleRate has something to
        // sample against.
        private static System.Threading.Timer _heartbeatTimer;
        private const double HeartbeatIntervalSeconds = 60.0;

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

                // Tracing: capture 100% of started transactions. ProfilesSampleRate
                // multiplies on top of this, so TracesSampleRate=1.0 + ProfilesSampleRate=1.0
                // means every transaction we START is profiled. NOTE: this only
                // matters when we actually call SentrySdk.StartTransaction —
                // without explicit transactions, no profile data is collected
                // regardless of the sample rates. See RunInstrumented below.
                o.TracesSampleRate = 1.0;

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
                    var s = _settings;
                    bool consent = s == null || s.enableCrashReports;
                    bool isCrash = evt.Exception?.Data.Contains("Chromatics.HandledByCrashDialog") == true
                                   || evt.Tags.TryGetValue("kind", out var k) && k == "user_feedback";
                    if (consent || isCrash) return evt;
                    Logger.WriteVerbose($"[Sentry] BeforeSend dropped event {evt.EventId} — consent disabled (non-crash)");
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
            Logger.WriteVerbose($"[Sentry] Initialize complete: enabled={SentrySdk.IsEnabled}, defaultConsent={_settings?.enableCrashReports ?? true}");

            StartHeartbeat();
        }

        private static void StartHeartbeat()
        {
            // Dev-time note: the heartbeat only emits under the consent gate
            // because BeforeSend drops events when enableCrashReports is off.
            // Interval is every 60s — enough to populate the Performance tab
            // and provide profile samples without eating quota.
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = new System.Threading.Timer(_ =>
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

                    // Op is "task" — a Sentry-standard category for
                    // background work. Custom descriptive op strings
                    // aren't indexed by the Traces explorer the same way
                    // standard ops are; transactions arrive but don't
                    // surface in the default view. See
                    // https://develop.sentry.dev/sdk/performance/span-operations/
                    var tx = SentrySdk.StartTransaction("app.heartbeat", "task");
                    tx.Description = "Periodic process-metrics heartbeat";

                    // Sentry's Trace Explorer default filter hides
                    // transactions tagged in_foreground=false — a scenario
                    // that fires whenever another Windows app has focus,
                    // even if Chromatics is visible. Override both fields
                    // so the heartbeat shows up regardless of which window
                    // the user happens to have focused.
                    try
                    {
                        tx.Contexts.App.InForeground = true;
                    }
                    catch { }
                    // Span-less transactions that finish in ~0ms are often
                    // hidden by the Sentry dashboard's Trace/Insights views
                    // (they're treated as trivial / unusable). Wrap the stat
                    // collection in an explicit child span and give the
                    // transaction a real, non-zero duration so it renders
                    // in the UI. The span also gives us a place to stamp
                    // the collected metrics as both tags (filterable) and
                    // extras (displayed in the event detail panel).
                    var span = tx.StartChild("metrics.collect", "collect process metrics");
                    try
                    {
                        using var p = System.Diagnostics.Process.GetCurrentProcess();

                        // Memory: working set (resident physical memory),
                        // private bytes (committed), and managed heap size.
                        // All in MB for readability in the Sentry UI.
                        long workingSetMb = p.WorkingSet64 / 1024 / 1024;
                        long privateMb = p.PrivateMemorySize64 / 1024 / 1024;
                        long managedHeapMb = GC.GetTotalMemory(forceFullCollection: false) / 1024 / 1024;
                        int gc0 = GC.CollectionCount(0);
                        int gc1 = GC.CollectionCount(1);
                        int gc2 = GC.CollectionCount(2);
                        int threads = p.Threads.Count;

                        // Tags: searchable / aggregatable in Dashboards, but
                        // cardinality-sensitive (Sentry limits them). Stamp
                        // on BOTH the transaction AND the span so either
                        // view surfaces them.
                        tx.SetTag("working_set_mb", workingSetMb.ToString());
                        tx.SetTag("private_memory_mb", privateMb.ToString());
                        tx.SetTag("managed_heap_mb", managedHeapMb.ToString());
                        tx.SetTag("gc_gen0", gc0.ToString());
                        tx.SetTag("gc_gen1", gc1.ToString());
                        tx.SetTag("gc_gen2", gc2.ToString());
                        tx.SetTag("thread_count", threads.ToString());

                        // Span data: visible in the event detail view under
                        // "Additional Data". Numeric so Dashboards can plot
                        // them directly without a string→int coercion.
                        span.SetData("working_set_mb", workingSetMb);
                        span.SetData("private_memory_mb", privateMb);
                        span.SetData("managed_heap_mb", managedHeapMb);
                        span.SetData("gc_gen0", gc0);
                        span.SetData("gc_gen1", gc1);
                        span.SetData("gc_gen2", gc2);
                        span.SetData("thread_count", threads);

                        // CPU usage: percent of a single core used since the
                        // last heartbeat. Divided by core count so the value
                        // is normalised (100% = one core fully saturated,
                        // capped at 100% even on machines with many cores).
                        // First sample is 0% — we need two points to compute
                        // a delta.
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
                                tx.SetTag("cpu_percent", cpuPct.ToString("F1"));
                                span.SetData("cpu_percent", cpuPct);
                            }
                        }
                        _lastProcessorTime = cpuTime;
                        _lastSampleTime = now;

                        // Longer sleep (300ms) so the transaction's wall-
                        // clock duration is well above any minimum-duration
                        // filter Sentry's Trace Explorer might apply. 100ms
                        // was borderline — 300ms is comfortably visible in
                        // every default view. Runs on the thread pool so
                        // zero impact on user-visible latency.
                        System.Threading.Thread.Sleep(300);
                    }
                    catch { /* non-critical */ }
                    span.Finish(SpanStatus.Ok);
                    tx.Finish(SpanStatus.Ok);

                    // Belt-and-suspenders: also capture an Info-level
                    // message for the Issues tab. Unlike transactions,
                    // Sentry renders CaptureMessage events unconditionally
                    // in the Issues list, so if this line shows up but the
                    // app.heartbeat transaction doesn't, it definitively
                    // narrows the problem to transaction-filter-side.
                    // Tagged so it can be grouped / filtered out of regular
                    // issue triage.
                    try
                    {
                        SentrySdk.CaptureMessage(
                            "app.heartbeat pulse",
                            scope =>
                            {
                                scope.SetTag("kind", "heartbeat");
                                scope.Level = SentryLevel.Info;
                            });
                    }
                    catch { }
                }
                catch { /* heartbeat must never throw */ }
            }, null, TimeSpan.FromSeconds(HeartbeatIntervalSeconds), TimeSpan.FromSeconds(HeartbeatIntervalSeconds));
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
            {
                Logger.WriteVerbose($"[Sentry] CaptureCrash skipped: SDK not available (initialized={_initialized}, sdkEnabled={SentrySdk.IsEnabled})");
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
