using Chromatics.Core;
using Chromatics.Enums;
using HidSharp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Chromatics.Extensions.RGB.NET.Devices.EVision.Protocol
{
    // HidSharp-based discovery for EVision-firmware keyboards (Sonix
    // VS11K28A and its rebrands). Mirrors RedragonDiscovery's shape:
    //   - VID + PID filter against EVisionKeyboardModel.
    //   - Interface filter via MI_XX parsing on the Windows device path.
    //   - Usage page filter (vendor-defined 0xFF1C) via the report descriptor.
    //   - Dedupe by (PID, parent-path) to avoid multiple HidDevice
    //     collisions per physical keyboard.
    //
    // OpenRGB's REGISTER_HID_DETECTOR_IP for every device in this
    // family uses interface 1 + usage page 0xFF1C as the sole filter
    // (no usage ID), so we mirror that combination here.
    internal static class EVisionDiscovery
    {
        // Vendor-defined usage page on EVision firmware. OpenRGB's
        // detector calls this EVISION_KEYBOARD_USAGE_PAGE.
        public const ushort EVisionUsagePage = 0xFF1C;

        // USB interface number the lighting control protocol lives on.
        // Interface 0 is the keyboard HID, interface 2 is media keys —
        // both reject the vendor protocol if we send to them.
        public const int ControlInterfaceNumber = 1;

        private static readonly Regex _miPattern = new(
            @"[&#]mi_([0-9a-f]{2})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public sealed class Candidate
        {
            public HidDevice Hid { get; init; }
            public EVisionKeyboardModel Model { get; init; }
            public string Manufacturer { get; init; }
            public string Product { get; init; }
        }

        public static IReadOnlyList<Candidate> Discover()
        {
            var results = new List<Candidate>();

            IEnumerable<HidDevice> hidDevices;
            try { hidDevices = DeviceList.Local.GetHidDevices(); }
            catch { return results; }

            var byKey = new Dictionary<string, Candidate>(StringComparer.OrdinalIgnoreCase);

            foreach (var hid in hidDevices)
            {
                int vid, pid;
                try { vid = hid.VendorID; pid = hid.ProductID; } catch { continue; }

                uint key32 = ((uint)vid << 16) | (uint)(pid & 0xFFFF);
                if (!Enum.IsDefined(typeof(EVisionKeyboardModel), key32)) continue;
                var model = (EVisionKeyboardModel)key32;
                if (model == EVisionKeyboardModel.Unknown) continue;

                string path;
                try { path = hid.DevicePath ?? string.Empty; } catch { continue; }

                int? iface = ExtractInterfaceNumber(path);
                if (iface.HasValue && iface.Value != ControlInterfaceNumber)
                    continue;

                if (!HasEVisionUsagePage(hid))
                    continue;

                string dedupKey = $"{vid:X4}|{pid:X4}|{StripCollectionSuffix(path)}";
                if (byKey.TryGetValue(dedupKey, out var existing))
                {
                    if (GetOutputReportLength(hid) > GetOutputReportLength(existing.Hid))
                        byKey[dedupKey] = MakeCandidate(hid, model);
                    continue;
                }

                byKey[dedupKey] = MakeCandidate(hid, model);
            }

            results.AddRange(byKey.Values);

            if (results.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("[EVision] Discovery resolved ").Append(results.Count).Append(" candidate(s):");
                foreach (var c in results)
                    sb.Append("\n  ").Append(c.Model.DisplayName(c.Hid.VendorID, c.Hid.ProductID))
                      .AppendFormat(" ({0:X4}:{1:X4}) path={2}", c.Hid.VendorID, c.Hid.ProductID, c.Hid.DevicePath);
                Logger.WriteVerbose(sb.ToString());
            }

            return results;
        }

        private static Candidate MakeCandidate(HidDevice hid, EVisionKeyboardModel model)
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

        private static int? ExtractInterfaceNumber(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) return null;
            var m = _miPattern.Match(devicePath);
            if (!m.Success) return null;
            return int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
        }

        private static string StripCollectionSuffix(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) return string.Empty;
            int idx = devicePath.IndexOf("&Col", StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? devicePath.Substring(0, idx) : devicePath;
        }

        private static int GetOutputReportLength(HidDevice hid)
        {
            try { return hid.GetMaxOutputReportLength(); }
            catch { return 0; }
        }

        private static bool HasEVisionUsagePage(HidDevice hid)
        {
            try
            {
                var descriptor = hid.GetReportDescriptor();
                foreach (var item in descriptor.DeviceItems)
                {
                    foreach (var usage in item.Usages.GetAllValues())
                    {
                        ushort page = (ushort)(usage >> 16);
                        if (page == EVisionUsagePage) return true;
                    }
                }
                return false;
            }
            catch
            {
                // Descriptor read failed — keep this candidate so we don't
                // silently lose the device. The interface-number check
                // upstream still rules out the keyboard-input interface.
                return true;
            }
        }
    }
}
