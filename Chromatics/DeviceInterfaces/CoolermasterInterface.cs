using System;
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

namespace Chromatics.DeviceInterfaces
{
    internal class CoolermasterInterface
    {
        public static CoolermasterLib InitializeCoolermasterSDK()
        {
            CoolermasterLib coolermaster = null;
            coolermaster = new CoolermasterLib();

            var coolermasterstat = coolermaster.InitializeSDK();

            if (!coolermasterstat)
                return null;

            return coolermaster;
        }
    }

    public class CoolermasterSdkWrapper
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void KEY_CALLBACK(int iRow, int iColumn, [MarshalAs(UnmanagedType.I1)] bool bPressed);

        public const int MAX_LED_ROW = 6;

        public const int MAX_LED_COLUMN = 22;

        public const string sdkDLL = @"CoolermasterSDKDLL64.dll";

        [DllImport(sdkDLL, EntryPoint = "GetNowTime")]
        public static extern IntPtr GetNowTime();

        [DllImport(sdkDLL, EntryPoint = "GetNowCPUUsage", CallingConvention = CallingConvention.StdCall)]
        public static extern int GetNowCPUUsage();

        [DllImport(sdkDLL, EntryPoint = "GetRamUsage")]
        public static extern uint GetRamUsage();

        [DllImport(sdkDLL, EntryPoint = "GetNowVolumePeekValue")]
        public static extern float GetNowVolumePeekValue();

        [DllImport(sdkDLL, EntryPoint = "SetControlDevice")]
        public static extern void SetControlDevice(DEVICE_INDEX devIndex);

        [DllImport(sdkDLL, EntryPoint = "IsDevicePlug")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsDevicePlug();

        [DllImport(sdkDLL, EntryPoint = "GetDeviceLayout")]
        public static extern LAYOUT_KEYBOARD GetDeviceLayout();

        [DllImport(sdkDLL, EntryPoint = "EnableLedControl")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool EnableLedControl([MarshalAs(UnmanagedType.I1)] bool bEnable);

        [DllImport(sdkDLL, EntryPoint = "SwitchLedEffect")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SwitchLedEffect(EFF_INDEX iEffectIndex);

        [DllImport(sdkDLL, EntryPoint = "RefreshLed")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool RefreshLed([MarshalAs(UnmanagedType.I1)] bool bAuto);

        [DllImport(sdkDLL, EntryPoint = "SetFullLedColor")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SetFullLedColor(byte r, byte g, byte b);

        [DllImport(sdkDLL, EntryPoint = "SetAllLedColor")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SetAllLedColor(COLOR_MATRIX colorMatrix);

        [DllImport(sdkDLL, EntryPoint = "SetLedColor")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SetLedColor(int iRow, int iColumn, byte r, byte g, byte b);

        [DllImport(sdkDLL, EntryPoint = "EnableKeyInterrupt")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool EnableKeyInterrupt([MarshalAs(UnmanagedType.I1)] bool bEnable);

        [DllImport(sdkDLL, EntryPoint = "SetKeyCallBack")]
        public static extern void SetKeyCallBack(KEY_CALLBACK callback);

        [StructLayout(LayoutKind.Sequential)]
        public struct KEY_COLOR
        {
            public byte r;
            public byte g;
            public byte b;

            public KEY_COLOR(byte r, byte g, byte b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COLOR_MATRIX
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_LED_ROW * MAX_LED_COLUMN,
                ArraySubType = UnmanagedType.Struct)] public KEY_COLOR[,] KeyColor;
        }

        #region Enums

        public enum EFF_INDEX
        {
            EFF_FULL_ON = 0,
            EFF_BREATH = 1,
            EFF_BREATH_CYCLE = 2,
            EFF_SINGLE = 3,
            EFF_WAVE = 4,
            EFF_RIPPLE = 5,
            EFF_CROSS = 6,
            EFF_RAIN = 7,
            EFF_STAR = 8,
            EFF_SNAKE = 9,
            EFF_REC = 10,
            EFF_SPECTRUM = 11,
            EFF_RAPID_FIRE = 12,
            EFF_INDICATOR = 13,
            EFF_MULTI_1 = 224,
            EFF_MULTI_2 = 225,
            EFF_MULTI_3 = 226,
            EFF_MULTI_4 = 227,
            EFF_OFF = 254
        }

        public enum DEVICE_INDEX
        {
            DEV_MKeys_L = 0,
            DEV_MKeys_S = 1,
            DEV_MKeys_L_White = 2,
            DEV_MKeys_M_White = 3,
            DEV_MMouse_L = 4,
            DEV_MMouse_S = 5,
            DEV_MKeys_M = 6,
            DEV_MKeys_S_White = 7
        }

        public enum DEVICE_TYPE
        {
            KEYBOARD = 0,
            MOUSE = 1
        }

        public enum LAYOUT_KEYBOARD
        {
            LAYOUT_UNINIT = 0,
            LAYOUT_US = 1,
            LAYOUT_EU = 2
        }

        #endregion
    }

    public interface ICoolermasterSdk
    {
        bool InitializeSDK();
        void ResetCoolermasterDevices(bool DeviceKeyboard, bool DeviceMouse, Color basecol);
        void Shutdown();

        void UpdateState(string type, Color col, bool disablekeys, [Optional] Color col2,
            [Optional] bool direction, [Optional] int speed);

        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapMouseLighting(string region, Color col, bool clear);

        Task Ripple1(Color burstcol, int speed);
        Task Ripple2(Color burstcol, int speed);
        void Flash1(Color burstcol, int speed, string[] region);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
        void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
    }

    public class CoolermasterLib : ICoolermasterSdk
    {
        private static readonly ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private static bool _boot;

        private static readonly Dictionary<CoolermasterSdkWrapper.DEVICE_INDEX, CoolermasterSdkWrapper.DEVICE_TYPE>
            _devices = new Dictionary<CoolermasterSdkWrapper.DEVICE_INDEX, CoolermasterSdkWrapper.DEVICE_TYPE>();

        private static readonly Dictionary<string, Color> _KeyboardState = new Dictionary<string, Color>();

        #region keytranslator

        public static readonly Dictionary<string, int[]> KeyCoords = new Dictionary<string, int[]>
        {
            {"Escape", new[] {0, 0}},
            {"F1", new[] {0, 1}},
            {"F2", new[] {0, 2}},
            {"F3", new[] {0, 3}},
            {"F4", new[] {0, 4}},
            {"F5", new[] {0, 6}},
            {"F6", new[] {0, 7}},
            {"F7", new[] {0, 8}},
            {"F8", new[] {0, 9}},
            {"F9", new[] {0, 11}},
            {"F10", new[] {0, 12}},
            {"F11", new[] {0, 13}},
            {"F12", new[] {0, 14}},
            {"PrintScreen", new[] {0, 15}},
            {"Scroll", new[] {0, 16}},
            {"Pause", new[] {0, 17}},
            {"Macro1", new[] {0, 18}},
            {"Macro2", new[] {0, 19}},
            {"Macro3", new[] {0, 20}},
            {"Macro4", new[] {0, 21}},
            {"OmeTilde", new[] {1, 0}},
            {"D1", new[] {1, 1}},
            {"D2", new[] {1, 2}},
            {"D3", new[] {1, 3}},
            {"D4", new[] {1, 4}},
            {"D5", new[] {1, 5}},
            {"D6", new[] {1, 6}},
            {"D7", new[] {1, 7}},
            {"D8", new[] {1, 8}},
            {"D9", new[] {1, 9}},
            {"D0", new[] {1, 10}},
            {"OemMinus", new[] {1, 11}},
            {"OemEquals", new[] {1, 12}},
            {"Backspace", new[] {1, 14}},
            {"Insert", new[] {1, 15}},
            {"Home", new[] {1, 16}},
            {"PageUp", new[] {1, 17}},
            {"NumLock", new[] {1, 18}},
            {"NumDivide", new[] {1, 19}},
            {"NumMultiply", new[] {1, 20}},
            {"NumSubtract", new[] {1, 21}},
            {"Tab", new[] {2, 0}},
            {"Q", new[] {2, 1}},
            {"W", new[] {2, 2}},
            {"E", new[] {2, 3}},
            {"R", new[] {2, 4}},
            {"T", new[] {2, 5}},
            {"Y", new[] {2, 6}},
            {"U", new[] {2, 7}},
            {"I", new[] {2, 8}},
            {"O", new[] {2, 9}},
            {"P", new[] {2, 10}},
            {"OemLeftBracket", new[] {2, 11}},
            {"OemRightBracket", new[] {2, 12}},
            {"OemBackslash", new[] {2, 14}},
            {"Delete", new[] {2, 15}},
            {"End", new[] {2, 16}},
            {"PageDown", new[] {2, 17}},
            {"Num7", new[] {2, 18}},
            {"Num8", new[] {2, 19}},
            {"Num9", new[] {2, 20}},
            {"NumAdd", new[] {2, 21}},
            {"CapsLock", new[] {3, 0}},
            {"A", new[] {3, 1}},
            {"S", new[] {3, 2}},
            {"D", new[] {3, 3}},
            {"F", new[] {3, 4}},
            {"G", new[] {3, 5}},
            {"H", new[] {3, 6}},
            {"J", new[] {3, 7}},
            {"K", new[] {3, 8}},
            {"L", new[] {3, 9}},
            {"OemSemicolon", new[] {3, 10}},
            {"OemApostrophe", new[] {3, 11}},
            {"JpnYen", new[] {3, 12}},
            {"Enter", new[] {3, 14}},
            {"Num4", new[] {3, 18}},
            {"Num5", new[] {3, 19}},
            {"Num6", new[] {3, 20}},
            {"LeftShift", new[] {4, 0}},
            {"EurPound", new[] {4, 1}},
            {"Z", new[] {4, 2}},
            {"X", new[] {4, 3}},
            {"C", new[] {4, 4}},
            {"V", new[] {4, 5}},
            {"B", new[] {4, 6}},
            {"N", new[] {4, 7}},
            {"M", new[] {4, 8}},
            {"OemComma", new[] {4, 9}},
            {"OemPeriod", new[] {4, 10}},
            {"OemSlash", new[] {4, 11}},
            {"RightShift", new[] {4, 14}},
            {"Up", new[] {4, 16}},
            {"Num1", new[] {4, 18}},
            {"Num2", new[] {4, 19}},
            {"Num3", new[] {4, 20}},
            {"NumEnter", new[] {4, 21}},
            {"LeftControl", new[] {5, 0}},
            {"LeftWindows", new[] {5, 1}},
            {"LeftAlt", new[] {5, 2}},
            {"Space", new[] {5, 6}},
            {"RightAlt", new[] {5, 10}},
            {"RightWindows", new[] {5, 11}},
            {"RightMenu", new[] {5, 12}},
            {"RightControl", new[] {5, 14}},
            {"Left", new[] {5, 15}},
            {"Down", new[] {5, 16}},
            {"Right", new[] {5, 17}},
            {"Num0", new[] {5, 18}},
            {"NumDecimal", new[] {5, 20}}
        };

        #endregion


        private static readonly object _CoolermasterRipple1 = new object();


        private static readonly object _CoolermasterRipple2 = new object();


        private static readonly object _CoolermasterFlash1 = new object();

        private static int _CoolermasterFlash2Step;
        private static bool _CoolermasterFlash2Running;
        private static Dictionary<string, Color> _flashpresets = new Dictionary<string, Color>();
        private static readonly object _CoolermasterFlash2 = new object();

        private static int _CoolermasterFlash3Step;
        private static bool _CoolermasterFlash3Running;
        private static readonly object _CoolermasterFlash3 = new object();

        private static int _CoolermasterFlash4Step;
        private static bool _CoolermasterFlash4Running;
        private static Dictionary<string, Color> _flashpresets4 = new Dictionary<string, Color>();
        private static readonly object _CoolermasterFlash4 = new object();

        private readonly CancellationTokenSource CCTS = new CancellationTokenSource();
        private bool _initialized;
        private bool CoolermasterDeviceKeyboard = true;

        private bool CoolermasterDeviceMouse = true;

        //private CoolermasterSdkWrapper.COLOR_MATRIX color_matrix = new CoolermasterSdkWrapper.COLOR_MATRIX();
        private CoolermasterSdkWrapper.KEY_COLOR[,] key_colors =
            new CoolermasterSdkWrapper.KEY_COLOR[CoolermasterSdkWrapper.MAX_LED_ROW,
                CoolermasterSdkWrapper.MAX_LED_COLUMN];

        //private long lastUpdateTime = 0;
        private bool keyboard_updated;

        private bool peripheral_updated;
        private Stopwatch watch = new Stopwatch();


        public bool InitializeSDK()
        {
            try
            {
                //Initialize State

                if (!_boot)
                {
                    foreach (var key in KeyCoords)
                    {
                        _KeyboardState.Add(key.Key, Color.Black);
                        Debug.WriteLine("Added " + key.Key + " to library.");
                    }

                    _boot = true;
                }

                var _found = false;

                write.WriteConsole(ConsoleTypes.COOLERMASTER, "Attempting to load Coolermaster SDK..");

                var devices = Enum.GetValues(typeof(CoolermasterSdkWrapper.DEVICE_INDEX))
                    .Cast<CoolermasterSdkWrapper.DEVICE_INDEX>();
                foreach (var d in devices)
                {
                    CoolermasterSdkWrapper.SetControlDevice(d);
                    if (CoolermasterSdkWrapper.IsDevicePlug() && CoolermasterSdkWrapper.EnableLedControl(true))
                    {
                        write.WriteConsole(ConsoleTypes.COOLERMASTER, "Found Coolermaster Device: " + d);

                        if (d == CoolermasterSdkWrapper.DEVICE_INDEX.DEV_MMouse_L ||
                            d == CoolermasterSdkWrapper.DEVICE_INDEX.DEV_MMouse_S)
                            _devices.Add(d, CoolermasterSdkWrapper.DEVICE_TYPE.MOUSE);
                        else
                            _devices.Add(d, CoolermasterSdkWrapper.DEVICE_TYPE.KEYBOARD);

                        _found = true;
                        break;
                    }
                }

                if (_found)
                {
                    _initialized = true;
                    return false;
                }
                write.WriteConsole(ConsoleTypes.COOLERMASTER, "Unable to find any valid Coolermaster devices.");
                return false;
            }
            catch (Exception ex)
            {
                write.WriteConsole(ConsoleTypes.COOLERMASTER, "Coolermaster SDK failed to load. Error: " + ex.Message);
                return false;
            }
        }

        public void Shutdown()
        {
            if (_initialized)
            {
                CoolermasterSdkWrapper.EnableLedControl(false);
                _initialized = false;
            }
        }

        public void ResetCoolermasterDevices(bool DeviceKeyboard, bool DeviceMouse, Color basecol)
        {
            CoolermasterDeviceKeyboard = DeviceKeyboard;
            CoolermasterDeviceMouse = DeviceMouse;

            if (_initialized)
                if (CoolermasterDeviceKeyboard)
                    UpdateState("static", basecol, false);
                else
                    Shutdown();
        }

        public void UpdateState(string type, Color col, bool disablekeys, [Optional] Color col2,
            [Optional] bool direction, [Optional] int speed)
        {
            if (!_initialized)
                return;

            MemoryTasks.Cleanup();
            ResetEffects();

            if (type == "reset")
            {
                try
                {
                    if (CoolermasterDeviceKeyboard && disablekeys != true)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.KEYBOARD)
                                break;
                    if (CoolermasterDeviceMouse)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.MOUSE)
                                break;
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
                    if (CoolermasterDeviceKeyboard && disablekeys != true)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.KEYBOARD)
                            {
                                foreach (var key in _KeyboardState)
                                    _KeyboardState[key.Key] = col;

                                UpdateCoolermasterStateAll(col);
                                break;
                            }
                    if (CoolermasterDeviceMouse)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.MOUSE)
                                break;
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
                    if (CoolermasterDeviceKeyboard && disablekeys != true)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.KEYBOARD)
                                break;
                    if (CoolermasterDeviceMouse)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.MOUSE)
                                break;
                });
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
            }
            else if (type == "wave")
            {
                var _CrSt = new Task(() =>
                {
                    if (CoolermasterDeviceKeyboard && disablekeys != true)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.KEYBOARD)
                            {
                                CoolermasterSdkWrapper.SwitchLedEffect(CoolermasterSdkWrapper.EFF_INDEX.EFF_WAVE);
                                break;
                            }
                    if (CoolermasterDeviceMouse)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.MOUSE)
                            {
                                CoolermasterSdkWrapper.SwitchLedEffect(CoolermasterSdkWrapper.EFF_INDEX.EFF_WAVE);
                                break;
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
                        if (CoolermasterDeviceKeyboard && disablekeys != true)
                            foreach (var d in _devices)
                                if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.KEYBOARD)
                                    break;
                        if (CoolermasterDeviceMouse)
                            foreach (var d in _devices)
                                if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.MOUSE)
                                    break;
                    }
                    catch (Exception ex)
                    {
                        write.WriteConsole(ConsoleTypes.ERROR, "Coolermaster (Breath): " + ex.Message);
                    }
                });
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
            }
            else if (type == "pulse")
            {
                var _CrSt = new Task(() =>
                {
                    if (CoolermasterDeviceKeyboard && disablekeys != true)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.KEYBOARD)
                                break;
                    if (CoolermasterDeviceMouse)
                        foreach (var d in _devices)
                            if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.MOUSE)
                                break;
                }, CCTS.Token);
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
                //RzPulse = true;
            }

            MemoryTasks.Cleanup();
        }


        public void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (!_initialized)
                return;

            if (FFXIVHotbar.keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            try
            {
                if (CoolermasterDeviceKeyboard)
                    if (KeyCoords.ContainsKey(key))
                    {
                        _KeyboardState[key] = col;
                        UpdateCoolermasterState();
                    }
            }
            catch (Exception ex)
            {
                write.WriteConsole(ConsoleTypes.ERROR, "Coolermaster (" + key + "): " + ex.Message);
                write.WriteConsole(ConsoleTypes.ERROR, "Internal Error (" + key + "): " + ex.StackTrace);
            }
        }


        public void ApplyMapMouseLighting(string region, Color col, bool clear)
        {
            if (!_initialized)
                return;

            if (CoolermasterDeviceMouse)
            {
                //Unimplemented
            }
        }

        public Task Ripple1(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!_initialized)
                    return;

                lock (_CoolermasterRipple1)
                {
                    if (CoolermasterDeviceKeyboard)
                    {
                        var presets = new Dictionary<string, Color>();


                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in DeviceEffects._GlobalKeys)
                                    if (_KeyboardState.ContainsKey(key))
                                    {
                                        var ccX = _KeyboardState[key];
                                        presets.Add(key, ccX);
                                    }
                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep0, key);
                                    if (pos > -1)
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = presets[key];
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
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = presets[key];
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
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = presets[key];
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
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = presets[key];
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
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = presets[key];
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
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = presets[key];
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
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = presets[key];
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
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = presets[key];
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in DeviceEffects._GlobalKeys)
                                    if (_KeyboardState.ContainsKey(key))
                                        _KeyboardState[key] = presets[key];

                                presets.Clear();
                                //HoldReader = false;

                                //MemoryReaderLock.Enabled = true;
                            }

                            if (i < 9)
                                Thread.Sleep(speed);

                            UpdateCoolermasterState();
                        }
                    }
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!_initialized)
                    return;

                lock (_CoolermasterRipple2)
                {
                    if (CoolermasterDeviceKeyboard)
                    {
                        var presets = new Dictionary<string, Color>();

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in DeviceEffects._GlobalKeys)
                                    if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                        if (_KeyboardState.ContainsKey(key))
                                        {
                                            var ccX = _KeyboardState[key];
                                            presets.Add(key, ccX);
                                        }

                                //HoldReader = true;
                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep0, key);
                                    if (pos > -1)
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                            if (_KeyboardState.ContainsKey(key))
                                                _KeyboardState[key] = burstcol;
                                }
                            }
                            else if (i == 2)
                            {
                                //Step 1
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep1, key);
                                    if (pos > -1)
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                            if (_KeyboardState.ContainsKey(key))
                                                _KeyboardState[key] = burstcol;
                                }
                            }
                            else if (i == 3)
                            {
                                //Step 2
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep2, key);
                                    if (pos > -1)
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                            if (_KeyboardState.ContainsKey(key))
                                                _KeyboardState[key] = burstcol;
                                }
                            }
                            else if (i == 4)
                            {
                                //Step 3
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep3, key);
                                    if (pos > -1)
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                            if (_KeyboardState.ContainsKey(key))
                                                _KeyboardState[key] = burstcol;
                                }
                            }
                            else if (i == 5)
                            {
                                //Step 4
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep4, key);
                                    if (pos > -1)
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                            if (_KeyboardState.ContainsKey(key))
                                                _KeyboardState[key] = burstcol;
                                }
                            }
                            else if (i == 6)
                            {
                                //Step 5
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep5, key);
                                    if (pos > -1)
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                            if (_KeyboardState.ContainsKey(key))
                                                _KeyboardState[key] = burstcol;
                                }
                            }
                            else if (i == 7)
                            {
                                //Step 6
                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._PulseOutStep6, key);
                                    if (pos > -1)
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                            if (_KeyboardState.ContainsKey(key))
                                                _KeyboardState[key] = burstcol;
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
                                        if (!FFXIVHotbar.keybindwhitelist.Contains(key))
                                            if (_KeyboardState.ContainsKey(key))
                                                _KeyboardState[key] = burstcol;
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in DeviceEffects._GlobalKeys)
                                {
                                    //ApplyMapKeyLighting(key, presets[key], true);
                                }


                                //presets.Clear();
                                presets.Clear();
                                //HoldReader = false;
                            }

                            if (i < 9)
                                Thread.Sleep(speed);

                            UpdateCoolermasterState();
                        }
                    }
                }
            });
        }

        public void Flash1(Color burstcol, int speed, string[] region)
        {
            lock (_CoolermasterFlash1)
            {
                if (!_initialized)
                    return;

                var presets = new Dictionary<string, Color>();


                for (var i = 0; i <= 8; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in region)
                                if (_KeyboardState.ContainsKey(key))
                                {
                                    var ccX = _KeyboardState[key];
                                    presets.Add(key, ccX);
                                }

                        //HoldReader = true;
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in region)
                                if (_KeyboardState.ContainsKey(key))
                                    _KeyboardState[key] = burstcol;
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in DeviceEffects._GlobalKeys3)
                                if (_KeyboardState.ContainsKey(key))
                                    _KeyboardState[key] = presets[key];
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in region)
                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (_KeyboardState.ContainsKey(key))
                                    _KeyboardState[key] = burstcol;
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in DeviceEffects._GlobalKeys3)
                                if (_KeyboardState.ContainsKey(key))
                                    _KeyboardState[key] = presets[key];
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in region)
                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (_KeyboardState.ContainsKey(key))
                                    _KeyboardState[key] = burstcol;
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in DeviceEffects._GlobalKeys3)
                                if (_KeyboardState.ContainsKey(key))
                                    _KeyboardState[key] = presets[key];
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in region)
                                if (_KeyboardState.ContainsKey(key))
                                    _KeyboardState[key] = burstcol;
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in DeviceEffects._GlobalKeys3)
                                if (_KeyboardState.ContainsKey(key))
                                    _KeyboardState[key] = presets[key];

                        presets.Clear();
                        //HoldReader = false;
                    }

                    if (i < 8)
                        Thread.Sleep(speed);

                    UpdateCoolermasterState();
                }
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                if (!_initialized)
                    return;

                lock (_CoolermasterFlash2)
                {
                    var flashpresets = new Dictionary<string, Color>();


                    if (!_CoolermasterFlash2Running)
                    {
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in regions)
                                if (_KeyboardState.ContainsKey(key))
                                {
                                    var ccX = _KeyboardState[key];
                                    flashpresets.Add(key, ccX);
                                }

                        _CoolermasterFlash2Running = true;
                        _CoolermasterFlash2Step = 0;
                        _flashpresets = flashpresets;
                    }

                    if (_CoolermasterFlash2Running)
                        while (_CoolermasterFlash2Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_CoolermasterFlash2Step == 0)
                            {
                                if (CoolermasterDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;

                                    UpdateCoolermasterState();
                                }

                                _CoolermasterFlash2Step = 1;
                            }
                            else if (_CoolermasterFlash2Step == 1)
                            {
                                if (CoolermasterDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = _flashpresets[key];

                                    UpdateCoolermasterState();
                                }

                                _CoolermasterFlash2Step = 0;
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
                if (!_initialized)
                    return;

                lock (_CoolermasterFlash3)
                {
                    if (CoolermasterDeviceKeyboard)
                    {
                        var presets = new Dictionary<string, Color>();
                        _CoolermasterFlash3Running = true;
                        _CoolermasterFlash3Step = 0;

                        if (_CoolermasterFlash3Running == false)
                        {
                            //
                        }
                        else
                        {
                            while (_CoolermasterFlash3Running)
                            {
                                if (cts.IsCancellationRequested)
                                    break;

                                if (_CoolermasterFlash3Step == 0)
                                {
                                    foreach (var key in DeviceEffects._NumFlash)
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;
                                    _CoolermasterFlash3Step = 1;
                                }
                                else if (_CoolermasterFlash3Step == 1)
                                {
                                    foreach (var key in DeviceEffects._NumFlash)
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = Color.Black;

                                    _CoolermasterFlash3Step = 0;
                                }

                                UpdateCoolermasterState();
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
            try
            {
                if (!_initialized)
                    return;

                lock (_CoolermasterFlash4)
                {
                    var flashpresets = new Dictionary<string, Color>();


                    if (!_CoolermasterFlash4Running)
                    {
                        if (CoolermasterDeviceKeyboard)
                            foreach (var key in regions)
                                if (_KeyboardState.ContainsKey(key))
                                {
                                    var ccX = _KeyboardState[key];
                                    flashpresets.Add(key, ccX);
                                }

                        _CoolermasterFlash4Running = true;
                        _CoolermasterFlash4Step = 0;
                        _flashpresets4 = flashpresets;
                    }

                    if (_CoolermasterFlash4Running)
                        while (_CoolermasterFlash4Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_CoolermasterFlash4Step == 0)
                            {
                                if (CoolermasterDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = burstcol;

                                    UpdateCoolermasterState();
                                }

                                _CoolermasterFlash4Step = 1;
                            }
                            else if (_CoolermasterFlash4Step == 1)
                            {
                                if (CoolermasterDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (_KeyboardState.ContainsKey(key))
                                            _KeyboardState[key] = _flashpresets4[key];

                                    UpdateCoolermasterState();
                                }

                                _CoolermasterFlash4Step = 0;
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

        private void Reset()
        {
            if (_initialized && (keyboard_updated || peripheral_updated))
            {
                keyboard_updated = false;
                peripheral_updated = false;
            }
        }

        private void ResetEffects()
        {
            if (!_initialized)
                return;

            if (CoolermasterDeviceKeyboard)
                foreach (var d in _devices)
                    if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.KEYBOARD)
                    {
                        CoolermasterSdkWrapper.SwitchLedEffect(CoolermasterSdkWrapper.EFF_INDEX.EFF_OFF);
                        break;
                    }
            if (CoolermasterDeviceMouse)
                foreach (var d in _devices)
                    if (d.Value == CoolermasterSdkWrapper.DEVICE_TYPE.MOUSE)
                    {
                        CoolermasterSdkWrapper.SwitchLedEffect(CoolermasterSdkWrapper.EFF_INDEX.EFF_OFF);
                        break;
                    }
        }

        private void UpdateCoolermasterState()
        {
            if (!_initialized)
                return;

            ResetEffects();

            foreach (var key in _KeyboardState)
                if (KeyCoords.ContainsKey(key.Key))
                {
                    var keyid = KeyCoords[key.Key];
                    var col = key.Value;
                    CoolermasterSdkWrapper.SetLedColor(keyid[0], keyid[1], col.R, col.G, col.B);
                }
        }

        private void UpdateCoolermasterStateAll(Color col)
        {
            if (!_initialized)
                return;

            ResetEffects();

            CoolermasterSdkWrapper.SetFullLedColor(col.R, col.G, col.B);
        }
    }
}