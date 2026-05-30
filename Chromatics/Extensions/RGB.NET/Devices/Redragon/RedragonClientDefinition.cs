using Chromatics.Extensions.RGB.NET.Devices.Redragon.Protocol;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon
{
    // Runtime descriptor for one adopted Redragon mouse. Captures the bits
    // the UpdateQueue + Device need at construction time so neither has to
    // re-probe the USB descriptor per frame.
    //
    // Identity model is simpler than Alienware: every Redragon mouse on
    // the OpenRGB protocol family is single-zone (LightCount == 1), so
    // there's nothing to negotiate beyond VID/PID/Model.
    public sealed class RedragonClientDefinition
    {
        public RedragonClientDefinition(
            int vendorId,
            int productId,
            RedragonMouseModel model,
            string manufacturer,
            string product,
            string devicePath)
        {
            VendorId = vendorId;
            ProductId = productId;
            Model = model;
            Manufacturer = manufacturer ?? string.Empty;
            Product = string.IsNullOrEmpty(product) ? model.DisplayName(productId) : product;
            DevicePath = devicePath ?? string.Empty;
        }

        public int VendorId { get; }
        public int ProductId { get; }
        public RedragonMouseModel Model { get; }
        public string Manufacturer { get; }
        public string Product { get; }

        // Stable USB path. Used by the hot-plug reconcile to match
        // candidates against already-open devices when the user has more
        // than one Redragon mouse of the same model plugged in.
        public string DevicePath { get; }

        // Single-LED model — every Redragon mouse on this protocol exposes
        // exactly one addressable colour zone (the logo + DPI underglow
        // share one register). Hard-coded so the Device + UpdateQueue
        // don't have to carry it.
        public const int LightCount = 1;
    }
}
