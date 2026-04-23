using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Localization;
using Chromatics.Views;
using Chromatics.Views.Dialogs;
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
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                GameController.OnGameExited = () =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
                            d.Shutdown();

                        // Same rationale as OnTrayCloseClick: cooperative
                        // shutdown leaves background threads alive, which
                        // zombies the process and breaks the next launch.
                        try { Chromatics.Core.SentryService.Shutdown(); } catch { }
                        try { System.Diagnostics.Process.GetCurrentProcess().Kill(); } catch { }
                    });
                };

                var settings = AppSettings.GetSettings();
                if (settings.firstrun)
                {
                    // Defer MainWindow construction entirely until the wizard
                    // completes. Device providers are not enabled yet at this
                    // point, so constructing MainWindow (which boots the
                    // GameController / RGBController) would crash.
                    var wizard = new FirstRunDialog();
                    wizard.Closed += (_, _) =>
                    {
                        var mainWindow = new MainWindow();
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                    };
                    wizard.Show();
                }
                else
                {
                    var mainWindow = new MainWindow();
                    desktop.MainWindow = mainWindow;
                    if (settings.trayonstartup)
                        mainWindow.Hide();
                    else
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

            // desktop.Shutdown() ends the Avalonia message loop but leaves
            // background threads (Sentry.Profiling EventPipe, RGB.NET update
            // timer, Sharlayan polling, Hue update trigger) alive — none of
            // them respect a cooperative shutdown signal. Without an explicit
            // Process.Kill the "closed" Chromatics survives as a zombie that
            // holds the single-instance mutex AND Sentry's envelope cache,
            // making the next launch see "Already running" and silently
            // dropping crash events. SentryService.Shutdown does a sync
            // 3-second flush first so any pending events leave the wire
            // before we kill the process.
            try { Chromatics.Core.SentryService.Shutdown(); } catch { }
            try { System.Diagnostics.Process.GetCurrentProcess().Kill(); } catch { }
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
