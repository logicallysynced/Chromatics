using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    public class NanoleafDeviceInfo : IRGBDeviceInfo
    {
        public NanoleafDeviceInfo(NanoleafClientDefinition def)
        {
            Id = def.Id;
            DeviceName = string.IsNullOrEmpty(def.Label) ? def.Id : def.Label;
            Manufacturer = "Nanoleaf";
            Model = string.IsNullOrEmpty(def.Model) ? "Nanoleaf" : def.Model;

            // A wall of panels is a 2D matrix so grid-aware effects engage;
            // a single panel or a 4D-style strip degrades to LedStripe. The
            // provider passes the resolved panel count, so one panel reads
            // as a controller.
            DeviceType = def.PanelCount > 2 ? RGBDeviceType.LedMatrix
                       : def.PanelCount == 2 ? RGBDeviceType.LedStripe
                       : RGBDeviceType.LedController;
        }

        public string Id { get; }
        public RGBDeviceType DeviceType { get; }
        public string DeviceName { get; }
        public string Manufacturer { get; }
        public string Model { get; }
        public object LayoutMetadata { get; set; }
    }
}
