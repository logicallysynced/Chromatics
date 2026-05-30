using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon
{
    public class RedragonDeviceInfo : IRGBDeviceInfo
    {
        public RedragonDeviceInfo(RedragonClientDefinition def)
        {
            Manufacturer = string.IsNullOrEmpty(def.Manufacturer) ? "Redragon" : def.Manufacturer;
            Model = string.IsNullOrEmpty(def.Product) ? "Redragon Mouse" : def.Product;
            DeviceName = Model;
            DeviceType = RGBDeviceType.Mouse;
        }

        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }
    }
}
