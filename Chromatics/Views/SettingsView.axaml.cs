using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Chromatics.ViewModels;
using Chromatics.Views.Dialogs;
using System;

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
            var ok = await DialogService.ConfirmAsync(
                "Reset Chromatics?",
                "Are you sure you wish to reset Chromatics? All settings, color palettes and layers will be reset.");

            if (!ok) return;

            if (DataContext is SettingsViewModel vm)
            {
                vm.ResetChromatics();
                await DialogService.ShowAsync("Chromatics Reset", "Chromatics has been reset. Chromatics will now close.");
                ShutdownApp();
            }
        }

        private async void OnClearCacheClick(object sender, RoutedEventArgs e)
        {
            var ok = await DialogService.ConfirmAsync(
                "Clear Cache?",
                "Are you sure you wish to clear Chromatics cache?");

            if (!ok) return;

            if (DataContext is SettingsViewModel vm)
            {
                vm.ClearCache();
                await DialogService.ShowAsync("Cache Cleared", "Cache cleared. Chromatics will now close.");
                ShutdownApp();
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
