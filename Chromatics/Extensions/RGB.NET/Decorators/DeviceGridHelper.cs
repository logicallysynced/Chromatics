using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Localization;
using Chromatics.Layers;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    internal static class DeviceGridHelper
    {
        // Returns a row/col grid for the LEDs in the group. Tries three sources
        // in order: keyboard layout grid, device mapping overrides (X/Y from the
        // Mappings tab), and finally a synthetic default grid.
        internal static (Dictionary<LedId, int[]> grid, int maxRow, int maxCol) GetGrid(ListLedGroup ledGroup)
        {
            var kbGrid = KeyLocalization.GetActiveGrid(AppSettings.GetSettings().keyboardLayout);
            if (HasCoverage(ledGroup, kbGrid))
            {
                int maxR = kbGrid.Values.Max(p => p[0]);
                int maxC = kbGrid.Values.Max(p => p[1]);
                return (kbGrid, maxR, maxC);
            }

            // Try device mapping overrides (positions from the Mappings tab)
            var overrideGrid = BuildFromMappingOverrides(ledGroup);
            if (overrideGrid != null)
                return overrideGrid.Value;

            // Fallback: build a default grid from LED ordering
            return BuildDefaultGrid(ledGroup);
        }

        private static bool HasCoverage(ListLedGroup ledGroup, Dictionary<LedId, int[]> grid)
        {
            foreach (var led in ledGroup)
                if (grid.ContainsKey(led.Id))
                    return true;
            return false;
        }

        private static (Dictionary<LedId, int[]> grid, int maxRow, int maxCol)? BuildFromMappingOverrides(ListLedGroup ledGroup)
        {
            var leds = ledGroup.ToArray();
            if (leds.Length == 0) return null;

            var device = leds[0].Device;
            if (device == null) return null;

            // Use the same GUID generation as RGBController
            var deviceId = Chromatics.Helpers.DeviceHelper.GenerateDeviceGuid(device.DeviceInfo.DeviceName);
            var overrides = MappingLayers.GetDeviceLayoutOverrides(deviceId);
            if (overrides.Count == 0) return null;

            var matched = new Dictionary<LedId, (double x, double y)>();
            foreach (var led in leds)
            {
                if (overrides.TryGetValue(led.Id, out var pos))
                    matched[led.Id] = (pos.X, pos.Y);
            }

            if (matched.Count < 2) return null;

            return NormalizeToGrid(matched);
        }

        private static (Dictionary<LedId, int[]> grid, int maxRow, int maxCol) BuildDefaultGrid(ListLedGroup ledGroup)
        {
            var leds = ledGroup.ToArray();
            int count = leds.Length;
            if (count == 0)
                return (new Dictionary<LedId, int[]>(), 0, 0);

            // Arrange as a rectangle: wider than tall, like most devices
            int cols = (int)Math.Ceiling(Math.Sqrt(count * 2));
            int rows = (int)Math.Ceiling((double)count / cols);
            cols = Math.Max(1, cols);
            rows = Math.Max(1, rows);

            var grid = new Dictionary<LedId, int[]>();
            for (int i = 0; i < count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                grid[leds[i].Id] = [r, c];
            }

            return (grid, Math.Max(0, rows - 1), Math.Max(0, cols - 1));
        }

        private static (Dictionary<LedId, int[]> grid, int maxRow, int maxCol) NormalizeToGrid(Dictionary<LedId, (double x, double y)> positions)
        {
            double minX = positions.Values.Min(p => p.x);
            double maxX = positions.Values.Max(p => p.x);
            double minY = positions.Values.Min(p => p.y);
            double maxY = positions.Values.Max(p => p.y);

            double rangeX = maxX - minX;
            double rangeY = maxY - minY;
            if (rangeX < 0.01) rangeX = 1;
            if (rangeY < 0.01) rangeY = 1;

            // Scale to a grid roughly matching keyboard proportions (6 rows x 22 cols)
            int gridRows = 5;
            int gridCols = Math.Max(5, (int)(gridRows * rangeX / rangeY));

            var grid = new Dictionary<LedId, int[]>();
            foreach (var (id, (x, y)) in positions)
            {
                int r = (int)Math.Round((y - minY) / rangeY * gridRows);
                int c = (int)Math.Round((x - minX) / rangeX * gridCols);
                grid[id] = [Math.Clamp(r, 0, gridRows), Math.Clamp(c, 0, gridCols)];
            }

            return (grid, gridRows, gridCols);
        }
    }

}
