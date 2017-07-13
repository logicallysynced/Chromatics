using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.DeviceInterfaces;
using System.Diagnostics;

/* Hubs all commands sent from FFXIVInterface and re-routes them to their correct Device interface.
 * 
 */

namespace Chromatics
{
    partial class Chromatics
    {
        private ILogitechSdk _logitech;
        private IRazerSdk _razer;
        private ICorsairSdk _corsair;
        private ILIFXSdk _lifx;
        private IHueSdk _hue;

        public void InitializeSDK()
        {
            WriteConsole(ConsoleTypes.RAZER, "Attempting to load Razer SDK..");
            _razer = RazerInterface.InitializeRazerSDK();
            if (_razer != null)
            {
                RazerSDK = true;
                RazerSDKCalled = 1;
                WriteConsole(ConsoleTypes.RAZER, "Razer SDK Loaded");
                _razer.InitializeLights();
            }
            else
            {
                WriteConsole(ConsoleTypes.RAZER, "Razer SDK failed to load.");
            }
            
            WriteConsole(ConsoleTypes.LOGITECH, "Attempting to load Logitech SDK..");
            _logitech = LogitechInterface.InitializeLogitechSDK();
            if (_logitech != null)
            {
                LogitechSDK = true;
                LogitechSDKCalled = 1;
                WriteConsole(ConsoleTypes.LOGITECH, "Logitech SDK Loaded");
            }
            else
            {
                WriteConsole(ConsoleTypes.LOGITECH, "Logitech SDK failed to load.");
            }
            
            //WriteConsole(ConsoleTypes.CORSAIR, "Attempting to load Corsair SDK..");
            _corsair = CorsairInterface.InitializeCorsairSDK();
            if (_corsair != null)
            {
                CorsairSDK = true;
                CorsairSDKCalled = 1;
                //WriteConsole(ConsoleTypes.CORSAIR, "Corsair SDK Loaded");
            }
            else
            {
                WriteConsole(ConsoleTypes.CORSAIR, "CUE SDK failed to load.");
            }

            //Load LIFX SDK
            _lifx = DeviceInterfaces.LIFXInterface.InitializeLIFXSDK();
            if (_lifx != null)
            {
                LifxSDK = true;
                LifxSDKCalled = 1;
                //WriteConsole(ConsoleTypes.LIFX, "LIFX SDK Loaded");
            }
            else
            {
                WriteConsole(ConsoleTypes.LIFX, "LIFX SDK failed to load.");
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

        public void GlobalResetDevices()
        {
            var _BaseColor = System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor);

            if (RazerSDKCalled == 1)
            {
                _razer.ResetRazerDevices(RazerDeviceKeyboard, RazerDeviceKeypad, RazerDeviceMouse, RazerDeviceMousepad, RazerDeviceHeadset, _BaseColor);
            }

            if (LogitechSDKCalled == 1)
            {
                _logitech.ResetLogitechDevices(LogitechDeviceKeyboard, _BaseColor);
            }

            if (CorsairSDKCalled == 1)
            {
                _corsair.ResetCorsairDevices(CorsairDeviceKeyboard, CorsairDeviceKeypad, CorsairDeviceMouse, CorsairDeviceMousepad, CorsairDeviceHeadset, _BaseColor);
            }

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
        public void GlobalUpdateState(string type, System.Drawing.Color col, bool disablekeys, [Optional]System.Drawing.Color col2, [Optional]bool direction, [Optional]int speed)
        {
            if (RazerSDKCalled == 1)
            {
                _razer.UpdateState(type, col, disablekeys, col2, direction, speed);
            }

            if (LogitechSDKCalled == 1)
            {
                _logitech.UpdateState(type, col, disablekeys, col2, direction, speed);
            }

            if (CorsairSDKCalled == 1)
            {
                _corsair.UpdateState(type, col, disablekeys, col2, direction, speed);
            }
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
        public void GlobalUpdateBulbState(DeviceModeTypes mode, System.Drawing.Color col, int transition)
        {
            if (LifxSDKCalled == 1)
            {
                if (mode != DeviceModeTypes.DISABLED)
                {
                    if (mode == DeviceModeTypes.STANDBY)
                    {
                        _lifx.LIFXUpdateState(mode, System.Drawing.Color.Black, transition);
                    }
                    else
                    {
                        _lifx.LIFXUpdateState(mode, col, transition);
                    }
                }
            }

            if (HueSDKCalled == 1)
            {
                if (mode != DeviceModeTypes.DISABLED)
                {
                    if (mode == DeviceModeTypes.STANDBY)
                    {
                        _hue.HUEUpdateState(mode, System.Drawing.Color.Black, transition);
                    }
                    else
                    {
                        _hue.HUEUpdateState(mode, col, transition);
                    }
                }
            }
        }

        public void GlobalUpdateBulbStateBrightness(DeviceModeTypes mode, System.Drawing.Color col, ushort brightness, int transition)
        {
            if (LifxSDKCalled == 1)
            {
                if (mode != DeviceModeTypes.DISABLED)
                {
                    if (mode == DeviceModeTypes.STANDBY)
                    {
                        _lifx.LIFXUpdateStateBrightness(mode, System.Drawing.Color.Black, brightness, transition);
                    }
                    else
                    {
                        _lifx.LIFXUpdateStateBrightness(mode, col, brightness, transition);
                    }
                }
            }

            if (HueSDKCalled == 1)
            {
                if (mode != DeviceModeTypes.DISABLED)
                {
                    if (mode == DeviceModeTypes.STANDBY)
                    {
                        _hue.HUEUpdateStateBrightness(mode, System.Drawing.Color.Black, brightness, transition);
                    }
                    else
                    {
                        _hue.HUEUpdateStateBrightness(mode, col, brightness, transition);
                    }
                }
            }
        }

        //_lifx.LIFXUpdateStateBrightness(9, col_tpfull, (ushort) pol_TPZ, 250);

        public void GlobalKeyboardUpdate()
        {
            if (!HoldReader)
            {
                if (RazerSDKCalled == 1)
                {
                    _razer.KeyboardUpdate();
                }
            }
        }

        //Send a lighting command to a specific Keyboard LED
        public void GlobalApplyMapKeyLighting(string key, System.Drawing.Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (RazerSDKCalled == 1)
            {
                _razer.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);
            }

            if (LogitechSDKCalled == 1)
            {
                if (key == "Macro1")
                {
                    _logitech.ApplyMapKeyLighting("Macro1", col, clear, bypasswhitelist);
                }
                else if (key == "Macro2")
                {
                    _logitech.ApplyMapKeyLighting("Macro4", col, clear, bypasswhitelist);
                }
                else if (key == "Macro3")
                {
                    _logitech.ApplyMapKeyLighting("Macro7", col, clear, bypasswhitelist);
                }
                else if (key == "Macro4")
                {
                    _logitech.ApplyMapKeyLighting("Macro10", col, clear, bypasswhitelist);
                }
                else if (key == "Macro5")
                {
                    _logitech.ApplyMapKeyLighting("Macro13", col, clear, bypasswhitelist);
                }
                else
                {
                    _logitech.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);
                }

                _logitech.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);
            }

            if (CorsairSDKCalled == 1)
            {
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
            }
        }

        //Send a lighting command to a specific Keyboard LED outside of MapKey scope
        public void GlobalApplyMapLogoLighting(string key, System.Drawing.Color col, bool clear)
        {
            if (RazerSDKCalled == 1)
            {
                _razer.ApplyMapLogoLighting(key, col, clear);
            }

            if (LogitechSDKCalled == 1)
            {
                //
            }

            if (CorsairSDKCalled == 1)
            {
                _corsair.ApplyMapLogoLighting(key, col, clear);
            }
        }

        //Send a lighting command to a specific Mouse LED
        public void GlobalApplyMapMouseLighting(string region, System.Drawing.Color col, bool clear)
        {
            if (RazerSDKCalled == 1)
            {
                _razer.ApplyMapMouseLighting(region, col, clear);
            }

            if (LogitechSDKCalled == 1)
            {
                //
            }

            if (CorsairSDKCalled == 1)
            {
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
        }

        //Send a lighting command to a specific Mousepad or HUE/LIFX LED
        public void GlobalApplyMapPadLighting(int region, System.Drawing.Color col, bool clear)
        {
            if (RazerSDKCalled == 1)
            {
                _razer.ApplyMapPadLighting(region, col, clear);
            }

            if (LogitechSDKCalled == 1)
            {
                //
            }

            if (CorsairSDKCalled == 1)
            {
                string _CorsairPadRegion = "Pad1";

                if (region == 0) { _CorsairPadRegion = "Pad15"; }
                else if (region == 1) { _CorsairPadRegion = "Pad14"; }
                else if (region == 2) { _CorsairPadRegion = "Pad13"; }
                else if (region == 3) { _CorsairPadRegion = "Pad12"; }
                else if (region == 4) { _CorsairPadRegion = "Pad11"; }
                else if (region == 5) { _CorsairPadRegion = "Pad10"; }
                else if (region == 6) { _CorsairPadRegion = "Pad9"; }
                else if (region == 7) { _CorsairPadRegion = "Pad8"; }
                else if (region == 8) { _CorsairPadRegion = "Pad7"; }
                else if (region == 9) { _CorsairPadRegion = "Pad6"; }
                else if (region == 10) { _CorsairPadRegion = "Pad5"; }
                else if (region == 11) { _CorsairPadRegion = "Pad4"; }
                else if (region == 12) { _CorsairPadRegion = "Pad3"; }
                else if (region == 13) { _CorsairPadRegion = "Pad2"; }
                else if (region == 14) { _CorsairPadRegion = "Pad1"; }

                _corsair.ApplyMapPadLighting(_CorsairPadRegion, col, clear);
            }
        }
        
        //Send a ripple effect to a Keyboard that reverts back to the base colour once completed
        public void GlobalRipple1(System.Drawing.Color burstcol, int speed, System.Drawing.Color _BaseColor)
        {
            MemoryTasks.Cleanup();

            if (RazerSDKCalled == 1)
            {
                var rippleTask = _razer.Ripple1(burstcol, speed);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
                
            }

            if (LogitechSDKCalled == 1)
            {
                var rippleTask = _logitech.Ripple1(burstcol, speed, _BaseColor);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (CorsairSDKCalled == 1)
            {
                var rippleTask = _corsair.Ripple1(burstcol, speed, _BaseColor);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }
        }

        //Send a ripple effect to a Keyboard that keeps its new colour
        public void GlobalRipple2(System.Drawing.Color burstcol, int speed)
        {
            MemoryTasks.Cleanup();

            if (RazerSDKCalled == 1)
            {
                var rippleTask2 = _razer.Ripple2(burstcol, speed);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }

            if (LogitechSDKCalled == 1)
            {
                Task logitechRipple2 = _logitech.Ripple2(burstcol, speed);
                MemoryTasks.Add(logitechRipple2);
                MemoryTasks.Run(logitechRipple2);
            }
        
            if (CorsairSDKCalled == 1)
            {
                var rippleTask2 = _corsair.Ripple2(burstcol, speed);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }
        }

        //Send a timed flash effect to a Keyboard
        private CancellationTokenSource _RzFl1CTS = new CancellationTokenSource();
        private CancellationTokenSource _CorsairF12CTS = new CancellationTokenSource();
        private CancellationTokenSource _LogitechFl1CTS = new CancellationTokenSource();
        private Task RzFlash;
        private Task logFlash;
        private Task CorsairFlash;

        public void GlobalFlash1(System.Drawing.Color burstcol, int speed, string[] regions)
        {
            MemoryTasks.Cleanup();

            if (RazerSDKCalled == 1)
            {
                RzFlash = null;
                _RzFl1CTS = new CancellationTokenSource();

                RzFlash = new Task(() =>
                {
                    HoldReader = true;
                    _razer.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _RzFl1CTS.Token);
                MemoryTasks.Add(RzFlash);
                MemoryTasks.Run(RzFlash);
            }

            if (LogitechSDKCalled == 1)
            {
                logFlash = null;
                _LogitechFl1CTS = new CancellationTokenSource();

                logFlash = new Task(() =>
                {
                    HoldReader = true;
                    _logitech.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _LogitechFl1CTS.Token);
                MemoryTasks.Add(logFlash);
                MemoryTasks.Run(logFlash);
            }

            if (CorsairSDKCalled == 1)
            {
                CorsairFlash = null;
                _CorsairF12CTS = new CancellationTokenSource();

                CorsairFlash = new Task(() =>
                {
                    HoldReader = true;
                    _corsair.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _CorsairF12CTS.Token);
                MemoryTasks.Add(CorsairFlash);
                MemoryTasks.Run(CorsairFlash);
            }
        }

        //Send a continuous flash effect to a Keyboard
        bool GlobalFlash2Running = false;
        private CancellationTokenSource _RzFl2CTS = new CancellationTokenSource();
        private CancellationTokenSource _CorsairFl2CTS = new CancellationTokenSource();
        private CancellationTokenSource _LogiFl2CTS = new CancellationTokenSource();
        Task _RzFl2;
        Task _CorsairFl2;
        Task _LogiFl2;

        public void GlobalFlash2(System.Drawing.Color burstcol, int speed, string[] template)
        {
            MemoryTasks.Cleanup();

            if (!GlobalFlash2Running)
            {
                if (RazerSDKCalled == 1)
                {
                    _RzFl2 = null;
                    _RzFl2CTS = new CancellationTokenSource();
                    _RzFl2 = new Task(() => { _razer.Flash2(burstcol, speed, _RzFl2CTS.Token, template); }, _RzFl2CTS.Token);
                    MemoryTasks.Add(_RzFl2);
                    MemoryTasks.Run(_RzFl2);
                }

                if (LogitechSDKCalled == 1)
                {
                    _LogiFl2 = null;
                    _LogiFl2CTS = new CancellationTokenSource();
                    _LogiFl2 = new Task(() => { _logitech.Flash2(burstcol, speed, _LogiFl2CTS.Token, template); }, _LogiFl2CTS.Token);
                    MemoryTasks.Add(_LogiFl2);
                    MemoryTasks.Run(_LogiFl2);
                }

                if (CorsairSDKCalled == 1)
                {
                    _CorsairFl2 = null;
                    _CorsairFl2CTS = new CancellationTokenSource();
                    _CorsairFl2 = new Task(() => { _corsair.Flash2(burstcol, speed, _CorsairFl2CTS.Token, template); }, _CorsairFl2CTS.Token);
                    MemoryTasks.Add(_CorsairFl2);
                    MemoryTasks.Run(_CorsairFl2);
                }

                GlobalFlash2Running = true;
            }
        }

        public void ToggleGlobalFlash2(bool toggle)
        {
            if (!toggle && GlobalFlash2Running)
            {
                GlobalFlash2Running = false;

                if (RazerSDKCalled == 1)
                {
                    _RzFl2CTS.Cancel();
                    MemoryTasks.Remove(_RzFl2);
                }

                if (CorsairSDKCalled == 1)
                {
                    _CorsairFl2CTS.Cancel();
                    MemoryTasks.Remove(_CorsairFl2);
                }

                if (LogitechSDKCalled == 1)
                {
                    _LogiFl2CTS.Cancel();
                    MemoryTasks.Remove(_LogiFl2);
                }

                Debug.WriteLine("Stopping Flash 2");

                MemoryTasks.Cleanup();
            }

            MemoryTasks.Cleanup();
        }

        //Send a continuous flash effect to Numpad
        bool GlobalFlash3Running = false;
        private CancellationTokenSource _RzFl3CTS = new CancellationTokenSource();
        private CancellationTokenSource _CorsairFl3CTS = new CancellationTokenSource();
        private CancellationTokenSource _LogiFl3CTS = new CancellationTokenSource();
        private Task _RzFl3;
        private Task _CorsairFl3;
        private Task _LogiFl3;

        public void GlobalFlash3(System.Drawing.Color burstcol, int speed)
        {
            MemoryTasks.Cleanup();

            if (!GlobalFlash3Running)
            {
                if (RazerSDKCalled == 1)
                {
                    _RzFl3 = null;
                    _RzFl3CTS = new CancellationTokenSource();
                    _RzFl3 = new Task(() => { _razer.Flash3(burstcol, speed, _RzFl3CTS.Token); }, _RzFl3CTS.Token);
                    MemoryTasks.Add(_RzFl3);
                    MemoryTasks.Run(_RzFl3);
                }

                if (LogitechSDKCalled == 1)
                {
                    _LogiFl3 = null;
                    _LogiFl3CTS = new CancellationTokenSource();
                    _LogiFl3 = new Task(() => { _logitech.Flash3(burstcol, speed, _LogiFl3CTS.Token); }, _LogiFl3CTS.Token);
                    MemoryTasks.Add(_LogiFl3);
                    MemoryTasks.Run(_LogiFl3);
                }

                if (CorsairSDKCalled == 1)
                {
                    _CorsairFl3 = null;
                    _CorsairFl3CTS = new CancellationTokenSource();
                    _CorsairFl3 = new Task(() => { _corsair.Flash3(burstcol, speed, _CorsairFl3CTS.Token); }, _CorsairFl3CTS.Token);
                    MemoryTasks.Add(_CorsairFl3);
                    MemoryTasks.Run(_CorsairFl3);
                }

                GlobalFlash3Running = true;
            }
        }

        public void ToggleGlobalFlash3(bool toggle)
        {
            if (!toggle && GlobalFlash3Running)
            {
                GlobalFlash3Running = false;
                //_RazerFlash2Running = false;
                //_CorsairFlash2Running = false;

                if (RazerSDKCalled == 1)
                {
                    _RzFl3CTS.Cancel();
                    MemoryTasks.Remove(_RzFl3);
                }

                if (CorsairSDKCalled == 1)
                {
                    _CorsairFl3CTS.Cancel();
                    MemoryTasks.Remove(_CorsairFl3);
                }

                if (LogitechSDKCalled == 1)
                {
                    _LogiFl3CTS.Cancel();
                    MemoryTasks.Remove(_LogiFl3);
                }
                
                //Debug.WriteLine("Stopping Flash 3");

                MemoryTasks.Cleanup();
            }

            MemoryTasks.Cleanup();
        }
    }
}
