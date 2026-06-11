using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Layers;
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
    // Per-layer copy model. The dialog shows one row per layer on
    // the source device; the user ticks which layers to copy and
    // (for Dynamic layers) tweaks the per-LedId mapping in an
    // expandable section.
    //
    // Base and Effect layers don't get the per-LED expander —
    // they're whole-device layers and the mapping is implicit
    // (identity for keyboard pairs, positional fallback for
    // anything else). What they do get is a "will replace existing"
    // badge so the user knows the destination's existing Base /
    // Effect layer is about to be evicted.
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
            RebuildLayerRows();
        }

        private readonly IReadOnlyDictionary<Guid, IRGBDevice> _connectedDevices;

        public ObservableCollection<DeviceItem> Devices { get; } = new();
        public ObservableCollection<DeviceItem> AvailableDestinations { get; } = new();
        public ObservableCollection<LayerCopyRow> LayerRows { get; } = new();

        // Shared LedId pool for the per-row destination dropdowns. The
        // list is the same for every row in the dialog (it's the dest
        // device's full LedId set), so we hold it at the dialog VM
        // level and the AXAML binds the per-row ComboBox to it via the
        // window's DataContext. Avoids the per-row collection
        // duplication and lets the AXAML binding path stay simple
        // (no nested-type cast).
        public ObservableCollection<LedId> AvailableDestLedIds { get; } = new();

        [ObservableProperty] private DeviceItem _selectedSource;
        [ObservableProperty] private DeviceItem _selectedDestination;
        [ObservableProperty] private string _summaryText;
        [ObservableProperty] private bool _hasNoLayers;
        [ObservableProperty] private bool _canCopy;

        public void SelectAll()
        {
            foreach (var row in LayerRows) row.IsSelected = true;
            // UpdateSummary fires from each row's PropertyChanged handler,
            // but recompute once at the end so the summary stays consistent
            // even if a row was already selected and didn't trigger.
            UpdateSummary();
        }

        public void ClearAll()
        {
            foreach (var row in LayerRows) row.IsSelected = false;
            UpdateSummary();
        }

        partial void OnSelectedSourceChanged(DeviceItem value)
        {
            RebuildDestinationOptions();
            if (SelectedDestination == null
                || !AvailableDestinations.Any(d => d.DeviceId == SelectedDestination.DeviceId))
            {
                SelectedDestination = AvailableDestinations.FirstOrDefault();
            }
            RebuildLayerRows();
        }

        partial void OnSelectedDestinationChanged(DeviceItem value)
        {
            RebuildLayerRows();
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

        private void RebuildLayerRows()
        {
            // Detach handlers from previous rows so deselected rows don't
            // continue firing summary updates after a source switch.
            foreach (var row in LayerRows) row.PropertyChanged -= OnRowChanged;
            LayerRows.Clear();

            HasNoLayers = false;
            if (SelectedSource == null || SelectedDestination == null) { UpdateSummary(); return; }
            if (!_connectedDevices.TryGetValue(SelectedSource.DeviceId, out var src)) { UpdateSummary(); return; }
            if (!_connectedDevices.TryGetValue(SelectedDestination.DeviceId, out var dst)) { UpdateSummary(); return; }

            AvailableDestLedIds.Clear();
            foreach (var led in dst.OrderBy(l => (int)l.Id))
                AvailableDestLedIds.Add(led.Id);

            // Only Dynamic layers are copyable. Base and Effect layers
            // are at-most-one per device by design and don't carry the
            // per-LedId mapping data that makes copying meaningful;
            // filtering them out here keeps the dialog focused on
            // the layers users actually want to duplicate.
            //
            // Order matches the Mappings tab: descending zindex so the
            // top-most layer in the Mappings list is the top row in
            // the copy dialog. MappingViewModel.RefreshLayers uses
            // `.OrderByDescending(l => l.zindex)`; mirror that here
            // so the two views stay visually consistent.
            var sourceLayers = MappingLayers.GetLayers().Values
                .Where(l => l.deviceGuid == SelectedSource.DeviceId
                         && l.rootLayerType == LayerType.DynamicLayer)
                .OrderByDescending(l => l.zindex)
                .ToList();

            if (sourceLayers.Count == 0)
            {
                HasNoLayers = true;
                UpdateSummary();
                return;
            }

            foreach (var layer in sourceLayers)
            {
                var usedSourceLedIds = layer.deviceLeds?.Values.Distinct().ToList() ?? new List<LedId>();
                var defaultMap = LayerCopier.ComputeDefaultMappingForLayer(usedSourceLedIds, src, dst);

                var row = new LayerCopyRow
                {
                    SourceLayer = layer,
                    IsSelected = true,
                    DisplayName = BuildLayerDisplayName(layer),
                    TypeBadge = BuildLayerTypeBadge(layer.rootLayerType),
                    RootLayerType = layer.rootLayerType,
                    WillReplaceExisting = LayerCopier.DestinationAlreadyHasLayerOfType(
                        SelectedDestination.DeviceId, layer.rootLayerType),
                };

                foreach (var ledId in usedSourceLedIds.OrderBy(id => (int)id))
                {
                    bool hasMapping = defaultMap.TryGetValue(ledId, out var destLedId);
                    row.KeyMappings.Add(new MappingRow
                    {
                        SourceLedId = ledId,
                        SourceLedIdLabel = ledId.ToString(),
                        DestinationLedId = hasMapping ? destLedId : default,
                        HasDestination = hasMapping,
                    });
                }

                row.PropertyChanged += OnRowChanged;
                LayerRows.Add(row);
            }

            UpdateSummary();
        }

        private void OnRowChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerCopyRow.IsSelected)) UpdateSummary();
        }

        private void UpdateSummary()
        {
            int selected = LayerRows.Count(r => r.IsSelected);
            CanCopy = selected > 0;
            string template = LocalizationService.Instance["{0} layer(s) selected."];
            SummaryText = string.Format(template, selected);
        }

        // Build the list of copy plans for LayerCopier.Apply. Walks
        // the ticked LayerRows and snapshots their current
        // mapping state into immutable plan entries.
        public IReadOnlyList<LayerCopier.LayerCopyPlan> BuildPlans()
        {
            var plans = new List<LayerCopier.LayerCopyPlan>();
            foreach (var row in LayerRows)
            {
                if (!row.IsSelected) continue;
                var mapping = new Dictionary<LedId, LedId>();
                foreach (var m in row.KeyMappings)
                {
                    if (!m.HasDestination) continue;
                    mapping[m.SourceLedId] = m.DestinationLedId;
                }
                plans.Add(new LayerCopier.LayerCopyPlan
                {
                    SourceLayer = row.SourceLayer,
                    Mapping = mapping,
                });
            }
            return plans;
        }

        // Friendly per-layer label. Only Dynamic layers reach this
        // dialog (Base / Effect are filtered out in RebuildLayerRows),
        // so the label is just the dynamic sub-type name — no need
        // to prefix every row with "Dynamic:" when it's the only
        // category in the list.
        private static string BuildLayerDisplayName(Layer layer)
        {
            return layer.rootLayerType switch
            {
                LayerType.DynamicLayer => DynamicLayerTypeFromComboIndex(layer.layerTypeindex).ToString(),
                LayerType.BaseLayer => ((BaseLayerType)layer.layerTypeindex).ToString(),
                LayerType.EffectLayer => LocalizationService.Instance["Effect"],
                _ => layer.rootLayerType.ToString(),
            };
        }

        private static string BuildLayerTypeBadge(LayerType rootLayerType) => rootLayerType switch
        {
            LayerType.BaseLayer => LocalizationService.Instance["Base"],
            LayerType.EffectLayer => LocalizationService.Instance["Effect"],
            LayerType.DynamicLayer => LocalizationService.Instance["Dynamic"],
            _ => rootLayerType.ToString(),
        };

        // CLAUDE.md note: `layer.layerTypeindex` for Dynamic layers is the
        // ComboBox position in _dynamicLayerOrder, NOT the enum value. The
        // first 10 positions coincide with the enum by accident; from
        // position 10 onward they diverge. Use the official order array
        // GameController consumes.
        private static readonly DynamicLayerType[] _dynamicLayerOrder =
        {
            DynamicLayerType.None,
            DynamicLayerType.Highlight,
            DynamicLayerType.Keybinds,
            DynamicLayerType.EnmityTracker,
            DynamicLayerType.TargetHP,
            DynamicLayerType.TargetCastbar,
            DynamicLayerType.HPTracker,
            DynamicLayerType.MPTracker,
            DynamicLayerType.JobGaugeA,
            DynamicLayerType.JobGaugeB,
            DynamicLayerType.JobGaugeC,
            DynamicLayerType.ExperienceTracker,
            DynamicLayerType.BattleStance,
            DynamicLayerType.Castbar,
            DynamicLayerType.JobClassesHighlight,
            DynamicLayerType.ReactiveWeatherHighlight,
            DynamicLayerType.FocusTargetHP,
            DynamicLayerType.FocusTargetCastbar,
        };

        private static DynamicLayerType DynamicLayerTypeFromComboIndex(int index)
        {
            if (index < 0 || index >= _dynamicLayerOrder.Length) return DynamicLayerType.None;
            return _dynamicLayerOrder[index];
        }

        public sealed class DeviceItem
        {
            public Guid DeviceId { get; init; }
            public string Name { get; init; }
            public RGBDeviceType DeviceType { get; init; }
            public override string ToString() => Name;
        }

        public partial class LayerCopyRow : ObservableObject
        {
            [ObservableProperty] private bool _isSelected;
            [ObservableProperty] private string _displayName;
            [ObservableProperty] private string _typeBadge;
            [ObservableProperty] private LayerType _rootLayerType;
            [ObservableProperty] private bool _willReplaceExisting;

            public Layer SourceLayer { get; init; }
            public ObservableCollection<MappingRow> KeyMappings { get; } = new();

            // Per-LED mapping is only meaningful for Dynamic layers —
            // Base / Effect cover the whole device and we use identity /
            // positional fallback under the hood. Hides the expander
            // section on Base / Effect rows.
            public bool ShowKeyMappings => RootLayerType == LayerType.DynamicLayer && KeyMappings.Count > 0;

            partial void OnRootLayerTypeChanged(LayerType value) => OnPropertyChanged(nameof(ShowKeyMappings));
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
