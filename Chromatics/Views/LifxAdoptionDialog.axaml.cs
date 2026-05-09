using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Models;
using Chromatics.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Chromatics.Views
{
    public partial class LifxAdoptionDialog : Window
    {
        private readonly LifxAdoptionDialogViewModel _vm;
        private CancellationTokenSource _cts;

        public List<LifxAdoptedDevice> SelectedDevices { get; private set; } = new();
        public bool Saved { get; private set; }

        public LifxAdoptionDialog() : this(new Dictionary<string, LifxAdoptedDevice>()) { }

        public LifxAdoptionDialog(IReadOnlyDictionary<string, LifxAdoptedDevice> alreadyAdopted)
        {
            InitializeComponent();
            _vm = new LifxAdoptionDialogViewModel();
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
            };
        }

        private async void OnDiscoverAgain(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            // Preserve only the user's CHECKED bulbs across the re-run.
            // StartDiscoveryAsync seeds everything in `alreadyAdopted` as
            // IsSelected=true, so passing the full list (including bulbs
            // the user just unchecked) would resurrect their checks. Bulbs
            // not in the dict get re-added by discovery as fresh entries
            // with IsSelected=false, exactly matching their state before
            // the re-run.
            var preserved = _vm.Bulbs
                .Where(b => b.IsSelected)
                .ToDictionary(
                    b => b.Mac,
                    b => new LifxAdoptedDevice
                    {
                        Mac = b.Mac, Label = b.Label,
                        LastIp = b.IsOnline ? b.IpDisplay : null,
                        ProductId = b.ProductId, ZoneCount = b.ZoneCount,
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
                .Select(b => new LifxAdoptedDevice
                {
                    Mac = b.Mac,
                    Label = b.Label,
                    LastIp = b.IsOnline ? b.IpDisplay : null,
                    ProductId = b.ProductId,
                    ZoneCount = b.ZoneCount,
                })
                .ToList();
            Saved = true;
            Close();
        }
    }
}
