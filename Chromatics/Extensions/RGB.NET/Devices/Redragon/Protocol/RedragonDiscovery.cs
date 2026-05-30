using Chromatics.Core;
using Chromatics.Enums;
using HidSharp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon.Protocol
{
    // HidSharp-based discovery for Redragon mice on the shared OpenRGB
    // protocol family. Walks the local HID device list, filters by
    // Redragon's USB vendor id (0x04D9), the known PID table, USB
    // interface 2, and HID usage page 0xFFA0 — the same combination
    // OpenRGB's RedragonControllerDetect.cpp uses to single out the
    // control interface.
    //
    // The interface filter is load-bearing on multi-interface mice like
    // the M908 Impact. Without it we'd accept every HidDevice that
    // happens to expose usage page 0xFFA0 (the OEM uses 0xFFA0 broadly,
    // so input + mouse-extra interfaces frequently share it). Opening
    // two HidDevice handles against the same physical mouse means the
    // provider creates two RedragonDevice instances, and two update
    // queues at 30Hz each = 60 feature reports per second hammering the
    // same USB endpoint. That race is what manifests as the M908's
    // constant LED flicker on rapid colour changes.
    //
    // PIDs outside the curated table are intentionally skipped. Redragon
    // ships many non-mouse products on the same VID (keyboards, headsets,
    // gamepads) — none of which speak this protocol. Wandering off-list
    // could brick devices that share the VID with a different firmware.
    internal static class RedragonDiscovery
    {
        public const int VidRedragon = 0x04D9;

        // Vendor-defined usage page. OpenRGB calls this REDRAGON_MOUSE_USAGE_PAGE.
        public const ushort RedragonUsagePage = 0xFFA0;

        // USB interface number for the lighting control interface on every
        // Redragon mouse OpenRGB drives. The other interfaces are for HID
        // input (buttons + movement) and don't accept feature-report writes.
        public const int ControlInterfaceNumber = 2;

        // Matches "&MI_XX" or "&Mi_XX" anywhere in a Windows HID device
        // path. Case-insensitive because some firmware revisions report
        // the path in mixed case. The hex digits are the USB interface
        // number — Windows formats it as exactly two hex digits per
        // USB descriptor convention.
        private static readonly Regex _miPattern = new(
            @"[&#]mi_([0-9a-f]{2})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public sealed class Candidate
        {
            public HidDevice Hid { get; init; }
            public RedragonMouseModel Model { get; init; }
            public string Manufacturer { get; init; }
            public string Product { get; init; }
        }

        public static IReadOnlyList<Candidate> Discover()
        {
            var results = new List<Candidate>();

            IEnumerable<HidDevice> hidDevices;
            try { hidDevices = DeviceList.Local.GetHidDevices(); }
            catch { return results; }

            // First pass: collect every candidate that matches VID + PID +
            // interface number, grouped by (PID, parent-path) so we can
            // dedupe collisions where the same physical interface shows up
            // as multiple HidDevices (one per HID collection).
            var byKey = new Dictionary<string, Candidate>(StringComparer.OrdinalIgnoreCase);

            foreach (var hid in hidDevices)
            {
                int vid, pid;
                try { vid = hid.VendorID; pid = hid.ProductID; } catch { continue; }
                if (vid != VidRedragon) continue;

                if (!Enum.IsDefined(typeof(RedragonMouseModel), pid)) continue;
                var model = (RedragonMouseModel)pid;
                if (model == RedragonMouseModel.Unknown) continue;

                string path;
                try { path = hid.DevicePath ?? string.Empty; } catch { continue; }

                int? iface = ExtractInterfaceNumber(path);
                if (iface.HasValue && iface.Value != ControlInterfaceNumber)
                    continue;

                if (!HasRedragonUsagePage(hid))
                    continue;

                // Dedupe key collapses every HID collection that belongs to
                // the same physical (VID, PID, MI_02) interface onto one
                // candidate. Picking by max feature-report length favours
                // the collection that carries the 16-byte control reports
                // over any sibling collection on the same interface.
                string key = $"{pid:X4}|{StripCollectionSuffix(path)}";
                if (byKey.TryGetValue(key, out var existing))
                {
                    if (GetFeatureReportLength(hid) > GetFeatureReportLength(existing.Hid))
                    {
                        byKey[key] = MakeCandidate(hid, model);
                    }
                    continue;
                }

                byKey[key] = MakeCandidate(hid, model);
            }

            results.AddRange(byKey.Values);

            if (results.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("[Redragon] Discovery resolved ").Append(results.Count).Append(" candidate(s):");
                foreach (var c in results)
                    sb.Append("\n  ").Append(c.Model.DisplayName(c.Hid.ProductID))
                      .Append(" (").AppendFormat("{0:X4}:{1:X4}", c.Hid.VendorID, c.Hid.ProductID).Append(") path=").Append(c.Hid.DevicePath);
                Logger.WriteVerbose(sb.ToString());
            }

            return results;
        }

        private static Candidate MakeCandidate(HidDevice hid, RedragonMouseModel model)
        {
            string mfg = "", prod = "";
            try { mfg  = hid.GetManufacturer() ?? ""; } catch { /* ignore */ }
            try { prod = hid.GetProductName() ?? ""; } catch { /* ignore */ }

            return new Candidate
            {
                Hid = hid,
                Model = model,
                Manufacturer = mfg,
                Product = prod,
            };
        }

        // Extract the USB interface number from a HID device path.
        // Returns null when no MI_XX segment is present — single-interface
        // devices (and non-Windows paths) end up here. In that case the
        // caller proceeds with the descriptor probe, since a single-
        // interface mouse can't pick the wrong interface anyway.
        private static int? ExtractInterfaceNumber(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) return null;
            var m = _miPattern.Match(devicePath);
            if (!m.Success) return null;
            return int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
        }

        // Strip the "&Col_XX" or "&col_XX" segment that distinguishes HID
        // collections within the same USB interface. Two paths that
        // differ only in collection should dedupe to one candidate.
        private static string StripCollectionSuffix(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) return string.Empty;
            int idx = devicePath.IndexOf("&Col", StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? devicePath.Substring(0, idx) : devicePath;
        }

        private static int GetFeatureReportLength(HidDevice hid)
        {
            try { return hid.GetMaxFeatureReportLength(); }
            catch { return 0; }
        }

        private static bool HasRedragonUsagePage(HidDevice hid)
        {
            try
            {
                var descriptor = hid.GetReportDescriptor();
                foreach (var item in descriptor.DeviceItems)
                {
                    foreach (var usage in item.Usages.GetAllValues())
                    {
                        // Usage value is (page << 16) | id. We only care about
                        // the page (high 16 bits).
                        ushort page = (ushort)(usage >> 16);
                        if (page == RedragonUsagePage) return true;
                    }
                }
                return false;
            }
            catch
            {
                // Descriptor read failed — keep this candidate so we don't
                // silently lose the device on the firmware versions where
                // descriptor parsing fails. The interface-number filter
                // upstream still rules out the input interfaces.
                return true;
            }
        }
    }
}
