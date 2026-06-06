#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Chromatics.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Views.Dialogs
{
    public partial class UpdateDialog : Window
    {
        private readonly UpdateResult _result = null!;
        private bool _installing;

        public UpdateDialog() => InitializeComponent();

        public UpdateDialog(UpdateResult result)
        {
            InitializeComponent();
            _result = result;

            var current = typeof(UpdateDialog).Assembly.GetName().Version;
            var next = result.Info.TargetFullRelease.Version;
            VersionText.Text = $"Current: {current?.Major}.{current?.Minor}.{current?.Build}   →   New: {next.Major}.{next.Minor}.{next.Patch}";

            if (result.IsPreRelease)
            {
                BetaBadge.IsVisible = true;
                BetaNote.IsVisible  = true;
            }

            ChangelogMarkdown.Markdown = BuildCumulativeChangelog(result);
        }

        // Concatenate notes from every release between the user's installed
        // version and the target — Velopack stores the changelog markdown on
        // each VelopackAsset at pack time, so DeltasToTarget gives us all
        // intermediate version notes. Showing only TargetFullRelease.Notes
        // would skip every release the user hasn't seen yet, which is the
        // common case for someone who skipped a few updates.
        private static string BuildCumulativeChangelog(UpdateResult result)
        {
            var versions = new List<(Velopack.SemanticVersion Version, string Notes)>();

            void AddIfNotes(Velopack.SemanticVersion? v, string? notes)
            {
                if (v == null || string.IsNullOrWhiteSpace(notes)) return;
                if (versions.Any(e => e.Version.Equals(v))) return;
                versions.Add((v, notes!));
            }

            AddIfNotes(result.Info.TargetFullRelease?.Version, result.Info.TargetFullRelease?.NotesMarkdown);

            if (result.Info.DeltasToTarget != null)
                foreach (var d in result.Info.DeltasToTarget)
                    AddIfNotes(d.Version, d.NotesMarkdown);

            if (versions.Count == 0)
                return "(No release notes provided.)";

            // Newest first — same ordering as the CHANGELOG.md file the user
            // is used to seeing on GitHub.
            versions.Sort((a, b) => b.Version.CompareTo(a.Version));

            var sb = new StringBuilder();
            for (int i = 0; i < versions.Count; i++)
            {
                if (i > 0) sb.AppendLine().AppendLine("---").AppendLine();
                sb.Append("## ").AppendLine(versions[i].Version.ToString());
                sb.AppendLine();
                sb.AppendLine(versions[i].Notes.Trim());
            }
            return sb.ToString();
        }

        private async void OnInstall(object sender, RoutedEventArgs e)
        {
            if (_installing) return;
            _installing = true;

            InstallButton.IsEnabled = false;
            LaterButton.IsEnabled   = false;
            DownloadProgress.IsVisible = true;
            StatusText.IsVisible = true;
            StatusText.Text = "Downloading…";

            try
            {
                await UpdateService.DownloadAndApplyAsync(_result, pct =>
                    Dispatcher.UIThread.Post(() => DownloadProgress.Value = pct));

                // ApplyUpdatesAndRestart exits the process — this line is never reached.
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Update failed: {ex.Message}";
                InstallButton.IsEnabled = true;
                LaterButton.IsEnabled   = true;
                DownloadProgress.IsVisible = false;
                _installing = false;
            }
        }

        private void OnLater(object sender, RoutedEventArgs e) => Close();
    }
}
