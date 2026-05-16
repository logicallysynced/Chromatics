#nullable enable
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Extensions.RGB.NET.Devices.LIFX;
using Chromatics.Extensions.RGB.NET.Devices.Yeelight;
using Chromatics.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Views.Dialogs
{
    public partial class FirstRunDialog : Window
    {
        private bool _suppressLanguageWrite;

        // Re-entrancy guards for the network-discovery tiles. Setting
        // IsChecked=false from inside an IsCheckedChanged handler re-fires
        // the same handler — without these flags the tile would loop
        // (and re-prompt for discovery on every loop).
        private bool _suppressHueChange;
        private bool _suppressLifxChange;
        private bool _suppressYeelightChange;

        // Captured discovery results per network provider. The OnContinue
        // handler reads these to write the adoption lists into settings —
        // separate from the tile state because we want the tile to
        // remain enabled across repeated open/close cycles within the
        // same dialog session.
        private List<HueAdoptedDevice>? _huePending;
        private string? _hueBridgeIp;
        private string? _hueBridgeKey;
        private List<LifxAdoptedDevice>? _lifxPending;
        private List<YeelightAdoptedDevice>? _yeelightPending;

        public FirstRunDialog()
        {
            InitializeComponent();

            foreach (var tile in LocalTiles())
                tile.IsCheckedChanged += OnTileChanged;

            // Network-discovery tiles get their own handler that runs
            // discovery on check and untoggles on empty / cancel.
            TileHue.IsCheckedChanged += OnHueTileChanged;
            TileLifx.IsCheckedChanged += OnLifxTileChanged;
            TileYeelight.IsCheckedChanged += OnYeelightTileChanged;
            TileAlienware.IsCheckedChanged += OnTileChanged;
            TileDynamicLighting.IsCheckedChanged += OnTileChanged;

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

        // Local-discovery tiles (USB / SDK init / no network probe). The
        // network-discovery tiles (Hue / LIFX / Yeelight) have their own
        // handlers below.
        private ToggleButton[] LocalTiles() =>
        [
            TileRazer, TileLogitech, TileCorsair, TileCoolermaster,
            TileSteelSeries, TileAsus, TileMsi, TileWooting, TileNovation, TileOpenRgb, TilePlayStation, TileQmkRawHid,
        ];

        // All tiles, used by UpdateContinueState to compute "any selected".
        private ToggleButton[] AllTiles() =>
        [
            TileRazer, TileLogitech, TileCorsair, TileCoolermaster,
            TileSteelSeries, TileAsus, TileMsi, TileWooting, TileNovation, TileOpenRgb, TilePlayStation, TileQmkRawHid,
            TileHue, TileLifx, TileYeelight, TileAlienware, TileDynamicLighting,
        ];

        private void OnTileChanged(object? sender, RoutedEventArgs e) => UpdateContinueState();

        // ── Network discovery flows ──────────────────────────────────
        //
        // Each handler runs the same discovery + adoption pipeline the
        // Settings page uses. On success, it stores the adoption result
        // for the OnContinue handler to write to settings. On failure
        // (cancel / no devices found) it un-checks the tile so the
        // user can see the discovery didn't take, and so Continue
        // doesn't enable an empty provider.

        private async void OnHueTileChanged(object? sender, RoutedEventArgs e)
        {
            if (_suppressHueChange) { UpdateContinueState(); return; }
            if (TileHue.IsChecked != true)
            {
                _huePending = null;
                _hueBridgeIp = null;
                _hueBridgeKey = null;
                UpdateContinueState();
                return;
            }

            var s = AppSettings.GetSettings();
            try
            {
                var bridgeDlg = new HueBridgeDialog(s.deviceHueBridgeIP);
                await bridgeDlg.ShowDialog(this);
                if (!bridgeDlg.BridgeConfigured) { UncheckHue(); return; }

                _hueBridgeIp = bridgeDlg.BridgeIp;
                _hueBridgeKey = string.IsNullOrEmpty(bridgeDlg.BridgeKey) ? s.deviceHueBridgeClientKey : bridgeDlg.BridgeKey;

                var alreadyAdopted = (s.deviceHueAdoptedDevices ?? new List<HueAdoptedDevice>())
                    .ToDictionary(d => d.LightId, d => d);

                var adoptDlg = new HueAdoptionDialog(_hueBridgeIp ?? "", _hueBridgeKey ?? "", alreadyAdopted);
                await adoptDlg.ShowDialog(this);

                if (!adoptDlg.Saved || adoptDlg.SelectedDevices == null || adoptDlg.SelectedDevices.Count == 0)
                {
                    UncheckHue();
                    return;
                }
                _huePending = adoptDlg.SelectedDevices;
            }
            catch
            {
                UncheckHue();
                return;
            }
            UpdateContinueState();
        }

        private void UncheckHue()
        {
            _suppressHueChange = true;
            try { TileHue.IsChecked = false; } finally { _suppressHueChange = false; }
            _huePending = null;
            _hueBridgeIp = null;
            _hueBridgeKey = null;
            UpdateContinueState();
        }

        private async void OnLifxTileChanged(object? sender, RoutedEventArgs e)
        {
            if (_suppressLifxChange) { UpdateContinueState(); return; }
            if (TileLifx.IsChecked != true)
            {
                _lifxPending = null;
                UpdateContinueState();
                return;
            }

            try
            {
                var dlg = new LifxAdoptionDialog(new Dictionary<string, LifxAdoptedDevice>());
                await dlg.ShowDialog(this);
                if (!dlg.Saved || dlg.SelectedDevices == null || dlg.SelectedDevices.Count == 0)
                {
                    UncheckLifx();
                    return;
                }
                _lifxPending = dlg.SelectedDevices;
            }
            catch
            {
                UncheckLifx();
                return;
            }
            UpdateContinueState();
        }

        private void UncheckLifx()
        {
            _suppressLifxChange = true;
            try { TileLifx.IsChecked = false; } finally { _suppressLifxChange = false; }
            _lifxPending = null;
            UpdateContinueState();
        }

        private async void OnYeelightTileChanged(object? sender, RoutedEventArgs e)
        {
            if (_suppressYeelightChange) { UpdateContinueState(); return; }
            if (TileYeelight.IsChecked != true)
            {
                _yeelightPending = null;
                UpdateContinueState();
                return;
            }

            try
            {
                var dlg = new YeelightAdoptionDialog(new Dictionary<string, YeelightAdoptedDevice>());
                await dlg.ShowDialog(this);
                if (!dlg.Saved || dlg.SelectedDevices == null || dlg.SelectedDevices.Count == 0)
                {
                    UncheckYeelight();
                    return;
                }
                _yeelightPending = dlg.SelectedDevices;
            }
            catch
            {
                UncheckYeelight();
                return;
            }
            UpdateContinueState();
        }

        private void UncheckYeelight()
        {
            _suppressYeelightChange = true;
            try { TileYeelight.IsChecked = false; } finally { _suppressYeelightChange = false; }
            _yeelightPending = null;
            UpdateContinueState();
        }

        private void UpdateContinueState()
        {
            bool anySelected = AllTiles().Any(t => t.IsChecked == true);
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
            s.deviceAlienwareEnabled       = TileAlienware.IsChecked       ?? false;
            s.deviceDynamicLightingEnabled = TileDynamicLighting.IsChecked ?? false;

            // Network-discovery providers: persist the adoption results
            // captured during the in-dialog discovery flows. The tile's
            // checked state is the source of truth for "should this
            // provider load on next start" — we only persist the adoption
            // payload when the tile is checked AND discovery succeeded.
            s.deviceHueEnabled = (TileHue.IsChecked == true) && _huePending != null;
            if (s.deviceHueEnabled)
            {
                if (!string.IsNullOrEmpty(_hueBridgeIp)) s.deviceHueBridgeIP = _hueBridgeIp;
                if (!string.IsNullOrEmpty(_hueBridgeKey)) s.deviceHueBridgeClientKey = _hueBridgeKey;
                s.deviceHueAdoptedDevices = _huePending!;
            }

            s.deviceLifxEnabled = (TileLifx.IsChecked == true) && _lifxPending != null;
            if (s.deviceLifxEnabled)
                s.deviceLifxAdoptedDevices = _lifxPending!;

            s.deviceYeelightEnabled = (TileYeelight.IsChecked == true) && _yeelightPending != null;
            if (s.deviceYeelightEnabled)
                s.deviceYeelightAdoptedDevices = _yeelightPending!;

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
