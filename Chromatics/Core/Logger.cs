using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Models;
using Sentry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;

namespace Chromatics.Core
{
    public delegate void OnConsoleLoggedEventHandler(object source, OnConsoleLoggedEventArgs e);

    public static class Logger
    {
        public static event OnConsoleLoggedEventHandler OnConsoleLogged = delegate { };

        // Messages logged before the console subscriber attaches (e.g. the
        // .chromatics3 file migration in Program.Main) are buffered here and
        // replayed atomically by the first caller of Attach.
        private const int MaxBuffered = 500;
        private static readonly List<OnConsoleLoggedEventArgs> _buffer = new();
        private static readonly System.Threading.Lock _gate = new();

        // Verbose log — written by both WriteConsole and WriteVerbose.
        // WriteVerbose-only messages are suppressed from the Console tab.
        private static string _logDirectory;
        private static readonly System.Threading.Lock _fileLock = new();
        private const long MaxLogBytes = 10 * 1024 * 1024; // 10 MB before rotation

        public static void SetLogDirectory(string directory)
        {
            _logDirectory = directory;
        }

        public static void WriteConsole(LoggerTypes type, string message)
        {
            var color = (Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(type).Value;
            var timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
            var args = new OnConsoleLoggedEventArgs($"[{timestamp}] {message}", color);

            AppendToVerboseLog($"[{timestamp}] [{type}] {message}");

            ForwardToSentry(type, message);

            lock (_gate)
            {
                if (_buffer.Count >= MaxBuffered)
                    _buffer.RemoveAt(0);
                _buffer.Add(args);
                OnConsoleLogged(null, args);
            }
        }

        // Forwards ERROR-tier lines only to Sentry. Two channels:
        //   1. AddBreadcrumb — buffered locally on the SDK, attached to any
        //      subsequent captured exception so error reports include the
        //      surrounding context. Cheap; kept for all error lines.
        //   2. CaptureMessage at Error level — surfaces non-exception
        //      failures (e.g. device init errors) in the Issues tab.
        // Honours the user's enableCrashReports toggle automatically —
        // SentrySdk.IsEnabled is false when consent has been withheld.
        //
        // Info/system/device/etc. lines deliberately do NOT forward. They
        // were useful for ad-hoc debugging but were noisy in the Sentry
        // Logs tab and counted against event quota. Breadcrumbs still
        // capture the preceding ~100 lines on any error that fires.
        private static void ForwardToSentry(LoggerTypes type, string message)
        {
            if (!SentrySdk.IsEnabled) return;

            try
            {
                var breadcrumbLevel = type switch
                {
                    LoggerTypes.Error => BreadcrumbLevel.Error,
                    _ => BreadcrumbLevel.Info,
                };

                // Breadcrumbs for every line (preserves surrounding context
                // on errors) — they don't leave the SDK until an event is
                // actually captured, so cost is negligible.
                SentrySdk.AddBreadcrumb(message, category: type.ToString(), level: breadcrumbLevel);

                // Only errors actually ship to Sentry.
                if (type == LoggerTypes.Error)
                {
                    SentrySdk.CaptureMessage($"[{type}] {message}", SentryLevel.Error);
                }
            }
            catch
            {
                // Logging must never throw.
            }
        }

        // Writes only to verbose.log — never shown in the Console tab.
        // Use for noisy startup/migration messages and internal diagnostics.
        public static void WriteVerbose(string message)
        {
            var timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
            AppendToVerboseLog($"[{timestamp}] [VERBOSE] {message}");
        }

        // Atomically replays any pre-subscription messages to the handler,
        // then subscribes the handler for future messages. Called by the
        // ConsoleViewModel constructor so messages logged during Program.Main
        // (before Avalonia boots) are not lost.
        public static void AttachSubscriberAndDrain(OnConsoleLoggedEventHandler handler)
        {
            lock (_gate)
            {
                foreach (var e in _buffer)
                    handler(null, e);
                _buffer.Clear();
                OnConsoleLogged += handler;
            }
        }

        private static void AppendToVerboseLog(string line)
        {
            if (string.IsNullOrEmpty(_logDirectory)) return;

            lock (_fileLock)
            {
                try
                {
                    var path = Path.Combine(_logDirectory, "verbose.log");

                    if (File.Exists(path) && new FileInfo(path).Length > MaxLogBytes)
                        File.Move(path, path + ".old", overwrite: true);

                    File.AppendAllText(path, line + Environment.NewLine);
                }
                catch { }
            }
        }
    }
}
