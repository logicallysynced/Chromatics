using System;
using System.Diagnostics;
using Chromatics.Core;
using Chromatics.Enums;
using RGB.NET.Devices.PlayStation;

namespace Chromatics.Extensions.RGB.NET.Devices.PlayStation
{
    // Routes upstream RGB.NET.Devices.PlayStation diagnostics into Chromatics'
    // Logger so the Console tab keeps showing the same messages it did when
    // the provider lived in Chromatics' own tree. The upstream DLL writes its
    // diagnostics through System.Diagnostics.Trace with a "[RGB.NET.PlayStation]"
    // prefix; this listener filters on that prefix and forwards each line to
    // Logger.WriteConsole. Hot-plug Connected / Disconnected lines are already
    // covered by RGBController's generic DevicesChanged handler ("Found ..." /
    // "Lost ..."), so this hook only needs to bridge error/diagnostic output.
    //
    // EnsureInstalled is idempotent — safe to call on every Load toggle and on
    // every settings-driven enable/disable cycle without piling up listeners.
    internal static class PlayStationProviderHooks
    {
        private const string Prefix = "[RGB.NET.PlayStation]";
        private static readonly object Gate = new();
        private static bool _installed;

        public static void EnsureInstalled()
        {
            lock (Gate)
            {
                if (_installed) return;
                Trace.Listeners.Add(new ChromaticsTraceListener());
                _installed = true;
            }
        }

        // Call after LoadDeviceProvider(PlayStationDeviceProvider.Instance)
        // returns. Replaces the original Chromatics provider's "No PlayStation
        // controllers detected" hint, which the upstream provider doesn't
        // emit. Per-device open failures (exclusive access, descriptor query
        // refused) come through the Trace listener already.
        public static void EmitPostLoadHints()
        {
            try
            {
                if (PlayStationDeviceProvider.Instance.Devices.Count == 0)
                {
                    Logger.WriteConsole(LoggerTypes.Devices,
                        "[PlayStation] No PlayStation controllers detected. Connect a DualShock 4 or DualSense over USB or Bluetooth — Chromatics will pick it up automatically. If one is already connected, ensure it isn't hidden by HidHide and isn't bound to DS4Windows / reWASD in exclusive mode.");
                }
            }
            catch
            {
                // Best-effort — never let a missing-hint check upset the host.
            }
        }

        // Heuristic for routing a forwarded message to the right Logger
        // category. Phrases that signal an error path go to Error (with
        // forwardToSentry: false — these are local diagnostic events, not
        // Sentry-worthy crashes). Everything else goes to Devices.
        private static bool LooksLikeError(string message)
        {
            // Cheap case-insensitive substring check across the few phrases
            // the upstream uses for failure paths.
            return message.Contains("fail", StringComparison.OrdinalIgnoreCase)
                || message.Contains("denied", StringComparison.OrdinalIgnoreCase)
                || message.Contains("could not", StringComparison.OrdinalIgnoreCase)
                || message.Contains("threw", StringComparison.OrdinalIgnoreCase);
        }

        // The upstream emits messages as plain Trace.WriteLine(string) calls,
        // which the framework routes to every registered TraceListener's
        // WriteLine(string) override. We override both WriteLine and Write so
        // any future Trace.Write usage in the upstream still gets picked up.
        private sealed class ChromaticsTraceListener : TraceListener
        {
            public override void Write(string message) => Forward(message);
            public override void WriteLine(string message) => Forward(message);

            private static void Forward(string message)
            {
                if (string.IsNullOrEmpty(message)) return;
                if (!message.StartsWith(Prefix, StringComparison.Ordinal)) return;

                LoggerTypes type = LooksLikeError(message) ? LoggerTypes.Error : LoggerTypes.Devices;
                Logger.WriteConsole(type, message, forwardToSentry: false);
            }
        }
    }
}
