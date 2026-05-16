using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Chromatics.ViewModels
{
    // ViewModel for the copy-layers dialog. Owns the list of legal
    // destination devices (filtered by IsCopyAllowed), the editable
    // LedId mapping rows, and the computed counters that surface at
    // the bottom of the dialog ("3 layers will be copied; 2 source
    // LEDs have no destination and will be dropped").
    //
    // The View binds two ComboBoxes (source / destination) and an
    // ItemsControl of MappingRow entries; the dialog code-behind
    // commits the copy via LayerCopier.Apply on Confirm.
    public partial class CopyLayersDialogViewModel : ViewModelBase
    {
        public CopyLayersDialogViewModel(IReadOnlyDictionary<Guid, IRGBDevice> connectedDevices, Guid initialSourceGuid)
        {
            _connectedDevices = connectedDevices ?? new Dictionary<Guid, IRGBDevice>();

            foreach (var (guid, device) in _connectedDevices)
            {
                Devices.Add(new DeviceItem
                {
                    DeviceId = guid,
                    Name = device.DeviceInfo?.DeviceName ?? "Device",
                    DeviceType = device.DeviceInfo?.DeviceType ?? RGBDeviceType.Unknown,
                });
            }

            SelectedSource = Devices.FirstOrDefault(d => d.DeviceId == initialSourceGuid)
                          ?? Devices.FirstOrDefault();
            RebuildDestinationOptions();
            SelectedDestination = AvailableDestinations.FirstOrDefault();
            RebuildMappings();
        }

        private readonly IReadOnlyDictionary<Guid, IRGBDevice> _connectedDevices;

        public ObservableCollection<DeviceItem> Devices { get; } = new();
        public ObservableCollection<DeviceItem> AvailableDestinations { get; } = new();
        public ObservableCollection<MappingRow> Mappings { get; } = new();
        public ObservableCollection<LedId> AvailableDestLedIds { get; } = new();

        [ObservableProperty]
        private DeviceItem _selectedSource;

        [ObservableProperty]
        private DeviceItem _selectedDestination;

        [ObservableProperty]
        private string _summaryText;

        partial void OnSelectedSourceChanged(DeviceItem value)
        {
            RebuildDestinationOptions();
            // Reset destination if it's now ineligible for the new source.
            if (SelectedDestination == null
                || !AvailableDestinations.Any(d => d.DeviceId == SelectedDestination.DeviceId))
            {
                SelectedDestination = AvailableDestinations.FirstOrDefault();
            }
            RebuildMappings();
        }

        partial void OnSelectedDestinationChanged(DeviceItem value)
        {
            RebuildMappings();
        }

        private void RebuildDestinationOptions()
        {
            AvailableDestinations.Clear();
            if (SelectedSource == null) return;
            foreach (var d in Devices)
            {
                if (d.DeviceId == SelectedSource.DeviceId) continue;
                if (!LayerCopier.IsCopyAllowed(SelectedSource.DeviceType, d.DeviceType)) continue;
                AvailableDestinations.Add(d);
            }
        }

        private void RebuildMappings()
        {
            Mappings.Clear();
            AvailableDestLedIds.Clear();

            if (SelectedSource == null || SelectedDestination == null) { UpdateSummary(0, 0); return; }
            if (!_connectedDevices.TryGetValue(SelectedSource.DeviceId, out var src)) { UpdateSummary(0, 0); return; }
            if (!_connectedDevices.TryGetValue(SelectedDestination.DeviceId, out var dst)) { UpdateSummary(0, 0); return; }

            // Surface the dest's full LedId set so the user can override
            // each row to any LedId on the dest if they don't like the
            // default match.
            foreach (var led in dst.OrderBy(l => (int)l.Id))
                AvailableDestLedIds.Add(led.Id);

            var defaultMap = LayerCopier.ComputeDefaultMapping(src, dst);
            int unmatched = 0;
            foreach (var sled in src.OrderBy(l => (int)l.Id))
            {
                bool hasMapping = defaultMap.TryGetValue(sled.Id, out var destLedId);
                if (!hasMapping) unmatched++;
                Mappings.Add(new MappingRow
                {
                    SourceLedId = sled.Id,
                    SourceLedIdLabel = sled.Id.ToString(),
                    DestinationLedId = hasMapping ? destLedId : default,
                    HasDestination = hasMapping,
                });
            }

            // Layer count is determined dynamically — caller queries it via
            // BuildResolvedMapping when committing. Surface a rough summary
            // here.
            int layerCount = Chromatics.Layers.MappingLayers.GetLayers().Values
                .Count(l => l.deviceGuid == SelectedSource.DeviceId);
            UpdateSummary(layerCount, unmatched);
        }

        private void UpdateSummary(int layerCount, int unmatched)
        {
            string template = LocalizationService.Instance["{0} layer(s) will be copied. {1} source LED(s) have no destination mapping and will be dropped."];
            SummaryText = string.Format(template, layerCount, unmatched);
        }

        // Snapshot the current row state into a Dictionary the caller
        // hands to LayerCopier.Apply. Rows where the user cleared the
        // destination are dropped (no entry added).
        public Dictionary<LedId, LedId> BuildResolvedMapping()
        {
            var map = new Dictionary<LedId, LedId>();
            foreach (var row in Mappings)
            {
                if (!row.HasDestination) continue;
                map[row.SourceLedId] = row.DestinationLedId;
            }
            return map;
        }

        public sealed class DeviceItem
        {
            public Guid DeviceId { get; init; }
            public string Name { get; init; }
            public RGBDeviceType DeviceType { get; init; }
            public override string ToString() => Name;
        }

        public partial class MappingRow : ObservableObject
        {
            [ObservableProperty] private LedId _sourceLedId;
            [ObservableProperty] private string _sourceLedIdLabel;
            [ObservableProperty] private LedId _destinationLedId;
            [ObservableProperty] private bool _hasDestination;
        }
    }
}
