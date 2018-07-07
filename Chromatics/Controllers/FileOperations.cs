using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;
using Chromatics.Controllers;
using Chromatics.Datastore;

namespace Chromatics
{
    partial class Chromatics : ILogWrite
    {
        public void SaveDevices()
        {
            //WriteConsole(ConsoleTypes.SYSTEM, "Saving states to devices.chromatics..");
            var dr = new DeviceDataStore();
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/devices.chromatics";

            dr.DeviceOperationRazerKeyboard = _razerDeviceKeyboard;
            dr.DeviceOperationRazerMouse = _razerDeviceMouse;
            dr.DeviceOperationRazerMousepad = _razerDeviceMousepad;
            dr.DeviceOperationRazerKeypad = _razerDeviceKeypad;
            dr.DeviceOperationRazerHeadset = _razerDeviceHeadset;
            dr.DeviceOperationRazerChromaLink = _razerDeviceChromaLink;

            dr.DeviceOperationCorsairKeyboard = _corsairDeviceKeyboard;
            dr.DeviceOperationCorsairMouse = _corsairDeviceMouse;
            dr.DeviceOperationCorsairMousepad = _corsairDeviceMousepad;
            dr.DeviceOperationCorsairKeypad = _corsairDeviceKeypad; //Not Implemented
            dr.DeviceOperationCorsairHeadset = _corsairDeviceHeadset;

            dr.DeviceOperationCoolermasterKeyboard = _coolermasterDeviceKeyboard;
            dr.DeviceOperationCoolermasterMouse = _coolermasterDeviceMouse;

            dr.DeviceOperationSteelKeyboard = _steelDeviceKeyboard;
            dr.DeviceOperationSteelHeadset = _steelDeviceHeadset;
            dr.DeviceOperationSteelMouse = _steelDeviceMouse;

            dr.DeviceOperationWootingKeyboard = _wootingDeviceKeyboard;

            dr.DeviceOperationRoccatKeyboard = _roccatDeviceKeyboard;
            dr.DeviceOperationRoccatMouse = _roccatDeviceMouse;

            dr.DeviceOperationLogitechKeyboard = _logitechDeviceKeyboard;

            dr.DeviceOperationKeyboard = _deviceKeyboard;
            dr.DeviceOperationMouse = _deviceMouse;
            dr.DeviceOperationMousepad = _deviceMousepad;
            dr.DeviceOperationHeadset = _deviceHeadset;
            dr.DeviceOperationKeypad = _deviceKeypad;
            dr.DeviceOperationCL = _deviceCL;

            dr.KeysSingleKeyModeEnabled = _KeysSingleKeyModeEnabled;
            dr.KeySingleKeyMode = Helpers.ConvertDevModeToString(_KeysSingleKeyMode);

            dr.KeysMultiKeyModeEnabled = _KeysMultiKeyModeEnabled;
            dr.KeyMultiKeyMode = Helpers.ConvertDevMultiModeToString(_KeysMultiKeyMode);

            dr.MouseZone1Mode = Helpers.ConvertDevModeToString(_MouseZone1Mode);
            dr.MouseZone2Mode = Helpers.ConvertDevModeToString(_MouseZone2Mode);
            dr.MouseZone3Mode = Helpers.ConvertDevModeToString(_MouseZone3Mode);
            dr.MouseStrip1Mode = Helpers.ConvertDevModeToString(_MouseStrip1Mode);
            dr.MouseStrip2Mode = Helpers.ConvertDevModeToString(_MouseStrip2Mode);

            dr.PadZone1Mode = Helpers.ConvertDevModeToString(_PadZone1Mode);
            dr.PadZone2Mode = Helpers.ConvertDevModeToString(_PadZone2Mode);
            dr.PadZone3Mode = Helpers.ConvertDevModeToString(_PadZone3Mode);

            dr.CLZone1Mode = Helpers.ConvertDevModeToString(_CLZone1Mode);
            dr.CLZone2Mode = Helpers.ConvertDevModeToString(_CLZone2Mode);
            dr.CLZone3Mode = Helpers.ConvertDevModeToString(_CLZone3Mode);
            dr.CLZone4Mode = Helpers.ConvertDevModeToString(_CLZone4Mode);
            dr.CLZone5Mode = Helpers.ConvertDevModeToString(_CLZone5Mode);

            dr.HeadsetZone1Mode = Helpers.ConvertDevModeToString(_HeadsetZone1Mode);
            dr.KeypadZone1Mode = Helpers.ConvertDevMultiModeToString(_KeypadZone1Mode);

            dr.LightbarMode = Helpers.ConvertLightbarModeToString(_LightbarMode);

            var lifxLoad = "";
            var hueLoad = "";

            if (LifxSdk && _lifx.LifxModeMemory.Any())
            {
                var lifxEx = _lifx.LifxModeMemory.Select(lp => lp.Key + "|" + lp.Value + "|" + _lifx.LifxStateMemory[lp.Key]).ToArray();
                lifxLoad = string.Join(",", lifxEx);
            }


            if (HueSdk && _hue.HueModeMemory.Any())
            {
                var hueEx = _hue.HueModeMemory.Select(hp => hp.Key + "|" + hp.Value + "|" + _hue.HueStateMemory[hp.Key]).ToArray();
                hueLoad = string.Join(",", hueEx);
            }


            dr.DeviceOperationHueDefault = _hueDefault;
            dr.DeviceOperationLifxDevices = lifxLoad;
            dr.DeviceOperationHueDevices = hueLoad;

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var x = new XmlSerializer(dr.GetType());
                    x.Serialize(sw, dr);
                    sw.WriteLine();
                    sw.Close();
                }

                //WriteConsole(ConsoleTypes.SYSTEM, "Saved states to devices.chromatics.");
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, @"Error saving states to devices.chromatics. Error: " + ex.Message);
            }
        }

        private void LoadDevices()
        {
            WriteConsole(ConsoleTypes.System, @"Searching for devices.chromatics..");
            var ds = new DeviceDataStore();
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/devices.chromatics";

            if (File.Exists(path))
            {
                //Read Device Save
                WriteConsole(ConsoleTypes.System, @"Attempting to load devices.chromatics..");
                using (var sr = new StreamReader(path))
                {
                    try
                    {
                        var reader = new XmlSerializer(ds.GetType());
                        var dr = (DeviceDataStore) reader.Deserialize(sr);
                        sr.Close();

                        _razerDeviceKeyboard = dr.DeviceOperationRazerKeyboard;
                        _razerDeviceMouse = dr.DeviceOperationRazerMouse;
                        _razerDeviceMousepad = dr.DeviceOperationRazerMousepad;
                        _razerDeviceKeypad = dr.DeviceOperationRazerKeypad;
                        _razerDeviceHeadset = dr.DeviceOperationRazerHeadset;
                        _razerDeviceChromaLink = dr.DeviceOperationRazerChromaLink;

                        _corsairDeviceKeyboard = dr.DeviceOperationCorsairKeyboard;
                        _corsairDeviceMouse = dr.DeviceOperationCorsairMouse;
                        _corsairDeviceMousepad = dr.DeviceOperationCorsairMousepad;
                        _corsairDeviceKeypad = dr.DeviceOperationCorsairKeypad; //Not Implemented
                        _corsairDeviceHeadset = dr.DeviceOperationCorsairHeadset;

                        _coolermasterDeviceKeyboard = dr.DeviceOperationCoolermasterKeyboard;
                        _coolermasterDeviceMouse = dr.DeviceOperationCoolermasterMouse;

                        _steelDeviceKeyboard = dr.DeviceOperationSteelKeyboard;
                        _steelDeviceMouse = dr.DeviceOperationSteelMouse;
                        _steelDeviceHeadset = dr.DeviceOperationSteelHeadset;

                        _wootingDeviceKeyboard = dr.DeviceOperationWootingKeyboard;

                        _roccatDeviceKeyboard = dr.DeviceOperationRoccatKeyboard;
                        _roccatDeviceMouse = dr.DeviceOperationRoccatMouse;

                        _logitechDeviceKeyboard = dr.DeviceOperationLogitechKeyboard;
                        _hueDefault = dr.DeviceOperationHueDefault;

                        _deviceKeyboard = dr.DeviceOperationKeyboard;
                        _deviceMouse = dr.DeviceOperationMouse;
                        _deviceMousepad = dr.DeviceOperationMousepad;
                        _deviceHeadset = dr.DeviceOperationHeadset;
                        _deviceKeypad = dr.DeviceOperationKeypad;
                        _deviceCL = dr.DeviceOperationCL;

                        _KeysSingleKeyModeEnabled = dr.KeysSingleKeyModeEnabled;
                        _KeysSingleKeyMode = Helpers.ConvertStringToDevMode(dr.KeySingleKeyMode);

                        _KeysMultiKeyModeEnabled = dr.KeysMultiKeyModeEnabled;
                        _KeysMultiKeyMode = Helpers.ConvertStringToDevMultiMode(dr.KeyMultiKeyMode);

                        _MouseZone1Mode = Helpers.ConvertStringToDevMode(dr.MouseZone1Mode);
                        _MouseZone2Mode = Helpers.ConvertStringToDevMode(dr.MouseZone2Mode);
                        _MouseZone3Mode = Helpers.ConvertStringToDevMode(dr.MouseZone3Mode);
                        _MouseStrip1Mode = Helpers.ConvertStringToDevMode(dr.MouseStrip1Mode);
                        _MouseStrip2Mode = Helpers.ConvertStringToDevMode(dr.MouseStrip2Mode);

                        _PadZone1Mode = Helpers.ConvertStringToDevMode(dr.PadZone1Mode);
                        _PadZone2Mode = Helpers.ConvertStringToDevMode(dr.PadZone2Mode);
                        _PadZone3Mode = Helpers.ConvertStringToDevMode(dr.PadZone3Mode);

                        _CLZone1Mode = Helpers.ConvertStringToDevMode(dr.CLZone1Mode);
                        _CLZone2Mode = Helpers.ConvertStringToDevMode(dr.CLZone2Mode);
                        _CLZone3Mode = Helpers.ConvertStringToDevMode(dr.CLZone3Mode);
                        _CLZone4Mode = Helpers.ConvertStringToDevMode(dr.CLZone4Mode);
                        _CLZone5Mode = Helpers.ConvertStringToDevMode(dr.CLZone5Mode);

                        _HeadsetZone1Mode = Helpers.ConvertStringToDevMode(dr.HeadsetZone1Mode);
                        _KeypadZone1Mode = Helpers.ConvertStringToDevMultiMode(dr.KeypadZone1Mode);

                        _LightbarMode = Helpers.ConvertStringToLightbarMode(dr.LightbarMode);

                        var lifxLoad = dr.DeviceOperationLifxDevices;

                        if (lifxLoad != "" && LifxSdkCalled == 1)
                        {
                            var lifxDevices = lifxLoad.Split(',');
                            foreach (var ld in lifxDevices)
                            {
                                var lState = ld.Split('|');

                                //BulbModeTypes LMode = BulbModeTypes.DISABLED;
                                //LMode = LState[1].ToString();
                                //int.TryParse(LState[1], out LMode);
                                var lMode = (BulbModeTypes) Enum.Parse(typeof(BulbModeTypes), lState[1]);

                                var lEnabled = 0;
                                int.TryParse(lState[2], out lEnabled);
                                _lifx.LifxModeMemory.Add(lState[0], lMode);
                                _lifx.LifxStateMemory.Add(lState[0], lEnabled);
                            }
                        }

                        var hueLoad = dr.DeviceOperationHueDevices;
                        if (hueLoad != "")
                        {
                            var hueDevices = hueLoad.Split(',');
                            foreach (var hd in hueDevices)
                            {
                                var hState = hd.Split('|');
                                //var HMode = 0;
                                //int.TryParse(HState[1], out HMode);
                                var hMode = (BulbModeTypes) Enum.Parse(typeof(BulbModeTypes), hState[1]);
                                var hEnabled = 0;
                                int.TryParse(hState[2], out hEnabled);
                                _hue.HueModeMemory.Add(hState[0], hMode);
                                _hue.HueStateMemory.Add(hState[0], hEnabled);
                                //HueModeMemory.Add(HState[0], HMode, HEnabled);
                            }
                        }

                        WriteConsole(ConsoleTypes.System, @"devices.chromatics loaded.");
                    }
                    catch (Exception ex)
                    {
                        WriteConsole(ConsoleTypes.Error, @"Error loading devices.chromatics. Error: " + ex.Message);
                    }
                }
            }
            else
            {
                //Create Device Save
                WriteConsole(ConsoleTypes.System, @"devices.chromatics not found. Creating one..");
                try
                {
                    using (var sw = new StreamWriter(path))
                    {
                        var x = new XmlSerializer(ds.GetType());
                        x.Serialize(sw, ds);
                        sw.WriteLine();
                        sw.Close();
                    }
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error, @"Error creating devices.chromatics. Error: " + ex.Message);
                }
            }
        }
        
        private void SaveColorMappings(int report)
        {
            //if (report == 1)
            //    WriteConsole(ConsoleTypes.SYSTEM, "Saving states to mappings.chromatics..");
            var colorMappings = new FfxivColorMappings();
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/mappings.chromatics";

            colorMappings = ColorMappings;

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var x = new XmlSerializer(colorMappings.GetType());
                    x.Serialize(sw, colorMappings);
                    sw.WriteLine();
                    sw.Close();
                }

                // if (report == 1)
                //     WriteConsole(ConsoleTypes.SYSTEM, "Saved states to mappings.chromatics.");
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, @"Error saving states to mappings.chromatics. Error: " + ex.Message);
            }
        }

        private void LoadColorMappings()
        {
            WriteConsole(ConsoleTypes.System, @"Searching for mappings.chromatics..");
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/mappings.chromatics";

            if (File.Exists(path))
            {
                //Read Device Save
                WriteConsole(ConsoleTypes.System, @"Attempting to load mappings.chromatics..");
                using (var sr = new StreamReader(path))
                {
                    try
                    {
                        var reader = new XmlSerializer(ColorMappings.GetType());
                        var colorMappings = (FfxivColorMappings) reader.Deserialize(sr);
                        sr.Close();

                        ColorMappings = colorMappings;

                        WriteConsole(ConsoleTypes.System, @"mappings.chromatics loaded.");
                    }
                    catch (Exception ex)
                    {
                        WriteConsole(ConsoleTypes.Error, @"Error loading mappings.chromatics. Error: " + ex.Message);
                    }
                }
            }
            else
            {
                //Create Device Save
                WriteConsole(ConsoleTypes.System, @"mappings.chromatics not found. Creating one..");
                try
                {
                    using (var sw = new StreamWriter(path))
                    {
                        var x = new XmlSerializer(ColorMappings.GetType());
                        x.Serialize(sw, ColorMappings);
                        sw.WriteLine();
                        sw.Close();
                    }
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error, @"Error creating mappings.chromatics. Error: " + ex.Message);
                }
            }
        }

        private void SaveChromaticsSettings(int report)
        {
            //if (report == 1)
            //    WriteConsole(ConsoleTypes.SYSTEM, "Saving states to settings.chromatics..");
            var chromaticsSettings = new ChromaticsSettings();
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/settings.chromatics";

            chromaticsSettings = ChromaticsSettings;

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var x = new XmlSerializer(chromaticsSettings.GetType());
                    x.Serialize(sw, chromaticsSettings);
                    sw.WriteLine();
                    sw.Close();
                }

                //if (report == 1)
                //    WriteConsole(ConsoleTypes.SYSTEM, "Saved states to settings.chromatics.");
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, @"Error saving states to settings.chromatics. Error: " + ex.Message);
            }
        }

        private void LoadChromaticsSettings()
        {
            WriteConsole(ConsoleTypes.System, @"Searching for settings.chromatics..");
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/settings.chromatics";

            if (File.Exists(path))
            {
                //Read Device Save
                WriteConsole(ConsoleTypes.System, @"Attempting to load settings.chromatics..");
                using (var sr = new StreamReader(path))
                {
                    try
                    {
                        var reader = new XmlSerializer(ChromaticsSettings.GetType());
                        var chromaticsSettings = (ChromaticsSettings) reader.Deserialize(sr);
                        sr.Close();

                        ChromaticsSettings = chromaticsSettings;

                        WriteConsole(ConsoleTypes.System, @"settings.chromatics loaded.");
                    }
                    catch (Exception ex)
                    {
                        WriteConsole(ConsoleTypes.Error, @"Error loading settings.chromatics. Error: " + ex.Message);
                    }
                }
            }
            else
            {
                //Create Device Save
                WriteConsole(ConsoleTypes.System, @"settings.chromatics not found. Creating one..");
                try
                {
                    using (var sw = new StreamWriter(path))
                    {
                        var x = new XmlSerializer(ChromaticsSettings.GetType());
                        x.Serialize(sw, ChromaticsSettings);
                        sw.WriteLine();
                        sw.Close();
                    }
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error, @"Error creating settings.chromatics. Error: " + ex.Message);
                }
            }
        }

        private void ImportColorMappings()
        {
            var open = new OpenFileDialog();
            open.Filter = "Chromatics Palette Files|*.chromatics";
            open.Title = "Import Color Palette";
            open.AddExtension = true;
            open.AutoUpgradeEnabled = true;
            open.CheckFileExists = true;
            open.CheckPathExists = true;
            open.DefaultExt = "chromatics";
            open.DereferenceLinks = true;
            open.FileName = "mypalette";
            open.FilterIndex = 1;
            open.Multiselect = false;
            open.ReadOnlyChecked = false;
            open.RestoreDirectory = false;
            open.ShowHelp = false;
            open.ShowReadOnly = false;
            open.SupportMultiDottedExtensions = false;
            open.ValidateNames = true;

            if (open.ShowDialog() == DialogResult.OK)
            {
                WriteConsole(ConsoleTypes.System, @"Importing Color Palette..");

                try
                {
                    using (var sr = new StreamReader(open.FileName))
                    {
                        var reader = new XmlSerializer(ColorMappings.GetType());
                        var colorMappings = (FfxivColorMappings) reader.Deserialize(sr);
                        sr.Close();

                        ColorMappings = colorMappings;

                        WriteConsole(ConsoleTypes.System,
                            "Success. Imported Color Palette from " + open.FileName + ".");
                        open.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error, @"Error importing Color Palette. Error: " + ex.Message);
                    open.Dispose();
                }
            }
        }

        private void ExportColorMappings()
        {
            var save = new SaveFileDialog();
            save.AddExtension = true;
            save.AutoUpgradeEnabled = true;
            save.CheckFileExists = false;
            save.CheckPathExists = true;
            save.CreatePrompt = false;
            save.DefaultExt = "chromatics";
            save.DereferenceLinks = true;
            save.FileName = "mypalette";
            save.Filter = "Chromatics Palette Files|*.chromatics";
            save.FilterIndex = 1;
            save.InitialDirectory = "";
            save.OverwritePrompt = true;
            save.RestoreDirectory = false;
            save.ShowHelp = false;
            save.SupportMultiDottedExtensions = false;
            save.Title = "Export Color Palette";
            save.ValidateNames = true;


            if (save.ShowDialog() == DialogResult.OK)
            {
                SaveColorMappings(0);

                WriteConsole(ConsoleTypes.System, @"Exporting Color Palette..");

                var colorMappings = new FfxivColorMappings();
                colorMappings = ColorMappings;

                try
                {
                    using (var sw = new StreamWriter(save.FileName, false))
                    {
                        var x = new XmlSerializer(colorMappings.GetType());
                        x.Serialize(sw, colorMappings);
                        sw.WriteLine();
                        sw.Close();
                    }

                    WriteConsole(ConsoleTypes.System, @"Success. Exported Color Palette to " + save.FileName + ".");
                    save.Dispose();
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error, @"Error exporting Color Palette. Error: " + ex.Message);
                }
            }
        }
    }
}