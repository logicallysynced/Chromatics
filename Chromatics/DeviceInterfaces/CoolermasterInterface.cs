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
        public static CoolermasterLib InitializeCoolermasterSdk()
        {
            CoolermasterLib coolermaster = null;
            coolermaster = new CoolermasterLib();

            var coolermasterstat = coolermaster.InitializeSdk();

            if (!coolermasterstat)
                return null;

            return coolermaster;
        }
    }

    public class CoolermasterSdkWrapper
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void KeyCallback(int iRow, int iColumn, [MarshalAs(UnmanagedType.I1)] bool bPressed);

        public const int MaxLedRow = 6;

        public const int MaxLedColumn = 22;

        public const string SdkDll = @"CoolermasterSDKDLL64.dll";

        [DllImport(SdkDll, EntryPoint = "GetNowTime")]
        public static extern IntPtr GetNowTime();

        [DllImport(SdkDll, EntryPoint = "GetNowCPUUsage", CallingConvention = CallingConvention.StdCall)]
        public static extern int GetNowCPUUsage();

        [DllImport(SdkDll, EntryPoint = "GetRamUsage")]
        public static extern uint GetRamUsage();

        [DllImport(SdkDll, EntryPoint = "GetNowVolumePeekValue")]
        public static extern float GetNowVolumePeekValue();

        [DllImport(SdkDll, EntryPoint = "SetControlDevice")]
        public static extern void SetControlDevice(DeviceIndex devIndex);

        [DllImport(SdkDll, EntryPoint = "IsDevicePlug")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsDevicePlug();

        [DllImport(SdkDll, EntryPoint = "GetDeviceLayout")]
        public static extern LayoutKeyboard GetDeviceLayout();

        [DllImport(SdkDll, EntryPoint = "EnableLedControl")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool EnableLedControl([MarshalAs(UnmanagedType.I1)] bool bEnable);

        [DllImport(SdkDll, EntryPoint = "SwitchLedEffect")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SwitchLedEffect(EffIndex iEffectIndex);

        [DllImport(SdkDll, EntryPoint = "RefreshLed")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool RefreshLed([MarshalAs(UnmanagedType.I1)] bool bAuto);

        [DllImport(SdkDll, EntryPoint = "SetFullLedColor")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SetFullLedColor(byte r, byte g, byte b);

        [DllImport(SdkDll, EntryPoint = "SetAllLedColor")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SetAllLedColor(ColorMatrix colorMatrix);

        [DllImport(SdkDll, EntryPoint = "SetLedColor")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SetLedColor(int iRow, int iColumn, byte r, byte g, byte b);

        [DllImport(SdkDll, EntryPoint = "EnableKeyInterrupt")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool EnableKeyInterrupt([MarshalAs(UnmanagedType.I1)] bool bEnable);

        [DllImport(SdkDll, EntryPoint = "SetKeyCallBack")]
        public static extern void SetKeyCallBack(KeyCallback callback);

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyColor
        {
            public byte r;
            public byte g;
            public byte b;

            public KeyColor(byte r, byte g, byte b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ColorMatrix
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxLedRow * MaxLedColumn,
                ArraySubType = UnmanagedType.Struct)] public KeyColor[,] KeyColor;
        }

        #region Enums

        public enum EffIndex
        {
            EffFullOn = 0,
            EffBreath = 1,
            EffBreathCycle = 2,
            EffSingle = 3,
            EffWave = 4,
            EffRipple = 5,
            EffCross = 6,
            EffRain = 7,
            EffStar = 8,
            EffSnake = 9,
            EffRec = 10,
            EffSpectrum = 11,
            EffRapidFire = 12,
            EffIndicator = 13,
            EffMulti1 = 224,
            EffMulti2 = 225,
            EffMulti3 = 226,
            EffMulti4 = 227,
            EffOff = 254
        }

        public enum DeviceIndex
        {
            DevMKeysL = 0,
            DevMKeysS = 1,
            DevMKeysLWhite = 2,
            DevMKeysMWhite = 3,
            DevMMouseL = 4,
            DevMMouseS = 5,
            DevMKeysM = 6,
            DevMKeysSWhite = 7
        }

        public enum DeviceType
        {
            Keyboard = 0,
            Mouse = 1
        }

        public enum LayoutKeyboard
        {
            LayoutUninit = 0,
            LayoutUs = 1,
            LayoutEu = 2
        }

        #endregion
    }

    public interface ICoolermasterSdk
    {
        bool InitializeSdk();
        void ResetCoolermasterDevices(bool deviceKeyboard, bool deviceMouse, Color basecol);
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
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private static bool _boot;

        private static readonly Dictionary<CoolermasterSdkWrapper.DeviceIndex, CoolermasterSdkWrapper.DeviceType>
            Devices = new Dictionary<CoolermasterSdkWrapper.DeviceIndex, CoolermasterSdkWrapper.DeviceType>();

        private static readonly Dictionary<string, Color> KeyboardState = new Dictionary<string, Color>();

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


        private static readonly object CoolermasterRipple1 = new object();


        private static readonly object CoolermasterRipple2 = new object();


        private static readonly object CoolermasterFlash1 = new object();

        private static int _coolermasterFlash2Step;
        private static bool _coolermasterFlash2Running;
        private static Dictionary<string, Color> _flashpresets = new Dictionary<string, Color>();
        private static readonly object CoolermasterFlash2 = new object();

        private static int _coolermasterFlash3Step;
        private static bool _coolermasterFlash3Running;
        private static readonly object CoolermasterFlash3 = new object();

        private static int _coolermasterFlash4Step;
        private static bool _coolermasterFlash4Running;
        private static Dictionary<string, Color> _flashpresets4 = new Dictionary<string, Color>();
        private static readonly object CoolermasterFlash4 = new object();

        private readonly CancellationTokenSource _ccts = new CancellationTokenSource();
        private bool _initialized;
        private bool _coolermasterDeviceKeyboard = true;

        private bool _coolermasterDeviceMouse = true;

        //private CoolermasterSdkWrapper.COLOR_MATRIX color_matrix = new CoolermasterSdkWrapper.COLOR_MATRIX();
        private CoolermasterSdkWrapper.KeyColor[,] _keyColors =
            new CoolermasterSdkWrapper.KeyColor[CoolermasterSdkWrapper.MaxLedRow,
                CoolermasterSdkWrapper.MaxLedColumn];

        //private long lastUpdateTime = 0;
        private bool _keyboardUpdated;

        private bool _peripheralUpdated;
        private Stopwatch _watch = new Stopwatch();


        public bool InitializeSdk()
        {
            try
            {
                //Initialize State

                if (!_boot)
                {
                    foreach (var key in KeyCoords)
                    {
                        KeyboardState.Add(key.Key, Color.Black);
                        Debug.WriteLine("Added " + key.Key + " to library.");
                    }

                    _boot = true;
                }

                var found = false;

                Write.WriteConsole(ConsoleTypes.Coolermaster, "Attempting to load Coolermaster SDK..");

                var devices = Enum.GetValues(typeof(CoolermasterSdkWrapper.DeviceIndex))
                    .Cast<CoolermasterSdkWrapper.DeviceIndex>();
                foreach (var d in devices)
                {
                    CoolermasterSdkWrapper.SetControlDevice(d);
                    if (CoolermasterSdkWrapper.IsDevicePlug() && CoolermasterSdkWrapper.EnableLedControl(true))
                    {
                        Write.WriteConsole(ConsoleTypes.Coolermaster, "Found Coolermaster Device: " + d);

                        if (d == CoolermasterSdkWrapper.DeviceIndex.DevMMouseL ||
                            d == CoolermasterSdkWrapper.DeviceIndex.DevMMouseS)
                            Devices.Add(d, CoolermasterSdkWrapper.DeviceType.Mouse);
                        else
                            Devices.Add(d, CoolermasterSdkWrapper.DeviceType.Keyboard);

                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    _initialized = true;
                    return false;
                }
                Write.WriteConsole(ConsoleTypes.Coolermaster, "Unable to find any valid Coolermaster devices.");
                return false;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Coolermaster, "Coolermaster SDK failed to load. Error: " + ex.Message);
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

        public void ResetCoolermasterDevices(bool deviceKeyboard, bool deviceMouse, Color basecol)
        {
            _coolermasterDeviceKeyboard = deviceKeyboard;
            _coolermasterDeviceMouse = deviceMouse;

            if (_initialized)
                if (_coolermasterDeviceKeyboard)
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
                    if (_coolermasterDeviceKeyboard && disablekeys != true)
                        foreach (var d in Devices)
                            if (d.Value == CoolermasterSdkWrapper.DeviceType.Keyboard)
                                break;
                    if (_coolermasterDeviceMouse)
                        foreach (var d in Devices)
                            if (d.Value == CoolermasterSdkWrapper.DeviceType.Mouse)
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
                    if (_coolermasterDeviceKeyboard && disablekeys != true)
                        if (Devices.Any(d => d.Value == CoolermasterSdkWrapper.DeviceType.Keyboard))
                        {
                            foreach (var key in KeyboardState)
                                KeyboardState[key.Key] = col;

                            UpdateCoolermasterStateAll(col);
                        }
                    if (_coolermasterDeviceMouse)
                        foreach (var d in Devices)
                            if (d.Value == CoolermasterSdkWrapper.DeviceType.Mouse)
                                break;
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
                    if (_coolermasterDeviceKeyboard && disablekeys != true)
                        foreach (var d in Devices)
                            if (d.Value == CoolermasterSdkWrapper.DeviceType.Keyboard)
                                break;
                    if (_coolermasterDeviceMouse)
                        foreach (var d in Devices)
                            if (d.Value == CoolermasterSdkWrapper.DeviceType.Mouse)
                                break;
                });
                MemoryTasks.Add(crSt);
                MemoryTasks.Run(crSt);
            }
            else if (type == "wave")
            {
                var crSt = new Task(() =>
                {
                    if (_coolermasterDeviceKeyboard && disablekeys != true)
                        if (Devices.Any(d => d.Value == CoolermasterSdkWrapper.DeviceType.Keyboard))
                        {
                            CoolermasterSdkWrapper.SwitchLedEffect(CoolermasterSdkWrapper.EffIndex.EffWave);
                        }
                    if (!_coolermasterDeviceMouse) return;
                    {
                        if (Devices.Any(d => d.Value == CoolermasterSdkWrapper.DeviceType.Mouse))
                        {
                            CoolermasterSdkWrapper.SwitchLedEffect(CoolermasterSdkWrapper.EffIndex.EffWave);
                        }
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
                        if (_coolermasterDeviceKeyboard && disablekeys != true)
                            foreach (var d in Devices)
                                if (d.Value == CoolermasterSdkWrapper.DeviceType.Keyboard)
                                    break;
                        if (_coolermasterDeviceMouse)
                            foreach (var d in Devices)
                                if (d.Value == CoolermasterSdkWrapper.DeviceType.Mouse)
                                    break;
                    }
                    catch (Exception ex)
                    {
                        Write.WriteConsole(ConsoleTypes.Error, "Coolermaster (Breath): " + ex.Message);
                    }
                });
                MemoryTasks.Add(crSt);
                MemoryTasks.Run(crSt);
            }
            else if (type == "pulse")
            {
                var crSt = new Task(() =>
                {
                    if (_coolermasterDeviceKeyboard && disablekeys != true)
                        foreach (var d in Devices)
                            if (d.Value == CoolermasterSdkWrapper.DeviceType.Keyboard)
                                break;
                    if (_coolermasterDeviceMouse)
                        foreach (var d in Devices)
                            if (d.Value == CoolermasterSdkWrapper.DeviceType.Mouse)
                                break;
                }, _ccts.Token);
                MemoryTasks.Add(crSt);
                MemoryTasks.Run(crSt);
                //RzPulse = true;
            }

            MemoryTasks.Cleanup();
        }


        public void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (!_initialized)
                return;

            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            try
            {
                if (_coolermasterDeviceKeyboard)
                    if (KeyCoords.ContainsKey(key))
                    {
                        KeyboardState[key] = col;
                        UpdateCoolermasterState();
                    }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, "Coolermaster (" + key + "): " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Error, "Internal Error (" + key + "): " + ex.StackTrace);
            }
        }


        public void ApplyMapMouseLighting(string region, Color col, bool clear)
        {
            if (!_initialized)
                return;

            if (_coolermasterDeviceMouse)
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

                lock (CoolermasterRipple1)
                {
                    if (_coolermasterDeviceKeyboard)
                    {
                        var presets = new Dictionary<string, Color>();


                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in DeviceEffects.GlobalKeys)
                                    if (KeyboardState.ContainsKey(key))
                                    {
                                        var ccX = KeyboardState[key];
                                        presets.Add(key, ccX);
                                    }
                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                    if (pos > -1)
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = presets[key];
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
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = presets[key];
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
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = presets[key];
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
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = presets[key];
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
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = presets[key];
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
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = presets[key];
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
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = presets[key];
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
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    }
                                    else
                                    {
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = presets[key];
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in DeviceEffects.GlobalKeys)
                                    if (KeyboardState.ContainsKey(key))
                                        KeyboardState[key] = presets[key];

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

                lock (CoolermasterRipple2)
                {
                    if (_coolermasterDeviceKeyboard)
                    {
                        var presets = new Dictionary<string, Color>();

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in DeviceEffects.GlobalKeys)
                                    if (!FfxivHotbar.Keybindwhitelist.Contains(key))
                                        if (KeyboardState.ContainsKey(key))
                                        {
                                            var ccX = KeyboardState[key];
                                            presets.Add(key, ccX);
                                        }

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
                                            if (KeyboardState.ContainsKey(key))
                                                KeyboardState[key] = burstcol;
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
                                            if (KeyboardState.ContainsKey(key))
                                                KeyboardState[key] = burstcol;
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
                                            if (KeyboardState.ContainsKey(key))
                                                KeyboardState[key] = burstcol;
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
                                            if (KeyboardState.ContainsKey(key))
                                                KeyboardState[key] = burstcol;
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
                                            if (KeyboardState.ContainsKey(key))
                                                KeyboardState[key] = burstcol;
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
                                            if (KeyboardState.ContainsKey(key))
                                                KeyboardState[key] = burstcol;
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
                                            if (KeyboardState.ContainsKey(key))
                                                KeyboardState[key] = burstcol;
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
                                            if (KeyboardState.ContainsKey(key))
                                                KeyboardState[key] = burstcol;
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

                            UpdateCoolermasterState();
                        }
                    }
                }
            });
        }

        public void Flash1(Color burstcol, int speed, string[] region)
        {
            lock (CoolermasterFlash1)
            {
                if (!_initialized)
                    return;

                var presets = new Dictionary<string, Color>();


                for (var i = 0; i <= 8; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in region)
                                if (KeyboardState.ContainsKey(key))
                                {
                                    var ccX = KeyboardState[key];
                                    presets.Add(key, ccX);
                                }

                        //HoldReader = true;
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in region)
                                if (KeyboardState.ContainsKey(key))
                                    KeyboardState[key] = burstcol;
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (KeyboardState.ContainsKey(key))
                                    KeyboardState[key] = presets[key];
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in region)
                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (KeyboardState.ContainsKey(key))
                                    KeyboardState[key] = burstcol;
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (KeyboardState.ContainsKey(key))
                                    KeyboardState[key] = presets[key];
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in region)
                                //ApplyMapKeyLighting(key, burstcol, true);
                                //refreshKeyGrid
                                if (KeyboardState.ContainsKey(key))
                                    KeyboardState[key] = burstcol;
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (KeyboardState.ContainsKey(key))
                                    KeyboardState[key] = presets[key];
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in region)
                                if (KeyboardState.ContainsKey(key))
                                    KeyboardState[key] = burstcol;
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in DeviceEffects.GlobalKeys3)
                                if (KeyboardState.ContainsKey(key))
                                    KeyboardState[key] = presets[key];

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

                lock (CoolermasterFlash2)
                {
                    var flashpresets = new Dictionary<string, Color>();


                    if (!_coolermasterFlash2Running)
                    {
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in regions)
                                if (KeyboardState.ContainsKey(key))
                                {
                                    var ccX = KeyboardState[key];
                                    flashpresets.Add(key, ccX);
                                }

                        _coolermasterFlash2Running = true;
                        _coolermasterFlash2Step = 0;
                        _flashpresets = flashpresets;
                    }

                    if (_coolermasterFlash2Running)
                        while (_coolermasterFlash2Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_coolermasterFlash2Step == 0)
                            {
                                if (_coolermasterDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;

                                    UpdateCoolermasterState();
                                }

                                _coolermasterFlash2Step = 1;
                            }
                            else if (_coolermasterFlash2Step == 1)
                            {
                                if (_coolermasterDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = _flashpresets[key];

                                    UpdateCoolermasterState();
                                }

                                _coolermasterFlash2Step = 0;
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

                lock (CoolermasterFlash3)
                {
                    if (_coolermasterDeviceKeyboard)
                    {
                        var presets = new Dictionary<string, Color>();
                        _coolermasterFlash3Running = true;
                        _coolermasterFlash3Step = 0;

                        if (_coolermasterFlash3Running == false)
                        {
                            //
                        }
                        else
                        {
                            while (_coolermasterFlash3Running)
                            {
                                if (cts.IsCancellationRequested)
                                    break;

                                if (_coolermasterFlash3Step == 0)
                                {
                                    foreach (var key in DeviceEffects.NumFlash)
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;
                                    _coolermasterFlash3Step = 1;
                                }
                                else if (_coolermasterFlash3Step == 1)
                                {
                                    foreach (var key in DeviceEffects.NumFlash)
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = Color.Black;

                                    _coolermasterFlash3Step = 0;
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

                lock (CoolermasterFlash4)
                {
                    var flashpresets = new Dictionary<string, Color>();


                    if (!_coolermasterFlash4Running)
                    {
                        if (_coolermasterDeviceKeyboard)
                            foreach (var key in regions)
                                if (KeyboardState.ContainsKey(key))
                                {
                                    var ccX = KeyboardState[key];
                                    flashpresets.Add(key, ccX);
                                }

                        _coolermasterFlash4Running = true;
                        _coolermasterFlash4Step = 0;
                        _flashpresets4 = flashpresets;
                    }

                    if (_coolermasterFlash4Running)
                        while (_coolermasterFlash4Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_coolermasterFlash4Step == 0)
                            {
                                if (_coolermasterDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = burstcol;

                                    UpdateCoolermasterState();
                                }

                                _coolermasterFlash4Step = 1;
                            }
                            else if (_coolermasterFlash4Step == 1)
                            {
                                if (_coolermasterDeviceKeyboard)
                                {
                                    foreach (var key in regions)
                                        if (KeyboardState.ContainsKey(key))
                                            KeyboardState[key] = _flashpresets4[key];

                                    UpdateCoolermasterState();
                                }

                                _coolermasterFlash4Step = 0;
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
            if (_initialized && (_keyboardUpdated || _peripheralUpdated))
            {
                _keyboardUpdated = false;
                _peripheralUpdated = false;
            }
        }

        private void ResetEffects()
        {
            if (!_initialized)
                return;

            if (_coolermasterDeviceKeyboard)
                if (Devices.Any(d => d.Value == CoolermasterSdkWrapper.DeviceType.Keyboard))
                {
                    CoolermasterSdkWrapper.SwitchLedEffect(CoolermasterSdkWrapper.EffIndex.EffOff);
                }
            if (_coolermasterDeviceMouse)
                if (Devices.Any(d => d.Value == CoolermasterSdkWrapper.DeviceType.Mouse))
                {
                    CoolermasterSdkWrapper.SwitchLedEffect(CoolermasterSdkWrapper.EffIndex.EffOff);
                }
        }

        private void UpdateCoolermasterState()
        {
            if (!_initialized)
                return;

            ResetEffects();

            foreach (var key in KeyboardState)
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