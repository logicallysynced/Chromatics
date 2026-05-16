using RGB.NET.Core;
using Windows.Devices.Lights;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    public class DynamicLightingDeviceInfo : IRGBDeviceInfo
    {
        public DynamicLightingDeviceInfo(DynamicLightingClientDefinition def)
        {
            DeviceName = def.DisplayName;
            // The OS doesn't expose the OEM/manufacturer string through
            // LampArray itself — it's part of the underlying USB descriptor
            // but Microsoft's WinRT surface drops it. Use a stable label
            // instead of a noisy "Unknown".
            Manufacturer = "Windows Dynamic Lighting";
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
