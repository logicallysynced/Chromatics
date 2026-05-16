using Chromatics.Extensions.RGB.NET.Devices.Yeelight;
using Chromatics.Extensions.RGB.NET.Devices.Yeelight.Protocol;
using Chromatics.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.ViewModels
{
    public partial class YeelightAdoptionDialogViewModel : ViewModelBase
    {
        // Discovered + already-adopted bulbs are merged into a single list,
        // pre-checked for any Id that's already adopted. The dialog reads
        // back the IsSelected state on Save to compute the new adoption set.
        public ObservableCollection<YeelightBulbItem> Bulbs { get; } = new();

        [ObservableProperty]
        private string _statusText;

        [ObservableProperty]
        private bool _isDiscovering;

        public YeelightAdoptionDialogViewModel()
        {
            LocalizationService.Instance.PropertyChanged += OnLocaleChanged;
            UpdateStatus();
        }

        private void OnLocaleChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizationService.Version))
                UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (IsDiscovering)
                StatusText = LocalizationService.Instance["Searching for Yeelight devices on your network..."];
            else if (Bulbs.Count == 0)
                StatusText = LocalizationService.Instance["No Yeelight devices found. Make sure each bulb has LAN Control enabled in the Yeelight or Mi Home app."];
            else
                StatusText = string.Format(LocalizationService.Instance["{0} Yeelight device(s) found."], Bulbs.Count);
        }

        public async Task StartDiscoveryAsync(IReadOnlyDictionary<string, YeelightAdoptedDevice> alreadyAdopted, CancellationToken ct)
        {
            IsDiscovering = true;
            UpdateStatus();
            Bulbs.Clear();

            // Seed with offline-but-adopted devices so the user can see them
            // even if they don't respond to discovery this run.
            foreach (var (id, dev) in alreadyAdopted)
            {
                var item = new YeelightBulbItem
                {
                    Id = id,
                    Label = dev.Label,
                    IpDisplay = LocalizationService.Instance["(offline)"],
                    LastPort = dev.LastPort > 0 ? dev.LastPort : 55443,
                    Model = YeelightModelCatalog.GetOrDefault(dev.Model).DisplayName,
                    FirmwareVersion = dev.FirmwareVersion,
                    Support = dev.Support?.ToArray() ?? Array.Empty<string>(),
                    IsSelected = true,
                    IsOnline = false,
                };
                Bulbs.Add(item);
            }

            var byId = new Dictionary<string, YeelightBulbItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Bulbs) byId[item.Id] = item;

            try
            {
                var results = await YeelightDiscovery.DiscoverAsync(TimeSpan.FromSeconds(3), ct);

                foreach (var dev in results)
                {
                    if (string.IsNullOrEmpty(dev.Id) || dev.Endpoint == null) continue;

                    if (byId.TryGetValue(dev.Id, out var existing))
                    {
                        existing.IpDisplay = dev.Endpoint.Address.ToString();
                        existing.LastPort = dev.Endpoint.Port;
                        existing.IsOnline = true;
                        if (!string.IsNullOrEmpty(dev.DisplayLabel)) existing.Label = dev.DisplayLabel;
                        if (!string.IsNullOrEmpty(dev.Model))
                            existing.Model = YeelightModelCatalog.GetOrDefault(dev.Model).DisplayName;
                        if (!string.IsNullOrEmpty(dev.FirmwareVersion)) existing.FirmwareVersion = dev.FirmwareVersion;
                        if (dev.Support != null)
                        {
                            var arr = new string[dev.Support.Count];
                            for (int i = 0; i < dev.Support.Count; i++) arr[i] = dev.Support[i];
                            existing.Support = arr;
                        }
                    }
                    else
                    {
                        var arr = Array.Empty<string>();
                        if (dev.Support != null)
                        {
                            arr = new string[dev.Support.Count];
                            for (int i = 0; i < dev.Support.Count; i++) arr[i] = dev.Support[i];
                        }
                        var item = new YeelightBulbItem
                        {
                            Id = dev.Id,
                            Label = string.IsNullOrEmpty(dev.DisplayLabel) ? dev.Id : dev.DisplayLabel,
                            IpDisplay = dev.Endpoint.Address.ToString(),
                            LastPort = dev.Endpoint.Port,
                            Model = YeelightModelCatalog.GetOrDefault(dev.Model).DisplayName,
                            FirmwareVersion = dev.FirmwareVersion,
                            Support = arr,
                            IsSelected = false,
                            IsOnline = true,
                        };
                        Bulbs.Add(item);
                        byId[item.Id] = item;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                StatusText = string.Format(LocalizationService.Instance["Discovery failed: {0}"], ex.Message);
                IsDiscovering = false;
                return;
            }

            IsDiscovering = false;
            UpdateStatus();
        }
    }

    public partial class YeelightBulbItem : ObservableObject
    {
        [ObservableProperty] private string _id;
        [ObservableProperty] private string _label;
        [ObservableProperty] private string _ipDisplay;
        [ObservableProperty] private int _lastPort = 55443;
        [ObservableProperty] private string _model;
        [ObservableProperty] private string _firmwareVersion;
        [ObservableProperty] private string[] _support = System.Array.Empty<string>();
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isOnline;
    }
}
