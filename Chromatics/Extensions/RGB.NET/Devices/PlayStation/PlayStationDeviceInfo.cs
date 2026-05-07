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

            // DeviceName feeds DeviceHelper.GenerateDeviceGuid (used by Chromatics
            // to key per-device persistence: layer mappings, Mappings-tab layout
            // overrides, brightness, etc.). Including the controller's serial
            // number gives every physical controller a stable identity that
            // survives app restarts and disambiguates two same-model controllers
            // without depending on enumeration order. Sony's serial descriptor is
            // populated for both transports — on USB it's the controller's USB
            // serial string, on Bluetooth it's the controller's MAC address.
            //
            // Transport tag is also included so the device list is unambiguous
            // when the same controller is connected by multiple methods (rare,
            // but happens when users dock a wired controller while paired over
            // BT). Trade-off: switching the same controller from USB to BT
            // produces a new GUID and the user's saved mappings for the USB
            // pairing don't auto-apply to the BT pairing. Acceptable v1 — the
            // alternative (transport-agnostic name) hides legitimate
            // dual-presence cases.
            string transportTag = transport == PlayStationTransport.Bluetooth ? "BT" : "USB";
            string serialTag = !string.IsNullOrEmpty(SerialNumber) ? $" [{SerialNumber}]" : "";
            DeviceName = $"{Model} ({transportTag}){serialTag}";
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
