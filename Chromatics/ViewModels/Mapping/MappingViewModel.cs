using Avalonia.Threading;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.Localization;
using Chromatics.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Chromatics.ViewModels.Mapping
{
    // Top-level VM for the Mapping tab. Sources of truth:
    //  - MappingLayers (static): persistent layer store. VMs read/write through
    //    its APIs so ops survive app restart.
    //  - AppSettings: keyboard layout. Layout changes get forwarded into the
    //    remap path so user key picks follow the printed label.
    public sealed partial class MappingViewModel : ObservableObject, IDisposable
    {
        public ObservableCollection<DeviceOptionItem> Devices { get; } = new();
        public ObservableCollection<LayerItemViewModel> Layers { get; } = new();
        public ObservableCollection<VirtualDeviceViewModel> VirtualDevices { get; } = new();
        public IReadOnlyList<LayerTypeOption> AddLayerOptions { get; }

        [ObservableProperty] private DeviceOptionItem _selectedDevice;
        [ObservableProperty] private LayerTypeOption _selectedLayerTypeToAdd;
        [ObservableProperty] private LayerItemViewModel _selectedLayer;
        [ObservableProperty] private bool _isPreviewing;
        // Mirrors SelectedDevice — the view binds a ContentControl to this so
        // only the selected device's virtual keyboard is rendered.
        [ObservableProperty] private VirtualDeviceViewModel _selectedVirtualDevice;
        [ObservableProperty] private bool _isSelectedDeviceEnabled;
        // Drives the "no devices found" placeholder in the Mapping view — starts
        // true so the placeholder shows while RGBController is still enumerating.
        [ObservableProperty] private bool _noDevicesAvailable = true;

        private IReadOnlyDictionary<Guid, IRGBDevice> _connectedDevices;
        private readonly Dictionary<int, LedId> _pendingKeySelection = new();

        // Suppresses OnIsSelectedDeviceEnabledChanged while RefreshIsDeviceEnabled
        // is writing the actual state so we don't call Add/RemoveDevice during a read.
        private bool _suspendDeviceEnabledSync;

        // Selected (non-editing) layer whose keys are highlighted on the virtual
        // device. -1 = no selection; all layers shown.
        private int _selectedLayerIdForDisplay = -1;

        // Throttle flag: prevents flooding the UI thread with preview dispatches
        // when Surface_Updating fires faster than Avalonia can process them.
        private volatile bool _previewUpdatePending;

        public MappingViewModel()
        {
            AddLayerOptions = Enum.GetValues<LayerType>()
                .Where(lt => lt == LayerType.DynamicLayer)
                .Select(lt => new LayerTypeOption(
                    EnumExtensions.GetAttribute<DisplayAttribute>(lt).Name,
                    (int)lt,
                    new[] { LayerModes.None }))
                .ToList();

            _selectedLayerTypeToAdd = AddLayerOptions.FirstOrDefault();

            AppSettings.KeyboardLayoutChanged += OnKeyboardLayoutChanged;
            GameController.jobChanged += OnJobChanged;
            LocalizationService.Instance.PropertyChanged += OnLocalizationVersionChanged;

            RefreshRotatingTip();
        }

        // Rotating tips shown above the layer list. Source strings are kept
        // here as English keys so they round-trip through LocalizationService —
        // every entry must also be present in Chromatics/locale/en.json so
        // translate.py can fan it out to the other locales.
        private static readonly string[] _tipKeys = new[]
        {
            "Drag layers in the list to reorder how they stack — higher layers paint over lower ones.",
            "Each device has its own brightness slider — open the sun icon in the device toolbar.",
            "Disable the global brightness or per-device brightness slider to silence a device without removing its layers.",
            "Use the Highlight layer to keep important keys (skills, gauges) a single colour so they stand out from background effects.",
            "On non-keyboard devices, unlock the keys (padlock icon) to drag them into custom shapes — rings, crosses, anything.",
            "Click the reset (↺) icon to restore the default key positions for a non-keyboard device.",
            "Effect layers add temporary bursts of lighting on top of your base layer — they fire automatically for raid mechanics, weather changes, and other in-game events.",
            "Switch keyboard layout (QWERTY / QWERTZ / AZERTY) in Settings — your keybinds remap automatically.",
            "Use the Job Gauge layer types to mirror your in-game gauges directly onto your devices.",
            "Export your layer configuration from the Mapping tab to share it or back it up before experimenting.",
            "The Audio Visualizer base layer turns your devices into a music spectrum analyser — pulses to whatever's playing on your PC.",
            "The Reactive Weather base layer changes colour based on the in-game weather and time of day.",
            "Unsupported device? If you have OpenRGB installed and running, enable it in Settings → Device Providers — it covers many third-party devices.",
            "The Battle Stance dynamic layer reacts to whether you're in combat — great for ambient lighting cues.",
        };

        // Stored as the English key, not the resolved string, so the property
        // re-localises on every read. Without this, switching language while
        // the Mapping tab is showing a tip leaves the previously-rendered
        // English text in place until the next tab visit.
        private string _rotatingTipKey = string.Empty;

        public string RotatingTip =>
            string.IsNullOrEmpty(_rotatingTipKey)
                ? string.Empty
                : LocalizationService.Instance[_rotatingTipKey];

        // Track the previously shown index so the next rotation always lands
        // on a different tip — random-with-replacement on a small list lands
        // on the same string roughly every N visits, which is jarring.
        private static int _lastTipIndex = -1;
        private static readonly Random _tipRandom = new Random();

        public void RefreshRotatingTip()
        {
            if (_tipKeys.Length == 0) return;

            int idx;
            if (_tipKeys.Length == 1)
            {
                idx = 0;
            }
            else
            {
                do { idx = _tipRandom.Next(_tipKeys.Length); }
                while (idx == _lastTipIndex);
            }

            _lastTipIndex = idx;
            _rotatingTipKey = _tipKeys[idx];
            OnPropertyChanged(nameof(RotatingTip));
        }

        // Forwarded from LocalizationService.PropertyChanged in the ctor — when
        // the user switches language, re-render whatever tip is currently
        // showing. The property getter does the lookup against the new
        // language's translation table.
        private void OnLocalizationVersionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizationService.Version))
                OnPropertyChanged(nameof(RotatingTip));
        }

        public void Dispose()
        {
            AppSettings.KeyboardLayoutChanged -= OnKeyboardLayoutChanged;
            GameController.jobChanged -= OnJobChanged;
            LocalizationService.Instance.PropertyChanged -= OnLocalizationVersionChanged;
            RGBController.ClearAvaloniaPreviewCallback();
        }

        partial void OnSelectedDeviceChanged(DeviceOptionItem value)
        {
            SelectedVirtualDevice = value == null
                ? null
                : VirtualDevices.FirstOrDefault(v => v.DeviceId == value.DeviceId);
            _selectedLayerIdForDisplay = -1;
            RefreshLayers();
            RefreshIsDeviceEnabled();
            SyncKeycapEditBadges();
        }

        partial void OnIsPreviewingChanged(bool value)
        {
            MappingLayers.SetPreview(value);
            if (value)
            {
                RGBController.SetAvaloniaPreviewCallback(OnPreviewTick);
            }
            else
            {
                RGBController.ClearAvaloniaPreviewCallback();
                VisualiseLayers();
            }
        }

        partial void OnIsSelectedDeviceEnabledChanged(bool value)
        {
            if (_suspendDeviceEnabledSync) return;
            if (_connectedDevices == null || SelectedDevice == null) return;
            if (!_connectedDevices.TryGetValue(SelectedDevice.DeviceId, out var device)) return;
            if (value)
                RGBController.AddDevice(device);
            else
                RGBController.RemoveDevice(device);
        }

        // Populate device list from the live RGB surface and add defaults for
        // any new device that doesn't yet have layers in persistence.
        public void RefreshDevices(IReadOnlyDictionary<Guid, IRGBDevice> connectedDevices)
        {
            if (connectedDevices == null) return;
            _connectedDevices = connectedDevices;

            var keep = new HashSet<Guid>(connectedDevices.Keys);

            for (int i = Devices.Count - 1; i >= 0; i--)
            {
                if (!keep.Contains(Devices[i].DeviceId)) Devices.RemoveAt(i);
            }
            for (int i = VirtualDevices.Count - 1; i >= 0; i--)
            {
                if (!keep.Contains(VirtualDevices[i].DeviceId)) VirtualDevices.RemoveAt(i);
            }

            bool layersChanged = false;

            foreach (var kvp in connectedDevices.ToList())
            {
                if (Devices.Any(d => d.DeviceId == kvp.Key)) continue;

                Devices.Add(new DeviceOptionItem(kvp.Key, kvp.Value.DeviceInfo.DeviceName, kvp.Value.DeviceInfo.DeviceType));

                VirtualDevices.Add(BuildVirtualDevice(kvp.Key, kvp.Value));

                // Only seed defaults if the device has NO layers at all. If any
                // were restored from persistence we leave them alone and just
                // top up any missing Base/Effect pins.
                bool hasAny = MappingLayers.GetLayers().Values.Any(l => l.deviceGuid == kvp.Key);
                if (!hasAny)
                {
                    CreateDefaultLayers(kvp.Key, kvp.Value);
                    layersChanged = true;
                }
                else
                {
                    int before = MappingLayers.CountLayers();
                    EnsureBaseAndEffectLayers(kvp.Key, kvp.Value.DeviceInfo.DeviceType);
                    if (MappingLayers.CountLayers() != before) layersChanged = true;
                }
            }

            // Persist default layers immediately so the file exists from first
            // boot — without this the layers.chromatics4 file only appears once
            // the user interacts with the Mapping tab.
            if (layersChanged)
                MappingLayers.SaveMappings();

            // Re-seed if SelectedDevice is null (first call) OR points at a
            // device that just got pruned above (e.g. its provider was
            // disabled). Without the second check the ComboBox renders blank
            // because SelectedItem no longer matches any entry in ItemsSource.
            if (SelectedDevice == null || !Devices.Contains(SelectedDevice))
                SelectedDevice = Devices.FirstOrDefault();

            // If SelectedDevice was already set, partial-method didn't run;
            // sync the virtual-device pointer manually so a freshly-enumerated
            // device swaps its keyboard in without needing a dropdown change.
            if (SelectedDevice != null && (SelectedVirtualDevice == null ||
                SelectedVirtualDevice.DeviceId != SelectedDevice.DeviceId))
            {
                SelectedVirtualDevice = VirtualDevices.FirstOrDefault(v => v.DeviceId == SelectedDevice.DeviceId);
            }

            NoDevicesAvailable = Devices.Count == 0;

            // Device set may have changed while SelectedDevice held steady —
            // re-pull layers so newly-seeded rows appear in the list.
            RefreshLayers();
            RefreshIsDeviceEnabled();
        }

        // Full default stack for a first-seen device. Mirrors the old
        // Uc_Mappings.CreateDefaults: Base first, a suite of gameplay-focused
        // Dynamic layers on keyboards, Effect last.
        private static void CreateDefaultLayers(Guid deviceId, IRGBDevice device)
        {
            var deviceType = device.DeviceInfo.DeviceType;
            if (deviceType == RGBDeviceType.None || deviceType == RGBDeviceType.All) return;

            var baseKeys = new Dictionary<int, LedId>();
            int baseIdx = 0;
            foreach (var led in device)
                baseKeys[baseIdx++] = led.Id;

            int z = 1;

            MappingLayers.AddLayer(0, LayerType.BaseLayer, deviceId, deviceType,
                (int)BaseLayerType.ReactiveWeather, z++, true,
                new Dictionary<int, LedId>(baseKeys), false, LayerModes.None);

            if (deviceType == RGBDeviceType.Keyboard)
            {
                var layout = AppSettings.GetSettings().keyboardLayout;
                var reactiveWeatherKeys = layout == KeyboardLocalization.azerty
                    ? LedKeyHelper.DefaultKeys_ReactiveWeather_AZERTY
                    : LedKeyHelper.DefaultKeys_ReactiveWeather_QWERTY;

                MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.DynamicLayer, deviceId, deviceType,
                    (int)DynamicLayerType.ReactiveWeatherHighlight, z++, true,
                    new Dictionary<int, LedId>(reactiveWeatherKeys), true, LayerModes.Interpolate);

                MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.DynamicLayer, deviceId, deviceType,
                    (int)DynamicLayerType.Keybinds, z++, true,
                    new Dictionary<int, LedId>(LedKeyHelper.DefaultKeys_Keybinds), true, LayerModes.Interpolate);

                MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.DynamicLayer, deviceId, deviceType,
                    (int)DynamicLayerType.HPTracker, z++, true,
                    new Dictionary<int, LedId>(LedKeyHelper.DefaultKeys_HP), true, LayerModes.Interpolate);

                MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.DynamicLayer, deviceId, deviceType,
                    (int)DynamicLayerType.MPTracker, z++, true,
                    new Dictionary<int, LedId>(LedKeyHelper.DefaultKeys_MP), true, LayerModes.Interpolate);

                MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.DynamicLayer, deviceId, deviceType,
                    (int)DynamicLayerType.TargetHP, z++, true,
                    new Dictionary<int, LedId>(LedKeyHelper.DefaultKeys_TargetHP), true, LayerModes.Interpolate);

                MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.DynamicLayer, deviceId, deviceType,
                    (int)DynamicLayerType.Castbar, z++, true,
                    new Dictionary<int, LedId>(LedKeyHelper.DefaultKeys_Castbar), true, LayerModes.Interpolate);

                MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.DynamicLayer, deviceId, deviceType,
                    (int)DynamicLayerType.EnmityTracker, z++, true,
                    new Dictionary<int, LedId>(LedKeyHelper.DefaultKeys_Enmity), true, LayerModes.Interpolate);
            }

            MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.EffectLayer, deviceId, deviceType,
                0, z, true, new Dictionary<int, LedId>(baseKeys), false, LayerModes.None);

            Logger.WriteConsole(LoggerTypes.System,
                $"Creating default layers for device {device.DeviceInfo.DeviceName} ({deviceType}). Key count: {baseKeys.Count}");
        }

        public void RefreshLayers()
        {
            Layers.Clear();
            if (SelectedDevice == null) return;

            // Descending zindex so Effect (highest z) sits at the top of the list
            // and Base (lowest z) at the bottom, matching the old Uc_Mappings layout.
            var deviceLayers = MappingLayers.GetLayers().Values
                .Where(l => l.deviceGuid == SelectedDevice.DeviceId)
                .OrderByDescending(l => l.zindex);

            foreach (var layer in deviceLayers)
                Layers.Add(WrapLayer(layer));

            if (!IsPreviewing) VisualiseLayers();
        }

        public int AddDynamicLayer()
        {
            if (SelectedDevice == null) return -1;
            return AddLayerInternal(LayerType.DynamicLayer, SelectedDevice.DeviceId, SelectedDevice.DeviceType);
        }

        // Moves the layer at `fromIndex` to `toIndex` in the visible (zindex-
        // ordered) list, then renumbers zindex so persistence reflects the
        // new stacking order. Base/Effect layers are pinned: they can't move,
        // and DynamicLayers can't cross over them into the pinned slots.
        public void MoveLayer(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || toIndex < 0) return;
            if (fromIndex >= Layers.Count || toIndex >= Layers.Count) return;
            if (fromIndex == toIndex) return;

            var moving = Layers[fromIndex];
            if (moving.RootLayerType != LayerType.DynamicLayer) return;

            int clamped = ClampToDynamicRange(toIndex);
            if (clamped == fromIndex) return;

            Layers.Move(fromIndex, clamped);
            RenumberZIndex();
            if (!IsPreviewing) VisualiseLayers();
        }

        public void RemoveLayer(int layerId)
        {
            var vm = Layers.FirstOrDefault(l => l.LayerId == layerId);
            if (vm == null) return;
            if (vm.RootLayerType != LayerType.DynamicLayer) return;

            if (_selectedLayerIdForDisplay == layerId) _selectedLayerIdForDisplay = -1;

            Layers.Remove(vm);
            MappingLayers.RemoveLayer(layerId);
            MappingLayers.SaveMappings();
            RenumberZIndex();
            if (!IsPreviewing) VisualiseLayers();
        }

        // List slot 0 holds Effect (top); the final slot holds Base (bottom).
        // Dynamic drops must stay strictly inside that range.
        private int ClampToDynamicRange(int index)
        {
            int min = 0;
            int max = Layers.Count - 1;
            if (Layers.Count > 0 && Layers[0].RootLayerType == LayerType.EffectLayer) min = 1;
            if (Layers.Count > 0 && Layers[^1].RootLayerType == LayerType.BaseLayer) max = Layers.Count - 2;
            if (index < min) return min;
            if (index > max) return max;
            return index;
        }

        public int DuplicateLayer(int layerId)
        {
            var source = MappingLayers.GetLayer(layerId);
            if (source == null) return -1;
            if (source.rootLayerType != LayerType.DynamicLayer) return -1;

            var copiedLeds = source.deviceLeds != null
                ? new Dictionary<int, LedId>(source.deviceLeds)
                : new Dictionary<int, LedId>();

            int newZ = Layers.Count + 1;
            int newId = MappingLayers.AddLayer(
                MappingLayers.CountLayers(),
                source.rootLayerType,
                source.deviceGuid,
                source.deviceType,
                source.layerTypeindex,
                newZ,
                source.Enabled,
                copiedLeds,
                source.allowBleed,
                source.layerModes);

            var created = MappingLayers.GetLayer(newId);
            if (created != null && created.deviceGuid == SelectedDevice?.DeviceId)
                InsertDynamicBelowEffect(WrapLayer(created));

            MappingLayers.SaveMappings();
            return newId;
        }

        public void ApplyKeyboardLayoutChange(KeyboardLocalization from, KeyboardLocalization to, bool remapLayers = true)
        {
            if (remapLayers)
                MappingLayers.RemapLedIdsForLayoutChange(from, to);
            MappingLayers.SaveMappings();
            RebuildKeyboardVirtualDevices(to);
            RefreshLayers();
        }

        private void RebuildKeyboardVirtualDevices(KeyboardLocalization layout)
        {
            if (_connectedDevices == null) return;
            for (int i = 0; i < VirtualDevices.Count; i++)
            {
                var vd = VirtualDevices[i];
                if (vd.DeviceType != RGBDeviceType.Keyboard) continue;
                if (!_connectedDevices.ContainsKey(vd.DeviceId)) continue;
                var rebuilt = VirtualDeviceViewModel.BuildForKeyboard(vd.DeviceId, vd.DeviceName, layout);
                rebuilt.SetPickKeyCallback(PickKey);
                VirtualDevices[i] = rebuilt;
            }
            if (SelectedDevice != null)
                SelectedVirtualDevice = VirtualDevices.FirstOrDefault(v => v.DeviceId == SelectedDevice.DeviceId);
        }

        // Bound to the "Add Layer" button. Currently only DynamicLayer is user-
        // addable (Base/Effect are seeded per device in CreateDefaultLayers).
        [RelayCommand]
        private void AddSelectedLayer()
        {
            if (SelectedDevice == null) return;
            AddDynamicLayer();
        }

        // Pops visual state back to a known-clean baseline. Used after import or
        // a device-set change where existing LayerItemViewModel wrappers may point
        // at stale Layer records.
        public void ReloadAfterImport()
        {
            MappingLayers.SaveMappings();
            RefreshVirtualDevicePositions();
            RefreshLayers();
        }

        // Applies the current MappingLayers device-layout overrides to every
        // keycap on every virtual device. Called after import so newly-loaded
        // positions take effect without tearing down and rebuilding the VMs.
        // Keys with no override are returned to their grid-computed defaults.
        private void RefreshVirtualDevicePositions()
        {
            foreach (var device in VirtualDevices)
            {
                if (device.DeviceType == RGBDeviceType.Keyboard) continue;
                var overrides = MappingLayers.GetDeviceLayoutOverrides(device.DeviceId);
                foreach (var keycap in device.Keycaps)
                {
                    if (overrides != null && overrides.TryGetValue(keycap.LedType, out var pos))
                    {
                        keycap.X = pos.X;
                        keycap.Y = pos.Y;
                    }
                    else
                    {
                        keycap.X = keycap.DefaultX;
                        keycap.Y = keycap.DefaultY;
                    }
                }
            }
        }

        private void OnKeyboardLayoutChanged(object sender, KeyboardLayoutChangedEventArgs e)
            => ApplyKeyboardLayoutChange(e.OldLayout, e.NewLayout, e.RemapLayers);

        private int AddLayerInternal(LayerType layerType, Guid deviceId, RGBDeviceType deviceType)
        {
            int zIndex = Layers.Count + 1;
            int newId = MappingLayers.AddLayer(
                MappingLayers.CountLayers(),
                layerType,
                deviceId,
                deviceType,
                0,
                zIndex,
                false,
                new Dictionary<int, LedId>(),
                false,
                LayerModes.Interpolate);

            var layer = MappingLayers.GetLayer(newId);
            if (layer != null && deviceId == SelectedDevice?.DeviceId)
                InsertDynamicBelowEffect(WrapLayer(layer));

            MappingLayers.SaveMappings();
            return newId;
        }

        // New dynamics land at the top of the dynamic range — just below the
        // Effect pin when present, otherwise at index 0. Matches the old flow
        // where newly added layers got the highest zindex and surfaced on top.
        private void InsertDynamicBelowEffect(LayerItemViewModel vm)
        {
            int insertAt = Layers.Count > 0 && Layers[0].RootLayerType == LayerType.EffectLayer
                ? 1
                : 0;
            Layers.Insert(insertAt, vm);
            RenumberZIndex();
        }

        // Every device must carry exactly one BaseLayer (pinned to lowest z) and
        // one EffectLayer (pinned to highest z). Seed whichever is missing so
        // older mapping files — or builds that persisted without them — still
        // come up with the required structural layers.
        private static void EnsureBaseAndEffectLayers(Guid deviceId, RGBDeviceType deviceType)
        {
            var existing = MappingLayers.GetLayers().Values
                .Where(l => l.deviceGuid == deviceId)
                .ToList();

            bool hasBase = existing.Any(l => l.rootLayerType == LayerType.BaseLayer);
            bool hasEffect = existing.Any(l => l.rootLayerType == LayerType.EffectLayer);

            int maxZ = existing.Count == 0 ? 0 : existing.Max(l => l.zindex);

            if (!hasBase)
            {
                MappingLayers.AddLayer(0, LayerType.BaseLayer, deviceId, deviceType,
                    0, 1, true, new Dictionary<int, LedId>(), false, LayerModes.None);
            }

            if (!hasEffect)
            {
                int effectZ = Math.Max(maxZ + 1, 2);
                MappingLayers.AddLayer(MappingLayers.CountLayers(), LayerType.EffectLayer, deviceId, deviceType,
                    0, effectZ, true, new Dictionary<int, LedId>(), false, LayerModes.None);
            }
        }

        // Below this LED count, a "keyboard" is almost certainly a zone-lit
        // board (1-5 zone Razer / Logitech / Corsair models) rather than a
        // per-key board. Drawing the full QWERTY layout for those gives the
        // user 104 mappable keycaps with only a handful that actually paint
        // - confusing, and most user mappings silently no-op. Below the
        // threshold we drop to the same flat-grid renderer that mice,
        // headsets, Hue and LIFX use. 20 is above any reasonable zone count
        // (the densest zone keyboards top out around 12) and well below the
        // ~40 minimum a real per-key keyboard exposes (alphabet alone is 26).
        private const int ZoneKeyboardLedThreshold = 20;

        private VirtualDeviceViewModel BuildVirtualDevice(Guid deviceId, IRGBDevice device)
        {
            var available = new HashSet<LedId>(device.Select(l => l.Id));
            var layout = AppSettings.GetSettings().keyboardLayout;

            bool isKeyboard = device.DeviceInfo.DeviceType == RGBDeviceType.Keyboard;
            bool isPerKeyKeyboard = isKeyboard && available.Count >= ZoneKeyboardLedThreshold;

            VirtualDeviceViewModel vdvm;
            if (isPerKeyKeyboard)
            {
                vdvm = VirtualDeviceViewModel.BuildForKeyboard(deviceId, device.DeviceInfo.DeviceName, layout, available);
            }
            else
            {
                // HashSet<LedId> enumerates in hash order — that's why users saw
                // "Mouse 20, Mouse 5, Mouse 17" instead of Mouse 1..n. Sort by the
                // LedId enum value so per-device key groups (Mouse1..MouseN,
                // Custom1..CustomN) render in natural ascending order. Zone-lit
                // keyboards fall through here too: their LedIds (Keyboard_Custom1..N
                // or a handful of named keys) get the same flat grid render as
                // any non-keyboard device.
                var keys = available
                    .OrderBy(id => (int)id)
                    .Select(id => new KeyboardKey(id.ToString(), id))
                    .ToList();
                // For zone-lit keyboards, report LedMatrix as the VM's device
                // type so VirtualDeviceViewModel.Build()'s internal routing
                // chooses BuildNonKeyboardLayout (flat grid) instead of
                // BuildKeyboardLayout (full QWERTY), and so SupportsDragReposition
                // / RefreshVirtualDevicePositions treat the tiles as user-
                // repositionable like any other non-keyboard device. The
                // underlying RGB.NET device.DeviceInfo.DeviceType remains
                // Keyboard for everyone else (RGBController, MappingLayers,
                // LayerCopier, raid-effect device-type gates).
                var vmDeviceType = isKeyboard ? RGBDeviceType.LedMatrix : device.DeviceInfo.DeviceType;
                vdvm = VirtualDeviceViewModel.BuildFromKeys(deviceId, device.DeviceInfo.DeviceName, vmDeviceType, keys, available);
            }

            vdvm.SetPickKeyCallback(PickKey);
            return vdvm;
        }

        private LayerItemViewModel WrapLayer(Layer layer)
        {
            return new LayerItemViewModel(
                layer,
                onEdit: id => ToggleEditing(id),
                onCopy: id => DuplicateLayer(id),
                onDelete: id => RemoveLayer(id),
                onClearKeys: ClearKeySelection,
                onReverseKeys: ReverseKeySelection,
                onUndoKeys: UndoKeySelection,
                onLayerStateChanged: () => { if (!IsPreviewing) VisualiseLayers(); });
        }

        // Flip the edit flag on the clicked layer, clearing it on every other
        // layer so only one card is highlighted at a time. Commits any
        // in-progress key selection from the previously-editing layer and loads
        // the new target's saved keys into _pendingKeySelection so virtual
        // keycap badges reflect the current mapped state immediately.
        private void ToggleEditing(int layerId)
        {
            var target = Layers.FirstOrDefault(l => l.LayerId == layerId);
            if (target == null) return;

            bool turningOn = !target.IsEditing;

            // Commit and clear any in-progress key selection before switching.
            var currentEditing = Layers.FirstOrDefault(l => l.IsEditing);
            if (currentEditing != null)
                CommitPendingKeySelection(currentEditing.LayerId);
            _pendingKeySelection.Clear();

            foreach (var l in Layers) l.IsEditing = false;

            if (turningOn)
            {
                target.IsEditing = true;
                var layer = MappingLayers.GetLayer(layerId);
                if (layer?.deviceLeds != null)
                {
                    foreach (var kvp in layer.deviceLeds)
                        _pendingKeySelection[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // Edit mode closed — repaint the static layer view so the
                // committed key selection shows immediately without preview.
                if (!IsPreviewing) VisualiseLayers();
            }

            SyncKeycapEditBadges();
        }

        private void RenumberZIndex()
        {
            // Top of the list = highest z (renders last, on top). Bottom = lowest
            // z (renders first). Assigning Count-i keeps the visual ordering and
            // the persisted zindex semantics in sync.
            for (int i = 0; i < Layers.Count; i++)
            {
                int z = Layers.Count - i;
                if (Layers[i].ZIndex != z) Layers[i].ZIndex = z;
            }
        }

        // Called by VirtualDeviceView when a keycap is clicked while any layer
        // is in edit mode. Toggles the led into/out of _pendingKeySelection and
        // shifts 1-based indices down when a middle entry is removed, mirroring
        // the old Uc_Mappings OnKeyCapPressed flow.
        public void PickKey(LedId ledId)
        {
            if (!Layers.Any(l => l.IsEditing)) return;

            if (_pendingKeySelection.ContainsValue(ledId))
            {
                var entry = _pendingKeySelection.First(kvp => kvp.Value == ledId);
                int removedKey = entry.Key;
                _pendingKeySelection.Remove(removedKey);
                var toShift = _pendingKeySelection
                    .Where(kvp => kvp.Key > removedKey)
                    .OrderBy(kvp => kvp.Key)
                    .ToList();
                foreach (var kvp in toShift)
                {
                    _pendingKeySelection.Remove(kvp.Key);
                    _pendingKeySelection[kvp.Key - 1] = kvp.Value;
                }
            }
            else
            {
                int nextIndex = _pendingKeySelection.Count == 0
                    ? 1
                    : _pendingKeySelection.Keys.Max() + 1;
                _pendingKeySelection[nextIndex] = ledId;
            }

            SyncKeycapEditBadges();
        }

        private void ClearKeySelection()
        {
            _pendingKeySelection.Clear();
            SyncKeycapEditBadges();
        }

        private void ReverseKeySelection()
        {
            if (_pendingKeySelection.Count == 0) return;
            var values = _pendingKeySelection
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value)
                .ToList();
            values.Reverse();
            _pendingKeySelection.Clear();
            for (int i = 0; i < values.Count; i++)
                _pendingKeySelection[i + 1] = values[i];
            SyncKeycapEditBadges();
        }

        private void UndoKeySelection()
        {
            var editingLayer = Layers.FirstOrDefault(l => l.IsEditing);
            if (editingLayer == null) return;
            var layer = MappingLayers.GetLayer(editingLayer.LayerId);
            _pendingKeySelection.Clear();
            if (layer?.deviceLeds != null)
                foreach (var kvp in layer.deviceLeds)
                    _pendingKeySelection[kvp.Key] = kvp.Value;
            SyncKeycapEditBadges();
        }

        private void CommitPendingKeySelection(int layerId)
        {
            var layer = MappingLayers.GetLayer(layerId);
            if (layer == null) return;
            layer.deviceLeds = new Dictionary<int, LedId>(_pendingKeySelection);
            layer.requestUpdate = true;
            MappingLayers.UpdateLayer(layer);
            System.Threading.Tasks.Task.Run(() => MappingLayers.SaveMappings());
        }

        // Writes IsEditing + EditIndex onto every keycap of the currently
        // displayed virtual device so the canvas reflects _pendingKeySelection.
        private void SyncKeycapEditBadges()
        {
            var device = SelectedVirtualDevice;
            if (device == null) return;
            var ledToIndex = _pendingKeySelection.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            bool anyEditing = Layers.Any(l => l.IsEditing);
            foreach (var keycap in device.Keycaps)
            {
                bool inSelection = false;
                int idx = 0;
                if (anyEditing) inSelection = ledToIndex.TryGetValue(keycap.LedType, out idx);
                keycap.IsEditing = inSelection;
                keycap.EditIndex = inSelection ? idx.ToString() : string.Empty;
            }
        }

        private void RefreshIsDeviceEnabled()
        {
            _suspendDeviceEnabledSync = true;
            try
            {
                if (_connectedDevices == null || SelectedDevice == null)
                {
                    IsSelectedDeviceEnabled = true;
                    return;
                }
                if (!_connectedDevices.TryGetValue(SelectedDevice.DeviceId, out var device))
                {
                    IsSelectedDeviceEnabled = true;
                    return;
                }
                var activeDevices = RGBController.GetActiveDevices();
                IsSelectedDeviceEnabled = activeDevices == null
                    || !activeDevices.TryGetValue(device, out bool active)
                    || active;
            }
            finally
            {
                _suspendDeviceEnabledSync = false;
            }
        }

        // Paints each keycap with its layer's accent color when not in preview.
        // If a specific layer is selected for display, only that layer's keys
        // are highlighted — all others reset to DarkGray, giving the user a
        // clear picture of exactly which keys belong to that layer.
        public void VisualiseLayers()
        {
            var device = SelectedVirtualDevice;
            if (device == null) return;

            var keycapByLed = device.Keycaps
                .Where(k => !k.IsEditing)
                .ToDictionary(k => k.LedType, k => k);

            foreach (var keycap in keycapByLed.Values)
                keycap.FillColor = System.Drawing.Color.DarkGray;

            var layers = MappingLayers.GetLayers().Values
                .Where(l => l.deviceGuid == device.DeviceId)
                .OrderBy(l => l.zindex);

            if (_selectedLayerIdForDisplay >= 0)
            {
                // Selection mode: highlight only the selected layer's keys.
                var selLayer = MappingLayers.GetLayer(_selectedLayerIdForDisplay);
                if (selLayer?.deviceLeds != null)
                {
                    var selColor = (System.Drawing.Color)EnumExtensions
                        .GetAttribute<System.ComponentModel.DefaultValueAttribute>(selLayer.rootLayerType).Value;
                    foreach (var ledId in selLayer.deviceLeds.Values)
                    {
                        if (keycapByLed.TryGetValue(ledId, out var keycap))
                            keycap.FillColor = selColor;
                    }
                }
                return;
            }

            // Normal mode: paint all enabled layers in zindex order (ascending
            // so higher-z layers paint over lower-z, matching the render stack).
            foreach (var layer in layers)
            {
                if (layer.rootLayerType == LayerType.BaseLayer && !layer.Enabled) continue;
                if (!layer.Enabled || layer.rootLayerType == LayerType.EffectLayer) continue;
                if (layer.deviceLeds == null) continue;

                var highlight = (System.Drawing.Color)EnumExtensions
                    .GetAttribute<System.ComponentModel.DefaultValueAttribute>(layer.rootLayerType).Value;

                foreach (var ledId in layer.deviceLeds.Values)
                {
                    if (keycapByLed.TryGetValue(ledId, out var keycap))
                        keycap.FillColor = highlight;
                }
            }
        }

        // Reads live LED colors from the RGB surface and applies them to the
        // keycaps so the virtual device mirrors the hardware state.
        private void VisualisePreview()
        {
            var device = SelectedVirtualDevice;
            if (device == null) return;
            if (_connectedDevices == null || SelectedDevice == null) return;
            if (!_connectedDevices.TryGetValue(SelectedDevice.DeviceId, out var rgbDevice)) return;

            var keycapByLed = device.Keycaps
                .Where(k => !k.IsEditing)
                .ToDictionary(k => k.LedType, k => k);

            foreach (var led in rgbDevice)
            {
                if (!keycapByLed.TryGetValue(led.Id, out var keycap)) continue;
                keycap.FillColor = System.Drawing.Color.FromArgb(
                    (int)(led.Color.A * 255),
                    (int)(led.Color.R * 255),
                    (int)(led.Color.G * 255),
                    (int)(led.Color.B * 255));
            }
        }

        // Called from Surface_Updating (background thread). Throttled to one
        // pending UI dispatch at a time so rapid surface ticks don't queue up.
        private void OnPreviewTick()
        {
            if (_previewUpdatePending) return;
            _previewUpdatePending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _previewUpdatePending = false;
                if (IsPreviewing) VisualisePreview();
            }, DispatcherPriority.Background);
        }

        // Toggles the selected-for-display layer. Clicking the same layer again
        // deselects it (returning to the all-layers view).
        public void SelectLayer(int layerId)
        {
            // Negative id = forced deselect; positive = toggle.
            int newSelection = layerId < 0 ? -1
                : _selectedLayerIdForDisplay == layerId ? -1
                : layerId;

            foreach (var lvm in Layers)
                lvm.IsSelected = lvm.LayerId == newSelection;

            _selectedLayerIdForDisplay = newSelection;
            if (!IsPreviewing) VisualiseLayers();
        }

        // Clears the selected layer and returns to the all-layers view.
        // Called from the view when the user clicks in the empty list area.
        public void ClearLayerSelection()
        {
            if (_selectedLayerIdForDisplay < 0) return;
            foreach (var lvm in Layers) lvm.IsSelected = false;
            _selectedLayerIdForDisplay = -1;
            if (!IsPreviewing) VisualiseLayers();
        }

        private void OnJobChanged()
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var layer in Layers)
                    layer.RefreshHelpText();
            });
        }
    }
}
