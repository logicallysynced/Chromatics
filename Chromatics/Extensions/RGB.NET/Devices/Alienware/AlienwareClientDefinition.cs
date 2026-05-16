using Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware
{
    // Runtime descriptor for one adopted Alienware AlienFX device.
    // Captures everything the UpdateQueue and Device need from
    // discovery time so they don't have to re-probe per frame.
    public sealed class AlienwareClientDefinition
    {
        public AlienwareClientDefinition(
            int vendorId,
            int productId,
            string manufacturer,
            string product,
            AlienwareApiVersion apiVersion,
            int lightCount,
            int reportLength,
            string devicePath)
        {
            VendorId = vendorId;
            ProductId = productId;
            Manufacturer = manufacturer ?? string.Empty;
            Product = product ?? string.Empty;
            ApiVersion = apiVersion;
            LightCount = lightCount;
            ReportLength = reportLength;
            DevicePath = devicePath ?? string.Empty;
        }

        public int VendorId { get; }
        public int ProductId { get; }
        public string Manufacturer { get; }
        public string Product { get; }

        // Which AlienFX HID dialect this device speaks. Decided by the
        // discovery probe from VID + report-length geometry; the
        // UpdateQueue dispatches to the corresponding protocol builder.
        public AlienwareApiVersion ApiVersion { get; }

        // For per-key (V5/V8) devices: number of addressable lights the
        // firmware exposes. We map each one to LedId.Custom1+i.
        // For zone (V4) devices: number of named zones.
        public int LightCount { get; }

        // HID report length the device negotiated. V5 uses the device's
        // FeatureReportByteLength (typically 65); V8 uses
        // OutputReportByteLength (typically 65); V4 uses its own.
        // Buffers are sized to this value.
        public int ReportLength { get; }

        // Stable identity for matching against persisted SettingsModel
        // entries. AlienFX devices generally only have one of each
        // model per machine but we include the device path for
        // disambiguation when there are duplicates.
        public string DevicePath { get; }

        public string Identity => $"{VendorId:X4}:{ProductId:X4}:{Manufacturer}:{Product}";
    }
}
