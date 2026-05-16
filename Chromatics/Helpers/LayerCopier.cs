using Chromatics.Layers;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Helpers
{
    // Builds + applies a "copy all layers from device A to device B"
    // operation. Splits into two phases so the user can review the
    // proposed LedId mapping in CopyLayersDialog before committing
    // anything to disk.
    //
    //   1. ComputeDefaultMapping(source, dest)
    //      Returns a fresh Dictionary<LedId, LedId> that maps every
    //      LedId on the source device to the best-fit LedId on the
    //      dest device. Caller mutates this dict via the dialog UI
    //      before invoking Apply.
    //
    //   2. Apply(sourceGuid, destGuid, destDeviceType, mapping)
    //      Walks MappingLayers' layer collection, finds every layer
    //      bound to sourceGuid, and creates a parallel layer on
    //      destGuid with the same root / dynamic type, zindex,
    //      Enabled flag, allowBleed flag, and layer modes — but
    //      with deviceLeds remapped through the supplied mapping.
    //      Original layers on the source device are left alone.
    //
    // Mapping defaults:
    //   - Keyboard → Keyboard: identity match by LedId. Keyboard
    //     layouts are stable across vendors thanks to
    //     KeyLocalization, so Escape on a Razer board lands on
    //     Escape on a Logitech / Alienware / Dynamic Lighting
    //     board without further work.
    //   - Cross-type (Mouse → Headset, etc.) or same-type
    //     non-keyboard: exact LedId match first (Custom1 → Custom1),
    //     falling back to positional index match for LedIds that
    //     don't exist on the dest. Capped at min(sourceLedCount,
    //     destLedCount) so the user doesn't see a half-mapped
    //     dest with phantom LedIds.
    public static class LayerCopier
    {
        // Returns true iff this device pair is a legal copy target.
        // Keyboards can only copy to keyboards (semantic layouts
        // can't sensibly project onto a mouse / chassis / strip).
        // Everything else is permitted to copy onto everything
        // else.
        public static bool IsCopyAllowed(RGBDeviceType source, RGBDeviceType dest)
        {
            bool srcIsKeyboard = source == RGBDeviceType.Keyboard;
            bool dstIsKeyboard = dest == RGBDeviceType.Keyboard;
            if (srcIsKeyboard != dstIsKeyboard) return false;
            return true;
        }

        public static Dictionary<LedId, LedId> ComputeDefaultMapping(IRGBDevice source, IRGBDevice dest)
        {
            var map = new Dictionary<LedId, LedId>();
            if (source == null || dest == null) return map;

            var sourceLeds = source.OrderBy(l => (int)l.Id).ToList();
            var destLeds = dest.OrderBy(l => (int)l.Id).ToList();
            if (sourceLeds.Count == 0 || destLeds.Count == 0) return map;

            var destIds = new HashSet<LedId>(destLeds.Select(l => l.Id));
            bool keyboardPair = source.DeviceInfo.DeviceType == RGBDeviceType.Keyboard
                             && dest.DeviceInfo.DeviceType == RGBDeviceType.Keyboard;

            if (keyboardPair)
            {
                // Identity match. Source LedIds that don't exist on
                // the destination are dropped silently; we surface
                // the gap in the dialog so the user can decide
                // whether to manually remap them.
                foreach (var sled in sourceLeds)
                    if (destIds.Contains(sled.Id))
                        map[sled.Id] = sled.Id;
                return map;
            }

            // Non-keyboard pair (same type or cross-type). Strategy:
            // exact match where the dest has the same LedId; fall
            // back to positional index match otherwise.
            int n = Math.Min(sourceLeds.Count, destLeds.Count);
            for (int i = 0; i < n; i++)
            {
                var sled = sourceLeds[i];
                if (destIds.Contains(sled.Id))
                    map[sled.Id] = sled.Id;
                else
                    map[sled.Id] = destLeds[i].Id;
            }
            return map;
        }

        public sealed class CopyResult
        {
            public int LayersCopied { get; init; }
            public int LedMappingsDropped { get; init; }
        }

        public static CopyResult Apply(
            Guid sourceGuid,
            Guid destGuid,
            RGBDeviceType destDeviceType,
            IReadOnlyDictionary<LedId, LedId> ledMap)
        {
            if (sourceGuid == Guid.Empty || destGuid == Guid.Empty || sourceGuid == destGuid)
                return new CopyResult { LayersCopied = 0, LedMappingsDropped = 0 };

            ledMap ??= new Dictionary<LedId, LedId>();

            // Snapshot to avoid mutating the live dict while iterating.
            var existing = MappingLayers.GetLayers()
                .Where(kvp => kvp.Value.deviceGuid == sourceGuid)
                .Select(kvp => kvp.Value)
                .OrderBy(l => l.layerIndex)
                .ToList();

            if (existing.Count == 0)
                return new CopyResult { LayersCopied = 0, LedMappingsDropped = 0 };

            int layersCopied = 0;
            int droppedMappings = 0;

            foreach (var src in existing)
            {
                var remappedDeviceLeds = new Dictionary<int, LedId>();
                if (src.deviceLeds != null)
                {
                    foreach (var (positionIndex, sourceLedId) in src.deviceLeds)
                    {
                        if (ledMap.TryGetValue(sourceLedId, out var destLedId))
                            remappedDeviceLeds[positionIndex] = destLedId;
                        else
                            droppedMappings++;
                    }
                }

                int newIndex = MappingLayers.GetLayers().Count;
                MappingLayers.AddLayer(
                    index: newIndex,
                    rootLayerType: src.rootLayerType,
                    deviceGuid: destGuid,
                    deviceType: destDeviceType,
                    layerTypeIndex: src.layerTypeindex,
                    zindex: src.zindex,
                    enabled: src.Enabled,
                    deviceLeds: remappedDeviceLeds,
                    allowBleed: src.allowBleed,
                    layerModes: src.layerModes);
                layersCopied++;
            }

            MappingLayers.SaveMappings();
            return new CopyResult
            {
                LayersCopied = layersCopied,
                LedMappingsDropped = droppedMappings,
            };
        }
    }
}
