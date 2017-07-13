using Corale.Colore.Core;
using Corale.Colore.Razer.Mouse;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;

/* Contains all Razer SDK code (via Colore) for detection, initilization, states and effects.
 * Corale.Colore.dll is used to port the Razer SDK into C#
 * https://github.com/CoraleStudios/Colore
 */

namespace Chromatics.DeviceInterfaces
{
    public class RazerInterface
    {
        private static ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static RazerLib InitializeRazerSDK()
        {
            RazerLib razer = null;
            
            razer = new RazerLib();

            string arch = "";
            //string arch = @"C:\Program Files\Razer Chroma SDK\bin\RzChromaSDK64.dll";

            if (Environment.Is64BitOperatingSystem == true)
            {
                arch = Environment.GetEnvironmentVariable("ProgramW6432") + @"\Razer Chroma SDK\bin\RzChromaSDK64.dll";
                write.WriteConsole(ConsoleTypes.SYSTEM, "Architecture: x64");
            }
            else
            {
                arch = Environment.GetEnvironmentVariable("ProgramFiles(x86)") + @"\Razer Chroma SDK\bin\RzChromaSDK.dll";
                write.WriteConsole(ConsoleTypes.SYSTEM, "Architecture: x86");
            }

            if (File.Exists(arch))
            {
                write.WriteConsole(ConsoleTypes.RAZER, "Razer SDK Detected: " + arch);

                if (Chroma.SdkAvailable == false)
                {
                    write.WriteConsole(ConsoleTypes.RAZER, "Razer SDK not found");
                    return null;
                }
            }
            else
            {
                //Razer SDK DLL Not Found

                if (Environment.Is64BitOperatingSystem == true)
                {
                    write.WriteConsole(ConsoleTypes.RAZER, "The Razer SDK (RzChromaSDK64.dll) Could not be found on this computer. Please install Synapse and restart ACT if you wish to use Chromatics with Razer devices.");
                }
                else
                {
                    write.WriteConsole(ConsoleTypes.RAZER, "The Razer SDK (RzChromaSDK.dll) Could not be found on this computer. Please install Synapse and restart ACT if you wish to use Chromatics with Razer devices.");
                }

                return null;
            }


            write.WriteConsole(ConsoleTypes.RAZER, "Start Colore Setup");

            if (Chroma.Instance.Initialized != true)
            {
                write.WriteConsole(ConsoleTypes.RAZER, "Attempting to load Colore..");
                Chroma.Instance.Initialize();
            }
            else
            {
                write.WriteConsole(ConsoleTypes.RAZER, "Colore Already loaded.");
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
        void ResetRazerDevices(bool DeviceKeyboard, bool DeviceKeypad, bool DeviceMouse, bool DeviceMousepad, bool DeviceHeadset, System.Drawing.Color basecol);
        void KeyboardUpdate();
        void ApplyMapKeyLighting(string key, System.Drawing.Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapLogoLighting(string key, System.Drawing.Color col, bool clear);
        void ApplyMapMouseLighting(string key, System.Drawing.Color col, bool clear);
        void ApplyMapPadLighting(int region, System.Drawing.Color col, bool clear);

        void UpdateState(string type, System.Drawing.Color col, bool disablekeys,
            [Optional] System.Drawing.Color col2, [Optional] bool direction, [Optional] int speed);

        Task Ripple1(System.Drawing.Color burstcol, int speed);

        Task Ripple2(System.Drawing.Color burstcol, int speed);

        void Flash1(System.Drawing.Color burstcol, int speed, string[] regions);
        void Flash2(System.Drawing.Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(System.Drawing.Color burstcol, int speed, CancellationToken cts);
    }

    public class RazerLib : IRazerSdk
    {
        private static ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private readonly Dictionary<string, string> Razerkeyids = new Dictionary<string, string>()
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

        private bool RazerDeviceKeyboard = true;
        private bool RazerDeviceKeypad = true;
        private bool RazerDeviceMouse = true;
        private bool RazerDeviceMousepad = true;
        private bool RazerDeviceHeadset = true;
        private bool RazerDeathstalker = false;

        private Corale.Colore.Razer.Keyboard.Effects.Custom keyboardGrid = Corale.Colore.Razer.Keyboard.Effects.Custom.Create();
        private Corale.Colore.Razer.Mouse.Effects.Custom mouseGrid = Corale.Colore.Razer.Mouse.Effects.Custom.Create();
        private Corale.Colore.Razer.Mousepad.Effects.Custom mousepadGrid = Corale.Colore.Razer.Mousepad.Effects.Custom.Create();
        
        public void ResetRazerDevices(bool DeviceKeyboard, bool DeviceKeypad, bool DeviceMouse, bool DeviceMousepad, bool DeviceHeadset, System.Drawing.Color basecol)
        {
            RazerDeviceKeyboard = DeviceKeyboard;
            RazerDeviceKeypad = DeviceKeypad;
            RazerDeviceMouse = DeviceMouse;
            RazerDeviceMousepad = DeviceMousepad;
            RazerDeviceHeadset = DeviceHeadset;
            RazerDeathstalker = Chroma.Instance.Query(Corale.Colore.Razer.Devices.Deathstalker).Connected;

            if (RazerDeviceKeyboard)
            {
                UpdateState("static", basecol, false);
            }
        }

        public void InitializeLights()
        {
            //Debug.WriteLine("Setting Razer Default");
            UpdateState("static", System.Drawing.Color.DeepSkyBlue, false);
        }

        //Handle device send/recieve
        private CancellationTokenSource RCTS = new CancellationTokenSource();
        public void UpdateState(string type, System.Drawing.Color col, bool disablekeys, [Optional]System.Drawing.Color col2, [Optional]bool direction, [Optional]int speed)
        {
            MemoryTasks.Cleanup();
            uint RzCol = Corale.Colore.WinForms.Extensions.ToColoreColor(col);
            uint RzCol2 = Corale.Colore.WinForms.Extensions.ToColoreColor(col2);

            if (type == "reset")
            {
                //
            }
            else if (type == "static")
            {
                Task _RzSt = new Task(() =>
                {
                    try
                    {
                        if (RazerDeviceHeadset == true) { Headset.Instance.SetAll(RzCol); }
                        if (RazerDeviceKeyboard == true && disablekeys != true)
                        {
                            //Keyboard.Instance.SetAll(RzCol);
                            keyboardGrid.Set(RzCol);
                        }
                        if (RazerDeviceKeypad == true) { Keypad.Instance.SetAll(RzCol); }
                        if (RazerDeviceMouse == true)
                        {
                            //Mouse.Instance.SetAll(RzCol);
                            ApplyMapMouseLighting("ScrollWheel", col, false);
                            ApplyMapMouseLighting("Logo", col, false);
                            ApplyMapMouseLighting("Backlight", col, false);
                        }
                        if (RazerDeviceMousepad == true) { Mousepad.Instance.SetAll(RzCol); }
                    }
                    catch (Exception ex) {
                        write.WriteConsole(ConsoleTypes.ERROR, "Razer (Static): " + ex.Message); 
                    }
                });
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
            }
            else if (type == "transition")
            {
                Task _RzSt = new Task(() =>
                {
                    if (RazerDeviceHeadset == true) { Headset.Instance.SetAll(RzCol); }
                    if (RazerDeviceKeyboard == true && disablekeys != true)
                    {
                        Razertransition(col, direction);
                    }
                    if (RazerDeviceKeypad == true) { Keypad.Instance.SetAll(RzCol); }
                    if (RazerDeviceMouse == true) { Mouse.Instance.SetAll(RzCol); }
                    if (RazerDeviceMousepad == true) { Mousepad.Instance.SetAll(RzCol); }
                });
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
            }
            else if (type == "wave")
            {
                Task _RzSt = new Task(() =>
                {
                    try
                    {
                        if (RazerDeviceHeadset == true) { Headset.Instance.SetEffect(Corale.Colore.Razer.Headset.Effects.Effect.SpectrumCycling); }
                        if (RazerDeviceKeyboard == true && !RazerDeathstalker && disablekeys != true) { Keyboard.Instance.SetWave(Corale.Colore.Razer.Keyboard.Effects.Direction.LeftToRight); }
                        if (RazerDeviceKeypad == true) { Keypad.Instance.SetWave(Corale.Colore.Razer.Keypad.Effects.Direction.LeftToRight); }
                        if (RazerDeviceMouse == true) { Mouse.Instance.SetWave(Corale.Colore.Razer.Mouse.Effects.Direction.FrontToBack); }
                        if (RazerDeviceMousepad == true) { Mousepad.Instance.SetWave(Corale.Colore.Razer.Mousepad.Effects.Direction.LeftToRight); }
                    }
                    catch (Exception ex) {
                        write.WriteConsole(ConsoleTypes.ERROR, "Razer (Wave): " + ex.Message);
                    }
                });
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
            }
            else if (type == "breath")
            {
                Task _RzSt = new Task(() =>
                {
                    try
                    {
                        if (RazerDeviceHeadset == true) { Headset.Instance.SetBreathing(RzCol); }
                        if (RazerDeviceKeypad == true) { Keypad.Instance.SetBreathing(RzCol, RzCol2); }
                        if (RazerDeviceMouse == true)
                        {
                            Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.Backlight);
                            Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.Logo);
                            Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.ScrollWheel);
                        }
                        if (RazerDeviceMousepad == true) { Mousepad.Instance.SetBreathing(RzCol, RzCol2); }
                        if (RazerDeviceKeyboard == true && disablekeys != true) { Keyboard.Instance.SetBreathing(RzCol, RzCol2); }
                    }
                    catch (Exception ex) {
                        write.WriteConsole(ConsoleTypes.ERROR, "Razer (Breath): " + ex.Message); 
                    }
                });
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);

            }
            else if (type == "pulse")
            {
                Task _RzSt = new Task(() =>
                {
                    if (RazerDeviceHeadset == true) { Headset.Instance.SetAll(RzCol); }
                    if (RazerDeviceKeyboard == true && disablekeys != true)
                    {
                        RazertransitionConst(col, col2, true, speed);

                    }
                    if (RazerDeviceKeypad == true) { Keypad.Instance.SetAll(RzCol); }
                    if (RazerDeviceMouse == true) { Mouse.Instance.SetAll(RzCol); }
                    if (RazerDeviceMousepad == true) { Mousepad.Instance.SetAll(RzCol); }
                }, RCTS.Token);
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
                //RzPulse = true;
            }

            MemoryTasks.Cleanup();
        }

        public void KeyboardUpdate()
        {
            Chroma.Instance.Keyboard.SetCustom(keyboardGrid);
        }

        public void ApplyMapKeyLighting(string key, System.Drawing.Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (FFXIVHotbar.keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            //keyboardGrid
            uint RzCol = Corale.Colore.WinForms.Extensions.ToColoreColor(col);

            //Send Lighting
            if (RazerDeviceKeyboard == true)
            {
                try
                {
                    if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                    {
                        Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);

                        if (clear)
                        {
                            if (Keyboard.Instance[keyid].Value != RzCol)
                            {
                                Keyboard.Instance.SetKey(keyid, RzCol, clear);
                            }
                        }
                        else
                        {
                            if (keyboardGrid[keyid].Value != RzCol)
                            {
                                keyboardGrid[keyid] = RzCol;
                            }
                        }

                    }
                }
                catch (Exception ex) {
                    write.WriteConsole(ConsoleTypes.ERROR, "Razer Keyboard (" + key + "): " + ex.Message); 
                }
            }
        }

        public void ApplyMapLogoLighting(string key, System.Drawing.Color col, bool clear)
        {

            uint RzCol = Corale.Colore.WinForms.Extensions.ToColoreColor(col);

            //Send Lighting
            if (RazerDeviceKeyboard == true)
            {
                try
                {
                    if (Keyboard.Instance[0, 20].Value != RzCol)
                    {
                        Keyboard.Instance.SetPosition(0, 20, RzCol, clear);
                    }
                }
                catch (Exception ex) {
                    write.WriteConsole(ConsoleTypes.ERROR, "Razer (MapLogo): " + ex.Message); 
                }
            }
        }

        public void ApplyMapMouseLighting(string region, System.Drawing.Color col, bool clear)
        {

            uint RzCol = Corale.Colore.WinForms.Extensions.ToColoreColor(col);

            //Send Lighting
            if (RazerDeviceMouse == true)
            {
                try
                {
                    if (region == "Backlight")
                    {
                        if (Mouse.Instance[GridLed.Backlight].Value != RzCol)
                        {
                            Corale.Colore.Core.Mouse.Instance[GridLed.Backlight] = RzCol;
                        }
                    }
                    else
                    {
                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Mouse.Led), region))
                        {
                            Corale.Colore.Razer.Mouse.Led regionid = (Corale.Colore.Razer.Mouse.Led)Enum.Parse(typeof(Corale.Colore.Razer.Mouse.Led), region);
                            if (Mouse.Instance[regionid].Value != RzCol)
                            {
                                Mouse.Instance.SetLed(regionid, RzCol, clear);
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    write.WriteConsole(ConsoleTypes.ERROR, "Razer Mouse (" + region + "): " + ex.Message); 
                }
            }
        }

        public void ApplyMapPadLighting(int region, System.Drawing.Color col, bool clear)
        {
            uint RzCol = Corale.Colore.WinForms.Extensions.ToColoreColor(col);

            try
            {
                if (RazerDeviceMousepad == true)
                {
                    if (Mousepad.Instance[region].Value != RzCol)
                    {
                        Corale.Colore.Core.Mousepad.Instance[region] = RzCol;
                    }
                }
            }
            catch (Exception ex) {
                write.WriteConsole(ConsoleTypes.ERROR, "Razer Mousepad (" + region + "): " + ex.Message); 
            }
        }

        static readonly object _Razertransition = new object();
        private void Razertransition(System.Drawing.Color col, bool forward)
        {
            lock (_Razertransition)
            {
                if (RazerDeviceKeyboard == true)
                {
                    uint RzCol = Corale.Colore.WinForms.Extensions.ToColoreColor(col);
                    for (uint c = 0; c < Corale.Colore.Razer.Keyboard.Constants.MaxColumns; c++)
                    {
                        for (uint r = 0; r < Corale.Colore.Razer.Keyboard.Constants.MaxRows; r++)
                        {
                            var row = (forward) ? r : Corale.Colore.Razer.Keyboard.Constants.MaxRows - r - 1;
                            var colu = (forward) ? c : Corale.Colore.Razer.Keyboard.Constants.MaxColumns - c - 1;
                            Keyboard.Instance[Convert.ToInt32(row), Convert.ToInt32(colu)] = RzCol;
                        }
                        Thread.Sleep(15);
                    }
                }
            }
        }

        static readonly object _RazerRipple1 = new object();
        public Task Ripple1(System.Drawing.Color burstcol, int speed)
        {
            return new Task(() =>
            {
                lock (_RazerRipple1)
                {
                    if (RazerDeviceKeyboard == true)
                    {
                        Dictionary<string, System.Drawing.Color> presets = new Dictionary<string, System.Drawing.Color>();
                        var refreshGrid = Corale.Colore.Razer.Keyboard.Effects.Custom.Create();
                        refreshGrid = keyboardGrid;

                        for (int i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup
                                
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    Corale.Colore.Razer.Keyboard.Key fkey = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    Corale.Colore.Core.Color cc = keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                    System.Drawing.Color ccX = Corale.Colore.WinForms.Extensions.ToSystemColor(cc); //System.Drawing.Color.FromArgb(cc.R, cc.G, cc.B);
                                    presets.Add(key, ccX);
                                }

                                //Keyboard.Instance.SetCustom(keyboard_custom);

                                //Chroma.Instance.Keyboard.SetCustom(keyboardGrid);
                                //HoldReader = true;

                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep0, key);
                                    if (pos > -1)
                                    {
                                        //ApplyMapKeyLighting(key, burstcol, true);
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    else
                                    {
                                        //ApplyMapKeyLighting(key, presets[key], true);
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                    }
                                }
                            }
                            else if (i == 2)
                            {
                                //Step 1
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep1, key);
                                    if (pos > -1)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    else
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                    }
                                }
                            }
                            else if (i == 3)
                            {
                                //Step 2
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep2, key);
                                    if (pos > -1)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    else
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                    }
                                }
                            }
                            else if (i == 4)
                            {
                                //Step 3
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep3, key);
                                    if (pos > -1)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    else
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                    }
                                }
                            }
                            else if (i == 5)
                            {
                                //Step 4
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep4, key);
                                    if (pos > -1)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    else
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                    }
                                }
                            }
                            else if (i == 6)
                            {
                                //Step 5
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep5, key);
                                    if (pos > -1)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    else
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                    }
                                }
                            }
                            else if (i == 7)
                            {
                                //Step 6
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep6, key);
                                    if (pos > -1)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    else
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                    }
                                }
                            }
                            else if (i == 8)
                            {
                                //Step 7
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep7, key);
                                    if (pos > -1)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    else
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                    {
                                        //ApplyMapKeyLighting(key, presets[key], true);
                                        Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                        refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                    }
                                }

                                presets.Clear();
                                //HoldReader = false;

                                //MemoryReaderLock.Enabled = true;
                            }

                            if (i < 9)
                            {
                                Thread.Sleep(speed);
                            }

                            Chroma.Instance.Keyboard.SetCustom(refreshGrid);
                        }
                    }
                }
            });
        }


        static readonly object _RazerRipple2 = new object();
        public Task Ripple2(System.Drawing.Color burstcol, int speed)
        {
            return new Task(() =>
            {
                lock (_RazerRipple2)
                {
                    if (RazerDeviceKeyboard == true)
                    {
                        Dictionary<string, System.Drawing.Color> presets = new Dictionary<string, System.Drawing.Color>();
                        var refreshGrid = Corale.Colore.Razer.Keyboard.Effects.Custom.Create();
                        refreshGrid = keyboardGrid;

                        for (int i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                    {
                                        Corale.Colore.Razer.Keyboard.Key fkey = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                        Corale.Colore.Core.Color cc = keyboardGrid[fkey];
                                        System.Drawing.Color ccX = Corale.Colore.WinForms.Extensions.ToSystemColor(cc);
                                        presets.Add(key, ccX);
                                    }
                                }

                                //Keyboard.Instance.SetCustom(keyboard_custom);

                                //HoldReader = true;

                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep0, key);
                                    if (pos > -1)
                                    {
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        {
                                            if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                            {
                                                Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                                refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /*
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                        */
                                    }
                                }
                            }
                            else if (i == 2)
                            {
                                //Step 1
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep1, key);
                                    if (pos > -1)
                                    {
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        {
                                            if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                            {
                                                Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                                refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /*
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                        */
                                    }
                                }
                            }
                            else if (i == 3)
                            {
                                //Step 2
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep2, key);
                                    if (pos > -1)
                                    {
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        {
                                            if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                            {
                                                Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                                refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /*
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                        */
                                    }
                                }
                            }
                            else if (i == 4)
                            {
                                //Step 3
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep3, key);
                                    if (pos > -1)
                                    {
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        {
                                            if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                            {
                                                Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                                refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /*
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                        */
                                    }
                                }
                            }
                            else if (i == 5)
                            {
                                //Step 4
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep4, key);
                                    if (pos > -1)
                                    {
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        {
                                            if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                            {
                                                Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                                refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /*
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                        */
                                    }
                                }
                            }
                            else if (i == 6)
                            {
                                //Step 5
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep5, key);
                                    if (pos > -1)
                                    {
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        {
                                            if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                            {
                                                Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                                refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /*
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                        */
                                    }
                                }
                            }
                            else if (i == 7)
                            {
                                //Step 6
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep6, key);
                                    if (pos > -1)
                                    {
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        {
                                            if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                            {
                                                Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                                refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /*
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                        */
                                    }
                                }
                            }
                            else if (i == 8)
                            {
                                //Step 7
                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    int pos = Array.IndexOf(DeviceEffects._PulseOutStep7, key);
                                    if (pos > -1)
                                    {
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        {
                                            if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                            {
                                                Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                                refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /*
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                        }
                                        */
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (string key in DeviceEffects._GlobalKeys)
                                {
                                    //ApplyMapKeyLighting(key, presets[key], true);
                                }


                                //presets.Clear();
                                presets.Clear();
                                //HoldReader = false;
                            }

                            if (i < 9)
                            {
                                Thread.Sleep(speed);
                            }

                            Chroma.Instance.Keyboard.SetCustom(refreshGrid);
                        }
                    }
                }
            });
        }


        static readonly object _RazerFlash1 = new object();
        public void Flash1(System.Drawing.Color burstcol, int speed, string[] region)
        {
            lock (_RazerFlash1)
            {
                Dictionary<string, System.Drawing.Color> presets = new Dictionary<string, System.Drawing.Color>();
                Corale.Colore.Core.Color ScrollWheel = new Corale.Colore.Core.Color();
                Corale.Colore.Core.Color Logo = new Corale.Colore.Core.Color();
                Corale.Colore.Core.Color Backlight = new Corale.Colore.Core.Color();
                Corale.Colore.Core.Color Pad1 = new Corale.Colore.Core.Color();
                Corale.Colore.Core.Color Pad2 = new Corale.Colore.Core.Color();

                System.Drawing.Color Pad1Conv = System.Drawing.Color.FromArgb(Pad1.R, Pad1.G, Pad1.B);
                System.Drawing.Color Pad2Conv = System.Drawing.Color.FromArgb(Pad2.R, Pad2.G, Pad2.B);
                System.Drawing.Color ScrollWheelConv = System.Drawing.Color.FromArgb(ScrollWheel.R, ScrollWheel.G, ScrollWheel.B);
                System.Drawing.Color LogoConv = System.Drawing.Color.FromArgb(Logo.R, Logo.G, Logo.B);
                System.Drawing.Color BacklightConv = System.Drawing.Color.FromArgb(Backlight.R, Backlight.G, Backlight.B);

                var refreshKeyGrid = Corale.Colore.Razer.Keyboard.Effects.Custom.Create();
                refreshKeyGrid = keyboardGrid;

                for (int i = 0; i <= 8; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in region)
                            {
                                Corale.Colore.Razer.Keyboard.Key fkey = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                Corale.Colore.Core.Color cc = keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                System.Drawing.Color ccX = Corale.Colore.WinForms.Extensions.ToSystemColor(cc);
                                presets.Add(key, ccX);
                            }
                        }

                        if (RazerDeviceMouse)
                        {
                            ScrollWheel = Mouse.Instance[1];
                            Logo = Mouse.Instance[2];
                            Backlight = Mouse.Instance[3];
                            Pad1 = Mousepad.Instance[7];
                            Pad2 = Mousepad.Instance[14];
                        }
                        //Keyboard.Instance.SetCustom(keyboard_custom);

                        //HoldReader = true;

                    }
                    else if (i == 1)
                    {
                        //Step 0
                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in region)
                            {

                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                {
                                    Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                }

                            }
                        }

                        if (RazerDeviceMouse)
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
                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in DeviceEffects._GlobalKeys3)
                            {

                                if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                {
                                    Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                }
                            }
                        }

                        if (RazerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", ScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", LogoConv, false);
                            ApplyMapMouseLighting("Backlight", BacklightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Pad1); }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in region)
                            {

                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                {
                                    Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                }

                            }
                        }

                        if (RazerDeviceMouse)
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
                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in DeviceEffects._GlobalKeys3)
                            {

                                if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                {
                                    Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                }
                            }
                        }

                        if (RazerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", ScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", LogoConv, false);
                            ApplyMapMouseLighting("Backlight", BacklightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Pad1); }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in region)
                            {

                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                {
                                    Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                }

                            }
                        }

                        if (RazerDeviceMouse)
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
                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in DeviceEffects._GlobalKeys3)
                            {

                                if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                {
                                    Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                }
                            }
                        }

                        if (RazerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", ScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", LogoConv, false);
                            ApplyMapMouseLighting("Backlight", BacklightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Pad1); }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in region)
                            {

                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                {
                                    Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                }

                            }
                        }

                        if (RazerDeviceMouse)
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
                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in DeviceEffects._GlobalKeys3)
                            {

                                if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                {
                                    Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                    refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(presets[key]);
                                }
                            }
                        }

                        if (RazerDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", ScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", LogoConv, false);
                            ApplyMapMouseLighting("Backlight", BacklightConv, false);
                        }

                        //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Pad1); }

                        presets.Clear();
                        //HoldReader = false;
                    }

                    if (i < 8)
                    {
                        Thread.Sleep(speed);
                    }

                    Chroma.Instance.Keyboard.SetCustom(refreshKeyGrid);
                }
            }
        }

        private static int _RazerFlash2Step = 0;
        private static bool _RazerFlash2Running = false;
        private static Dictionary<string, System.Drawing.Color> _flashpresets = new Dictionary<string, System.Drawing.Color>();
        //Corale.Colore.Core.Color Pad1 = new Corale.Colore.Core.Color();
        static readonly object _RazerFlash2 = new object();
        public void Flash2(System.Drawing.Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                lock (_RazerFlash2)
                {
                    Dictionary<string, System.Drawing.Color> flashpresets = new Dictionary<string, System.Drawing.Color>();
                                        
                    Corale.Colore.Core.Color RzScrollWheel = new Corale.Colore.Core.Color();
                    Corale.Colore.Core.Color RzLogo = new Corale.Colore.Core.Color();
                    System.Drawing.Color RzScrollWheelConv = new System.Drawing.Color();
                    System.Drawing.Color RzLogoConv = new System.Drawing.Color();

                    var refreshKeyGrid = Corale.Colore.Razer.Keyboard.Effects.Custom.Create();
                    refreshKeyGrid = keyboardGrid;
                    

                    if (!_RazerFlash2Running)
                    {
                        if (RazerDeviceMouse)
                        {
                            RzScrollWheel = Mouse.Instance[1];
                            RzLogo = Mouse.Instance[2];
                            RzScrollWheelConv = System.Drawing.Color.FromArgb(RzScrollWheel.R, RzScrollWheel.G, RzScrollWheel.B);
                            RzLogoConv = System.Drawing.Color.FromArgb(RzLogo.R, RzLogo.G, RzLogo.B);
                        }

                        if (RazerDeviceKeyboard)
                        {
                            foreach (string key in regions)
                            {
                                Corale.Colore.Razer.Keyboard.Key fkey = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                Corale.Colore.Core.Color cc = keyboardGrid[fkey]; //Keyboard.Instance[fkey];
                                System.Drawing.Color ccX = Corale.Colore.WinForms.Extensions.ToSystemColor(cc);
                                flashpresets.Add(key, ccX);

                            }
                        }


                        _RazerFlash2Running = true;
                        _RazerFlash2Step = 0;
                        _flashpresets = flashpresets;
                    }

                    if (_RazerFlash2Running)
                    {
                        while (_RazerFlash2Running)
                        {
                            if (cts.IsCancellationRequested)
                            {
                                break;
                            }

                            if (_RazerFlash2Step == 0)
                            {
                                if (RazerDeviceKeyboard)
                                {
                                    foreach (string key in regions)
                                    {

                                        //ApplyMapKeyLighting(key, burstcol, true);
                                        //refreshKeyGrid
                                        
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);

                                            refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }

                                    Chroma.Instance.Keyboard.SetCustom(refreshKeyGrid);
                                }

                                if (RazerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                                    ApplyMapMouseLighting("Logo", burstcol, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

                                _RazerFlash2Step = 1;

                            }
                            else if (_RazerFlash2Step == 1)
                            {
                                if (RazerDeviceKeyboard)
                                {
                                    foreach (string key in regions)
                                    {
                                        
                                        //ApplyMapKeyLighting(key, burstcol, true);
                                        //refreshKeyGrid
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);

                                            refreshKeyGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(_flashpresets[key]);
                                        }
                                    }

                                    Chroma.Instance.Keyboard.SetCustom(refreshKeyGrid);
                                }

                                if (RazerDeviceMouse)
                                {
                                    ApplyMapMouseLighting("ScrollWheel", RzScrollWheelConv, false);
                                    ApplyMapMouseLighting("Logo", RzLogoConv, false);
                                }

                                //if (RazerDeviceHeadset) { Headset.Instance.SetAll(Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol)); }

                                _RazerFlash2Step = 0;


                            }

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

        private static int _RazerFlash3Step = 0;
        private static bool _RazerFlash3Running = false;
        static readonly object _RazerFlash3 = new object();
        public void Flash3(System.Drawing.Color burstcol, int speed, CancellationToken cts)
        {
            try
            {
                //DeviceEffects._NumFlash
                lock (_RazerFlash3)
                {
                    if (RazerDeviceKeyboard == true)
                    {
                        Dictionary<string, System.Drawing.Color> presets = new Dictionary<string, System.Drawing.Color>();
                        var refreshGrid = Corale.Colore.Razer.Keyboard.Effects.Custom.Create();
                        refreshGrid = keyboardGrid;
                        //Debug.WriteLine("Running Flash 3");
                        _RazerFlash3Running = true;
                        _RazerFlash3Step = 0;

                        if (_RazerFlash3Running == false)
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
                            while (_RazerFlash3Running == true)
                            {
                                //cts.ThrowIfCancellationRequested();

                                if (cts.IsCancellationRequested)
                                {
                                    break;
                                }

                                if (_RazerFlash3Step == 0)
                                {
                                    foreach (string key in DeviceEffects._NumFlash)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(burstcol);
                                        }
                                    }
                                    _RazerFlash3Step = 1;
                                }
                                else if (_RazerFlash3Step == 1)
                                {
                                    foreach (string key in DeviceEffects._NumFlash)
                                    {
                                        if (Enum.IsDefined(typeof(Corale.Colore.Razer.Keyboard.Key), key))
                                        {
                                            Corale.Colore.Razer.Keyboard.Key keyid = (Corale.Colore.Razer.Keyboard.Key)Enum.Parse(typeof(Corale.Colore.Razer.Keyboard.Key), key);
                                            refreshGrid[keyid] = Corale.Colore.WinForms.Extensions.ToColoreColor(System.Drawing.Color.Black);
                                        }
                                    }

                                    _RazerFlash3Step = 0;
                                }

                                Chroma.Instance.Keyboard.SetCustom(refreshGrid);
                                Thread.Sleep(speed);
                            }
                        }
                    }
                }
            }
            catch
            {
                //
            }
        }


        static readonly object _RazertransitionConst = new object();
        private void RazertransitionConst(System.Drawing.Color col1, System.Drawing.Color col2, bool forward, int speed)
        {
            lock (_RazertransitionConst)
            {
                if (RazerDeviceKeyboard == true)
                {
                    int i = 1;
                    uint RzCol = Corale.Colore.WinForms.Extensions.ToColoreColor(col1);
                    uint RzCol2 = Corale.Colore.WinForms.Extensions.ToColoreColor(col2);
                    var state = 0;

                    while (state == 6)
                    {
                        RCTS.Token.ThrowIfCancellationRequested();
                        if (i == 1)
                        {
                            for (uint c = 0; c < Corale.Colore.Razer.Keyboard.Constants.MaxColumns; c++)
                            {
                                for (uint r = 0; r < Corale.Colore.Razer.Keyboard.Constants.MaxRows; r++)
                                {
                                    if (state != 6) { break; }
                                    var row = (forward) ? r : Corale.Colore.Razer.Keyboard.Constants.MaxRows - r - 1;
                                    var colu = (forward) ? c : Corale.Colore.Razer.Keyboard.Constants.MaxColumns - c - 1;
                                    try
                                    {
                                        Keyboard.Instance[Convert.ToInt32(row), Convert.ToInt32(colu)] = RzCol;
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
                            for (uint c = 0; c < Corale.Colore.Razer.Keyboard.Constants.MaxColumns; c++)
                            {
                                for (uint r = 0; r < Corale.Colore.Razer.Keyboard.Constants.MaxRows; r++)
                                {
                                    if (state != 6) { break; }
                                    var row = (forward) ? r : Corale.Colore.Razer.Keyboard.Constants.MaxRows - r - 1;
                                    var colu = (forward) ? c : Corale.Colore.Razer.Keyboard.Constants.MaxColumns - c - 1;
                                    try
                                    {
                                        Keyboard.Instance[Convert.ToInt32(row), Convert.ToInt32(colu)] = RzCol2;
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
