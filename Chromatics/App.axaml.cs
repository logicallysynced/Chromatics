using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Localization;
using Chromatics.Views;
using System;

namespace Chromatics
{
    public partial class App : Application
    {
        public static event Action<bool> ThemeVariantChanged;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            RefreshTheme();
            var settings = AppSettings.GetSettings();
            LocalizationService.Instance.SetLanguage(settings.systemLanguage);
        }

        public void RefreshTheme()
        {
            var settings = AppSettings.GetSettings();
            bool isDark;
            if (settings.systemTheme == Theme.System)
            {
                isDark = SystemHelpers.IsDarkModeEnabled();
            }
            else
            {
                isDark = settings.systemTheme == Theme.Dark;
            }
            RequestedThemeVariant = isDark
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
            ThemeVariantChanged?.Invoke(!isDark);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();
                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var settings = AppSettings.GetSettings();
                if (settings.trayonstartup)
                {
                    mainWindow.Hide();
                }
                else
                {
                    mainWindow.Show();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnTrayIconClicked(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnTrayShowClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnTrayCloseClick(object sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private void ShowMainWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is Window window)
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
            }
        }
    }
}
