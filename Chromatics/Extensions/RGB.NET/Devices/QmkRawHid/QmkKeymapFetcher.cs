using Chromatics.Core;
using Chromatics.Enums;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // Lookup of QMK per-board keymap data — matrix-position-to-keycap-label
    // mapping for ~500 QMK boards (the subset whose keyboard.json includes
    // "label" fields). Sourced from snakkarike/qmk_firmware and pre-processed
    // into Chromatics/Resources/qmk_keymap_data.json by
    // build_qmk_keymap_index.py at the workspace root.
    //
    // The file ships as an embedded resource inside the Chromatics assembly,
    // so this lookup is purely in-memory: no network fetch, no disk cache,
    // no failure mode for offline users. Bundled size is ~450 KB compact
    // JSON for 2650 boards (boards without labels get an empty entry so the
    // runtime still knows the board is recognised; the layout merge then
    // falls back to LedId.Custom_* for every LED on that board).
    //
    // The bundled data refreshes when build_qmk_keymap_index.py is re-run
    // and the resulting JSON is committed. Re-run periodically to pick up
    // newly-upstreamed boards.
    internal static class QmkKeymapFetcher
    {
        private const string ResourceName = "Chromatics.Resources.qmk_keymap_data.json";

        // Loaded once, never mutated. Key = "VID:PID" hex uppercase
        // (e.g. "8968:4E4C"); value = the parsed Labels dict.
        private static Dictionary<string, QmkKeymap> _byVidPid;
        private static readonly object _initLock = new();

        public sealed class QmkKeymap
        {
            // Keycap label at each keyswitch matrix coordinate.  Boards
            // present in the bundle but with no labels in their source JSON
            // get an empty dictionary — discovery still recognises them but
            // the layout merge step falls back to LedId.Custom_* for every
            // LED.
            public IReadOnlyDictionary<(byte col, byte row), string> Labels { get; set; }
        }

        // Returns the cached entry for (vendorId, productId), or null if the
        // board isn't in the bundle. Synchronous — the data is already in
        // memory once the embedded resource has been parsed. Kept async-shaped
        // so callers don't have to be rewritten when the implementation
        // changed (and so a future fallback to remote fetch can re-add the
        // await without another API churn).
        public static Task<QmkKeymap> TryGetKeymapAsync(int vendorId, int productId)
        {
            try
            {
                if (!TryEnsureLoaded()) return Task.FromResult<QmkKeymap>(null);
                string key = $"{vendorId:X4}:{productId:X4}";
                return Task.FromResult(_byVidPid.TryGetValue(key, out var km) ? km : null);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] keymap lookup failed for {vendorId:X4}:{productId:X4} ({ex.Message}); falling back to Custom1..N.",
                    forwardToSentry: false);
                return Task.FromResult<QmkKeymap>(null);
            }
        }

        // ── Bundle load ──────────────────────────────────────────────

        private static bool TryEnsureLoaded()
        {
            if (_byVidPid != null) return true;
            lock (_initLock)
            {
                if (_byVidPid != null) return true;
                _byVidPid = LoadBundle();
                return _byVidPid != null;
            }
        }

        private static Dictionary<string, QmkKeymap> LoadBundle()
        {
            try
            {
                var asm = typeof(QmkKeymapFetcher).Assembly;
                using var stream = asm.GetManifestResourceStream(ResourceName);
                if (stream == null)
                {
                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[QMK] embedded keymap bundle '{ResourceName}' missing; per-key semantic mapping will use Custom1..N for every board.",
                        forwardToSentry: false);
                    return new Dictionary<string, QmkKeymap>();
                }
                using var doc = JsonDocument.Parse(stream);
                return ParseBundle(doc.RootElement);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Error,
                    $"[QMK] failed to load embedded keymap bundle: {ex.Message}",
                    forwardToSentry: false);
                return new Dictionary<string, QmkKeymap>();
            }
        }

        // Bundle schema (see build_qmk_keymap_index.py):
        //   {
        //     "_source": ..., "_generated_utc": ..., "_count": N,
        //     "boards": {
        //       "VID:PID": [[col, row, "label"], ...],
        //       ...
        //     }
        //   }
        private static Dictionary<string, QmkKeymap> ParseBundle(JsonElement root)
        {
            var result = new Dictionary<string, QmkKeymap>(StringComparer.OrdinalIgnoreCase);
            if (!root.TryGetProperty("boards", out var boards) || boards.ValueKind != JsonValueKind.Object)
                return result;

            foreach (var board in boards.EnumerateObject())
            {
                var labels = new Dictionary<(byte col, byte row), string>();
                if (board.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var rec in board.Value.EnumerateArray())
                    {
                        if (rec.ValueKind != JsonValueKind.Array || rec.GetArrayLength() < 3) continue;
                        int col = rec[0].GetInt32();
                        int row = rec[1].GetInt32();
                        if (rec[2].ValueKind != JsonValueKind.String) continue;
                        string label = rec[2].GetString();
                        if (col < 0 || col > byte.MaxValue || row < 0 || row > byte.MaxValue) continue;
                        if (string.IsNullOrWhiteSpace(label)) continue;
                        // First-write-wins on collision within a board (shouldn't
                        // happen — the build script already de-dupes).
                        var key = ((byte)col, (byte)row);
                        if (!labels.ContainsKey(key))
                            labels[key] = label;
                    }
                }
                result[board.Name] = new QmkKeymap { Labels = labels };
            }
            return result;
        }

        // ── Layout merge: firmware LED records + bundled labels → LedId list ──

        // Combines the firmware's GetLedInfo records (per-LED matrix col/row)
        // with the bundle's labels (per matrix coord → "Esc" / "F1" / etc.)
        // to assign a semantic LedId.Keyboard_* to each LED. LEDs whose
        // (col, row) doesn't appear in the bundle (underglow, board variants,
        // or boards whose source JSON omits labels entirely) fall back to
        // LedId.Custom1+firmwareIndex.
        //
        // Physical placement is derived from matrix coords × a fixed cell
        // size — good enough for the Mapping tab preview; users adjust via
        // drag-position UX from there.
        public static IReadOnlyList<QmkLedLayoutEntry> BuildLayout(
            QmkKeymap keymap,
            IReadOnlyList<(int firmwareIndex, byte col, byte row)> ledRecords)
        {
            const float cellW = 60f;
            const float cellH = 60f;
            var entries = new List<QmkLedLayoutEntry>(ledRecords.Count);

            for (int i = 0; i < ledRecords.Count; i++)
            {
                var rec = ledRecords[i];
                LedId ledId = LedId.Invalid;

                if (keymap?.Labels != null
                    && keymap.Labels.TryGetValue((rec.col, rec.row), out string label))
                {
                    ledId = QmkKeycodeMap.ToLedId(label);
                }

                if (ledId == LedId.Invalid)
                    ledId = (LedId)((int)LedId.Custom1 + i);

                var location = new Point(rec.col * cellW, rec.row * cellH);
                var size = new Size(cellW, cellH);
                entries.Add(new QmkLedLayoutEntry(rec.firmwareIndex, rec.col, rec.row, ledId, location, size));
            }

            // De-dupe LedIds: two LEDs claiming the same semantic id (left vs
            // right Shift both labelled "Shift") keep the first and demote
            // the rest to Custom_* so RGB.NET's uniqueness invariant holds.
            var seen = new HashSet<LedId>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].PreferredLedId == LedId.Invalid) continue;
                if ((int)entries[i].PreferredLedId >= (int)LedId.Custom1) { seen.Add(entries[i].PreferredLedId); continue; }
                if (seen.Add(entries[i].PreferredLedId)) continue;
                entries[i] = new QmkLedLayoutEntry(
                    entries[i].FirmwareIndex, entries[i].MatrixCol, entries[i].MatrixRow,
                    (LedId)((int)LedId.Custom1 + i),
                    entries[i].Location, entries[i].Size);
            }

            return entries;
        }
    }
}
