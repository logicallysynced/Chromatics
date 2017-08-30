using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;
using Corale.Colore.Core;
using Corale.Colore.Razer;
using Corale.Colore.Razer.Keyboard;
using Corale.Colore.Razer.Keyboard.Effects;
using Corale.Colore.Razer.Mouse;
using Corale.Colore.WinForms;
using GalaSoft.MvvmLight.Ioc;
using Color = System.Drawing.Color;
using Constants = Corale.Colore.Razer.Keyboard.Constants;
using Effect = Corale.Colore.Razer.Headset.Effects.Effect;

/* Contains all Razer SDK code (via Colore) for detection, initilization, states and effects.
 * Corale.Colore.dll is used to port the Razer SDK into C#
 * https://github.com/CoraleStudios/Colore
 */

namespace Chromatics.DeviceInterfaces
{
    public class RazerInterface
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static RazerLib InitializeRazerSdk()
        {
            var razer = new RazerLib();

            string arch;
            //string arch = @"C:\Program Files\Razer Chroma SDK\bin\RzChromaSDK64.dll";

            if (Environment.Is64BitOperatingSystem)
            {
                arch = Environment.GetEnvironmentVariable("ProgramW6432") + @"\Razer Chroma SDK\bin\RzChromaSDK64.dll";
                Write.WriteConsole(ConsoleTypes.System, "Architecture: x64");
            }
            else
            {
                arch = Environment.GetEnvironmentVariable("ProgramFiles(x86)") +
                       @"\Razer Chroma SDK\bin\RzChromaSDK.dll";
                Write.WriteConsole(ConsoleTypes.System, "Architecture: x86");
            }

            if (File.Exists(arch))
            {
                Write.WriteConsole(ConsoleTypes.Razer, "Razer SDK Detected: " + arch);

                if (Chroma.SdkAvailable == false)
                {
                    Write.WriteConsole(ConsoleTypes.Razer, "Razer SDK not found");
                    return null;
                }
            }
            else
            {
                //Razer SDK DLL Not Found

                if (Environment.Is64BitOperatingSystem)
                    Write.WriteConsole(ConsoleTypes.Razer,
                        "The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Uninstall any previous versions of Razer SDK & Synapse and then reinstall Razer Synapse.");
                else
                    Write.WriteConsole(ConsoleTypes.Razer,
                        "The Razer SDK (RzChromaSDK.dll) Could not be found on this computer. Uninstall any previous versions of Razer SDK & Synapse and then reinstall Razer Synapse.");

                return null;
            }


            Write.WriteConsole(ConsoleTypes.Razer, "Start Colore Setup");

            if (Chroma.Instance.Initialized != true)
            {
                Write.WriteConsole(ConsoleTypes.Razer, "Attempting to load Colore..");
                Chroma.Instance.Initialize();
            }
            else
            {
                Write.WriteConsole(ConsoleTypes.Razer, "Colore Already loaded.");
            }


            //UpdateState("static", System.Drawing.Color.DeepSkyBlue, false);
            //write.WriteConsole(ConsoleTypes.RAZER, "Razer SDK Loaded");
            //Console.WriteLine("CALL");

            return razer;
        }
    }

    public class RazerSdkWrapper
    {
        //
    }

    public interface IRazerSdk
    {
        void InitializeLights();

        void ResetRazerDevices(bool deviceKeyboard, bool deviceKeypad, bool deviceMouse, bool deviceMousepad,
            bool deviceHeadset, bool deviceChromaLink, Color basecol);

        void KeyboardUpdate();
        void SetLights(Color col);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapLogoLighting(string key, Color col, bool clear);
        void ApplyMapMouseLighting(string key, Color col, bool clear);
        void ApplyMapPadLighting(int region, Color col, bool clear);

        void ApplyMapHeadsetLighting(Color col, bool clear);
        void ApplyMapKeypadLighting(Color col, bool clear);

        void ApplyMapChromaLinkLighting(Color col, int pos);

        //void UpdateState(string type, Color col, bool disablekeys, [Optional] Color col2, [Optional] bool direction, [Optional] int speed);

        void SetWave();

        Task Ripple1(Color burstcol, int speed);

        Task Ripple2(Color burstcol, int speed);

        void Flash1(Color burstcol, int speed, string[] regions);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
        void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
    }

    public class RazerLib : IRazerSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private static readonly object _Razertransition = new object();

        private static readonly object RazerRipple1 = new object();


        private static readonly object RazerRipple2 = new object();


        private static readonly object RazerFlash1 = new object();

        private static int _razerFlash2Step;
        private static bool _razerFlash2Running;

        private static Dictionary<string, Color> _flashpresets = new Dictionary<string, Color>();

        //Corale.Colore.Core.Color Pad1 = new Corale.Colore.Core.Color();
        private static readonly object RazerFlash2 = new object();

        private static int _razerFlash3Step;
        private static bool _razerFlash3Running;
        private static readonly object RazerFlash3 = new object();

        private static int _razerFlash4Step;
        private static bool _razerFlash4Running;

        private static Dictionary<string, Color> _flashpresets4 = new Dictionary<string, Color>();

        //Corale.Colore.Core.Color Pad1 = new Corale.Colore.Core.Color();
        private static readonly object RazerFlash4 = new object();

        private static readonly object _RazertransitionConst = new object();

        private readonly Dictionary<string, string> _razerkeyids = new Dictionary<string, string>
        {
            //Keys
            {"D1", "1 Key"},
            {"D2", "2 Key"},
            {"D3", "3 Key"},
            {"D4", "4 Key"},
            {"D5", "5 Key"},
            {"D6", "6 Key"},
            {"D7", "7 Key"},
            {"D8", "8 Key"},
            {"D9", "9 Key"},
            {"D0", "0 Key"},
            {"A", "A Key"},
            {"B", "B Key"},
            {"C", "C Key"},
            {"D", "D Key"},
            {"E", "E Key"},
            {"F", "F Key"},
            {"G", "G Key"},
            {"H", "H Key"},
            {"I", "I Key"},
            {"J", "J Key"},
            {"K", "K Key"},
            {"L", "L Key"},
            {"M", "M Key"},
            {"N", "N Key"},
            {"O", "O Key"},
            {"P", "P Key"},
            {"Q", "Q Key"},
            {"R", "R Key"},
            {"S", "S Key"},
            {"T", "T Key"},
            {"U", "U Key"},
            {"V", "V Key"},
            {"W", "W Key"},
            {"X", "X Key"},
            {"Y", "Y Key"},
            {"Z", "Z Key"},
            {"NumLock", "Numlock Key"},
            {"Num0", "Numlock 0"},
            {"Num1", "Numlock 1"},
            {"Num2", "Numlock 2"},
            {"Num3", "Numlock 3"},
            {"Num4", "Numlock 4"},
            {"Num5", "Numlock 5"},
            {"Num6", "Numlock 6"},
            {"Num7", "Numlock 7"},
            {"Num8", "Numlock 8"},
            {"Num9", "Numlock 9"},
            {"NumDivide", "Num Divide"},
            {"NumMultiply", "Num Multiply"},
            {"NumSubtract", "Num Subtract"},
            {"NumAdd", "Num Add"},
            {"NumEnter", "Num Enter"},
            {"NumDecimal", "Num Decimal"},
            {"PrintScreen", "Print Screen"},
            {"Scroll", "Scroll Lock"},
            {"Pause", "Pause Key"},
            {"Insert", "Insert Key"},
            {"Home", "Home Key"},
            {"PageUp", "Page Up"},
            {"PageDown", "Page Down"},
            {"Delete", "Delete Key"},
            {"End", "End Key"},
            {"Up", "Up Key"},
            {"Left", "Left Key"},
            {"Right", "Right Key"},
            {"Down", "Down Key"},
            {"Tab", "Tab Key"},
            {"CapsLock", "Caps Lock"},
            {"Backspace", "Backspace Key"},
            {"Enter", "Enter Key"},
            {"LeftControl", "Left Control"},
            {"LeftWindows", "Left Windows"},
            {"LeftAlt", "Left Alt"},
            {"Space", "Spacebar"},
            {"RightControl", "Right Control"},
            {"Function", "Fn Function"},
            {"RightAlt", "Right Alt"},
            {"RightMenu", "Right Menu"},
            {"LeftShift", "Left Shift"},
            {"RightShift", "Right Shift"},
            {"Macro1", "Macro 1"},
            {"Macro2", "Macro 2"},
            {"Macro3", "Macro 3"},
            {"Macro4", "Macro 4"},
            {"Macro5", "Macro 5"},
            {"OemTilde", "Tilde (~) Key"},
            {"OemMinus", "Minus (-) Key"},
            {"OemEquals", "Equals (=) Key"},
            {"OemLeftBracket", "Left square bracket ([)"},
            {"OemRightBracket", "Right square bracket (])"},
            {"OemSlash", "Forwardslash (/)"},
            {"OemSemicolon", "Semi-colon (;) Key"},
            {"OemApostrophe", "Apostrophe (') Key"},
            {"OemComma", "Comma (,) Key"},
            {"OemPeriod", "Period/full stop (.) Key"},
            {"OemBackslash", "Backslash Key"},
            {"EurPound", "Pound sign (#) Key"},
            {"JpnYen", "Yen (¥) Key"},
            {"Escape", "Esc Key"}
        };

        private Custom _keyboardGrid = Custom.Create();
        private Corale.Colore.Razer.Mouse.Effects.Custom _mouseGrid = Corale.Colore.Razer.Mouse.Effects.Custom.Create();

        private Corale.Colore.Razer.Mousepad.Effects.Custom _mousepadGrid =
            Corale.Colore.Razer.Mousepad.Effects.Custom.Create();

        private bool _razerDeathstalker;
        private bool _razerDeviceHeadset = true;

        private bool _razerDeviceKeyboard = true;
        private bool _razerDeviceKeypad = true;
        private bool _razerDeviceMouse = true;
        private bool _razerDeviceMousepad = true;
        private bool _razerDeviceChromaLink = true;

        //Handle device send/recieve
        private readonly CancellationTokenSource _rcts = new CancellationTokenSource();

        public void ResetRazerDevices(bool deviceKeyboard, bool deviceKeypad, bool deviceMouse, bool deviceMousepad,
            bool deviceHeadset, bool deviceChromaLink, Color basecol)
        {
            _razerDeviceKeyboard = deviceKeyboard;
            _razerDeviceKeypad = deviceKeypad;
            _razerDeviceMouse = deviceMouse;
            _razerDeviceMousepad = deviceMousepad;
            _razerDeviceHeadset = deviceHeadset;
            _razerDeviceChromaLink = deviceChromaLink;
            _razerDeathstalker = Chroma.Instance.Query(Devices.Deathstalker).Connected;

            if (_razerDeviceKeyboard)
                SetLights(basecol);
        }

        public void InitializeLights()
        {
            //Debug.WriteLine("Setting Razer Default");
            SetLights(Color.DeepSkyBlue);
        }

        public void SetLights(Color col)
        {
            if (!_razerDeviceKeyboard) return;

            /*
            var eff = new Static(col.ToColoreColor());
            Keyboard.Instance.SetStatic(eff);

            Keyboard.Instance.SetAll(col.ToColoreColor());
            */

            lock (RazerFlash1)
            {
                _keyboardGrid.Set(col.ToColoreColor());
            }
        }
        
        public void SetWave()
        {
            try
            {
                if (_razerDeviceHeadset)
                    Headset.Instance.SetEffect(Effect.SpectrumCycling);

                if (_razerDeviceKeyboard)
                    Keyboard.Instance.SetWave(Direction.LeftToRight);

                if (_razerDeviceKeypad)
                    Keypad.Instance.SetWave(Corale.Colore.Razer.Keypad.Effects.Direction.LeftToRight);

                if (_razerDeviceMouse)
                    Mouse.Instance.SetWave(Corale.Colore.Razer.Mouse.Effects.Direction.FrontToBack);

                if (_razerDeviceMousepad)
                    Mousepad.Instance.SetWave(Corale.Colore.Razer.Mousepad.Effects.Direction.LeftToRight);

                /*
                if (_razerDeviceChromaLink)
                    ChromaLink.Instance.SetEffect(Corale.Colore.Razer.ChromaLink.Effects.Effect.SpectrumCycling);
                */
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer (Wave): " + ex.Message);
            }
        }

        public void KeyboardUpdate()
        {
            Chroma.Instance.Keyboard.SetCustom(_keyboardGrid);
        }

        public void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            //keyboardGrid
            uint rzCol = col.ToColoreColor();

            //Send Lighting
            if (_razerDeviceKeyboard)
                try
                {
                    if (Enum.IsDefined(typeof(Key), key))
                    {
                        var keyid = (Key) Enum.Parse(typeof(Key), key);

                        if (clear)
                        {
                            if (Keyboard.Instance[keyid].Value != rzCol)
                                Keyboard.Instance.SetKey(keyid, rzCol, clear);
                        }
                        else
                        {
                            lock (RazerFlash1)
                            {
                                if (_keyboardGrid[keyid].Value != rzCol)
                                    _keyboardGrid[keyid] = rzCol;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Write.WriteConsole(ConsoleTypes.Error, "Razer Keyboard (" + key + "): " + ex.Message);
                }
        }

        public void ApplyMapLogoLighting(string key, Color col, bool clear)
        {
            uint rzCol = col.ToColoreColor();

            //Send Lighting
            if (_razerDeviceKeyboard)
                try
                {
                    if (Keyboard.Instance[0, 20].Value != rzCol)
                        Keyboard.Instance.SetPosition(0, 20, rzCol, clear);
                }
                catch (Exception ex)
                {
                    Write.WriteConsole(ConsoleTypes.Error, "Razer (MapLogo): " + ex.Message);
                }
        }

        public void ApplyMapMouseLighting(string region, Color col, bool clear)
        {
            uint rzCol = col.ToColoreColor();

            //Send Lighting
            if (_razerDeviceMouse)
                try
                {
                    if (!Enum.IsDefined(typeof(Led), region)) return;
                    
                    var regionid = (Led)Enum.Parse(typeof(Led), region);

                    if (regionid == Led.Backlight)
                    {
                        if (Mouse.Instance[GridLed.Backlight].Value != rzCol)
                        {
                            Mouse.Instance[GridLed.Backlight] = rzCol;
                        }
                        return;
                    }

                    if (Mouse.Instance[regionid].Value != rzCol)
                    {
                        Mouse.Instance[regionid] = rzCol;
                    }

                }
                catch (Exception ex)
                {
                    Write.WriteConsole(ConsoleTypes.Error, "Razer Mouse (" + region + "): " + ex.Message);
                }
        }

        public void ApplyMapPadLighting(int region, Color col, bool clear)
        {
            uint rzCol = col.ToColoreColor();

            try
            {
                if (_razerDeviceMousepad)
                    if (Mousepad.Instance[region].Value != rzCol)
                        Mousepad.Instance[region] = rzCol;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer Mousepad (" + region + "): " + ex.Message);
            }
        }

        public void ApplyMapHeadsetLighting(Color col, bool clear)
        {
            uint rzCol = col.ToColoreColor();

            try
            {
                if (_razerDeviceHeadset)
                        Headset.Instance.SetAll(rzCol);
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer Headset: " + ex.Message);
            }
        }

        public void ApplyMapKeypadLighting(Color col, bool clear)
        {
            uint rzCol = col.ToColoreColor();
            
            try
            {
                if (_razerDeviceKeypad)
                    if (Keypad.Instance[0,0].Value != rzCol)
                        Keypad.Instance.SetAll(rzCol);
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer Keypad: " + ex.Message);
            }
        }

        public void ApplyMapChromaLinkLighting(Color col, int pos)
        {
            if (pos >= Corale.Colore.Razer.ChromaLink.Constants.MaxLEDs) return;
            uint rzCol = col.ToColoreColor();
            
            try
            {
                if (_razerDeviceChromaLink)
                {
                    if (ChromaLink.Instance[pos].Value != rzCol)
                    {
                        ChromaLink.Instance[pos] = rzCol;
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer ChromaLink: " + ex.Message);
            }
        }

        public Task Ripple1(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                lock (RazerRipple1)
                {
                    if (!_razerDeviceKeyboard) return;
                    var presets = new Dictionary<string, Color>();
                    Custom refreshGrid;
                    refreshGrid = _keyboardGrid;

                    for (var i = 0; i <= 9; i++)
                    {
                        if (i == 0)
                        {
                            //Setup

                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var fkey = (Key) Enum.Parse(typeof(Key), key);
                                var cc = _keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                var ccX = cc
                                    .ToSystemColor(); //System.Drawing.Color.FromArgb(cc.R, cc.G, cc.B);
                                presets.Add(key, ccX);
                            }

                            //Keyboard.Instance.SetCustom(keyboard_custom);

                            //Chroma.Instance.Keyboard.SetCustom(keyboardGrid);
                            //HoldReader = true;
                        }
                        else if (i == 1)
                        {
                            //Step 0
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                if (pos > -1)
                                {
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                }
                                else
                                {
                                    //ApplyMapKeyLighting(key, presets[key], true);
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = presets[key].ToColoreColor();
                                    }
                                }
                            }
                        }
                        else if (i == 2)
                        {
                            //Step 1
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                if (pos > -1)
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = presets[key].ToColoreColor();
                                    }
                                }
                            }
                        }
                        else if (i == 3)
                        {
                            //Step 2
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                                if (pos > -1)
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = presets[key].ToColoreColor();
                                    }
                                }
                            }
                        }
                        else if (i == 4)
                        {
                            //Step 3
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                if (pos > -1)
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = presets[key].ToColoreColor();
                                    }
                                }
                            }
                        }
                        else if (i == 5)
                        {
                            //Step 4
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                if (pos > -1)
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = presets[key].ToColoreColor();
                                    }
                                }
                            }
                        }
                        else if (i == 6)
                        {
                            //Step 5
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                if (pos > -1)
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = presets[key].ToColoreColor();
                                    }
                                }
                            }
                        }
                        else if (i == 7)
                        {
                            //Step 6
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                if (pos > -1)
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = presets[key].ToColoreColor();
                                    }
                                }
                            }
                        }
                        else if (i == 8)
                        {
                            //Step 7
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                if (pos > -1)
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = presets[key].ToColoreColor();
                                    }
                                }
                            }
                        }
                        else if (i == 9)
                        {
                            //Spin down

                            foreach (var key in DeviceEffects.GlobalKeys)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    //ApplyMapKeyLighting(key, presets[key], true);
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshGrid[keyid] = presets[key].ToColoreColor();
                                }

                            presets.Clear();
                            //HoldReader = false;

                            //MemoryReaderLock.Enabled = true;
                        }

                        if (i < 9)
                            Thread.Sleep(speed);

                        Chroma.Instance.Keyboard.SetCustom(refreshGrid);
                    }
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                lock (RazerRipple2)
                {
                    if (!_razerDeviceKeyboard) return;
                    var presets = new Dictionary<string, Color>();
                    var refreshGrid = Custom.Create();
                    refreshGrid = _keyboardGrid;

                    for (var i = 0; i <= 9; i++)
                    {
                        if (i == 0)
                        {
                            //Setup

                            foreach (var key in DeviceEffects.GlobalKeys)
                                if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                {
                                    var fkey = (Key) Enum.Parse(typeof(Key), key);
                                    var cc = _keyboardGrid[fkey];
                                    var ccX = cc.ToSystemColor();
                                    presets.Add(key, ccX);
                                }

                            //Keyboard.Instance.SetCustom(keyboard_custom);

                            //HoldReader = true;
                        }
                        else if (i == 1)
                        {
                            //Step 0
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                if (pos > -1)
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);
                                            refreshGrid[keyid] = burstcol.ToColoreColor();
                                        }
                            }
                        }
                        else if (i == 2)
                        {
                            //Step 1
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                if (pos > -1)
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);
                                            refreshGrid[keyid] = burstcol.ToColoreColor();
                                        }
                            }
                        }
                        else if (i == 3)
                        {
                            //Step 2
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                                if (pos > -1)
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);
                                            refreshGrid[keyid] = burstcol.ToColoreColor();
                                        }
                            }
                        }
                        else if (i == 4)
                        {
                            //Step 3
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                if (pos > -1)
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);
                                            refreshGrid[keyid] = burstcol.ToColoreColor();
                                        }
                            }
                        }
                        else if (i == 5)
                        {
                            //Step 4
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                if (pos > -1)
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);
                                            refreshGrid[keyid] = burstcol.ToColoreColor();
                                        }
                            }
                        }
                        else if (i == 6)
                        {
                            //Step 5
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                if (pos > -1)
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);
                                            refreshGrid[keyid] = burstcol.ToColoreColor();
                                        }
                            }
                        }
                        else if (i == 7)
                        {
                            //Step 6
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                if (pos > -1)
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);
                                            refreshGrid[keyid] = burstcol.ToColoreColor();
                                        }
                            }
                        }
                        else if (i == 8)
                        {
                            //Step 7
                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                if (pos > -1)
                                {
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);
                                            refreshGrid[keyid] = burstcol.ToColoreColor();
                                        }
                                }
                            }
                        }
                        else if (i == 9)
                        {
                            //Spin down

                            foreach (var key in DeviceEffects.GlobalKeys)
                            {
                                //ApplyMapKeyLighting(key, presets[key], true);
                            }


                            //presets.Clear();
                            presets.Clear();
                            //HoldReader = false;
                        }

                        if (i < 9)
                            Thread.Sleep(speed);

                        Chroma.Instance.Keyboard.SetCustom(refreshGrid);
                    }
                }
            });
        }

        public void Flash1(Color burstcol, int speed, string[] region)
        {
            lock (RazerFlash1)
            {
                var presets = new Dictionary<string, Color>();
                var scrollWheel = new Corale.Colore.Core.Color();
                var logo = new Corale.Colore.Core.Color();
                var backlight = new Corale.Colore.Core.Color();
                var pad1 = new Corale.Colore.Core.Color();
                var pad2 = new Corale.Colore.Core.Color();

                var pad1Conv = Color.FromArgb(pad1.R, pad1.G, pad1.B);
                var pad2Conv = Color.FromArgb(pad2.R, pad2.G, pad2.B);
                var scrollWheelConv = Color.FromArgb(scrollWheel.R, scrollWheel.G, scrollWheel.B);
                var logoConv = Color.FromArgb(logo.R, logo.G, logo.B);
                var backlightConv = Color.FromArgb(backlight.R, backlight.G, backlight.B);

                Custom refreshKeyGrid;
                refreshKeyGrid = _keyboardGrid;

                for (var i = 0; i <= 8; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        if (_razerDeviceKeyboard)
                            foreach (var key in region)
                            {
                                var fkey = (Key) Enum.Parse(typeof(Key), key);
                                var cc = _keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                var ccX = cc.ToSystemColor();
                                presets.Add(key, ccX);
                            }

                        if (_razerDeviceMouse)
                        {
                            scrollWheel = Mouse.Instance[1];
                            logo = Mouse.Instance[2];
                            backlight = Mouse.Instance[3];
                            pad1 = Mousepad.Instance[7];
                            pad2 = Mousepad.Instance[14];
                        }
                        //Keyboard.Instance.SetCustom(keyboard_custom);

                        //HoldReader = true;
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        if (_razerDeviceKeyboard)
                            foreach (var key in region)
                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = burstcol.ToColoreColor();
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        if (_razerDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = presets[key].ToColoreColor();
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Pad1); }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        if (_razerDeviceKeyboard)
                            foreach (var key in region)
                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = burstcol.ToColoreColor();
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        if (_razerDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = presets[key].ToColoreColor();
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Pad1); }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        if (_razerDeviceKeyboard)
                            foreach (var key in region)
                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = burstcol.ToColoreColor();
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        if (_razerDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = presets[key].ToColoreColor();
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Pad1); }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        if (_razerDeviceKeyboard)
                            foreach (var key in region)
                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = burstcol.ToColoreColor();
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        if (_razerDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = presets[key].ToColoreColor();
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Pad1); }

                        presets.Clear();
                        //HoldReader = false;
                    }

                    if (i < 8)
                        Thread.Sleep(speed);

                    Chroma.Instance.Keyboard.SetCustom(refreshKeyGrid);
                }
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                lock (RazerFlash2)
                {
                    var flashpresets = new Dictionary<string, Color>();
                    var rzScrollWheel = new Corale.Colore.Core.Color();
                    var rzLogo = new Corale.Colore.Core.Color();
                    var rzScrollWheelConv = new Color();
                    var rzLogoConv = new Color();
                    var refreshKeyGrid = Custom.Create();
                    refreshKeyGrid = _keyboardGrid;


                    if (!_razerFlash2Running)
                    {
                        if (_razerDeviceMouse)
                        {
                            rzScrollWheel = Mouse.Instance[1];
                            rzLogo = Mouse.Instance[2];
                            rzScrollWheelConv = Color.FromArgb(rzScrollWheel.R, rzScrollWheel.G, rzScrollWheel.B);
                            rzLogoConv = Color.FromArgb(rzLogo.R, rzLogo.G, rzLogo.B);
                        }

                        if (_razerDeviceKeyboard)
                            foreach (var key in regions)
                            {
                                var fkey = (Key) Enum.Parse(typeof(Key), key);
                                var cc = _keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                var ccX = cc.ToSystemColor();
                                flashpresets.Add(key, ccX);
                            }

                        _razerFlash2Running = true;
                        _razerFlash2Step = 0;
                        _flashpresets = flashpresets;
                    }

                    if (_razerFlash2Running)
                        while (_razerFlash2Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_razerFlash2Step == 0)
                            {
                                if (_razerDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);

                                            refreshKeyGrid[keyid] = burstcol.ToColoreColor();
                                        }

                                    Chroma.Instance.Keyboard.SetCustom(refreshKeyGrid);
                                }

                                if (_razerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                                    ApplyMapMouseLighting("Logo", burstcol, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

                                _razerFlash2Step = 1;
                            }
                            else if (_razerFlash2Step == 1)
                            {
                                if (_razerDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);

                                            refreshKeyGrid[keyid] = _flashpresets[key].ToColoreColor();
                                        }

                                    Chroma.Instance.Keyboard.SetCustom(refreshKeyGrid);
                                }

                                if (_razerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", rzScrollWheelConv, false);
                                    ApplyMapMouseLighting("Logo", rzLogoConv, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

                                _razerFlash2Step = 0;
                            }

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
                //DeviceEffects._NumFlash
                lock (RazerFlash3)
                {
                    if (!_razerDeviceKeyboard) return;
                    var presets = new Dictionary<string, Color>();
                    var refreshGrid = Custom.Create();
                    refreshGrid = _keyboardGrid;
                    //Debug.WriteLine("Running Flash 3");
                    _razerFlash3Running = true;
                    _razerFlash3Step = 0;

                    if (_razerFlash3Running == false)
                    {
                        /*
                            foreach (string key in DeviceEffects._NumFlash)
                            {
                                Corale.Colore.Razer.Keyboard.Key fkey = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                Corale.Colore.Core.Color cc = keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                System.Drawing.Color ccX = Corale.Colore.WinForms.Extensions.ToSystemColor(cc);
                                presets.Add(key, ccX);
                            }
                            */
                    }
                    else
                    {
                        while (_razerFlash3Running)
                        {
                            //cts.ThrowIfCancellationRequested();

                            if (cts.IsCancellationRequested)
                                break;

                            if (_razerFlash3Step == 0)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = burstcol.ToColoreColor();
                                    }
                                _razerFlash3Step = 1;
                            }
                            else if (_razerFlash3Step == 1)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = Color.Black.ToColoreColor();
                                    }

                                _razerFlash3Step = 0;
                            }

                            Chroma.Instance.Keyboard.SetCustom(refreshGrid);
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
                lock (RazerFlash4)
                {
                    var flashpresets = new Dictionary<string, Color>();
                    var rzScrollWheel = new Corale.Colore.Core.Color();
                    var rzLogo = new Corale.Colore.Core.Color();
                    var rzScrollWheelConv = new Color();
                    var rzLogoConv = new Color();
                    var refreshKeyGrid = Custom.Create();
                    refreshKeyGrid = _keyboardGrid;


                    if (!_razerFlash4Running)
                    {
                        if (_razerDeviceMouse)
                        {
                            rzScrollWheel = Mouse.Instance[1];
                            rzLogo = Mouse.Instance[2];
                            rzScrollWheelConv = Color.FromArgb(rzScrollWheel.R, rzScrollWheel.G, rzScrollWheel.B);
                            rzLogoConv = Color.FromArgb(rzLogo.R, rzLogo.G, rzLogo.B);
                        }

                        if (_razerDeviceKeyboard)
                            foreach (var key in regions)
                            {
                                var fkey = (Key) Enum.Parse(typeof(Key), key);
                                var cc = _keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                var ccX = cc.ToSystemColor();
                                flashpresets.Add(key, ccX);
                            }

                        _razerFlash4Running = true;
                        _razerFlash4Step = 0;
                        _flashpresets4 = flashpresets;
                    }

                    if (_razerFlash4Running)
                        while (_razerFlash4Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_razerFlash4Step == 0)
                            {
                                if (_razerDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);

                                            refreshKeyGrid[keyid] = burstcol.ToColoreColor();
                                        }

                                    Chroma.Instance.Keyboard.SetCustom(refreshKeyGrid);
                                }

                                if (_razerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                                    ApplyMapMouseLighting("Logo", burstcol, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

                                _razerFlash4Step = 1;
                            }
                            else if (_razerFlash4Step == 1)
                            {
                                if (_razerDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (Enum.IsDefined(typeof(Key), key))
                                        {
                                            var keyid = (Key) Enum.Parse(typeof(Key), key);

                                            refreshKeyGrid[keyid] = _flashpresets4[key].ToColoreColor();
                                        }

                                    Chroma.Instance.Keyboard.SetCustom(refreshKeyGrid);
                                }

                                if (_razerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", rzScrollWheelConv, false);
                                    ApplyMapMouseLighting("Logo", rzLogoConv, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

                                _razerFlash4Step = 0;
                            }

                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
                //
            }
        }

        private void Razertransition(Color col, bool forward)
        {
            lock (_Razertransition)
            {
                if (_razerDeviceKeyboard)
                {
                    uint rzCol = col.ToColoreColor();
                    for (uint c = 0; c < Constants.MaxColumns; c++)
                    {
                        for (uint r = 0; r < Constants.MaxRows; r++)
                        {
                            var row = forward ? r : Constants.MaxRows - r - 1;
                            var colu = forward ? c : Constants.MaxColumns - c - 1;
                            Keyboard.Instance[Convert.ToInt32(row), Convert.ToInt32(colu)] = rzCol;
                        }
                        Thread.Sleep(15);
                    }
                }
            }
        }

        private void RazertransitionConst(Color col1, Color col2, bool forward, int speed)
        {
            lock (_RazertransitionConst)
            {
                if (_razerDeviceKeyboard)
                {
                    var i = 1;
                    uint rzCol = col1.ToColoreColor();
                    uint rzCol2 = col2.ToColoreColor();
                    var state = 0;

                    while (state == 6)
                    {
                        _rcts.Token.ThrowIfCancellationRequested();
                        if (i == 1)
                        {
                            for (uint c = 0; c < Constants.MaxColumns; c++)
                            {
                                for (uint r = 0; r < Constants.MaxRows; r++)
                                {
                                    if (state != 6) break;
                                    var row = forward ? r : Constants.MaxRows - r - 1;
                                    var colu = forward ? c : Constants.MaxColumns - c - 1;
                                    try
                                    {
                                        Keyboard.Instance[Convert.ToInt32(row), Convert.ToInt32(colu)] = rzCol;
                                    }
                                    catch (Exception)
                                    {
                                        //Debug.WriteLine(ex.Message);
                                        //rtb_console.AppendText(ex.Message + " \r\n");
                                    }
                                }
                                Thread.Sleep(speed);
                            }
                            i = 2;
                        }
                        else if (i == 2)
                        {
                            for (uint c = 0; c < Constants.MaxColumns; c++)
                            {
                                for (uint r = 0; r < Constants.MaxRows; r++)
                                {
                                    if (state != 6) break;
                                    var row = forward ? r : Constants.MaxRows - r - 1;
                                    var colu = forward ? c : Constants.MaxColumns - c - 1;
                                    try
                                    {
                                        Keyboard.Instance[Convert.ToInt32(row), Convert.ToInt32(colu)] = rzCol2;
                                    }
                                    catch (Exception)
                                    {
                                        //Debug.WriteLine(ex.Message);
                                        //rtb_console.AppendText(ex.Message + " \r\n");
                                    }
                                }
                                Thread.Sleep(speed);
                            }
                            i = 1;
                        }
                    }
                }
            }
        }

        
    }
}