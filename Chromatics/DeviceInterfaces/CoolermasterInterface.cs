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
        public static extern bool IsDevicePlug(DeviceIndex devIndex);

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
        public static extern bool SetAllLedColor(ColorMatrix colorMatrix, DeviceIndex devIndex);

        [DllImport(SdkDll, EntryPoint = "SetLedColor")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SetLedColor(int iRow, int iColumn, byte r, byte g, byte b, DeviceIndex devIndex);

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
                ArraySubType = UnmanagedType.Struct)]
            public KeyColor[,] KeyColor;
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
            DevMKeysSWhite = 7,
            DevMM520 = 8,
            DevMM530 = 9,
            DevMK750 = 10

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

        void SetLights(Color col);
        void SetWave();
        void StopEffects();

        void SetAllLights(Color color);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapMouseLighting(string region, Color col, bool clear);

        Task Ripple1(Color burstcol, int speed);
        Task Ripple2(Color burstcol, int speed);
        void Flash1(Color burstcol, int speed, string[] region);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
        void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50);
        void CycleEffect(int interval, CancellationTokenSource token);
    }

    public class CoolermasterLib : ICoolermasterSdk
    {
        private readonly object _initLock = new object();
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private readonly List<CoolermasterSdkWrapper.DeviceIndex> _keyboards = new List<CoolermasterSdkWrapper.DeviceIndex>();
        private readonly List<CoolermasterSdkWrapper.DeviceIndex> _mice = new List<CoolermasterSdkWrapper.DeviceIndex>();
        private readonly CoolermasterSdkWrapper.ColorMatrix _colorMatrix = new CoolermasterSdkWrapper.ColorMatrix();
        private readonly Timer _updateTimer;
        /// <summary>
        /// Control whether to update the keyboard
        /// </summary>
        private bool _coolermasterDeviceKeyboard = true;
        /// <summary>
        /// Control whether to update the mouse
        /// </summary>
        private bool _coolermasterDeviceMouse = true;

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

        public bool IsInitialized { get; set; } = false;

        #region Key Mappings

        public static readonly Dictionary<string, int[]> KeyMappings = new Dictionary<string, int[]>
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
            {"OemTilde", new[] {1, 0}},
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

        public static IEnumerable<CoolermasterSdkWrapper.DeviceIndex> SupportedDevices => Enum.GetValues(typeof(CoolermasterSdkWrapper.DeviceIndex)).Cast<CoolermasterSdkWrapper.DeviceIndex>();
        public static IEnumerable<CoolermasterSdkWrapper.DeviceIndex> SupportedKeyboardDevices => SupportedDevices.Where(d => d != CoolermasterSdkWrapper.DeviceIndex.DevMMouseL && d != CoolermasterSdkWrapper.DeviceIndex.DevMMouseS);
        public static IEnumerable<CoolermasterSdkWrapper.DeviceIndex> SupportedMouseDevices => SupportedDevices.Except(SupportedKeyboardDevices);

        private static CoolermasterSdkWrapper.KeyColor MapColor(Color color) => new CoolermasterSdkWrapper.KeyColor(color.R, color.G, color.B);
        private static Color MapKeyColor(CoolermasterSdkWrapper.KeyColor color) => Color.FromArgb(1, color.r, color.g, color.b);
        private bool SetKeyColor(string key, Color color)
        {
            if (!KeyMappings.ContainsKey(key))
                return false;

            _colorMatrix.KeyColor[KeyMappings[key][0], KeyMappings[key][1]] = MapColor(color);
            return true;
        }
        private Color? GetKeyColor(string key)
        {
            if (!KeyMappings.ContainsKey(key))
                return null;

            return MapKeyColor(_colorMatrix.KeyColor[KeyMappings[key][0], KeyMappings[key][1]]);
        }

        public CoolermasterLib()
        {
            _colorMatrix.KeyColor = new CoolermasterSdkWrapper.KeyColor[CoolermasterSdkWrapper.MaxLedRow, CoolermasterSdkWrapper.MaxLedColumn];
            _updateTimer = new Timer((_) => ApplyKeyboardLighting(), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void SetAllLights(Color color)
        {
            if (!IsInitialized) return;

            if (_coolermasterDeviceKeyboard)
            {
                SetLights(color);
                ApplyKeyboardLightingSoon();
            }
        }

        public void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (!IsInitialized || !_coolermasterDeviceKeyboard)
                return;

            // clear is unused?
            // whitelist appears to be a blacklist

            // macro 5, 16, 17, 18???

            if (!bypasswhitelist && FfxivHotbar.Keybindwhitelist.Contains(key))
                return;

            if (!KeyMappings.ContainsKey(key))
            {
                Debug.WriteLine($"Unknown key: '{key}'");
                return;
            }

            SetKeyColor(key, col);

            ApplyKeyboardLightingSoon();
        }

        public void ApplyMapMouseLighting(string region, Color col, bool clear)
        {
            if (!IsInitialized || !_coolermasterDeviceKeyboard)
                return;

            foreach (var mouseDevice in _mice)
            {
                CoolermasterSdkWrapper.SetControlDevice(mouseDevice);

                switch (region)
                {
                    case "All":
                        CoolermasterSdkWrapper.SetLedColor(0, 0, col.R, col.G, col.B, mouseDevice);
                        CoolermasterSdkWrapper.SetLedColor(0, 1, col.R, col.G, col.B, mouseDevice);
                        CoolermasterSdkWrapper.SetLedColor(0, 2, col.R, col.G, col.B, mouseDevice);
                        CoolermasterSdkWrapper.SetLedColor(0, 3, col.R, col.G, col.B, mouseDevice);
                        break;
                    case "MouseFront":
                        CoolermasterSdkWrapper.SetLedColor(0, 0, col.R, col.G, col.B, mouseDevice);
                        break;
                    case "MouseScroll":
                        CoolermasterSdkWrapper.SetLedColor(0, 1, col.R, col.G, col.B, mouseDevice);
                        break;
                    case "MouseSide":
                        CoolermasterSdkWrapper.SetLedColor(0, 2, col.R, col.G, col.B, mouseDevice);
                        CoolermasterSdkWrapper.SetLedColor(0, 3, col.R, col.G, col.B, mouseDevice);
                        break;
                }
            }
        }

        public Task Ripple1(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!IsInitialized || !_coolermasterDeviceKeyboard)
                    return;

                lock (CoolermasterRipple1)
                {
                    if (_keyboards.Any())
                    {
                        var previousValues = new Dictionary<string, Color>();
                        var safeKeys = DeviceEffects.GlobalKeys.Where(KeyMappings.ContainsKey);
                        var enumerable = safeKeys.ToList();

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in enumerable)
                                {
                                    previousValues.Add(key, GetKeyColor(key).Value);
                                }
                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                    SetKeyColor(key, pos > -1 ? burstcol : previousValues[key]);
                                }
                            }
                            else if (i == 2)
                            {
                                //Step 1
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                    SetKeyColor(key, pos > -1 ? burstcol : previousValues[key]);
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
                                        SetKeyColor(key, burstcol);
                                    }
                                    else
                                    {
                                        SetKeyColor(key, previousValues[key]);
                                    }
                                }
                            }
                            else if (i == 4)
                            {
                                //Step 3
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                    SetKeyColor(key, pos > -1 ? burstcol : previousValues[key]);
                                }
                            }
                            else if (i == 5)
                            {
                                //Step 4
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                    SetKeyColor(key, pos > -1 ? burstcol : previousValues[key]);
                                }
                            }
                            else if (i == 6)
                            {
                                //Step 5
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                    SetKeyColor(key, pos > -1 ? burstcol : previousValues[key]);
                                }
                            }
                            else if (i == 7)
                            {
                                //Step 6
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                    SetKeyColor(key, pos > -1 ? burstcol : previousValues[key]);
                                }
                            }
                            else if (i == 8)
                            {
                                //Step 7
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                    SetKeyColor(key, pos > -1 ? burstcol : previousValues[key]);
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in enumerable.Where(key => previousValues.ContainsKey(key)))
                                {
                                    SetKeyColor(key, previousValues[key]);
                                }

                                previousValues.Clear();
                            }

                            if (i < 9)
                                Thread.Sleep(speed);

                            ApplyKeyboardLighting();
                        }
                    }
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!IsInitialized || !_coolermasterDeviceKeyboard)
                    return;

                var safeKeys = DeviceEffects.GlobalKeys.Except(FfxivHotbar.Keybindwhitelist);

                lock (CoolermasterRipple2)
                {
                    if (_keyboards.Any())
                    {
                        var previousValues = new Dictionary<string, Color>();
                        var enumerable = safeKeys.ToList();

                        for (var i = 0; i <= 9; i++)
                        {

                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in enumerable
                                    .Where(KeyMappings.ContainsKey))
                                {
                                    previousValues.Add(key, GetKeyColor(key).Value);
                                }
                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                    if (pos > -1)
                                        SetKeyColor(key, burstcol);
                                }
                            }
                            else if (i == 2)
                            {
                                //Step 1
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                    if (pos > -1)
                                        SetKeyColor(key, burstcol);
                                }
                            }
                            else if (i == 3)
                            {
                                //Step 2
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                                    if (pos > -1)
                                        SetKeyColor(key, burstcol);
                                }
                            }
                            else if (i == 4)
                            {
                                //Step 3
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                    if (pos > -1)
                                        SetKeyColor(key, burstcol);
                                }
                            }
                            else if (i == 5)
                            {
                                //Step 4
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                    if (pos > -1)
                                        SetKeyColor(key, burstcol);
                                }
                            }
                            else if (i == 6)
                            {
                                //Step 5
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                    if (pos > -1)
                                        SetKeyColor(key, burstcol);
                                }
                            }
                            else if (i == 7)
                            {
                                //Step 6
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                    if (pos > -1)
                                        SetKeyColor(key, burstcol);
                                }
                            }
                            else if (i == 8)
                            {
                                //Step 7
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                    if (pos > -1)
                                        SetKeyColor(key, burstcol);
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in previousValues.Keys)
                                {
                                    SetKeyColor(key, previousValues[key]);
                                }

                                previousValues.Clear();
                            }

                            if (i < 9)
                                Thread.Sleep(speed);

                            ApplyKeyboardLighting();
                        }
                    }
                }
            });
        }

        public void Flash1(Color burstcol, int speed, string[] region)
        {
            lock (CoolermasterFlash1)
            {
                if (!IsInitialized || !_coolermasterDeviceKeyboard)
                    return;

                var previousValues = new Dictionary<string, Color>();
                var mappedRegion = region.Where(KeyMappings.ContainsKey);
                var enumerable = mappedRegion.ToList();
                for (var i = 0; i <= 8; i++)
                {

                    if (i == 0)
                    {
                        //Setup

                        if (_keyboards.Any())
                            foreach (var key in enumerable)
                            {
                                previousValues.Add(key, GetKeyColor(key).Value);
                            }
                    }
                    else if (i % 2 == 1)
                    {
                        //Step 1, 3, 5, 7
                        if (_keyboards.Any())
                            foreach (var key in enumerable)
                                SetKeyColor(key, burstcol);
                    }
                    else if (i % 2 == 0)
                    {
                        //Step 2, 4, 6, 8
                        if (_keyboards.Any())
                            foreach (var key in enumerable)
                                SetKeyColor(key, previousValues[key]);
                    }

                    if (i < 8)
                        Thread.Sleep(speed);

                    ApplyKeyboardLighting();
                }
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                if (!IsInitialized || !_coolermasterDeviceKeyboard)
                    return;

                lock (CoolermasterFlash2)
                {
                    var previousValues = new Dictionary<string, Color>();
                    var safeKeys = regions.Where(KeyMappings.ContainsKey);

                    var enumerable = safeKeys.ToList();
                    if (!_coolermasterFlash2Running)
                    {
                        foreach (var key in enumerable)
                            previousValues.Add(key, GetKeyColor(key).Value);

                        _coolermasterFlash2Running = true;
                        _coolermasterFlash2Step = 0;
                        _flashpresets = previousValues;
                    }

                    if (_coolermasterFlash2Running)
                        while (_coolermasterFlash2Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_coolermasterFlash2Step == 0)
                            {
                                if (_keyboards.Any())
                                {
                                    foreach (var key in enumerable)
                                        SetKeyColor(key, burstcol);

                                    ApplyKeyboardLighting();
                                }

                                _coolermasterFlash2Step = 1;
                            }
                            else if (_coolermasterFlash2Step == 1)
                            {
                                if (_keyboards.Any())
                                {
                                    foreach (var key in enumerable)
                                        SetKeyColor(key, _flashpresets[key]);

                                    ApplyKeyboardLighting();
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
                if (!IsInitialized || !_coolermasterDeviceKeyboard)
                    return;

                lock (CoolermasterFlash3)
                {
                    if (_keyboards.Any())
                    {
                        //var previousValues = new Dictionary<string, Color>();
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
                                    foreach (var key in DeviceEffects.NumFlash.Where(KeyMappings.ContainsKey))
                                        SetKeyColor(key, burstcol);

                                    _coolermasterFlash3Step = 1;
                                }
                                else if (_coolermasterFlash3Step == 1)
                                {
                                    foreach (var key in DeviceEffects.NumFlash.Where(KeyMappings.ContainsKey))
                                        SetKeyColor(key, Color.Black);

                                    _coolermasterFlash3Step = 0;
                                }

                                ApplyKeyboardLighting();
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
                if (!IsInitialized || !_coolermasterDeviceKeyboard)
                    return;

                var safeKeys = regions.Where(KeyMappings.ContainsKey);

                lock (CoolermasterFlash4)
                {
                    var flashpresets = new Dictionary<string, Color>();

                    var enumerable = safeKeys.ToList();
                    if (!_coolermasterFlash4Running)
                    {
                        if (_keyboards.Any())
                            foreach (var key in enumerable)
                                flashpresets.Add(key, GetKeyColor(key).Value);

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
                                if (_keyboards.Any())
                                {
                                    foreach (var key in enumerable)
                                        SetKeyColor(key, burstcol);
                                }

                                _coolermasterFlash4Step = 1;
                            }
                            else if (_coolermasterFlash4Step == 1)
                            {
                                if (_keyboards.Any())
                                {
                                    foreach (var key in enumerable)
                                        SetKeyColor(key, _flashpresets4[key]);

                                }

                                _coolermasterFlash4Step = 0;
                            }

                            ApplyKeyboardLighting();
                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
                //
            }
        }

        public void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50)
        {
            if (!IsInitialized || !_coolermasterDeviceKeyboard) return;
            if (cts.IsCancellationRequested) return;


            Dictionary<string, ColorFader> colorFaderDict = new Dictionary<string, ColorFader>();
            var safeKeys = regions.Where(KeyMappings.ContainsKey);
            var enumerable = safeKeys.ToList();

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

                    if (_keyboards.Any())
                    {
                        var rndCol = toColor[rnd.Next(toColor.Length)];

                        colorFaderDict.Add(key, new ColorFader(toColor[0], rndCol, interval));
                    }
                }

                Task t = Task.Factory.StartNew(() =>
                {
                    //Thread.Sleep(500);

                    var _regions = enumerable.OrderBy(x => rnd.Next()).ToArray();

                    foreach (var key in _regions)
                    {
                        if (cts.IsCancellationRequested) return;

                        foreach (var color in colorFaderDict[key].Fade())
                        {
                            if (cts.IsCancellationRequested) return;
                            if (_keyboards.Any())
                            {
                                SetKeyColor(key, color);
                                ApplyKeyboardLightingSoon();
                            }
                        }

                        //Keyboard.SetCustomAsync(refreshKeyGrid);
                        Thread.Sleep(speed);
                    }
                });

                Thread.Sleep(colorFaderDict.Count * speed);
            }
        }

        private readonly object lockObject = new object();
        public void CycleEffect(int interval, CancellationTokenSource token)
        {
            if (!_coolermasterDeviceKeyboard) return;
            if (!_keyboards.Any())
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

                ApplyKeyboardLightingSoon();
            }
            Thread.Sleep(interval);
        }

        public bool InitializeSdk()
        {
            try
            {
                lock (_initLock)
                {
                    if (IsInitialized)
                        return true;

                    Write.WriteConsole(ConsoleTypes.Coolermaster, @"Attempting to initializer Coolermaster support...");

                    foreach (var supportedDevice in SupportedKeyboardDevices)
                    {
                        CoolermasterSdkWrapper.SetControlDevice(supportedDevice);
                        if (CoolermasterSdkWrapper.IsDevicePlug(supportedDevice))
                        {
                            Write.WriteConsole(ConsoleTypes.Coolermaster, $"Found a {supportedDevice} Coolermaster keyboard.");
                            _keyboards.Add(supportedDevice);
                            CoolermasterSdkWrapper.EnableLedControl(true);
                        }

                    }

                    foreach (var supportedDevice in SupportedMouseDevices)
                    {
                        CoolermasterSdkWrapper.SetControlDevice(supportedDevice);
                        if (CoolermasterSdkWrapper.IsDevicePlug(supportedDevice))
                        {
                            Write.WriteConsole(ConsoleTypes.Coolermaster, $"Found a {supportedDevice} Coolermaster mouse.");
                            _mice.Add(supportedDevice);
                            CoolermasterSdkWrapper.EnableLedControl(true);
                        }
                    }

                    if (_keyboards.Any() || _mice.Any())
                    {
                        IsInitialized = true;
                        return true;
                    }
                    else
                    {
                        Write.WriteConsole(ConsoleTypes.Coolermaster, @"Did not find any supported Coolermaster devices.");
                    }

                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Coolermaster, @"Coolermaster SDK failed to load. Error: " + ex.Message);
            }
            return false;
        }

        public void ResetCoolermasterDevices(bool deviceKeyboard, bool deviceMouse, Color basecol)
        {
            _coolermasterDeviceKeyboard = deviceKeyboard;
            _coolermasterDeviceMouse = deviceMouse;
        }

        public void SetLights(Color col)
        {
            if (!IsInitialized || !_coolermasterDeviceKeyboard)
                return;

            foreach (var mapping in KeyMappings)
            {
                _colorMatrix.KeyColor[mapping.Value[0], mapping.Value[1]] = MapColor(col);
            }

            ApplyKeyboardLighting();
        }

        public void SetWave()
        {
        }

        public void Shutdown()
        {
            foreach (var keyboard in _keyboards)
            {
                CoolermasterSdkWrapper.SetControlDevice(keyboard);
                CoolermasterSdkWrapper.EnableLedControl(false);
            }
        }

        public void StopEffects()
        {
        }

        private void ApplyKeyboardLighting()
        {
            if (!IsInitialized || !_coolermasterDeviceKeyboard)
                return;

            foreach (var keyboardDevice in _keyboards)
            {
                CoolermasterSdkWrapper.SetControlDevice(keyboardDevice);
                CoolermasterSdkWrapper.SetAllLedColor(_colorMatrix, keyboardDevice);
            }

            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void ApplyKeyboardLightingSoon()
        {
            if (!IsInitialized || !_coolermasterDeviceKeyboard)
                return;

            _updateTimer.Change(TimeSpan.FromMilliseconds(50), Timeout.InfiniteTimeSpan);
        }
    }
}