namespace Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol
{
    // Which AlienFX HID dialect the device speaks. Decided at discovery
    // time from the device's USB vendor id and HID report geometry — see
    // AlienwareDiscovery for the detection rules.
    //
    //   ZoneV4         — 5-zone chassis (Aurora R7-R14 desktops, m15R1-R6
    //                    zone laptops, m17R1, Dell G7/G5). Routed through
    //                    T-Troll's LightFX_SDK.dll because the V4 wire
    //                    format isn't fully documented in their open
    //                    source. Aurora-RGB targets this same path.
    //                    VID 0x187C, ~34-byte output reports.
    //
    //   PerKeyV5       — Per-key notebook keyboards (Area51m-R2, x17R2,
    //                    m15R3 / R4 / R5, m15R6, x15R2, m17R3). Pure
    //                    managed HidSharp via HID feature reports.
    //                    VID 0x0d62, FeatureReportByteLength typically 65.
    //
    //   PerKeyV8       — Per-key external keyboards (AW510K, AW920K,
    //                    AW768, AW410K). Pure managed HidSharp via HID
    //                    write reports. VID 0x04f2, OutputReportByteLength
    //                    typically 65.
    //
    // Mice (V7), monitors (V6), and the headset family aren't covered in
    // this initial release — they share the same protocol families but
    // need additional capability tables we don't have yet.
    public enum AlienwareApiVersion
    {
        Unknown = 0,
        ZoneV4 = 4,
        PerKeyV5 = 5,
        PerKeyV8 = 8,
    }
}
