using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Localization;
using HueApi;
using System;
using System.Net;
using System.Threading;

namespace Chromatics.Views
{
    public partial class HueBridgeDialog : Window
    {
        public bool BridgeConfigured { get; private set; }
        public string BridgeIp { get; private set; }
        public string BridgeKey { get; private set; }

        private CancellationTokenSource _discoveryCts;

        public HueBridgeDialog() : this(initialIp: null) { }

        public HueBridgeDialog(string initialIp)
        {
            InitializeComponent();
            IpText.Text = initialIp ?? "";

            // Run cloud discovery as soon as the dialog is on screen so the
            // list appears progressively. Manual entry stays available the
            // whole time as a fallback.
            Opened += async (_, __) =>
            {
                _discoveryCts = new CancellationTokenSource();
                try
                {
                    var bridges = await HueBridgeDiscovery.DiscoverAsync(_discoveryCts.Token);
                    DiscoveryProgress.IsVisible = false;

                    if (bridges.Count == 0)
                    {
                        DiscoveryStatusText.Text = LocalizationService.Instance["No Hue bridges found automatically. Enter your bridge IP below to connect manually."];
                        return;
                    }

                    DiscoveryStatusText.Text = string.Format(
                        LocalizationService.Instance["{0} Hue bridge(s) found on your network."],
                        bridges.Count);
                    DiscoveredBridgesList.ItemsSource = bridges;
                    DiscoveredBridgesPanel.IsVisible = true;

                    // Pre-fill the manual IP textbox with the first discovered
                    // bridge so a single-bridge user can just press Submit.
                    if (string.IsNullOrEmpty(IpText.Text))
                        IpText.Text = bridges[0].InternalIp;
                }
                catch (Exception)
                {
                    DiscoveryProgress.IsVisible = false;
                    DiscoveryStatusText.Text = LocalizationService.Instance["No Hue bridges found automatically. Enter your bridge IP below to connect manually."];
                }
            };

            Closed += (_, __) =>
            {
                _discoveryCts?.Cancel();
                _discoveryCts?.Dispose();
            };
        }

        private void OnUseDiscoveredBridge(object sender, RoutedEventArgs e)
        {
            // Tag carries the IP. Plant it in the manual textbox so the
            // existing Submit path can run unmodified — single source of
            // truth for the IP that will be paired against.
            if (sender is Button b && b.Tag is string ip)
                IpText.Text = ip;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            BridgeConfigured = false;
            Close();
        }

        // Pair against the bridge directly (no provider load) so the caller
        // can run the adoption dialog before any bulbs are attached to the
        // surface. Returns BridgeIp + BridgeKey on success — the caller
        // saves them and proceeds to adoption.
        //
        // LocalHueApi.RegisterAsync does the link-button handshake; the
        // bridge rejects the call until the physical button has been
        // pressed in the last ~30s. The exception type tells us which
        // failure mode to surface to the user.
        private async void OnSubmit(object sender, RoutedEventArgs e)
        {
            var ip = IpText.Text?.Trim() ?? "";
            if (!IPAddress.TryParse(ip, out _))
            {
                StatusText.Text = LocalizationService.Instance["That doesn't look like a valid IP address."];
                return;
            }

            SubmitButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            SubmitButton.Content = LocalizationService.Instance["Connecting..."];
            StatusText.Text = "";

            try
            {
                var regResult = await LocalHueApi.RegisterAsync(ip, "chromatics", "RGB.NET");
                if (regResult == null || string.IsNullOrEmpty(regResult.Username))
                {
                    StatusText.Text = LocalizationService.Instance["Couldn't pair with the bridge. Press the button on the bridge and try again."];
                }
                else
                {
                    BridgeConfigured = true;
                    BridgeIp = ip;
                    BridgeKey = regResult.Username;
                    Close();
                    return;
                }
            }
            catch (HueApi.Models.Exceptions.LinkButtonNotPressedException)
            {
                StatusText.Text = LocalizationService.Instance["Press the button on the bridge, then click Submit again."];
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error, $"[Hue] Bridge pair failed: {ex.Message}");
                StatusText.Text = string.Format(LocalizationService.Instance["Unable to connect to the Hue bridge at {0}."], ip);
            }

            BridgeConfigured = false;
            SubmitButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            SubmitButton.Content = LocalizationService.Instance["Submit"];
        }
    }
}
