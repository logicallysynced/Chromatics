using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;

namespace Chromatics.Helpers
{
    public static class NetworkFailureHelper
    {
        /// <summary>
        /// True when an exception describes a network device that could not be
        /// reached, rather than a fault in Chromatics. Callers log these for the
        /// user but keep them out of Sentry, where a bulb switched off at the
        /// wall would otherwise read as an application error.
        /// </summary>
        public static bool IsUnreachable(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                switch (current)
                {
                    case SocketException:
                    case TimeoutException:
                    case HttpRequestException:
                    case IOException:
                    case OperationCanceledException:
                        return true;
                }

                if (current is AggregateException aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions)
                    {
                        if (IsUnreachable(inner)) return true;
                    }
                }
            }

            return false;
        }
    }
}
