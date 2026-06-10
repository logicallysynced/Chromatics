using Chromatics.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using HueApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.ViewModels
{
    public partial class HueAdoptionDialogViewModel : ViewModelBase, IDisposable
    {
        public ObservableCollection<HueBulbItem> Bulbs { get; } = new();

        [ObservableProperty]
        private string _statusText;

        [ObservableProperty]
        private bool _isLoading;

        public HueAdoptionDialogViewModel()
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
            if (IsLoading)
                StatusText = LocalizationService.Instance["Loading lights from your Hue bridge..."];
            else if (Bulbs.Count == 0)
                StatusText = LocalizationService.Instance["No lights found on this bridge."];
            else
                StatusText = string.Format(LocalizationService.Instance["{0} Hue light(s) on this bridge."], Bulbs.Count);
        }

        public void Dispose()
        {
            LocalizationService.Instance.PropertyChanged -= OnLocaleChanged;
        }

        // Query the bridge for its lights, merging with the user's already-
        // adopted set so previously-adopted bulbs come back pre-checked. New
        // lights since the last adoption are unchecked by default.
        public async Task LoadBulbsAsync(string bridgeIp, string bridgeKey, IReadOnlyDictionary<Guid, Chromatics.Extensions.RGB.NET.Devices.Hue.HueAdoptedDevice> alreadyAdopted, CancellationToken ct)
        {
            IsLoading = true;
            UpdateStatus();
            Bulbs.Clear();

            try
            {
                var api = new LocalHueApi(bridgeIp, bridgeKey);
                var lights = await api.Light.GetAllAsync();
                var devices = await api.Device.GetAllAsync();

                var modelByDevice = new Dictionary<Guid, string>();
                foreach (var d in devices.Data)
                    modelByDevice[d.Id] = d.ProductData?.ModelId ?? "";

                foreach (var light in lights.Data)
                {
                    string model = "";
                    if (light.Owner != null && modelByDevice.TryGetValue(light.Owner.Rid, out var m))
                        model = m;

                    bool wasAdopted = alreadyAdopted.ContainsKey(light.Id);

                    Bulbs.Add(new HueBulbItem
                    {
                        LightId = light.Id,
                        Label = light.Metadata?.Name ?? light.Id.ToString(),
                        ModelId = !string.IsNullOrEmpty(model) ? model : (light.Type ?? ""),
                        IsSelected = wasAdopted,
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                StatusText = string.Format(LocalizationService.Instance["Couldn't read lights from the bridge: {0}"], ex.Message);
                IsLoading = false;
                return;
            }

            IsLoading = false;
            UpdateStatus();
        }
    }

    public partial class HueBulbItem : ObservableObject
    {
        [ObservableProperty] private Guid _lightId;
        [ObservableProperty] private string _label;
        [ObservableProperty] private string _modelId;
        [ObservableProperty] private bool _isSelected;
    }
}
