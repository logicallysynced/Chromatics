using Chromatics.Models;
using System;
using System.Collections.Generic;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    // Maps USB vendor ids to the Chromatics vendor-provider name that
    // would otherwise own the same physical device. Used by both the
    // DynamicLightingRGBDeviceProvider's auto-dedup logic (skip a
    // LampArray device if its OEM has a Chromatics vendor provider
    // already enabled) and the conflict-popup logic in
    // SettingsViewModel.
    //
    // Default behaviour when both providers cover a device: the
    // existing vendor SDK wins. This is the safer default because:
    //
    //   1. The vendor SDK predates Dynamic Lighting and tends to be
    //      better tested per-device.
    //   2. The vendor SDK doesn't have the foreground/background
    //      priority gate Dynamic Lighting has, so it works during
    //      gameplay regardless of which package identity Chromatics
    //      ships with.
    //
    // Users who want Dynamic Lighting to win on a specific device can
    // override per-device in the Mapping tab (separate UI commit;
    // this layer just handles the default).
    internal static class DynamicLightingVendorOverlap
    {
        // Vendor IDs of OEMs whose hardware appears in BOTH a Chromatics
        // vendor provider AND Windows Dynamic Lighting. The list is
        // narrow because Dynamic Lighting only ships with a subset of
        // OEMs that signed onto the HID Lighting and Illumination
        // standard. CoolerMaster, Wooting, Novation, Corsair, OpenRGB
        // are NOT in this list — none of them ship Dynamic Lighting
        // firmware as of 2026.
        private static readonly Dictionary<uint, OverlapEntry> _byVendorId = new()
        {
            // Razer
            [0x1532] = new("Razer", s => s.deviceRazerEnabled),
            // Logitech G LIGHTSYNC + general Logitech
            [0x046D] = new("Logitech", s => s.deviceLogitechEnabled),
            // ASUS ROG
            [0x0B05] = new("ASUS", s => s.deviceAsusEnabled),
            // MSI Mystic Light
            [0x1462] = new("MSI", s => s.deviceMsiEnabled),
            // SteelSeries Apex
            [0x1038] = new("SteelSeries", s => s.deviceSteelseriesEnabled),
        };

        // Returns the vendor display name when the supplied USB vendor id
        // belongs to an OEM whose Chromatics provider is currently enabled.
        // Returns null when there's no overlap (either an unknown OEM, or
        // the OEM's Chromatics provider isn't currently enabled).
        public static string TryGetEnabledVendorOwner(uint hardwareVendorId, SettingsModel settings)
        {
            if (settings == null) return null;
            if (!_byVendorId.TryGetValue(hardwareVendorId, out var entry)) return null;
            return entry.IsEnabled(settings) ? entry.DisplayName : null;
        }

        // List the display names of every Chromatics vendor provider
        // currently enabled whose hardware overlaps with Dynamic Lighting.
        // Used by the SettingsViewModel toggle to show a one-time
        // conflict popup when the user enables Dynamic Lighting.
        public static IReadOnlyList<string> GetEnabledOverlappingVendorNames(SettingsModel settings)
        {
            if (settings == null) return Array.Empty<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var (_, entry) in _byVendorId)
            {
                if (!entry.IsEnabled(settings)) continue;
                if (seen.Add(entry.DisplayName)) result.Add(entry.DisplayName);
            }
            return result;
        }

        // True when the supplied vendor display name (case-insensitive)
        // matches one of the Dynamic-Lighting-overlapping OEMs. Used to
        // gate the conflict popup on the vendor toggles in
        // SettingsViewModel — only Razer / Logitech / ASUS / MSI /
        // SteelSeries trigger the popup; CoolerMaster, Wooting, etc.
        // don't because they don't overlap with Dynamic Lighting.
        public static bool IsOverlappingVendorName(string vendorName)
        {
            if (string.IsNullOrEmpty(vendorName)) return false;
            foreach (var (_, entry) in _byVendorId)
                if (string.Equals(entry.DisplayName, vendorName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private sealed class OverlapEntry
        {
            public string DisplayName { get; }
            public Func<SettingsModel, bool> IsEnabled { get; }

            public OverlapEntry(string displayName, Func<SettingsModel, bool> isEnabled)
            {
                DisplayName = displayName;
                IsEnabled = isEnabled;
            }
        }
    }
}
