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
            // Program.Main. Offload BOTH RGBController.Setup (device enumeration)
            // AND KeyController.Setup to a background task — KeyController's
            // SetWindowsHookEx path touches Process.MainModule, which blocks
            // for hundreds of ms to seconds on Windows while the OS resolves
            // the loaded-module list, freezing the UI and tab switching.
            Logger.WriteConsole(LoggerTypes.System, "Chromatics is starting up..");

            await Task.Run(() =>
            {
                Chromatics.Core.SentryService.RunInstrumented(
                    "app.startup",
                    "Chromatics backend init (keyboard hook + RGB provider load)",
                    () =>
                    {
                        // LoadMappings runs first so it's populated BEFORE
                        // RGBController.Setup fires DeviceConnectionChanged.
                        // Off the UI thread because it deserialises the whole
                        // layers.chromatics4 file — the single largest UI-thread
                        // blocker during startup. LoadMappings uses
                        // ConcurrentDictionary internally so concurrent reads
                        // from the UI thread are safe.
                        if (!Chromatics.Layers.MappingLayers.LoadMappings())
                        {
                            Logger.WriteConsole(LoggerTypes.System, "No layer file found. Defaults will be created per device.");
                        }
                        KeyController.Setup();
                        RGBController.Setup();
                    });
            });
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

            // desktop.Shutdown() ends Avalonia's loop but background threads
            // (Sentry.Profiling EventPipe, RGB.NET update timer that wasn't
            // fully torn down by RGBController.Unload, Sharlayan polling,
            // Hue update trigger) keep the process alive — and Environment.Exit
            // can stall on Sentry's AppDomain.ProcessExit flush handler. Without
            // an explicit Process.Kill the "closed" Chromatics survives as a
            // zombie that holds the single-instance mutex AND Sentry's envelope
            // cache, causing both "Already running" prompts on the next launch
            // AND silent crash-event drops. SentryService.Shutdown does a sync
            // 3-second flush first so any pending events leave the wire before
            // we kill the process.
            try { Chromatics.Core.SentryService.Shutdown(); } catch { }
            try { Process.GetCurrentProcess().Kill(); } catch { }
        }

        private void OnHelpClick(object sender, RoutedEventArgs e)
        {
            const string url = "https://docs.chromaticsffxiv.com/chromatics-4";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
