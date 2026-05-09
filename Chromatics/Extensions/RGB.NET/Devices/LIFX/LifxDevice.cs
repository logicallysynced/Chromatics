using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol;
using RGB.NET.Core;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX
{
    public class LifxDevice : AbstractRGBDevice<LifxDeviceInfo>
    {
        private readonly LifxUpdateQueue _updateQueue;
        private readonly LifxClientDefinition _def;

        public LifxDevice(LifxDeviceInfo info, LifxUpdateQueue updateQueue, LifxClientDefinition def)
            : base(info, updateQueue)
        {
            _updateQueue = updateQueue;
            _def = def;
            InitializeLayout();
        }

        public LifxClientDefinition Definition => _def;

        public void BeginShutdown() => _updateQueue.BeginShutdown();
        public Task CaptureOriginalStateAsync() => _updateQueue.CaptureOriginalStateAsync();
        public Task RestoreOriginalStateAsync() => _updateQueue.RestoreOriginalStateAsync();

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _updateQueue.SetPerDeviceBrightness(correction);

        // Layout maps device shape to RGB.NET LEDs. Three families:
        //   - multizone strips → N LEDs in a row, where N is whatever the
        //     device just told us via StateExtendedColorZones (chained
        //     Beams report combined zones; e.g. 7 Beams = 70 zones)
        //   - matrix tiles → width*height LEDs in a grid, capped at 64
        //   - single-colour bulbs / spots / candles → 1 LED at Custom1
        //
        // ZoneCount > 1 wins over the product catalog: an unknown SKU that
        // reports 70 zones is treated as multizone even though the catalog
        // doesn't flag its product id, because the device itself is the
        // ground truth.
        private void InitializeLayout()
        {
            var product = LifxProductCatalog.GetOrDefault(_def.ProductId);

            // Multizone if either the device reports more than one zone OR
            // the catalog hints it (covers the case where the discovery
            // GetExtendedColorZones reply was dropped on the network).
            int zones = _def.ZoneCount > 1 ? _def.ZoneCount
                      : product.IsMultizone && product.HintZones > 0 ? product.HintZones
                      : 0;

            if (zones > 1)
            {
                if (zones > 82) zones = 82;

                const float cellWidth = 60f;
                for (int i = 0; i < zones; i++)
                {
                    var led = AddLed((LedId)((int)LedId.Custom1 + i),
                        new Point(i * cellWidth, 0),
                        new Size(cellWidth, 14));
                    if (led != null) led.Shape = Shape.Rectangle;
                }
                return;
            }

            if (product.IsMatrix)
            {
                int width = product.HintMatrixWidth > 0 ? product.HintMatrixWidth : 8;
                int height = product.HintMatrixHeight > 0 ? product.HintMatrixHeight : 8;
                if (width * height > 64) { width = 8; height = 8; }

                const float cellSize = 36f;
                for (int row = 0; row < height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        int idx = row * width + col;
                        var led = AddLed((LedId)((int)LedId.Custom1 + idx),
                            new Point(col * cellSize, row * cellSize),
                            new Size(cellSize, cellSize));
                        if (led != null) led.Shape = Shape.Rectangle;
                    }
                }
                return;
            }

            var single = AddLed(LedId.Custom1, new Point(0, 0), new Size(80));
            if (single != null) single.Shape = Shape.Circle;
        }
    }
}
