using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    public class QmkRawHidDeviceInfo : IRGBDeviceInfo
    {
        public QmkRawHidDeviceInfo(QmkRawHidClientDefinition def)
        {
            DeviceName = string.IsNullOrEmpty(def.Product)
                ? (string.IsNullOrEmpty(def.FirmwareDeviceName) ? "QMK Keyboard" : def.FirmwareDeviceName)
                : def.Product;
            Manufacturer = string.IsNullOrEmpty(def.Manufacturer) ? "QMK" : def.Manufacturer;
            Model = string.IsNullOrEmpty(def.FirmwareDeviceName) ? def.Product : def.FirmwareDeviceName;
            DeviceType = RGBDeviceType.Keyboard;
        }

        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }
    }
}
