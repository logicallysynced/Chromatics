#nullable enable
using Chromatics.Core;
using Chromatics.Enums;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Chromatics.Helpers
{
    // Carries the update info AND which channel it came from, so the
    // download step uses the matching feed URL.
    public record UpdateResult(UpdateInfo Info, bool IsBeta);

    public static class UpdateService
    {
        // Stable builds are served from the root update directory.
        // Beta builds live in /beta/ so the two feeds never collide.
        private const string StableFeedUrl = "https://chromaticsffxiv.com/chromatics4/update/stable/";
        private const string BetaFeedUrl   = "https://chromaticsffxiv.com/chromatics4/update/beta/";

        // Returns true when the installed package was built with --channel beta.
        // Reads from local Velopack metadata — no network call.
        // Returns false when not installed (dev/IDE launch) or on the stable channel.
        public static bool IsBetaChannel()
        {
            try
            {
                var mgr = new UpdateManager(new SimpleWebSource(StableFeedUrl));
                if (!mgr.IsInstalled) return false;
                return string.Equals(ReadInstalledChannel(mgr), "beta", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // Checks for an update. Beta installs check the beta feed exclusively.
        // Stable installs check the stable feed, then optionally the beta feed when
        // the user has opted in to beta updates.
        // Both checks are skipped when not running inside a Velopack-managed directory
        // (i.e. running directly from a build output or IDE), so dev/debug launches
        // are never shown a spurious update prompt.
        public static async Task<UpdateResult?> CheckAsync(bool includeBeta)
        {
            try
            {
                var probeMgr = new UpdateManager(new SimpleWebSource(StableFeedUrl));
                if (!probeMgr.IsInstalled)
                    return null;

                bool isBeta = string.Equals(ReadInstalledChannel(probeMgr), "beta", StringComparison.OrdinalIgnoreCase);

                if (isBeta)
                {
                    var betaMgr  = new UpdateManager(new SimpleWebSource(BetaFeedUrl));
                    var betaInfo = await betaMgr.CheckForUpdatesAsync();
                    return betaInfo != null ? new UpdateResult(betaInfo, IsBeta: true) : null;
                }

                var stableInfo = await probeMgr.CheckForUpdatesAsync();
                if (stableInfo != null)
                    return new UpdateResult(stableInfo, IsBeta: false);

                if (!includeBeta)
                    return null;

                var betaMgr2  = new UpdateManager(new SimpleWebSource(BetaFeedUrl));
                var betaInfo2 = await betaMgr2.CheckForUpdatesAsync();
                return betaInfo2 != null ? new UpdateResult(betaInfo2, IsBeta: true) : null;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.System, $"Update check failed: {ex.Message}");
                return null;
            }
        }

        public static async Task DownloadAndApplyAsync(UpdateResult result, Action<int>? progress = null)
        {
            var feedUrl = result.IsBeta ? BetaFeedUrl : StableFeedUrl;
            var mgr = new UpdateManager(new SimpleWebSource(feedUrl));
            await mgr.DownloadUpdatesAsync(result.Info, progress);
            mgr.ApplyUpdatesAndRestart(result.Info);
        }

        // DefaultChannel and Locator are internal in the Velopack version we target.
        // Read via reflection; isolated here so the rest of the class stays clean.
        // Wrapped by callers in try/catch so any future Velopack API change is silent.
        private static string? ReadInstalledChannel(UpdateManager mgr)
        {
            var prop = typeof(UpdateManager).GetProperty("DefaultChannel",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return prop?.GetValue(mgr) as string;
        }
    }
}
