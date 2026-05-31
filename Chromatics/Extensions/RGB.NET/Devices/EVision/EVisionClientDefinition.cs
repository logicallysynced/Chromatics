using Chromatics.Extensions.RGB.NET.Devices.EVision.Protocol;

namespace Chromatics.Extensions.RGB.NET.Devices.EVision
{
    // Runtime descriptor for one adopted EVision-firmware keyboard.
    // Captures the bits the UpdateQueue and Device need at construction
    // time so neither has to re-probe the USB descriptor per frame.
    //
    // Identity model is the same as Redragon: every adopted device is
    // a single 126-LED matrix keyboard, no per-model variant beyond
    // the display name (the OpenRGB driver itself does not branch).
    public sealed class EVisionClientDefinition
    {
        public EVisionClientDefinition(
            int vendorId,
            int productId,
            EVisionKeyboardModel model,
            string manufacturer,
            string product,
            string devicePath)
        {
            VendorId = vendorId;
            ProductId = productId;
            Model = model;
            Manufacturer = manufacturer ?? string.Empty;
            Product = string.IsNullOrEmpty(product) ? model.DisplayName(vendorId, productId) : product;
            DevicePath = devicePath ?? string.Empty;
        }

        public int VendorId { get; }
        public int ProductId { get; }
        public EVisionKeyboardModel Model { get; }
        public string Manufacturer { get; }
        public string Product { get; }

        public string DevicePath { get; }

        // Every EVision V1 keyboard exposes 126 firmware LED slots
        // regardless of physical key count. TKL boards leave the
        // numpad-block slots permanently black; ISO boards re-use the
        // same wire slots as ANSI. Hard-coded here so the Device and
        // UpdateQueue don't have to carry it.
        public const int LightCount = EVisionKeyboardProtocol.TotalLeds;
    }
}
