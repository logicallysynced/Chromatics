#nullable enable
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Chromatics.Core;
using System;
using System.Linq;

namespace Chromatics.Views.Dialogs
{
    public partial class FirstRunDialog : Window
    {
        public FirstRunDialog()
        {
            InitializeComponent();

            foreach (var tile in Tiles())
                tile.IsCheckedChanged += OnTileChanged;

            UpdateContinueState();
        }

        private ToggleButton[] Tiles() =>
        [
            TileRazer, TileLogitech, TileCorsair, TileCoolermaster,
            TileSteelSeries, TileAsus, TileMsi, TileWooting, TileNovation, TileOpenRgb,
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

            // Hue is deliberately omitted from the wizard — it needs the
            // bridge-pairing dialog which is inappropriate for first-run flow.
            // Users enable Hue from Settings → Device Providers when ready.

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
