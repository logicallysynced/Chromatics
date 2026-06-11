using Chromatics.Core;
using Chromatics.Layers;
using Chromatics.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace Chromatics.Helpers
{
    public static class FileOperationsHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        // Chromatics 4 data filenames. Legacy Chromatics-3 variants are migrated
        // on startup by MigrateLegacyChromatics3Files — see that method for the
        // full migration algorithm and preservation policy.
        internal const string LayersFile   = "layers.chromatics4";
        internal const string PaletteFile  = "palette.chromatics4";
        internal const string EffectsFile  = "effects.chromatics4";
        internal const string SettingsFile = "settings.chromatics4";

        internal const string LayersFileLegacy3   = "layers.chromatics3";
        internal const string PaletteFileLegacy3  = "palette.chromatics3";
        internal const string EffectsFileLegacy3  = "effects.chromatics3";
        internal const string SettingsFileLegacy3 = "settings.chromatics3";

        // Test-only redirect. When set (via SetConfigDirectoryOverride), every
        // call to GetConfigDirectory returns this path instead of %AppData%.
        // Production code must never set this — it exists so unit tests can
        // point SaveMappings/SaveLayerMappings at a temp directory and not
        // clobber the user's real .chromatics4 files when `dotnet test` runs.
        private static string _configDirectoryOverride;

        public static void SetConfigDirectoryOverride(string path)
        {
            _configDirectoryOverride = path;
        }

        // Returns the directory where Chromatics user-data files (.chromatics4) live.
        // Always %AppData%\Chromatics\ regardless of install type (managed Setup.exe
        // or portable ZIP). Velopack's portable updater replaces the install tree on
        // each update, so storing data next to the exe meant portable users lost
        // their layers/palettes/settings every time. Keeping everything in AppData
        // survives updates, reinstalls, and roams with the Windows profile.
        public static string GetConfigDirectory()
        {
            if (!string.IsNullOrEmpty(_configDirectoryOverride))
            {
                Directory.CreateDirectory(_configDirectoryOverride);
                return _configDirectoryOverride;
            }

            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Chromatics");
            Directory.CreateDirectory(appData);
            return appData;
        }

        // One-shot migration of user data files from the exe directory to
        // %AppData%\Chromatics. Covers the pre-4.0.x layout where portable installs
        // (and in-place updates that didn't relocate) kept data next to the exe.
        // Source files are deleted after a successful move; conflicts (where the
        // target already exists) leave the exe-dir copy in place and log a warning.
        // Returns the list of filenames that were relocated this run.
        public static IReadOnlyList<string> MigrateExeDirDataToAppData()
            => MigrateExeDirDataToAppData(AppContext.BaseDirectory, GetConfigDirectory());

        public static IReadOnlyList<string> MigrateExeDirDataToAppData(string sourceDir, string targetDir)
        {
            var moved = new List<string>();

            if (string.IsNullOrWhiteSpace(sourceDir) || string.IsNullOrWhiteSpace(targetDir))
                return moved;
            if (!Directory.Exists(sourceDir))
                return moved;
            if (string.Equals(
                    Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(targetDir).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
                return moved;

            var knownFiles = new[]
            {
                LayersFile,   PaletteFile,   EffectsFile,   SettingsFile,
                LayersFileLegacy3, PaletteFileLegacy3, EffectsFileLegacy3, SettingsFileLegacy3,
                LayersFileLegacy3   + ".migrated",
                PaletteFileLegacy3  + ".migrated",
                EffectsFileLegacy3  + ".migrated",
                SettingsFileLegacy3 + ".migrated",
            };

            var candidates = new List<string>(knownFiles);

            try
            {
                candidates.AddRange(Directory.EnumerateFiles(sourceDir, "backup_layers_*.chromatics4")
                                             .Select(Path.GetFileName));
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"Failed to scan {sourceDir} for backup files: {ex.Message}");
            }

            Directory.CreateDirectory(targetDir);

            foreach (var name in candidates)
            {
                var src = Path.Combine(sourceDir, name);
                var dst = Path.Combine(targetDir, name);

                if (!File.Exists(src)) continue;

                try
                {
                    if (File.Exists(dst))
                    {
                        Logger.WriteVerbose(
                            $"{name} already present in AppData — leaving exe-dir copy untouched.");
                        continue;
                    }

                    File.Move(src, dst);
                    moved.Add(name);
                    Logger.WriteConsole(Enums.LoggerTypes.System,
                        $"Relocated {name} from install directory to AppData.");
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error,
                        $"Failed to relocate {name} to AppData: {ex.Message}");
                }
            }

            return moved;
        }

        // One-shot migration of Chromatics-3 data files to their Chromatics-4
        // equivalents. For each pair:
        //   - If the .chromatics3 file exists and .chromatics4 does not, copy it.
        //   - Rename the source .chromatics3 to .chromatics3.migrated so it is
        //     preserved but never re-examined on future launches (idempotent).
        //   - If .chromatics3.migrated already exists (from a prior run that
        //     somehow re-created the .chromatics3), delete the duplicate so we
        //     don't fight ourselves next launch.
        // Safe to call every startup — no-op when no .chromatics3 files exist.
        // Returns the list of filenames that were freshly migrated this run.
        public static IReadOnlyList<string> MigrateLegacyChromatics3Files()
            => MigrateLegacyChromatics3Files(GetConfigDirectory());

        public static IReadOnlyList<string> MigrateLegacyChromatics3Files(string directory)
        {
            var migrated = new List<string>();

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return migrated;

            var pairs = new[]
            {
                (LayersFileLegacy3,   LayersFile),
                (PaletteFileLegacy3,  PaletteFile),
                (EffectsFileLegacy3,  EffectsFile),
                (SettingsFileLegacy3, SettingsFile),
            };

            foreach (var (legacyName, newName) in pairs)
            {
                var legacyPath   = Path.Combine(directory, legacyName);
                var newPath      = Path.Combine(directory, newName);
                var migratedPath = legacyPath + ".migrated";

                if (!File.Exists(legacyPath)) continue;

                try
                {
                    if (!File.Exists(newPath))
                    {
                        File.Copy(legacyPath, newPath);
                        migrated.Add(legacyName);
                        Logger.WriteConsole(Enums.LoggerTypes.System, $"Migrated {legacyName} → {newName}.");
                    }
                    else
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.System,
                            $"{newName} already present — preserving existing {legacyName} as {legacyName}.migrated without overwriting.");
                    }

                    if (File.Exists(migratedPath))
                    {
                        File.Delete(legacyPath);
                        Logger.WriteConsole(Enums.LoggerTypes.System,
                            $"Removed duplicate {legacyName} (a {legacyName}.migrated from a prior run already exists).");
                    }
                    else
                    {
                        File.Move(legacyPath, migratedPath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"Failed to migrate {legacyName}: {ex.Message}");
                }
            }

            return migrated;
        }

        // Drag-repositioning a keycap fires SaveMappings on a thread-pool task on
        // every pointer-release. Rapid drags (or the preview tick touching the
        // same file path) can overlap and collide on the sibling ".tmp" handle,
        // producing "the process cannot access the file" IOExceptions.
        // Serialising all layer writes through this lock makes the
        // write-then-File.Replace pair atomic from the caller's perspective.
        private static readonly System.Threading.Lock _layerSaveLock = new();

        public static void SaveLayerMappings(ConcurrentDictionary<int, Layer> mappings,
            IDictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts = null,
            IDictionary<Guid, int> deviceBrightness = null,
            IEnumerable<Guid> disabledDevices = null)
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, LayersFile);

            try
            {
                // Deep snapshot before serialising: Newtonsoft streams the payload
                // and enumerates every nested collection. A concurrent mutation
                // (e.g. drag-save racing with SetDeviceKeyPosition, or a future
                // edit-mode key pick touching Layer.deviceLeds) would throw
                // "Collection was modified" mid-write. Cloning the inner dicts
                // here means the serialiser only ever sees frozen data.
                var layersSnapshot = new ConcurrentDictionary<int, Layer>();
                foreach (var kvp in mappings)
                {
                    var src = kvp.Value;
                    var ledsCopy = src.deviceLeds != null
                        ? new Dictionary<int, RGB.NET.Core.LedId>(src.deviceLeds)
                        : new Dictionary<int, RGB.NET.Core.LedId>();
                    layersSnapshot[kvp.Key] = new Layer(
                        src.layerVersion, src.layerID, src.layerIndex, src.rootLayerType,
                        src.deviceGuid, src.deviceType, src.layerTypeindex, src.zindex,
                        src.Enabled, ledsCopy, src.allowBleed, src.layerModes);
                }

                var layoutsSnapshot = new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>();
                if (deviceLayouts != null)
                {
                    foreach (var kvp in deviceLayouts)
                    {
                        layoutsSnapshot[kvp.Key] = kvp.Value != null
                            ? new Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>(kvp.Value)
                            : new Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>();
                    }
                }

                var brightnessSnapshot = new Dictionary<Guid, int>();
                if (deviceBrightness != null)
                {
                    foreach (var kvp in deviceBrightness)
                        brightnessSnapshot[kvp.Key] = kvp.Value;
                }

                var disabledSnapshot = new List<Guid>();
                if (disabledDevices != null)
                    disabledSnapshot.AddRange(disabledDevices);

                var wrapper = new MappingFileV3
                {
                    schemaVersion = 6,
                    layers = layersSnapshot,
                    deviceLayouts = layoutsSnapshot,
                    deviceBrightness = brightnessSnapshot,
                    disabledDevices = disabledSnapshot
                };

                WriteJsonAtomic(path, wrapper);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Saving Layers: {ex.Message}");
            }
        }

        // Serialize to a sibling .tmp file and only replace the real file once
        // the write has fully succeeded. If serialization throws mid-write (as
        // it did historically when the DictionaryConverter recursed on
        // Dictionary<LedId, …>), the real file stays intact and subsequent
        // loads don't hit "Unterminated string" on a half-written JSON blob.
        private static void WriteJsonAtomic(string path, object payload)
        {
            var tmp = path + ".tmp";
            lock (_layerSaveLock)
            {
                using (var sw = new StreamWriter(tmp, false))
                {
                    var serializer = new JsonSerializer();
                    serializer.Converters.Add(new DictionaryConverter());
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    serializer.Serialize(sw, payload);
                    sw.WriteLine();
                    sw.Flush();
                }

                // File.Replace can throw "Unable to remove the file to be
                // replaced" when AV / OneDrive / Dropbox is briefly holding
                // the destination open. Retry with backoff before giving up.
                ReplaceWithRetry(tmp, path);
            }
        }

        private static void ReplaceWithRetry(string tmp, string path)
        {
            const int maxAttempts = 5;
            int delayMs = 50;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                        File.Replace(tmp, path, null);
                    else
                        File.Move(tmp, path);
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    Thread.Sleep(delayMs);
                    delayMs *= 2;
                }
                catch (UnauthorizedAccessException) when (attempt < maxAttempts)
                {
                    Thread.Sleep(delayMs);
                    delayMs *= 2;
                }
            }
        }

        // V2 of the file was a bare ConcurrentDictionary<int, Layer>; V3 wraps
        // that dict in an object with deviceLayouts. V4 adds deviceBrightness.
        // V5 adds disabledDevices. Detect via JObject having a "schemaVersion"
        // key. Legacy files (chromatics3 / V2) load with empty layouts /
        // brightness / disabled maps and upgrade to the latest schema on the
        // next save.
        public static (ConcurrentDictionary<int, Layer> layers,
                       Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts,
                       Dictionary<Guid, int> deviceBrightness,
                       List<Guid> disabledDevices)
            LoadLayerMappings()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, LayersFile);

            try
            {
                string json;
                using (var sr = new StreamReader(path))
                    json = sr.ReadToEnd();

                return ParseMappingFile(json);
            }
            catch (Exception ex)
            {
                // Log + rethrow — see LoadEffectSettings for rationale.
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Layers: {ex.Message}");
                throw;
            }
        }

        // Older callers (Tests, ImportLayerMappingsFromPath) want the
        // backwards-compatible 3-tuple shape. Drop the disabledDevices payload
        // and forward.
        internal static (ConcurrentDictionary<int, Layer> layers,
                        Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts,
                        Dictionary<Guid, int> deviceBrightness)
            LoadLayerMappings_LegacyTriple()
        {
            var (l, ly, b, _) = LoadLayerMappings();
            return (l, ly, b);
        }

        // Validates a raw JSON string as a Chromatics layer file.
        // Returns (true, null) when valid; (false, reason) when not.
        // Supports both the current schema-versioned format and the legacy
        // integer-keyed format so import remains backwards-compatible.
        public static (bool valid, string reason) ValidateLayerJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return (false, "The file is empty.");

            JToken token;
            try { token = JToken.Parse(json); }
            catch (JsonException) { return (false, "The file is not valid JSON."); }

            if (token is not JObject obj)
                return (false, "The file does not contain a JSON object.");

            // Current format: top-level object has schemaVersion + layers.
            if (obj["schemaVersion"] != null && obj["layers"] != null)
                return (true, null);

            // Legacy format: every top-level key is an integer layer ID.
            if (obj.Properties().All(p => int.TryParse(p.Name, out _)))
                return (true, null);

            return (false,
                "The file does not appear to be a Chromatics layer file. " +
                "It may be a palette, settings, or other Chromatics data file.");
        }

        // Convenience overload that reads from disk before validating.
        public static (bool valid, string reason) ValidateLayerFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return (false, "No file path specified.");
            if (!File.Exists(filePath))
                return (false, "File does not exist.");

            string json;
            try
            {
                using var sr = new StreamReader(filePath);
                json = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                return (false, $"Could not read file: {ex.Message}");
            }

            return ValidateLayerJson(json);
        }

        // Validates a raw JSON string as a Chromatics palette file.
        // Returns (true, null) when valid; (false, reason) when not.
        // Does not cover the legacy .chromatics XML format — that path is
        // handled by the XmlSerializer branch in ImportColorMappingsFromPath.
        public static (bool valid, string reason) ValidatePaletteJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return (false, "The file is empty.");

            JToken token;
            try { token = JToken.Parse(json); }
            catch (JsonException) { return (false, "The file is not valid JSON."); }

            if (token is not JObject obj)
                return (false, "The file does not contain a JSON object.");

            // Positive identification of layer files so we can give a specific message.
            if (obj["schemaVersion"] != null && obj["layers"] != null)
                return (false,
                    "This looks like a layer mapping file, not a colour palette. " +
                    "To import your layers, use the Import button on the Mapping tab instead.");

            if (obj.Properties().Any() && obj.Properties().All(p => int.TryParse(p.Name, out _)))
                return (false,
                    "This looks like a legacy layer mapping file, not a colour palette. " +
                    "To import your layers, use the Import button on the Mapping tab instead.");

            // PaletteColorModel always serialises a top-level "version" field.
            if (obj["version"] != null)
                return (true, null);

            return (false,
                "The file does not appear to be a Chromatics colour palette. " +
                "Please select a palette file (palette.chromatics4 or palette.chromatics3).");
        }

        // Convenience overload that reads from disk before validating.
        public static (bool valid, string reason) ValidatePaletteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return (false, "No file path specified.");
            if (!File.Exists(filePath))
                return (false, "File does not exist.");

            string json;
            try
            {
                using var sr = new StreamReader(filePath);
                json = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                return (false, $"Could not read file: {ex.Message}");
            }

            return ValidatePaletteJson(json);
        }

        private static (ConcurrentDictionary<int, Layer> layers,
                        Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts,
                        Dictionary<Guid, int> deviceBrightness,
                        List<Guid> disabledDevices)
            ParseMappingFile(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return (null, null, null, null);

            JToken token;
            try { token = JToken.Parse(json); }
            catch (JsonException) { return (null, null, null, null); }

            if (token is not JObject obj) return (null, null, null, null);

            // Current schema-versioned format.
            if (obj["schemaVersion"] != null && obj["layers"] != null)
            {
                try
                {
                    var layers = obj["layers"].ToObject<ConcurrentDictionary<int, Layer>>(
                        JsonSerializer.Create(new JsonSerializerSettings
                        {
                            Converters = { new DictionaryConverter() }
                        }));

                    var deviceLayouts = obj["deviceLayouts"]?
                        .ToObject<Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>>()
                        ?? new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>();

                    // Optional: present from schemaVersion 4 onward. V3 files
                    // and chromatics3 imports come back with an empty map.
                    var deviceBrightness = obj["deviceBrightness"]?
                        .ToObject<Dictionary<Guid, int>>()
                        ?? new Dictionary<Guid, int>();

                    // Optional: present from schemaVersion 5 onward. v3/v4
                    // files come back with an empty list (no per-device
                    // disable history persisted).
                    var disabledDevices = obj["disabledDevices"]?
                        .ToObject<List<Guid>>()
                        ?? new List<Guid>();

                    return (layers, deviceLayouts, deviceBrightness, disabledDevices);
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error,
                        $"Failed to parse layer file (schema format): {ex.Message}");
                    return (null, null, null, null);
                }
            }

            // Legacy format: all top-level keys are integer layer IDs.
            // Guard against non-layer files (e.g. palette/settings files with string keys).
            if (!obj.Properties().All(p => int.TryParse(p.Name, out _)))
                return (null, null, null, null);

            try
            {
                var legacy = obj.ToObject<ConcurrentDictionary<int, Layer>>(
                    JsonSerializer.Create(new JsonSerializerSettings
                    {
                        Converters = { new DictionaryConverter() }
                    }));
                return (legacy,
                    new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>(),
                    new Dictionary<Guid, int>(),
                    new List<Guid>());
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"Failed to parse layer file (legacy format): {ex.Message}");
                return (null, null, null, null);
            }
        }




        public static bool CheckLayerMappingsExist()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, LayersFile);

            if (File.Exists(path))
                return true;

            return false;
        }

        // Import — callers supply the path (picked via Avalonia StorageProvider).
        // Imported files don't propagate the disabledDevices list (the user is
        // bringing in mapping data, not a disable history) — that part of the
        // tuple is dropped here. Any caller wanting the full v5 payload should
        // call LoadLayerMappings against the live config dir instead.
        public static (ConcurrentDictionary<int, Layer> layers,
                       Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts,
                       Dictionary<Guid, int> deviceBrightness)
            ImportLayerMappingsFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return (null, null, null);

            Logger.WriteConsole(Enums.LoggerTypes.System, @"Importing Layers..");

            try
            {
                string json;
                using (var sr = new StreamReader(path))
                    json = sr.ReadToEnd();

                var (layers, layouts, brightness, _) = ParseMappingFile(json);
                Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully imported layers from {path}.");
                return (layers, layouts, brightness);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error importing layers. Error: {ex.Message}");
                return (null, null, null);
            }
        }

        public static void ExportLayerMappingsToPath(ConcurrentDictionary<int, Layer> layers, string path,
            IDictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts = null,
            IDictionary<Guid, int> deviceBrightness = null)
        {
            if (string.IsNullOrWhiteSpace(path) || layers == null) return;

            Logger.WriteConsole(Enums.LoggerTypes.System, @"Exporting Layers..");

            try
            {
                var layersCopy = new ConcurrentDictionary<int, Layer>();
                foreach (var key in layers.Keys)
                    layersCopy[key] = CloneLayerWithoutDeviceGuid(layers[key]);

                var wrapper = new MappingFileV3
                {
                    schemaVersion = 6,
                    layers = layersCopy,
                    deviceLayouts = deviceLayouts != null
                        ? new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>(deviceLayouts)
                        : new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>(),
                    deviceBrightness = deviceBrightness != null
                        ? new Dictionary<Guid, int>(deviceBrightness)
                        : new Dictionary<Guid, int>(),
                    // Exported files don't carry a disabledDevices list — the
                    // user is sharing mappings, not their personal disable
                    // history. Empty list keeps the schema valid.
                    disabledDevices = new List<Guid>()
                };

                WriteJsonAtomic(path, wrapper);

                Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully exported layers to {path}.");
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error exporting layers. Error: {ex.Message}");
            }
        }

        // Helper method to clone a layer without the deviceGuid
        private static Layer CloneLayerWithoutDeviceGuid(Layer originalLayer)
        {
            return new Layer(
                originalLayer.layerVersion,
                originalLayer.layerID,
                originalLayer.layerIndex,
                originalLayer.rootLayerType,
                Guid.Empty, // deviceGuid set to empty
                originalLayer.deviceType,
                originalLayer.layerTypeindex,
                originalLayer.zindex,
                originalLayer.Enabled,
                new Dictionary<int, LedId>(originalLayer.deviceLeds), // Ensure proper conversion
                originalLayer.allowBleed,
                originalLayer.layerModes
            );
        }

        public static bool CreateLayersBackup()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, LayersFile);
            var backupFilePath = Path.Combine(Path.GetDirectoryName(path), $"backup_layers_{DateTime.Now:yyyyMMdd_HHmmss}.chromatics4");

            try
            {
                File.Copy(path, backupFilePath);
                Logger.WriteConsole(Enums.LoggerTypes.System, $"Backing up layers to {backupFilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Failed to create backup: {ex.Message}");
                return false;
            }
        }


        public static void SaveColorMappings(PaletteColorModel palette)
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, PaletteFile);

            try
            {
                WriteJsonAtomic(path, palette);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Saving Color Palette: {ex.Message}");
            }
        }

        public static PaletteColorModel LoadColorMappings()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, PaletteFile);
            var result = new PaletteColorModel();

            try
            {
                using (var sr = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<PaletteColorModel>(sr.ReadToEnd());
                    sr.Close();
                }

                if (result == null)
                    return null;

                var migrated   = MigratePaletteIfNeeded(result);
                var normalized = NormalizePaletteDisplayNames(result);
                if (migrated || normalized)
                    SaveColorMappings(result);

                return result;
            }
            catch (Exception ex)
            {
                // Log + rethrow — see LoadEffectSettings for rationale.
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Color Palette: {ex.Message}");
                throw;
            }
        }

        // Upgrades older palette files in-place to the current schema. Fields added in later
        // versions are auto-populated by their C# initialisers during deserialisation, so the
        // migration only needs to (a) tag the file with the new version, and (b) drop or remap
        // any values that no longer make sense in the new schema. Returns true if the palette
        // was mutated and should be re-persisted.
        private static bool MigratePaletteIfNeeded(PaletteColorModel palette)
        {
            if (palette.version == PaletteColorModel.CurrentVersion)
                return false;

            var from = palette.version ?? "1";
            Logger.WriteConsole(Enums.LoggerTypes.System,
                $"Migrating colour palette from v{from} to v{PaletteColorModel.CurrentVersion}.");

            // v1 -> v2: Dawntrail job-gauge additions. Newly added ColorMapping fields come from
            // their initialisers automatically, so there is no per-field remap required here.
            // Future palette-schema transitions should branch on `from` and mutate the model in
            // place before the final version bump below.

            palette.version = PaletteColorModel.CurrentVersion;
            return true;
        }

        // Corrects display names that were renamed in a later version of Chromatics.
        // ColorMapping.Name is serialised, so stale palette files will restore old names even after
        // the C# initialiser has been updated. This runs on every load (version-independent) so
        // palettes that skipped migration still get corrected names.
        private static bool NormalizePaletteDisplayNames(PaletteColorModel palette)
        {
            var changed = false;

            // Dawntrail: NIN Huton replaced by Kazematoi in the same gauge-A slot.
            if (palette.JobNINHuton != null && palette.JobNINHuton.Name != "NIN: Kazematoi")
            {
                palette.JobNINHuton.Name = "NIN: Kazematoi";
                changed = true;
            }

            return changed;
        }

        public static bool CheckColorMappingsExist()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, PaletteFile);

            if (File.Exists(path))
                return true;

            return false;
        }



        public static PaletteColorModel ImportColorMappingsFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

            var ext = Path.GetExtension(path);

            if (ext == ".chromatics4" || ext == ".chromatics3" || ext == ".chromatics2")
            {
                Logger.WriteConsole(Enums.LoggerTypes.System, @"Importing Color Palette..");
                try
                {
                    using var sr = new StreamReader(path);
                    var result = JsonConvert.DeserializeObject<PaletteColorModel>(sr.ReadToEnd());
                    NormalizePaletteDisplayNames(result);
                    Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully imported Color Palette from {path}.");
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error importing Color Palette. Error: {ex.Message}");
                    return null;
                }
            }
            else if (ext == ".chromatics")
            {
                Logger.WriteConsole(Enums.LoggerTypes.System, @"Converting legacy Color Palette..");
                try
                {
                    Debug.WriteLine("Legacy file detected");
                    var result = new PaletteColorModel();
                    using var sr = new StreamReader(path);
                    var reader = new XmlSerializer(typeof(LegacyColorMappings));
                    var data = sr.ReadToEnd();
                    data = data.Replace("FfxivColorMappings", "LegacyColorMappings");
                    var bytes = Encoding.ASCII.GetBytes(data);
                    using var ms = new MemoryStream(bytes);
                    var colorMappings = (LegacyColorMappings)reader.Deserialize(ms);

                    foreach (var p in colorMappings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        foreach (var f in result.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (p.Name.Contains(f.Name))
                            {
                                var color = ColorTranslator.FromHtml((string)p.GetValue(colorMappings));
                                var mapping = (ColorMapping)f.GetValue(result);
                                f.SetValue(result, new ColorMapping(mapping.Name, mapping.Type, color));
                            }
                        }
                    }

                    Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully converted & imported legacy Color Palette from {path}.");
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error importing legacy Color Palette. Error: {ex.Message}");
                    return null;
                }
            }

            Logger.WriteConsole(Enums.LoggerTypes.Error, @"Unsupported palette file extension.");
            return null;
        }

        public static void ExportColorMappingsToPath(PaletteColorModel palette, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || palette == null) return;

            Logger.WriteConsole(Enums.LoggerTypes.System, @"Exporting Color Palette..");
            try
            {
                using var sw = new StreamWriter(path, false);
                var serializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
                serializer.Serialize(sw, palette);
                sw.WriteLine();
                Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully exported color palette to {path}.");
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error exporting Color Palette. Error: {ex.Message}");
            }
        }

        public static void SaveEffectSettings(EffectTypesModel palette)
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, EffectsFile);

            try
            {
                WriteJsonAtomic(path, palette);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Saving Effects: {ex.Message}");
            }
        }

        public static EffectTypesModel LoadEffectSettings()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, EffectsFile);

            try
            {
                using var sr = new StreamReader(path);
                return JsonConvert.DeserializeObject<EffectTypesModel>(sr.ReadToEnd());
            }
            catch (Exception ex)
            {
                // Log + rethrow. The previous "log + return null" form caused
                // the app to silently continue with broken/missing effect data
                // when effects.chromatics4 was corrupted — no exception
                // reached the crash handler, so the user saw "no dialog, just
                // a zombie process". Letting it propagate routes through
                // App.OnFrameworkInitializationCompleted's try/catch into
                // CrashHandler, which shows the themed dialog and force-kills.
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Effects: {ex.Message}");
                throw;
            }
        }

        public static bool CheckEffectSettingsExist()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, EffectsFile);

            if (File.Exists(path))
                return true;

            return false;
        }

        public static void SaveSettings(SettingsModel settings)
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, SettingsFile);

            try
            {
                WriteJsonAtomic(path, settings);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Saving Settings: {ex.Message}");
            }
        }

        public static SettingsModel LoadSettings()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, SettingsFile);

            try
            {
                using var sr = new StreamReader(path);
                return JsonConvert.DeserializeObject<SettingsModel>(sr.ReadToEnd());
            }
            catch (Exception ex)
            {
                // Log + rethrow — see LoadEffectSettings for rationale.
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Settings: {ex.Message}");
                throw;
            }
        }

        public static bool CheckSettingsExist()
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, SettingsFile);

            if (File.Exists(path))
                return true;

            return false;
        }

        public static string GetCsvData(string url, string csvPath)
        {
            var enviroment = GetConfigDirectory();
            var path = Path.Combine(enviroment, csvPath);

            // Run the async work on a thread-pool thread so callers on the UI
            // thread don't deadlock waiting for it to resume on a captured
            // SynchronizationContext. This still blocks the caller (the layer
            // pipeline is synchronous), but without the deadlock risk.
            using var dataStoreResult = Task.Run(() => _httpClient.GetAsync(new Uri(url))).GetAwaiter().GetResult();

            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var lastModified = dataStoreResult.Content.Headers.LastModified;

                if (fileInfo.LastWriteTimeUtc >= lastModified)
                {
                    return path;
                }
            }

            var dataStore = Task.Run(() => dataStoreResult.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
            // Was `csvPath` — the relative file name — which dropped the download
            // into the current working directory instead of next to the executable.
            // `path` (computed above from `enviroment`) is the correct absolute target.
            File.WriteAllText(path, dataStore);

            if (File.Exists(path))
            {
                return path;
            }

            #if DEBUG
            Debug.WriteLine(@"An error occurred downloading the file " + csvPath + @" from URI: " + url);
            #endif

            return string.Empty;
        }

    }
}
