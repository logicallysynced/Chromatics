using HidSharp;
using System;
using System.Collections.Generic;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon.Protocol
{
    // HidSharp-based discovery for Redragon mice on the shared OpenRGB
    // protocol family. Walks the local HID device list, filters by
    // Redragon's USB vendor id (0x04D9), and matches PID against the
    // RedragonMouseModel enum.
    //
    // Mirrors the Alienware pattern: returns a flat candidate list. The
    // provider's LoadDevices walks the result, opens each HidDevice, and
    // builds one RedragonDevice per detected hardware. The Mapping tab is
    // the user's per-device toggle once the provider is enabled.
    //
    // PIDs outside the curated table are intentionally skipped. Redragon
    // ships many non-mouse products on the same VID (keyboards, headsets,
    // gamepads) — none of which speak this protocol. Wandering off-list
    // would brick devices that share the VID with a different firmware.
    internal static class RedragonDiscovery
    {
        public const int VidRedragon = 0x04D9;

        // Redragon mouse interface descriptors: USB interface 2, HID usage
        // page 0xFFA0 (vendor-defined). HidSharp surfaces the HID interface
        // as a single HidDevice per interface, so we want only the one
        // whose usage page is 0xFFA0 — that's the HID interface OpenRGB
        // writes feature reports to. The other interfaces on the same VID
        // are the mouse-input interfaces and don't accept these reports.
        public const ushort RedragonUsagePage = 0xFFA0;

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

            foreach (var hid in hidDevices)
            {
                int vid, pid;
                try { vid = hid.VendorID; pid = hid.ProductID; } catch { continue; }
                if (vid != VidRedragon) continue;

                if (!Enum.IsDefined(typeof(RedragonMouseModel), pid)) continue;
                var model = (RedragonMouseModel)pid;
                if (model == RedragonMouseModel.Unknown) continue;

                // Filter to the vendor-defined HID interface. Without this
                // check we pick up the mouse-input interface too and the
                // feature-report writes silently fail (or worse, get
                // re-interpreted as a button event). HidSharp's
                // ReportDescriptor.DeviceItems / Usages chain is the only
                // way to read the usage page without opening the device,
                // and even that throws on a couple of older Redragon
                // firmwares. Fall back to "trust the PID and open it" if
                // the descriptor read fails — the apply command we send
                // first is a no-op on the input interface, so the worst
                // case is a wasted handle until the next reconcile.
                if (!IsRedragonControlInterface(hid))
                    continue;

                string mfg = "", prod = "";
                try { mfg  = hid.GetManufacturer() ?? ""; } catch { /* ignore */ }
                try { prod = hid.GetProductName() ?? ""; } catch { /* ignore */ }

                results.Add(new Candidate
                {
                    Hid = hid,
                    Model = model,
                    Manufacturer = mfg,
                    Product = prod,
                });
            }

            return results;
        }

        private static bool IsRedragonControlInterface(HidDevice hid)
        {
            try
            {
                var descriptor = hid.GetReportDescriptor();
                foreach (var item in descriptor.DeviceItems)
                {
                    foreach (var usage in item.Usages.GetAllValues())
                    {
                        // Usage value is (page << 16) | id. We only care about
                        // the page byte (high 16 bits).
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
                // descriptor parsing fails. The protocol's apply command is
                // benign on wrong interfaces.
                return true;
            }
        }
    }
}
