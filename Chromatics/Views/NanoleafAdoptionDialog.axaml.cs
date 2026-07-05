using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf;
using Chromatics.ViewModels;
using System.Collections.Generic;
using System.Threading;

namespace Chromatics.Views
{
    public partial class NanoleafAdoptionDialog : Window
    {
        private readonly NanoleafAdoptionDialogViewModel _vm;
        private CancellationTokenSource _cts;

        public List<NanoleafAdoptedDevice> SelectedDevices { get; private set; } = new();
        public bool Saved { get; private set; }

        public NanoleafAdoptionDialog() : this(new List<NanoleafAdoptedDevice>()) { }

        public NanoleafAdoptionDialog(IEnumerable<NanoleafAdoptedDevice> alreadyPaired)
        {
            InitializeComponent();
            _vm = new NanoleafAdoptionDialogViewModel();
            DataContext = _vm;

            Opened += async (_, __) =>
            {
                _cts = new CancellationTokenSource();
                await _vm.StartDiscoveryAsync(alreadyPaired, _cts.Token);
            };

            Closed += (_, __) =>
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _vm.Dispose();
            };
        }

        private async void OnDiscoverAgain(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            // Preserve rows already holding a token across the re-run so a
            // fresh sweep doesn't drop the user's paired controllers.
            await _vm.StartDiscoveryAsync(_vm.GetAdopted(), _cts.Token);
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Saved = false;
            Close();
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            SelectedDevices = _vm.GetAdopted();
            Saved = true;
            Close();
        }
    }
}
