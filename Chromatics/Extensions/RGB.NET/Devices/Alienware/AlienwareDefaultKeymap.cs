using Chromatics.Localization;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware
{
    // Best-effort (row, col) → LedId mapping for per-key Alienware
    // keyboards (V5 notebook, V8 external like AW510K / AW920K).
    //
    // The honest situation: Alienware doesn't ship a public per-board
    // light-index → physical-key table, and T-Troll's open source builds
    // these from user-supplied registry mappings rather than hardcoding
    // them. Without hardware to empirically capture the mapping, we
    // fall back to a standard ANSI 104 enumeration ordered top-to-bottom,
    // left-to-right — which matches how most matrix keyboards
    // enumerate firmware-side.
    //
    // For the user this means:
    //   - Highlight / Keybind layers will likely paint roughly the right
    //     keys on AW510K / AW920K-class boards out of the box.
    //   - Where firmware enumeration order differs from this assumption,
    //     keys will paint the wrong physical positions until the user
    //     remaps them via the Mapping tab.
    //   - Lights past the first 104 (status bars, side LEDs, logos)
    //     surface as LedId.Custom105..N for manual placement.
    //
    // When community testing confirms a specific board's enumeration
    // pattern, we can replace this default with a per-PID table.
    internal static class AlienwareDefaultKeymap
    {
        public sealed class KeymapEntry
        {
            public LedId LedId { get; init; }
            public Point Location { get; init; }
            public Size Size { get; init; }
        }

        // Default ANSI 104 QWERTY layout pulled from Chromatics' shared
        // KeyLocalization grid (the same data that powers the Logitech
        // and other per-key keyboard providers). Iterated in row-major
        // order so light index 0 = top-left key (Escape).
        public static IReadOnlyList<KeymapEntry> BuildAnsi104(int totalLights, float cellSize = 30f)
        {
            var grid = KeyLocalization.QWERTY_Grid;
            // Sort by row then column so the enumeration matches the
            // matrix-scan order the firmware most commonly uses.
            var ordered = grid
                .OrderBy(kvp => kvp.Value[0])
                .ThenBy(kvp => kvp.Value[1])
                .ToList();

            var result = new List<KeymapEntry>(Math.Max(totalLights, ordered.Count));
            int produced = 0;

            for (int i = 0; i < Math.Min(totalLights, ordered.Count); i++)
            {
                var (ledId, rc) = (ordered[i].Key, ordered[i].Value);
                int row = rc[0];
                int col = rc[1];
                result.Add(new KeymapEntry
                {
                    LedId = ledId,
                    Location = new Point(col * cellSize, row * cellSize),
                    Size = new Size(cellSize, cellSize),
                });
                produced++;
            }

            // Lights past the standard ANSI grid (chassis logos, status
            // bars, side strips) get Custom* slots arranged in a small
            // tail row below the keyboard so they're visually distinct
            // and easy to find in the Mapping tab.
            int tailRow = (grid.Values.Max(p => p[0]) + 2);
            int tailCol = 0;
            int customIndex = 0;
            for (int i = produced; i < totalLights; i++)
            {
                var entry = new KeymapEntry
                {
                    LedId = (LedId)((int)LedId.Custom1 + customIndex),
                    Location = new Point(tailCol * cellSize, tailRow * cellSize),
                    Size = new Size(cellSize, cellSize),
                };
                result.Add(entry);
                customIndex++;
                tailCol++;
                if (tailCol >= 24) { tailCol = 0; tailRow++; }
            }

            return result;
        }
    }
}
