using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        }

        public void Dispose()
        {
            AppSettings.KeyboardLayoutChanged -= OnKeyboardLayoutChanged;
        }

        partial void OnSelectedDeviceChanged(DeviceOptionItem value)
        {
            SelectedVirtualDevice = value == null
                ? null
                : VirtualDevices.FirstOrDefault(v => v.DeviceId == value.DeviceId);
            RefreshLayers();
        }

        partial void OnIsPreviewingChanged(bool value) => MappingLayers.SetPreview(value);

        // Populate device list from the live RGB surface and add defaults for
        // any new device that doesn't yet have layers in persistence.
        public void RefreshDevices(IReadOnlyDictionary<Guid, IRGBDevice> connectedDevices)
        {
            if (connectedDevices == null) return;

            var keep = new HashSet<Guid>(connectedDevices.Keys);

            for (int i = Devices.Count - 1; i >= 0; i--)
            {
                if (!keep.Contains(Devices[i].DeviceId)) Devices.RemoveAt(i);
            }
            for (int i = VirtualDevices.Count - 1; i >= 0; i--)
            {
                if (!keep.Contains(VirtualDevices[i].DeviceId)) VirtualDevices.RemoveAt(i);
            }

            foreach (var kvp in connectedDevices)
            {
                if (Devices.Any(d => d.DeviceId == kvp.Key)) continue;

                Devices.Add(new DeviceOptionItem(kvp.Key, kvp.Value.DeviceInfo.DeviceName, kvp.Value.DeviceInfo.DeviceType));

                VirtualDevices.Add(BuildVirtualDevice(kvp.Key, kvp.Value));

                // Only seed defaults if the device has NO layers at all. If any
                // were restored from persistence we leave them alone and just
                // top up any missing Base/Effect pins.
                bool hasAny = MappingLayers.GetLayers().Values.Any(l => l.deviceGuid == kvp.Key);
                if (!hasAny)
                    CreateDefaultLayers(kvp.Key, kvp.Value);
                else
                    EnsureBaseAndEffectLayers(kvp.Key, kvp.Value.DeviceInfo.DeviceType);
            }

            SelectedDevice ??= Devices.FirstOrDefault();

            // If SelectedDevice was already set, partial-method didn't run;
            // sync the virtual-device pointer manually so a freshly-enumerated
            // device swaps its keyboard in without needing a dropdown change.
            if (SelectedDevice != null && (SelectedVirtualDevice == null ||
                SelectedVirtualDevice.DeviceId != SelectedDevice.DeviceId))
            {
                SelectedVirtualDevice = VirtualDevices.FirstOrDefault(v => v.DeviceId == SelectedDevice.DeviceId);
            }

            // Device set may have changed while SelectedDevice held steady —
            // re-pull layers so newly-seeded rows appear in the list.
            RefreshLayers();
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
        }

        public void RemoveLayer(int layerId)
        {
            var vm = Layers.FirstOrDefault(l => l.LayerId == layerId);
            if (vm == null) return;
            if (vm.RootLayerType != LayerType.DynamicLayer) return;

            Layers.Remove(vm);
            MappingLayers.RemoveLayer(layerId);
            RenumberZIndex();
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

            return newId;
        }

        public void ApplyKeyboardLayoutChange(KeyboardLocalization from, KeyboardLocalization to)
        {
            MappingLayers.RemapLedIdsForLayoutChange(from, to);
            MappingLayers.SaveMappings();
            RefreshLayers();
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
            RefreshLayers();
        }

        private void OnKeyboardLayoutChanged(object sender, KeyboardLayoutChangedEventArgs e)
            => ApplyKeyboardLayoutChange(e.OldLayout, e.NewLayout);

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

        private VirtualDeviceViewModel BuildVirtualDevice(Guid deviceId, IRGBDevice device)
        {
            var available = new HashSet<LedId>(device.Select(l => l.Id));
            var layout = AppSettings.GetSettings().keyboardLayout;

            if (device.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
                return VirtualDeviceViewModel.BuildForKeyboard(deviceId, device.DeviceInfo.DeviceName, layout, available);

            // HashSet<LedId> enumerates in hash order — that's why users saw
            // "Mouse 20, Mouse 5, Mouse 17" instead of Mouse 1..n. Sort by the
            // LedId enum value so per-device key groups (Mouse1..MouseN,
            // Custom1..CustomN) render in natural ascending order.
            var keys = available
                .OrderBy(id => (int)id)
                .Select(id => new KeyboardKey(id.ToString(), id))
                .ToList();
            return VirtualDeviceViewModel.BuildFromKeys(deviceId, device.DeviceInfo.DeviceName, device.DeviceInfo.DeviceType, keys, available);
        }

        private LayerItemViewModel WrapLayer(Layer layer)
        {
            return new LayerItemViewModel(
                layer,
                onEdit: id => ToggleEditing(id),
                onCopy: id => DuplicateLayer(id),
                onDelete: id => RemoveLayer(id));
        }

        // Flip the edit flag on the clicked layer, clearing it on every other
        // layer so only one card is highlighted at a time. The actual
        // keycap-picking wiring plugs in on top of this — the flag is the
        // visual anchor both sides share.
        private void ToggleEditing(int layerId)
        {
            var target = Layers.FirstOrDefault(l => l.LayerId == layerId);
            if (target == null) return;

            bool turningOn = !target.IsEditing;
            foreach (var l in Layers) l.IsEditing = false;
            target.IsEditing = turningOn;
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
    }
}
