using Chromatics.Core;
using Chromatics.Layers;
using Chromatics.Models;
using CsvHelper;
using FFXIVWeather.Models;
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
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace Chromatics.Helpers
{
    public static class FileOperationsHelper
    {
        private static bool weatherDataLoaded;
        private static WeatherData weatherData;
        private static readonly HttpClient _httpClient = new HttpClient();

        // Drag-repositioning a keycap fires SaveMappings on a thread-pool task on
        // every pointer-release. Rapid drags (or the preview tick touching the
        // same file path) can overlap and collide on the sibling ".tmp" handle,
        // producing "the process cannot access the file" IOExceptions.
        // Serialising all layer writes through this lock makes the
        // write-then-File.Replace pair atomic from the caller's perspective.
        private static readonly object _layerSaveLock = new object();

        public static void SaveLayerMappings(ConcurrentDictionary<int, Layer> mappings,
            IDictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts = null)
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";

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

                var wrapper = new MappingFileV3
                {
                    schemaVersion = 3,
                    layers = layersSnapshot,
                    deviceLayouts = layoutsSnapshot
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

                if (File.Exists(path))
                    File.Replace(tmp, path, null);
                else
                    File.Move(tmp, path);
            }
        }

        // V2 of the file was a bare ConcurrentDictionary<int, Layer>; V3 wraps
        // that dict in an object with deviceLayouts. Detect via JObject having
        // a "schemaVersion" key. Legacy files load with an empty layouts map
        // and upgrade to V3 on the next save.
        public static (ConcurrentDictionary<int, Layer> layers,
                       Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts)
            LoadLayerMappings()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";

            try
            {
                string json;
                using (var sr = new StreamReader(path))
                    json = sr.ReadToEnd();

                return ParseMappingFile(json);
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Layers: {ex.Message}");
                return (null, null);
            }
        }

        private static (ConcurrentDictionary<int, Layer> layers,
                        Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts)
            ParseMappingFile(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return (null, null);

            var token = JToken.Parse(json);
            if (token is JObject obj && obj["schemaVersion"] != null && obj["layers"] != null)
            {
                var layers = obj["layers"].ToObject<ConcurrentDictionary<int, Layer>>(
                    JsonSerializer.Create(new JsonSerializerSettings
                    {
                        Converters = { new DictionaryConverter() }
                    }));

                var deviceLayouts = obj["deviceLayouts"]?
                    .ToObject<Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>>()
                    ?? new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>();

                return (layers, deviceLayouts);
            }

            var legacy = token.ToObject<ConcurrentDictionary<int, Layer>>(
                JsonSerializer.Create(new JsonSerializerSettings
                {
                    Converters = { new DictionaryConverter() }
                }));
            return (legacy, new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>());
        }




        public static bool CheckLayerMappingsExist()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";

            if (File.Exists(path))
                return true;

            return false;
        }

        public static (ConcurrentDictionary<int, Layer> layers,
                       Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts)
            ImportLayerMappings()
        {
            var open = new OpenFileDialog
            {
                Filter = "Chromatics Layer Files|*.chromatics3",
                Title = "Import Chromatics Layers",
                AddExtension = true,
                AutoUpgradeEnabled = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "chromatics3",
                DereferenceLinks = true,
                FileName = "layers",
                FilterIndex = 1,
                Multiselect = false,
                ReadOnlyChecked = false,
                RestoreDirectory = false,
                ShowHelp = false,
                ShowReadOnly = false,
                SupportMultiDottedExtensions = false,
                ValidateNames = true
            };

            if (open.ShowDialog() != DialogResult.OK)
            {
                return (null, null);
            }

            Logger.WriteConsole(Enums.LoggerTypes.System, @"Importing Layers..");

            try
            {
                string json;
                using (var sr = new StreamReader(open.FileName))
                    json = sr.ReadToEnd();

                var parsed = ParseMappingFile(json);
                Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully imported layers from {open.FileName}.");
                return parsed;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error importing layers. Error: {ex.Message}");
                return (null, null);
            }
            finally
            {
                open.Dispose();
            }
        }

        // Dialogless import — callers (e.g. Avalonia code-behind using StorageProvider)
        // hand us a path directly. Shares the deserialize/logging path with the
        // dialog-based overload above.
        public static (ConcurrentDictionary<int, Layer> layers,
                       Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts)
            ImportLayerMappingsFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return (null, null);

            Logger.WriteConsole(Enums.LoggerTypes.System, @"Importing Layers..");

            try
            {
                string json;
                using (var sr = new StreamReader(path))
                    json = sr.ReadToEnd();

                var parsed = ParseMappingFile(json);
                Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully imported layers from {path}.");
                return parsed;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error importing layers. Error: {ex.Message}");
                return (null, null);
            }
        }

        public static void ExportLayerMappingsToPath(ConcurrentDictionary<int, Layer> layers, string path,
            IDictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts = null)
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
                    schemaVersion = 3,
                    layers = layersCopy,
                    deviceLayouts = deviceLayouts != null
                        ? new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>(deviceLayouts)
                        : new Dictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>>()
                };

                WriteJsonAtomic(path, wrapper);

                Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully exported layers to {path}.");
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error exporting layers. Error: {ex.Message}");
            }
        }

        public static void ExportLayerMappings(ConcurrentDictionary<int, Layer> layers,
            IDictionary<Guid, Dictionary<RGB.NET.Core.LedId, DeviceKeyPosition>> deviceLayouts = null)
        {
            var save = new SaveFileDialog
            {
                AddExtension = true,
                AutoUpgradeEnabled = true,
                CheckFileExists = false,
                CheckPathExists = true,
                CreatePrompt = false,
                DefaultExt = "chromatics3",
                DereferenceLinks = true,
                FileName = "layers",
                Filter = "Chromatics Layer Files|*.chromatics3",
                FilterIndex = 1,
                InitialDirectory = "",
                OverwritePrompt = true,
                RestoreDirectory = false,
                ShowHelp = false,
                SupportMultiDottedExtensions = false,
                Title = "Export Chromatics Layers",
                ValidateNames = true
            };

            if (save.ShowDialog() == DialogResult.OK)
            {
                ExportLayerMappingsToPath(layers, save.FileName, deviceLayouts);
                save.Dispose();
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
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";
            var backupFilePath = Path.Combine(Path.GetDirectoryName(path), $"backup_layers_{DateTime.Now:yyyyMMdd_HHmmss}.chromatics3");

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
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/palette.chromatics3";

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var serializer = new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    };

                    serializer.Serialize(sw, palette);
                    sw.WriteLine();
                    sw.Close();
                }


            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Saving Color Palette: {ex.Message}");
            }
        }

        public static PaletteColorModel LoadColorMappings()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/palette.chromatics3";
            var result = new PaletteColorModel();

            try
            {
                using (var sr = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<PaletteColorModel>(sr.ReadToEnd());
                    sr.Close();
                }

                if (result != null)
                    return result;

                return null;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Color Palette: {ex.Message}");
                return null;
            }
        }

        public static bool CheckColorMappingsExist()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/palette.chromatics3";

            if (File.Exists(path))
                return true;

            return false;
        }



        public static PaletteColorModel ImportColorMappings()
        {
            var open = new OpenFileDialog
            {
                Filter = "Chromatics Palette Files|*.chromatics3|Legacy Palette Files|*.chromatics",
                Title = "Import Color Palette",
                AddExtension = true,
                AutoUpgradeEnabled = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "chromatics3",
                DereferenceLinks = true,
                FileName = "mypalette",
                FilterIndex = 1,
                Multiselect = false,
                ReadOnlyChecked = false,
                RestoreDirectory = false,
                ShowHelp = false,
                ShowReadOnly = false,
                SupportMultiDottedExtensions = false,
                ValidateNames = true
            };

            if (open.ShowDialog() == DialogResult.OK)
            {
                var ext = Path.GetExtension(open.FileName);

                if (ext == ".chromatics3")
                {
                    Logger.WriteConsole(Enums.LoggerTypes.System, @"Importing Color Palette..");

                    try
                    {
                        var result = new PaletteColorModel();

                        using (var sr = new StreamReader(open.FileName))
                        {
                            result = JsonConvert.DeserializeObject<PaletteColorModel>(sr.ReadToEnd());
                            sr.Close();

                            Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully imported Color Palette from {open.FileName}.");
                            open.Dispose();
                        }

                        return result;

                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error importing Color Palette. Error: {ex.Message}");
                        open.Dispose();
                        return null;
                    }
                }
                else if (ext == ".chromatics")
                {
                    //Import color mappings from Chromatics 2.x and convert
                    Logger.WriteConsole(Enums.LoggerTypes.System, @"Converting legacy Color Palette..");

                    try
                    {
#if DEBUG
                        Debug.WriteLine("Legacy file detected");
#endif

                        var result = new PaletteColorModel();

                        using (var sr = new StreamReader(open.FileName))
                        {
                            var reader = new XmlSerializer(typeof(LegacyColorMappings));
                            var data = sr.ReadToEnd();
                            sr.Close();

                            data = data.Replace("FfxivColorMappings", "LegacyColorMappings");
                            var bytes = Encoding.ASCII.GetBytes(data);
                            var _sr = new MemoryStream(bytes);

                            var colorMappings = (LegacyColorMappings)reader.Deserialize(_sr);

                            _sr.Close();

                            foreach (var p in colorMappings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                            {
                                foreach (var f in result.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                                {
                                    if (p.Name.Contains(f.Name))
                                    {
                                        var color = ColorTranslator.FromHtml((string)p.GetValue(colorMappings));

                                        var mapping = (ColorMapping)f.GetValue(result);
                                        var new_mapping = new ColorMapping(mapping.Name, mapping.Type, color);
                                        f.SetValue(result, new_mapping);
                                    }
                                }
                            }

                            Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully converted & imported legacy Color Palette from {open.FileName}.");
                            open.Dispose();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error importing legacy Color Palette. Error: {ex.Message}");
                        open.Dispose();
                        return null;
                    }
                }

                Logger.WriteConsole(Enums.LoggerTypes.Error, @"Error importing legacy Color Palette.");
                return null;

            }
            else
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, @"Error importing Color Palette.");
                return null;
            }
        }

        public static void ExportColorMappings(PaletteColorModel palette)
        {
            var save = new SaveFileDialog
            {
                AddExtension = true,
                AutoUpgradeEnabled = true,
                CheckFileExists = false,
                CheckPathExists = true,
                CreatePrompt = false,
                DefaultExt = "chromatics3",
                DereferenceLinks = true,
                FileName = "mypalette",
                Filter = "Chromatics Palette Files|*.chromatics3",
                FilterIndex = 1,
                InitialDirectory = "",
                OverwritePrompt = true,
                RestoreDirectory = false,
                ShowHelp = false,
                SupportMultiDottedExtensions = false,
                Title = "Export Color Palette",
                ValidateNames = true
            };


            if (save.ShowDialog() == DialogResult.OK)
            {
                Logger.WriteConsole(Enums.LoggerTypes.System, @"Exporting Color Palette..");

                try
                {
                    using (var sw = new StreamWriter(save.FileName, false))
                    {
                        var serializer = new JsonSerializer
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };

                        serializer.Serialize(sw, palette);
                        sw.WriteLine();
                        sw.Close();
                    }

                    Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully exported color palette to {save.FileName}.");
                    save.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error exporting Color Palette. Error: {ex.Message}");
                }
            }
        }

        public static void SaveEffectSettings(EffectTypesModel palette)
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/effects.chromatics3";

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var serializer = new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    };

                    serializer.Serialize(sw, palette);
                    sw.WriteLine();
                    sw.Close();
                }


            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Saving Effects: {ex.Message}");
            }
        }

        public static EffectTypesModel LoadEffectSettings()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/effects.chromatics3";
            var result = new EffectTypesModel();

            try
            {
                using (var sr = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<EffectTypesModel>(sr.ReadToEnd());
                    sr.Close();
                }

                if (result != null)
                    return result;

                return null;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Effects: {ex.Message}");
                return null;
            }
        }

        public static bool CheckEffectSettingsExist()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/effects.chromatics3";

            if (File.Exists(path))
                return true;

            return false;
        }

        public static void SaveSettings(SettingsModel settings)
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/settings.chromatics3";

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var serializer = new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    };

                    serializer.Serialize(sw, settings);
                    sw.WriteLine();
                    sw.Close();
                }


            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Saving Settings: {ex.Message}");
            }
        }

        public static SettingsModel LoadSettings()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/settings.chromatics3";
            var result = new SettingsModel();

            try
            {
                using (var sr = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<SettingsModel>(sr.ReadToEnd());
                    sr.Close();
                }

                if (result != null)
                    return result;

                return null;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Settings: {ex.Message}");
                return null;
            }
        }

        public static bool CheckSettingsExist()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/settings.chromatics3";

            if (File.Exists(path))
                return true;

            return false;
        }

        public static bool CheckWeatherDataLoaded()
        {
            return weatherDataLoaded;
        }

        public static WeatherData GetWeatherDataLoaded()
        {
            if (!weatherDataLoaded) return null;

            return weatherData;
        }

        public static string GetCsvData(string url, string csvPath)
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
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

        public static WeatherData GetUpdatedWeatherData()
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var WeatherKindsOutputPath = directory + @"/weatherKinds.json";
            var WeatherRateIndicesOutputPath = directory + @"/weatherRateIndices.json";
            var TerriTypesOutputPath = directory + @"/terriTypes.json";

            // GarlandTools
            var dataStoreResult = _httpClient.GetAsync(new Uri(@"https://www.garlandtools.org/db/doc/core/en/3/data.json")).GetAwaiter().GetResult();

            if (File.Exists(WeatherKindsOutputPath) && File.Exists(WeatherRateIndicesOutputPath) && File.Exists(TerriTypesOutputPath))
            {
                var fileInfo = new FileInfo(WeatherRateIndicesOutputPath);
                var lastModified = dataStoreResult.Content.Headers.LastModified;

                if (fileInfo.LastWriteTimeUtc >= lastModified)
                {
                    weatherDataLoaded = true;
                    var _weatherRateIndices = JsonConvert.DeserializeObject<List<WeatherRateIndex>>(File.ReadAllText(WeatherRateIndicesOutputPath));
                    var _terriTypes = JsonConvert.DeserializeObject<List<TerriType>>(File.ReadAllText(TerriTypesOutputPath));
                    var _weatherKinds = JsonConvert.DeserializeObject<List<Weather>>(File.ReadAllText(WeatherKindsOutputPath));

                    weatherData = new WeatherData
                    {
                        WeatherRateIndices = _weatherRateIndices,
                        TerriTypes = _terriTypes,
                        WeatherKinds = _weatherKinds
                    };

                    return weatherData;
                }
            }

            Logger.WriteConsole(Enums.LoggerTypes.System, @"Updated FFXIV data is available");
            Logger.WriteConsole(Enums.LoggerTypes.System, $"Requesting data from Garland Tools..");

            var dataStoreRaw = _httpClient.GetStringAsync(new Uri("https://www.garlandtools.org/db/doc/core/en/3/data.json")).GetAwaiter().GetResult();
            var dataStore = JObject.Parse(dataStoreRaw);

            var weatherRateIndices = new List<WeatherRateIndex>();
            var wris = dataStore["skywatcher"]["weatherRateIndex"].Children()
                .Select(token => token.Children().First());
            foreach (var wri in wris)
            {
                weatherRateIndices.Add(new WeatherRateIndex
                {
                    Id = wri["id"].ToObject<int>(),
                    Rates = wri["rates"].Children()
                        .Select(rate => new WeatherRate
                        {
                            Id = rate["weather"].ToObject<int>(),
                            Rate = rate["rate"].ToObject<int>(),
                        })
                        .ToArray(),
                });
            }

            // Quick validation of a design assumption.
            var wriLastN = 0;
            foreach (var weatherRateIndex in weatherRateIndices)
            {
                if (weatherRateIndex.Id != wriLastN)
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"Garland Tools: Data is not continuous and/or sorted in ascending order.");
                wriLastN++;
            }

            File.WriteAllText(WeatherRateIndicesOutputPath, JsonConvert.SerializeObject(weatherRateIndices));

            // XIVAPI
            #if DEBUG
            Debug.WriteLine(@"Requesting data from XIVAPI and FFCafe...");
            #endif

            var terriTypes = new List<TerriType>();
            {
                var page = 1;
                var pageTotal = 1;
                while (page <= pageTotal)
                {
                    var dataStore2Raw = _httpClient.GetStringAsync(new Uri($"https://xivapi.com/TerritoryType?columns=ID,WeatherRate,PlaceName&Page={page}")).GetAwaiter().GetResult();
                    var dataStore2 = JObject.Parse(dataStore2Raw);

                    pageTotal = dataStore2["Pagination"]["PageTotal"].ToObject<int>();

                    foreach (var child in dataStore2["Results"].Children())
                    {
                        if (!child["PlaceName"].Children().Any()) continue;

                        terriTypes.Add(new TerriType
                        {
                            Id = child["ID"].ToObject<int>(),
                            WeatherRate = child["WeatherRate"].ToObject<int>(),
                            NameEn = child["PlaceName"]["Name_en"].ToObject<string>(),
                            NameDe = child["PlaceName"]["Name_de"].ToObject<string>(),
                            NameFr = child["PlaceName"]["Name_fr"].ToObject<string>(),
                            NameJa = child["PlaceName"]["Name_ja"].ToObject<string>(),
                        });
                    }

                    page++;
                }

                var cafeCsvRaw = _httpClient.GetStreamAsync(new Uri(@"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/refs/heads/master/csv/en/PlaceName.csv")).GetAwaiter().GetResult();
                using var cafeSr = new StreamReader(cafeCsvRaw);
                using var cafeCsv = new CsvReader(cafeSr, CultureInfo.InvariantCulture);
                for (var i = 0; i < 3; i++) cafeCsv.Read();
                while (cafeCsv.Read())
                {
                    var id = cafeCsv.GetField<int>(0);
                    var terriType = terriTypes.FirstOrDefault(tt => tt.Id == id);
                    if (terriType == null)
                        continue;
                    terriType.NameZh = cafeCsv.GetField<string>(1);
                }
            }

            // Quick validation of a design assumption.
            var ttLastN = 0;
            foreach (var terriType in terriTypes)
            {
                if (terriType.Id < ttLastN)
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"XIVAPI: Data is not continuous and/or sorted in ascending order.");
                ttLastN = terriType.Id;
            }

            File.WriteAllText(TerriTypesOutputPath, JsonConvert.SerializeObject(terriTypes));

            var weatherKinds = new List<Weather>();

            {
                var page = 1;
                var pageTotal = 1;
                while (page <= pageTotal)
                {
                    var dataStore2Raw = _httpClient.GetStringAsync(new Uri($"https://xivapi.com/Weather?columns=ID,Name_en,Name_de,Name_fr,Name_ja&Page={page}")).GetAwaiter().GetResult();
                    var dataStore2 = JObject.Parse(dataStore2Raw);

                    pageTotal = dataStore2["Pagination"]["PageTotal"].ToObject<int>();

                    foreach (var child in dataStore2["Results"].Children())
                    {
                        var id = child["ID"].ToObject<int>();

                        weatherKinds.Add(new Weather
                        {
                            Id = id,
                            NameEn = child["Name_en"].ToObject<string>(),
                            NameDe = child["Name_de"].ToObject<string>(),
                            NameFr = child["Name_fr"].ToObject<string>(),
                            NameJa = child["Name_ja"].ToObject<string>(),
                        });
                    }

                    page++;
                }

                var cafeCsvRaw = _httpClient.GetStreamAsync(new Uri(@"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/refs/heads/master/csv/en/Weather.csv")).GetAwaiter().GetResult();
                using var cafeSr = new StreamReader(cafeCsvRaw);
                using var cafeCsv = new CsvReader(cafeSr, CultureInfo.InvariantCulture);
                for (var i = 0; i < 3; i++) cafeCsv.Read();
                while (cafeCsv.Read())
                {
                    var id = cafeCsv.GetField<int>(0);
                    var weatherKind = weatherKinds.FirstOrDefault(wk => wk.Id == id);
                    if (weatherKind == null)
                        continue;
                    weatherKind.NameZh = cafeCsv.GetField<string>(2);
                }
            }


            // Quick validation of a design assumption.
            var wkLastN = 1;
            foreach (var weatherKind in weatherKinds)
            {
                if (weatherKind.Id != wkLastN)
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"FFCafe: Data is not continuous and/or sorted in ascending order.");
                wkLastN++;
            }
            File.WriteAllText(WeatherKindsOutputPath, JsonConvert.SerializeObject(weatherKinds));

            Logger.WriteConsole(Enums.LoggerTypes.System, $"Successfully updated internal database from Garland Tools");
            weatherDataLoaded = true;

            weatherData = new WeatherData
            {
                WeatherRateIndices = weatherRateIndices,
                TerriTypes = terriTypes,
                WeatherKinds = weatherKinds
            };

            return weatherData;
        }
    }
}
