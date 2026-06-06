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
            int maxRow = 0;
            int maxCol = 0;
            // First pass: position the keys we recognise from the QWERTY
            // grid, and track the bounding box. LEDs not in the grid
            // (G-keys, media keys, mode indicators) get handled below.
            // Without that second pass they keep RGB.NET's default
            // single-row layout at (pos*19, 0) — fine numerically but
            // their X values run up into the thousands, which blows out
            // device.Size and makes ConicalGradientTexture sample the
            // visible keys from a thin strip on one side. Visible
            // symptom: conical gradients render as a vertical band on
            // boards with lots of media / G-keys (G513, G915, etc.).
            var unmappedLeds = new System.Collections.Generic.List<Led>();
            foreach (Led led in device)
            {
                if (!grid.TryGetValue(led.Id, out int[] pos))
                {
                    unmappedLeds.Add(led);
                    continue;
                }
                // grid stores [row, col]; physical x increases with col and
                // physical y increases with row. CellSize is the spacing
                // between adjacent keys.
                led.Location = new Point(pos[1] * CellSize, pos[0] * CellSize);
                led.Size = new Size(CellSize, CellSize);
                if (pos[0] > maxRow) maxRow = pos[0];
                if (pos[1] > maxCol) maxCol = pos[1];
            }

            // Park unmapped LEDs in extra rows below the QWERTY grid so
            // they have valid coordinates but don't drag device.Size out
            // to thousand-pixel widths. Wrap at the same column count as
            // the main grid so the layout stays roughly square. Position
            // is just for spatial decorators (conical/radial gradients)
            // to have a sensible coord — these LEDs don't show up in the
            // virtual keyboard render at all.
            int colsPerRow = maxCol + 1;
            for (int i = 0; i < unmappedLeds.Count; i++)
            {
                int row = maxRow + 1 + (i / colsPerRow);
                int col = i % colsPerRow;
                unmappedLeds[i].Location = new Point(col * CellSize, row * CellSize);
                unmappedLeds[i].Size = new Size(CellSize, CellSize);
            }

            // Recompute device.Size from the post-fixup LED set. RGB.NET's
            // AbstractRGBDevice.OnAttached auto-fills Size when it sees
            // Size.Invalid (NaN x NaN), but some Logitech device-class
            // implementations don't reach that base call and the device
            // ends up exposing Size=NaN x NaN at DevicesChanged.Added time.
            // ConicalGradientTexture and other Size-aware brushes need a
            // real bounding box to normalise LED positions, otherwise the
            // gradient samples against NaN and collapses to a single band.
            //
            // Always overwrite here because the first pass may have rewritten
            // positions that previously satisfied the auto-compute. The
            // bounding box we derive now is the definitive one for the
            // post-fixup layout.
            float maxX = 0f, maxY = 0f;
            foreach (Led led in device)
            {
                float right = led.Location.X + led.Size.Width;
                float bottom = led.Location.Y + led.Size.Height;
                if (!float.IsNaN(right) && right > maxX) maxX = right;
                if (!float.IsNaN(bottom) && bottom > maxY) maxY = bottom;
            }
            if (maxX > 0 && maxY > 0)
                device.Size = new Size(maxX, maxY);
        }
    }
}
