#nullable enable
using Chromatics.Core;
using Chromatics.Enums;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Chromatics.Helpers
{
    // Carries the update info AND which channel it came from.
    // IsBeta    — which feed URL to use for download (routing only).
    // IsPreRelease — whether to display the update as a beta/pre-release in the UI.
    //   These differ when a stable build is cross-published to the beta feed so that
    //   beta channel installs (which Velopack filters by channel) can receive it.
    public record UpdateResult(UpdateInfo Info, bool IsBeta, bool IsPreRelease);

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
        //   - Stable feed is always checked.
        //   - Beta feed is checked when includeBeta is true OR when on the beta channel.
        //     Beta channel installs must check their own feed because Velopack filters
        //     updates by channel — a "win"-channel stable package is invisible to a
        //     "beta"-channel install. Stable releases are cross-published to the beta
        //     feed as beta-channel packages so this always delivers the right update.
        //   - When both feeds return a result, the higher version wins; stable wins ties.
        // Skipped entirely when not running inside a Velopack-managed directory
        // (dev/IDE launches never see a spurious update prompt).
        public static async Task<UpdateResult?> CheckAsync(bool includeBeta)
        {
            var probeMgr = new UpdateManager(new SimpleWebSource(StableFeedUrl));
            if (!probeMgr.IsInstalled)
                return null;

            bool isBeta = string.Equals(ReadInstalledChannel(probeMgr), "beta", StringComparison.OrdinalIgnoreCase);

            var stableResult = await CheckFeed(StableFeedUrl, isBeta: false);
            bool checkBeta   = includeBeta || isBeta;
            var betaResult   = checkBeta ? await CheckFeed(BetaFeedUrl, isBeta: true) : null;

            // When the beta result is a stable build cross-published as a beta-channel
            // package (so Velopack's channel filter lets beta installs see it), stableResult
            // will be null due to that same filter.  Detect this by checking whether the
            // beta result's version appears in the stable feed JSON directly — if it does,
            // the update is genuinely stable and should not be labelled pre-release.
            if (betaResult != null && stableResult == null)
            {
                var ver = betaResult.Info.TargetFullRelease.Version;
                if (await IsVersionInStableFeedAsync($"{ver.Major}.{ver.Minor}.{ver.Patch}"))
                    betaResult = betaResult with { IsPreRelease = false };
            }

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
                return info != null ? new UpdateResult(info, IsBeta: isBeta, IsPreRelease: isBeta) : null;
            }
            catch
            {
                return null;
            }
        }

        // Fetches the stable releases.json directly (bypassing Velopack's channel filter)
        // and checks whether the given version string appears in it.  Used to detect
        // stable builds cross-published to the beta feed so they are not labelled pre-release.
        private static async Task<bool> IsVersionInStableFeedAsync(string version)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var json = await http.GetStringAsync(StableFeedUrl + "releases.json");
                return json.Contains($"\"{version}\"", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
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
