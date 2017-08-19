using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using Roccat_Talk.RyosTalkFX;
using Roccat_Talk.RyosTalkFX.KeyboardLayouts;

namespace Chromatics.DeviceInterfaces
{
    internal class RoccatInterface
    {
        public static RoccatLib InitializeRoccatSDK()
        {
            RoccatLib roccat = null;

            if (Process.GetProcessesByName("Roccat Talk").Length > 0)
            {
                roccat = new RoccatLib();

                var roccatstat = roccat.InitializeSDK();

                if (!roccatstat)
                    return null;
            }

            return roccat;
        }
    }

    public class RoccatSdkWrapper
    {
        //
    }

    public interface IRoccatSdk
    {
        bool InitializeSDK();
        void Shutdown();
        void ResetRoccatDevices(bool DeviceKeyboard, Color basecol);

        void UpdateState(string type, Color col, bool disablekeys, [Optional] Color col2,
            [Optional] bool direction, [Optional] int speed);
    }

    public class RoccatLib : IRoccatSdk
    {
        private static readonly ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private static RyosTalkFXConnection client;

        private static readonly KeyboardState MasterState = new KeyboardState();

        private readonly CancellationTokenSource CCTS = new CancellationTokenSource();

        #region keytranslator

        private readonly Dictionary<string, Key> KeyIDs = new Dictionary<string, Key>
        {
            //Keys
            {"D1", KeyboardLayout_EN.ONE},
            {"D2", KeyboardLayout_EN.TWO},
            {"D3", KeyboardLayout_EN.THREE},
            {"D4", KeyboardLayout_EN.FOUR},
            {"D5", KeyboardLayout_EN.FIVE},
            {"D6", KeyboardLayout_EN.SIX},
            {"D7", KeyboardLayout_EN.SEVEN},
            {"D8", KeyboardLayout_EN.EIGHT},
            {"D9", KeyboardLayout_EN.NINE},
            {"D0", KeyboardLayout_EN.ZERO},
            {"A", KeyboardLayout_EN.A},
            {"B", KeyboardLayout_EN.B},
            {"C", KeyboardLayout_EN.C},
            {"D", KeyboardLayout_EN.D},
            {"E", KeyboardLayout_EN.E},
            {"F", KeyboardLayout_EN.F},
            {"G", KeyboardLayout_EN.G},
            {"H", KeyboardLayout_EN.H},
            {"I", KeyboardLayout_EN.I},
            {"J", KeyboardLayout_EN.J},
            {"K", KeyboardLayout_EN.K},
            {"L", KeyboardLayout_EN.L},
            {"M", KeyboardLayout_EN.M},
            {"N", KeyboardLayout_EN.N},
            {"O", KeyboardLayout_EN.O},
            {"P", KeyboardLayout_EN.P},
            {"Q", KeyboardLayout_EN.Q},
            {"R", KeyboardLayout_EN.R},
            {"S", KeyboardLayout_EN.S},
            {"T", KeyboardLayout_EN.T},
            {"U", KeyboardLayout_EN.U},
            {"V", KeyboardLayout_EN.V},
            {"W", KeyboardLayout_EN.W},
            {"X", KeyboardLayout_EN.X},
            {"Y", KeyboardLayout_EN.Y},
            {"Z", KeyboardLayout_EN.Z},
            {"NumLock", KeyboardLayout_EN.NUM_LOCK},
            {"Num0", KeyboardLayout_EN.KP_ZERO},
            {"Num1", KeyboardLayout_EN.KP_ONE},
            {"Num2", KeyboardLayout_EN.KP_TWO},
            {"Num3", KeyboardLayout_EN.KP_THREE},
            {"Num4", KeyboardLayout_EN.KP_FOUR},
            {"Num5", KeyboardLayout_EN.KP_FIVE},
            {"Num6", KeyboardLayout_EN.KP_SIX},
            {"Num7", KeyboardLayout_EN.KP_SEVEN},
            {"Num8", KeyboardLayout_EN.KP_EIGHT},
            {"Num9", KeyboardLayout_EN.KP_NINE},
            {"NumDivide", KeyboardLayout_EN.KP_SLASH},
            {"NumMultiply", KeyboardLayout_EN.KP_ASTERISK},
            {"NumSubtract", KeyboardLayout_EN.KP_HYPHEN},
            {"NumAdd", KeyboardLayout_EN.KP_PLUS},
            {"NumEnter", KeyboardLayout_EN.KP_ENTER},
            {"NumDecimal", KeyboardLayout_EN.KP_PERIOD},
            {"PrintScreen", KeyboardLayout_EN.PRINT_SCREEN},
            {"Scroll", KeyboardLayout_EN.SCROLL_LOCK},
            {"Pause", KeyboardLayout_EN.PAUSE},
            {"Insert", KeyboardLayout_EN.INSERT},
            {"Home", KeyboardLayout_EN.HOME},
            {"PageUp", KeyboardLayout_EN.PAGE_UP},
            {"PageDown", KeyboardLayout_EN.PAGE_DOWN},
            {"Delete", KeyboardLayout_EN.DELETE},
            {"End", KeyboardLayout_EN.END},
            {"Up", KeyboardLayout_EN.UP},
            {"Left", KeyboardLayout_EN.LEFT},
            {"Right", KeyboardLayout_EN.RIGHT},
            {"Down", KeyboardLayout_EN.DOWN},
            {"Tab", KeyboardLayout_EN.TAB},
            {"CapsLock", KeyboardLayout_EN.CAPS_LOCK},
            {"Backspace", KeyboardLayout_EN.BACKSPACE},
            {"Enter", KeyboardLayout_EN.ENTER},
            {"LeftControl", KeyboardLayout_EN.LEFT_CTRL},
            {"LeftWindows", KeyboardLayout_EN.WIN},
            {"LeftAlt", KeyboardLayout_EN.LEFT_ALT},
            {"Space", KeyboardLayout_EN.SPACE},
            {"RightControl", KeyboardLayout_EN.RIGHT_CTRL},
            {"Function", KeyboardLayout_EN.F},
            {"RightAlt", KeyboardLayout_EN.RIGHT_ALT},
            {"RightMenu", KeyboardLayout_EN.MENU},
            {"LeftShift", KeyboardLayout_EN.LEFT_SHIFT},
            {"RightShift", KeyboardLayout_EN.RIGHT_SHIFT},
            {"Macro1", KeyboardLayout_EN.M1},
            {"Macro2", KeyboardLayout_EN.M2},
            {"Macro3", KeyboardLayout_EN.M3},
            {"Macro4", KeyboardLayout_EN.M4},
            {"Macro5", KeyboardLayout_EN.M5},
            {"OemTilde", KeyboardLayout_EN.GRAVE},
            {"OemMinus", KeyboardLayout_EN.HYPHEN},
            {"OemEquals", KeyboardLayout_EN.EQUALS},
            {"OemLeftBracket", KeyboardLayout_EN.LEFT_BRACKET},
            {"OemRightBracket", KeyboardLayout_EN.RIGHT_BRACKET},
            {"OemSlash", KeyboardLayout_EN.FORWARD_SLASH},
            {"OemSemicolon", KeyboardLayout_EN.SEMI_COLON},
            {"OemApostrophe", KeyboardLayout_EN.APOSTROPHE},
            {"OemComma", KeyboardLayout_EN.COMMA},
            {"OemPeriod", KeyboardLayout_EN.PERIOD},
            {"OemBackslash", KeyboardLayout_EN.BACKSLASH},
            {"Escape", KeyboardLayout_EN.ESC}
        };

        #endregion

        private bool _initialized;
        private bool RoccatDeviceKeyboard = true;

        public bool InitializeSDK()
        {
            try
            {
                client = new RyosTalkFXConnection();
                client.Initialize();
                client.EnterSdkMode();
                _initialized = true;

                //client.SetLedOn(KeyboardLayout_EN);

                return true;
            }
            catch (Exception ex)
            {
                write.WriteConsole(ConsoleTypes.ROCCAT, "Roccat SDK failed to load. Error: " + ex.Message);
                return false;
            }
        }

        public void Shutdown()
        {
            if (_initialized)
            {
                client.ExitSdkMode();
                _initialized = false;
            }
        }

        public void ResetRoccatDevices(bool DeviceKeyboard, Color basecol)
        {
            RoccatDeviceKeyboard = DeviceKeyboard;

            if (_initialized)
                if (RoccatDeviceKeyboard)
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

            if (type == "reset")
            {
                try
                {
                    if (RoccatDeviceKeyboard && disablekeys != true)
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
                    if (RoccatDeviceKeyboard && disablekeys != true)
                        UpdateRoccatStateAll(col);
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
                    if (RoccatDeviceKeyboard && disablekeys != true)
                    {
                        //
                    }
                });
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
            }
            else if (type == "wave")
            {
                var _CrSt = new Task(() =>
                {
                    if (RoccatDeviceKeyboard && disablekeys != true)
                    {
                        //
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
                        if (RoccatDeviceKeyboard && disablekeys != true)
                        {
                            //
                        }
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
                    if (RoccatDeviceKeyboard && disablekeys != true)
                    {
                        //
                    }
                }, CCTS.Token);
                MemoryTasks.Add(_CrSt);
                MemoryTasks.Run(_CrSt);
                //RzPulse = true;
            }

            MemoryTasks.Cleanup();
        }

        private void UpdateRoccatState()
        {
            if (!_initialized)
                return;


            if (RoccatDeviceKeyboard)
            {
                MasterState.AllLedsOn();
                client.SetWholeKeyboardState(MasterState);
            }
        }

        private void UpdateRoccatStateAll(Color col)
        {
            if (!_initialized)
                return;

            if (RoccatDeviceKeyboard)
            {
                foreach (var key in KeyIDs)
                {
                    //
                }


                MasterState.AllLedsOn();
                client.SetWholeKeyboardState(MasterState);
            }
        }
    }
}