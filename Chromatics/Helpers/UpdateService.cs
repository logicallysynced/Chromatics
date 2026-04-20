#nullable enable
using Chromatics.Core;
using Chromatics.Enums;
using System;
using System.IO;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Chromatics.Helpers
{
    // IsBeta     — which feed URL to use for download (routing only).
    // IsPreRelease — whether to display the update as a beta/pre-release in the UI.
    //   With marker-file-based channel identity these are always equal, but the
    //   two concepts stay separate so future schemes (e.g. pre-release suffixed
    //   stable builds) remain expressible.
    public record UpdateResult(UpdateInfo Info, bool IsBeta, bool IsPreRelease);

    public static class UpdateService
    {
        private const string StableFeedUrl = "https://chromaticsffxiv.com/chromatics4/update/stable/";
        private const string BetaFeedUrl   = "https://chromaticsffxiv.com/chromatics4/update/beta/";

        // Marker file that ships inside the nupkg for real beta builds only.
        // Stable releases (including any a beta install migrates to) never have it,
        // so after a beta→stable migration the title drops the [BETA] suffix cleanly.
        //
        // Velopack's own channel metadata is ignored for identity purposes —
        // publish.py no longer sets --channel, and the feed JSON files are aliased
        // under every channel name so any client can fetch them regardless of what
        // Velopack thinks its installed channel is.
        private const string BetaMarkerFileName = "channel.beta";

        public static bool IsBetaChannel()
        {
            try
            {
                var markerPath = Path.Combine(AppContext.BaseDirectory, BetaMarkerFileName);
                return File.Exists(markerPath);
            }
            catch
            {
                return false;
            }
        }

        // Checks for an update.
        //   - Stable feed is always checked.
        //   - Beta feed is checked only when the user has opted in to beta updates.
        //     A beta install that opts out naturally migrates to stable because
        //     the stable feed's releases.beta.json alias lets it see stable updates.
        //   - When both feeds return a result, the higher version wins; stable wins ties.
        // Skipped entirely when not running inside a Velopack-managed directory
        // (dev/IDE launches never see a spurious update prompt).
        public static async Task<UpdateResult?> CheckAsync(bool includeBeta)
        {
            var probeMgr = new UpdateManager(new SimpleWebSource(StableFeedUrl));
            if (!probeMgr.IsInstalled)
                return null;

            var stableResult = await CheckFeed(StableFeedUrl, isBeta: false);
            var betaResult   = includeBeta ? await CheckFeed(BetaFeedUrl, isBeta: true) : null;

            Logger.WriteConsole(LoggerTypes.System,
                $"[Update] stable={FormatResult(stableResult)} beta={FormatResult(betaResult)} includeBeta={includeBeta} markerBeta={IsBetaChannel()}");

            if (stableResult != null && betaResult != null)
            {
                var sv = stableResult.Info.TargetFullRelease.Version;
                var bv = betaResult.Info.TargetFullRelease.Version;
                return sv >= bv ? stableResult : betaResult;
            }

            return stableResult ?? betaResult;
        }

        // Fetches one feed and returns null (silently) when the feed is unreachable
        // or does not yet contain a release JSON — expected when the channel has
        // never had a release published.
        private static async Task<UpdateResult?> CheckFeed(string feedUrl, bool isBeta)
        {
            try
            {
                var mgr  = new UpdateManager(new SimpleWebSource(feedUrl));
                var info = await mgr.CheckForUpdatesAsync();
                return info != null ? new UpdateResult(info, IsBeta: isBeta, IsPreRelease: isBeta) : null;
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

        private static string FormatResult(UpdateResult? r) =>
            r == null
                ? "null"
                : $"{r.Info.TargetFullRelease.Version} (IsBeta={r.IsBeta}, IsPreRelease={r.IsPreRelease})";
    }
}
