namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol
{
    // QMK Raw HID transport constants. Reference:
    // https://docs.qmk.fm/#/feature_rawhid (firmware side) and
    // OpenRGB's QMKOpenRGBController + VIA's protocol/constants
    // for the host side.
    internal static class QmkRawHidConstants
    {
        // Vendor-defined HID usage page + usage that every QMK keyboard
        // exposes on its Raw HID interface. Discovery filters HidSharp's
        // enumeration on these two values rather than VID/PID so we work
        // across NovelKeys, KBDFans, Drop, GMMK, Glorious, etc. without
        // a hardcoded allow-list.
        public const ushort RawHidUsagePage = 0xFF60;
        public const ushort RawHidUsage     = 0x61;

        // The actual per-device raw HID report size is read from HIDP_CAPS
        // at discovery time and carried on Candidate / QmkRawHidClientDefinition
        // — stock QMK builds expose 32-byte reports, OpenRGB-QMK builds
        // expose 64-byte reports, and a build that gets it wrong (e.g.
        // RAW_EPSIZE redefined inside openrgb.h but not propagated globally
        // via OPT_DEFS) silently drops every reply. The transport layer
        // honours whatever the firmware advertises rather than hardcoding
        // either side of that.

        // Default request/response wait. Most QMK Raw HID round-trips
        // complete in ~5-20ms over USB Full Speed; 250ms is generous
        // and matches OpenRGB's default for the same protocol.
        public const int  ResponseTimeoutMs = 250;
    }
}
