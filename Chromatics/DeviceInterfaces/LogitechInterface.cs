using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using Chromatics.DeviceInterfaces.EffectLibrary;
using System.Collections.Generic;
using Chromatics.FFXIVInterfaces;

/* Contains all Logitech SDK code for detection, initialization, states and effects.
 * 
 * 
 */

namespace Chromatics.DeviceInterfaces
{
    public class LogitechInterface
    {
        private static ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static Logitech InitializeLogitechSDK()
        {
            Logitech logitech = null;
            if (Process.GetProcessesByName("LCore").Length > 0)
            {
                logitech = new Logitech();
                var result = logitech.InitializeLights();
                if (!result)
                {
                    return null;
                }
            }
            return logitech;
        }


    }

    public class LogitechSdkWrapper
    {
        //LED SDK
        private const int LOGI_DEVICETYPE_MONOCHROME_ORD = 0;
        private const int LOGI_DEVICETYPE_RGB_ORD = 1;
        private const int LOGI_DEVICETYPE_PERKEY_RGB_ORD = 2;

        public const int LOGI_DEVICETYPE_MONOCHROME = (1 << LOGI_DEVICETYPE_MONOCHROME_ORD);
        public const int LOGI_DEVICETYPE_RGB = (1 << LOGI_DEVICETYPE_RGB_ORD);
        public const int LOGI_DEVICETYPE_PERKEY_RGB = (1 << LOGI_DEVICETYPE_PERKEY_RGB_ORD);
        public const int LOGI_LED_BITMAP_WIDTH = 21;
        public const int LOGI_LED_BITMAP_HEIGHT = 6;
        public const int LOGI_LED_BITMAP_BYTES_PER_KEY = 4;

        public const int LOGI_LED_BITMAP_SIZE =
            LOGI_LED_BITMAP_WIDTH * LOGI_LED_BITMAP_HEIGHT * LOGI_LED_BITMAP_BYTES_PER_KEY;

        public const int LOGI_LED_DURATION_INFINITE = 0;

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedInit();

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetTargetDevice(int targetDevice);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedGetSdkVersion(ref int majorNum, ref int minorNum, ref int buildNum);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSaveCurrentLighting();

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLighting(int redPercentage, int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedRestoreLighting();

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedFlashLighting(int redPercentage, int greenPercentage, int bluePercentage,
            int milliSecondsDuration, int milliSecondsInterval);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedPulseLighting(int redPercentage, int greenPercentage, int bluePercentage,
            int milliSecondsDuration, int milliSecondsInterval);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedStopEffects();

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingFromBitmap(byte[] bitmap);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingForKeyWithScanCode(int keyCode, int redPercentage,
            int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingForKeyWithHidCode(int keyCode, int redPercentage,
            int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingForKeyWithQuartzCode(int keyCode, int redPercentage,
            int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSetLightingForKeyWithKeyName(KeyboardNames keyCode, int redPercentage,
            int greenPercentage, int bluePercentage);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedSaveLightingForKey(KeyboardNames keyName);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedRestoreLightingForKey(KeyboardNames keyName);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedFlashSingleKey(KeyboardNames keyName, int redPercentage, int greenPercentage,
            int bluePercentage, int msDuration, int msInterval);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedPulseSingleKey(KeyboardNames keyName, int startRedPercentage,
            int startGreenPercentage, int startBluePercentage, int finishRedPercentage, int finishGreenPercentage,
            int finishBluePercentage, int msDuration, bool isInfinite);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLedStopEffectsOnKey(KeyboardNames keyName);

        [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
        public static extern void LogiLedShutdown();

        public static void LogiColourCycle(Color col, object lockObject, CancellationToken token)
        {
            lock (lockObject)
            {
                while (true)
                {
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        LogitechSdkWrapper.LogiLedSetLighting((int)Math.Ceiling((double)(250 * 100) / 255),
                            (int)Math.Ceiling((double)(x * 100) / 255), 0);
                    }
                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        LogitechSdkWrapper.LogiLedSetLighting((int)Math.Ceiling((double)(x * 100) / 255),
                            (int)Math.Ceiling((double)(250 * 100) / 255), 0);
                    }
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        LogitechSdkWrapper.LogiLedSetLighting((int)Math.Ceiling((double)(x * 100) / 255),
                            (int)Math.Ceiling((double)(250 * 100) / 255), 0);
                    }
                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        LogitechSdkWrapper.LogiLedSetLighting(0, (int)Math.Ceiling((double)(x * 100) / 255),
                            (int)Math.Ceiling((double)(250 * 100) / 255));
                    }
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        LogitechSdkWrapper.LogiLedSetLighting((int)Math.Ceiling((double)(x * 100) / 255), 0,
                            (int)Math.Ceiling((double)(250 * 100) / 255));
                    }
                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        LogitechSdkWrapper.LogiLedSetLighting((int)Math.Ceiling((double)(250 * 100) / 255), 0,
                            (int)Math.Ceiling((double)(x * 100) / 255));
                    }
                    if (token.IsCancellationRequested) break;
                }
                Thread.Sleep(100);
                LogitechSdkWrapper.LogiLedSetLighting((int)Math.Ceiling((double)(col.R * 100) / 255),
                    (int)Math.Ceiling((double)(col.G * 100) / 255), (int)Math.Ceiling((double)(col.B * 100) / 255));
            }
        }

    }

    public interface ILogitechSdk
    {
        bool InitializeLights();
        void SetLights(Color color);
        void StopEffects();
        void ColorCycle(Color color, CancellationToken token);
        void Pulse(Color color, int milliSecondsDuration, int milliSecondsInterval);
        void ResetLogitechDevices(bool LogitechDeviceKeyboard, System.Drawing.Color basecol);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void UpdateState(string type, Color col, bool disablekeys,
            [Optional] Color col2, [Optional] bool direction, [Optional] int speed);

        Task Ripple1(Color burstcol, int speed, Color baseColor);

        Task Ripple2(Color burstcol, int speed);

        void Flash1(Color burstcol, int speed);

        void Flash3(Color burstcol, int speed, CancellationToken cts);
    }

    public class Logitech : ILogitechSdk
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

        private readonly string[] _logitechflash3 = { "NumLock", "NumDivide", "NumMultiply", "Num7", "Num8", "Num9", "Num4", "Num5", "Num6", "Num1", "Num2", "Num3" };
        */
        #endregion

        private bool LogitechDeviceKeyboard = true;

        public void ApplyMapKeyLighting(string key, System.Drawing.Color color, bool clear, [Optional] bool bypasswhitelist)
        {
            if (!LogitechDeviceKeyboard)
                return;

            if (FFXIVHotbar.keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            KeyboardNames keyName;

            if (Enum.TryParse<KeyboardNames>(key, out keyName))
            {
                //write.WriteConsole(ConsoleTypes.LOGITECH, "Parsed to " + keyName);
                LogitechSdkWrapper.LogiLedSetLightingForKeyWithScanCode((int)keyName,
                (int)Math.Ceiling((double)(color.R * 100) / 255),
                (int)Math.Ceiling((double)(color.G * 100) / 255),
                (int)Math.Ceiling((double)(color.B * 100) / 255));
            }


        }
        public void ResetLogitechDevices(bool DeviceKeyboard, Color basecol)
        {
            LogitechDeviceKeyboard = DeviceKeyboard;

            if (LogitechDeviceKeyboard)
            {
                SetLights(basecol);
            }
            else
            {
                StopEffects();
            }
            
        }
        public void ColorCycle(Color color, CancellationToken token)
        {
            if (LogitechDeviceKeyboard)
                LogitechSdkWrapper.LogiColourCycle(color, new object(), token);
        }

        public bool InitializeLights()
        {
            var result = true;
            try
            {
                LogitechSdkWrapper.LogiLedInit();
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public void Pulse(Color color, int milliSecondsDuration, int milliSecondsInterval)
        {
            if (LogitechDeviceKeyboard)
                LogitechSdkWrapper.LogiLedPulseLighting((int)Math.Ceiling((double)(color.R * 100) / 255),
                                   (int)Math.Ceiling((double)(color.G * 100) / 255),
                                   (int)Math.Ceiling((double)(color.B * 100) / 255),
                                   LogitechSdkWrapper.LOGI_LED_DURATION_INFINITE,
                                   60);

        }

        public void SetLights(Color color)
        {
            if (LogitechDeviceKeyboard)
                LogitechSdkWrapper.LogiLedSetLighting((int)Math.Ceiling((double)(color.R * 100) / 255),
                                   (int)Math.Ceiling((double)(color.G * 100) / 255),
                                   (int)Math.Ceiling((double)(color.B * 100) / 255));
        }

        public void StopEffects()
        {
            LogitechSdkWrapper.LogiLedStopEffects();
        }

        private CancellationTokenSource _cancellationTokenSource;

        public void UpdateState(string type, System.Drawing.Color col, bool disablekeys,
            [Optional] System.Drawing.Color col2, [Optional] bool direction, [Optional] int speed)
        {
            if (!LogitechDeviceKeyboard)
                return;


            MemoryTasks.Cleanup();
            _cancellationTokenSource?.Cancel();

            if (type == "reset")
            {

                this.StopEffects();
                Thread.Sleep(100);

                this.SetLights(Color.FromArgb(0, 0, 0));
            }
            else if (type == "static")
            {

                Task _RzSt = new Task(() =>
                {
                    this.StopEffects();
                    Thread.Sleep(100);

                    this.SetLights(col);

                });
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
            }
            else if (type == "transition")
            {
                Task _RzSt = new Task(() =>
                {
                    this.StopEffects();
                    Thread.Sleep(100);

                    this.SetLights(col);
                });
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
            }
            else if (type == "wave")
            {
                _cancellationTokenSource = new CancellationTokenSource();
               
                Task _RzSt = new Task(() =>
                {

                    StopEffects();
                    Thread.Sleep(100);                

                    ColorCycle(col, _cancellationTokenSource.Token);
                }, _cancellationTokenSource.Token);
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
            }
            else if (type == "breath")
            {
                _cancellationTokenSource = new CancellationTokenSource();
                Task _RzSt = new Task(() =>
                {

                    //TODO: Does this need to be canceled since it is infinite?
                    //Roxas: I'm not using the breath effect currently so as I don't know what purpose it will serve, having a cancellation token is probably a good idea.
                    this.Pulse(col, LogitechSdkWrapper.LOGI_LED_DURATION_INFINITE, 60);
                }, _cancellationTokenSource.Token);
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
            }
            else if (type == "pulse")
            {
                Task _RzSt = new Task(() =>
                {
                    this.StopEffects();
                    Thread.Sleep(100);

                    this.SetLights(col);
                });
                MemoryTasks.Add(_RzSt);
                MemoryTasks.Run(_RzSt);
            }

            MemoryTasks.Cleanup();
        }

        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                if (!LogitechDeviceKeyboard)
                    return;

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        foreach (var key in DeviceEffects._GlobalKeys)
                            try
                            {
                                KeyboardNames keyName;
                                if (Enum.TryParse<KeyboardNames>(key, out keyName))
                                {
                                    LogitechSdkWrapper.LogiLedSaveLightingForKey(keyName);
                                }
                            }
                            catch (Exception ex)
                            {
                                write.WriteConsole(ConsoleTypes.ERROR, "(" + key + "): " + ex.Message);
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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                            else
                            {
                                LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));

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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                            else
                            {
                                LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                            else
                            {
                                LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                            else
                            {
                                LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                            else
                            {
                                LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                            else
                            {
                                LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                            else
                            {
                                LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                            else
                            {
                                LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
                            }
                        }
                    }
                    else if (i == 9)
                    {
                        //Spin down

                        foreach (var key in DeviceEffects._GlobalKeys)
                            LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));

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

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!LogitechDeviceKeyboard)
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
                        foreach (var key in DeviceEffects._GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects._PulseOutStep0, key);
                            if (pos > -1)
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
                                ApplyMapKeyLighting(key, burstcol, false);
                            }
                        }
                    }

                    if (i < 9)
                        Thread.Sleep(speed);
                }
            });
        }

        public void Flash1(Color burstcol, int speed)
        {
            if (!LogitechDeviceKeyboard)
                return;

            for (var i = 0; i <= 8; i++)
            {
                if (i == 0)
                {
                    //Setup
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        LogitechSdkWrapper.LogiLedSaveLightingForKey(ToKeyboardNames(key));
                    }
                }
                else if (i == 1)
                {
                    //Step 0
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        ApplyMapKeyLighting(key, burstcol, false);
                    }
                }
                else if (i == 2)
                {
                    //Step 1
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
                    }
                }
                else if (i == 3)
                {
                    //Step 2
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        ApplyMapKeyLighting(key, burstcol, false);
                    }
                }
                else if (i == 4)
                {
                    //Step 3
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
                    }
                }
                else if (i == 5)
                {
                    //Step 4
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        ApplyMapKeyLighting(key, burstcol, false);
                    }
                }
                else if (i == 6)
                {
                    //Step 5
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));
                    }
                }
                else if (i == 7)
                {
                    //Step 6
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        ApplyMapKeyLighting(key, burstcol, false);
                    }
                }
                else if (i == 8)
                {
                    //Step 7
                    foreach (var key in DeviceEffects._GlobalKeys3)
                    {
                        LogitechSdkWrapper.LogiLedRestoreLightingForKey(ToKeyboardNames(key));

                    }
                }

                if (i < 8)
                    Thread.Sleep(speed);
            }
        }

        private int _LogiFlash3Step = 0;
        private bool _LogiFlash3Running = false;
        static readonly object _Flash3 = new object();
        public void Flash3(Color burstcol, int speed, CancellationToken cts)
        {
            if (!LogitechDeviceKeyboard)
                return;

            try
            {
                lock (_Flash3)
                {
                    var presets = new Dictionary<string, Color>();
                    _LogiFlash3Running = true;

                    if (_LogiFlash3Running == true)
                    {
                        while (_LogiFlash3Running == true)
                        {
                            cts.ThrowIfCancellationRequested();

                            if (cts.IsCancellationRequested)
                            {
                                break;
                            }

                            if (_LogiFlash3Step == 0)
                            {
                                foreach (var key in DeviceEffects._NumFlash)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._NumFlash, key);
                                    if (pos > -1)
                                        ApplyMapKeyLighting(key, burstcol, false);
                                }
                                _LogiFlash3Step = 1;
                            }
                            else if (_LogiFlash3Step == 1)
                            {
                                foreach (var key in DeviceEffects._NumFlash)
                                {
                                    var pos = Array.IndexOf(DeviceEffects._NumFlash, key);
                                    if (pos > -1)
                                        ApplyMapKeyLighting(key, Color.Black, false);
                                }

                                _LogiFlash3Step = 0;


                            }

                            //CueSDK.KeyboardSDK.Update();
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

        private static KeyboardNames ToKeyboardNames(string key)
        {
            KeyboardNames keyName;
            if (Enum.TryParse<KeyboardNames>(key, out keyName))
            {
                return keyName;
            }
            return KeyboardNames.G_BADGE;
        }
    }

    public enum KeyboardNames
    {
        ESC = 0x01,
        F1 = 0x3b,
        F2 = 0x3c,
        F3 = 0x3d,
        F4 = 0x3e,
        F5 = 0x3f,
        F6 = 0x40,
        F7 = 0x41,
        F8 = 0x42,
        F9 = 0x43,
        F10 = 0x44,
        F11 = 0x57,
        F12 = 0x58,
        PrintScreen = 0x137,
        Scroll = 0x46,
        Pause = 0x145,
        OemTilde = 0x29,
        D1 = 0x02,
        D2 = 0x03,
        D3 = 0x04,
        D4 = 0x05,
        D5 = 0x06,
        D6 = 0x07,
        D7 = 0x08,
        D8 = 0x09,
        D9 = 0x0A,
        D0 = 0x0B,
        OemMinus = 0x0C,
        OemEquals = 0x0D,
        Backspace = 0x0E,
        Insert = 0x152,
        Home = 0x147,
        PageUp = 0x149,
        NumLock = 0x45,
        NumDivide = 0x135,
        NumMultiply = 0x37,
        NumSubtract = 0x4A,
        Tab = 0x0F,
        Q = 0x10,
        W = 0x11,
        E = 0x12,
        R = 0x13,
        T = 0x14,
        Y = 0x15,
        U = 0x16,
        I = 0x17,
        O = 0x18,
        P = 0x19,
        OemLeftBracket = 0x1A,
        OemRightBracket = 0x1B,
        OemBackslash = 0x2B,
        Delete = 0x153,
        End = 0x14F,
        PageDown = 0x151,
        Num7 = 0x47,
        Num8 = 0x48,
        Num9 = 0x49,
        NumAdd = 0x4E,
        CapsLock = 0x3A,
        A = 0x1E,
        S = 0x1F,
        D = 0x20,
        F = 0x21,
        G = 0x22,
        H = 0x23,
        J = 0x24,
        K = 0x25,
        L = 0x26,
        OemSemicolon = 0x27,
        OemApostrophe = 0x28,
        Enter = 0x1C,
        Num4 = 0x4B,
        Num5 = 0x4C,
        Num6 = 0x4D,
        LeftShift = 0x2A,
        Z = 0x2C,
        X = 0x2D,
        C = 0x2E,
        V = 0x2F,
        B = 0x30,
        N = 0x31,
        M = 0x32,
        OemComma = 0x33,
        OemPeriod = 0x34,
        OemSlash = 0x35,
        RightShift = 0x36,
        Up = 0x148,
        Num1 = 0x4F,
        Num2 = 0x50,
        Num3 = 0x51,
        NumEnter = 0x11C,
        LeftControl = 0x1D,
        LeftWindows = 0x15B,
        LeftAlt = 0x38,
        Space = 0x39,
        RightAlt = 0x138,
        RIGHT_WINDOWS = 0x15C,
        RightMenu = 0x15D,
        RightControl = 0x11D,
        Left = 0x14B,
        Down = 0x150,
        Right = 0x14D,
        Num0 = 0x52,
        NumDecimal = 0x53,
        Macro1 = 0xFFF1,
        Macro4 = 0xFFF2,
        Macro7 = 0xFFF3,
        Macro10 = 0xFFF4,
        Macro13 = 0xFFF5,
        G_6 = 0xFFF6,
        G_7 = 0xFFF7,
        G_8 = 0xFFF8,
        G_9 = 0xFFF9,
        G_LOGO = 0xFFFF1,
        G_BADGE = 0xFFFF2
    }
}
