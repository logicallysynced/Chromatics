using System;
using System.Linq;
using System.Net.Sockets;

namespace Chromatics.Helpers
{
    /// <summary>
    /// Decides whether a fault raised off the UI thread is worth crashing the
    /// app and telling Sentry about, or is just the user's network and hardware
    /// behaving normally.
    /// </summary>
    /// <remarks>
    /// One policy, two callers: Program's unobserved-task handler uses it to
    /// decide whether to show the crash dialog, and SentryService's BeforeSend
    /// filter uses it to decide whether to report. They used to carry a copy
    /// each, with a comment warning they had to agree - and by the time it
    /// mattered they had already drifted six socket codes apart.
    /// </remarks>
    public static class BackgroundFaultPolicy
    {
        private const int MaxDepth = 16;

        public static bool IsBenign(Exception ex) => IsBenign(ex, 0);

        private static bool IsBenign(Exception ex, int depth)
        {
            if (ex is null || depth > MaxDepth) return false;

            // Every member has to be benign for the whole set to be, so one real
            // fault travelling alongside a dropped connection still reports.
            if (ex is AggregateException aggregate)
            {
                var flattened = aggregate.Flatten();
                return flattened.InnerExceptions.Count > 0
                    && flattened.InnerExceptions.All(inner => IsBenign(inner, depth + 1));
            }

            switch (ex)
            {
                case SocketException socket:
                    return IsBenignSocketError(socket.SocketErrorCode);

                case ObjectDisposedException:
                case OperationCanceledException:
                    return true;

                // Avalonia's WndProc raises ShutdownRequested a second time on
                // system logoff, after OnClosed already called desktop.Shutdown().
                // DoShutdown throws because the flag is set. We Process.Kill right
                // after, so the user never sees it.
                case InvalidOperationException invalid
                    when invalid.Message.StartsWith("Application is already shutting down", StringComparison.Ordinal):
                    return true;
            }

            // Walk the wrapper chain rather than checking a single level. A bulb
            // that drops its connection mid-write arrives as
            // DeviceProviderException -> IOException -> SocketException, and only
            // the innermost frame carries the socket code, so a one-level check
            // called it fatal and showed the crash dialog (CHROMATICS-1W).
            return IsBenign(ex.InnerException, depth + 1);
        }

        private static bool IsBenignSocketError(SocketError code) => code switch
        {
            // Teardown: the far end or our own stack closed the socket while a
            // write was in flight.
            SocketError.OperationAborted => true,
            SocketError.ConnectionReset => true,
            SocketError.ConnectionAborted => true,
            SocketError.Interrupted => true,
            SocketError.Shutdown => true,
            SocketError.NetworkReset => true,

            // Unreachable: switched off at the wall, moved, or never listening.
            SocketError.ConnectionRefused => true,
            SocketError.TimedOut => true,
            SocketError.HostUnreachable => true,
            SocketError.NetworkUnreachable => true,
            SocketError.HostDown => true,
            SocketError.NetworkDown => true,

            _ => false,
        };
    }
}
