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

        // The update dialog prepends its own "## {version}" heading per
        // release, but notes embedded by older publishes start with the
        // version's changelog heading themselves and run to the end of
        // CHANGELOG.md - rendering both doubled the version line and
        // stacked older sections into every entry. Trim each release's
        // notes to just its own bullets: drop a leading heading that names
        // this version (any of the changelog heading shapes, 4-part
        // tolerated), then cut at the next "## " section. New publishes
        // embed bullets only, so this is a no-op for them.
        public static string TrimNotesToOwnSection(string notes, string version)
        {
            if (string.IsNullOrWhiteSpace(notes)) return string.Empty;
            var text = notes.Trim();

            // Leading horizontal rules would double up with the separator
            // the dialog inserts between entries.
            text = System.Text.RegularExpressions.Regex.Replace(
                text, @"\A(?:-{3,}[ \t]*\r?\n)+", string.Empty).TrimStart();

            // \b keeps a version from matching its own numeric prefix -
            // without it, "4.3.2" strips a "## 4.3.26" heading and adopts
            // that release's bullets.
            var ownHeading = new System.Text.RegularExpressions.Regex(
                @"^##\s+\[?v?" + System.Text.RegularExpressions.Regex.Escape(version) + @"(\.0)?\b\]?[^\n]*\r?\n?");
            text = ownHeading.Replace(text, string.Empty, 1).TrimStart();

            // Cut trailing sections, but never at position zero: a leading
            // heading still present here names a DIFFERENT version
            // (mislabelled asset), and cutting at zero would wipe the whole
            // entry to empty. Keep that heading with its own section and cut
            // at the one after it.
            foreach (System.Text.RegularExpressions.Match section in
                System.Text.RegularExpressions.Regex.Matches(
                    text, @"^##\s+\S", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                if (section.Index > 0)
                {
                    text = text[..section.Index].TrimEnd();
                    break;
                }
            }

            return text;
        }

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
#if PORTABLE_BUILD
            // Portable builds carry the com.logicallysynced.Chromatics.Portable
            // fusion identity, while the update feed serves nupkgs built with
            // the installer's com.logicallysynced.Chromatics identity.
            // Applying one of those nupkgs in-place would replace the portable's
            // Chromatics.exe with one whose fusion manifest points at the
            // installer's sparse package, leaving the portable unable to start
            // (Windows would reject it with "The process has no package
            // identity"). Portable users update by downloading a fresh
            // Chromatics-win-Portable-X.Y.Z.zip from the website.
            Logger.WriteVerbose("[Update] Portable build — in-app update check skipped.");
            return null;
#else
            var probeMgr = new UpdateManager(new SimpleWebSource(StableFeedUrl));
            if (!probeMgr.IsInstalled)
                return null;

            var stableResult = await CheckFeed(StableFeedUrl, isBeta: false);
            var betaResult   = includeBeta ? await CheckFeed(BetaFeedUrl, isBeta: true) : null;

            Logger.WriteVerbose(
                $"[Update] stable={FormatResult(stableResult)} beta={FormatResult(betaResult)} includeBeta={includeBeta} markerBeta={IsBetaChannel()}");

            if (stableResult != null && betaResult != null)
            {
                var sv = stableResult.Info.TargetFullRelease.Version;
                var bv = betaResult.Info.TargetFullRelease.Version;
                return sv >= bv ? stableResult : betaResult;
            }

            return stableResult ?? betaResult;
#endif
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
