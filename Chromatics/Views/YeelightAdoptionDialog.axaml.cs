using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Extensions.RGB.NET.Devices.Yeelight;
using Chromatics.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Chromatics.Views
{
    public partial class YeelightAdoptionDialog : Window
    {
        private readonly YeelightAdoptionDialogViewModel _vm;
        private CancellationTokenSource _cts;

        public List<YeelightAdoptedDevice> SelectedDevices { get; private set; } = new();
        public bool Saved { get; private set; }

        public YeelightAdoptionDialog() : this(new Dictionary<string, YeelightAdoptedDevice>()) { }

        public YeelightAdoptionDialog(IReadOnlyDictionary<string, YeelightAdoptedDevice> alreadyAdopted)
        {
            InitializeComponent();
            _vm = new YeelightAdoptionDialogViewModel();
            DataContext = _vm;

            Opened += async (_, __) =>
            {
                _cts = new CancellationTokenSource();
                await _vm.StartDiscoveryAsync(alreadyAdopted, _cts.Token);
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
            // Preserve only the user's CHECKED bulbs across the re-run.
            // StartDiscoveryAsync seeds everything in `alreadyAdopted` as
            // IsSelected=true, so passing the full list (including bulbs
            // the user just unchecked) would resurrect their checks.
            // GroupBy tolerates duplicate row ids (CHROMATICS-1C class).
            var preserved = _vm.Bulbs
                .Where(b => b.IsSelected)
                .GroupBy(b => b.Id)
                .Select(g => g.Last())
                .ToDictionary(
                    b => b.Id,
                    b => new YeelightAdoptedDevice
                    {
                        Id = b.Id,
                        Label = b.Label,
                        LastIp = b.IsOnline ? b.IpDisplay : null,
                        LastPort = b.LastPort,
                        Model = b.Model,
                        FirmwareVersion = b.FirmwareVersion,
                        Support = b.Support?.ToList() ?? new List<string>(),
                    });
            await _vm.StartDiscoveryAsync(preserved, _cts.Token);
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Saved = false;
            Close();
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            SelectedDevices = _vm.Bulbs
                .Where(b => b.IsSelected)
                .Select(b => new YeelightAdoptedDevice
                {
                    Id = b.Id,
                    Label = b.Label,
                    LastIp = b.IsOnline ? b.IpDisplay : null,
                    LastPort = b.LastPort,
                    Model = b.Model,
                    FirmwareVersion = b.FirmwareVersion,
                    Support = b.Support?.ToList() ?? new List<string>(),
                })
                .ToList();
            Saved = true;
            Close();
        }
    }
}
