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

        // Checks for an update. Rules:
        //   - Stable is always checked, regardless of installed channel or opt-in.
        //   - Beta feed is additionally checked when includeBeta is true.
        //   - When both feeds have an update, the higher version wins; stable wins ties.
        //   - This means a beta user with beta opt-out naturally migrates to stable,
        //     and a beta user with beta opt-in still gets the newer stable if one exists.
        // Skipped entirely when not running inside a Velopack-managed directory
        // (dev/IDE launches never see a spurious update prompt).
        public static async Task<UpdateResult?> CheckAsync(bool includeBeta)
        {
            var probeMgr = new UpdateManager(new SimpleWebSource(StableFeedUrl));
            if (!probeMgr.IsInstalled)
                return null;

            var stableResult = await CheckFeed(StableFeedUrl, isBeta: false);
            var betaResult   = includeBeta ? await CheckFeed(BetaFeedUrl, isBeta: true) : null;

            if (stableResult != null && betaResult != null)
            {
                var sv = stableResult.Info.TargetFullRelease.Version;
                var bv = betaResult.Info.TargetFullRelease.Version;
                return sv >= bv ? stableResult : betaResult;
            }

            return stableResult ?? betaResult;
        }

        // Fetches one feed and returns null (silently) when the feed is
        // unreachable or does not yet contain a release JSON — this is expected
        // when the channel has never had a release published.
        private static async Task<UpdateResult?> CheckFeed(string feedUrl, bool isBeta)
        {
            try
            {
                var mgr  = new UpdateManager(new SimpleWebSource(feedUrl));
                var info = await mgr.CheckForUpdatesAsync();
                return info != null ? new UpdateResult(info, isBeta) : null;
            }
            catch
            {
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
