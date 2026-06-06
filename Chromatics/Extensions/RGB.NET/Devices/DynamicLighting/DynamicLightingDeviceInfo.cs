using RGB.NET.Core;
using Windows.Devices.Lights;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    public class DynamicLightingDeviceInfo : IRGBDeviceInfo
    {
        public DynamicLightingDeviceInfo(DynamicLightingClientDefinition def)
        {
            // Windows hands us bare model names through DeviceInformation.Name
            // ("G512" rather than "Logitech G512"), and two devices from the
            // same vendor can collide in the Mappings tab combo without the
            // prefix. Re-prefix by looking up the OEM name from the LampArray
            // HardwareVendorId and tag with "(Dynamic Lighting)" so the
            // device is also distinguishable from the same physical board's
            // entry under its vendor SDK provider.
            string baseName = def.DisplayName ?? string.Empty;
            string vendorPrefix = def.LampArray != null
                ? DynamicLightingVendorOverlap.TryGetVendorDisplayName(def.LampArray.HardwareVendorId)
                : null;

            bool alreadyPrefixed = !string.IsNullOrEmpty(vendorPrefix)
                && baseName.StartsWith(vendorPrefix, System.StringComparison.OrdinalIgnoreCase);

            string prefixed = (vendorPrefix == null || alreadyPrefixed)
                ? baseName
                : $"{vendorPrefix} {baseName}".Trim();

            DeviceName = string.IsNullOrWhiteSpace(prefixed)
                ? "Dynamic Lighting device"
                : $"{prefixed} (Dynamic Lighting)";

            Manufacturer = vendorPrefix ?? "Windows Dynamic Lighting";
            Model = def.LampArray?.LampArrayKind.ToString() ?? "LampArray";
            DeviceType = MapDeviceType(def.LampArray?.LampArrayKind ?? LampArrayKind.Undefined);
        }

        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }

        // LampArrayKind → RGBDeviceType. The LampArrayKind enum carries
        // OEM intent ("this device is a keyboard / mouse / chassis / ...");
        // we map it onto the closest RGBDeviceType so the Mapping tab
        // groups Dynamic Lighting devices with the right vendor counterparts.
        private static RGBDeviceType MapDeviceType(LampArrayKind kind)
        {
            return kind switch
            {
                LampArrayKind.Keyboard       => RGBDeviceType.Keyboard,
                LampArrayKind.Mouse          => RGBDeviceType.Mouse,
                LampArrayKind.GameController => RGBDeviceType.GameController,
                LampArrayKind.Chassis        => RGBDeviceType.Mainboard,
                LampArrayKind.Wearable       => RGBDeviceType.LedController,
                LampArrayKind.Furniture      => RGBDeviceType.LedController,
                LampArrayKind.Art            => RGBDeviceType.LedController,
                LampArrayKind.Peripheral     => RGBDeviceType.LedController,
                LampArrayKind.Scene          => RGBDeviceType.LedController,
                LampArrayKind.Notification   => RGBDeviceType.LedController,
                _                            => RGBDeviceType.Unknown,
            };
        }
    }
}
