using Chromatics.Enums;
using Chromatics.Layers;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Helpers
{
    // Layer-by-layer copy from one device to another.
    //
    // Caller builds a list of LayerCopyPlan entries from the dialog
    // (one per source layer the user ticked, plus the per-LedId
    // mapping for any Dynamic layers) and hands it to Apply. The
    // engine handles Base/Effect uniqueness automatically — if the
    // destination already has a Base or Effect layer, the existing
    // one is removed before the new one is added, so the user
    // doesn't end up with two of either.
    //
    // For Dynamic layers, multiple instances are allowed; the new
    // layer is appended without touching anything pre-existing on
    // the destination.
    public static class LayerCopier
    {
        // Returns true iff this device pair is a legal copy target.
        // Keyboards can only copy to keyboards — the consistent
        // ANSI 104 layout across vendors lets per-key mappings
        // project meaningfully. Other device types can cross-copy
        // freely (mouse → headset, chassis → strip).
        public static bool IsCopyAllowed(RGBDeviceType source, RGBDeviceType dest)
        {
            bool srcKb = source == RGBDeviceType.Keyboard;
            bool dstKb = dest == RGBDeviceType.Keyboard;
            if (srcKb != dstKb) return false;
            return true;
        }

        // Default LedId mapping for a SINGLE layer being copied. Only
        // computes entries for LedIds in `usedSourceLedIds` — the set
        // of LedIds the source layer actually paints. Keeps the per-
        // layer mapping UI focused on the keys that matter for that
        // specific layer rather than the entire source device.
        public static Dictionary<LedId, LedId> ComputeDefaultMappingForLayer(
            IReadOnlyCollection<LedId> usedSourceLedIds,
            IRGBDevice source,
            IRGBDevice dest)
        {
            var map = new Dictionary<LedId, LedId>();
            if (usedSourceLedIds == null || source == null || dest == null) return map;
            if (usedSourceLedIds.Count == 0) return map;

            bool keyboardPair = source.DeviceInfo.DeviceType == RGBDeviceType.Keyboard
                             && dest.DeviceInfo.DeviceType == RGBDeviceType.Keyboard;

            var destLeds = dest.OrderBy(l => (int)l.Id).ToList();
            var destIds = new HashSet<LedId>(destLeds.Select(l => l.Id));

            if (keyboardPair)
            {
                // Identity match by LedId for keyboard-pair copies.
                foreach (var ledId in usedSourceLedIds)
                    if (destIds.Contains(ledId))
                        map[ledId] = ledId;
                return map;
            }

            // Non-keyboard pair (same type or cross-type): exact match
            // first; positional fallback otherwise (Nth LED in source
            // ordering → Nth LED in dest ordering).
            var sourceLeds = source.OrderBy(l => (int)l.Id).ToList();
            int n = Math.Min(sourceLeds.Count, destLeds.Count);
            var positionalFallback = new Dictionary<LedId, LedId>();
            for (int i = 0; i < n; i++)
                positionalFallback[sourceLeds[i].Id] = destLeds[i].Id;

            foreach (var ledId in usedSourceLedIds)
            {
                if (destIds.Contains(ledId))
                    map[ledId] = ledId;
                else if (positionalFallback.TryGetValue(ledId, out var fallback))
                    map[ledId] = fallback;
            }
            return map;
        }

        // One copy operation. The view-model passes a list of these
        // into Apply; each represents "copy this source layer onto
        // the destination with this LedId mapping".
        public sealed class LayerCopyPlan
        {
            public Layer SourceLayer { get; init; }

            // Source LedId → destination LedId. Entries missing for
            // an LedId in the source layer's deviceLeds drop that
            // LedId during copy (the destination layer simply
            // doesn't cover it).
            public IReadOnlyDictionary<LedId, LedId> Mapping { get; init; } = new Dictionary<LedId, LedId>();
        }

        public sealed class CopyResult
        {
            public int LayersAdded { get; init; }
            public int LayersReplaced { get; init; }
            public int LedMappingsDropped { get; init; }
        }

        // True when the destination already has a layer of the
        // given root type (Base or Effect). Dialog uses this to
        // decide whether to show the "will replace existing"
        // warning per-row.
        public static bool DestinationAlreadyHasLayerOfType(Guid destGuid, LayerType rootLayerType)
        {
            if (destGuid == Guid.Empty) return false;
            if (rootLayerType != LayerType.BaseLayer && rootLayerType != LayerType.EffectLayer) return false;
            return MappingLayers.GetLayers().Values
                .Any(l => l.deviceGuid == destGuid && l.rootLayerType == rootLayerType);
        }

        public static CopyResult Apply(
            Guid sourceGuid,
            Guid destGuid,
            RGBDeviceType destDeviceType,
            IReadOnlyList<LayerCopyPlan> plans)
        {
            if (sourceGuid == Guid.Empty || destGuid == Guid.Empty || sourceGuid == destGuid)
                return new CopyResult();
            if (plans == null || plans.Count == 0)
                return new CopyResult();

            int added = 0;
            int replaced = 0;
            int dropped = 0;

            foreach (var plan in plans)
            {
                var src = plan.SourceLayer;
                if (src == null) continue;
                var mapping = plan.Mapping ?? new Dictionary<LedId, LedId>();

                // Base / Effect: at most one per device. Remove the
                // existing one on dest (if any) before adding the
                // copy.
                bool isStructural = src.rootLayerType == LayerType.BaseLayer
                                 || src.rootLayerType == LayerType.EffectLayer;
                if (isStructural)
                {
                    var existing = MappingLayers.GetLayers().Values
                        .Where(l => l.deviceGuid == destGuid && l.rootLayerType == src.rootLayerType)
                        .Select(l => l.layerID)
                        .ToList();
                    foreach (var existingId in existing)
                        MappingLayers.RemoveLayer(existingId);
                    if (existing.Count > 0) replaced++;
                    else added++;
                }
                else
                {
                    added++;
                }

                // Remap deviceLeds: drop entries whose source LedId
                // has no mapping; otherwise translate to the dest
                // LedId.
                var remapped = new Dictionary<int, LedId>();
                if (src.deviceLeds != null)
                {
                    foreach (var (positionIndex, sourceLedId) in src.deviceLeds)
                    {
                        if (mapping.TryGetValue(sourceLedId, out var destLedId))
                            remapped[positionIndex] = destLedId;
                        else
                            dropped++;
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
                    deviceLeds: remapped,
                    allowBleed: src.allowBleed,
                    layerModes: src.layerModes);
            }

            MappingLayers.SaveMappings();
            return new CopyResult
            {
                LayersAdded = added,
                LayersReplaced = replaced,
                LedMappingsDropped = dropped,
            };
        }
    }
}
