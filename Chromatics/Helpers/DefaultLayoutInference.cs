using Chromatics.Localization;
using RGB.NET.Core;
using System.Linq;

namespace Chromatics.Helpers
{
    // Synthesise LEDs for devices that arrive from their RGB.NET provider with
    // an empty LED set. The Chromatics surface needs at least one Led per
    // device for any layer to paint anything, so without this fixup a 0-LED
    // device shows up in the Mappings tab but can't be addressed.
    //
    // Replaces the legacy `Layouts/Default/Keyboard/*.xml` +
    // `Layouts/Default/Headset/*.xml` fallback that loaded an Artemis-shaped
    // default layout via RGB.NET.Layout. Those XMLs ship with the archived
    // RGB.NET-Resources repo and aren't being maintained — and the headset
    // path was already broken (the code pointed at `.../Keyboard/Artemis 4
    // LEDs headset.xml`, the file lives under `.../Headset/`). Deriving the
    // layout algorithmically from KeyLocalization's QWERTY grid removes the
    // dependency on the archived data while also fixing the headset path.
    //
    // Only the topological layout matters here — every decorator that reads
    // Led.Location does so relative to other LEDs on the same device.
    // CellSize=19 mirrors RGB.NET's own LogitechPerKeyRGBDevice spacing so
    // synthesised devices behave the same way as real Logitech per-key
    // boards after LogitechLayoutFixup applies.
    public static class DefaultLayoutInference
    {
        private const float CellSize = 19f;

        /// <summary>
        /// If the device has zero LEDs and is a supported type, populate it
        /// with a synthetic LED grid so decorators have something to paint.
        /// No-op for devices that already have LEDs or for unsupported types.
        /// </summary>
        public static void Apply(IRGBDevice device)
        {
            if (device == null) return;
            if (device.Count() > 0) return;

            switch (device.DeviceInfo.DeviceType)
            {
                case RGBDeviceType.Keyboard:
                    ApplyKeyboard(device);
                    break;
                case RGBDeviceType.Headset:
                    ApplyHeadset(device);
                    break;
            }
        }

        private static void ApplyKeyboard(IRGBDevice device)
        {
            // Same grid LogitechLayoutFixup uses — full ANSI 104 set of
            // Keyboard_* LedIds with [row, col] coordinates.
            var grid = KeyLocalization.QWERTY_Grid;
            int maxRow = 0;
            int maxCol = 0;
            foreach (var kvp in grid)
            {
                int row = kvp.Value[0];
                int col = kvp.Value[1];
                device.AddLed(kvp.Key, new Point(col * CellSize, row * CellSize), new Size(CellSize, CellSize));
                if (row > maxRow) maxRow = row;
                if (col > maxCol) maxCol = col;
            }
            device.Size = new Size((maxCol + 1) * CellSize, (maxRow + 1) * CellSize);
        }

        private static void ApplyHeadset(IRGBDevice device)
        {
            // 2x2 grid: left ear top/bottom, right ear top/bottom. Mirrors the
            // count and topology of the legacy Artemis 4-LED headset XML. The
            // exact spacing doesn't matter — DeviceGridHelper's third-tier
            // fallback normalises whatever positions we set into a row/col
            // grid for decorators.
            const float earGap = CellSize * 6f;
            device.AddLed(LedId.Headset1, new Point(0,      0),         new Size(CellSize, CellSize));
            device.AddLed(LedId.Headset2, new Point(0,      CellSize),  new Size(CellSize, CellSize));
            device.AddLed(LedId.Headset3, new Point(earGap, 0),         new Size(CellSize, CellSize));
            device.AddLed(LedId.Headset4, new Point(earGap, CellSize),  new Size(CellSize, CellSize));
            device.Size = new Size(earGap + CellSize, CellSize * 2f);
        }
    }
}
