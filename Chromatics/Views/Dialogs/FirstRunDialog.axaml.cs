#nullable enable
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Localization;
using System;
using System.Linq;

namespace Chromatics.Views.Dialogs
{
    public partial class FirstRunDialog : Window
    {
        private bool _suppressLanguageWrite;

        public FirstRunDialog()
        {
            InitializeComponent();

            foreach (var tile in Tiles())
                tile.IsCheckedChanged += OnTileChanged;

            // Populate the language picker from the Language enum so it stays
            // in lock-step with the Settings → Language dropdown. Suppress the
            // SelectionChanged write-back during initial selection so we don't
            // re-persist the unchanged value on dialog open.
            var current = AppSettings.GetSettings().systemLanguage;
            var options = Enum.GetValues<Language>()
                .Select(l => new LanguageEntry(l, l.GetDisplayName()))
                .ToArray();
            _suppressLanguageWrite = true;
            LanguageBox.ItemsSource = options;
            LanguageBox.SelectedItem = options.FirstOrDefault(o => o.Value == current) ?? options[0];
            _suppressLanguageWrite = false;

            UpdateContinueState();
        }

        private sealed record LanguageEntry(Language Value, string DisplayName)
        {
            public override string ToString() => DisplayName;
        }

        private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_suppressLanguageWrite) return;
            if (LanguageBox.SelectedItem is not LanguageEntry entry) return;

            var s = AppSettings.GetSettings();
            s.systemLanguage = entry.Value;
            AppSettings.SaveSettings(s);
            // Bumps LocalizationService.Version, which all {loc:Tr Key='...'}
            // bindings in this dialog (and elsewhere) re-evaluate against — so
            // the dialog re-renders in the new language immediately.
            LocalizationService.Instance.SetLanguage(entry.Value);
        }

        private ToggleButton[] Tiles() =>
        [
            TileRazer, TileLogitech, TileCorsair, TileCoolermaster,
            TileSteelSeries, TileAsus, TileMsi, TileWooting, TileNovation, TileOpenRgb, TilePlayStation, TileQmkRawHid,
        ];

        private void OnTileChanged(object? sender, RoutedEventArgs e) => UpdateContinueState();

        private void UpdateContinueState()
        {
            bool anySelected = Tiles().Any(t => t.IsChecked == true);
            BtnContinue.IsEnabled = anySelected;
            HintText.IsVisible = !anySelected;
        }

        private void OnContinue(object? sender, RoutedEventArgs e)
        {
            var s = AppSettings.GetSettings();

            s.deviceRazerEnabled        = TileRazer.IsChecked        ?? false;
            s.deviceLogitechEnabled     = TileLogitech.IsChecked     ?? false;
            s.deviceCorsairEnabled      = TileCorsair.IsChecked      ?? false;
            s.deviceCoolermasterEnabled = TileCoolermaster.IsChecked ?? false;
            s.deviceSteelseriesEnabled  = TileSteelSeries.IsChecked  ?? false;
            s.deviceAsusEnabled         = TileAsus.IsChecked         ?? false;
            s.deviceMsiEnabled          = TileMsi.IsChecked          ?? false;
            s.deviceWootingEnabled      = TileWooting.IsChecked      ?? false;
            s.deviceNovationEnabled     = TileNovation.IsChecked     ?? false;
            s.deviceOpenRGBEnabled      = TileOpenRgb.IsChecked      ?? false;
            s.devicePlayStationEnabled  = TilePlayStation.IsChecked  ?? false;
            s.deviceQmkRawHidEnabled    = TileQmkRawHid.IsChecked    ?? false;

            // Hue and LIFX are deliberately omitted from the wizard — both
            // need bridge / network-discovery dialogs that are inappropriate
            // for first-run. Users enable them from Settings → Device
            // Providers when ready. QMK is fine here: discovery is local
            // (USB HID) and auto-adopt happens on first provider load.

            s.firstrun = false;
            AppSettings.SaveSettings(s);

            Close();
        }

        private void OnExit(object? sender, RoutedEventArgs e)
        {
            // Exit without writing firstrun=false so the wizard re-appears next launch.
            Environment.Exit(0);
        }
    }
}
