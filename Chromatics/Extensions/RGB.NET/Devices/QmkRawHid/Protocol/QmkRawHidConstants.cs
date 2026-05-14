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

        // QMK Raw HID transfers are 32-byte payloads. Windows HidSharp
        // expects an extra leading report-id byte, so the output buffer
        // is 33 bytes total (report id 0 + 32 data bytes). Reply reports
        // are the same shape minus the leading id on the HidSharp read
        // side (HidStream strips the id from inputs).
        public const int  ReportPayloadBytes  = 32;
        public const int  OutputReportBytes   = ReportPayloadBytes + 1;

        // Default request/response wait. Most QMK Raw HID round-trips
        // complete in ~5-20ms over USB Full Speed; 250ms is generous
        // and matches OpenRGB's default for the same protocol.
        public const int  ResponseTimeoutMs   = 250;
    }
}
