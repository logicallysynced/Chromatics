using Chromatics.Core;
using Chromatics.Enums;
using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // Option C from the design discussion: fetch VIA keymap JSON from the
    // upstream via-keyboards repo (or a CDN-mirrored equivalent) for any
    // adopted board, then merge against the firmware's per-LED matrix
    // coordinates to produce semantic LedId.Keyboard_* mappings. Falls
    // back to LedId.Custom1..N when no keymap is fetchable (offline first
    // run, unknown board, schema mismatch).
    //
    // Cache lives at %APPDATA%/Chromatics/QmkKeymaps/{vid:X4}_{pid:X4}.json
    // and is consulted before any network request. Cache hits are
    // unconditional — keymaps are board-defining and don't expire; if a
    // user re-flashes with a different layout they can clear the cache
    // dir manually. The index itself is refetched every 14 days so new
    // boards added upstream show up without a Chromatics release.
    internal static class QmkKeymapFetcher
    {
        // The via-keyboards index URL. Each keyboard entry maps a kebab-name
        // path to vendor/product ids; the keymap JSONs themselves live one
        // path-level deeper. URL is centralised so a future change of upstream
        // host (or use of OpenSignalRGB's database in parallel) only touches
        // this one constant.
        private const string IndexUrl    = "https://www.caniusevia.com/keyboards.json";
        private const string KeymapBase  = "https://www.caniusevia.com/keyboards/";

        private const int RequestTimeoutSeconds = 8;
        private static readonly TimeSpan IndexCacheTtl = TimeSpan.FromDays(14);

        private static readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = true })
        {
            Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds),
        };

        // Loaded index: keyed by (vendorId, productId). Populated lazily on
        // first call. ConcurrentDictionary so a multi-board adoption flow
        // doesn't serialise behind one another's lookups.
        private static ConcurrentDictionary<(int vid, int pid), string> _indexByVidPid;
        private static readonly object _indexInitLock = new();

        public sealed class QmkKeymap
        {
            public string Name { get; set; }
            public int Cols { get; set; }
            public int Rows { get; set; }
            // Keycode at each matrix coordinate. Key = (col, row). Value is
            // the QMK keycode string (e.g. "KC_A", "KC_F1", "KC_NO").
            // Underglow / non-matrix LEDs are absent from this dictionary;
            // they get LedId.Custom1+i fallback ids in the merge step.
            public IReadOnlyDictionary<(byte col, byte row), string> Keycodes { get; set; }
        }

        // Returns the cached/fetched keymap, or null on failure. Never
        // throws — the caller treats null as "fall back to Custom1..N".
        public static async Task<QmkKeymap> TryGetKeymapAsync(int vendorId, int productId)
        {
            try
            {
                string diskPath = ResolveCachePath(vendorId, productId);
                if (TryLoadCached(diskPath, out QmkKeymap cached)) return cached;

                if (!TryEnsureIndex()) return null;
                if (!_indexByVidPid.TryGetValue((vendorId, productId), out string keymapPath))
                    return null;

                string url = KeymapBase + keymapPath;
                string body = await _http.GetStringAsync(url).ConfigureAwait(false);
                QmkKeymap parsed = ParseKeymapJson(body);
                if (parsed == null) return null;

                TrySaveCache(diskPath, body);
                return parsed;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] VIA keymap fetch failed for {vendorId:X4}:{productId:X4} ({ex.Message}); falling back to Custom1..N.",
                    forwardToSentry: false);
                return null;
            }
        }

        // ── Index ────────────────────────────────────────────────────

        private static bool TryEnsureIndex()
        {
            if (_indexByVidPid != null) return true;
            lock (_indexInitLock)
            {
                if (_indexByVidPid != null) return true;
                _indexByVidPid = LoadIndex();
                return _indexByVidPid != null;
            }
        }

        private static ConcurrentDictionary<(int vid, int pid), string> LoadIndex()
        {
            string indexCache = Path.Combine(GetCacheDir(), "_index.json");
            string body = null;

            if (File.Exists(indexCache))
            {
                var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(indexCache);
                if (age < IndexCacheTtl)
                {
                    try { body = File.ReadAllText(indexCache); }
                    catch { /* fall through to refetch */ }
                }
            }

            if (string.IsNullOrEmpty(body))
            {
                try { body = _http.GetStringAsync(IndexUrl).GetAwaiter().GetResult(); }
                catch (Exception ex)
                {
                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[QMK] keymap index fetch failed ({ex.Message}); per-key semantic layout will use Custom1..N for now.",
                        forwardToSentry: false);
                    return null;
                }
                try { File.WriteAllText(indexCache, body); } catch { /* best-effort */ }
            }

            return ParseIndex(body);
        }

        // Index format (caniusevia.com): { "<kebab-name>": { "vendorId": "0xABCD",
        // "productId": "0x1234", "name": "...", "definition_version": "...", ... } }
        // We only need vendorId / productId / kebab-name. vendorId/productId are
        // strings with "0x" prefix.
        private static ConcurrentDictionary<(int vid, int pid), string> ParseIndex(string json)
        {
            var dict = new ConcurrentDictionary<(int vid, int pid), string>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var kvp in doc.RootElement.EnumerateObject())
                {
                    string path = kvp.Name;
                    if (!kvp.Value.TryGetProperty("vendorId", out var vidProp)) continue;
                    if (!kvp.Value.TryGetProperty("productId", out var pidProp)) continue;
                    if (!TryParseHexId(vidProp.GetString(), out int vid)) continue;
                    if (!TryParseHexId(pidProp.GetString(), out int pid)) continue;
                    dict[(vid, pid)] = path + ".json";
                }
            }
            catch { /* malformed index — return whatever parsed */ }
            return dict;
        }

        private static bool TryParseHexId(string s, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(s)) return false;
            string t = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s.Substring(2) : s;
            return int.TryParse(t, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        // ── Keymap parse ─────────────────────────────────────────────

        // VIA keymap JSON layout fields used:
        //   "matrix": { "rows": N, "cols": N }
        //   "layouts": { "keymap": [ row, row, ... ] } where each row entry is
        //     either a "control object" { "x":..., "y":..., "w":... } or a
        //     string label like "0,5\n\n\nA" — "row,col" prefix tells us
        //     where in the firmware matrix this keycap sits.
        //
        // We walk the keymap array, tracking explicit "matrix" prefixes; the
        // resulting dictionary lets the layout merger answer
        // "what keycap is at (col, row)?" cheaply.
        private static QmkKeymap ParseKeymapJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                int cols = 0, rows = 0;
                if (root.TryGetProperty("matrix", out var matrix))
                {
                    if (matrix.TryGetProperty("cols", out var c)) cols = c.GetInt32();
                    if (matrix.TryGetProperty("rows", out var r)) rows = r.GetInt32();
                }

                var keycodes = new Dictionary<(byte col, byte row), string>();
                if (root.TryGetProperty("layouts", out var layouts)
                    && layouts.TryGetProperty("keymap", out var keymap))
                {
                    foreach (var rowElem in keymap.EnumerateArray())
                    {
                        if (rowElem.ValueKind != JsonValueKind.Array) continue;
                        foreach (var cellElem in rowElem.EnumerateArray())
                        {
                            if (cellElem.ValueKind != JsonValueKind.String) continue;
                            string label = cellElem.GetString();
                            if (string.IsNullOrEmpty(label)) continue;

                            // Label shape: "row,col" then newline-separated labels
                            // (legend on each keycap face). We only need the
                            // matrix coordinate prefix.
                            int comma = label.IndexOf(',');
                            int nl = label.IndexOf('\n');
                            if (comma <= 0 || nl <= comma) continue;

                            if (!byte.TryParse(label.Substring(0, comma), out byte row)) continue;
                            if (!byte.TryParse(label.Substring(comma + 1, nl - comma - 1), out byte col)) continue;

                            string keycodeLabel = label.Substring(nl + 1).Trim();
                            keycodes[(col, row)] = keycodeLabel;
                        }
                    }
                }

                return new QmkKeymap
                {
                    Cols = cols,
                    Rows = rows,
                    Keycodes = keycodes,
                };
            }
            catch { return null; }
        }

        // ── Cache ────────────────────────────────────────────────────

        private static string GetCacheDir()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "Chromatics", "QmkKeymaps");
            try { Directory.CreateDirectory(dir); } catch { /* best-effort */ }
            return dir;
        }

        private static string ResolveCachePath(int vid, int pid)
            => Path.Combine(GetCacheDir(), $"{vid:X4}_{pid:X4}.json");

        private static bool TryLoadCached(string path, out QmkKeymap keymap)
        {
            keymap = null;
            try
            {
                if (!File.Exists(path)) return false;
                string body = File.ReadAllText(path);
                keymap = ParseKeymapJson(body);
                return keymap != null;
            }
            catch { return false; }
        }

        private static void TrySaveCache(string path, string body)
        {
            try { File.WriteAllText(path, body); }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] keymap cache write failed for {Path.GetFileName(path)}: {ex.Message}.",
                    forwardToSentry: false);
            }
        }

        // ── Layout merge: keymap + LED matrix → ordered LedLayoutEntry list ──

        // Combines the VIA keymap (col,row → keycode label) with the firmware's
        // GetLedInfo per-LED records (firmwareIndex → col,row) to produce a
        // list ordered by firmwareIndex where each LED has a semantic
        // LedId.Keyboard_* whenever the keycode label is mappable, or
        // LedId.Custom1+i when it isn't.
        //
        // Physical layout (Point/Size) is approximated from the matrix
        // coordinates so the Avalonia preview shows a recognisable grid
        // shape out of the box; users can fine-tune via the Mapping tab.
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

                if (keymap?.Keycodes != null
                    && keymap.Keycodes.TryGetValue((rec.col, rec.row), out string keycode))
                {
                    ledId = QmkKeycodeMap.ToLedId(keycode);
                }

                if (ledId == LedId.Invalid)
                    ledId = (LedId)((int)LedId.Custom1 + i);

                var location = new Point(rec.col * cellW, rec.row * cellH);
                var size = new Size(cellW, cellH);
                entries.Add(new QmkLedLayoutEntry(rec.firmwareIndex, rec.col, rec.row, ledId, location, size));
            }

            // De-dupe LedIds (a keymap might claim two LEDs share a semantic
            // id — e.g. left+right shift sometimes both map to KC_LSFT).
            // RGB.NET requires unique ids per device, so the second
            // collision falls back to Custom1+i.
            var seen = new HashSet<LedId>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].PreferredLedId == LedId.Invalid) continue;
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
