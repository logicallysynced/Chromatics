#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Chromatics.Helpers;
using System;
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

            var notes = result.Info.TargetFullRelease.NotesMarkdown;
            ChangelogMarkdown.Markdown = string.IsNullOrWhiteSpace(notes)
                ? "(No release notes provided.)"
                : notes;
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
