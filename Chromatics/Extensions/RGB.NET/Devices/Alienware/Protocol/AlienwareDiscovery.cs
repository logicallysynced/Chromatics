using HidSharp;
using System.Collections.Generic;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol
{
    // HidSharp-based discovery for AlienFX devices. Walks the local HID
    // device list, filters by Alienware-relevant USB vendor ids, then
    // probes each candidate's HID descriptor to decide which AlienFX
    // dialect (V4 zone, V5 per-key notebook, V8 per-key external) it
    // speaks. Detection signals are taken from T-Troll's reference SDK
    // (alienfx-tools, MIT licensed) which has the only public
    // documentation of the geometry-based version probe.
    //
    // The result is a flat candidate list — adopt all, then let the
    // user disable specific devices from the Mapping tab if they only
    // want a subset. Mirrors the QmkRawHidDiscovery pattern.
    internal static class AlienwareDiscovery
    {
        // USB vendor ids relevant to AlienFX devices. Filtering up-front
        // keeps the per-device probe loop cheap on machines with hundreds
        // of HID nodes (VR headsets, tablets, fitness trackers all
        // enumerate as HID devices on Windows).
        public const int VidAlienware = 0x187C; // V4 zone (Aurora R7-R14, m15R1-R6 zone, m17R1, Dell G7/G5)
        public const int VidDarfon    = 0x0D62; // V5 per-key notebook (Area51m-R2, x17R2, m15R3+, m17R3)
        public const int VidChicony   = 0x04F2; // V8 per-key external (AW510K, AW920K, AW768, AW410K)

        public sealed class Candidate
        {
            public HidDevice Hid { get; init; }
            public AlienwareApiVersion ApiVersion { get; init; }
            public int ReportLength { get; init; }
            public string Manufacturer { get; init; }
            public string Product { get; init; }

            // V8 / V5 firmware reports the addressable light count
            // dynamically through later interactions. For initial discovery
            // we estimate a sensible default per dialect; the device's
            // first paint frame can grow this if the firmware exposes more.
            public int LightCount { get; init; }
        }

        public static IReadOnlyList<Candidate> Discover()
        {
            var results = new List<Candidate>();

            IEnumerable<HidDevice> hidDevices;
            try { hidDevices = DeviceList.Local.GetHidDevices(); }
            catch { return results; }

            foreach (var hid in hidDevices)
            {
                int vid;
                try { vid = hid.VendorID; } catch { continue; }

                AlienwareApiVersion version;
                int reportLen;
                int lightCount;

                if (vid == VidAlienware)
                {
                    // V4 — Alienware-branded zone chassis. Detected purely
                    // by VID at this layer; the actual report-length probe
                    // happens in the V4 protocol handler when we open the
                    // device. Default report length 34 bytes per T-Troll
                    // (the documented V4 output-report size). Light count
                    // varies per chassis (Aurora R7 has ~4, R14 has ~8); we
                    // expose 16 as a generous upper bound and trim
                    // unbound LEDs at the Device layer.
                    version = AlienwareApiVersion.ZoneV4;
                    reportLen = 34;
                    lightCount = 16;
                }
                else if (vid == VidDarfon)
                {
                    // V5 — notebook per-key keyboards. T-Troll's detection
                    // gates on Usage==0xcc AND OutputReportByteLength==0.
                    // We can't query HID usage through HidSharp without
                    // opening the device; defer the strict check to the
                    // probe step in the provider's Load. Default light
                    // count 100 (typical full-size notebook keyboard); the
                    // firmware tolerates writes to invalid indices.
                    version = AlienwareApiVersion.PerKeyV5;
                    reportLen = AlienwarePerKeyV5Protocol.ReportLength;
                    lightCount = 100;
                }
                else if (vid == VidChicony)
                {
                    // V8 — external per-key keyboards (AW510K, AW920K,
                    // AW768, AW410K). T-Troll's detection gates on
                    // OutputReportByteLength==65. We trust the VID match
                    // here — Chicony makes other peripherals but they
                    // don't enumerate as HID with this VID + 65-byte
                    // output report combo. Default light count 105 covers
                    // the full ANSI 104 + the chassis logo.
                    version = AlienwareApiVersion.PerKeyV8;
                    reportLen = AlienwarePerKeyV8Protocol.ReportLength;
                    lightCount = 105;
                }
                else
                {
                    continue;
                }

                string mfg = "";
                string prod = "";
                try { mfg  = hid.GetManufacturer() ?? ""; } catch { /* ignore */ }
                try { prod = hid.GetProductName() ?? ""; } catch { /* ignore */ }

                results.Add(new Candidate
                {
                    Hid = hid,
                    ApiVersion = version,
                    ReportLength = reportLen,
                    Manufacturer = mfg,
                    Product = prod,
                    LightCount = lightCount,
                });
            }

            return results;
        }
    }
}
