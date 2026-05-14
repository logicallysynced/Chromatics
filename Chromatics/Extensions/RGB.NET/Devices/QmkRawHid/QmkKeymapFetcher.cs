using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // Option C from the design discussion: resolve a per-board layout for any
    // QMK keyboard the user has adopted. Pulls two artifacts:
    //
    //   1) A flat VID/PID → keyboard.json path index hosted on chromatics-docs.
    //      Built by scripts/build_qmk_keymap_index.py from snakkarike/qmk_firmware
    //      and refreshed independently of a Chromatics release.
    //
    //   2) The board's own keyboard.json (or older info.json) fetched on demand
    //      from snakkarike/qmk_firmware via the GitHub raw CDN. We parse its
    //      layouts.<first>.layout array, building a (matrix_row, matrix_col) →
    //      keycap-label dictionary. The label is fed to QmkKeycodeMap to derive
    //      the semantic LedId.Keyboard_* for each per-key LED.
    //
    // Coverage is mixed by design — QMK's schema is inconsistent across boards.
    // Some keyboard.json files include "label" fields on every layout entry
    // (NK87 etc. — full semantic mapping); some omit them entirely (NK65 — only
    // matrix coords). When a label isn't available the merge step falls back to
    // LedId.Custom_* for that LED, and the user positions it via the Mapping
    // tab. Better partial than nothing.
    //
    // Cache lives under FileOperationsHelper.GetConfigDirectory()/QmkKeymaps
    // (i.e. %AppData%/Chromatics/QmkKeymaps) so it survives Velopack updates
    // and gets wiped by Settings → Reset (see SettingsViewModel.ResetChromatics).
    internal static class QmkKeymapFetcher
    {
        private const string IndexUrl = "https://raw.githubusercontent.com/logicallysynced/chromatics-docs/main/qmk_keymap_index.json";
        private const string KeymapRawBase = "https://raw.githubusercontent.com/snakkarike/qmk_firmware/master/";

        private const int RequestTimeoutSeconds = 8;
        private static readonly TimeSpan IndexCacheTtl = TimeSpan.FromDays(14);

        private static readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = true })
        {
            Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds),
        };

        // Loaded index: VID:PID key → repo-relative path. Populated lazily.
        private static Dictionary<string, string> _indexByVidPid;
        private static readonly object _indexInitLock = new();

        public sealed class QmkKeymap
        {
            // Keycap label at each keyswitch matrix coordinate. Only entries
            // whose source JSON included a "label" field appear here — boards
            // without labels return an empty dictionary and the merge step
            // falls back to LedId.Custom_* for every LED.
            public IReadOnlyDictionary<(byte col, byte row), string> Labels { get; set; }
        }

        public static async Task<QmkKeymap> TryGetKeymapAsync(int vendorId, int productId)
        {
            try
            {
                string diskPath = ResolveCachePath(vendorId, productId);
                if (TryLoadCachedKeymap(diskPath, out var cached)) return cached;

                if (!TryEnsureIndex()) return null;
                string key = $"{vendorId:X4}:{productId:X4}";
                if (!_indexByVidPid.TryGetValue(key, out string repoPath)) return null;

                string url = KeymapRawBase + UrlPathEncode(repoPath);
                string body = await _http.GetStringAsync(url).ConfigureAwait(false);
                var parsed = ParseKeyboardJson(body);
                if (parsed == null) return null;

                TrySaveCache(diskPath, body);
                return parsed;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] keymap fetch failed for {vendorId:X4}:{productId:X4} ({ex.Message}); falling back to Custom1..N.",
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

        private static Dictionary<string, string> LoadIndex()
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

        // Index format: { "_source": ..., "_generated_utc": ..., "_count": N,
        //                 "index": { "ABCD:1234": "keyboards/<vendor>/<board>/keyboard.json", ... } }
        private static Dictionary<string, string> ParseIndex(string json)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("index", out var inner))
                {
                    foreach (var kvp in inner.EnumerateObject())
                    {
                        string path = kvp.Value.GetString();
                        if (!string.IsNullOrEmpty(path))
                            dict[kvp.Name] = path;
                    }
                }
            }
            catch { /* malformed — return whatever parsed */ }
            return dict;
        }

        // ── Keyboard.json parse ──────────────────────────────────────

        // Schema we care about (everything else ignored):
        //   "layouts": {
        //     "LAYOUT_<something>": {
        //       "layout": [
        //         { "matrix": [row, col], "label": "Esc", "x": 0, "y": 0 },
        //         ...
        //       ]
        //     },
        //     ...
        //   }
        //
        // QMK boards frequently expose multiple LAYOUT_* alternates (ANSI vs ISO
        // vs split-spacebar) — we pick the first that yielded any labels. The
        // alternates usually share matrix coords for the keys they overlap on,
        // so the choice is mostly cosmetic for our purposes.
        //
        // Label coverage varies wildly across boards. NK87's entries have
        // "label": "Esc"; NK65's same field is absent. We emit only what's
        // actually there; the merge step in BuildLayout falls back to
        // LedId.Custom_* for LEDs whose (col,row) has no label.
        private static QmkKeymap ParseKeyboardJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var labels = new Dictionary<(byte col, byte row), string>();
                if (root.TryGetProperty("layouts", out var layouts) && layouts.ValueKind == JsonValueKind.Object)
                {
                    foreach (var layoutProp in layouts.EnumerateObject())
                    {
                        if (!layoutProp.Value.TryGetProperty("layout", out var layoutArr)) continue;
                        if (layoutArr.ValueKind != JsonValueKind.Array) continue;
                        if (layoutArr.GetArrayLength() == 0) continue;

                        foreach (var entry in layoutArr.EnumerateArray())
                        {
                            if (entry.ValueKind != JsonValueKind.Object) continue;
                            if (!entry.TryGetProperty("matrix", out var mat)) continue;
                            if (mat.ValueKind != JsonValueKind.Array || mat.GetArrayLength() < 2) continue;
                            if (!entry.TryGetProperty("label", out var labelProp)) continue;
                            if (labelProp.ValueKind != JsonValueKind.String) continue;

                            int row = mat[0].GetInt32();
                            int col = mat[1].GetInt32();
                            if (row < 0 || row > byte.MaxValue || col < 0 || col > byte.MaxValue) continue;

                            string label = labelProp.GetString();
                            if (string.IsNullOrWhiteSpace(label)) continue;

                            // First-write-wins across alternate layouts: a key
                            // that appears in LAYOUT_60_ansi and LAYOUT_60_iso
                            // at the same matrix coord keeps the ANSI label,
                            // which is the conventional preferred default.
                            var key = ((byte)col, (byte)row);
                            if (!labels.ContainsKey(key))
                                labels[key] = label;
                        }

                        // First layout with content satisfies us — alternates
                        // mostly share matrix coords, no reason to keep parsing.
                        if (labels.Count > 0) break;
                    }
                }

                return new QmkKeymap { Labels = labels };
            }
            catch { return null; }
        }

        // ── Cache ────────────────────────────────────────────────────

        // Public so SettingsViewModel.ResetChromatics can wipe the cache when
        // the user resets the app. Recursive delete; missing dir is a no-op.
        public static void ClearCache()
        {
            try
            {
                var dir = GetCacheDir();
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] keymap cache clear failed: {ex.Message}",
                    forwardToSentry: false);
            }

            lock (_indexInitLock) { _indexByVidPid = null; }
        }

        private static string GetCacheDir()
        {
            string dir = Path.Combine(FileOperationsHelper.GetConfigDirectory(), "QmkKeymaps");
            try { Directory.CreateDirectory(dir); } catch { /* best-effort */ }
            return dir;
        }

        private static string ResolveCachePath(int vid, int pid)
            => Path.Combine(GetCacheDir(), $"{vid:X4}_{pid:X4}.json");

        private static bool TryLoadCachedKeymap(string path, out QmkKeymap keymap)
        {
            keymap = null;
            try
            {
                if (!File.Exists(path)) return false;
                string body = File.ReadAllText(path);
                keymap = ParseKeyboardJson(body);
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

        // Path-segment encode (preserve slashes) for paths containing spaces or
        // other characters QMK keymap filenames occasionally use.
        private static string UrlPathEncode(string path)
        {
            return string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
        }

        // ── Layout merge: firmware LED records + keymap labels → LedId list ──

        // Combines the firmware's GetLedInfo records (per-LED matrix col/row)
        // with the keymap's labels (per matrix coord → "Esc" / "F1" / etc.) to
        // assign a semantic LedId.Keyboard_* to each LED. LEDs whose
        // (col, row) doesn't appear in the keymap (underglow, board variants,
        // or boards whose keyboard.json omits labels entirely) fall back to
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
            // right Shift both label "Shift", for instance) keep the first
            // and demote the rest to Custom_* so RGB.NET's uniqueness
            // invariant holds.
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
