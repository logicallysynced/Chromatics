using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.Controllers;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;
using Colore;
using Colore.Data;
using Colore.Effects.ChromaLink;
using Colore.Effects.Headset;
using Colore.Effects.Keyboard;
using Colore.Effects.Keypad;
using Colore.Effects.Mouse;
using Colore.Effects.Mousepad;
using Colore.Native;
using GalaSoft.MvvmLight.Ioc;
using Color = System.Drawing.Color;
using ColoreColor = Colore.Data.Color;

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
            RazerLib razer = null;
            razer = new RazerLib();

            var razerstat = razer.InitializeSdk();

            return !razerstat ? null : razer;
        }
    }

    public class RazerSdkWrapper
    {
        //
    }

    public interface IRazerSdk
    {
        bool InitializeSdk();
        void ShutdownSdk();
        void InitializeLights(Color initColor);

        void ResetRazerDevices(bool deviceKeyboard, bool deviceKeypad, bool deviceMouse, bool deviceMousepad,
            bool deviceHeadset, bool deviceChromaLink, Color basecol);

        void DeviceUpdate();
        void SetLights(Color col);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapLogoLighting(string key, Color col, bool clear);
        void ApplyMapMouseLighting(string key, Color col, bool clear);
        void ApplyMapPadLighting(int region, Color col, bool clear);

        void ApplyMapHeadsetLighting(Color col, bool clear);
        void ApplyMapKeypadLighting(Color col, bool clear);

        void ApplyMapChromaLinkLighting(Color col, int pos);

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

        //Corale.Colore.Core.Color Pad1 = new ColoreColor();
        private static readonly object RazerFlash2 = new object();

        private static int _razerFlash3Step;
        private static bool _razerFlash3Running;
        private static readonly object RazerFlash3 = new object();

        private static int _razerFlash4Step;
        private static bool _razerFlash4Running;

        private static Dictionary<string, Color> _flashpresets4 = new Dictionary<string, Color>();

        //Corale.Colore.Core.Color Pad1 = new ColoreColor();
        private static readonly object RazerFlash4 = new object();

        private static readonly object _RazertransitionConst = new object();

        private IChroma _IChroma;

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
        
        /*
        private readonly Dictionary<string, int> _padkeyids = new Dictionary<string, int>
        {
            {"D1", 0},
        }
        */


        private KeyboardCustom _keyboardGrid = KeyboardCustom.Create();
        private MouseCustom _mouseGrid = MouseCustom.Create();
        private MousepadCustom _mousepadGrid = MousepadCustom.Create();

        private IChroma Chroma;
        private IKeyboard Keyboard;
        private IMouse Mouse;
        private IHeadset Headset;
        private IMousepad Mousepad;
        private IKeypad Keypad;
        private IChromaLink ChromaLink;

        private bool _razerDeathstalker;
        private bool _razerDeviceHeadset = true;

        private bool _razerDeviceKeyboard = true;
        private bool _razerDeviceKeypad = true;
        private bool _razerDeviceMouse = true;
        private bool _razerDeviceMousepad = true;
        private bool _razerDeviceChromaLink = true;

        //Handle device send/recieve
        private readonly CancellationTokenSource _rcts = new CancellationTokenSource();

        private ColoreColor ToColoreCol(Color col)
        {
            var color = new ColoreColor((byte)col.R, (byte)col.G, (byte)col.B);
            return color;
        }
        
        private Color FromColoreCol(ColoreColor col)
        {
            return Color.FromArgb(255, col.R, col.G, col.B);
        }

        public void ShutdownSdk()
        {
            if (Chroma.Initialized)
            {
                Chroma.UninitializeAsync().RunSynchronously();
                Chroma = null;
            }
        }


        public bool InitializeSdk()
        {
            //var appInfo = new AppInfo("Chromatics", "Lighting effects for Final Fantasy XIV", "Roxas Keyheart", "hello@chromaticsffxiv.com", Category.Game);
            
            try
            {
                if (!File.Exists(Environment.GetEnvironmentVariable("ProgramW6432") + @"\Razer Chroma SDK\bin\RzChromaSDK64.dll"))
                {
                    //Razer SDK DLL Not Found

                    Write.WriteConsole(ConsoleTypes.Razer,
                        Environment.Is64BitOperatingSystem
                            ? "The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Uninstall any previous versions of Razer SDK & Synapse and then reinstall Razer Synapse."
                            : "The Razer SDK (RzChromaSDK.dll) Could not be found on this computer. Uninstall any previous versions of Razer SDK & Synapse and then reinstall Razer Synapse.");

                    return false;
                }

                //var task = Task.Run(ColoreProvider.CreateNativeAsync).GetAwaiter().GetResult();
                
                var task = ColoreProvider.CreateNativeAsync();
                task.Wait();
                Chroma = task.Result;

                Keyboard = Chroma.Keyboard;
                Mouse = Chroma.Mouse;
                Mousepad = Chroma.Mousepad;
                Headset = Chroma.Headset;
                ChromaLink = Chroma.ChromaLink;

                Write.WriteConsole(ConsoleTypes.Razer, "Razer SDK Loaded (" + Chroma.SdkVersion + ")");

                return true;
            }
            catch (Exception e)
            {
                if (e.InnerException != null) Write.WriteConsole(ConsoleTypes.Razer, e.InnerException.ToString());
                return false;
            }
            
        }

        public void ResetRazerDevices(bool deviceKeyboard, bool deviceKeypad, bool deviceMouse, bool deviceMousepad,
            bool deviceHeadset, bool deviceChromaLink, Color basecol)
        {
            if (_razerDeviceKeyboard && !deviceKeyboard)
                Keyboard.SetStaticAsync(new KeyboardStatic(ToColoreCol(basecol)));

            if (_razerDeviceMouse && !deviceMouse)
                Mouse.SetStaticAsync(new MouseStatic(ToColoreCol(basecol)));

            if (_razerDeviceMousepad && !deviceMousepad)
                Mousepad.SetStaticAsync(new MousepadStatic(ToColoreCol(basecol)));

            if (_razerDeviceHeadset && !deviceHeadset)
                Headset.SetStaticAsync(new HeadsetStatic(ToColoreCol(basecol)));

            if (_razerDeviceKeypad && !deviceKeypad)
                Keypad.SetStaticAsync(new KeypadStatic(ToColoreCol(basecol)));

            if (_razerDeviceChromaLink && !deviceChromaLink)
            {
                ChromaLink.SetStaticAsync(new ChromaLinkStatic(ToColoreCol(basecol)));
            }

            _razerDeviceKeyboard = deviceKeyboard;
            _razerDeviceKeypad = deviceKeypad;
            _razerDeviceMouse = deviceMouse;
            _razerDeviceMousepad = deviceMousepad;
            _razerDeviceHeadset = deviceHeadset;
            _razerDeviceChromaLink = deviceChromaLink;
            _razerDeathstalker = Keyboard.IsDeathstalkerConnected;

        }

        public void InitializeLights(Color initColor)
        {
            //Debug.WriteLine("Setting Razer Default");
            SetLights(initColor);

        }

        public void SetLights(Color col)
        {
            if (!_razerDeviceKeyboard) return;

            /*
            var eff = new Static(ToColoreCol(col));
            Keyboard.SetStatic(eff);

            Keyboard.SetAllAsync(ToColoreCol(col));
            */

            lock (RazerFlash1)
            {
                _keyboardGrid.Set(ToColoreCol(col));
            }
        }
        
        public void SetWave()
        {
            try
            {
                /*
                if (_razerDeviceHeadset)
                {
                    Headset.SetEffectAsync(HeadsetEffect.None);
                }

                if (_razerDeviceKeyboard)
                {
                    Keyboard.SetEffectAsync(Key)
                }

                if (_razerDeviceKeypad)
                {
                    Keypad.SetWave(Corale.Colore.Razer.Keypad.Effects.Direction.LeftToRight);
                }

                if (_razerDeviceMouse)
                {
                    Mouse.SetWave(Corale.Colore.Razer.Mouse.Effects.Direction.FrontToBack);
                }

                if (_razerDeviceMousepad)
                {
                    Mousepad.Instance.SetWave(Corale.Colore.Razer.Mousepad.Effects.Direction.LeftToRight);
                }

                
                if (_razerDeviceChromaLink)
                    ChromaLink.SetEffect(Corale.Colore.Razer.ChromaLink.Effects.Effect.SpectrumCycling);
                */
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer (Wave): " + ex.Message);
            }
        }

        public void DeviceUpdate()
        {
            lock (RazerFlash1)
            {
                Chroma.Keyboard.SetCustomAsync(_keyboardGrid);
            }

            //Chroma.Mouse.SetCustomAsync(_mouseGrid);
            Chroma.Mousepad.SetCustomAsync(_mousepadGrid);
        }

        public void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            //keyboardGrid
            uint rzCol = ToColoreCol(col);

            //Send Lighting
            if (_razerDeviceKeyboard)
                try
                {
                    if (Enum.IsDefined(typeof(Key), key))
                    {
                        var keyid = (Key) Enum.Parse(typeof(Key), key);

                        if (clear)
                        {
                            if (Keyboard[keyid].Value != rzCol)
                                Keyboard.SetKeyAsync(keyid, rzCol, clear);
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
            uint rzCol = ToColoreCol(col);

            //Send Lighting
            lock (_Razertransition)
            {
                if (_razerDeviceKeyboard)
                    try
                    {
                        if (_keyboardGrid[Key.Logo].Value != rzCol)
                            _keyboardGrid[Key.Logo] = rzCol;
                    }
                    catch (Exception ex)
                    {
                        Write.WriteConsole(ConsoleTypes.Error, "Razer (MapLogo): " + ex.Message);
                    }
            }
        }

        public void ApplyMapMouseLighting(string region, Color col, bool clear)
        {
            uint rzCol = ToColoreCol(col);

            //Send Lighting
            if (_razerDeviceMouse)
                try
                {
                    if (!Enum.IsDefined(typeof(GridLed), region)) return;
                    
                    var regionid = (GridLed)Enum.Parse(typeof(GridLed), region);

                    if (regionid == GridLed.LeftSide1 || regionid == GridLed.LeftSide2 || regionid == GridLed.LeftSide3 ||
                        regionid == GridLed.LeftSide4 || regionid == GridLed.LeftSide5 || regionid == GridLed.LeftSide6 ||
                        regionid == GridLed.LeftSide7 || regionid == GridLed.RightSide1
                        || regionid == GridLed.RightSide2 || regionid == GridLed.RightSide3 || regionid == GridLed.RightSide4 ||
                        regionid == GridLed.RightSide5 || regionid == GridLed.RightSide6 || regionid == GridLed.RightSide7)
                    {
                        _mouseGrid[regionid] = rzCol;
                        return;
                    }

                    if (_mouseGrid[regionid].Value != rzCol)
                        _mouseGrid[regionid] = rzCol;

                }
                catch (Exception ex)
                {
                    Write.WriteConsole(ConsoleTypes.Error, "Razer Mouse (" + region + "): " + ex.Message);
                }
        }

        public void ApplyMapPadLighting(int region, Color col, bool clear)
        {
            uint rzCol = ToColoreCol(col);

            try
            {
                if (_razerDeviceMousepad)
                    if (_mousepadGrid[region].Value != rzCol)
                        _mousepadGrid[region] = rzCol;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer Mousepad (" + region + "): " + ex.Message);
            }
        }

        public void ApplyMapHeadsetLighting(Color col, bool clear)
        {
            uint rzCol = ToColoreCol(col);

            try
            {
                if (_razerDeviceHeadset)
                        Headset.SetAllAsync(rzCol);
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer Headset: " + ex.Message);
            }
        }

        public void ApplyMapKeypadLighting(Color col, bool clear)
        {
            uint rzCol = ToColoreCol(col);
            
            try
            {
                if (_razerDeviceKeypad)
                    if (Keypad[0,0].Value != rzCol)
                        Keypad.SetAllAsync(rzCol);
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Razer Keypad: " + ex.Message);
            }
        }

        public void ApplyMapChromaLinkLighting(Color col, int pos)
        {
            if (pos >= ChromaLinkConstants.MaxLeds) return;
            uint rzCol = ToColoreCol(col);
            
            try
            {
                if (_razerDeviceChromaLink)
                {
                    if (ChromaLink[pos].Value != rzCol)
                    {
                        ChromaLink[pos] = rzCol;
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
                    KeyboardCustom refreshGrid;
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
                                var ccX = FromColoreCol(cc);
                                    //.ToSystemColor(); //System.Drawing.Color.FromArgb(cc.R, cc.G, cc.B);
                                presets.Add(key, ccX);
                            }

                            //Keyboard.SetCustom(keyboard_custom);

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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                }
                                else
                                {
                                    //ApplyMapKeyLighting(key, presets[key], true);
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ToColoreCol(presets[key]);
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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ToColoreCol(presets[key]);
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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ToColoreCol(presets[key]);
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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ToColoreCol(presets[key]);
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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ToColoreCol(presets[key]);
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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ToColoreCol(presets[key]);
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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ToColoreCol(presets[key]);
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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                }
                                else
                                {
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ToColoreCol(presets[key]);
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
                                    refreshGrid[keyid] = ToColoreCol(presets[key]);
                                }

                            presets.Clear();
                            //HoldReader = false;

                            //MemoryReaderLock.Enabled = true;
                        }

                        if (i < 9)
                            Thread.Sleep(speed);

                        Keyboard.SetCustomAsync(refreshGrid);
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
                    var refreshGrid = KeyboardCustom.Create();
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
                                    var ccX = FromColoreCol(cc);
                                    presets.Add(key, ccX);
                                }

                            //Keyboard.SetCustom(keyboard_custom);

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
                                            refreshGrid[keyid] = ToColoreCol(burstcol);
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
                                            refreshGrid[keyid] = ToColoreCol(burstcol);
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
                                            refreshGrid[keyid] = ToColoreCol(burstcol);
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
                                            refreshGrid[keyid] = ToColoreCol(burstcol);
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
                                            refreshGrid[keyid] = ToColoreCol(burstcol);
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
                                            refreshGrid[keyid] = ToColoreCol(burstcol);
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
                                            refreshGrid[keyid] = ToColoreCol(burstcol);
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
                                            refreshGrid[keyid] = ToColoreCol(burstcol);
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

                        Keyboard.SetCustomAsync(refreshGrid);
                    }
                }
            });
        }

        public void Flash1(Color burstcol, int speed, string[] region)
        {
            lock (RazerFlash1)
            {
                var presets = new Dictionary<string, Color>();
                var scrollWheel = new ColoreColor();
                var logo = new ColoreColor();
                var backlight = new ColoreColor();
                var pad1 = new ColoreColor();
                var pad2 = new ColoreColor();

                var pad1Conv = Color.FromArgb(pad1.R, pad1.G, pad1.B);
                var pad2Conv = Color.FromArgb(pad2.R, pad2.G, pad2.B);
                var scrollWheelConv = Color.FromArgb(scrollWheel.R, scrollWheel.G, scrollWheel.B);
                var logoConv = Color.FromArgb(logo.R, logo.G, logo.B);
                var backlightConv = Color.FromArgb(backlight.R, backlight.G, backlight.B);

                KeyboardCustom refreshKeyGrid;
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
                                var ccX = FromColoreCol(cc);
                                presets.Add(key, ccX);
                            }

                        if (_razerDeviceMouse)
                        {
                            /*
                            scrollWheel = Mouse.Instance[1];
                            logo = Mouse.Instance[2];
                            backlight = Mouse.Instance[3];
                            pad1 = Mousepad.Instance[7];
                            pad2 = Mousepad.Instance[14];
                            */
                        }
                        //Keyboard.SetCustom(keyboard_custom);

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
                                    refreshKeyGrid[keyid] = ToColoreCol(burstcol);
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.SetAllAsync(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        if (_razerDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = ToColoreCol(presets[key]);
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.SetAllAsync(Pad1); }
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
                                    refreshKeyGrid[keyid] = ToColoreCol(burstcol);
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.SetAllAsync(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        if (_razerDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = ToColoreCol(presets[key]);
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.SetAllAsync(Pad1); }
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
                                    refreshKeyGrid[keyid] = ToColoreCol(burstcol);
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.SetAllAsync(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        if (_razerDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = ToColoreCol(presets[key]);
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.SetAllAsync(Pad1); }
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
                                    refreshKeyGrid[keyid] = ToColoreCol(burstcol);
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.SetAllAsync(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        if (_razerDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (Enum.IsDefined(typeof(Key), key))
                                {
                                    var keyid = (Key) Enum.Parse(typeof(Key), key);
                                    refreshKeyGrid[keyid] = ToColoreCol(presets[key]);
                                }

                        if (_razerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.SetAllAsync(Pad1); }

                        presets.Clear();
                        //HoldReader = false;
                    }

                    if (i < 8)
                        Thread.Sleep(speed);

                    Keyboard.SetCustomAsync(refreshKeyGrid);
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
                    var rzScrollWheel = new ColoreColor();
                    var rzLogo = new ColoreColor();
                    var rzScrollWheelConv = new Color();
                    var rzLogoConv = new Color();
                    var refreshKeyGrid = KeyboardCustom.Create();

                    refreshKeyGrid = _keyboardGrid;

                    if (!_razerFlash2Running)
                    {
                        if (_razerDeviceMouse)
                        {
                            rzScrollWheel = Mouse[GridLed.ScrollWheel];
                            rzLogo = Mouse[GridLed.Logo];
                            rzScrollWheelConv = Color.FromArgb(rzScrollWheel.R, rzScrollWheel.G, rzScrollWheel.B);
                            rzLogoConv = Color.FromArgb(rzLogo.R, rzLogo.G, rzLogo.B);
                        }

                        if (_razerDeviceKeyboard)
                            foreach (var key in regions)
                            {
                                var fkey = (Key) Enum.Parse(typeof(Key), key);
                                var cc = _keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                var ccX = FromColoreCol(cc);
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

                                            refreshKeyGrid[keyid] = ToColoreCol(burstcol);
                                        }

                                    Keyboard.SetCustomAsync(refreshKeyGrid);
                                }

                                if (_razerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                                    ApplyMapMouseLighting("Logo", burstcol, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.SetAllAsync(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

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

                                            refreshKeyGrid[keyid] = ToColoreCol(_flashpresets[key]);
                                        }

                                    Keyboard.SetCustomAsync(refreshKeyGrid);
                                }

                                if (_razerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", rzScrollWheelConv, false);
                                    ApplyMapMouseLighting("Logo", rzLogoConv, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.SetAllAsync(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

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
                    var refreshGrid = KeyboardCustom.Create();
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
                                        refreshGrid[keyid] = ToColoreCol(burstcol);
                                    }
                                _razerFlash3Step = 1;
                            }
                            else if (_razerFlash3Step == 1)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                    if (Enum.IsDefined(typeof(Key), key))
                                    {
                                        var keyid = (Key) Enum.Parse(typeof(Key), key);
                                        refreshGrid[keyid] = ColoreColor.Black;
                                    }

                                _razerFlash3Step = 0;
                            }

                            Keyboard.SetCustomAsync(refreshGrid);
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
                    var rzScrollWheel = new ColoreColor();
                    var rzLogo = new ColoreColor();
                    var rzScrollWheelConv = new Color();
                    var rzLogoConv = new Color();
                    var refreshKeyGrid = KeyboardCustom.Create();
                    refreshKeyGrid = _keyboardGrid;


                    if (!_razerFlash4Running)
                    {
                        if (_razerDeviceMouse)
                        {
                            rzScrollWheel = Mouse[GridLed.ScrollWheel];
                            rzLogo = Mouse[GridLed.Logo];
                            rzScrollWheelConv = Color.FromArgb(rzScrollWheel.R, rzScrollWheel.G, rzScrollWheel.B);
                            rzLogoConv = Color.FromArgb(rzLogo.R, rzLogo.G, rzLogo.B);
                        }

                        if (_razerDeviceKeyboard)
                            foreach (var key in regions)
                            {
                                var fkey = (Key) Enum.Parse(typeof(Key), key);
                                var cc = _keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                var ccX = FromColoreCol(cc);
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

                                            refreshKeyGrid[keyid] = ToColoreCol(burstcol);
                                        }

                                    Keyboard.SetCustomAsync(refreshKeyGrid);
                                }

                                if (_razerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                                    ApplyMapMouseLighting("Logo", burstcol, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.SetAllAsync(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

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

                                            refreshKeyGrid[keyid] = ToColoreCol(_flashpresets4[key]);
                                        }

                                    Keyboard.SetCustomAsync(refreshKeyGrid);
                                }

                                if (_razerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", rzScrollWheelConv, false);
                                    ApplyMapMouseLighting("Logo", rzLogoConv, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.SetAllAsync(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

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
                    uint rzCol = ToColoreCol(col);
                    for (uint c = 0; c < KeyboardConstants.MaxColumns; c++)
                    {
                        for (uint r = 0; r < KeyboardConstants.MaxRows; r++)
                        {
                            var row = forward ? r : KeyboardConstants.MaxRows - r - 1;
                            var colu = forward ? c : KeyboardConstants.MaxColumns - c - 1;
                            Keyboard[Convert.ToInt32(row), Convert.ToInt32(colu)] = rzCol;
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
                    uint rzCol = ToColoreCol(col1);
                    uint rzCol2 = ToColoreCol(col2);
                    var state = 0;

                    while (state == 6)
                    {
                        _rcts.Token.ThrowIfCancellationRequested();
                        if (i == 1)
                        {
                            for (uint c = 0; c < KeyboardConstants.MaxColumns; c++)
                            {
                                for (uint r = 0; r < KeyboardConstants.MaxRows; r++)
                                {
                                    if (state != 6) break;
                                    var row = forward ? r : KeyboardConstants.MaxRows - r - 1;
                                    var colu = forward ? c : KeyboardConstants.MaxColumns - c - 1;
                                    try
                                    {
                                        Keyboard[Convert.ToInt32(row), Convert.ToInt32(colu)] = rzCol;
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
                            for (uint c = 0; c < KeyboardConstants.MaxColumns; c++)
                            {
                                for (uint r = 0; r < KeyboardConstants.MaxRows; r++)
                                {
                                    if (state != 6) break;
                                    var row = forward ? r : KeyboardConstants.MaxRows - r - 1;
                                    var colu = forward ? c : KeyboardConstants.MaxColumns - c - 1;
                                    try
                                    {
                                        Keyboard[Convert.ToInt32(row), Convert.ToInt32(colu)] = rzCol2;
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