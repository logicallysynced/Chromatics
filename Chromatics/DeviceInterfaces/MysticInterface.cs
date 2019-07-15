using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

/* Contains all MysticLight SDK code for detection, initialization, states and effects.
 * 
 * 
 */

namespace Chromatics.DeviceInterfaces
{
    public class MysticInterface
    {
        private static ILogWrite _write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static Mystic InitializeMysticSdk()
        {
            Mystic mystic = null;
            if (Process.GetProcessesByName("MysticLight").Length > 0)
            {
                mystic = new Mystic();
                var result = mystic.InitializeLights();
                if (!result)
                    return null;
            }
            return mystic;
        }
    }

    public class MysticSdkWrapper
    {
        #region Libary Management

        private static IntPtr _dllHandle = IntPtr.Zero;

        /// <summary>
        /// Gets the loaded architecture (x64/x86).
        /// </summary>
        internal static string LoadedArchitecture { get; private set; }

        /// <summary>
        /// Reloads the SDK.
        /// </summary>
        internal static void Reload()
        {
            UnloadMsiSDK();
            LoadMsiSDK();
        }

        private static void LoadMsiSDK()
        {
            if (_dllHandle != IntPtr.Zero) return;

            string dllPath = "MysticLight_SDK.dll";

            _dllHandle = LoadLibrary(dllPath);

            _initializePointer = (InitializePointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_Initialize"), typeof(InitializePointer));
            _getDeviceInfoPointer = (GetDeviceInfoPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetDeviceInfo"), typeof(GetDeviceInfoPointer));
            _getLedInfoPointer = (GetLedInfoPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetLedInfo"), typeof(GetLedInfoPointer));
            _getLedColorPointer = (GetLedColorPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetLedColor"), typeof(GetLedColorPointer));
            _getLedStylePointer = (GetLedStylePointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetLedStyle"), typeof(GetLedStylePointer));
            _getLedMaxBrightPointer = (GetLedMaxBrightPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetLedMaxBright"), typeof(GetLedMaxBrightPointer));
            _getLedBrightPointer = (GetLedBrightPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetLedBright"), typeof(GetLedBrightPointer));
            _getLedMaxSpeedPointer = (GetLedMaxSpeedPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetLedMaxSpeed"), typeof(GetLedMaxSpeedPointer));
            _getLedSpeedPointer = (GetLedSpeedPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetLedSpeed"), typeof(GetLedSpeedPointer));
            _setLedColorPointer = (SetLedColorPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_SetLedColor"), typeof(SetLedColorPointer));
            _setLedStylePointer = (SetLedStylePointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_SetLedStyle"), typeof(SetLedStylePointer));
            _setLedBrightPointer = (SetLedBrightPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_SetLedBright"), typeof(SetLedBrightPointer));
            _setLedSpeedPointer = (SetLedSpeedPointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_SetLedSpeed"), typeof(SetLedSpeedPointer));
            _getErrorMessagePointer = (GetErrorMessagePointer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(_dllHandle, "MLAPI_GetErrorMessage"), typeof(GetErrorMessagePointer));
        }

        public static void UnloadMsiSDK()
        {
            if (_dllHandle == IntPtr.Zero) return;

            while (FreeLibrary(_dllHandle)) ;
            _dllHandle = IntPtr.Zero;
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr dllHandle);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr dllHandle, string name);

        #endregion

        #region SDK-METHODS

        #region Pointers

        private static InitializePointer _initializePointer;
        private static GetDeviceInfoPointer _getDeviceInfoPointer;
        private static GetLedInfoPointer _getLedInfoPointer;
        private static GetLedColorPointer _getLedColorPointer;
        private static GetLedStylePointer _getLedStylePointer;
        private static GetLedMaxBrightPointer _getLedMaxBrightPointer;
        private static GetLedBrightPointer _getLedBrightPointer;
        private static GetLedMaxSpeedPointer _getLedMaxSpeedPointer;
        private static GetLedSpeedPointer _getLedSpeedPointer;
        private static SetLedColorPointer _setLedColorPointer;
        private static SetLedStylePointer _setLedStylePointer;
        private static SetLedBrightPointer _setLedBrightPointer;
        private static SetLedSpeedPointer _setLedSpeedPointer;
        private static GetErrorMessagePointer _getErrorMessagePointer;

        #endregion

        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InitializePointer();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetDeviceInfoPointer(
            [Out, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] pDevType,
            [Out, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] pLedCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetLedInfoPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [Out, MarshalAs(UnmanagedType.BStr)] out string pName,
            [Out, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] pLedStyles);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetLedColorPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [Out, MarshalAs(UnmanagedType.I4)] out int r,
            [Out, MarshalAs(UnmanagedType.I4)] out int g,
            [Out, MarshalAs(UnmanagedType.I4)] out int b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetLedStylePointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [Out, MarshalAs(UnmanagedType.BStr)] out string style);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetLedMaxBrightPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [Out, MarshalAs(UnmanagedType.I4)] out int maxLevel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetLedBrightPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [Out, MarshalAs(UnmanagedType.I4)] out int currentLevel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetLedMaxSpeedPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [Out, MarshalAs(UnmanagedType.I4)] out int maxSpeed);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetLedSpeedPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [Out, MarshalAs(UnmanagedType.I4)] out int currentSpeed);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetLedColorPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [In, MarshalAs(UnmanagedType.I4)] int r,
            [In, MarshalAs(UnmanagedType.I4)] int g,
            [In, MarshalAs(UnmanagedType.I4)] int b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetLedStylePointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [In, MarshalAs(UnmanagedType.BStr)] string style);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetLedBrightPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [In, MarshalAs(UnmanagedType.I4)] int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetLedSpeedPointer(
            [In, MarshalAs(UnmanagedType.BStr)] string type,
            [In, MarshalAs(UnmanagedType.I4)] int index,
            [In, MarshalAs(UnmanagedType.I4)] int speed);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetErrorMessagePointer(
            [In, MarshalAs(UnmanagedType.I4)] int errorCode,
            [Out, MarshalAs(UnmanagedType.BStr)] out string pDesc);

        #endregion

        internal static int Initialize() => _initializePointer();
        internal static int GetDeviceInfo(out string[] pDevType, out int[] pLedCount)
        {
            // HACK - SDK GetDeviceInfo returns a string[] for ledCount, so we'll parse that to int.
            int result = _getDeviceInfoPointer(out pDevType, out string[] ledCount);
            pLedCount = new int[ledCount.Length];

            for (int i = 0; i < ledCount.Length; i++)
                pLedCount[i] = int.Parse(ledCount[i]);

            return result;
        }

        internal static int GetLedInfo(string type, int index, out string pName, out string[] pLedStyles) => _getLedInfoPointer(type, index, out pName, out pLedStyles);
        internal static int GetLedColor(string type, int index, out int r, out int g, out int b) => _getLedColorPointer(type, index, out r, out g, out b);
        internal static int GetLedStyle(string type, int index, out string style) => _getLedStylePointer(type, index, out style);
        internal static int GetLedMaxBright(string type, int index, out int maxLevel) => _getLedMaxBrightPointer(type, index, out maxLevel);
        internal static int GetLedBright(string type, int index, out int currentLevel) => _getLedBrightPointer(type, index, out currentLevel);
        internal static int GetLedMaxSpeed(string type, int index, out int maxSpeed) => _getLedMaxSpeedPointer(type, index, out maxSpeed);
        internal static int GetLedSpeed(string type, int index, out int currentSpeed) => _getLedSpeedPointer(type, index, out currentSpeed);
        internal static int SetLedColor(string type, int index, int r, int g, int b) => _setLedColorPointer(type, index, r, g, b);
        internal static int SetLedStyle(string type, int index, string style) => _setLedStylePointer(type, index, style);
        internal static int SetLedBright(string type, int index, int level) => _setLedBrightPointer(type, index, level);
        internal static int SetLedSpeed(string type, int index, int speed) => _setLedSpeedPointer(type, index, speed);

        internal static string GetErrorMessage(int errorCode)
        {
            _getErrorMessagePointer(errorCode, out string description);
            return description;
        }

        #endregion
    }

    public interface IMysticSdk
    {
        bool InitializeLights();
        void ShutdownSdk();
        void SetLights(Color color);
        void ResetMysticDevices(bool deviceKeyboard, bool deviceOther, Color basecol);
        void ApplyMapOtherLighting(int pos, Color col);
        void SetAllLights(Color col);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);

    }

    public class Mystic : IMysticSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private bool IsInitialized = false;

        private CancellationTokenSource _cancellationTokenSource;

        private bool _mysticDeviceKeyboard = true;
        private bool _mysticOtherDevices = true;

        private static Dictionary<KeyboardNames, Color> keyMappings = new Dictionary<KeyboardNames, Color>();
        
        public bool InitializeLights()
        {
            var result = true;
            try
            {
                MysticSdkWrapper.Reload();

                int errorCode;
                if ((errorCode = MysticSdkWrapper.Initialize()) != 0)
                    return false;

                result = true;
                IsInitialized = true;

                string[] devTypes = null;
                int[] devLeds = null;

                var devs = MysticSdkWrapper.GetDeviceInfo(out devTypes, out devLeds);

                var x = 0;
                foreach (var device in devTypes)
                {
                    Write.WriteConsole(ConsoleTypes.Mystic, @"DEBUG: Mystic Device: " + device + " (" + devLeds[x] + ")");
                    x++;
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public void ShutdownSdk()
        {
            try
            {
                MysticSdkWrapper.UnloadMsiSDK();
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Mystic, @"Mystic: " + ex.Message);
            }
            
        }

        public void ApplyMapKeyLighting(string key, Color color, bool clear, [Optional] bool bypasswhitelist)
        {
            /*
            MysticSdkWrapper.MysticLedSetTargetDevice(MysticSdkWrapper.MysticDevicetypePerkeyRgb);
            
            if (!_mysticDeviceKeyboard)
                return;

            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            KeyboardNames keyName;
            //StopEffects();

            if (Enum.TryParse(key, out keyName))
            {
                MysticSdkWrapper.MysticLedSetLightingForKeyWithScanCode((int) keyName,
                    (int) Math.Ceiling((double) (color.R * 100) / 255),
                    (int) Math.Ceiling((double) (color.G * 100) / 255),
                    (int) Math.Ceiling((double) (color.B * 100) / 255));
            }
            */
        }

        public void ApplyMapOtherLighting(int pos, Color color)
        {
            if (!IsInitialized) return;
            if (pos > 1) return;

            try
            {
                if (_mysticOtherDevices)
                {
                    string[] devTypes = null;
                    int[] devLeds = null;

                    var devs = MysticSdkWrapper.GetDeviceInfo(out devTypes, out devLeds);

                    var x = 0;
                    foreach (var device in devTypes)
                    {
                        if (x > devLeds.Length) continue;
                        
                        for (var i = 0; i < devLeds[x]; i++)
                        {
                            MysticSdkWrapper.SetLedColor(device, i, color.R, color.G, color.B);
                        }

                        x++;
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Mystic, @"Mystic: " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Mystic, @"Internal Error: " + ex.StackTrace);
            }
        }

        public void SetAllLights(Color color)
        {
            if (!IsInitialized) return;

            try
            {
                if (_mysticOtherDevices)
                {
                    string[] devTypes = null;
                    int[] devLeds = null;

                    var devs = MysticSdkWrapper.GetDeviceInfo(out devTypes, out devLeds);

                    var x = 0;
                    foreach (var device in devTypes)
                    {
                        if (x > devLeds.Length) continue;

                        MysticSdkWrapper.SetLedColor(device, devLeds[x], color.R, color.G, color.B);
                        x++;
                    }
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Mystic, @"Mystic: " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Mystic, @"Internal Error: " + ex.StackTrace);
            }
            

        }


        public void ResetMysticDevices(bool deviceKeyboard, bool deviceOther, Color basecol)
        {
            if (!IsInitialized) return;

            _mysticOtherDevices = deviceOther;

            
        }
        

        public void SetLights(Color color)
        {
            if (!IsInitialized) return;

            //

        }

        /*
        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                if (!_mysticDeviceKeyboard)
                    return;
                
                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        foreach (var key in DeviceEffects.GlobalKeys)
                            try
                            {
                                KeyboardNames keyName;
                                if (Enum.TryParse(key, out keyName))
                                    MysticSdkWrapper.MysticLedSaveLightingForKey(keyName);
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
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                            else
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                            else
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                            else
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                            else
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                            else
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                            else
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                            else
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                            else
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 9)
                    {
                        //Spin down

                        foreach (var key in DeviceEffects.GlobalKeys)
                            MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));

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
                    }

                    if (i < 9)
                        Thread.Sleep(speed);
                }
            });
        }

        public Task MultiRipple1(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!_mysticDeviceKeyboard) return;
                var presetsA = new Dictionary<int, Color>();
                var zones = 5;
                MysticSdkWrapper.MysticLedSetTargetDevice(MysticSdkWrapper.MysticDevicetypeRgb);

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        MysticSdkWrapper.MysticLedSaveCurrentLighting();

                        continue;
                    }

                    if (i == 1 || i == 3 || i == 5 || i == 7)
                    {
                        for (int x1 = 0; x1 < zones; x1++)
                        {
                            //ApplyMapMultiLighting(burstcol, x1.ToString()); //preset
                            MysticSdkWrapper.MysticLedRestoreLighting();
                        }
                    }

                    if (i == 2 || i == 4 || i == 6 || i == 8)
                    {
                        for (int x1 = 0; x1 < zones; x1++)
                        {
                            ApplyMapMultiLighting(burstcol, x1.ToString());
                        }
                    }

                    if (i == 9)
                    {
                        MysticSdkWrapper.MysticLedRestoreLighting();
                    }

                    if (i < 9)
                    {
                        Thread.Sleep(speed);
                    }
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!_mysticDeviceKeyboard)
                    return;

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        //Setup
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                            if (pos > -1)
                                ApplyMapKeyLighting(key, burstcol, false);
                        }
                    }

                    if (i < 9)
                        Thread.Sleep(speed);
                }
            });
        }

        public Task MultiRipple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!_mysticDeviceKeyboard) return;
                var presetsA = new Dictionary<int, Color>();
                var zones = 5;
                MysticSdkWrapper.MysticLedSetTargetDevice(MysticSdkWrapper.MysticDevicetypeRgb);

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        MysticSdkWrapper.MysticLedSaveCurrentLighting();

                        continue;
                    }

                    if (i == 1 || i == 3 || i == 5 || i == 7)
                    {
                        for (int x1 = 0; x1 < zones; x1++)
                        {
                            //ApplyMapMultiLighting(burstcol, x1.ToString()); //preset
                            MysticSdkWrapper.MysticLedRestoreLighting();
                        }
                    }

                    if (i == 2 || i == 4 || i == 6 || i == 8)
                    {
                        for (int x1 = 0; x1 < zones; x1++)
                        {
                            ApplyMapMultiLighting(burstcol, x1.ToString());
                        }
                    }

                    if (i == 9)
                    {
                        //MysticSdkWrapper.MysticLedRestoreLighting();
                    }

                    if (i < 9)
                    {
                        Thread.Sleep(speed);
                    }
                }
            });
        }

        public void Flash1(Color burstcol, int speed, string[] regions)
        {
            if (!_mysticDeviceKeyboard)
                return;

            for (var i = 0; i <= 8; i++)
            {
                if (i == 0)
                    foreach (var key in regions)
                        MysticSdkWrapper.MysticLedSaveLightingForKey(ToKeyboardNames(key));
                else if (i == 1)
                    foreach (var key in regions)
                        ApplyMapKeyLighting(key, burstcol, false);
                else if (i == 2)
                    foreach (var key in regions)
                        MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                else if (i == 3)
                    foreach (var key in regions)
                        ApplyMapKeyLighting(key, burstcol, false);
                else if (i == 4)
                    foreach (var key in regions)
                        MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                else if (i == 5)
                    foreach (var key in regions)
                        ApplyMapKeyLighting(key, burstcol, false);
                else if (i == 6)
                    foreach (var key in regions)
                        MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));
                else if (i == 7)
                    foreach (var key in regions)
                        ApplyMapKeyLighting(key, burstcol, false);
                else if (i == 8)
                    foreach (var key in regions)
                        MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));

                if (i < 8)
                    Thread.Sleep(speed);
            }
        }

        public void SingleFlash1(Color burstcol, int speed, string[] regions)
        {
            if (!_mysticDeviceKeyboard)
                return;

            for (var i = 0; i <= 8; i++)
            {
                if (i == 0)
                    MysticSdkWrapper.MysticLedSaveCurrentLighting();

                else if (i == 1)
                    ApplyMapSingleLighting(burstcol);

                else if (i == 2)
                    MysticSdkWrapper.MysticLedRestoreLighting();

                else if (i == 3)
                    ApplyMapSingleLighting(burstcol);

                else if (i == 4)
                    MysticSdkWrapper.MysticLedRestoreLighting();

                else if (i == 5)
                    ApplyMapSingleLighting(burstcol);

                else if (i == 6)
                    MysticSdkWrapper.MysticLedRestoreLighting();

                else if (i == 7)
                    ApplyMapSingleLighting(burstcol);

                else if (i == 8)
                    MysticSdkWrapper.MysticLedRestoreLighting();
                
                if (i < 8)
                    Thread.Sleep(speed);
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (!_mysticDeviceKeyboard)
                return;

            if (!_mysticFlash2Running)
            {
                foreach (var key in regions)
                    MysticSdkWrapper.MysticLedSaveLightingForKey(ToKeyboardNames(key));

                _mysticFlash2Running = true;
                _mysticFlash2Step = 0;
            }

            if (_mysticFlash2Running)
                while (_mysticFlash2Running)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    if (_mysticFlash2Step == 0)
                    {
                        if (_mysticDeviceKeyboard)
                            foreach (var key in regions)
                                ApplyMapKeyLighting(key, burstcol, false);

                        _mysticFlash2Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_mysticFlash2Step == 1)
                    {
                        if (_mysticDeviceKeyboard)
                            foreach (var key in regions)
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));

                        _mysticFlash2Step = 0;

                        Thread.Sleep(speed);
                    }
                }
        }

        public void SingleFlash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (!_mysticDeviceKeyboard)
                return;

            if (!_mysticFlash2Running)
            {
                MysticSdkWrapper.MysticLedSaveCurrentLighting();

                _mysticFlash2Running = true;
                _mysticFlash2Step = 0;
            }

            if (_mysticFlash2Running)
                while (_mysticFlash2Running)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    if (_mysticFlash2Step == 0)
                    {
                        if (_mysticDeviceKeyboard)
                            ApplyMapSingleLighting(burstcol);

                        _mysticFlash2Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_mysticFlash2Step == 1)
                    {
                        if (_mysticDeviceKeyboard)
                            MysticSdkWrapper.MysticLedRestoreLighting();


                        _mysticFlash2Step = 0;

                        Thread.Sleep(speed);
                    }
                }
        }

        public void Flash3(Color burstcol, int speed, CancellationToken cts)
        {
            if (!_mysticDeviceKeyboard)
                return;

            try
            {
                lock (_Flash3)
                {
                    var presets = new Dictionary<string, Color>();
                    _mysticFlash3Running = true;
                    _mysticFlash3Step = 0;

                    if (_mysticFlash3Running)
                        while (_mysticFlash3Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_mysticFlash3Step == 0)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.NumFlash, key);
                                    if (pos > -1)
                                        ApplyMapKeyLighting(key, burstcol, false);
                                }
                                _mysticFlash3Step = 1;
                            }
                            else if (_mysticFlash3Step == 1)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.NumFlash, key);
                                    if (pos > -1)
                                        ApplyMapKeyLighting(key, Color.Black, false);
                                }

                                _mysticFlash3Step = 0;
                            }

                            //CueSDK.KeyboardSDK.Update();
                            Thread.Sleep(speed);
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
            if (!_mysticDeviceKeyboard)
                return;

            if (!_mysticFlash4Running)
            {
                foreach (var key in regions)
                    MysticSdkWrapper.MysticLedSaveLightingForKey(ToKeyboardNames(key));

                _mysticFlash4Running = true;
                _mysticFlash4Step = 0;
            }

            if (_mysticFlash4Running)
                while (_mysticFlash4Running)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    if (_mysticFlash4Step == 0)
                    {
                        if (_mysticDeviceKeyboard)
                            foreach (var key in regions)
                                ApplyMapKeyLighting(key, burstcol, false);

                        _mysticFlash4Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_mysticFlash4Step == 1)
                    {
                        if (_mysticDeviceKeyboard)
                            foreach (var key in regions)
                                MysticSdkWrapper.MysticLedRestoreLightingForKey(ToKeyboardNames(key));

                        _mysticFlash4Step = 0;

                        Thread.Sleep(speed);
                    }
                }
        }

        public void SingleFlash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (!_mysticDeviceKeyboard)
                return;

            if (!_mysticFlash4Running)
            {
                MysticSdkWrapper.MysticLedSaveCurrentLighting();

                _mysticFlash4Running = true;
                _mysticFlash4Step = 0;
            }

            if (_mysticFlash4Running)
                while (_mysticFlash4Running)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    if (_mysticFlash4Step == 0)
                    {
                        if (_mysticDeviceKeyboard)
                            ApplyMapSingleLighting(burstcol);
                        
                        _mysticFlash4Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_mysticFlash4Step == 1)
                    {
                        if (_mysticDeviceKeyboard)
                            MysticSdkWrapper.MysticLedRestoreLighting();
                        
                        _mysticFlash4Step = 0;

                        Thread.Sleep(speed);
                    }
                }
        }

        public void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50)
        {
            if (!_mysticDeviceKeyboard) return;
            if (cts.IsCancellationRequested) return;

            Dictionary<string, ColorFader> colorFaderDict = new Dictionary<string, ColorFader>();

            //Keyboard.SetCustomAsync(refreshKeyGrid);
            //
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

                    MysticSdkWrapper.MysticLedSaveLightingForKey(ToKeyboardNames(key));
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

                        Thread.Sleep(speed);
                    }
                });

                Thread.Sleep(colorFaderDict.Count * speed);
            }
        }

        private readonly object lockObject = new object();
        public void CycleEffect(int interval, CancellationTokenSource token)
        {
            if (!_mysticDeviceKeyboard) return;

            while (true)
            {
                lock (lockObject)
                {
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        MysticSdkWrapper.MysticLedSetLighting((int) Math.Ceiling((double) (250 * 100) / 255),
                            (int) Math.Ceiling((double) (x * 100) / 255), 0);
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        MysticSdkWrapper.MysticLedSetLighting((int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255), 0);
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        MysticSdkWrapper.MysticLedSetLighting((int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255), 0);
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        MysticSdkWrapper.MysticLedSetLighting(0, (int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255));
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        MysticSdkWrapper.MysticLedSetLighting((int) Math.Ceiling((double) (x * 100) / 255), 0,
                            (int) Math.Ceiling((double) (250 * 100) / 255));
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        MysticSdkWrapper.MysticLedSetLighting((int) Math.Ceiling((double) (250 * 100) / 255), 0,
                            (int) Math.Ceiling((double) (x * 100) / 255));
                    }

                    if (token.IsCancellationRequested) break;
                }
            }
            Thread.Sleep(interval);
        }

        */
        
    }

}