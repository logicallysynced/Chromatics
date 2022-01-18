using Chromatics.Core;
using Chromatics.Layers;
using Chromatics.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Chromatics.Helpers
{
    public static class FileOperationsHelper
    {
        public static void SaveLayerMappings(ConcurrentDictionary<int, Layer> mappings)
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";
            
            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var serializer = new JsonSerializer();
                    serializer.Converters.Add(new DictionaryConverter());
                    serializer.NullValueHandling = NullValueHandling.Ignore;

                    serializer.Serialize(sw, mappings);
                    sw.WriteLine();
                    sw.Close();
                }


            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Saving Layers: {ex.Message}");
            }
        }

        public static ConcurrentDictionary<int, Layer> LoadLayerMappings()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";
            var result = new ConcurrentDictionary<int, Layer>();

            try
            {
                using (var sr = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<ConcurrentDictionary<int, Layer>>(sr.ReadToEnd(), new DictionaryConverter());
                    sr.Close();
                }

                if (result != null)
                    return result;

                return null;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"Error Loading Layers: {ex.Message}");
                return null;
            }
        }

        public static bool CheckLayerMappingsExist()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";

            if (File.Exists(path))
                return true;

            return false;
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
                        Debug.WriteLine("Legacy file detected");
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
    }
}
