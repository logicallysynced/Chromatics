using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Helpers;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Chromatics.Views
{
    public partial class HueBridgeDialog : Window
    {
        public bool BridgeConfigured { get; private set; }
        public string BridgeIp { get; private set; }

        public HueBridgeDialog()
        {
            InitializeComponent();
        }

        public HueBridgeDialog(string initialIp) : this()
        {
            IpText.Text = initialIp ?? "";
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            BridgeConfigured = false;
            Close();
        }

        private async void OnSubmit(object sender, RoutedEventArgs e)
        {
            var ip = IpText.Text?.Trim() ?? "";
            if (!IPAddress.TryParse(ip, out _))
            {
                StatusText.Text = "That doesn't look like a valid IP address.";
                return;
            }

            SubmitButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            SubmitButton.Content = "Connecting…";
            StatusText.Text = "";

            // Use the singleton — the constructor throws if _instance is
            // already set (which it is after first launch / first connect).
            var provider = HueRGBDeviceProvider.Instance;
            provider.ClientDefinitions.Clear();
            var bridge = new HueClientDefinition(ip, "chromatics", "");
            provider.ClientDefinitions.Add(bridge);

            // Mirror the old 10s timeout race. If the provider faults after the
            // timeout wins, observe it so it surfaces in the log.
            var loadTask = Task.Run(() => RGBController.LoadDeviceProvider(provider));
            var winner = await Task.WhenAny(loadTask, Task.Delay(10000));

            if (winner == loadTask && loadTask.Result)
            {
                BridgeConfigured = true;
                BridgeIp = ip;
                Close();
                return;
            }

            if (winner != loadTask)
            {
                _ = loadTask.ContinueWith(
                    t => Logger.WriteConsole(LoggerTypes.Error, $"[Hue] LoadDeviceProvider faulted after timeout: {t.Exception?.GetBaseException()?.Message}"),
                    TaskContinuationOptions.OnlyOnFaulted);
                StatusText.Text = "Timed out. Press the bridge button and try again.";
            }
            else
            {
                Logger.WriteConsole(LoggerTypes.Error, "[Hue] LoadDeviceProvider returned false.");
                StatusText.Text = $"Unable to connect to the Hue bridge at {ip}.";
            }

            BridgeConfigured = false;
            SubmitButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            SubmitButton.Content = "Submit";
        }
    }
}
