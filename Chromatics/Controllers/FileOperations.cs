using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;
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

            dr.DeviceOperation_RazerKeyboard = RazerDeviceKeyboard;
            dr.DeviceOperation_RazerMouse = RazerDeviceMouse;
            dr.DeviceOperation_RazerMousepad = RazerDeviceMousepad;
            dr.DeviceOperation_RazerKeypad = RazerDeviceKeypad;
            dr.DeviceOperation_RazerHeadset = RazerDeviceHeadset;

            dr.DeviceOperation_CorsairKeyboard = CorsairDeviceKeyboard;
            dr.DeviceOperation_CorsairMouse = CorsairDeviceMouse;
            dr.DeviceOperation_CorsairMousepad = CorsairDeviceMousepad;
            dr.DeviceOperation_CorsairKeypad = CorsairDeviceKeypad; //Not Implemented
            dr.DeviceOperation_CorsairHeadset = CorsairDeviceHeadset;

            dr.DeviceOperation_MouseToggle = MouseToggle;
            
            dr.DeviceOperation_LogitechKeyboard = LogitechDeviceKeyboard;

            var _LifxLoad = "";
            var _HueLoad = "";

            if (LifxSDK && _lifx.LifxModeMemory.Count() > 0)
            {
                var _LifxList = new List<string>();
                foreach (var _LP in _lifx.LifxModeMemory)
                {
                    var _LA = _LP.Key + "|" + _LP.Value + "|" + _lifx.LifxStateMemory[_LP.Key];
                    _LifxList.Add(_LA);
                }

                var _lifxEx = _LifxList.ToArray();
                _LifxLoad = string.Join(",", _lifxEx);
            }

            
            if (HueSDK && _hue.HueModeMemory.Count() > 0)
            {
                List<string> _HueList = new List<string>();
                foreach (KeyValuePair<string, DeviceModeTypes> _HP in _hue.HueModeMemory)
                {
                    string _HA = _HP.Key + "|" + _HP.Value + "|" + _hue.HueStateMemory[_HP.Key];
                    _HueList.Add(_HA);
                }

                string[] _HueEx = _HueList.ToArray();
                _HueLoad = string.Join(",", _HueEx);
            }
            

            dr.DeviceOperation_HUEDefault = HUEDefault;
            dr.DeviceOperation_LifxDevices = _LifxLoad;
            dr.DeviceOperation_HueDevices = _HueLoad;

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
                WriteConsole(ConsoleTypes.ERROR, "Error saving states to devices.chromatics. Error: " + ex.Message);
            }
        }

        private void LoadDevices()
        {
            WriteConsole(ConsoleTypes.SYSTEM, "Searching for devices.chromatics..");
            var ds = new DeviceDataStore();
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/devices.chromatics";

            if (File.Exists(path))
            {
                //Read Device Save
                WriteConsole(ConsoleTypes.SYSTEM, "Attempting to load devices.chromatics..");
                using (var sr = new StreamReader(path))
                {
                    try
                    {
                        var reader = new XmlSerializer(ds.GetType());
                        var dr = (DeviceDataStore) reader.Deserialize(sr);
                        sr.Close();

                        RazerDeviceKeyboard = dr.DeviceOperation_RazerKeyboard;
                        RazerDeviceMouse = dr.DeviceOperation_RazerMouse;
                        RazerDeviceMousepad = dr.DeviceOperation_RazerMousepad;
                        RazerDeviceKeypad = dr.DeviceOperation_RazerKeypad;
                        RazerDeviceHeadset = dr.DeviceOperation_RazerHeadset;

                        CorsairDeviceKeyboard = dr.DeviceOperation_CorsairKeyboard;
                        CorsairDeviceMouse = dr.DeviceOperation_CorsairMouse;
                        CorsairDeviceMousepad = dr.DeviceOperation_CorsairMousepad;
                        CorsairDeviceKeypad = dr.DeviceOperation_CorsairKeypad; //Not Implemented
                        CorsairDeviceHeadset = dr.DeviceOperation_CorsairHeadset;

                        MouseToggle = dr.DeviceOperation_MouseToggle;
                        
                        LogitechDeviceKeyboard = dr.DeviceOperation_LogitechKeyboard;
                        HUEDefault = dr.DeviceOperation_HUEDefault;

                        var _LifxLoad = dr.DeviceOperation_LifxDevices;
                        if (_LifxLoad != "")
                        {
                            var _LifxDevices = _LifxLoad.Split(',');
                            foreach (var _LD in _LifxDevices)
                            {
                                var LState = _LD.Split('|');

                                //DeviceModeTypes LMode = DeviceModeTypes.DISABLED;
                                //LMode = LState[1].ToString();
                                //int.TryParse(LState[1], out LMode);
                                DeviceModeTypes LMode = (DeviceModeTypes)Enum.Parse(typeof(DeviceModeTypes), LState[1]);

                                var LEnabled = 0;
                                int.TryParse(LState[2], out LEnabled);
                                _lifx.LifxModeMemory.Add(LState[0], LMode);
                                _lifx.LifxStateMemory.Add(LState[0], LEnabled);
                            }
                        }

                        var _HueLoad = dr.DeviceOperation_HueDevices;
                        if (_HueLoad != "")
                        {
                            var _HueDevices = _HueLoad.Split(',');
                            foreach (var _HD in _HueDevices)
                            {
                                var HState = _HD.Split('|');
                                //var HMode = 0;
                                //int.TryParse(HState[1], out HMode);
                                DeviceModeTypes HMode = (DeviceModeTypes)Enum.Parse(typeof(DeviceModeTypes), HState[1]);
                                var HEnabled = 0;
                                int.TryParse(HState[2], out HEnabled);
                                _hue.HueModeMemory.Add(HState[0], HMode);
                                _hue.HueStateMemory.Add(HState[0], HEnabled);
                                //HueModeMemory.Add(HState[0], HMode, HEnabled);
                            }
                        }

                        WriteConsole(ConsoleTypes.SYSTEM, "devices.chromatics loaded.");
                    }
                    catch (Exception ex)
                    {
                        WriteConsole(ConsoleTypes.ERROR, "Error loading devices.chromatics. Error: " + ex.Message);
                    }
                }
            }
            else
            {
                //Create Device Save
                WriteConsole(ConsoleTypes.SYSTEM, "devices.chromatics not found. Creating one..");
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
                    WriteConsole(ConsoleTypes.ERROR, "Error creating devices.chromatics. Error: " + ex.Message);
                }
            }
        }

        private void SaveColorMappings(int report)
        {
            //if (report == 1)
            //    WriteConsole(ConsoleTypes.SYSTEM, "Saving states to mappings.chromatics..");
            var _ColorMappings = new FFXIVColorMappings();
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/mappings.chromatics";

            _ColorMappings = ColorMappings;

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var x = new XmlSerializer(_ColorMappings.GetType());
                    x.Serialize(sw, _ColorMappings);
                    sw.WriteLine();
                    sw.Close();
                }

               // if (report == 1)
               //     WriteConsole(ConsoleTypes.SYSTEM, "Saved states to mappings.chromatics.");
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.ERROR, "Error saving states to mappings.chromatics. Error: " + ex.Message);
            }
        }

        private void LoadColorMappings()
        {
            WriteConsole(ConsoleTypes.SYSTEM, "Searching for mappings.chromatics..");
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/mappings.chromatics";

            if (File.Exists(path))
            {
                //Read Device Save
                WriteConsole(ConsoleTypes.SYSTEM, "Attempting to load mappings.chromatics..");
                using (var sr = new StreamReader(path))
                {
                    try
                    {
                        var reader = new XmlSerializer(ColorMappings.GetType());
                        var _ColorMappings = (FFXIVColorMappings) reader.Deserialize(sr);
                        sr.Close();

                        ColorMappings = _ColorMappings;

                        WriteConsole(ConsoleTypes.SYSTEM, "mappings.chromatics loaded.");
                    }
                    catch (Exception ex)
                    {
                        WriteConsole(ConsoleTypes.ERROR, "Error loading mappings.chromatics. Error: " + ex.Message);
                    }
                }
            }
            else
            {
                //Create Device Save
                WriteConsole(ConsoleTypes.SYSTEM, "mappings.chromatics not found. Creating one..");
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
                    WriteConsole(ConsoleTypes.ERROR, "Error creating mappings.chromatics. Error: " + ex.Message);
                }
            }
        }

        private void SaveChromaticsSettings(int report)
        {
            //if (report == 1)
            //    WriteConsole(ConsoleTypes.SYSTEM, "Saving states to settings.chromatics..");
            var _ChromaticsSettings = new ChromaticsSettings();
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/settings.chromatics";

            _ChromaticsSettings = ChromaticsSettings;
            
            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var x = new XmlSerializer(_ChromaticsSettings.GetType());
                    x.Serialize(sw, _ChromaticsSettings);
                    sw.WriteLine();
                    sw.Close();
                }

                //if (report == 1)
                //    WriteConsole(ConsoleTypes.SYSTEM, "Saved states to settings.chromatics.");
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.ERROR, "Error saving states to settings.chromatics. Error: " + ex.Message);
            }
        }

        private void LoadChromaticsSettings()
        {
            WriteConsole(ConsoleTypes.SYSTEM, "Searching for settings.chromatics..");
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/settings.chromatics";

            if (File.Exists(path))
            {
                //Read Device Save
                WriteConsole(ConsoleTypes.SYSTEM, "Attempting to load settings.chromatics..");
                using (var sr = new StreamReader(path))
                {
                    try
                    {
                        var reader = new XmlSerializer(ChromaticsSettings.GetType());
                        var _ChromaticsSettings = (ChromaticsSettings)reader.Deserialize(sr);
                        sr.Close();

                        ChromaticsSettings = _ChromaticsSettings;

                        WriteConsole(ConsoleTypes.SYSTEM, "settings.chromatics loaded.");
                    }
                    catch (Exception ex)
                    {
                        WriteConsole(ConsoleTypes.ERROR, "Error loading settings.chromatics. Error: " + ex.Message);
                    }
                }
            }
            else
            {
                //Create Device Save
                WriteConsole(ConsoleTypes.SYSTEM, "settings.chromatics not found. Creating one..");
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
                    WriteConsole(ConsoleTypes.ERROR, "Error creating settings.chromatics. Error: " + ex.Message);
                }
            }
        }

        private void ImportColorMappings()
        {
            var _open = new OpenFileDialog();
            _open.Filter = "Chromatics Palette Files|*.chromatics";
            _open.Title = "Import Color Palette";
            _open.AddExtension = true;
            _open.AutoUpgradeEnabled = true;
            _open.CheckFileExists = true;
            _open.CheckPathExists = true;
            _open.DefaultExt = "chromatics";
            _open.DereferenceLinks = true;
            _open.FileName = "mypalette";
            _open.FilterIndex = 1;
            _open.Multiselect = false;
            _open.ReadOnlyChecked = false;
            _open.RestoreDirectory = false;
            _open.ShowHelp = false;
            _open.ShowReadOnly = false;
            _open.SupportMultiDottedExtensions = false;
            _open.ValidateNames = true;

            if (_open.ShowDialog() == DialogResult.OK)
            {
                WriteConsole(ConsoleTypes.SYSTEM, "Importing Color Palette..");

                try
                {
                    using (var sr = new StreamReader(_open.FileName))
                    {
                        var reader = new XmlSerializer(ColorMappings.GetType());
                        var _ColorMappings = (FFXIVColorMappings) reader.Deserialize(sr);
                        sr.Close();

                        ColorMappings = _ColorMappings;

                        WriteConsole(ConsoleTypes.SYSTEM, "Success. Imported Color Palette from " + _open.FileName + ".");
                        _open.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.ERROR, "Error importing Color Palette. Error: " + ex.Message);
                    _open.Dispose();
                }
            }
        }

        private void ExportColorMappings()
        {
            var _save = new SaveFileDialog();
            _save.AddExtension = true;
            _save.AutoUpgradeEnabled = true;
            _save.CheckFileExists = false;
            _save.CheckPathExists = true;
            _save.CreatePrompt = false;
            _save.DefaultExt = "chromatics";
            _save.DereferenceLinks = true;
            _save.FileName = "mypalette";
            _save.Filter = "Chromatics Palette Files|*.chromatics";
            _save.FilterIndex = 1;
            _save.InitialDirectory = "";
            _save.OverwritePrompt = true;
            _save.RestoreDirectory = false;
            _save.ShowHelp = false;
            _save.SupportMultiDottedExtensions = false;
            _save.Title = "Export Color Palette";
            _save.ValidateNames = true;


            if (_save.ShowDialog() == DialogResult.OK)
            {
                SaveColorMappings(0);

                WriteConsole(ConsoleTypes.SYSTEM, "Exporting Color Palette..");

                var _ColorMappings = new FFXIVColorMappings();
                _ColorMappings = ColorMappings;

                try
                {
                    using (var sw = new StreamWriter(_save.FileName, false))
                    {
                        var x = new XmlSerializer(_ColorMappings.GetType());
                        x.Serialize(sw, _ColorMappings);
                        sw.WriteLine();
                        sw.Close();
                    }

                    WriteConsole(ConsoleTypes.SYSTEM, "Success. Exported Color Palette to " + _save.FileName + ".");
                    _save.Dispose();
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.ERROR, "Error exporting Color Palette. Error: " + ex.Message);
                }
            }
        }
    }
}