using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.CorsairLibs;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;
using CUE.NET;
using CUE.NET.Brushes;
using CUE.NET.Devices.Generic;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Groups;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.VisualBasic.Devices;

/* Contains all Corsair SDK code for detection, initilization, states and effects.
 * 
 * 
 */

namespace Chromatics.DeviceInterfaces
{
    public class CorsairInterface
    {
        public static CorsairLib InitializeCorsairSdk()
        {
            CorsairLib corsair = null;
            corsair = new CorsairLib();

            var corsairstat = corsair.InitializeSdk();

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
        bool InitializeSdk();

        void ResetCorsairDevices(bool deviceKeyboard, bool deviceKeypad, bool deviceMouse, bool deviceMousepad,
            bool deviceHeadset, Color basecol);

        void CorsairUpdateLed();
        void SetLights(Color col);

        void UpdateState(string type, Color col, bool disablekeys, [Optional] Color col2, [Optional] bool direction,
            [Optional] int speed);

        void SetAllLights(Color color);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapMouseLighting(string region, Color col, bool clear);
        void ApplyMapLogoLighting(string key, Color col, bool clear);
        void ApplyMapPadLighting(string region, Color col, bool clear);
        void ApplyMapHeadsetLighting(Color col, bool clear);
        void ApplyMapStandLighting(string region, Color col, bool clear);

        Task Ripple1(Color burstcol, int speed, Color baseColor);
        Task Ripple2(Color burstcol, int speed);
        void Flash1(Color burstcol, int speed, string[] regions);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
        void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50);
        void CycleEffect(int interval, CancellationTokenSource token);
    }

    public class CorsairLib : ICorsairSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private static readonly object Corsairtransition = new object();
        private static readonly object CorsairRipple1 = new object();
        private static readonly object CorsairRipple2 = new object();
        private static readonly object CorsairFlash1 = new object();
        private static readonly object CorsairFlash2 = new object();
        private static readonly object CorsairFlash4 = new object();
        private static readonly object _CorsairtransitionConst = new object();

        private static Dictionary<string, Color> _presets = new Dictionary<string, Color>();

        private static int _corsairFlash3Step;
        private static bool _corsairFlash3Running;
        private static readonly object _Flash3 = new object();

        private static Dictionary<string, Color> _presets4 = new Dictionary<string, Color>();

        //Handle device send/recieve
        private readonly CancellationTokenSource _ccts = new CancellationTokenSource();

        #region Effect Steps

        private readonly Dictionary<string, CorsairLedId> _corsairkeyids = new Dictionary<string, CorsairLedId>
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
            {"Strip14", CorsairLedId.Invalid},
            {"Lightbar1", CorsairLedId.Lightbar1},
            {"Lightbar2", CorsairLedId.Lightbar2},
            {"Lightbar3", CorsairLedId.Lightbar3},
            {"Lightbar4", CorsairLedId.Lightbar4},
            {"Lightbar5", CorsairLedId.Lightbar5},
            {"Lightbar6", CorsairLedId.Lightbar6},
            {"Lightbar7", CorsairLedId.Lightbar7},
            {"Lightbar8", CorsairLedId.Lightbar8},
            {"Lightbar9", CorsairLedId.Lightbar9},
            {"Lightbar10", CorsairLedId.Lightbar10},
            {"Lightbar11", CorsairLedId.Lightbar11},
            {"Lightbar12", CorsairLedId.Lightbar12},
            {"Lightbar13", CorsairLedId.Lightbar13},
            {"Lightbar14", CorsairLedId.Lightbar14},
            {"Lightbar15", CorsairLedId.Lightbar15},
            {"Lightbar16", CorsairLedId.Lightbar16},
            {"Lightbar17", CorsairLedId.Lightbar17},
            {"Lightbar18", CorsairLedId.Lightbar18},
            {"Lightbar19", CorsairLedId.Lightbar19},
            {"HeadsetStandZone1", CorsairLedId.HeadsetStandZone1},
            {"HeadsetStandZone2", CorsairLedId.HeadsetStandZone2},
            {"HeadsetStandZone3", CorsairLedId.HeadsetStandZone3},
            {"HeadsetStandZone4", CorsairLedId.HeadsetStandZone4},
            {"HeadsetStandZone5", CorsairLedId.HeadsetStandZone5},
            {"HeadsetStandZone6", CorsairLedId.HeadsetStandZone6},
            {"HeadsetStandZone7", CorsairLedId.HeadsetStandZone7},
            {"HeadsetStandZone8", CorsairLedId.HeadsetStandZone8},
            {"HeadsetStandZone9", CorsairLedId.HeadsetStandZone9},
        };

        #endregion

        private ListLedGroup _corsairAllHeadsetLed;

        //Define Corsair LED Groups
        private ListLedGroup _corsairAllKeyboardLed;

        private ListLedGroup _corsairAllMouseLed;
        private ListLedGroup _corsairAllMousepadLed;
        private ListLedGroup _corsairAllStandLed;
        private bool _corsairFlash2Running;
        private int _corsairFlash2Step;
        private bool _corsairFlash4Running;
        private int _corsairFlash4Step;

        private KeyMapBrush _corsairKeyboardIndvBrush;
        private ListLedGroup _corsairKeyboardIndvLed;
        private KeyMapBrush _corsairMouseIndvBrush;
        private ListLedGroup _corsairMouseIndvLed;
        private KeyMapBrush _corsairMousepadIndvBrush;
        private ListLedGroup _corsairMousepadIndvLed;
        private KeyMapBrush _corsairStandIndvBrush;
        private ListLedGroup _corsairStandIndvLed;

        private bool _corsairDeviceHeadset = true;
        private bool _corsairDeviceKeyboard = true;
        private bool _corsairDeviceKeypad = true;
        private bool _corsairDeviceMouse = true;
        private bool _corsairDeviceMousepad = true;
        private bool _corsairDeviceStand = true;

        private Color _corsairLogo;
        private Color _corsairLogoConv;
        private Color _corsairScrollWheel;
        private Color _corsairScrollWheelConv;
        private Dictionary<string, Color> presets = new Dictionary<string, Color>();

        private bool pause;

        public bool InitializeSdk()
        {
            Write.WriteConsole(ConsoleTypes.Corsair, "Attempting to load CUE SDK..");

            try
            {
                CueSDK.Initialize();

                _corsairKeyboardIndvBrush = new KeyMapBrush();
                _corsairKeyboardIndvLed = new ListLedGroup(CueSDK.KeyboardSDK, CueSDK.KeyboardSDK);
                _corsairAllKeyboardLed = new ListLedGroup(CueSDK.KeyboardSDK, CueSDK.KeyboardSDK);
                _corsairAllKeyboardLed.ZIndex = 1;
                _corsairKeyboardIndvLed.ZIndex = 10;
                _corsairKeyboardIndvLed.Brush = _corsairKeyboardIndvBrush;
                _corsairAllKeyboardLed.Brush = (SolidColorBrush) Color.Black;
                

                _corsairMouseIndvBrush = new KeyMapBrush();
                _corsairMouseIndvLed = new ListLedGroup(CueSDK.MouseSDK, CueSDK.MouseSDK);
                _corsairAllMouseLed = new ListLedGroup(CueSDK.MouseSDK, CueSDK.MouseSDK);
                _corsairAllMouseLed.ZIndex = 1;
                _corsairMouseIndvLed.ZIndex = 10;
                _corsairMouseIndvLed.Brush = _corsairMouseIndvBrush;
                _corsairAllMouseLed.Brush = (SolidColorBrush) Color.Black;

                _corsairStandIndvBrush = new KeyMapBrush();
                _corsairStandIndvLed = new ListLedGroup(CueSDK.HeadsetStandSDK, CueSDK.HeadsetStandSDK);
                _corsairAllStandLed = new ListLedGroup(CueSDK.HeadsetStandSDK, CueSDK.HeadsetStandSDK);
                _corsairAllStandLed.ZIndex = 1;
                _corsairStandIndvLed.ZIndex = 10;
                _corsairStandIndvLed.Brush = _corsairStandIndvBrush;
                _corsairAllStandLed.Brush = (SolidColorBrush) Color.Black;

                _corsairMousepadIndvBrush = new KeyMapBrush();
                _corsairMousepadIndvLed = new ListLedGroup(CueSDK.MousematSDK, CueSDK.MousematSDK);
                _corsairAllMousepadLed = new ListLedGroup(CueSDK.MousematSDK, CueSDK.MousematSDK);
                _corsairAllMousepadLed.ZIndex = 1;
                _corsairMousepadIndvLed.ZIndex = 10;
                _corsairMousepadIndvLed.Brush = _corsairMousepadIndvBrush;
                _corsairAllMousepadLed.Brush = (SolidColorBrush) Color.Black;

                _corsairAllHeadsetLed = new ListLedGroup(CueSDK.HeadsetSDK, CueSDK.HeadsetSDK);
                _corsairAllHeadsetLed.ZIndex = 1;
                _corsairAllHeadsetLed.Brush = (SolidColorBrush) Color.Black;

                var corsairver = CueSDK.ProtocolDetails.ServerVersion.Split('.');
                var cV = int.Parse(corsairver[0]);

                if (cV < 2)
                {
                    Write.WriteConsole(ConsoleTypes.Error,
                        "Corsair device support requires CUE2 Version 2.0.0 or higher to operate. Please download the latest version of CUE2 from the Corsair website.");
                    return false;
                }

                Write.WriteConsole(ConsoleTypes.Corsair,
                    "CUE SDK Loaded (" + CueSDK.ProtocolDetails.SdkVersion + "/" +
                    CueSDK.ProtocolDetails.ServerVersion + ")");
                

                CueSDK.UpdateMode = UpdateMode.Continuous;
                //ResetCorsairDevices();

                if (_corsairDeviceHeadset && !string.IsNullOrEmpty(CueSDK.HeadsetSDK?.HeadsetDeviceInfo?.Model))
                {
                    try
                    {
                        _corsairDeviceHeadset = CueSDK.HeadsetSDK.DeviceInfo.Model != "VOID Wireless Demo";
                    }
                    catch (Exception e)
                    {
                        _corsairDeviceHeadset = false;
                        Console.WriteLine(e.InnerException);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Corsair, "CUE SDK failed to load. EX: " + ex.Message);
                return false;
            }
        }

        public void ResetCorsairDevices(bool deviceKeyboard, bool deviceKeypad, bool deviceMouse, bool deviceMousepad,
            bool deviceHeadset, Color basecol)
        {
            pause = true;
            
            if (_corsairDeviceKeyboard && !deviceKeyboard)
                _corsairAllKeyboardLed.Brush = (SolidColorBrush)basecol;

            if (_corsairDeviceHeadset && !deviceHeadset)
                _corsairAllHeadsetLed.Brush = (SolidColorBrush)basecol;
            
            if (_corsairDeviceMouse && !deviceMouse)
                _corsairAllMouseLed.Brush = (SolidColorBrush)basecol;

            if (_corsairDeviceMousepad && !deviceMousepad)
                _corsairAllMousepadLed.Brush = (SolidColorBrush)basecol;

            if (_corsairDeviceHeadset)
                _corsairAllStandLed.Brush = (SolidColorBrush)basecol;

            _corsairDeviceKeyboard = deviceKeyboard;
            _corsairDeviceKeypad = deviceKeypad;
            _corsairDeviceMouse = deviceMouse;
            _corsairDeviceMousepad = deviceMousepad;
            _corsairDeviceHeadset = deviceHeadset;
            _corsairDeviceStand = true;

            if (_corsairDeviceHeadset)
            {
                try
                {
                    _corsairDeviceHeadset = CueSDK.HeadsetSDK?.DeviceInfo?.Model != "VOID Wireless Demo";
                }
                catch (Exception e)
                {
                    _corsairDeviceHeadset = false;
                    Console.WriteLine(e.InnerException);
                }
            }
            //UpdateState("static", basecol, false);
            pause = false;
        }


        public void CorsairUpdateLed()
        {
            if (pause) return;

            if (_corsairDeviceHeadset && !string.IsNullOrEmpty(CueSDK.HeadsetSDK?.HeadsetDeviceInfo?.Model)) CueSDK.HeadsetSDK.Update();
            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model)) CueSDK.KeyboardSDK.Update();
            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model)) CueSDK.MouseSDK.Update();
            if (_corsairDeviceMousepad && !string.IsNullOrEmpty(CueSDK.MousematSDK?.MousematDeviceInfo?.Model)) CueSDK.MousematSDK.Update();
            if (_corsairDeviceStand && !string.IsNullOrEmpty(CueSDK.HeadsetStandSDK?.HeadsetStandDeviceInfo?.Model)) CueSDK.HeadsetStandSDK.Update();
        }

        public void SetAllLights(Color color)
        {
            if (pause) return;

            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
            {
                _corsairAllKeyboardLed.Brush = (SolidColorBrush)color;
                CueSDK.KeyboardSDK.Update(true);
            }

            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
            {
                _corsairAllMouseLed.Brush = (SolidColorBrush)color;
                CueSDK.MouseSDK.Update(true);
            }

            if (_corsairDeviceMousepad && !string.IsNullOrEmpty(CueSDK.MousematSDK?.MousematDeviceInfo?.Model))
            {
                _corsairAllMousepadLed.Brush = (SolidColorBrush)color;
                CueSDK.MousematSDK.Update(true);
            }

            if (_corsairDeviceHeadset && !string.IsNullOrEmpty(CueSDK.HeadsetSDK?.HeadsetDeviceInfo?.Model))
            {
                _corsairAllHeadsetLed.Brush = (SolidColorBrush)color;
                CueSDK.HeadsetSDK.Update(true);
            }
        }

        public void SetLights(Color col)
        {
            if (pause) return;

            try
            {
                if (!_corsairDeviceKeyboard || string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model)) return;

                _corsairAllKeyboardLed.Brush = (SolidColorBrush) col;
                CueSDK.KeyboardSDK.Update(true);
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Corsair (Static): " + ex.Message);
            }
        }

        public void UpdateState(string type, Color col, bool disablekeys, [Optional] Color col2,
            [Optional] bool direction, [Optional] int speed)
        {
            MemoryTasks.Cleanup();
            if (pause) return;
            //SolidColorBrush _CorsairAllLEDBrush = new SolidColorBrush(col);

            if (type == "reset")
            {
                try
                {
                    if (_corsairDeviceHeadset)
                    {
                        //
                    }
                    if (_corsairDeviceKeyboard && disablekeys != true)
                    {
                        //
                    }
                    if (_corsairDeviceKeypad)
                    {
                        //Not Implemented
                    }
                    if (_corsairDeviceMouse)
                    {
                        //
                    }
                    if (_corsairDeviceMousepad)
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
                    if (_corsairDeviceHeadset)
                        _corsairAllHeadsetLed.Brush = (SolidColorBrush) col;
                    if (_corsairDeviceKeyboard && disablekeys != true)
                    {
                        _corsairAllKeyboardLed.Brush = (SolidColorBrush) col;
                        CueSDK.KeyboardSDK.Update();
                    }

                    if (_corsairDeviceKeypad)
                    {
                        //Not Implemented
                    }
                    if (_corsairDeviceMouse)
                        _corsairAllMouseLed.Brush = (SolidColorBrush) col;
                    if (_corsairDeviceMousepad)
                        _corsairAllMousepadLed.Brush = (SolidColorBrush) col;
                    if (_corsairDeviceStand)
                        _corsairAllStandLed.Brush = (SolidColorBrush)col;
                }
                catch (Exception ex)
                {
                    Write.WriteConsole(ConsoleTypes.Error, "Corsair (Static)" + ex.Message);
                }
            }
            else if (type == "transition")
            {
                var crSt = new Task(() =>
                {
                    if (_corsairDeviceHeadset)
                    {
                        //CueSDK.HeadsetSDK.Brush = (SolidColorBrush)col;
                        //_CorsairAllHeadsetLED.Brush = _CorsairAllLEDBrush;
                        //_CorsairAllHeadsetLED.ZIndex = 1;
                        //CueSDK.HeadsetSDK.Update();
                    }
                    if (_corsairDeviceKeyboard && disablekeys != true)
                    {
                        //Corsairtransition(col, direction);
                    }
                    if (_corsairDeviceKeypad)
                    {
                        //Not Implemented
                    }
                    if (_corsairDeviceMouse)
                    {
                        //CueSDK.MouseSDK.Brush = (SolidColorBrush)col;
                        //_CorsairAllMouseLED.Brush = _CorsairAllLEDBrush;
                        //_CorsairAllMouseLED.ZIndex = 1;
                        //CueSDK.MouseSDK.Update();
                    }
                    if (_corsairDeviceMousepad)
                    {
                        //CueSDK.MousematSDK.Brush = (SolidColorBrush)col;
                        //_CorsairAllMousepadLED.Brush = _CorsairAllLEDBrush;
                        //_CorsairAllMousepadLED.ZIndex = 1;
                        //CueSDK.MousematSDK.Update();
                    }
                });
                MemoryTasks.Add(crSt);
                MemoryTasks.Run(crSt);
            }
            else if (type == "wave")
            {
                var crSt = new Task(() =>
                {
                    try
                    {
                        if (_corsairDeviceHeadset)
                        {
                            //Headset.Instance.SetEffect(Corale.Colore.Corsair.Headset.Effects.Effect.SpectrumCycling);
                        }
                        if (_corsairDeviceKeyboard && disablekeys != true)
                        {
                            //Keyboard.Instance.SetWave(Corale.Colore.Corsair.Keyboard.Effects.Direction.LeftToRight);
                        }
                        if (_corsairDeviceKeypad)
                        {
                            //Not Implemented
                        }
                        if (_corsairDeviceMouse)
                        {
                            //Mouse.Instance.SetWave(Corale.Colore.Corsair.Mouse.Effects.Direction.FrontToBack);
                        }
                        if (_corsairDeviceMousepad)
                        {
                            //Mousepad.Instance.SetWave(Corale.Colore.Corsair.Mousepad.Effects.Direction.LeftToRight);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Corsair (Wave): " + ex.Message);
                    }
                });
                MemoryTasks.Add(crSt);
                MemoryTasks.Run(crSt);
            }
            else if (type == "breath")
            {
                var crSt = new Task(() =>
                {
                    try
                    {
                        if (_corsairDeviceHeadset)
                        {
                            //Headset.Instance.SetBreathing(RzCol);
                        }
                        if (_corsairDeviceKeypad)
                        {
                            //Keypad.Instance.SetBreathing(RzCol, RzCol2);
                        }
                        if (_corsairDeviceMouse)
                        {
                            //Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.Backlight);
                            //Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.Logo);
                            //Mouse.Instance.SetBreathing(RzCol, RzCol2, Led.ScrollWheel);
                        }
                        if (_corsairDeviceMousepad)
                        {
                            //Mousepad.Instance.SetBreathing(RzCol, RzCol2);
                        }
                        if (_corsairDeviceKeyboard && disablekeys != true)
                        {
                            //Keyboard.Instance.SetBreathing(RzCol, RzCol2);
                        }
                    }
                    catch (Exception ex)
                    {
                        Write.WriteConsole(ConsoleTypes.Error, "Corsair (Breath): " + ex.Message);
                    }
                });
                MemoryTasks.Add(crSt);
                MemoryTasks.Run(crSt);
            }
            else if (type == "pulse")
            {
                var crSt = new Task(() =>
                {
                    if (_corsairDeviceHeadset)
                        _corsairAllHeadsetLed.Brush = (SolidColorBrush) col;
                    if (_corsairDeviceKeyboard && disablekeys != true)
                    {
                        //CorsairtransitionConst(col, col2, true, speed);
                    }
                    if (_corsairDeviceKeypad)
                    {
                        //Not Implemented
                    }
                    if (_corsairDeviceMouse)
                        _corsairAllMouseLed.Brush = (SolidColorBrush) col;
                    if (_corsairDeviceMousepad)
                        _corsairAllMousepadLed.Brush = (SolidColorBrush) col;
                    if (_corsairDeviceStand)
                        _corsairAllStandLed.Brush = (SolidColorBrush)col;
                }, _ccts.Token);
                MemoryTasks.Add(crSt);
                MemoryTasks.Run(crSt);
                //RzPulse = true;
            }

            //CorsairUpdateLED();
            MemoryTasks.Cleanup();
        }

        public void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (pause) return;

            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;
            
            try
            {
                if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                    if (_corsairkeyids.ContainsKey(key))
                        if (CueSDK.KeyboardSDK[_corsairkeyids[key]] != null)
                            _corsairKeyboardIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[key], col);
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Corsair (" + key + "): " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Error, "Internal Error (" + key + "): " + ex.StackTrace);
            }
        }

        public void ApplyMapMouseLighting(string region, Color col, bool clear)
        {
            if (pause) return;

            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                if (_corsairkeyids.ContainsKey(region))
                    if (CueSDK.MouseSDK[_corsairkeyids[region]] != null)
                        _corsairMouseIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[region], col);
        }

        public void ApplyMapLogoLighting(string key, Color col, bool clear)
        {
            if (pause) return;
            //Not Implemented
        }

        public void ApplyMapPadLighting(string region, Color col, bool clear)
        {
            if (pause) return;

            if (_corsairDeviceMousepad && !string.IsNullOrEmpty(CueSDK.MousematSDK?.MousematDeviceInfo?.Model))
                if (_corsairkeyids.ContainsKey(region))
                    if (CueSDK.MousematSDK[_corsairkeyids[region]] != null)
                        _corsairMousepadIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[region], col);
        }

        public void ApplyMapStandLighting(string region, Color col, bool clear)
        {
            if (pause) return;

            if (_corsairDeviceStand && !string.IsNullOrEmpty(CueSDK.HeadsetStandSDK?.HeadsetStandDeviceInfo?.Model))
                if (_corsairkeyids.ContainsKey(region))
                    if (CueSDK.HeadsetStandSDK[_corsairkeyids[region]] != null)
                        _corsairStandIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[region], col);
        }

        public void ApplyMapHeadsetLighting(Color col, bool clear)
        {
            if (pause) return;

            if (!_corsairDeviceHeadset || string.IsNullOrEmpty(CueSDK.HeadsetSDK?.HeadsetDeviceInfo?.Model)) return;
            var cc = new CorsairColor(col);

            if (CueSDK.HeadsetSDK[CorsairLedId.LeftLogo].Color == cc) return;

            CueSDK.HeadsetSDK[CorsairLedId.LeftLogo].Color = cc;
            CueSDK.HeadsetSDK[CorsairLedId.RightLogo].Color = cc;
        }

        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                lock (CorsairRipple1)
                {
                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                    {
                        var presets = new Dictionary<string, Color>();

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in DeviceEffects.GlobalKeys)
                                    try
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                        {
                                            var _key = _corsairkeyids[key];
                                            //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                            if (CueSDK.KeyboardSDK[_key] != null)
                                            {
                                                Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                                presets.Add(key, ccX);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Write.WriteConsole(ConsoleTypes.Error, "(" + key + "): " + ex.Message);
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
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
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
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
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
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
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
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
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
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
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
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
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
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
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
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in DeviceEffects.GlobalKeys)
                                    if (_corsairkeyids.ContainsKey(key))
                                        ApplyMapKeyLighting(key, presets[key], false);

                                ApplyMapKeyLighting("D1", baseColor, false);
                                ApplyMapKeyLighting("D2", baseColor, false);
                                ApplyMapKeyLighting("D3", baseColor, false);
                                ApplyMapKeyLighting("D4", baseColor, false);
                                ApplyMapKeyLighting("D5", baseColor, false);
                                ApplyMapKeyLighting("D6", baseColor, false);
                                ApplyMapKeyLighting("D7", baseColor, false);
                                ApplyMapKeyLighting("D8", baseColor, false);
                                ApplyMapKeyLighting("D9", baseColor, false);
                                ApplyMapKeyLighting("D0", baseColor, false);
                                ApplyMapKeyLighting("OemMinus", baseColor, false);
                                ApplyMapKeyLighting("OemEquals", baseColor, false);

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
                lock (CorsairRipple2)
                {
                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                    {
                        var presets = new Dictionary<string, Color>();
                        //uint burstcol = new Corale.Colore.Core.Color(RzCol.R, RzCol.G, RzCol.B);
                        var safeKeys = DeviceEffects.GlobalKeys.Except(FfxivHotbar.Keybindwhitelist);
                        var enumerable = safeKeys.ToList();

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                                foreach (var s in enumerable)
                                    if (_corsairkeyids.ContainsKey(s))
                                    {
                                        var key = _corsairkeyids[s];
                                        if (CueSDK.KeyboardSDK[key] != null)
                                        {
                                            //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                            Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(key);
                                            presets.Add(s, ccX);
                                        }
                                    }
                            else if (i == 1)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 2)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 3)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 4)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 5)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 6)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 7)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 8)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 9)
                                foreach (var key in enumerable)
                                    if (_corsairkeyids.ContainsKey(key))
                                    {
                                        //ApplyMapKeyLighting(key, presets[key], false);
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
            if (pause) return;

            lock (CorsairFlash1)
            {
                var presets = new Dictionary<string, Color>();
                var scrollWheel = new Color();
                var logo = new Color();
                var backlight = new Color();

                var scrollWheelConv = scrollWheel;
                var logoConv = logo;
                var backlightConv = backlight;

                for (var i = 0; i <= 8; i++)
                {
                    if (i == 0)
                    {
                        //Setup
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                {
                                    var _key = _corsairkeyids[key];
                                    if (CueSDK.KeyboardSDK[_key] != null)
                                    {
                                        //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                        Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                        presets.Add(key, ccX);
                                    }
                                }

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            if (CueSDK.MouseSDK[CorsairLedId.B3] != null)
                                scrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                            if (CueSDK.MouseSDK[CorsairLedId.B1] != null)
                                logo = CueSDK.MouseSDK[CorsairLedId.B1].Color;
                            if (CueSDK.MouseSDK[CorsairLedId.B4] != null)
                                backlight = CueSDK.MouseSDK[CorsairLedId.B4].Color;
                        }

                        //Keyboard.Instance.SetCustom(keyboard_custom);

                        //HoldReader = true;
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //HoldReader = false;
                    }

                    if (i < 8)
                        Thread.Sleep(speed);
                }
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (pause) return;

            lock (CorsairFlash2)
            {
                var presets = new Dictionary<string, Color>();

                if (!_corsairFlash2Running)
                {
                    if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                    {
                        //CorsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B3] != null)
                            _corsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B1] != null)
                            _corsairLogo = CueSDK.MouseSDK[CorsairLedId.B1].Color;

                        _corsairScrollWheelConv = _corsairScrollWheel;
                        _corsairLogoConv = _corsairLogo;
                    }

                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                        foreach (var key in regions)
                            if (_corsairkeyids.ContainsKey(key))
                            {
                                var _key = _corsairkeyids[key];
                                if (CueSDK.KeyboardSDK[_key] != null)
                                {
                                    //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                    Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                    presets.Add(key, ccX);
                                }
                            }

                    _corsairFlash2Running = true;
                    _corsairFlash2Step = 0;
                    _presets = presets;
                }

                if (_corsairFlash2Running)
                    while (_corsairFlash2Running)
                    {
                        if (cts.IsCancellationRequested)
                            break;

                        if (_corsairFlash2Step == 0)
                        {
                            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                                foreach (var key in regions)
                                    if (_corsairDeviceKeyboard)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                            {
                                ApplyMapMouseLighting("CorsairScrollWheel", burstcol, false);
                                ApplyMapMouseLighting("Logo", burstcol, false);
                            }
                            _corsairFlash2Step = 1;

                            Thread.Sleep(speed);
                        }
                        else if (_corsairFlash2Step == 1)
                        {
                            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                                foreach (var key in regions)
                                    if (_corsairDeviceKeyboard)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, _presets[key], false);
                            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                            {
                                ApplyMapMouseLighting("CorsairScrollWheel", _corsairScrollWheelConv, false);
                                ApplyMapMouseLighting("Logo", _corsairLogoConv, false);
                            }
                            _corsairFlash2Step = 0;

                            Thread.Sleep(speed);
                        }
                    }
            }
        }

        public void Flash3(Color burstcol, int speed, CancellationToken cts)
        {
            if (pause) return;

            try
            {
                //DeviceEffects._NumFlash
                lock (_Flash3)
                {
                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                    {
                        var presets = new Dictionary<string, Color>();
                        _corsairFlash3Running = true;
                        _corsairFlash3Step = 0;

                        if (_corsairFlash3Running == false)
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
                            while (_corsairFlash3Running)
                            {

                                if (cts.IsCancellationRequested)
                                    break;

                                if (_corsairFlash3Step == 0)
                                {
                                    foreach (var key in DeviceEffects.NumFlash)
                                    {
                                        var pos = Array.IndexOf(DeviceEffects.NumFlash, key);
                                        if (pos > -1)
                                            if (_corsairkeyids.ContainsKey(key))
                                                ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    _corsairFlash3Step = 1;
                                }
                                else if (_corsairFlash3Step == 1)
                                {
                                    foreach (var key in DeviceEffects.NumFlash)
                                    {
                                        var pos = Array.IndexOf(DeviceEffects.NumFlash, key);
                                        if (pos > -1)
                                            if (_corsairkeyids.ContainsKey(key))
                                                ApplyMapKeyLighting(key, Color.Black, false);
                                    }

                                    _corsairFlash3Step = 0;
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

        public void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (pause) return;

            lock (CorsairFlash4)
            {
                var presets = new Dictionary<string, Color>();

                if (!_corsairFlash4Running)
                {
                    if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                    {
                        //CorsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B3] != null)
                            _corsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B1] != null)
                            _corsairLogo = CueSDK.MouseSDK[CorsairLedId.B1].Color;

                        _corsairScrollWheelConv = _corsairScrollWheel;
                        _corsairLogoConv = _corsairLogo;
                    }

                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                        foreach (var key in regions)
                            if (_corsairkeyids.ContainsKey(key))
                            {
                                var _key = _corsairkeyids[key];
                                if (CueSDK.KeyboardSDK[_key] != null)
                                {
                                    //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                    Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                    presets.Add(key, ccX);
                                }
                            }

                    _corsairFlash4Running = true;
                    _corsairFlash4Step = 0;
                    _presets4 = presets;
                }

                if (_corsairFlash4Running)
                    while (_corsairFlash2Running)
                    {
                        if (cts.IsCancellationRequested)
                            break;

                        if (_corsairFlash4Step == 0)
                        {
                            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                                foreach (var key in regions)
                                    if (_corsairDeviceKeyboard)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                            {
                                ApplyMapMouseLighting("CorsairScrollWheel", burstcol, false);
                                ApplyMapMouseLighting("Logo", burstcol, false);
                            }
                            _corsairFlash4Step = 1;

                            Thread.Sleep(speed);
                        }
                        else if (_corsairFlash4Step == 1)
                        {
                            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                                foreach (var key in regions)
                                    if (_corsairDeviceKeyboard)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, _presets4[key], false);
                            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                            {
                                ApplyMapMouseLighting("CorsairScrollWheel", _corsairScrollWheelConv, false);
                                ApplyMapMouseLighting("Logo", _corsairLogoConv, false);
                            }
                            _corsairFlash4Step = 0;

                            Thread.Sleep(speed);
                        }
                    }
            }
        }

        private void Transition(Color col, bool forward)
        {
            if (pause) return;

            lock (Corsairtransition)
            {
                if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
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

        private void CorsairtransitionConst(Color col1, Color col2, bool forward, int speed)
        {
            if (pause) return;

            lock (_CorsairtransitionConst)
            {
                //To be implemented
            }
        }

        public void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50)
        {
            if (!_corsairDeviceKeyboard || string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model)) return;
            if (cts.IsCancellationRequested) return;

            var presets = new Dictionary<string, Color>();

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

                    if (_corsairkeyids.ContainsKey(key))
                    {
                        var rndCol = Color.Black;
                        var keyid = _corsairkeyids[key];

                        do
                        {
                            rndCol = toColor[rnd.Next(toColor.Length)];
                        }
                        while ((Color)_corsairKeyboardIndvBrush.CorsairGetColorReference(keyid) == rndCol);

                        colorFaderDict.Add(key, new ColorFader(_corsairKeyboardIndvBrush.CorsairGetColorReference(keyid), rndCol, interval));
                    }
                }

                Task t = Task.Factory.StartNew(() =>
                {
                    //Thread.Sleep(500);

                    var _regions = regions.OrderBy(x => rnd.Next()).ToArray();

                    foreach (var key in _regions)
                    {
                        if (cts.IsCancellationRequested) return;
                        if (!_corsairkeyids.ContainsKey(key)) continue;

                        foreach (var color in colorFaderDict[key].Fade())
                        {
                            if (cts.IsCancellationRequested) return;
                            if (_corsairkeyids.ContainsKey(key))
                            {
                                ApplyMapKeyLighting(key, color, false);
                            }
                        }

                        //Keyboard.SetCustomAsync(refreshKeyGrid);
                        Thread.Sleep(speed);
                    }
                });

                Thread.Sleep(colorFaderDict.Count * speed);
            }
        }

        public void CycleEffect(int interval, CancellationTokenSource token)
        {
            if (!_corsairDeviceKeyboard) return;
            if (string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
            {
                return;
            }

            while (true)
            {
                for (var x = 0; x <= 250; x += 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    var col = Color.FromArgb((int)Math.Ceiling((double)(250 * 100) / 255),
                        (int)Math.Ceiling((double)(x * 100) / 255), 0);
                    
                    SetAllLights(col);

                }
                for (var x = 250; x >= 5; x -= 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    var col = Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255),
                        (int)Math.Ceiling((double)(250 * 100) / 255), 0);

                    SetAllLights(col);

                }
                for (var x = 0; x <= 250; x += 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    var col = Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255),
                        (int)Math.Ceiling((double)(250 * 100) / 255), 0);

                    SetAllLights(col);

                }
                for (var x = 250; x >= 5; x -= 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    var col = Color.FromArgb(0, (int)Math.Ceiling((double)(x * 100) / 255),
                        (int)Math.Ceiling((double)(250 * 100) / 255));

                    SetAllLights(col);
                }
                for (var x = 0; x <= 250; x += 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    var col = Color.FromArgb((int)Math.Ceiling((double)(x * 100) / 255), 0,
                        (int)Math.Ceiling((double)(250 * 100) / 255));

                    SetAllLights(col);

                }
                for (var x = 250; x >= 5; x -= 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    var col = Color.FromArgb((int)Math.Ceiling((double)(250 * 100) / 255), 0,
                        (int)Math.Ceiling((double)(x * 100) / 255));

                    SetAllLights(col);

                }
                if (token.IsCancellationRequested) break;

            }
            Thread.Sleep(interval);
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