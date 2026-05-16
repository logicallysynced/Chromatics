using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight
{
    public class YeelightDeviceInfo : IRGBDeviceInfo
    {
        public YeelightDeviceInfo(YeelightClientDefinition def)
        {
            var product = YeelightModelCatalog.GetOrDefault(def.Model);

            DeviceName = string.IsNullOrEmpty(def.Label) ? product.DisplayName : def.Label;
            Manufacturer = "Yeelight";
            Model = product.DisplayName;
            DeviceType = product.DeviceType;
        }

        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }
    }
}
