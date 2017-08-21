using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.Controllers;
using Chromatics.DeviceInterfaces;

/* Hubs all commands sent from FFXIVInterface and re-routes them to their correct Device interface.
 * 
 */

namespace Chromatics
{
    partial class Chromatics
    {
        private ICoolermasterSdk _coolermaster;
        private CancellationTokenSource _coolermasterFl1Cts = new CancellationTokenSource();
        private Task _coolermasterFl2;
        private CancellationTokenSource _coolermasterFl2Cts = new CancellationTokenSource();
        private Task _coolermasterFl3;
        private CancellationTokenSource _coolermasterFl3Cts = new CancellationTokenSource();
        private Task _coolermasterFl4;
        private CancellationTokenSource _coolermasterFl4Cts = new CancellationTokenSource();
        private ICorsairSdk _corsair;
        private CancellationTokenSource _corsairF1Cts = new CancellationTokenSource();
        private Task _corsairFl2;
        private CancellationTokenSource _corsairFl2Cts = new CancellationTokenSource();
        private Task _corsairFl3;
        private CancellationTokenSource _corsairFl3Cts = new CancellationTokenSource();
        private Task _corsairFl4;
        private CancellationTokenSource _corsairFl4Cts = new CancellationTokenSource();
        private IHueSdk _hue;
        private CancellationTokenSource _hue4Cts = new CancellationTokenSource();

        private Task _hueFl4;

        //private IRoccatSdk _roccat;
        private ILifxSdk _lifx;

        private CancellationTokenSource _lifx4Cts = new CancellationTokenSource();
        private Task _lifxFl4;
        private Task _logiFl2;
        private CancellationTokenSource _logiFl2Cts = new CancellationTokenSource();
        private Task _logiFl3;
        private CancellationTokenSource _logiFl3Cts = new CancellationTokenSource();
        private Task _logiFl4;
        private CancellationTokenSource _logiFl4Cts = new CancellationTokenSource();
        private ILogitechSdk _logitech;
        private CancellationTokenSource _logitechFl1Cts = new CancellationTokenSource();
        private IRazerSdk _razer;

        //Send a timed flash effect to a Keyboard
        private CancellationTokenSource _rzFl1Cts = new CancellationTokenSource();

        private Task _rzFl2;
        private CancellationTokenSource _rzFl2Cts = new CancellationTokenSource();
        private Task _rzFl3;
        private CancellationTokenSource _rzFl3Cts = new CancellationTokenSource();
        private Task _rzFl4;
        private CancellationTokenSource _rzFl4Cts = new CancellationTokenSource();
        private Task _coolermasterFlash;
        private Task _corsairFlash;

        //Send a continuous flash effect to a Keyboard
        private bool _globalFlash2Running;

        //Send a continuous flash effect to Numpad
        private bool _globalFlash3Running;

        //Flash 4
        private bool _globalFlash4Running;

        private Task _logFlash;
        private Task _rzFlash;

        public void InitializeSdk()
        {
            WriteConsole(ConsoleTypes.Razer, "Attempting to load Razer SDK..");
            _razer = RazerInterface.InitializeRazerSdk();
            if (_razer != null)
            {
                RazerSdk = true;
                RazerSdkCalled = 1;
                WriteConsole(ConsoleTypes.Razer, "Razer SDK Loaded");
                _razer.InitializeLights();
            }
            else
            {
                WriteConsole(ConsoleTypes.Razer, "Razer SDK failed to load.");
            }

            WriteConsole(ConsoleTypes.Logitech, "Attempting to load Logitech SDK..");
            _logitech = LogitechInterface.InitializeLogitechSdk();
            if (_logitech != null)
            {
                LogitechSdk = true;
                LogitechSdkCalled = 1;
                WriteConsole(ConsoleTypes.Logitech, "Logitech SDK Loaded");
            }
            else
            {
                WriteConsole(ConsoleTypes.Logitech, "Logitech SDK failed to load.");
            }

            //WriteConsole(ConsoleTypes.CORSAIR, "Attempting to load Corsair SDK..");
            _corsair = CorsairInterface.InitializeCorsairSdk();
            if (_corsair != null)
            {
                CorsairSdk = true;
                CorsairSdkCalled = 1;
                //WriteConsole(ConsoleTypes.CORSAIR, "Corsair SDK Loaded");
            }
            else
            {
                WriteConsole(ConsoleTypes.Corsair, "CUE SDK failed to load.");
            }

            //WriteConsole(ConsoleTypes.CORSAIR, "Attempting to load Corsair SDK..");
            _coolermaster = CoolermasterInterface.InitializeCoolermasterSdk();
            if (_coolermaster != null)
            {
                CoolermasterSdk = true;
                CoolermasterSdkCalled = 1;
                WriteConsole(ConsoleTypes.Coolermaster, "Coolermaster SDK Loaded");
            }
            else
            {
                WriteConsole(ConsoleTypes.Coolermaster, "Coolermaster SDK failed to load.");
            }

            //Load LIFX SDK
            _lifx = LifxInterface.InitializeLifxsdk();
            if (_lifx != null)
            {
                LifxSdk = true;
                LifxSdkCalled = 1;
                //WriteConsole(ConsoleTypes.LIFX, "LIFX SDK Loaded");
            }
            else
            {
                WriteConsole(ConsoleTypes.Lifx, "LIFX SDK failed to load.");
            }


            //Load HUE SDK - ENABLE THIS TO TEST

            /*
            _hue = DeviceInterfaces.HueInterface.InitializeHueSDK(HUEDefault);
            if (_hue != null)
            {
                HueSDK = true;
                HueSDKCalled = 1;
                WriteConsole(ConsoleTypes.HUE, "HUE SDK Loaded");
            }
            else
            {
                WriteConsole(ConsoleTypes.HUE, "HUE SDK failed to load.");
            }
            */


            //InitializeLIFXSDK();
            ResetDeviceDataGrid();
        }

        public void ShutDownDevices()
        {
            if (CoolermasterSdkCalled == 1)
                _coolermaster.Shutdown();
        }

        public void GlobalResetDevices()
        {
            var baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor);

            if (RazerSdkCalled == 1)
                _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse, _razerDeviceMousepad,
                    _razerDeviceHeadset, baseColor);

            if (LogitechSdkCalled == 1)
                _logitech.ResetLogitechDevices(_logitechDeviceKeyboard, baseColor);

            if (CorsairSdkCalled == 1)
                _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                    _corsairDeviceMousepad, _corsairDeviceHeadset, baseColor);

            if (CoolermasterSdkCalled == 1)
                _coolermaster.ResetCoolermasterDevices(_coolermasterDeviceKeyboard, _coolermasterDeviceMouse, baseColor);

            ResetDeviceDataGrid();
        }

        /* Sends a standard lighting update command (Do not throw to task)
         * Types:
         * Reset - Sends reset commands to clear device states
         * Static - Sends lighting command to all LED's on a device
         * Transition - Same as static except using a wipe transition
         * Wave - RGB rainbow scroll effect
         * Breath - Breathing effect on and off
         * Pulse - Constant wipe transitions between two colours
         */
        public void GlobalUpdateState(string type, Color col, bool disablekeys, [Optional] Color col2,
            [Optional] bool direction, [Optional] int speed)
        {
            if (RazerSdkCalled == 1)
                _razer.UpdateState(type, col, disablekeys, col2, direction, speed);

            if (LogitechSdkCalled == 1)
                _logitech.UpdateState(type, col, disablekeys, col2, direction, speed);

            if (CorsairSdkCalled == 1)
                _corsair.UpdateState(type, col, disablekeys, col2, direction, speed);

            if (CoolermasterSdkCalled == 1)
                _coolermaster.UpdateState(type, col, disablekeys, col2, direction, speed);
        }

        /* Sends a standard lighting update command to LIFX or HUE devices
         * Modes:
         * 0 - Disabled
         * 1 - Standby
         * 2 - Base Colour
         * 3 - Highlight Colour
         * 4 - Enmity Tracker
         * 5 - Target HP
         * 6 - Status Effect
         * 7 - HP
         * 8 - MP
         * 9 - TP
         * 10 - Castbar
         * 100 - All/System
        */
        public void GlobalUpdateBulbState(BulbModeTypes mode, Color col, int transition)
        {
            if (LifxSdkCalled == 1)
                if (mode != BulbModeTypes.Disabled)
                    if (mode == BulbModeTypes.Standby)
                        _lifx.LifxUpdateState(mode, Color.Black, transition);
                    else
                        _lifx.LifxUpdateState(mode, col, transition);

            if (HueSdkCalled == 1)
                if (mode != BulbModeTypes.Disabled)
                    if (mode == BulbModeTypes.Standby)
                        _hue.HueUpdateState(mode, Color.Black, transition);
                    else
                        _hue.HueUpdateState(mode, col, transition);
        }

        public void GlobalUpdateBulbStateBrightness(BulbModeTypes mode, Color col, ushort brightness, int transition)
        {
            if (LifxSdkCalled == 1)
                if (mode != BulbModeTypes.Disabled)
                    if (mode == BulbModeTypes.Standby)
                        _lifx.LifxUpdateStateBrightness(mode, Color.Black, brightness, transition);
                    else
                        _lifx.LifxUpdateStateBrightness(mode, col, brightness, transition);

            if (HueSdkCalled == 1)
                if (mode != BulbModeTypes.Disabled)
                    if (mode == BulbModeTypes.Standby)
                        _hue.HueUpdateStateBrightness(mode, Color.Black, brightness, transition);
                    else
                        _hue.HueUpdateStateBrightness(mode, col, brightness, transition);
        }

        //_lifx.LIFXUpdateStateBrightness(9, col_tpfull, (ushort) pol_TPZ, 250);

        public void GlobalKeyboardUpdate()
        {
            if (!HoldReader)
                if (RazerSdkCalled == 1)
                    _razer.KeyboardUpdate();
        }

        public void GlobalApplyAllKeyLighting(Color col)
        {
            if (RazerSdkCalled == 1)
                _razer.SetLights(col);

            if (LogitechSdkCalled == 1)
                _logitech.SetLights(col);

            if (CorsairSdkCalled == 1)
                _corsair.SetLights(col);

            if (CoolermasterSdkCalled == 1)
                _coolermaster.SetLights(col);
        }

        //Send a lighting command to a specific Keyboard LED
        public void GlobalApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (_KeysSingleKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
                _razer.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);

            if (LogitechSdkCalled == 1)
            {
                if (key == "Macro1")
                    _logitech.ApplyMapKeyLighting("Macro1", col, clear, bypasswhitelist);
                else if (key == "Macro2")
                    _logitech.ApplyMapKeyLighting("Macro4", col, clear, bypasswhitelist);
                else if (key == "Macro3")
                    _logitech.ApplyMapKeyLighting("Macro7", col, clear, bypasswhitelist);
                else if (key == "Macro4")
                    _logitech.ApplyMapKeyLighting("Macro10", col, clear, bypasswhitelist);
                else if (key == "Macro5")
                    _logitech.ApplyMapKeyLighting("Macro13", col, clear, bypasswhitelist);
                else
                    _logitech.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);

                _logitech.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);
            }

            if (CorsairSdkCalled == 1)
                if (key == "Macro1")
                {
                    _corsair.ApplyMapKeyLighting("Macro1", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro2", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro3", col, clear, bypasswhitelist);
                }
                else if (key == "Macro2")
                {
                    _corsair.ApplyMapKeyLighting("Macro4", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro5", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro6", col, clear, bypasswhitelist);
                }
                else if (key == "Macro3")
                {
                    _corsair.ApplyMapKeyLighting("Macro7", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro8", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro9", col, clear, bypasswhitelist);
                }
                else if (key == "Macro4")
                {
                    _corsair.ApplyMapKeyLighting("Macro10", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro11", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro12", col, clear, bypasswhitelist);
                }
                else if (key == "Macro5")
                {
                    _corsair.ApplyMapKeyLighting("Macro13", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro14", col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting("Macro15", col, clear, bypasswhitelist);
                }
                else
                {
                    _corsair.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);
                }

            if (CoolermasterSdkCalled == 1)
                _coolermaster.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);
        }

        public void GlobalApplyKeySingleLighting(DevModeTypes mode, Color col)
        {
            if (!_KeysSingleKeyModeEnabled || mode == DevModeTypes.Disabled || mode != _KeysSingleKeyMode) return;
            
            GlobalApplyAllKeyLighting(col);
            //Debug.WriteLine("Set Static");
        }

        public void GlobalApplyKeySingleLightingBrightness(DevModeTypes mode, Color col, double val)
        {
            if (!_KeysSingleKeyModeEnabled || mode == DevModeTypes.Disabled || mode != _KeysSingleKeyMode) return;
            
            var c1 = new Helpers.ColorRGB
            {
                R = col.R,
                G = col.G,
                B = col.B
            };

            Helpers.RGB2HSL(c1, out double h, out double s, out double l);

            l = (l - (1 - val));

            var c2 = Helpers.HSL2RGB(h, s, l);
            GlobalApplyAllKeyLighting(c2);
        }

        //Send a lighting command to a specific Keyboard LED outside of MapKey scope
        public void GlobalApplyMapLogoLighting(string key, Color col, bool clear)
        {
            if (RazerSdkCalled == 1)
                _razer.ApplyMapLogoLighting(key, col, clear);

            if (LogitechSdkCalled == 1)
            {
                //
            }

            if (CorsairSdkCalled == 1)
                _corsair.ApplyMapLogoLighting(key, col, clear);
        }

        //Send a lighting command to a specific Mouse LED
        public void GlobalApplyMapMouseLighting(string region, Color col, bool clear)
        {
            if (RazerSdkCalled == 1)
                _razer.ApplyMapMouseLighting(region, col, clear);

            if (LogitechSdkCalled == 1)
            {
                //
            }

            if (CorsairSdkCalled == 1)
                if (region == "All")
                {
                    _corsair.ApplyMapMouseLighting("MouseFront", col, clear);
                    _corsair.ApplyMapMouseLighting("MouseScroll", col, clear);
                    _corsair.ApplyMapMouseLighting("MouseSide", col, clear);
                    _corsair.ApplyMapMouseLighting("MouseLogo", col, clear);
                }
                else if (region == "Logo")
                {
                    _corsair.ApplyMapMouseLighting("MouseLogo", col, clear);
                    _corsair.ApplyMapMouseLighting("MouseFront", col, clear);
                }
                else if (region == "ScrollWheel")
                {
                    _corsair.ApplyMapMouseLighting("MouseScroll", col, clear);
                }
                else if (region == "Backlight")
                {
                    _corsair.ApplyMapMouseLighting("MouseSide", col, clear);
                }
                else
                {
                    _corsair.ApplyMapMouseLighting(region, col, clear);
                }
        }

        //Send a lighting command to a specific Mousepad or HUE/LIFX LED
        public void GlobalApplyMapPadLighting(int region, Color col, bool clear)
        {
            if (RazerSdkCalled == 1)
                _razer.ApplyMapPadLighting(region, col, clear);

            if (LogitechSdkCalled == 1)
            {
                //
            }

            if (CorsairSdkCalled == 1)
            {
                var corsairPadRegion = "Pad1";

                if (region == 0) corsairPadRegion = "Pad15";
                else if (region == 1) corsairPadRegion = "Pad14";
                else if (region == 2) corsairPadRegion = "Pad13";
                else if (region == 3) corsairPadRegion = "Pad12";
                else if (region == 4) corsairPadRegion = "Pad11";
                else if (region == 5) corsairPadRegion = "Pad10";
                else if (region == 6) corsairPadRegion = "Pad9";
                else if (region == 7) corsairPadRegion = "Pad8";
                else if (region == 8) corsairPadRegion = "Pad7";
                else if (region == 9) corsairPadRegion = "Pad6";
                else if (region == 10) corsairPadRegion = "Pad5";
                else if (region == 11) corsairPadRegion = "Pad4";
                else if (region == 12) corsairPadRegion = "Pad3";
                else if (region == 13) corsairPadRegion = "Pad2";
                else if (region == 14) corsairPadRegion = "Pad1";

                _corsair.ApplyMapPadLighting(corsairPadRegion, col, clear);
            }
        }

        //Send a ripple effect to a Keyboard that reverts back to the base colour once completed
        public void GlobalRipple1(Color burstcol, int speed, Color baseColor)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
            {
                var rippleTask = _razer.Ripple1(burstcol, speed);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (LogitechSdkCalled == 1)
            {
                var rippleTask = _logitech.Ripple1(burstcol, speed, baseColor);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (CorsairSdkCalled == 1)
            {
                var rippleTask = _corsair.Ripple1(burstcol, speed, baseColor);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (CoolermasterSdkCalled == 1)
            {
                var rippleTask = _coolermaster.Ripple1(burstcol, speed);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }
        }

        //Send a ripple effect to a Keyboard that keeps its new colour
        public void GlobalRipple2(Color burstcol, int speed)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
            {
                var rippleTask2 = _razer.Ripple2(burstcol, speed);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }

            if (LogitechSdkCalled == 1)
            {
                var logitechRipple2 = _logitech.Ripple2(burstcol, speed);
                MemoryTasks.Add(logitechRipple2);
                MemoryTasks.Run(logitechRipple2);
            }

            if (CorsairSdkCalled == 1)
            {
                var rippleTask2 = _corsair.Ripple2(burstcol, speed);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }

            if (CoolermasterSdkCalled == 1)
            {
                var rippleTask2 = _coolermaster.Ripple2(burstcol, speed);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }
        }

        public void GlobalFlash1(Color burstcol, int speed, string[] regions)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
            {
                _rzFlash = null;
                _rzFl1Cts = new CancellationTokenSource();

                _rzFlash = new Task(() =>
                {
                    HoldReader = true;
                    _razer.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _rzFl1Cts.Token);
                MemoryTasks.Add(_rzFlash);
                MemoryTasks.Run(_rzFlash);
            }

            if (LogitechSdkCalled == 1)
            {
                _logFlash = null;
                _logitechFl1Cts = new CancellationTokenSource();

                _logFlash = new Task(() =>
                {
                    HoldReader = true;
                    _logitech.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _logitechFl1Cts.Token);
                MemoryTasks.Add(_logFlash);
                MemoryTasks.Run(_logFlash);
            }

            if (CorsairSdkCalled == 1)
            {
                _corsairFlash = null;
                _corsairF1Cts = new CancellationTokenSource();

                _corsairFlash = new Task(() =>
                {
                    HoldReader = true;
                    _corsair.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _corsairF1Cts.Token);
                MemoryTasks.Add(_corsairFlash);
                MemoryTasks.Run(_corsairFlash);
            }

            if (CoolermasterSdkCalled == 1)
            {
                _coolermasterFlash = null;
                _coolermasterFl1Cts = new CancellationTokenSource();

                _coolermasterFlash = new Task(() =>
                {
                    HoldReader = true;
                    _coolermaster.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _coolermasterFl1Cts.Token);
                MemoryTasks.Add(_coolermasterFlash);
                MemoryTasks.Run(_coolermasterFlash);
            }
        }

        public void GlobalFlash2(Color burstcol, int speed, string[] template)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled)
                return;

            if (!_globalFlash2Running)
            {
                if (RazerSdkCalled == 1)
                {
                    _rzFl2 = null;
                    _rzFl2Cts = new CancellationTokenSource();
                    _rzFl2 = new Task(() => { _razer.Flash2(burstcol, speed, _rzFl2Cts.Token, template); },
                        _rzFl2Cts.Token);
                    MemoryTasks.Add(_rzFl2);
                    MemoryTasks.Run(_rzFl2);
                }

                if (LogitechSdkCalled == 1)
                {
                    _logiFl2 = null;
                    _logiFl2Cts = new CancellationTokenSource();
                    _logiFl2 = new Task(() => { _logitech.Flash2(burstcol, speed, _logiFl2Cts.Token, template); },
                        _logiFl2Cts.Token);
                    MemoryTasks.Add(_logiFl2);
                    MemoryTasks.Run(_logiFl2);
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairFl2 = null;
                    _corsairFl2Cts = new CancellationTokenSource();
                    _corsairFl2 = new Task(() => { _corsair.Flash2(burstcol, speed, _corsairFl2Cts.Token, template); },
                        _corsairFl2Cts.Token);
                    MemoryTasks.Add(_corsairFl2);
                    MemoryTasks.Run(_corsairFl2);
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterFl2 = null;
                    _coolermasterFl2Cts = new CancellationTokenSource();
                    _coolermasterFl2 =
                        new Task(() => { _coolermaster.Flash2(burstcol, speed, _coolermasterFl2Cts.Token, template); },
                            _coolermasterFl2Cts.Token);
                    MemoryTasks.Add(_coolermasterFl2);
                    MemoryTasks.Run(_coolermasterFl2);
                }

                _globalFlash2Running = true;
            }
        }

        public void ToggleGlobalFlash2(bool toggle)
        {
            if (_KeysSingleKeyModeEnabled)
                return;

            if (!toggle && _globalFlash2Running)
            {
                _globalFlash2Running = false;

                if (RazerSdkCalled == 1)
                {
                    _rzFl2Cts.Cancel();
                    MemoryTasks.Remove(_rzFl2);
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairFl2Cts.Cancel();
                    MemoryTasks.Remove(_corsairFl2);
                }

                if (LogitechSdkCalled == 1)
                {
                    _logiFl2Cts.Cancel();
                    MemoryTasks.Remove(_logiFl2);
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterFl2Cts.Cancel();
                    MemoryTasks.Remove(_coolermasterFl2);
                }

                //Debug.WriteLine("Stopping Flash 2");

                MemoryTasks.Cleanup();
            }

            MemoryTasks.Cleanup();
        }

        public void GlobalFlash3(Color burstcol, int speed)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled)
                return;

            if (!_globalFlash3Running)
            {
                if (RazerSdkCalled == 1)
                {
                    _rzFl3 = null;
                    _rzFl3Cts = new CancellationTokenSource();
                    _rzFl3 = new Task(() => { _razer.Flash3(burstcol, speed, _rzFl3Cts.Token); }, _rzFl3Cts.Token);
                    MemoryTasks.Add(_rzFl3);
                    MemoryTasks.Run(_rzFl3);
                }

                if (LogitechSdkCalled == 1)
                {
                    _logiFl3 = null;
                    _logiFl3Cts = new CancellationTokenSource();
                    _logiFl3 = new Task(() => { _logitech.Flash3(burstcol, speed, _logiFl3Cts.Token); },
                        _logiFl3Cts.Token);
                    MemoryTasks.Add(_logiFl3);
                    MemoryTasks.Run(_logiFl3);
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairFl3 = null;
                    _corsairFl3Cts = new CancellationTokenSource();
                    _corsairFl3 = new Task(() => { _corsair.Flash3(burstcol, speed, _corsairFl3Cts.Token); },
                        _corsairFl3Cts.Token);
                    MemoryTasks.Add(_corsairFl3);
                    MemoryTasks.Run(_corsairFl3);
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterFl3 = null;
                    _coolermasterFl3Cts = new CancellationTokenSource();
                    _coolermasterFl3 =
                        new Task(() => { _coolermaster.Flash3(burstcol, speed, _coolermasterFl3Cts.Token); },
                            _coolermasterFl3Cts.Token);
                    MemoryTasks.Add(_coolermasterFl3);
                    MemoryTasks.Run(_coolermasterFl3);
                }

                _globalFlash3Running = true;
            }
        }

        public void ToggleGlobalFlash3(bool toggle)
        {
            if (_KeysSingleKeyModeEnabled)
                return;

            if (!toggle && _globalFlash3Running)
            {
                _globalFlash3Running = false;
                //_RazerFlash2Running = false;
                //_CorsairFlash2Running = false;

                if (RazerSdkCalled == 1)
                {
                    _rzFl3Cts.Cancel();
                    MemoryTasks.Remove(_rzFl3);
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsairFl3Cts.Cancel();
                    MemoryTasks.Remove(_corsairFl3);
                }

                if (LogitechSdkCalled == 1)
                {
                    _coolermasterFl3Cts.Cancel();
                    MemoryTasks.Remove(_logiFl3);
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _logiFl3Cts.Cancel();
                    MemoryTasks.Remove(_coolermasterFl3);
                }

                //Debug.WriteLine("Stopping Flash 3");

                MemoryTasks.Cleanup();
            }

            MemoryTasks.Cleanup();
        }

        public void GlobalFlash4(Color basecol, Color burstcol, int speed, string[] template)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled)
                return;

            if (!_globalFlash4Running)
            {
                if (ChromaticsSettings.ChromaticsSettingsDfBellToggle)
                {
                    if (RazerSdkCalled == 1)
                    {
                        _rzFl4 = null;
                        _rzFl4Cts = new CancellationTokenSource();
                        _rzFl4 = new Task(() => { _razer.Flash4(burstcol, speed, _rzFl4Cts.Token, template); },
                            _rzFl4Cts.Token);
                        MemoryTasks.Add(_rzFl4);
                        MemoryTasks.Run(_rzFl4);
                    }

                    if (LogitechSdkCalled == 1)
                    {
                        _logiFl4 = null;
                        _logiFl4Cts = new CancellationTokenSource();
                        _logiFl4 = new Task(() => { _logitech.Flash4(burstcol, speed, _logiFl4Cts.Token, template); },
                            _logiFl4Cts.Token);
                        MemoryTasks.Add(_logiFl4);
                        MemoryTasks.Run(_logiFl4);
                    }

                    if (CorsairSdkCalled == 1)
                    {
                        _corsairFl4 = null;
                        _corsairFl4Cts = new CancellationTokenSource();
                        _corsairFl4 =
                            new Task(() => { _corsair.Flash4(burstcol, speed, _corsairFl4Cts.Token, template); },
                                _corsairFl4Cts.Token);
                        MemoryTasks.Add(_corsairFl4);
                        MemoryTasks.Run(_corsairFl4);
                    }

                    if (CoolermasterSdkCalled == 1)
                    {
                        _coolermasterFl4 = null;
                        _coolermasterFl4Cts = new CancellationTokenSource();
                        _coolermasterFl4 =
                            new Task(
                                () => { _coolermaster.Flash4(burstcol, speed, _coolermasterFl4Cts.Token, template); },
                                _coolermasterFl4Cts.Token);
                        MemoryTasks.Add(_coolermasterFl4);
                        MemoryTasks.Run(_coolermasterFl4);
                    }
                }

                if (LifxSdkCalled == 1)
                {
                    _lifxFl4 = null;
                    _lifx4Cts = new CancellationTokenSource();
                    _lifxFl4 = new Task(() => { _lifx.Flash4(basecol, burstcol, speed * 2, _lifx4Cts.Token); },
                        _lifx4Cts.Token);
                    MemoryTasks.Add(_lifxFl4);
                    MemoryTasks.Run(_lifxFl4);
                }

                if (HueSdkCalled == 1)
                {
                    _hueFl4 = null;
                    _hue4Cts = new CancellationTokenSource();
                    _hueFl4 = new Task(() => { _hue.Flash4(basecol, burstcol, speed * 2, _hue4Cts.Token); },
                        _hue4Cts.Token);
                    MemoryTasks.Add(_hueFl4);
                    MemoryTasks.Run(_hueFl4);
                }

                _globalFlash4Running = true;
            }
        }

        public void ToggleGlobalFlash4(bool toggle)
        {
            if (_KeysSingleKeyModeEnabled)
                return;

            if (!toggle && _globalFlash4Running)
            {
                _globalFlash4Running = false;

                if (ChromaticsSettings.ChromaticsSettingsDfBellToggle)
                {
                    if (RazerSdkCalled == 1)
                    {
                        _rzFl4Cts.Cancel();
                        MemoryTasks.Remove(_rzFl4);
                    }

                    if (CorsairSdkCalled == 1)
                    {
                        _corsairFl4Cts.Cancel();
                        MemoryTasks.Remove(_corsairFl4);
                    }

                    if (LogitechSdkCalled == 1)
                    {
                        _logiFl4Cts.Cancel();
                        MemoryTasks.Remove(_logiFl4);
                    }

                    if (CoolermasterSdkCalled == 1)
                    {
                        _coolermasterFl4Cts.Cancel();
                        MemoryTasks.Remove(_coolermasterFl4);
                    }
                }

                if (LifxSdkCalled == 1)
                {
                    _lifx4Cts.Cancel();
                    MemoryTasks.Remove(_lifxFl4);
                }

                if (HueSdkCalled == 1)
                {
                    _hue4Cts.Cancel();
                    MemoryTasks.Remove(_hueFl4);
                }

                Debug.WriteLine("Stopping Flash 4");

                MemoryTasks.Cleanup();
            }

            MemoryTasks.Cleanup();
        }
    }
}