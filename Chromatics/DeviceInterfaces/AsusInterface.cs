using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;
using GalaSoft.MvvmLight.Ioc;
using AuraServiceLib;

/* Contains all Asus SDK code for detection, initialization, states and effects.
 * 
 * 
 */
namespace Chromatics.DeviceInterfaces
{
    public class AsusInterface
    {
        private static ILogWrite _write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static Asus InitializeAsusSdk()
        {
            Asus Asus = null;
            var programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            var programFilesX86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");

            if (File.Exists(programFiles + @"\ASUS\AuraSDK\AuraSdk_x64.dll") || File.Exists(programFilesX86 + @"\ASUS\AuraSDK\AuraSdk_x64.dll"))
            {
                Asus = new Asus();
                var result = Asus.InitializeLights();
                if (!result)
                    return null;
            }
            
            return Asus;
        }
    }

    public class AsusSdkWrapper
    {
        //LED SDK

        public enum DeviceTypes
        {
            All = 0x00000000,
            Motherboard = 0x00010000,
            MotherboardLED = 0x00011000,
            AIO = 0x00012000,
            VGA = 0x00020000,
            Display = 0x00030000,
            Headset = 0x00040000,
            Microphone = 0x00050000,
            HDD = 0x00060000,
            BDD = 0x00061000,
            DRAM = 0x00070000,
            Keyboard = 0x00080000,
            Notebook = 0x00081000,
            NotebookZones = 0x00081001,
            Mouse = 0x00090000,
            Chassis = 0x000B0000,
            Projector = 0x000C0000
        }

        
    }

    public interface IAsusSdk
    {
        bool InitializeLights();

        void ShutdownSdk();
        void SetLights(Color color);
        void ResetAsusDevices(bool AsusDeviceKeyboard, bool AsusDeviceMouse, bool AsusDeviceHeadset, Color basecol);
        void SetAllLights(Color col);
        void ApplyMapSingleLighting(Color col);
        void ApplyMapMultiLighting(Color col, string region);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapMouseLighting(string key, Color col);
        void ApplyMapHeadsetLighting(string key, Color col);
        Task Ripple1(Color burstcol, int speed, Color baseColor);
        Task Ripple2(Color burstcol, int speed);
        Task MultiRipple1(Color burstcol, int speed);
        Task MultiRipple2(Color burstcol, int speed);
        void Flash1(Color burstcol, int speed, string[] regions);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
        void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void SingleFlash1(Color burstcol, int speed, string[] regions);
        void SingleFlash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void SingleFlash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50);
        void CycleEffect(int interval, CancellationTokenSource token);
        void DeviceUpdate();
    }

    public class Asus : IAsusSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private IAuraSdk sdk;
        private bool isInitialized = false;

        private CancellationTokenSource _cancellationTokenSource;

        private bool _AsusDeviceKeyboard = true;
        private bool _AsusDeviceMouse = true;
        private bool _AsusDeviceHeadset = true;
        private bool _AsusDeviceMousepad = true;
        private bool _AsusDeviceSpeakers = true;

        private bool _keyConnected = false;
        private bool _mouseConnected = false;
        private bool _headsetConnected = false;

        private static readonly object AsusRipple1 = new object();
        private static readonly object AsusRipple2 = new object();
        private static readonly object AsusFlash1 = new object();
        private static int _asusFlash2Step;
        private static bool _asusFlash2Running;
        private static Dictionary<string, Color> _flashpresets = new Dictionary<string, Color>();
        private static readonly object AsusFlash2 = new object();
        private static int _asusFlash3Step;
        private static bool _asusFlash3Running;
        private static readonly object AsusFlash3 = new object();
        private static int _asusFlash4Step;
        private static bool _asusFlash4Running;
        private static Dictionary<string, Color> _flashpresets4 = new Dictionary<string, Color>();
        private static readonly object AsusFlash4 = new object();

        private readonly Dictionary<string, ushort> _asuskeyids = new Dictionary<string, ushort>
        {
            //Keys
            {"F1", 59},
            {"F2", 60},
            {"F3", 61},
            {"F4", 62},
            {"F5", 63},
            {"F6", 64},
            {"F7", 65},
            {"F8", 66},
            {"F9", 67},
            {"F10", 68},
            {"F11", 87},
            {"F12", 88},
            {"D1", 2},
            {"D2", 3},
            {"D3", 4},
            {"D4", 5},
            {"D5", 6},
            {"D6", 7},
            {"D7", 8},
            {"D8", 9},
            {"D9", 10},
            {"D0", 11},
            {"A", 30},
            {"B", 48},
            {"C", 46},
            {"D", 32},
            {"E", 18},
            {"F", 33},
            {"G", 34},
            {"H", 35},
            {"I", 23},
            {"J", 36},
            {"K", 37},
            {"L", 38},
            {"M", 50},
            {"N", 49},
            {"O", 24},
            {"P", 25},
            {"Q", 16},
            {"R", 19},
            {"S", 31},
            {"T", 20},
            {"U", 22},
            {"V", 47},
            {"W", 17},
            {"X", 45},
            {"Y", 21},
            {"Z", 44},
            {"NumLock", 69},
            {"Num0", 82},
            {"Num1", 79},
            {"Num2", 80},
            {"Num3", 81},
            {"Num4", 75},
            {"Num5", 76},
            {"Num6", 77},
            {"Num7", 71},
            {"Num8", 72},
            {"Num9", 73},
            {"NumDivide", 181},
            {"NumMultiply", 55},
            {"NumSubtract", 74},
            {"NumAdd", 78},
            {"NumEnter", 156},
            {"NumDecimal", 83},
            {"PrintScreen", 183},
            {"Scroll", 70},
            {"Pause", 197},
            {"Insert", 210},
            {"Home", 199},
            {"PageUp", 201},
            {"PageDown", 209},
            {"Delete", 211},
            {"End", 207},
            {"Up", 200},
            {"Left", 203},
            {"Right", 205},
            {"Down", 208},
            {"Tab", 15},
            {"CapsLock", 58},
            {"Backspace", 14},
            {"Enter", 28},
            {"LeftControl", 29},
            {"LeftWindows", 219},
            {"LeftAlt", 56},
            {"Space", 57},
            {"RightControl", 157},
            {"Function", 256},
            {"RightAlt", 184},
            {"RightMenu", 221},
            {"LeftShift", 42},
            {"RightShift", 54},
            {"OemTilde", 41},
            {"OemMinus", 12},
            {"OemEquals", 13},
            {"OemLeftBracket", 26},
            {"OemRightBracket", 27},
            {"OemSlash", 53},
            {"OemSemicolon", 39},
            {"OemApostrophe", 40},
            {"OemComma", 51},
            {"OemPeriod", 52},
            {"OemBackslash", 43},
            {"Escape", 1},
            {"EurPound", 3},
            {"JpnYen", 2},
            {"Logo", 257},
            {"EffectLeft", 258},
            {"EffectRight", 259}
        };

        private Dictionary<ushort, IAuraRgbKey> _idToKey = new Dictionary<ushort, IAuraRgbKey>();

        private Dictionary<string, Color> prevKeyboard = new Dictionary<string, Color>();

        private static Dictionary<string, Color> keyMappings = new Dictionary<string, Color>();
        
        private static Dictionary<string, Color> mouseMappings = new Dictionary<string, Color>
        {
            {"0", Color.Black },
            {"1", Color.Black },
            {"2", Color.Black },
            {"3", Color.Black },
        };

        private static Dictionary<string, Color> padMappings = new Dictionary<string, Color>
        {
            {"0", Color.Black },
            {"1", Color.Black },
            {"2", Color.Black },
            {"3", Color.Black },
        };

        private void SetRgbLight(IAuraRgbKey rgbKey, Color color)
        {
            lock (rgbKey)
            {
                rgbKey.Red = color.R;
                rgbKey.Green = color.G;
                rgbKey.Blue = color.B;
            }
        }

        public bool InitializeLights()
        {
            var result = true;
            try
            {
                foreach (var hid in _asuskeyids)
                {
                    if (!prevKeyboard.ContainsKey(hid.Key))
                    {
                        prevKeyboard.Add(hid.Key, Color.Black);
                    }
                }
                
                sdk = new AuraSdk();

                if (sdk == null)
                {
                    return false;
                }

                sdk.SwitchMode();
                var devices = sdk.Enumerate((int)AsusSdkWrapper.DeviceTypes.All);
                var devconnected = false;
                
                foreach (IAuraSyncDevice dev in devices)
                {
                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Keyboard || dev.Type == (int)AsusSdkWrapper.DeviceTypes.Notebook) //Keyboard
                    {
                        //_AsusDeviceKeyboard = true;
                        devconnected = true;
                        _keyConnected = true;


                        var _keyboard = (IAuraSyncKeyboard) dev;


                        foreach (IAuraRgbKey key in _keyboard.Keys)
                        {
                            if (_idToKey.ContainsKey(key.Code))
                            {
                                _idToKey[key.Code] = key;
                            }
                            else
                            {
                                _idToKey.Add(key.Code, key);
                            }
                        }
                    }

                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Mouse) //Mouse
                    {
                        //_AsusDeviceMouse = true;
                        devconnected = true;
                        _mouseConnected = true;
                    }

                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Headset) //Headset
                    {
                        //_AsusDeviceHeadset = true;
                        devconnected = true;
                        _headsetConnected = true;
                    }
                }

                if (devconnected)
                {
                    isInitialized = true;
                }
                else
                {
                    Write.WriteConsole(ConsoleTypes.Asus, @"Cannot detect any Asus devices connected. Please connect a device and restart Chromatics.");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Asus, @"Asus Aura SDK failed to load. EX: " + ex);
                isInitialized = false;
                result = false;
            }
            return result;
        }

        public void ShutdownSdk()
        {
            try
            {
                //No yet implemented
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }
        
        public void DeviceUpdate()
        {
            if (!isInitialized) return;

            try
            {
                var devices = sdk.Enumerate((int)AsusSdkWrapper.DeviceTypes.All);
                foreach (IAuraSyncDevice dev in devices)
                {
                    if (_keyConnected)
                    {
                        if (dev.Type == (int) AsusSdkWrapper.DeviceTypes.Keyboard ||
                            dev.Type == (int) AsusSdkWrapper.DeviceTypes.Notebook) //Keyboard
                        {
                            dev.Apply();
                        }
                    }

                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Mouse) //Mouse
                    {
                        dev.Apply();
                    }

                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Headset) //Headset
                    {
                        dev.Apply();
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Asus,
                    "Asus Aura SDK, error when updating device. EX: " + ex);
            }
        }

        public void ApplyMapKeyLighting(string key, Color color, bool clear, [Optional] bool bypasswhitelist)
        {
            if (!_AsusDeviceKeyboard || !_keyConnected)
                return;

            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            try
            {
                if (_asuskeyids.ContainsKey(key))
                {
                    var keyString = _asuskeyids[key];

                    if (!_idToKey.ContainsKey(keyString))
                    {
                        return;
                    }

                    if (prevKeyboard.ContainsKey(key))
                    {
                        if (prevKeyboard[key] == color)
                            return;
                    }

                    var keyId = _idToKey[_asuskeyids[key]];
                    
                    SetRgbLight(keyId, color);

                    if (prevKeyboard.ContainsKey(key))
                    {
                        prevKeyboard[key] = color;
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, @"Asus (" + key + "): " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Error, @"Internal Error (" + key + "): " + ex.StackTrace);
            }
        }

        public void SetLights(Color color)
        {
            if (!_AsusDeviceKeyboard || !_keyConnected)
                return;

            try
            {
                foreach (var keyId in _idToKey)
                {
                    if (!_asuskeyids.ContainsValue(keyId.Key))
                    {
                        continue;
                    }

                    var keyString = _asuskeyids.FirstOrDefault(x => x.Value == keyId.Key);

                    if (prevKeyboard.ContainsKey(keyString.Key))
                    {
                        if (prevKeyboard[keyString.Key] == color)
                            continue;
                    }

                    SetRgbLight(keyId.Value, color);

                    if (prevKeyboard.ContainsKey(keyString.Key))
                    {
                        prevKeyboard[keyString.Key] = color;
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, @"Asus: " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Error, @"Internal Error: " + ex.StackTrace);
            }

        }

        public void SetAllLights(Color color)
        {
            if (!_AsusDeviceKeyboard || !_keyConnected)
                return;

            try
            {
                foreach (var keyId in _idToKey)
                {
                    if (!_asuskeyids.ContainsValue(keyId.Key))
                    {
                        continue;
                    }

                    var keyString = _asuskeyids.FirstOrDefault(x => x.Value == keyId.Key);

                    if (prevKeyboard.ContainsKey(keyString.Key))
                    {
                        if (prevKeyboard[keyString.Key] == color)
                            continue;
                    }

                    SetRgbLight(keyId.Value, color);

                    if (prevKeyboard.ContainsKey(keyString.Key))
                    {
                        prevKeyboard[keyString.Key] = color;
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, @"Asus: " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Error, @"Internal Error: " + ex.StackTrace);
            }
        }

        public void ApplyMapSingleLighting(Color color)
        {
            if (!_AsusDeviceKeyboard)
                return;

        }

        public void ApplyMapMultiLighting(Color color, string region)
        {
            if (!_AsusDeviceKeyboard)
                return;


            switch (region)
            {
                case "All":
                    break;
                case "0":
                    break;
                case "1":
                    break;
                case "2":
                    break;
                case "3":
                    break;
                case "4":
                    break;
            }
        }
        
        public void ApplyMapMouseLighting(string key, Color color)
        {
            if (!_AsusDeviceMouse)
                return;


            switch (key)
            {
                case "0":
                    break;
                case "1":
                    break;
                case "2":
                    break;
            }
        }
        
        public void ApplyMapHeadsetLighting(string key, Color color)
        {
            if (!_AsusDeviceHeadset)
                return;


            switch (key)
            {
                case "0":
                    break;
                case "1":
                    break;
                case "2":
                    break;
            }
        }

        public void ApplyMapPadLighting(string key, Color color)
        {
            if (!_AsusDeviceMousepad)
                return;


            switch (key)
            {
                case "0":
                    break;
                case "1":
                    break;
                case "2":
                    break;
            }
        }


        public void ResetAsusDevices(bool deviceKeyboard, bool deviceMouse, bool deviceHeadset, Color basecol)
        {

            _AsusDeviceKeyboard = deviceKeyboard;
            _AsusDeviceMouse = deviceMouse;
            _AsusDeviceHeadset = deviceHeadset;

        }
        
        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                if (!isInitialized || !_AsusDeviceKeyboard)
                    return;

                var presets = new Dictionary<string, Color>();

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        foreach (var key in DeviceEffects.GlobalKeys)
                            try
                            {
                                if (prevKeyboard.ContainsKey(key))
                                {
                                    presets.Add(key, prevKeyboard[key]);
                                }
                            }
                            catch (Exception ex)
                            {
                                Write.WriteConsole(ConsoleTypes.Error, @"(" + key + "): " + ex.Message);
                            }
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                            ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                            ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                            ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                            ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                            ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                            ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                            ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                            ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                        }
                    }
                    else if (i == 9)
                    {
                        //Spin down

                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            ApplyMapKeyLighting(key, presets[key], true);
                        }

                        
                        ApplyMapKeyLighting("D1", baseColor, true);
                        ApplyMapKeyLighting("D2", baseColor, true);
                        ApplyMapKeyLighting("D3", baseColor, true);
                        ApplyMapKeyLighting("D4", baseColor, true);
                        ApplyMapKeyLighting("D5", baseColor, true);
                        ApplyMapKeyLighting("D6", baseColor, true);
                        ApplyMapKeyLighting("D7", baseColor, true);
                        ApplyMapKeyLighting("D8", baseColor, true);
                        ApplyMapKeyLighting("D9", baseColor, true);
                        ApplyMapKeyLighting("D0", baseColor, true);
                        ApplyMapKeyLighting("OemMinus", baseColor, true);
                        ApplyMapKeyLighting("OemEquals", baseColor, true);

                        presets.Clear();
                    }

                    if (i < 9)
                    {
                        Thread.Sleep(speed);
                    }

                    DeviceUpdate();
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!isInitialized || !_AsusDeviceKeyboard)
                    return;

                var safeKeys = DeviceEffects.GlobalKeys.Except(FfxivHotbar.Keybindwhitelist);

                lock (AsusRipple2)
                {
                    var previousValues = new Dictionary<string, Color>();
                    var enumerable = safeKeys.ToList();

                    for (var i = 0; i <= 9; i++)
                    {

                        if (i == 0)
                        {
                            //Setup

                            foreach (var key in enumerable)
                                try
                                {
                                    if (prevKeyboard.ContainsKey(key))
                                    {
                                        previousValues.Add(key, prevKeyboard[key]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Write.WriteConsole(ConsoleTypes.Error, @"(" + key + "): " + ex.Message);
                                }
                        }
                        else if (i == 1)
                        {
                            //Step 0
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                if (pos > -1)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 2)
                        {
                            //Step 1
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                if (pos > -1)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 3)
                        {
                            //Step 2
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                                if (pos > -1)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 4)
                        {
                            //Step 3
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                if (pos > -1)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 5)
                        {
                            //Step 4
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                if (pos > -1)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 6)
                        {
                            //Step 5
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                if (pos > -1)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 7)
                        {
                            //Step 6
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                if (pos > -1)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 8)
                        {
                            //Step 7
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                if (pos > -1)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 9)
                        {
                            //Spin down

                            foreach (var key in previousValues.Keys)
                            {
                                ApplyMapKeyLighting(key, previousValues[key], true);
                            }

                            previousValues.Clear();
                        }

                        if (i < 9)
                        {
                            Thread.Sleep(speed);
                        }

                        DeviceUpdate();
                    }
                }
            });
        }

        public Task MultiRipple1(Color burstcol, int speed)
        {
            throw new NotImplementedException();
        }

        public Task MultiRipple2(Color burstcol, int speed)
        {
            throw new NotImplementedException();
        }

        public void Flash1(Color burstcol, int speed, string[] regions)
        {
            lock (AsusFlash1)
            {
                if (!isInitialized || !_AsusDeviceKeyboard)
                    return;

                var previousValues = new Dictionary<string, Color>();

                for (var i = 0; i <= 8; i++)
                {

                    if (i == 0)
                    {
                        //Setup

                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(key))
                            {
                                previousValues.Add(key, prevKeyboard[key]);
                            }
                        }
                    }
                    else if (i % 2 == 1)
                    {
                        //Step 1, 3, 5, 7
                        foreach (var key in regions)
                        {
                            ApplyMapKeyLighting(key, burstcol, true);
                        }
                    }
                    else if (i % 2 == 0)
                    {
                        //Step 2, 4, 6, 8
                        foreach (var key in regions)
                        {
                            ApplyMapKeyLighting(key, previousValues[key], true);
                        }
                    }

                    if (i < 8)
                    {
                        Thread.Sleep(speed);
                    }

                    DeviceUpdate();
                }
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                if (!isInitialized || !_AsusDeviceKeyboard)
                    return;

                lock (AsusFlash2)
                {
                    var previousValues = new Dictionary<string, Color>();

                    if (!_asusFlash2Running)
                    {
                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(key))
                            {
                                previousValues.Add(key, prevKeyboard[key]);
                            }
                        }
                        
                        _asusFlash2Running = true;
                        _asusFlash2Step = 0;
                        _flashpresets = previousValues;
                    }

                    if (_asusFlash2Running)
                        while (_asusFlash2Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_asusFlash2Step == 0)
                            {
                                foreach (var key in regions)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                                
                                _asusFlash2Step = 1;
                            }
                            else if (_asusFlash2Step == 1)
                            {
                                foreach (var key in regions)
                                {
                                    ApplyMapKeyLighting(key, _flashpresets[key], true);
                                }
                                
                                _asusFlash2Step = 0;
                            }

                            DeviceUpdate();
                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
                //
            }
        }

        public void Flash3(Color burstcol, int speed, CancellationToken cts)
        {
            try
            {
                if (!isInitialized || !_AsusDeviceKeyboard)
                    return;

                lock (AsusFlash3)
                {
                    //var previousValues = new Dictionary<string, Color>();
                    _asusFlash3Running = true;
                    _asusFlash3Step = 0;

                    if (_asusFlash3Running == false)
                    {
                        //
                    }
                    else
                    {
                        while (_asusFlash3Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_asusFlash3Step == 0)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    if (prevKeyboard.ContainsKey(key))
                                    {
                                        ApplyMapKeyLighting(key, burstcol, true);
                                    }
                                        
                                }
                                  

                                _asusFlash3Step = 1;
                            }
                            else if (_asusFlash3Step == 1)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    if (prevKeyboard.ContainsKey(key))
                                    {
                                        ApplyMapKeyLighting(key, Color.Black, true);
                                    }
                                }
                                
                                _asusFlash3Step = 0;
                            }

                            DeviceUpdate();
                            Thread.Sleep(speed);
                        }
                    }
                }
            }
            catch
            {
                //
            }
        }

        public void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                if (!isInitialized || !_AsusDeviceKeyboard)
                    return;
                
                lock (AsusFlash4)
                {
                    var flashpresets = new Dictionary<string, Color>();

                    if (!_asusFlash4Running)
                    {
                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(key))
                                flashpresets.Add(key, prevKeyboard[key]);
                        }
                            

                        _asusFlash4Running = true;
                        _asusFlash4Step = 0;
                        _flashpresets4 = flashpresets;
                    }

                    if (_asusFlash4Running)
                        while (_asusFlash4Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;
                            
                            if (_asusFlash4Step == 0)
                            {
                                foreach (var key in regions)
                                {
                                    ApplyMapKeyLighting(key, burstcol, true);
                                }
                                
                                _asusFlash4Step = 1;
                            }
                            else if (_asusFlash4Step == 1)
                            {
                                foreach (var key in regions)
                                {
                                    ApplyMapKeyLighting(key, _flashpresets4[key], true);
                                }
                                
                                _asusFlash4Step = 0;
                            }

                            DeviceUpdate();

                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
                //
            }
        }

        public void SingleFlash1(Color burstcol, int speed, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void SingleFlash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void SingleFlash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50)
        {
            if (!isInitialized || !_keyConnected) return;
            if (cts.IsCancellationRequested) return;

            Dictionary<string, ColorFader> colorFaderDict = new Dictionary<string, ColorFader>();
            
            Thread.Sleep(500);

            while (true)
            {
                if (cts.IsCancellationRequested) break;

                var rnd = new Random();
                colorFaderDict.Clear();

                foreach (var key in regions)
                {
                    if (cts.IsCancellationRequested) return;

                    var rndCol = toColor[rnd.Next(toColor.Length)];

                    colorFaderDict.Add(key, new ColorFader(toColor[0], rndCol, interval));
                }

                Task t = Task.Factory.StartNew(() =>
                {
                    //Thread.Sleep(500);

                    var _regions = regions.OrderBy(x => rnd.Next()).ToArray();

                    foreach (var key in _regions)
                    {
                        if (cts.IsCancellationRequested) return;

                        foreach (var color in colorFaderDict[key].Fade())
                        {
                            if (cts.IsCancellationRequested) return;

                            ApplyMapKeyLighting(key, color, false);
                        }

                        DeviceUpdate();
                        Thread.Sleep(speed);
                    }
                });


                Thread.Sleep(colorFaderDict.Count * speed);
            }
        }

        private readonly object lockObject = new object();
        public void CycleEffect(int interval, CancellationTokenSource token)
        {
            if (!isInitialized || !_AsusDeviceKeyboard)
                return;
            
            while (true)
            {
                lock (lockObject)
                {
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        
                        var col = Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255), (int)0);

                        SetAllLights(col);
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        
                        var col = Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255), (int)0);

                        SetAllLights(col);
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        
                        var color = Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255), (int)0);

                        SetAllLights(color);
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);

                        var color = Color.FromArgb((int) 0, (int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255));

                        SetAllLights(color);
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        var color = Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), (int)0, (int)Math.Ceiling((double)(250 * 100) / 255));

                        SetAllLights(color);
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);

                        var color = Color.FromArgb((int) Math.Ceiling((double) (250 * 100) / 255), (int) 0,
                            (int) Math.Ceiling((double) (x * 100) / 255));

                        SetAllLights(color);
                    }

                    if (token.IsCancellationRequested) break;

                    DeviceUpdate();
                }
            }
            Thread.Sleep(interval);
        }


    }
}