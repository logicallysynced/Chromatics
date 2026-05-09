using Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX
{
    public class LifxDeviceInfo : IRGBDeviceInfo
    {
        public LifxDeviceInfo(LifxClientDefinition def)
        {
            Mac = def.Mac;
            DeviceName = string.IsNullOrEmpty(def.Label) ? def.Mac : def.Label;
            Manufacturer = "LIFX";

            var product = LifxProductCatalog.GetOrDefault(def.ProductId);
            Model = product.Name;
            DeviceType = product.IsMatrix ? RGBDeviceType.LedMatrix
                       : product.IsMultizone ? RGBDeviceType.LedStripe
                       : RGBDeviceType.LedController;
        }

        public string Mac { get; }
        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }
    }
}
