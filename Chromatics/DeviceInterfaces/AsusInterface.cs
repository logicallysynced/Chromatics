using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

            
            Asus = new Asus();
            var result = Asus.InitializeLights();
            if (!result)
                return null;

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

        private readonly Dictionary<string, int> _asuskeyids = new Dictionary<string, int>
        {
            //Keys
            {"F1", 0x003B},
            {"F2", 0x003C},
            {"F3", 0x003D},
            {"F4", 0x003E},
            {"F5", 0x003F},
            {"F6", 0x0040},
            {"F7", 0x0041},
            {"F8", 0x0042},
            {"F9", 0x0043},
            {"F10", 0x0044},
            {"F11", 0x0057},
            {"F12", 0x0058},
            {"D1", 0x0002},
            {"D2", 0x0003},
            {"D3", 0x0004},
            {"D4", 0x0005},
            {"D5", 0x0006},
            {"D6", 0x0007},
            {"D7", 0x0008},
            {"D8", 0x0009},
            {"D9", 0x000A},
            {"D0", 0x000B},
            {"A", 0x001E},
            {"B", 0x0030},
            {"C", 0x002E},
            {"D", 0x0020},
            {"E", 0x0012},
            {"F", 0x0021},
            {"G", 0x0022},
            {"H", 0x0023},
            {"I", 0x0017},
            {"J", 0x0024},
            {"K", 0x0025},
            {"L", 0x0026},
            {"M", 0x0032},
            {"N", 0x0031},
            {"O", 0x0018},
            {"P", 0x0019},
            {"Q", 0x0010},
            {"R", 0x0013},
            {"S", 0x001F},
            {"T", 0x0014},
            {"U", 0x0016},
            {"V", 0x002F},
            {"W", 0x0011},
            {"X", 0x002D},
            {"Y", 0x0015},
            {"Z", 0x002C},
            {"NumLock", 0x0045},
            {"Num0", 0x0052},
            {"Num1", 0x004F},
            {"Num2", 0x0050},
            {"Num3", 0x0051},
            {"Num4", 0x004B},
            {"Num5", 0x004C},
            {"Num6", 0x004D},
            {"Num7", 0x0047},
            {"Num8", 0x0048},
            {"Num9", 0x0049},
            {"NumDivide", 0x00B5},
            {"NumMultiply", 0x0037},
            {"NumSubtract", 0x004A},
            {"NumAdd", 0x004E},
            {"NumEnter", 0x009C},
            {"NumDecimal", 0x0053},
            {"PrintScreen", 0x00B7},
            {"Scroll", 0x0046},
            {"Pause", 0x00C5},
            {"Insert", 0x00D2},
            {"Home", 0x00C7},
            {"PageUp", 0x00C9},
            {"PageDown", 0x00D1},
            {"Delete", 0x00D3},
            {"End", 0x00CF},
            {"Up", 0x00C8},
            {"Left", 0x00CB},
            {"Right", 0x00CD},
            {"Down", 0x00D0},
            {"Tab", 0x000F},
            {"CapsLock", 0x003A},
            {"Backspace", 0x000E},
            {"Enter", 0x001C},
            {"LeftControl", 0x001D},
            {"LeftWindows", 0x00DB},
            {"LeftAlt", 0x0038},
            {"Space", 0x0039},
            {"RightControl", 0x009D},
            {"Function", 0x0100},
            {"RightAlt", 0x00B8},
            {"RightMenu", 0x00DD},
            {"LeftShift", 0x002A},
            {"RightShift", 0x0036},
            {"OemTilde", 0x0029},
            {"OemMinus", 0x000C},
            {"OemEquals", 0x000D},
            {"OemLeftBracket", 0x001A},
            {"OemRightBracket", 0x001B},
            {"OemSlash", 0x0035},
            {"OemSemicolon", 0x0027},
            {"OemApostrophe", 0x0028},
            {"OemComma", 0x0033},
            {"OemPeriod", 0x0034},
            {"OemBackslash", 0x002B},
            {"Escape", 0x0001}
        };

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
                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Keyboard || dev.Type == (int)AsusSdkWrapper.DeviceTypes.Notebook || dev.Type == (int)AsusSdkWrapper.DeviceTypes.NotebookZones) //Keyboard
                    {
                        //_AsusDeviceKeyboard = true;
                        devconnected = true;
                    }

                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Mouse) //Mouse
                    {
                        //_AsusDeviceMouse = true;
                        devconnected = true;
                    }

                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Headset) //Headset
                    {
                        //_AsusDeviceHeadset = true;
                        devconnected = true;
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
                    if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Keyboard || dev.Type == (int)AsusSdkWrapper.DeviceTypes.Notebook || dev.Type == (int)AsusSdkWrapper.DeviceTypes.NotebookZones) //Keyboard
                    {
                        dev.Apply();
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
            if (!_AsusDeviceKeyboard)
                return;

            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            try
            {
                if (_asuskeyids.ContainsKey(key))
                {
                    if (prevKeyboard.ContainsKey(key))
                    {
                        if (prevKeyboard[key] == color)
                            return;
                    }

                    var devices = sdk.Enumerate((int)AsusSdkWrapper.DeviceTypes.All);
                    foreach (IAuraSyncDevice dev in devices)
                    {
                        if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Keyboard || dev.Type == (int)AsusSdkWrapper.DeviceTypes.Notebook) //Keyboard
                        {
                            if (dev.Lights[_asuskeyids[key]] != null)
                            {
                                dev.Lights[_asuskeyids[key]].Red = color.R;
                                dev.Lights[_asuskeyids[key]].Green = color.G;
                                dev.Lights[_asuskeyids[key]].Blue = color.B;

                                if (prevKeyboard.ContainsKey(key))
                                {
                                    prevKeyboard[key] = color;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, @"Asus (" + key + "): " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Error, @"Internal Error (" + key + "): " + ex.StackTrace);
            }
        }

        public void SetAllLights(Color color)
        {
            try
            {
                if (_AsusDeviceKeyboard)
                {
                    var devices = sdk.Enumerate((int)AsusSdkWrapper.DeviceTypes.All);
                    foreach (IAuraSyncDevice dev in devices)
                    {
                        if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Keyboard || dev.Type == (int)AsusSdkWrapper.DeviceTypes.Notebook) //Keyboard
                        {
                            foreach (var key in _asuskeyids)
                            {
                                if (prevKeyboard.ContainsKey(key.Key))
                                {
                                    if (prevKeyboard[key.Key] == color)
                                        continue;
                                }

                                dev.Lights[key.Value].Red = color.R;
                                dev.Lights[key.Value].Green = color.G;
                                dev.Lights[key.Value].Blue = color.B;

                                if (prevKeyboard.ContainsKey(key.Key))
                                {
                                    prevKeyboard[key.Key] = color;
                                }
                            }
                        }
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
        
        public void SetLights(Color color)
        {
            try
            {
                if (_AsusDeviceKeyboard)
                {
                    var devices = sdk.Enumerate((int)AsusSdkWrapper.DeviceTypes.All);
                    foreach (IAuraSyncDevice dev in devices)
                    {
                        if (dev.Type == (int)AsusSdkWrapper.DeviceTypes.Keyboard || dev.Type == (int)AsusSdkWrapper.DeviceTypes.Notebook) //Keyboard
                        {
                            foreach (var key in _asuskeyids)
                            {
                                if (prevKeyboard.ContainsKey(key.Key))
                                {
                                    if (prevKeyboard[key.Key] == color)
                                        continue;
                                }

                                dev.Lights[key.Value].Red = color.R;
                                dev.Lights[key.Value].Green = color.G;
                                dev.Lights[key.Value].Blue = color.B;

                                if (prevKeyboard.ContainsKey(key.Key))
                                {
                                    prevKeyboard[key.Key] = color;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, @"Asus: " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Error, @"Internal Error: " + ex.StackTrace);
            }

        }


        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                if (!isInitialized || !_AsusDeviceKeyboard)
                    return;

                var presets = new Dictionary<string, Color>();
                List<string> hids = new List<string>();
                List<Color> colors = new List<Color>();

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
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(burstcol);
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(presets[key]);
                            }
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(burstcol);
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(presets[key]);
                            }
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(burstcol);
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(presets[key]);
                            }
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(burstcol);
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(presets[key]);
                            }
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(burstcol);
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(presets[key]);
                            }
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(burstcol);
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(presets[key]);
                            }
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(burstcol);
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(presets[key]);
                            }
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(burstcol);
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(presets[key]);
                            }
                        }
                    }
                    else if (i == 9)
                    {
                        //Spin down

                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            hids.Add(key);
                            colors.Add(burstcol);
                            //ApplyMapKeyLighting(key, presets[key], true);
                        }

                        hids.Add("D1");
                        colors.Add(baseColor);
                        hids.Add("D2");
                        colors.Add(baseColor);
                        hids.Add("D3");
                        colors.Add(baseColor);
                        hids.Add("D4");
                        colors.Add(baseColor);
                        hids.Add("D5");
                        colors.Add(baseColor);
                        hids.Add("D6");
                        colors.Add(baseColor);
                        hids.Add("D7");
                        colors.Add(baseColor);
                        hids.Add("D8");
                        colors.Add(baseColor);
                        hids.Add("D9");
                        colors.Add(baseColor);
                        hids.Add("D0");
                        colors.Add(baseColor);
                        hids.Add("OemMinus");
                        colors.Add(baseColor);
                        hids.Add("OemEquals");
                        colors.Add(baseColor);

                        /*
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
                        */

                        presets.Clear();
                    }

                    if (i < 9)
                    {
                        Thread.Sleep(speed);
                    }

                    DeviceUpdate();
                    hids.Clear();
                    colors.Clear();
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
                    List<string> hids = new List<string>();
                    List<Color> colors = new List<Color>();

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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 9)
                        {
                            //Spin down

                            foreach (var key in previousValues.Keys)
                            {
                                hids.Add(key);
                                colors.Add(previousValues[key]);
                                //ApplyMapKeyLighting(key, previousValues[key], true);
                            }

                            previousValues.Clear();
                        }

                        if (i < 9)
                        {
                            Thread.Sleep(speed);
                        }

                        DeviceUpdate();
                        hids.Clear();
                        colors.Clear();
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
                List<string> hids = new List<string>();
                List<Color> colors = new List<Color>();

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
                            hids.Add(key);
                            colors.Add(burstcol);
                            //ApplyMapKeyLighting(key, burstcol, true);
                        }
                    }
                    else if (i % 2 == 0)
                    {
                        //Step 2, 4, 6, 8
                        foreach (var key in regions)
                        {
                            hids.Add(key);
                            colors.Add(previousValues[key]);
                            //ApplyMapKeyLighting(key, previousValues[key], true);
                        }
                    }

                    if (i < 8)
                    {
                        Thread.Sleep(speed);
                    }

                    DeviceUpdate();
                    hids.Clear();
                    colors.Clear();
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
                    List<string> hids = new List<string>();
                    List<Color> colors = new List<Color>();

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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                                
                                _asusFlash2Step = 1;
                            }
                            else if (_asusFlash2Step == 1)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(key);
                                    colors.Add(_flashpresets[key]);
                                    //ApplyMapKeyLighting(key, _flashpresets[key], true);
                                }
                                
                                _asusFlash2Step = 0;
                            }

                            DeviceUpdate();
                            hids.Clear();
                            colors.Clear();
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

                    List<string> hids = new List<string>();
                    List<Color> colors = new List<Color>();

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
                                        hids.Add(key);
                                        colors.Add(burstcol);
                                        //ApplyMapKeyLighting(key, burstcol, true);
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
                                        hids.Add(key);
                                        colors.Add(Color.Black);
                                        //ApplyMapKeyLighting(key, Color.Black, true);
                                    }
                                }
                                
                                _asusFlash3Step = 0;
                            }

                            DeviceUpdate();
                            hids.Clear();
                            colors.Clear();
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
                    List<string> hids = new List<string>();
                    List<Color> colors = new List<Color>();

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
                                    hids.Add(key);
                                    colors.Add(burstcol);
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                                
                                _asusFlash4Step = 1;
                            }
                            else if (_asusFlash4Step == 1)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(key);
                                    colors.Add(_flashpresets4[key]);
                                    //ApplyMapKeyLighting(key, _flashpresets4[key], true);
                                }
                                
                                _asusFlash4Step = 0;
                            }

                            DeviceUpdate();
                            hids.Clear();
                            colors.Clear();

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
            if (!isInitialized || !_AsusDeviceKeyboard) return;
            if (cts.IsCancellationRequested) return;
            
            Dictionary<string, ColorFader> colorFaderDict = new Dictionary<string, ColorFader>();

            //Keyboard.SetCustomAsync(refreshKeyGrid);
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

                    List<string> hids = new List<string>();
                    List<Color> colors = new List<Color>();

                    foreach (var key in _regions)
                    {
                        if (cts.IsCancellationRequested) return;
                        if (!colorFaderDict.ContainsKey(key)) continue;

                        foreach (var color in colorFaderDict[key].Fade())
                        {
                            if (cts.IsCancellationRequested) return;

                            if (prevKeyboard.ContainsKey(key))
                            {
                                hids.Add(key);
                                colors.Add(color);
                            }
                        }

                        DeviceUpdate();
                        hids.Clear();
                        colors.Clear();

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

            List<string> hids = new List<string>();
            List<Color> colors = new List<Color>();

            while (true)
            {
                lock (lockObject)
                {
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in _asuskeyids)
                        {
                            hids.Add(hid.Key);
                            colors.Add(Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255), (int)0));
                        }
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in _asuskeyids)
                        {
                            hids.Add(hid.Key);
                            
                            colors.Add(Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255), (int)0));
                        }
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in _asuskeyids)
                        {
                            hids.Add(hid.Key);
                            
                            colors.Add(Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255), (int)0));
                        }
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in _asuskeyids)
                        {
                            hids.Add(hid.Key);
                            
                            colors.Add(Color.FromArgb((int)0, (int)Math.Ceiling((double)(x * 100) / 255), (int)Math.Ceiling((double)(250 * 100) / 255)));
                        }
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in _asuskeyids)
                        {
                            hids.Add(hid.Key);

                            colors.Add(Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), (int)0, (int)Math.Ceiling((double)(250 * 100) / 255)));
                        }

                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        foreach (var hid in _asuskeyids)
                        {
                            hids.Add(hid.Key);
                            
                            colors.Add(Color.FromArgb((int)Math.Ceiling((double) (250 * 100) / 255), (int)0, (int)Math.Ceiling((double) (x * 100) / 255)));
                        }

                    }

                    if (token.IsCancellationRequested) break;

                    DeviceUpdate();
                    hids.Clear();
                    colors.Clear();
                }
            }
            Thread.Sleep(interval);
        }


    }
}