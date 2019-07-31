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

            dr.ChromaLinkLEDCountZ1 = _ChromaLinkLEDCountZ1;
            dr.ChromaLinkLEDCountZ2 = _ChromaLinkLEDCountZ2;
            dr.ChromaLinkLEDCountZ3 = _ChromaLinkLEDCountZ3;
            dr.ChromaLinkLEDCountZ4 = _ChromaLinkLEDCountZ4;
            dr.ChromaLinkLEDCountZ5 = _ChromaLinkLEDCountZ5;
            dr.ChromaLinkLEDCountZ6 = _ChromaLinkLEDCountZ6;

            dr.SDKRazer = _SDKRazer;
            dr.SDKLogitech = _SDKLogitech;
            dr.SDKCorsair = _SDKCorsair;
            dr.SDKCooler = _SDKCooler;
            dr.SDKSteelSeries = _SDKSteelSeries;
            dr.SDKWooting = _SDKWooting;
            dr.SDKAsus = _SDKAsus;
            dr.SDKMystic = _SDKMystic;
            dr.SDKLifx = _SDKLifx;

            dr.KeysSingleKeyModeEnabled = _KeysSingleKeyModeEnabled;
            dr.KeySingleKeyMode = Helpers.ConvertDevModeToString(_KeysSingleKeyMode);

            dr.KeysMultiKeyModeEnabled = _KeysMultiKeyModeEnabled;
            dr.KeyMultiKeyMode = Helpers.ConvertDevMultiModeToString(_KeysMultiKeyMode);
            dr.KeyMultiLEDCount = _KeyMultiLEDCount;
            dr.KeyMultiReverse = _KeyMultiReverse;

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
            dr.CLZone6Mode = Helpers.ConvertDevModeToString(_CLZone6Mode);

            dr.EnableKeypadBinds = _EnableKeypadBinds;
            dr.KZ1Enabled = _KZ1Enabled;
            dr.KZ2Enabled = _KZ2Enabled;
            dr.KZ3Enabled = _KZ3Enabled;
            dr.KZ4Enabled = _KZ4Enabled;
            dr.KZ5Enabled = _KZ5Enabled;
            dr.KZ6Enabled = _KZ6Enabled;
            dr.KZ7Enabled = _KZ7Enabled;
            dr.KZ8Enabled = _KZ8Enabled;
            dr.KZ9Enabled = _KZ9Enabled;
            dr.KZ10Enabled = _KZ10Enabled;
            dr.KZ11Enabled = _KZ11Enabled;
            dr.KZ12Enabled = _KZ12Enabled;
            dr.KZ13Enabled = _KZ13Enabled;
            dr.KZ14Enabled = _KZ14Enabled;
            dr.KZ15Enabled = _KZ15Enabled;
            dr.KZ16Enabled = _KZ16Enabled;
            dr.KZ17Enabled = _KZ17Enabled;
            dr.KZ18Enabled = _KZ18Enabled;
            dr.KZ19Enabled = _KZ19Enabled;
            dr.KZ20Enabled = _KZ20Enabled;

            if (_KeyBindMap.ContainsKey(1))
                dr.KeypadZ1Bind = _KeyBindMap[1];
            if (_KeyBindMap.ContainsKey(2))
                dr.KeypadZ2Bind = _KeyBindMap[2];
            if (_KeyBindMap.ContainsKey(3))
                dr.KeypadZ3Bind = _KeyBindMap[3];
            if (_KeyBindMap.ContainsKey(4))
                dr.KeypadZ4Bind = _KeyBindMap[4];
            if (_KeyBindMap.ContainsKey(5)) 
                dr.KeypadZ5Bind = _KeyBindMap[5];
            if (_KeyBindMap.ContainsKey(6))
                dr.KeypadZ6Bind = _KeyBindMap[6];
            if (_KeyBindMap.ContainsKey(7))
                dr.KeypadZ7Bind = _KeyBindMap[7];
            if (_KeyBindMap.ContainsKey(8))
                dr.KeypadZ8Bind = _KeyBindMap[8];
            if (_KeyBindMap.ContainsKey(9))
                dr.KeypadZ9Bind = _KeyBindMap[9];
            if (_KeyBindMap.ContainsKey(10))
                dr.KeypadZ10Bind = _KeyBindMap[10];
            if (_KeyBindMap.ContainsKey(11))
                dr.KeypadZ11Bind = _KeyBindMap[11];
            if (_KeyBindMap.ContainsKey(12))
                dr.KeypadZ12Bind = _KeyBindMap[12];
            if (_KeyBindMap.ContainsKey(13))
                dr.KeypadZ13Bind = _KeyBindMap[13];
            if (_KeyBindMap.ContainsKey(14))
                dr.KeypadZ14Bind = _KeyBindMap[14];
            if (_KeyBindMap.ContainsKey(15))
                dr.KeypadZ15Bind = _KeyBindMap[15];
            if (_KeyBindMap.ContainsKey(16))
                dr.KeypadZ16Bind = _KeyBindMap[16];
            if (_KeyBindMap.ContainsKey(17))
                dr.KeypadZ17Bind = _KeyBindMap[17];
            if (_KeyBindMap.ContainsKey(18))
                dr.KeypadZ18Bind = _KeyBindMap[18];
            if (_KeyBindMap.ContainsKey(19))
                dr.KeypadZ19Bind = _KeyBindMap[19];
            if (_KeyBindMap.ContainsKey(20))
                dr.KeypadZ20Bind = _KeyBindMap[20];

            if (_KeyBindModMap.ContainsKey(1))
                dr.kz1Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[1]);
            if (_KeyBindModMap.ContainsKey(2))
                dr.kz2Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[2]);
            if (_KeyBindModMap.ContainsKey(3))
                dr.kz3Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[3]);
            if (_KeyBindModMap.ContainsKey(4))
                dr.kz4Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[4]);
            if (_KeyBindModMap.ContainsKey(5))
                dr.kz5Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[5]);
            if (_KeyBindModMap.ContainsKey(6))
                dr.kz6Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[6]);
            if (_KeyBindModMap.ContainsKey(7))
                dr.kz7Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[7]);
            if (_KeyBindModMap.ContainsKey(8))
                dr.kz8Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[8]);
            if (_KeyBindModMap.ContainsKey(9))
                dr.kz9Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[9]);
            if (_KeyBindModMap.ContainsKey(10))
                dr.kz10Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[10]);
            if (_KeyBindModMap.ContainsKey(11))
                dr.kz11Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[11]);
            if (_KeyBindModMap.ContainsKey(12))
                dr.kz12Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[12]);
            if (_KeyBindModMap.ContainsKey(13))
                dr.kz13Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[13]);
            if (_KeyBindModMap.ContainsKey(14))
                dr.kz14Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[14]);
            if (_KeyBindModMap.ContainsKey(15))
                dr.kz15Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[15]);
            if (_KeyBindModMap.ContainsKey(16))
                dr.kz16Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[16]);
            if (_KeyBindModMap.ContainsKey(17))
                dr.kz17Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[17]);
            if (_KeyBindModMap.ContainsKey(18))
                dr.kz18Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[18]);
            if (_KeyBindModMap.ContainsKey(19))
                dr.kz19Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[19]);
            if (_KeyBindModMap.ContainsKey(20))
                dr.kz20Modifiers = Helpers.ConvertModifiersToString(_KeyBindModMap[20]);

            dr.OtherInterpolateEffects = _OtherInterpolateEffects;
            dr.ReverseInterpolateEffects = _ReverseInterpolateEffects;

            dr.HeadsetZone1Mode = Helpers.ConvertDevModeToString(_HeadsetZone1Mode);
            dr.KeypadZone1Mode = Helpers.ConvertDevMultiModeToString(_KeypadZone1Mode);

            dr.LightbarMode = Helpers.ConvertLightbarModeToString(_LightbarMode);
            dr.FKeyMode = Helpers.ConvertFKeyModeToString(_FKeyMode);

            var lifxLoad = "";

            if (LifxSdk && _lifx.LifxModeMemory.Any())
            {
                var lifxEx = _lifx.LifxModeMemory.Select(lp => lp.Key + "|" + lp.Value + "|" + _lifx.LifxStateMemory[lp.Key]).ToArray();
                lifxLoad = string.Join(",", lifxEx);
            }
            
            dr.DeviceOperationLifxDevices = lifxLoad;

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

                        _deviceKeyboard = dr.DeviceOperationKeyboard;
                        _deviceMouse = dr.DeviceOperationMouse;
                        _deviceMousepad = dr.DeviceOperationMousepad;
                        _deviceHeadset = dr.DeviceOperationHeadset;
                        _deviceKeypad = dr.DeviceOperationKeypad;
                        _deviceCL = dr.DeviceOperationCL;

                        _ChromaLinkLEDCountZ1 = dr.ChromaLinkLEDCountZ1;
                        _ChromaLinkLEDCountZ2 = dr.ChromaLinkLEDCountZ2;
                        _ChromaLinkLEDCountZ3 = dr.ChromaLinkLEDCountZ3;
                        _ChromaLinkLEDCountZ4 = dr.ChromaLinkLEDCountZ4;
                        _ChromaLinkLEDCountZ5 = dr.ChromaLinkLEDCountZ5;
                        _ChromaLinkLEDCountZ6 = dr.ChromaLinkLEDCountZ6;

                        _OtherInterpolateEffects = dr.OtherInterpolateEffects;
                        _ReverseInterpolateEffects = dr.ReverseInterpolateEffects;

                        _SDKRazer = dr.SDKRazer;
                        _SDKLogitech = dr.SDKLogitech;
                        _SDKCorsair = dr.SDKCorsair;
                        _SDKCooler = dr.SDKCooler;
                        _SDKSteelSeries = dr.SDKSteelSeries;
                        _SDKWooting = dr.SDKWooting;
                        _SDKAsus = dr.SDKAsus;
                        _SDKMystic = dr.SDKMystic;
                        _SDKLifx = dr.SDKLifx;
                        
                        _KeysSingleKeyModeEnabled = dr.KeysSingleKeyModeEnabled;
                        _KeysSingleKeyMode = Helpers.ConvertStringToDevMode(dr.KeySingleKeyMode);

                        _KeysMultiKeyModeEnabled = dr.KeysMultiKeyModeEnabled;
                        _KeysMultiKeyMode = Helpers.ConvertStringToDevMultiMode(dr.KeyMultiKeyMode);
                        _KeyMultiLEDCount = dr.KeyMultiLEDCount;
                        _KeyMultiReverse = dr.KeyMultiReverse;

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
                        _CLZone6Mode = Helpers.ConvertStringToDevMode(dr.CLZone6Mode);

                        _EnableKeypadBinds = dr.EnableKeypadBinds;
                        _KZ1Enabled = dr.KZ1Enabled;
                        _KZ2Enabled = dr.KZ2Enabled;
                        _KZ3Enabled = dr.KZ3Enabled;
                        _KZ4Enabled = dr.KZ4Enabled;
                        _KZ5Enabled = dr.KZ5Enabled;
                        _KZ6Enabled = dr.KZ6Enabled;
                        _KZ7Enabled = dr.KZ7Enabled;
                        _KZ8Enabled = dr.KZ8Enabled;
                        _KZ9Enabled = dr.KZ9Enabled;
                        _KZ10Enabled = dr.KZ10Enabled;
                        _KZ11Enabled = dr.KZ11Enabled;
                        _KZ12Enabled = dr.KZ12Enabled;
                        _KZ13Enabled = dr.KZ13Enabled;
                        _KZ14Enabled = dr.KZ14Enabled;
                        _KZ15Enabled = dr.KZ15Enabled;
                        _KZ16Enabled = dr.KZ16Enabled;
                        _KZ17Enabled = dr.KZ17Enabled;
                        _KZ18Enabled = dr.KZ18Enabled;
                        _KZ19Enabled = dr.KZ19Enabled;
                        _KZ20Enabled = dr.KZ20Enabled;

                        if (!_KeyBindModMap.ContainsKey(1))
                            _KeyBindModMap.Add(1, Helpers.ConvertStringToModifiers(dr.kz1Modifiers));

                        if (!_KeyBindModMap.ContainsKey(2))
                            _KeyBindModMap.Add(2, Helpers.ConvertStringToModifiers(dr.kz2Modifiers));

                        if (!_KeyBindModMap.ContainsKey(3))
                            _KeyBindModMap.Add(3, Helpers.ConvertStringToModifiers(dr.kz3Modifiers));

                        if (!_KeyBindModMap.ContainsKey(4))
                            _KeyBindModMap.Add(4, Helpers.ConvertStringToModifiers(dr.kz4Modifiers));

                        if (!_KeyBindModMap.ContainsKey(5))
                            _KeyBindModMap.Add(5, Helpers.ConvertStringToModifiers(dr.kz5Modifiers));

                        if (!_KeyBindModMap.ContainsKey(6))
                            _KeyBindModMap.Add(6, Helpers.ConvertStringToModifiers(dr.kz6Modifiers));

                        if (!_KeyBindModMap.ContainsKey(7))
                            _KeyBindModMap.Add(7, Helpers.ConvertStringToModifiers(dr.kz7Modifiers));

                        if (!_KeyBindModMap.ContainsKey(8))
                            _KeyBindModMap.Add(8, Helpers.ConvertStringToModifiers(dr.kz8Modifiers));

                        if (!_KeyBindModMap.ContainsKey(9))
                            _KeyBindModMap.Add(9, Helpers.ConvertStringToModifiers(dr.kz9Modifiers));

                        if (!_KeyBindModMap.ContainsKey(10))
                            _KeyBindModMap.Add(10, Helpers.ConvertStringToModifiers(dr.kz10Modifiers));

                        if (!_KeyBindModMap.ContainsKey(11))
                            _KeyBindModMap.Add(11, Helpers.ConvertStringToModifiers(dr.kz11Modifiers));

                        if (!_KeyBindModMap.ContainsKey(12))
                            _KeyBindModMap.Add(12, Helpers.ConvertStringToModifiers(dr.kz12Modifiers));

                        if (!_KeyBindModMap.ContainsKey(13))
                            _KeyBindModMap.Add(13, Helpers.ConvertStringToModifiers(dr.kz13Modifiers));

                        if (!_KeyBindModMap.ContainsKey(14))
                            _KeyBindModMap.Add(14, Helpers.ConvertStringToModifiers(dr.kz14Modifiers));

                        if (!_KeyBindModMap.ContainsKey(15))
                            _KeyBindModMap.Add(15, Helpers.ConvertStringToModifiers(dr.kz15Modifiers));

                        if (!_KeyBindModMap.ContainsKey(16))
                            _KeyBindModMap.Add(16, Helpers.ConvertStringToModifiers(dr.kz16Modifiers));

                        if (!_KeyBindModMap.ContainsKey(17))
                            _KeyBindModMap.Add(17, Helpers.ConvertStringToModifiers(dr.kz17Modifiers));

                        if (!_KeyBindModMap.ContainsKey(18))
                            _KeyBindModMap.Add(18, Helpers.ConvertStringToModifiers(dr.kz18Modifiers));

                        if (!_KeyBindModMap.ContainsKey(19))
                            _KeyBindModMap.Add(19, Helpers.ConvertStringToModifiers(dr.kz19Modifiers));

                        if (!_KeyBindModMap.ContainsKey(20))
                            _KeyBindModMap.Add(20, Helpers.ConvertStringToModifiers(dr.kz20Modifiers));

                        if (!_KeyBindMap.ContainsKey(1))
                            _KeyBindMap.Add(1, dr.KeypadZ1Bind);

                        if (!_KeyBindMap.ContainsKey(2))
                            _KeyBindMap.Add(2, dr.KeypadZ2Bind);

                        if (!_KeyBindMap.ContainsKey(3))
                            _KeyBindMap.Add(3, dr.KeypadZ3Bind);

                        if (!_KeyBindMap.ContainsKey(4))
                            _KeyBindMap.Add(4, dr.KeypadZ4Bind);

                        if (!_KeyBindMap.ContainsKey(5))
                            _KeyBindMap.Add(5, dr.KeypadZ5Bind);

                        if (!_KeyBindMap.ContainsKey(6))
                            _KeyBindMap.Add(6, dr.KeypadZ6Bind);

                        if (!_KeyBindMap.ContainsKey(7))
                            _KeyBindMap.Add(7, dr.KeypadZ7Bind);

                        if (!_KeyBindMap.ContainsKey(8))
                            _KeyBindMap.Add(8, dr.KeypadZ8Bind);

                        if (!_KeyBindMap.ContainsKey(9))
                            _KeyBindMap.Add(9, dr.KeypadZ9Bind);

                        if (!_KeyBindMap.ContainsKey(10))
                            _KeyBindMap.Add(10, dr.KeypadZ10Bind);

                        if (!_KeyBindMap.ContainsKey(11))
                            _KeyBindMap.Add(11, dr.KeypadZ11Bind);

                        if (!_KeyBindMap.ContainsKey(12))
                            _KeyBindMap.Add(12, dr.KeypadZ12Bind);

                        if (!_KeyBindMap.ContainsKey(13))
                            _KeyBindMap.Add(13, dr.KeypadZ13Bind);

                        if (!_KeyBindMap.ContainsKey(14))
                            _KeyBindMap.Add(14, dr.KeypadZ14Bind);

                        if (!_KeyBindMap.ContainsKey(15))
                            _KeyBindMap.Add(15, dr.KeypadZ15Bind);

                        if (!_KeyBindMap.ContainsKey(16))
                            _KeyBindMap.Add(16, dr.KeypadZ16Bind);

                        if (!_KeyBindMap.ContainsKey(17))
                            _KeyBindMap.Add(17, dr.KeypadZ17Bind);

                        if (!_KeyBindMap.ContainsKey(18))
                            _KeyBindMap.Add(18, dr.KeypadZ18Bind);

                        if (!_KeyBindMap.ContainsKey(19))
                            _KeyBindMap.Add(19, dr.KeypadZ19Bind);

                        if (!_KeyBindMap.ContainsKey(20))
                            _KeyBindMap.Add(20, dr.KeypadZ20Bind);


                        _HeadsetZone1Mode = Helpers.ConvertStringToDevMode(dr.HeadsetZone1Mode);
                        _KeypadZone1Mode = Helpers.ConvertStringToDevMultiMode(dr.KeypadZone1Mode);

                        _LightbarMode = Helpers.ConvertStringToLightbarMode(dr.LightbarMode);
                        _FKeyMode = Helpers.ConvertStringToFKeyMode(dr.FKeyMode);

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

                                if (_lifx.LifxModeMemory.ContainsKey(lState[0]))
                                {
                                    _lifx.LifxModeMemory[lState[0]] = lMode;
                                }
                                else
                                {
                                    _lifx.LifxModeMemory.Add(lState[0], lMode);
                                }

                                if (_lifx.LifxStateMemory.ContainsKey(lState[0]))
                                {
                                    _lifx.LifxStateMemory[lState[0]] = lEnabled;
                                }
                                else
                                {
                                    _lifx.LifxStateMemory.Add(lState[0], lEnabled);
                                }
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

        private void ExportDebugLog(string log)
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var SettingsPath = enviroment + @"/settings.chromatics";
            var DevicesPath = enviroment + @"/devices.chromatics";
            var MappingsPath = enviroment + @"/mappings.chromatics";
            var ACTPath = enviroment + @"/actdata.chromatics";

            var datenow = DateTime.UtcNow;

            var save = new SaveFileDialog();
            save.AddExtension = true;
            save.AutoUpgradeEnabled = true;
            save.CheckFileExists = false;
            save.CheckPathExists = true;
            save.CreatePrompt = false;
            save.DefaultExt = "txt";
            save.DereferenceLinks = true;
            save.FileName = "chromaticsdebug_" + datenow.Hour + datenow.Minute + datenow.Second + datenow.Day + datenow.Month + datenow.Year;
            save.Filter = "Chromatics Debug Log|*.txt";
            save.FilterIndex = 1;
            save.InitialDirectory = "";
            save.OverwritePrompt = true;
            save.RestoreDirectory = false;
            save.ShowHelp = false;
            save.SupportMultiDottedExtensions = false;
            save.Title = "Export Debug Log";
            save.ValidateNames = true;
            

            if (save.ShowDialog() == DialogResult.OK)
            {
                WriteConsole(ConsoleTypes.System, @"Exporting Debug Log..");


                try
                {
                    using (var sw = new StreamWriter(save.FileName, false))
                    {
                        sw.Write("======DEBUG LOG=====");
                        sw.WriteLine();
                        sw.Write("Exported Time: " + datenow.Hour + ":" + datenow.Minute + ":" + datenow.Second + " " + datenow.Day + "/" + datenow.Month + "/" + datenow.Year);
                        sw.WriteLine();
                        sw.Write("Player Job: " + DebugJobClass);
                        sw.WriteLine();
                        sw.Write("Player Location: " + DebugMapID);
                        sw.WriteLine();
                        sw.Write("Player Instance: " + DebugInstanceID);
                        sw.WriteLine();
                        sw.WriteLine();
                        sw.WriteLine();
                        sw.Write(log);
                        sw.WriteLine();
                        sw.WriteLine();
                        sw.WriteLine();

                        sw.Write("======CHROMATICS SETTINGS=====");
                        sw.WriteLine();
                        sw.WriteLine();
                        
                        if (File.Exists(SettingsPath))
                        {
                            //Read Device Save
                            using (var sr = new StreamReader(SettingsPath))
                            {
                                try
                                {
                                    sw.Write(sr.ReadToEnd());
                                    sr.Close();
                                    sw.WriteLine();
                                }
                                catch (Exception ex)
                                {
                                    sw.Write("Exception occurred while reading settings.chromatics. Exception: " +
                                             ex.Message);
                                }
                            }
                        }
                        else
                        {
                            sw.Write("settings.chromatics does not exist or is missing from Chromatics directory.");
                        }

                        sw.WriteLine();
                        sw.WriteLine();


                        sw.Write("======CHROMATICS DEVICES=====");
                        sw.WriteLine();
                        sw.WriteLine();
                        
                        if (File.Exists(DevicesPath))
                        {
                            //Read Device Save
                            using (var sr = new StreamReader(DevicesPath))
                            {
                                try
                                {
                                    sw.Write(sr.ReadToEnd());
                                    sr.Close();
                                    sw.WriteLine();
                                }
                                catch (Exception ex)
                                {
                                    sw.Write("Exception occurred while reading devices.chromatics. Exception: " +
                                             ex.Message);
                                }
                            }
                        }
                        else
                        {
                            sw.Write("devices.chromatics does not exist or is missing from Chromatics directory.");
                        }

                        sw.WriteLine();
                        sw.WriteLine();


                        sw.Write("======CHROMATICS MAPPINGS=====");
                        sw.WriteLine();
                        sw.WriteLine();
                        
                        if (File.Exists(MappingsPath))
                        {
                            //Read Device Save
                            using (var sr = new StreamReader(MappingsPath))
                            {
                                try
                                {
                                    sw.Write(sr.ReadToEnd());
                                    sr.Close();
                                    sw.WriteLine();
                                }
                                catch (Exception ex)
                                {
                                    sw.Write("Exception occurred while reading mappings.chromatics. Exception: " +
                                             ex.Message);
                                }
                            }
                        }
                        else
                        {
                            sw.Write("mappings.chromatics does not exist or is missing from Chromatics directory.");
                        }

                        sw.WriteLine();
                        sw.WriteLine();

                        sw.Write("======CHROMATICS ACTDATA=====");
                        sw.WriteLine();
                        sw.WriteLine();
                        
                        if (File.Exists(ACTPath))
                        {
                            //Read Device Save
                            using (var sr = new StreamReader(ACTPath))
                            {
                                try
                                {
                                    sw.Write(sr.ReadToEnd());
                                    sr.Close();
                                    sw.WriteLine();
                                }
                                catch (Exception ex)
                                {
                                    sw.Write("Exception occurred while reading actdata.chromatics. Exception: " +
                                             ex.Message);
                                }
                            }
                        }
                        else
                        {
                            sw.Write("actdata.chromatics does not exist or is missing from Chromatics directory.");
                        }

                        sw.WriteLine();
                        sw.WriteLine();


                        sw.WriteLine();
                        sw.Close();
                    }

                    WriteConsole(ConsoleTypes.System, @"Success. Exported Debug Log to " + save.FileName + ".");
                    save.Dispose();
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error, @"Error exporting Debug Log. Error: " + ex.Message);
                }
            }
        }
    }
}