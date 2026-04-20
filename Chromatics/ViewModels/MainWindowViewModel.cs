using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.ViewModels.Mapping;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Chromatics.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        [ObservableProperty]
        private string _title = "Chromatics";

        [ObservableProperty]
        private string _headerText = "Chromatics";

        [ObservableProperty]
        private bool _isLightTheme;

        public ConsoleViewModel Console { get; }
        public EffectsViewModel Effects { get; }
        public SettingsViewModel Settings { get; }
        public PaletteViewModel Palette { get; }
        public MappingViewModel Mapping { get; }

        public MainWindowViewModel()
        {
            Console = new ConsoleViewModel();
            Effects = new EffectsViewModel();
            Settings = new SettingsViewModel();
            Palette = new PaletteViewModel();
            Mapping = new MappingViewModel();

            // DeviceConnectionChanged subscription is deferred to InitializeAfterRgb()
            // so that device events fired during RGBController.Setup() (which runs
            // on a background thread) don't trigger RefreshDevices before
            // LoadMappings has populated _layers — that would create default layers
            // for every device on every launch, overwriting any user customisation.

            var version = typeof(MainWindowViewModel).Assembly.GetName().Version;
            if (version != null)
            {
                Title = $"Chromatics {version.Major}.{version.Minor}.{version.Build}";
            }

            _isLightTheme = Avalonia.Application.Current?.ActualThemeVariant
                            != Avalonia.Styling.ThemeVariant.Dark;
            App.ThemeVariantChanged += OnThemeVariantChanged;
        }

        // Called once RGBController.Setup has finished (from MainWindow.OnOpened).
        // At this point device enumeration is synchronous and LoadMappings can
        // migrate inline without racing the default-seeding in RefreshDevices.
        public void InitializeAfterRgb()
        {
            if (!MappingLayers.LoadMappings())
            {
                Logger.WriteConsole(LoggerTypes.System, "No layer file found. Defaults will be created per device.");
            }

            // Subscribe only after LoadMappings so Setup()-era device events
            // don't race the layer store before it's populated.
            RGBController.DeviceConnectionChanged += OnDeviceConnectionChanged;

            RefreshMappingDevices();
        }

        private void OnThemeVariantChanged(bool isLight)
        {
            IsLightTheme = isLight;
        }

        private void OnDeviceConnectionChanged(object sender, EventArgs e)
        {
            // RGBController raises this from whichever thread the RGB.NET provider
            // happens to be on. Marshal onto the UI thread before touching the
            // ObservableCollections that bind to Mapping.
            Avalonia.Threading.Dispatcher.UIThread.Post(RefreshMappingDevices);
        }

        private void RefreshMappingDevices()
        {
            Mapping.RefreshDevices(RGBController.GetLiveDevices());
        }

        public void Dispose()
        {
            App.ThemeVariantChanged -= OnThemeVariantChanged;
            RGBController.DeviceConnectionChanged -= OnDeviceConnectionChanged;
            Console.Dispose();
            Mapping.Dispose();
        }
    }
}
