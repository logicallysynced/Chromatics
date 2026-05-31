using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.EVision
{
    public class EVisionDeviceInfo : IRGBDeviceInfo
    {
        public EVisionDeviceInfo(EVisionClientDefinition def)
        {
            Manufacturer = string.IsNullOrEmpty(def.Manufacturer) ? "EVision" : def.Manufacturer;
            Model = string.IsNullOrEmpty(def.Product) ? "EVision Keyboard" : def.Product;
            DeviceName = Model;
            DeviceType = RGBDeviceType.Keyboard;
        }

        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }
    }
}
