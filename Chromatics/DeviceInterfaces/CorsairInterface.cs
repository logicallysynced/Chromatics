using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.CorsairLibs;
using CUE.NET;
using CUE.NET.Brushes;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Groups;
using GalaSoft.MvvmLight.Ioc;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;

/* Contains all Corsair SDK code for detection, initilization, states and effects.
 * 
 * 
 */

namespace Chromatics.DeviceInterfaces
{
    public class CorsairInterface
    {
        public static CorsairLib InitializeCorsairSDK()
        {
            CorsairLib corsair = null;
            corsair = new CorsairLib();

            var corsairstat = corsair.InitializeSDK();

            if (!corsairstat)
                return null;

            return corsair;
        }
    }

    public class CorsairSdkWrapper
    {
        //
    }

    public interface ICorsairSdk
    {
        bool InitializeSDK();
        void ResetCorsairDevices(bool DeviceKeyboard, bool DeviceKeypad, bool DeviceMouse, bool DeviceMousepad, bool DeviceHeadset, System.Drawing.Color basecol);
        void CorsairUpdateLED();
        void UpdateState(string type, Color col, bool disablekeys, [Optional] Color col2, [Optional] bool direction, [Optional] int speed);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapMouseLighting(string region, Color col, bool clear);
        void ApplyMapLogoLighting(string key, Color col, bool clear);
        void ApplyMapPadLighting(string region, Color col, bool clear);
        Task Ripple1(Color burstcol, int speed, Color _BaseColor);
        Task Ripple2(Color burstcol, int speed);
        void Flash1(Color burstcol, int speed, string[] regions);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
    }

    public class CorsairLib : ICorsairSdk
    {
        private static ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();

        #region Effect Steps
        /*
        private readonly string[] DeviceEffects._GlobalKeys =
        {
            "Y", "D5", "D6", "D7", "T", "U", "G", "H", "J", "F3", "F4", "F5", "F6",
            "F7", "D4", "D8", "R", "F", "C", "V", "B", "N", "M", "K", "I", "F2", "F8", "D3", "E", "D", "X", "Space",
            "OemComma", "L", "O", "D9", "F1", "F9", "D2", "D0", "W", "S", "Z", "LeftAlt", "P", "OemSemicolon",
            "OemPeriod", "RightAlt", "D1", "Q", "A", "LeftWindows", "F10", "OemMinus", "OemLeftBracket", "OemApostrophe",
            "OemSlash", "Function", "Escape", "OemTilde", "Tab", "CapsLock", "LeftShift", "LeftControl", "F11",
            "OemEquals", "RightMenu", "OemRightBracket", "Macro1", "Macro2", "Macro3", "Macro4", "Macro5", "F12",
            "Backspace", "OemBackslash", "Enter", "RightShift", "RightControl"
        };

        private string[] DeviceEffects._GlobalKeys2 =
        {
            "Y", "D5", "D6", "D7", "T", "U", "G", "H", "J", "D4", "D8", "R", "F", "C", "V",
            "B", "N", "M", "K", "I", "D3", "E", "D", "X", "Space", "OemComma", "L", "O", "D9", "D2", "D0", "W", "S", "Z",
            "LeftAlt", "P", "OemSemicolon", "OemPeriod", "RightAlt", "D1", "Q", "A", "LeftWindows", "OemMinus",
            "OemLeftBracket", "OemApostrophe", "OemSlash", "Function", "Escape", "OemTilde", "Tab", "CapsLock",
            "LeftShift", "LeftControl", "OemEquals", "RightMenu", "OemRightBracket", "Macro1", "Macro2", "Macro3",
            "Macro4", "Macro5", "Backspace", "OemBackslash", "Enter", "RightShift", "RightControl"
        };

        private readonly string[] DeviceEffects._GlobalKeys3 =
        {
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11",
            "F12", "NumLock", "Num0", "Num1", "Num2", "Num3", "Num4", "Num5", "Num6", "Num7", "Num8", "Num9",
            "NumDivide", "NumMultiply", "NumSubtract", "NumAdd", "NumEnter", "NumDecimal"
        };

        private readonly string[] DeviceEffects._PulseOutStep0 = { "Y" };
        private readonly string[] DeviceEffects._PulseOutStep1 = { "D5", "D6", "D7", "T", "U", "G", "H", "J" };

        private readonly string[] DeviceEffects._PulseOutStep2 =
        {
            "F3", "F4", "F5", "F6", "F7", "D4", "D8", "R", "F", "C", "V", "B",
            "N", "M", "K", "I"
        };

        private readonly string[] DeviceEffects._PulseOutStep3 = { "F2", "F8", "D3", "E", "D", "X", "Space", "OemComma", "L", "O", "D9" };

        private readonly string[] DeviceEffects._PulseOutStep4 =
        {
            "F1", "F9", "D2", "D0", "W", "S", "Z", "LeftAlt", "P", "OemSemicolon",
            "OemPeriod", "RightAlt"
        };

        private readonly string[] DeviceEffects._PulseOutStep5 =
        {
            "D1", "Q", "A", "LeftWindows", "F10", "OemMinus", "OemLeftBracket",
            "OemApostrophe", "OemSlash", "Function"
        };

        private readonly string[] DeviceEffects._PulseOutStep6 =
        {
            "Escape", "OemTilde", "Tab", "CapsLock", "LeftShift", "LeftControl",
            "F11", "OemEquals", "RightMenu", "OemRightBracket", "OemBackslash"
        };

        private readonly string[] DeviceEffects._PulseOutStep7 =
        {
            "Macro1", "Macro2", "Macro3", "Macro4", "Macro5", "F12", "Backspace",
            "Enter", "RightShift", "RightControl"
        };

        private readonly string[] DeviceEffects._NumFlash = { "NumLock", "NumDivide", "NumMultiply", "Num7", "Num8", "Num9", "Num4", "Num5", "Num6", "Num1", "Num2", "Num3" };
        */

        private readonly Dictionary<string, CorsairLedId> Corsairkeyids = new Dictionary<string, CorsairLedId>
        {
            //Keys
            {"F1", CorsairLedId.F1},
            {"F2", CorsairLedId.F2},
            {"F3", CorsairLedId.F3},
            {"F4", CorsairLedId.F4},
            {"F5", CorsairLedId.F5},
            {"F6", CorsairLedId.F6},
            {"F7", CorsairLedId.F7},
            {"F8", CorsairLedId.F8},
            {"F9", CorsairLedId.F9},
            {"F10", CorsairLedId.F10},
            {"F11", CorsairLedId.F11},
            {"F12", CorsairLedId.F12},
            {"D1", CorsairLedId.D1},
            {"D2", CorsairLedId.D2},
            {"D3", CorsairLedId.D3},
            {"D4", CorsairLedId.D4},
            {"D5", CorsairLedId.D5},
            {"D6", CorsairLedId.D6},
            {"D7", CorsairLedId.D7},
            {"D8", CorsairLedId.D8},
            {"D9", CorsairLedId.D9},
            {"D0", CorsairLedId.D0},
            {"A", CorsairLedId.A},
            {"B", CorsairLedId.B},
            {"C", CorsairLedId.C},
            {"D", CorsairLedId.D},
            {"E", CorsairLedId.E},
            {"F", CorsairLedId.F},
            {"G", CorsairLedId.G},
            {"H", CorsairLedId.H},
            {"I", CorsairLedId.I},
            {"J", CorsairLedId.J},
            {"K", CorsairLedId.K},
            {"L", CorsairLedId.L},
            {"M", CorsairLedId.M},
            {"N", CorsairLedId.N},
            {"O", CorsairLedId.O},
            {"P", CorsairLedId.P},
            {"Q", CorsairLedId.Q},
            {"R", CorsairLedId.R},
            {"S", CorsairLedId.S},
            {"T", CorsairLedId.T},
            {"U", CorsairLedId.U},
            {"V", CorsairLedId.V},
            {"W", CorsairLedId.W},
            {"X", CorsairLedId.X},
            {"Y", CorsairLedId.Y},
            {"Z", CorsairLedId.Z},
            {"NumLock", CorsairLedId.NumLock},
            {"Num0", CorsairLedId.Keypad0},
            {"Num1", CorsairLedId.Keypad1},
            {"Num2", CorsairLedId.Keypad2},
            {"Num3", CorsairLedId.Keypad3},
            {"Num4", CorsairLedId.Keypad4},
            {"Num5", CorsairLedId.Keypad5},
            {"Num6", CorsairLedId.Keypad6},
            {"Num7", CorsairLedId.Keypad7},
            {"Num8", CorsairLedId.Keypad8},
            {"Num9", CorsairLedId.Keypad9},
            {"NumDivide", CorsairLedId.KeypadSlash},
            {"NumMultiply", CorsairLedId.KeypadAsterisk},
            {"NumSubtract", CorsairLedId.KeypadMinus},
            {"NumAdd", CorsairLedId.KeypadPlus},
            {"NumEnter", CorsairLedId.KeypadEnter},
            {"NumDecimal", CorsairLedId.KeypadPeriodAndDelete},
            {"PrintScreen", CorsairLedId.PrintScreen},
            {"Scroll", CorsairLedId.ScrollLock},
            {"Pause", CorsairLedId.PauseBreak},
            {"Insert", CorsairLedId.Insert},
            {"Home", CorsairLedId.Home},
            {"PageUp", CorsairLedId.PageUp},
            {"PageDown", CorsairLedId.PageDown},
            {"Delete", CorsairLedId.Delete},
            {"End", CorsairLedId.End},
            {"Up", CorsairLedId.UpArrow},
            {"Left", CorsairLedId.LeftArrow},
            {"Right", CorsairLedId.RightArrow},
            {"Down", CorsairLedId.DownArrow},
            {"Tab", CorsairLedId.Tab},
            {"CapsLock", CorsairLedId.CapsLock},
            {"Backspace", CorsairLedId.Backspace},
            {"Enter", CorsairLedId.Enter},
            {"LeftControl", CorsairLedId.LeftCtrl},
            {"LeftWindows", CorsairLedId.WinLock},
            {"LeftAlt", CorsairLedId.LeftAlt},
            {"Space", CorsairLedId.Space},
            {"RightControl", CorsairLedId.RightCtrl},
            {"Function", CorsairLedId.LeftGui},
            {"RightAlt", CorsairLedId.RightAlt},
            {"RightMenu", CorsairLedId.RightGui},
            {"LeftShift", CorsairLedId.LeftShift},
            {"RightShift", CorsairLedId.RightShift},
            {"Macro1", CorsairLedId.G1}, //G1
            {"Macro2", CorsairLedId.G2}, //G2
            {"Macro3", CorsairLedId.G3}, //G3
            {"Macro4", CorsairLedId.G4}, //G4
            {"Macro5", CorsairLedId.G5}, //G5
            {"Macro6", CorsairLedId.G6}, //G6
            {"Macro7", CorsairLedId.G7}, //G7
            {"Macro8", CorsairLedId.G8}, //G8
            {"Macro9", CorsairLedId.G9}, //G9
            {"Macro10", CorsairLedId.G10}, //G10
            {"Macro11", CorsairLedId.G11}, //G11
            {"Macro12", CorsairLedId.G12}, //G12
            {"Macro13", CorsairLedId.G13}, //G13
            {"Macro14", CorsairLedId.G14}, //G14
            {"Macro15", CorsairLedId.G15}, //G15
            {"Macro16", CorsairLedId.G16}, //G16
            {"Macro17", CorsairLedId.G17}, //G17
            {"Macro18", CorsairLedId.G18}, //G18
            {"OemTilde", CorsairLedId.GraveAccentAndTilde},
            {"OemMinus", CorsairLedId.MinusAndUnderscore},
            {"OemEquals", CorsairLedId.EqualsAndPlus},
            {"OemLeftBracket", CorsairLedId.BracketLeft},
            {"OemRightBracket", CorsairLedId.BracketRight},
            {"OemSlash", CorsairLedId.SlashAndQuestionMark},
            {"OemSemicolon", CorsairLedId.SemicolonAndColon},
            {"OemApostrophe", CorsairLedId.ApostropheAndDoubleQuote},
            {"OemComma", CorsairLedId.CommaAndLessThan},
            {"OemPeriod", CorsairLedId.PeriodAndBiggerThan},
            {"OemBackslash", CorsairLedId.Backslash},
            {"EurPound", CorsairLedId.International1},
            {"JpnYen", CorsairLedId.International2},
            {"Escape", CorsairLedId.Escape},
            {"MouseFront", CorsairLedId.B2},
            {"MouseScroll", CorsairLedId.B3},
            {"MouseSide", CorsairLedId.B4},
            {"MouseLogo", CorsairLedId.B1},
            {"Pad1", CorsairLedId.Zone1},
            {"Pad2", CorsairLedId.Zone2},
            {"Pad3", CorsairLedId.Zone3},
            {"Pad4", CorsairLedId.Zone4},
            {"Pad5", CorsairLedId.Zone5},
            {"Pad6", CorsairLedId.Zone6},
            {"Pad7", CorsairLedId.Zone7},
            {"Pad8", CorsairLedId.Zone8},
            {"Pad9", CorsairLedId.Zone9},
            {"Pad10", CorsairLedId.Zone10},
            {"Pad11", CorsairLedId.Zone11},
            {"Pad12", CorsairLedId.Zone12},
            {"Pad13", CorsairLedId.Zone13},
            {"Pad14", CorsairLedId.Zone14},
            {"Pad15", CorsairLedId.Zone15},
            {"Strip1", CorsairLedId.Invalid},
            {"Strip2", CorsairLedId.Invalid},
            {"Strip3", CorsairLedId.Invalid},
            {"Strip4", CorsairLedId.Invalid},
            {"Strip5", CorsairLedId.Invalid},
            {"Strip6", CorsairLedId.Invalid},
            {"Strip7", CorsairLedId.Invalid},
            {"Strip8", CorsairLedId.Invalid},
            {"Strip9", CorsairLedId.Invalid},
            {"Strip10", CorsairLedId.Invalid},
            {"Strip11", CorsairLedId.Invalid},
            {"Strip12", CorsairLedId.Invalid},
            {"Strip13", CorsairLedId.Invalid},
            {"Strip14", CorsairLedId.Invalid}
        };
        #endregion

        private static readonly object _Corsairtransition = new object();
        private static readonly object _CorsairRipple1 = new object();
        private static readonly object _CorsairRipple2 = new object();
        private static readonly object _CorsairFlash1 = new object();
        private static readonly object _CorsairFlash2 = new object();
        private static readonly object _CorsairtransitionConst = new object();
        private ListLedGroup _CorsairAllHeadsetLED;

        //Define Corsair LED Groups
        private ListLedGroup _CorsairAllKeyboardLED;
        private ListLedGroup _CorsairAllMouseLED;
        private ListLedGroup _CorsairAllMousepadLED;
        private bool _CorsairFlash2Running;
        private int _CorsairFlash2Step;

        private KeyMapBrush _CorsairKeyboardIndvBrush;
        private ListLedGroup _CorsairKeyboardIndvLED;
        private KeyMapBrush _CorsairMouseIndvBrush;
        private ListLedGroup _CorsairMouseIndvLED;
        private KeyMapBrush _CorsairMousepadIndvBrush;
        private ListLedGroup _CorsairMousepadIndvLED;

        //Handle device send/recieve
        private readonly CancellationTokenSource CCTS = new CancellationTokenSource();
        private bool CorsairDeviceHeadset = true;
        private bool CorsairDeviceKeyboard = true;
        private bool CorsairDeviceKeypad = true;
        private bool CorsairDeviceMouse = true;
        private bool CorsairDeviceMousepad = true;

        private Color CorsairLogo;
        private Color CorsairLogoConv;
        private Color CorsairScrollWheel;
        private Color CorsairScrollWheelConv;
        private Dictionary<string, Color> presets = new Dictionary<string, Color>();

        public bool InitializeSDK()
        {
            write.WriteConsole(ConsoleTypes.CORSAIR, "Attempting to load CUE SDK..");

            try
            {
                CueSDK.Initialize();

                _CorsairKeyboardIndvBrush = new KeyMapBrush();
                _CorsairKeyboardIndvLED = new ListLedGroup(CueSDK.KeyboardSDK, CueSDK.KeyboardSDK);
                _CorsairAllKeyboardLED = new ListLedGroup(CueSDK.KeyboardSDK, CueSDK.KeyboardSDK);
                _CorsairAllKeyboardLED.ZIndex = 1;
                _CorsairKeyboardIndvLED.ZIndex = 10;
                _CorsairKeyboardIndvLED.Brush = _CorsairKeyboardIndvBrush;
                _CorsairAllKeyboardLED.Brush = (SolidColorBrush)Color.Black;

                _CorsairMouseIndvBrush = new KeyMapBrush();
                _CorsairMouseIndvLED = new ListLedGroup(CueSDK.MouseSDK, CueSDK.MouseSDK);
                _CorsairAllMouseLED = new ListLedGroup(CueSDK.MouseSDK, CueSDK.MouseSDK);
                _CorsairAllMouseLED.ZIndex = 1;
                _CorsairMouseIndvLED.ZIndex = 10;
                _CorsairMouseIndvLED.Brush = _CorsairMouseIndvBrush;
                _CorsairAllMouseLED.Brush = (SolidColorBrush)Color.Black;

                _CorsairMousepadIndvBrush = new KeyMapBrush();
                _CorsairMousepadIndvLED = new ListLedGroup(CueSDK.MousematSDK, CueSDK.MousematSDK);
                _CorsairAllMousepadLED = new ListLedGroup(CueSDK.MousematSDK, CueSDK.MousematSDK);
                _CorsairAllMousepadLED.ZIndex = 1;
                _CorsairMousepadIndvLED.ZIndex = 10;
                _CorsairMousepadIndvLED.Brush = _CorsairMousepadIndvBrush;
                _CorsairAllMousepadLED.Brush = (SolidColorBrush)Color.Black;

                _CorsairAllHeadsetLED = new ListLedGroup(CueSDK.HeadsetSDK, CueSDK.HeadsetSDK);
                _CorsairAllHeadsetLED.ZIndex = 1;
                _CorsairAllHeadsetLED.Brush = (SolidColorBrush)Color.Black;

                var corsairver = CueSDK.ProtocolDetails.ServerVersion.Split('.');
                int cV = int.Parse(corsairver[0]);

                if (cV < 2)
                {
                    write.WriteConsole(ConsoleTypes.ERROR, "Corsair device support requires CUE2 Version 2.0.0 or higher to operate. Please download the latest version of CUE2 from the Corsair website.");
                    return false;
                }

                write.WriteConsole(ConsoleTypes.CORSAIR, "CUE SDK Loaded (" + CueSDK.ProtocolDetails.SdkVersion + "/" + CueSDK.ProtocolDetails.ServerVersion + ")");

                CueSDK.UpdateMode = UpdateMode.Continuous;
                //ResetCorsairDevices();

                return true;
            }
            catch (Exception ex)
            {
                write.WriteConsole(ConsoleTypes.CORSAIR, "CUE SDK failed to load. EX: " + ex.Message);
                return false;
            }
        }

        public void ResetCorsairDevices(bool DeviceKeyboard, bool DeviceKeypad, bool DeviceMouse, bool DeviceMousepad, bool DeviceHeadset, System.Drawing.Color basecol)
        {
            CorsairDeviceKeyboard = DeviceKeyboard;
            CorsairDeviceKeypad = DeviceKeypad;
            CorsairDeviceMouse = DeviceMouse;
            CorsairDeviceMousepad = DeviceMousepad;
            CorsairDeviceHeadset = DeviceHeadset;

            if (CorsairDeviceKeyboard)
            {
                UpdateState("static", basecol, false);
            }
        }


        public void CorsairUpdateLED()
        {
            if (CorsairDeviceHeadset) CueSDK.HeadsetSDK.Update();
            if (CorsairDeviceKeyboard) CueSDK.KeyboardSDK.Update();
            if (CorsairDeviceMouse) CueSDK.MouseSDK.Update();
            if (CorsairDeviceMousepad) CueSDK.MousematSDK.Update();
        }

        public void UpdateState(string type, Color col, bool disablekeys, [Optional] Color col2,
            [Optional] bool direction, [Optional] int speed)
        {
            MemoryTasks.Cleanup();
            //SolidColorBrush _CorsairAllLEDBrush = new SolidColorBrush(col);

            if (type == "reset")
            {
                try
                {
                    if (CorsairDeviceHeadset)
                    {
                        //
                    }
                    if (CorsairDeviceKeyboard && disablekeys != true)
                    {
                        //
                    }
                    if (CorsairDeviceKeypad)
                    {
                        //Not Implemented
                    }
                    if (CorsairDeviceMouse)
                    {
                        //
                    }
                    if (CorsairDeviceMousepad)
                    {
                        //
                    }
                }
                catch
                {
                    //
                }
            }
            else if (type == "static")
            {
                try
                {
                    if (CorsairDeviceHeadset)
                        _CorsairAllHeadsetLED.Brush = (SolidColorBrush)col;
                    if (CorsairDeviceKeyboard && disablekeys != true)
                    {
                        _CorsairAllKeyboardLED.Brush = (SolidColorBrush)col;
                        CueSDK.KeyboardSDK.Update();
                    }

                    if (CorsairDeviceKeypad)
                    {
                        //Not Implemented
                    }
                    if (CorsairDeviceMouse)
                        _CorsairAllMouseLED.Brush = (SolidColorBrush)col;
                    if (CorsairDeviceMousepad)
                        _CorsairAllMousepadLED.Brush = (SolidColorBrush)col;
                }
                catch (Exception ex)
                {
                    write.WriteConsole(ConsoleTypes.ERROR, "Corsair (Static)" + ex.Message);
                }
            }
            else if (type == "transition")
            {
                var _CrSt = new Task(() =>
                {
                    if (CorsairDeviceHeadset)
                    {
                        //CueSDK.HeadsetSDK.Brush = (SolidColorBrush)col;
                        //_CorsairAllHeadsetLED.Brush = _CorsairAllLEDBrush;
                        //_CorsairAllHeadsetLED.ZIndex = 1;
                        //CueSDK.HeadsetSDK.Update();
                    }
                    if (CorsairDeviceKeyboard && disablekeys != true)
                    {
                        //Corsairtransition(col, direction);
                    }
                    if (CorsairDeviceKeypad)
                    {
                        //Not Implemented
                    }
                    if (CorsairDeviceMouse)
                    {
                        //CueSDK.MouseSDK.Brush = (SolidColorBrush)col;
                        //_CorsairAllMouseLED.Brush = _CorsairAllLEDBrush;
                        //_CorsairAllMouseLED.ZIndex = 1;
                        //CueSDK.MouseSDK.Update();
                    }
                    if (CorsairDeviceMousepad)
                    {
                        //CueSDK.MousematSDK.Brush = (SolidColorBrush)col;
                        //_CorsairAllMousepadLED.Brush = _CorsairAllLEDBrush;
                        //_CorsairAllMousepadLED.ZIndex = 1;
                        //CueSDK.MousematSDK.Update();
                    }
                });
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
            }
            else if (type == "wave")
            {
                var _CrSt = new Task(() =>
                {
                    try
                    {
                        if (CorsairDeviceHeadset)
                        {
                            //Headset.Instance.SetEffect(Corale.Colore.Corsair.Headset.Effects.Effect.SpectrumCycling);
                        }
                        if (CorsairDeviceKeyboard && disablekeys != true)
                        {
                            //Keyboard.Instance.SetWave(Corale.Colore.Corsair.Keyboard.Effects.Direction.LeftToRight);
                        }
                        if (CorsairDeviceKeypad)
                        {
                            //Not Implemented
                        }
                        if (CorsairDeviceMouse)
                        {
                            //Mouse.Instance.SetWave(Corale.Colore.Corsair.Mouse.Effects.Direction.FrontToBack);
                        }
                        if (CorsairDeviceMousepad)
                        {
                            //Mousepad.Instance.SetWave(Corale.Colore.Corsair.Mousepad.Effects.Direction.LeftToRight);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Corsair (Wave): " + ex.Message);
                    }
                });
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
            }
            else if (type == "breath")
            {
                var _CrSt = new Task(() =>
                {
                    try
                    {
                        if (CorsairDeviceHeadset)
                        {
                            //Headset.Instance.SetBreathing(RzCol);
                        }
                        if (CorsairDeviceKeypad)
                        {
                            //Keypad.Instance.SetBreathing(RzCol, RzCol2);
                        }
                        if (CorsairDeviceMouse)
                        {
                            //Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.Backlight);
                            //Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.Logo);
                            //Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.ScrollWheel);
                        }
                        if (CorsairDeviceMousepad)
                        {
                            //Mousepad.Instance.SetBreathing(RzCol, RzCol2);
                        }
                        if (CorsairDeviceKeyboard && disablekeys != true)
                        {
                            //Keyboard.Instance.SetBreathing(RzCol, RzCol2);
                        }
                    }
                    catch (Exception ex)
                    {
                        write.WriteConsole(ConsoleTypes.ERROR, "Corsair (Breath): " + ex.Message);
                    }
                });
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
            }
            else if (type == "pulse")
            {
                var _CrSt = new Task(() =>
                {
                    if (CorsairDeviceHeadset)
                        _CorsairAllHeadsetLED.Brush = (SolidColorBrush)col;
                    if (CorsairDeviceKeyboard && disablekeys != true)
                    {
                        //CorsairtransitionConst(col, col2, true, speed);
                    }
                    if (CorsairDeviceKeypad)
                    {
                        //Not Implemented
                    }
                    if (CorsairDeviceMouse)
                        _CorsairAllMouseLED.Brush = (SolidColorBrush)col;
                    if (CorsairDeviceMousepad)
                        _CorsairAllMousepadLED.Brush = (SolidColorBrush)col;
                }, CCTS.Token);
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
                //RzPulse = true;
            }

            //CorsairUpdateLED();
            MemoryTasks.Cleanup();
        }

        public void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {

            if (FFXIVHotbar.keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            try
            {
                if (CorsairDeviceKeyboard)
                    if (Corsairkeyids.ContainsKey(key))
                        if (CueSDK.KeyboardSDK[Corsairkeyids[key]] != null)
                            _CorsairKeyboardIndvBrush.CorsairApplyMapKeyLighting(Corsairkeyids[key], col);
            }
            catch (Exception ex)
            {
                write.WriteConsole(ConsoleTypes.ERROR, "Corsair ("+ key + "): " + ex.Message);
                write.WriteConsole(ConsoleTypes.ERROR, "Internal Error (" + key + "): " + ex.StackTrace);
            }
        }

        public void ApplyMapMouseLighting(string region, Color col, bool clear)
        {
            if (CorsairDeviceMouse)
                if (Corsairkeyids.ContainsKey(region))
                    if (CueSDK.MouseSDK[Corsairkeyids[region]] != null)
                        _CorsairMouseIndvBrush.CorsairApplyMapKeyLighting(Corsairkeyids[region], col);
        }

        public void ApplyMapLogoLighting(string key, Color col, bool clear)
        {
            //Not Implemented
        }

        public void ApplyMapPadLighting(string region, Color col, bool clear)
        {
            if (CorsairDeviceMousepad)
                if (Corsairkeyids.ContainsKey(region))
                    if (CueSDK.MousematSDK[Corsairkeyids[region]] != null)
                        _CorsairMousepadIndvBrush.CorsairApplyMapKeyLighting(Corsairkeyids[region], col);
        }

        private void transition(Color col, bool forward)
        {
            lock (_Corsairtransition)
            {
                if (CorsairDeviceKeyboard)
                {
                    //To be implemented

                    /*
                    RectangleF spot = new RectangleF(CueSDK.KeyboardSDK.DeviceRectangle.Width / 2f, CueSDK.KeyboardSDK.DeviceRectangle.Y / 2f, 160, 80);
                    PointF target = new PointF(spot.X, spot.Y);
                    RectangleLedGroup _CorsairKeyRec = new RectangleLedGroup(CueSDK.KeyboardSDK, spot);

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
                    */
                }
            }
        }

        public Task Ripple1(Color burstcol, int speed, Color _BaseColor)
        {
            return new Task(() =>
            {

                lock (_CorsairRipple1)
                {
                    if (CorsairDeviceKeyboard)
                    {
                        var presets = new Dictionary<string, Color>();

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in DeviceEffects._GlobalKeys)
                                    try
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                        {
                                            var _key = Corsairkeyids[key];
                                            //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                            if (CueSDK.KeyboardSDK[_key] != null)
                                            {
                                                Color ccX = _CorsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                                presets.Add(key, ccX);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        write.WriteConsole(ConsoleTypes.ERROR, "(" + key + "): " + ex.Message);
                                    }

                                //Keyboard.Instance.SetCustom(keyboard_custom);

                                //HoldReader = true;
                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep0, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 2)
                            {
                                //Step 1
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep1, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 3)
                            {
                                //Step 2
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep2, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 4)
                            {
                                //Step 3
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep3, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 5)
                            {
                                //Step 4
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep4, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 6)
                            {
                                //Step 5
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep5, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 7)
                            {
                                //Step 6
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep6, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 8)
                            {
                                //Step 7
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep7, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in DeviceEffects._GlobalKeys)
                                    if (Corsairkeyids.ContainsKey(key))
                                        ApplyMapKeyLighting(key, presets[key], false);

                                ApplyMapKeyLighting("D1", _BaseColor, false);
                                ApplyMapKeyLighting("D2", _BaseColor, false);
                                ApplyMapKeyLighting("D3", _BaseColor, false);
                                ApplyMapKeyLighting("D4", _BaseColor, false);
                                ApplyMapKeyLighting("D5", _BaseColor, false);
                                ApplyMapKeyLighting("D6", _BaseColor, false);
                                ApplyMapKeyLighting("D7", _BaseColor, false);
                                ApplyMapKeyLighting("D8", _BaseColor, false);
                                ApplyMapKeyLighting("D9", _BaseColor, false);
                                ApplyMapKeyLighting("D0", _BaseColor, false);
                                ApplyMapKeyLighting("OemMinus", _BaseColor, false);
                                ApplyMapKeyLighting("OemEquals", _BaseColor, false);

                                //presets.Clear();
                                //HoldReader = false;
                            }

                            if (i < 9)
                                Thread.Sleep(speed);

                            CueSDK.KeyboardSDK.Update();
                        }
                    }
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {

                lock (_CorsairRipple2)
                {
                    if (CorsairDeviceKeyboard)
                    {
                        var presets = new Dictionary<string, Color>();
                        //uint burstcol = new Corale.Colore.Core.Color(RzCol.R, RzCol.G, RzCol.B);

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    if (Corsairkeyids.ContainsKey(key))
                                    {
                                        var _key = Corsairkeyids[key];
                                        if (CueSDK.KeyboardSDK[_key] != null)
                                        {
                                            //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                            Color ccX = _CorsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                            presets.Add(key, ccX);
                                        }
                                    }
                                }

                                //Keyboard.Instance.SetCustom(keyboard_custom);

                                //HoldReader = true;
                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep0, key);
                                    if (pos > -1)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            }
                            else if (i == 2)
                            {
                                //Step 1
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep1, key);
                                    if (pos > -1)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            }
                            else if (i == 3)
                            {
                                //Step 2
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep2, key);
                                    if (pos > -1)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            }
                            else if (i == 4)
                            {
                                //Step 3
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep3, key);
                                    if (pos > -1)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            }
                            else if (i == 5)
                            {
                                //Step 4
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep4, key);
                                    if (pos > -1)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            }
                            else if (i == 6)
                            {
                                //Step 5
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep5, key);
                                    if (pos > -1)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            }
                            else if (i == 7)
                            {
                                //Step 6
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep6, key);
                                    if (pos > -1)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            }
                            else if (i == 8)
                            {
                                //Step 7
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep7, key);
                                    if (pos > -1)
                                    {
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    if (Corsairkeyids.ContainsKey(key))
                                    {
                                        //ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }


                                //presets.Clear();
                                //HoldReader = false;
                            }

                            if (i < 9)
                                Thread.Sleep(speed);
                        }
                    }
                }
            });
        }

        public void Flash1(Color burstcol, int speed, string[] regions)
        {
            lock (_CorsairFlash1)
            {
                var presets = new Dictionary<string, Color>();
                var ScrollWheel = new Color();
                var Logo = new Color();
                var Backlight = new Color();

                var ScrollWheelConv = ScrollWheel;
                var LogoConv = Logo;
                var BacklightConv = Backlight;

                for (var i = 0; i <= 8; i++)
                {
                    if (i == 0)
                    {
                        //Setup
                        if (CorsairDeviceKeyboard)
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                {
                                    var _key = Corsairkeyids[key];
                                    if (CueSDK.KeyboardSDK[_key] != null)
                                    {
                                        //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                        Color ccX = _CorsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                        presets.Add(key, ccX);
                                    }
                                }
                            }

                        if (CorsairDeviceMouse)
                        {
                            if (CueSDK.MouseSDK[CorsairLedId.B3] != null)
                            {
                                ScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                            }
                            if (CueSDK.MouseSDK[CorsairLedId.B1] != null)
                            {
                                Logo = CueSDK.MouseSDK[CorsairLedId.B1].Color;
                            }
                            if (CueSDK.MouseSDK[CorsairLedId.B4] != null)
                            {
                                Backlight = CueSDK.MouseSDK[CorsairLedId.B4].Color;
                            }
                        }

                        //Keyboard.Instance.SetCustom(keyboard_custom);

                        //HoldReader = true;
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        if (CorsairDeviceKeyboard)
                        {
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);
                            }
                        }

                        if (CorsairDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        if (CorsairDeviceKeyboard)
                        {
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);
                            }
                        }

                        if (CorsairDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", ScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", LogoConv, false);
                            ApplyMapMouseLighting("Backlight", BacklightConv, false);
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        if (CorsairDeviceKeyboard)
                        {
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);
                            }
                        }

                        if (CorsairDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        if (CorsairDeviceKeyboard)
                        {
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);
                            }
                        }

                        if (CorsairDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", ScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", LogoConv, false);
                            ApplyMapMouseLighting("Backlight", BacklightConv, false);
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        if (CorsairDeviceKeyboard)
                        {
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);
                            }
                        }

                        if (CorsairDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        if (CorsairDeviceKeyboard)
                        {
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);
                            }
                        }

                        if (CorsairDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", ScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", LogoConv, false);
                            ApplyMapMouseLighting("Backlight", BacklightConv, false);
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        if (CorsairDeviceKeyboard)
                        {
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);
                            }
                        }

                        if (CorsairDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        if (CorsairDeviceKeyboard)
                        {
                            foreach (var key in regions)
                            {
                                if (Corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);
                            }
                        }

                        if (CorsairDeviceMouse)
                        {
                            ApplyMapMouseLighting("ScrollWheel", ScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", LogoConv, false);
                            ApplyMapMouseLighting("Backlight", BacklightConv, false);
                        }

                        //HoldReader = false;
                    }

                    if (i < 8)
                        Thread.Sleep(speed);
                }
            }
        }

        private static Dictionary<string, Color> _presets = new Dictionary<string, Color>();
        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            lock (_CorsairFlash2)
            {
                var presets = new Dictionary<string, Color>();
                
                if (!_CorsairFlash2Running)
                {
                    if (CorsairDeviceMouse)
                    {
                        //CorsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B3] != null)
                        {
                            CorsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        }
                        if (CueSDK.MouseSDK[CorsairLedId.B1] != null)
                        {
                            CorsairLogo = CueSDK.MouseSDK[CorsairLedId.B1].Color;
                        }

                        CorsairScrollWheelConv = CorsairScrollWheel;
                        CorsairLogoConv = CorsairLogo;
                    }

                    if (CorsairDeviceKeyboard)
                    {
                        foreach (var key in regions)
                        {
                            if (Corsairkeyids.ContainsKey(key))
                            {
                                var _key = Corsairkeyids[key];
                                if (CueSDK.KeyboardSDK[_key] != null)
                                {
                                    //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                    Color ccX = _CorsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                    presets.Add(key, ccX);
                                }
                            }
                        }
                    }

                    _CorsairFlash2Running = true;
                    _CorsairFlash2Step = 0;
                    _presets = presets;
                }

                if (_CorsairFlash2Running)
                {
                    while (_CorsairFlash2Running)
                    {
                        if (cts.IsCancellationRequested)
                        {
                            break;
                        }

                        if (_CorsairFlash2Step == 0)
                        {
                            if (CorsairDeviceKeyboard)
                            {
                                foreach (var key in regions)
                                {
                                    if (CorsairDeviceKeyboard)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            }
                            if (CorsairDeviceMouse)
                            {
                                ApplyMapMouseLighting("CorsairScrollWheel", burstcol, false);
                                ApplyMapMouseLighting("Logo", burstcol, false);
                            }
                            _CorsairFlash2Step = 1;

                            Thread.Sleep(speed);
                        }
                        else if (_CorsairFlash2Step == 1)
                        {
                            if (CorsairDeviceKeyboard)
                            {
                                foreach (var key in regions)
                                {
                                    if (CorsairDeviceKeyboard)
                                        if (Corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, _presets[key], false);
                                }
                            }
                            if (CorsairDeviceMouse)
                            {
                                ApplyMapMouseLighting("CorsairScrollWheel", CorsairScrollWheelConv, false);
                                ApplyMapMouseLighting("Logo", CorsairLogoConv, false);
                            }
                            _CorsairFlash2Step = 0;

                            Thread.Sleep(speed);
                        }
                    }
                }
            }
        }

        private static int _CorsairFlash3Step = 0;
        private static bool _CorsairFlash3Running = false;
        static readonly object _Flash3 = new object();
        public void Flash3(System.Drawing.Color burstcol, int speed, CancellationToken cts)
        {
            try
            {
                //DeviceEffects._NumFlash
                lock (_Flash3)
                {
                    if (CorsairDeviceKeyboard == true)
                    {
                        var presets = new Dictionary<string, Color>();
                        _CorsairFlash3Running = true;
                        _CorsairFlash3Step = 0;

                        if (_CorsairFlash3Running == false)
                        {
                            /*
                            foreach (var key in DeviceEffects._GlobalKeys)
                                try
                                {
                                    if (Corsairkeyids.ContainsKey(key))
                                    {
                                        var _key = Corsairkeyids[key];
                                        //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                        if (CueSDK.KeyboardSDK[_key].Color != null)
                                        {
                                            Color ccX = _CorsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                            presets.Add(key, ccX);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    write.WriteConsole(ConsoleTypes.ERROR, "(" + key + "): " + ex.Message);
                                }
                            */


                        }
                        else
                        {
                            while (_CorsairFlash3Running == true)
                            {
                                //cts.ThrowIfCancellationRequested();

                                if (cts.IsCancellationRequested)
                                {
                                    break;
                                }

                                if (_CorsairFlash3Step == 0)
                                {
                                    foreach (var key in DeviceEffects._NumFlash)
                                    {
                                        var pos = Array.IndexOf(DeviceEffects._NumFlash, key);
                                        if (pos > -1)
                                            if (Corsairkeyids.ContainsKey(key))
                                                ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    _CorsairFlash3Step = 1;
                                }
                                else if (_CorsairFlash3Step == 1)
                                {
                                    foreach (var key in DeviceEffects._NumFlash)
                                    {
                                        var pos = Array.IndexOf(DeviceEffects._NumFlash, key);
                                        if (pos > -1)
                                            if (Corsairkeyids.ContainsKey(key))
                                                ApplyMapKeyLighting(key, Color.Black, false);
                                    }

                                    _CorsairFlash3Step = 0;


                                }

                                CueSDK.KeyboardSDK.Update();
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

        private void CorsairtransitionConst(Color col1, Color col2, bool forward, int speed)
        {
            lock (_CorsairtransitionConst)
            {
                //To be implemented
            }
        }
    }

    public static class ExceptionExtensions
    {
        public static Exception GetOriginalException(this Exception ex)
        {
            if (ex.InnerException == null) return ex;

            return ex.InnerException.GetOriginalException();
        }
    }
}