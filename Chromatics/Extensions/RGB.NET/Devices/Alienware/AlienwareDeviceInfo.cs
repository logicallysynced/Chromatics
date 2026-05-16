using Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware
{
    public class AlienwareDeviceInfo : IRGBDeviceInfo
    {
        public AlienwareDeviceInfo(AlienwareClientDefinition def)
        {
            Manufacturer = string.IsNullOrEmpty(def.Manufacturer) ? "Alienware" : def.Manufacturer;
            Model = string.IsNullOrEmpty(def.Product) ? $"AlienFX {def.ApiVersion}" : def.Product;
            DeviceName = string.IsNullOrEmpty(def.Product) ? $"Alienware AlienFX ({def.ApiVersion})" : def.Product;

            // Per-key keyboards (V5 notebook, V8 external) → Keyboard.
            // Zone-based chassis (V4) → Mainboard since the LEDs live on
            // the case rather than a peripheral.
            DeviceType = def.ApiVersion switch
            {
                AlienwareApiVersion.PerKeyV5 => RGBDeviceType.Keyboard,
                AlienwareApiVersion.PerKeyV8 => RGBDeviceType.Keyboard,
                AlienwareApiVersion.ZoneV4   => RGBDeviceType.Mainboard,
                _                            => RGBDeviceType.Unknown,
            };
        }

        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }
    }
}
