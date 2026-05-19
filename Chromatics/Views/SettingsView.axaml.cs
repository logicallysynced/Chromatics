using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.ViewModels;
using Chromatics.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Chromatics.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private async void OnResetClick(object sender, RoutedEventArgs e)
        {
            var message = "Are you sure you wish to reset Chromatics? All settings, color palettes and layers will be reset.";

            // If Windows Dynamic Lighting is currently enabled, warn the user
            // that background control needs the sparse package re-registered
            // — and that registration only takes effect on the next launch,
            // so the first launch after Reset is foreground-only and a second
            // launch is required for background lighting to come back.
            var settings = AppSettings.GetSettings();
            if (settings.deviceDynamicLightingEnabled && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                message += "\n\nWindows Dynamic Lighting is currently enabled. Reset will deregister Chromatics from Windows, so background lighting needs two launches to come back: the first launch re-registers Chromatics, the second picks up the new identity. Foreground lighting works on both launches.";
            }

            var ok = await DialogService.ConfirmAsync("Reset Chromatics?", message);

            if (!ok) return;

            if (DataContext is SettingsViewModel vm)
            {
                vm.ResetChromatics();
                await DialogService.ShowAsync("Chromatics Reset", "Chromatics has been reset. Chromatics will now close.");
                ShutdownApp();
            }
        }

        private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
        {
            var s = AppSettings.GetSettings();
            var result = await Task.Run(() => UpdateService.CheckAsync(s.betaChannel));

            if (result == null)
            {
                await DialogService.ShowAsync("Up to Date", "Chromatics is up to date.");
                return;
            }

            var owner = this.FindAncestorOfType<Window>();
            if (owner == null) return;
            var dialog = new UpdateDialog(result);
            await dialog.ShowDialog(owner);
        }

        private async void OnCollectLogsClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            LogCollectionHelper.CollectionResult result = null;
            try
            {
                var window = this.FindAncestorOfType<Window>();
                var consoleLines = window?.DataContext is MainWindowViewModel mwvm
                    ? mwvm.Console.Entries.Select(x => x.Message).ToList()
                    : new List<string>();

                result = await LogCollectionHelper.CollectAsync(consoleLines);

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Chromatics Logs",
                    SuggestedFileName = Path.GetFileNameWithoutExtension(result.ZipPath),
                    DefaultExtension = "zip",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new("Zip archive") { Patterns = new[] { "*.zip" } }
                    }
                });

                if (file == null) return;

                var destination = file.TryGetLocalPath();
                if (string.IsNullOrEmpty(destination)) return;

                File.Copy(result.ZipPath, destination, overwrite: true);
                await DialogService.ShowAsync("Logs Saved", $"Diagnostic bundle saved to:\n{destination}");
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"Collect logs failed: {ex.Message}");
                await DialogService.ShowAsync("Collect Logs Failed", $"Could not create the diagnostic bundle:\n{ex.Message}");
            }
            finally
            {
                if (result != null) LogCollectionHelper.Cleanup(result.TempDirectory);
            }
        }

        private static void ShutdownApp()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
