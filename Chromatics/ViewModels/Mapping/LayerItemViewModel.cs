using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Helpers;
using Chromatics.Layers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using IBrush = Avalonia.Media.IBrush;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using AvColor = Avalonia.Media.Color;

namespace Chromatics.ViewModels.Mapping
{
    // Wraps a single Layer in MappingLayers. Mutations commit straight back
    // through MappingLayers.UpdateLayer so the VM and persistence stay in
    // sync without a separate save step per property.
    public sealed partial class LayerItemViewModel : ObservableObject
    {
        private readonly Layer _layer;
        private readonly Action<int> _onDelete;
        private readonly Action<int> _onCopy;
        private readonly Action<int> _onEdit;
        private readonly Action _onClearKeys;
        private readonly Action _onReverseKeys;
        private readonly Action _onUndoKeys;
        private readonly Action _onLayerStateChanged;
        private bool _suspendCommit;

        public int LayerId => _layer.layerID;
        public LayerType RootLayerType => _layer.rootLayerType;
        public Guid DeviceId => _layer.deviceGuid;

        // Base and Effect are structural — not user-addable, not user-removable,
        // not draggable. The UI uses this to hide/disable the corresponding
        // affordances (drag grip, delete, copy).
        public bool IsLocked => _layer.rootLayerType != LayerType.DynamicLayer;
        public bool IsDraggable => !IsLocked;

        // Effect layers don't expose any user-selectable variant — their combo
        // is hidden entirely in the card and the badge itself carries the name.
        // Base layers keep their combo (user picks a BaseLayerType variant);
        // Dynamic layers are fully editable.
        public bool IsTypeEditable => _layer.rootLayerType != LayerType.EffectLayer;
        public bool IsTypeComboVisible => _layer.rootLayerType != LayerType.EffectLayer;

        // Edit / key-pick doesn't apply to Base or Effect layers because those
        // always span every LED on the device. Hide the pencil button on them.
        public bool IsEditButtonVisible => _layer.rootLayerType == LayerType.DynamicLayer;

        // Shown inside the accent badge. Dynamic layers show the stacking
        // index; structural layers spell their role out because the "number"
        // is effectively fixed (1 for base, max for effect) and not useful.
        public string BadgeText => _layer.rootLayerType switch
        {
            LayerType.BaseLayer   => "Base Layer",
            LayerType.EffectLayer => "Effect Layer",
            _ => ZIndex.ToString()
        };

        public Color AccentColor { get; }
        public IBrush AccentBrush { get; }
        public IReadOnlyList<LayerTypeOption> TypeOptions { get; }
        public IReadOnlyList<LayerModes> ModeOptions { get; }

        [ObservableProperty] private bool _isEnabled;
        [ObservableProperty] private int _zIndex;
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(HelpText))] private int _layerTypeIndex;
        [ObservableProperty] private LayerModes _mode;
        [ObservableProperty] private bool _allowBleed;

        // Visual state only — owned by MappingViewModel, which toggles it so
        // exactly one layer is in edit mode at a time.
        [ObservableProperty] private bool _isEditing;

        // True when the user has clicked this layer card to highlight its key
        // assignments on the virtual device. Set by MappingViewModel.SelectLayer.
        [ObservableProperty] private bool _isSelected;

        // Drag-reorder state flipped by LayerListView during DoDragDrop.
        // `IsDragging` dims the source card; `IsDropTargetAbove/Below` paints
        // the accent insertion indicator above or below a hovered target.
        // Cleared when the drag ends; no persistence.
        [ObservableProperty] private bool _isDragging;
        [ObservableProperty] private bool _isDropTargetAbove;
        [ObservableProperty] private bool _isDropTargetBelow;

        public LayerItemViewModel(Layer layer, Action<int> onEdit, Action<int> onCopy, Action<int> onDelete,
                                  Action onClearKeys = null, Action onReverseKeys = null, Action onUndoKeys = null,
                                  Action onLayerStateChanged = null)
        {
            _layer = layer;
            _onEdit = onEdit;
            _onCopy = onCopy;
            _onDelete = onDelete;
            _onClearKeys = onClearKeys;
            _onReverseKeys = onReverseKeys;
            _onUndoKeys = onUndoKeys;
            _onLayerStateChanged = onLayerStateChanged;

            AccentColor = (Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(layer.rootLayerType).Value;
            AccentBrush = new SolidColorBrush(AvColor.FromArgb(AccentColor.A, AccentColor.R, AccentColor.G, AccentColor.B));
            TypeOptions = BuildTypeOptions(layer.rootLayerType);
            ModeOptions = new[] { LayerModes.None, LayerModes.Interpolate, LayerModes.Fade };

            _suspendCommit = true;
            _isEnabled = layer.Enabled;
            _zIndex = layer.zindex;
            _layerTypeIndex = layer.layerTypeindex;
            _mode = layer.layerModes;
            _allowBleed = layer.allowBleed;
            _suspendCommit = false;
        }

        public Layer Model => _layer;

        partial void OnIsEnabledChanged(bool value)
        {
            Commit(l => l.Enabled = value);
            _onLayerStateChanged?.Invoke();
        }
        partial void OnZIndexChanged(int value)
        {
            Commit(l => l.zindex = value);
            // BadgeText derives from ZIndex for dynamic layers — nudge the
            // view so the card updates after a reorder.
            OnPropertyChanged(nameof(BadgeText));
        }
        partial void OnLayerTypeIndexChanged(int value) => Commit(l => l.layerTypeindex = value);
        partial void OnModeChanged(LayerModes value) => Commit(l => l.layerModes = value);
        partial void OnAllowBleedChanged(bool value) => Commit(l => l.allowBleed = value);

        private void Commit(Action<Layer> mutate)
        {
            if (_suspendCommit) return;
            mutate(_layer);
            _layer.requestUpdate = true;
            MappingLayers.UpdateLayer(_layer);
            MappingLayers.SaveMappings();
        }

        public string HelpText
        {
            get
            {
                if (_layer.rootLayerType == LayerType.EffectLayer)
                    return TextHelper.ParseLayerHelperText(
                        "The effect layer displays effects over other layers, depending on which effects are enabled.");
                var option = TypeOptions.FirstOrDefault(o => o.Value == LayerTypeIndex);
                if (option == null || string.IsNullOrEmpty(option.Description)) return string.Empty;
                return TextHelper.ParseLayerHelperText(option.Description);
            }
        }

        public void RefreshHelpText() => OnPropertyChanged(nameof(HelpText));

        [RelayCommand] private void Edit() => _onEdit?.Invoke(_layer.layerID);
        [RelayCommand] private void Copy() => _onCopy?.Invoke(_layer.layerID);
        [RelayCommand] private void Delete() => _onDelete?.Invoke(_layer.layerID);
        [RelayCommand] private void ClearKeys() => _onClearKeys?.Invoke();
        [RelayCommand] private void ReverseKeys() => _onReverseKeys?.Invoke();
        [RelayCommand] private void UndoKeys() => _onUndoKeys?.Invoke();

        private static readonly DynamicLayerType[] _dynamicLayerOrder =
        [
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
        ];

        private static IReadOnlyList<LayerTypeOption> BuildTypeOptions(LayerType layerType) => layerType switch
        {
            LayerType.BaseLayer    => BuildOptionsFor<BaseLayerType>(),
            LayerType.DynamicLayer => BuildOptionsFor(_dynamicLayerOrder),
            LayerType.EffectLayer  => BuildOptionsFor<EffectLayerType>(),
            _ => Array.Empty<LayerTypeOption>()
        };

        private static IReadOnlyList<LayerTypeOption> BuildOptionsFor<TEnum>(IEnumerable<TEnum> values) where TEnum : struct, Enum
        {
            return values.Select(v =>
                {
                    var display = EnumExtensions.GetAttribute<LayerDisplay>((Enum)(object)v);
                    return new LayerTypeOption(
                        display?.Name ?? v.ToString(),
                        Convert.ToInt32(v),
                        display?.LayerTypeCompatibility ?? new[] { LayerModes.None },
                        display?.Description);
                })
                .ToList();
        }

        private static IReadOnlyList<LayerTypeOption> BuildOptionsFor<TEnum>() where TEnum : struct, Enum
            => BuildOptionsFor(Enum.GetValues<TEnum>());
    }
}
