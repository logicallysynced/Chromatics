using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Helpers;
using Chromatics.Layers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Actor = Sharlayan.Core.Enums.Actor;
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

                // For Job Gauge types, substitute a job-specific description when in-game.
                if (_layer.rootLayerType == LayerType.DynamicLayer &&
                    (option.Value == (int)DynamicLayerType.JobGaugeA ||
                     option.Value == (int)DynamicLayerType.JobGaugeB ||
                     option.Value == (int)DynamicLayerType.JobGaugeC))
                {
                    var job = GameController.GetCurrectJob();
                    if (job != Actor.Job.Unknown &&
                        _jobGaugeDescriptions.TryGetValue((job, (DynamicLayerType)option.Value), out var jobDesc))
                        return TextHelper.ParseLayerHelperText(jobDesc);
                }

                return TextHelper.ParseLayerHelperText(option.Description);
            }
        }

        // Job-specific gauge descriptions shown on the layer-type ComboBox header when
        // the game is attached. Falls through to the generic [LayerDisplay] Description
        // for any job or gauge slot that has no entry.
        private static readonly Dictionary<(Actor.Job, DynamicLayerType), string> _jobGaugeDescriptions = new()
        {
            // ── Job Gauge A ──────────────────────────────────────────────────────────
            { (Actor.Job.WAR, DynamicLayerType.JobGaugeA), "Beast Gauge (0–100). Fills via combo finishers; spend at 50+ with Fell Cleave or Decimate." },
            { (Actor.Job.PLD, DynamicLayerType.JobGaugeA), "Oath Gauge (0–100). Fills in combat; spend via Sheltron and Holy Sheltron." },
            { (Actor.Job.MNK, DynamicLayerType.JobGaugeA), "Chakra Stacks (0–5). Fill via Meditation hits; spend with The Forbidden Chakra." },
            { (Actor.Job.DRG, DynamicLayerType.JobGaugeA), "Dragon Gauge (0–100). Fills during Blood of the Dragon; sustain to remain in Life of the Dragon." },
            { (Actor.Job.BRD, DynamicLayerType.JobGaugeA), "Soul Voice (0–100). Fills during songs; spend with Apex Arrow at 80+." },
            { (Actor.Job.WHM, DynamicLayerType.JobGaugeA), "Healing Lilies and Blood Lily. Lilies fill over time; three lilies charge a Blood Lily for Afflatus Misery." },
            { (Actor.Job.BLM, DynamicLayerType.JobGaugeA), "Astral Fire / Umbral Ice stance timer (0–15s). Tracks the 15-second buff window; lights up on Paradox proc." },
            { (Actor.Job.SMN, DynamicLayerType.JobGaugeA), "Active summon timer. Tracks the current phase: Carbuncle, Dreadwyrm, Bahamut, or Phoenix." },
            { (Actor.Job.SCH, DynamicLayerType.JobGaugeA), "Fairy Gauge (0–100). Fills via Dissipation and Aetherflow spends; used by Fey Union (Aetherpact)." },
            { (Actor.Job.NIN, DynamicLayerType.JobGaugeA), "Kazematoi Stacks (0–5). Fill via En Droit; spend with Dokumori." },
            { (Actor.Job.DRK, DynamicLayerType.JobGaugeA), "Blood Gauge (0–100). Fills via Bloodspiller combos; spend at 50+ with Bloodspiller or Quietus." },
            { (Actor.Job.AST, DynamicLayerType.JobGaugeA), "Current Arcana. Colour matches the card currently being played." },
            { (Actor.Job.MCH, DynamicLayerType.JobGaugeA), "Heat Gauge (0–100). Fills with weaponskills; triggers Hypercharge when full." },
            { (Actor.Job.SAM, DynamicLayerType.JobGaugeA), "Kenki Gauge (0–100). Fills via combos; spend with Hissatsu weaponskills." },
            { (Actor.Job.RDM, DynamicLayerType.JobGaugeA), "Black and White Mana balance (0–100 each). Both must reach 50+/80+ for enchanted melee combos." },
            { (Actor.Job.DNC, DynamicLayerType.JobGaugeA), "Espirit Gauge (0–100). Fills via partner procs and Technical Finish; spend at 50+ with Saber Dance." },
            { (Actor.Job.GNB, DynamicLayerType.JobGaugeA), "Powder Gauge / Cartridges (0–3). Filled by Gnashing Fang combo; spend with Burst Strike or Fated Circle." },
            { (Actor.Job.SGE, DynamicLayerType.JobGaugeA), "Addersgall Stacks (0–3). One stack fills every 20s; spend with Druochole, Kerachole, Ixochole, or Taurochole." },
            { (Actor.Job.RPR, DynamicLayerType.JobGaugeA), "Soul Gauge (0–100). Fills via Slice and Infernal Slice combos; spend at 50+ with Soul Scythe or Soul Slice." },
            { (Actor.Job.VPR, DynamicLayerType.JobGaugeA), "Vipersight Gauge. Tracks the twin serpent stacks during the Reawaken phase." },
            { (Actor.Job.PCT, DynamicLayerType.JobGaugeA), "Palette Gauge (0–100). Fills via Motif combos; spend at 50+ with Holy in White or Comet in Black." },

            // ── Job Gauge B ──────────────────────────────────────────────────────────
            { (Actor.Job.PLD, DynamicLayerType.JobGaugeB), "Confiteor Combo Timer. Tracks the remaining window to execute the Confiteor blade combo chain." },
            { (Actor.Job.MNK, DynamicLayerType.JobGaugeB), "Beast Chakra Aggregate (0–6). Total OpoOpo, Raptor and Coeurl stacks accumulated for Perfect Balance." },
            { (Actor.Job.BLM, DynamicLayerType.JobGaugeB), "Astral Soul Stacks (0–6). Fill with Fire IV during Astral Fire; spend all 6 stacks with Flare Star." },
            { (Actor.Job.SCH, DynamicLayerType.JobGaugeB), "Aetherflow Stacks (0–3). Spend with Energy Drain, Lustrate, Excogitation, Indomitability, or Sacred Soil." },
            { (Actor.Job.AST, DynamicLayerType.JobGaugeB), "Drawn Card. Colour matches the next card currently held in hand." },

            // ── Job Gauge C ──────────────────────────────────────────────────────────
            { (Actor.Job.DRG, DynamicLayerType.JobGaugeC), "Firstminds' Focus (0–2). Fills during Life of the Dragon; spend both stacks with Dragonfire Dive or Stardiver." },
            { (Actor.Job.BRD, DynamicLayerType.JobGaugeC), "Radiant Finale Codas. Tracks which song codas (Ballad, Paeon, Minuet) are stored for Radiant Finale." },
            { (Actor.Job.DRK, DynamicLayerType.JobGaugeC), "Living Shadow Timer. Tracks the remaining duration of the Living Shadow summon." },
            { (Actor.Job.GNB, DynamicLayerType.JobGaugeC), "Bloodfest Timer. Tracks the Bloodfest cooldown scaled to its maximum duration." },
            { (Actor.Job.MNK, DynamicLayerType.JobGaugeC), "Nadi (Lunar / Solar). Tracks which Nadi are accumulated; both are needed for Phantom Rush." },
            { (Actor.Job.PLD, DynamicLayerType.JobGaugeC), "Confiteor Combo Step (0–3). Shows progress through Confiteor → Blade of Faith → Truth → Valor." },
            { (Actor.Job.RDM, DynamicLayerType.JobGaugeC), "Mana Stacks (0–3). Fill via Enchanted Riposte → Zwerchhau → Redoublement; spend 3 with Verholy or Verflare." },
            { (Actor.Job.SAM, DynamicLayerType.JobGaugeC), "Kaeshi Ready. Indicator that a Kaeshi follow-up is queued for the last Hissatsu used." },
            { (Actor.Job.VPR, DynamicLayerType.JobGaugeC), "Reawakened Timer. Tracks the remaining duration of the Reawakened phase." },
            { (Actor.Job.AST, DynamicLayerType.JobGaugeC), "Draw Type (Astral / Umbral). Indicates which side of the draw cycle is currently active." },
        };

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
