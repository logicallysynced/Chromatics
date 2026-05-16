using Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol;
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
    public partial class LifxAdoptionDialogViewModel : ViewModelBase
    {
        // Discovered + already-adopted bulbs are merged into a single list,
        // pre-checked for any MAC that's already adopted. The dialog reads
        // back the IsSelected state on OK to compute the new adoption set.
        public ObservableCollection<LifxBulbItem> Bulbs { get; } = new();

        [ObservableProperty]
        private string _statusText;

        [ObservableProperty]
        private bool _isDiscovering;

        public LifxAdoptionDialogViewModel()
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
                StatusText = LocalizationService.Instance["Searching for LIFX devices on your network..."];
            else if (Bulbs.Count == 0)
                StatusText = LocalizationService.Instance["No LIFX devices found. Check that they are powered on and connected to the same network."];
            else
                StatusText = string.Format(LocalizationService.Instance["{0} LIFX device(s) found."], Bulbs.Count);
        }

        // Run discovery, merging results with the supplied list of already-
        // adopted MACs. Pre-existing devices stay pre-checked; newly-found
        // devices are unchecked; devices that were adopted but aren't on the
        // network show with a "(offline)" suffix and are pre-checked so the
        // user keeps them by default.
        public async Task StartDiscoveryAsync(IReadOnlyDictionary<string, Chromatics.Extensions.RGB.NET.Devices.LIFX.LifxAdoptedDevice> alreadyAdopted, CancellationToken ct)
        {
            IsDiscovering = true;
            UpdateStatus();
            Bulbs.Clear();

            // Seed with offline-but-adopted devices so the user can see them
            // even if they don't respond to discovery this run.
            foreach (var (mac, dev) in alreadyAdopted)
            {
                var item = new LifxBulbItem
                {
                    Mac = mac,
                    Label = dev.Label,
                    IpDisplay = LocalizationService.Instance["(offline)"],
                    ProductId = dev.ProductId,
                    ProductName = LifxProductCatalog.GetOrDefault(dev.ProductId).Name,
                    ZoneCount = dev.ZoneCount,
                    IsSelected = true,
                    IsOnline = false,
                };
                Bulbs.Add(item);
            }

            // Track per-MAC state during the streaming callbacks.
            var byMac = new Dictionary<string, LifxBulbItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Bulbs) byMac[item.Mac] = item;

            try
            {
                var results = await LifxDiscovery.DiscoverAsync(
                    TimeSpan.FromSeconds(3),
                    onDeviceSeen: dev =>
                    {
                        // The discovery callback fires on a background thread.
                        // Marshalling to the UI thread is the dispatcher's job —
                        // we let the closing collection update do the final
                        // reconciliation rather than racing here.
                    },
                    ct);

                // Reconcile: update existing items, add new ones.
                foreach (var dev in results)
                {
                    if (byMac.TryGetValue(dev.Mac, out var existing))
                    {
                        existing.IpDisplay = dev.Endpoint?.Address.ToString() ?? "";
                        existing.IsOnline = true;
                        if (!string.IsNullOrEmpty(dev.Label) && dev.Label != $"LIFX ({dev.Mac})")
                            existing.Label = dev.Label;
                        if (dev.ProductId > 0)
                        {
                            existing.ProductId = dev.ProductId;
                            existing.ProductName = LifxProductCatalog.GetOrDefault(dev.ProductId).Name;
                        }
                        if (dev.ZoneCount > 0) existing.ZoneCount = dev.ZoneCount;
                    }
                    else
                    {
                        var item = new LifxBulbItem
                        {
                            Mac = dev.Mac,
                            Label = string.IsNullOrEmpty(dev.Label) ? dev.Mac : dev.Label,
                            IpDisplay = dev.Endpoint?.Address.ToString() ?? "",
                            ProductId = dev.ProductId,
                            ProductName = LifxProductCatalog.GetOrDefault(dev.ProductId).Name,
                            ZoneCount = dev.ZoneCount,
                            IsSelected = false,
                            IsOnline = true,
                        };
                        Bulbs.Add(item);
                        byMac[item.Mac] = item;
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

    public partial class LifxBulbItem : ObservableObject
    {
        [ObservableProperty] private string _mac;
        [ObservableProperty] private string _label;
        [ObservableProperty] private string _ipDisplay;
        [ObservableProperty] private uint _productId;
        [ObservableProperty] private string _productName;
        [ObservableProperty] private ushort _zoneCount;
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isOnline;
    }
}
