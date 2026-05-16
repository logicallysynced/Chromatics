using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Models;
using Chromatics.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Chromatics.Views
{
    public partial class HueAdoptionDialog : Window
    {
        private readonly HueAdoptionDialogViewModel _vm;
        private CancellationTokenSource _cts;

        public List<HueAdoptedDevice> SelectedDevices { get; private set; } = new();
        public bool Saved { get; private set; }

        public HueAdoptionDialog() : this(string.Empty, string.Empty, new Dictionary<Guid, HueAdoptedDevice>()) { }

        public HueAdoptionDialog(string bridgeIp, string bridgeKey, IReadOnlyDictionary<Guid, HueAdoptedDevice> alreadyAdopted)
        {
            InitializeComponent();
            _vm = new HueAdoptionDialogViewModel();
            DataContext = _vm;

            Opened += async (_, __) =>
            {
                _cts = new CancellationTokenSource();
                if (!string.IsNullOrEmpty(bridgeIp) && !string.IsNullOrEmpty(bridgeKey))
                    await _vm.LoadBulbsAsync(bridgeIp, bridgeKey, alreadyAdopted, _cts.Token);
            };

            Closed += (_, __) =>
            {
                _cts?.Cancel();
                _cts?.Dispose();
            };
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
                .Select(b => new HueAdoptedDevice
                {
                    LightId = b.LightId,
                    Label = b.Label,
                    ModelId = b.ModelId,
                })
                .ToList();
            Saved = true;
            Close();
        }
    }
}
