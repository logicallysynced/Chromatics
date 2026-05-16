using Chromatics.Localization;
using RGB.NET.Core;
using System;

namespace Chromatics.Helpers
{
    // Rebind Led.Location for Logitech devices so position-aware decorators
    // (conical gradients, radial pulses, anything reading Led.Location.Y) work
    // correctly.
    //
    // Background:
    //   RGB.NET's LogitechPerKeyRGBDevice.InitializeLayout ships every LED as
    //   `new Point(pos++ * 19, 0)` — a flat single-row layout at Y=0 — with
    //   the comment "// TODO: ... they would need to be separated". The
    //   Logitech Lighting SDK has no API for "where is the F1 key?", so the
    //   provider has nothing to query and just lays LEDs out left-to-right.
    //   Conical gradients then collapse via atan2(0, dx) → left half / right
    //   half fade.
    //
    //   Other providers (Razer, Corsair, etc.) read per-key positions
    //   directly from their vendor SDK and don't need this fixup.
    //
    //   Historically RGB.NET shipped per-board layout XMLs for Logitech via
    //   the RGB.NET-Resources repo, but that repo is archived and isn't
    //   being maintained for new boards. Rather than depend on those XMLs
    //   we derive the layout algorithmically from KeyLocalization's QWERTY
    //   grid — which we already maintain for keyboard-localization purposes
    //   and which already encodes (row, col) per LedId for the entire ANSI
    //   104. The only thing we need to invent is a cell size; multiplying
    //   the grid coordinates by that yields a topologically correct layout
    //   (F-row above number-row above QWERTY-row above ...), which is all
    //   the position-aware decorators care about.
    //
    //   Trade-off versus the XML approach: we lose pixel-perfect
    //   per-keyboard accuracy (G513 vs G915 keycap offsets are slightly
    //   different in reality), and any non-keyboard LedIds (G-keys, media
    //   keys, mouse / headset LEDs) keep RGB.NET's default Y=0 because
    //   they're absent from the QWERTY grid. Acceptable trade — the
    //   alternative was either freezing on archived layout files or
    //   re-curating them ourselves.
    public static class LogitechLayoutFixup
    {
        // Standard keycap pitch in arbitrary units. Anything > 0 works for
        // the decorators (only ratios matter, not absolute scale); 19 mirrors
        // the value RGB.NET's own LogitechPerKeyRGBDevice uses so existing
        // decorator tunings (which assume a roughly 1u-per-key spacing in
        // RGB.NET-default coordinates) keep behaving the same way.
        private const float CellSize = 19f;

        /// <summary>
        /// Derive Led.Location for a Logitech device from the standard QWERTY
        /// grid so position-aware decorators (conical gradients, radial
        /// pulses) render correctly. No-op for non-Logitech devices.
        /// </summary>
        public static void Apply(IRGBDevice device, string appDirectory = null, bool? ansi = null)
        {
            // appDirectory and ansi are kept on the signature for source
            // compatibility with the previous XML-loading version — the
            // grid-derived approach doesn't need either.
            _ = appDirectory; _ = ansi;

            if (device == null) return;
            if (!string.Equals(device.DeviceInfo?.Manufacturer, "Logitech", StringComparison.OrdinalIgnoreCase))
                return;

            // Mice, headsets, and speakers don't have a keyboard-grid analogue
            // and aren't a target for radial / conical decorators in practice.
            // Leaving them on RGB.NET's defaults keeps this method conservative.
            if (device.DeviceInfo.DeviceType != RGBDeviceType.Keyboard) return;

            var grid = KeyLocalization.QWERTY_Grid;
            foreach (Led led in device)
            {
                if (!grid.TryGetValue(led.Id, out int[] pos)) continue;
                // grid stores [row, col]; physical x increases with col and
                // physical y increases with row. CellSize is the spacing
                // between adjacent keys.
                led.Location = new Point(pos[1] * CellSize, pos[0] * CellSize);
                led.Size = new Size(CellSize, CellSize);
            }
        }
    }
}
