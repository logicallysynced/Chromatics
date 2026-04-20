using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.ViewModels;
using Chromatics.Views.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Chromatics.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            Opened += OnOpened;
            Closed += OnClosed;
            Closing += OnClosing;
        }

        private async void OnOpened(object sender, EventArgs e)
        {
            // Match Fm_MainWindow's bring-up order so backend subsystems initialize
            // exactly as they did under WinForms. Settings are already loaded by
            // Program.Main — RGBController.Setup is the only long-running piece,
            // keep it off the UI thread.
            Logger.WriteConsole(LoggerTypes.System, "Chromatics is starting up..");

            KeyController.Setup();
            await Task.Run(() => RGBController.Setup());
            GameController.Setup();

            // Defer the VM population until after the first full layout/render
            // cycle has completed. If we run InitializeAfterRgb inline here, the
            // Mappings tab content (an unrealized TabItem on the first pass) is
            // still mid-measure when we mutate its bound ObservableCollections
            // — the container generator ends up producing blank rows until
            // something forces a re-realization (theme toggle, tray-hide/show).
            // Loaded priority fires after layout settles, so the ItemsControls
            // see a populated collection from their first container pass.
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () => (DataContext as MainWindowViewModel)?.InitializeAfterRgb(),
                Avalonia.Threading.DispatcherPriority.Loaded);

            if (AppSettings.GetSettings().checkupdates)
                _ = CheckForUpdateAsync();
        }

        private async Task CheckForUpdateAsync()
        {
            var includeBeta = AppSettings.GetSettings().betaChannel;
            var result = await UpdateService.CheckAsync(includeBeta);
            if (result == null) return;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new UpdateDialog(result);
                await dialog.ShowDialog(this);
            });
        }

        private void OnClosing(object sender, WindowClosingEventArgs e)
        {
            // Mirror the old Fm_MainWindow behavior: if minimize-to-tray is enabled
            // and the window is being closed by the user (red X), hide to tray
            // instead. Explicit shutdown (tray → Close menu) uses
            // desktop.Shutdown() and this handler is bypassed.
            var settings = AppSettings.GetSettings();
            if (!settings.minimizetray) return;
            if (e.CloseReason != WindowCloseReason.WindowClosing) return;

            e.Cancel = true;
            Hide();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            GameController.Exit();
            KeyController.Stop();
            RGBController.Unload();

            (DataContext as MainWindowViewModel)?.Dispose();

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private void OnHelpClick(object sender, RoutedEventArgs e)
        {
            const string url = "https://docs.chromaticsffxiv.com/chromatics-4";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
