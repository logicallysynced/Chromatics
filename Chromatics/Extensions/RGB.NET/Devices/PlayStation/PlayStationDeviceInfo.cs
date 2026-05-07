using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.PlayStation
{
    public class PlayStationDeviceInfo : IRGBDeviceInfo
    {
        public PlayStationDeviceInfo(PlayStationControllerType controllerType, PlayStationTransport transport, string serialNumber)
        {
            ControllerType = controllerType;
            Transport = transport;
            SerialNumber = serialNumber ?? "";

            DeviceType = RGBDeviceType.GameController;
            Manufacturer = "Sony";
            Model = controllerType switch
            {
                PlayStationControllerType.DualShock4 => "DualShock 4",
                PlayStationControllerType.DualSense => "DualSense",
                PlayStationControllerType.DualSenseEdge => "DualSense Edge",
                _ => "PlayStation Controller",
            };

            string transportTag = transport == PlayStationTransport.Bluetooth ? "BT" : "USB";
            DeviceName = $"{Model} ({transportTag})";
        }

        public PlayStationControllerType ControllerType { get; }
        public PlayStationTransport Transport { get; }
        public string SerialNumber { get; }

        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }
    }
}
