#nullable enable
using Chromatics.Core;
using Chromatics.Enums;
using System;
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

        // Checks for an update. Stable always takes priority over beta:
        //   1. If the stable feed has a newer version → return it.
        //   2. If stable is current AND betaChannel is opted in → check the beta feed.
        //   3. If neither feed has a newer version → return null (already up to date).
        //
        // Both checks are skipped when not running inside a Velopack-managed directory
        // (i.e. running directly from a build output or IDE), so dev/debug launches
        // are never shown a spurious update prompt.
        public static async Task<UpdateResult?> CheckAsync(bool includeBeta)
        {
            try
            {
                var stableMgr = new UpdateManager(new SimpleWebSource(StableFeedUrl));
                if (!stableMgr.IsInstalled)
                    return null;

                var stableInfo = await stableMgr.CheckForUpdatesAsync();
                if (stableInfo != null)
                    return new UpdateResult(stableInfo, IsBeta: false);

                if (!includeBeta)
                    return null;

                var betaMgr  = new UpdateManager(new SimpleWebSource(BetaFeedUrl));
                var betaInfo = await betaMgr.CheckForUpdatesAsync();
                return betaInfo != null ? new UpdateResult(betaInfo, IsBeta: true) : null;
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
    }
}
