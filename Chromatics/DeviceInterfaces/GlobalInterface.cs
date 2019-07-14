using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromatics.Controllers;
using Chromatics.DeviceInterfaces;
using Chromatics.DeviceInterfaces.EffectLibrary;
using CSharpAnalytics;

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

        private ISteelSdk _steel;
        private CancellationTokenSource _steelFl1Cts = new CancellationTokenSource();
        private Task _steelFl2;
        private CancellationTokenSource _steelFl2Cts = new CancellationTokenSource();
        private Task _steelFl3;
        private CancellationTokenSource _steelFl3Cts = new CancellationTokenSource();
        private Task _steelFl4;
        private CancellationTokenSource _steelFl4Cts = new CancellationTokenSource();
        private Task _steelFlash;

        private IWootingSdk _wooting;
        private CancellationTokenSource _wootingFl1Cts = new CancellationTokenSource();
        private Task _wootingFl2;
        private CancellationTokenSource _wootingFl2Cts = new CancellationTokenSource();
        private Task _wootingFl3;
        private CancellationTokenSource _wootingFl3Cts = new CancellationTokenSource();
        private Task _wootingFl4;
        private CancellationTokenSource _wootingFl4Cts = new CancellationTokenSource();
        private Task _wootingFlash;

        private IAsusSdk _asus;
        private CancellationTokenSource _asusFl1Cts = new CancellationTokenSource();
        private Task _asusFl2;
        private CancellationTokenSource _asusFl2Cts = new CancellationTokenSource();
        private Task _asusFl3;
        private CancellationTokenSource _asusFl3Cts = new CancellationTokenSource();
        private Task _asusFl4;
        private CancellationTokenSource _asusFl4Cts = new CancellationTokenSource();
        private Task _asusFlash;

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
        private CancellationTokenSource _rzFl1Cts = new CancellationTokenSource();

        private Task _rzFl2;
        private CancellationTokenSource _rzFl2Cts = new CancellationTokenSource();
        private Task _rzFl3;
        private CancellationTokenSource _rzFl3Cts = new CancellationTokenSource();
        private Task _rzFl4;
        private CancellationTokenSource _rzFl4Cts = new CancellationTokenSource();
        private Task _coolermasterFlash;
        private Task _corsairFlash;

        private bool _globalParticleRunning;
        private Task _rzPart;
        private CancellationTokenSource _rzPartCts = new CancellationTokenSource();
        private Task _logiPart;
        private CancellationTokenSource _logiPartCts = new CancellationTokenSource();
        private Task _corsairPart;
        private CancellationTokenSource _corsairPartCts = new CancellationTokenSource();
        private Task _coolermasterPart;
        private CancellationTokenSource _coolermasterPartCts = new CancellationTokenSource();
        private Task _steelPart;
        private CancellationTokenSource _steelPartCts = new CancellationTokenSource();
        private Task _wootingPart;
        private CancellationTokenSource _wootingPartCts = new CancellationTokenSource();
        private Task _asusPart;
        private CancellationTokenSource _asusPartCts = new CancellationTokenSource();

        private bool _globalCycleRunning;
        private Task _rzCycle;
        private CancellationTokenSource _rzCycleCts = new CancellationTokenSource();
        private Task _logiCycle;
        private CancellationTokenSource _logiCycleCts = new CancellationTokenSource();
        private Task _corsairCycle;
        private CancellationTokenSource _corsairCycleCts = new CancellationTokenSource();
        private Task _coolermasterCycle;
        private CancellationTokenSource _coolermasterCycleCts = new CancellationTokenSource();
        private Task _steelCycle;
        private CancellationTokenSource _steelCycleCts = new CancellationTokenSource();
        private Task _wootingCycle;
        private CancellationTokenSource _wootingCycleCts = new CancellationTokenSource();
        private Task _asusCycle;
        private CancellationTokenSource _asusCycleCts = new CancellationTokenSource();

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
            if (_SDKRazer && RazerSdkCalled == 0)
            {
                WriteConsole(ConsoleTypes.Razer, @"Attempting to load Razer SDK..");
                _razer = RazerInterface.InitializeRazerSdk();
                if (_razer != null)
                {
                    RazerSdk = true;
                    RazerSdkCalled = 1;
                    razerFirstSet = true;
                    //WriteConsole(ConsoleTypes.Razer, @"Razer SDK Loaded");
                    _razer.InitializeLights(ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Razer");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Razer, @"Razer SDK failed to load.");
                }
            }

            if (_SDKLogitech && LogitechSdkCalled == 0)
            {
                WriteConsole(ConsoleTypes.Logitech, @"Attempting to load Logitech SDK..");
                _logitech = LogitechInterface.InitializeLogitechSdk();
                if (_logitech != null)
                {
                    LogitechSdk = true;
                    LogitechSdkCalled = 1;
                    logitechFirstSet = true;
                    WriteConsole(ConsoleTypes.Logitech, @"Logitech SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Logitech");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Logitech, @"Logitech SDK failed to load. Please make sure LGS is running.");
                }
            }

            if (_SDKCorsair && CorsairSdkCalled == 0)
            {
                WriteConsole(ConsoleTypes.Corsair, @"Attempting to load Corsair SDK..");
                _corsair = CorsairInterface.InitializeCorsairSdk(Color.BlueViolet);
                if (_corsair != null)
                {
                    CorsairSdk = true;
                    CorsairSdkCalled = 1;
                    corsairFirstSet = true;
                    //WriteConsole(ConsoleTypes.Corsair, @"Corsair SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Corsair");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Corsair, @"CUE SDK failed to load. Please make sure CUE2 or iCUE is running.");
                }
            }

            if (_SDKCooler && CoolermasterSdkCalled == 0)
            {
                _coolermaster = CoolermasterInterface.InitializeCoolermasterSdk();
                if (_coolermaster != null)
                {
                    CoolermasterSdk = true;
                    CoolermasterSdkCalled = 1;
                    coolermasterFirstSet = true;
                    WriteConsole(ConsoleTypes.Coolermaster, @"Coolermaster SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Coolermaster");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Coolermaster, @"Coolermaster SDK failed to load.");
                }
            }

            if (_SDKSteelSeries && SteelSdkCalled == 0)
            {
                WriteConsole(ConsoleTypes.Steel, @"Attempting to load SteelSeries GameSense SDK..");
                _steel = SteelSeriesInterface.InitializeSteelSdk();
                if (_steel != null)
                {
                    SteelSdk = true;
                    SteelSdkCalled = 1;
                    steelFirstSet = true;
                
                    WriteConsole(ConsoleTypes.Steel, @"SteelSeries SDK Loaded.");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("SteelSeries");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Steel, @"SteelSeries SDK failed to load. Please make sure SteelSeries Engine is running.");
                }
            }

            if (_SDKWooting && WootingSdkCalled == 0)
            {
                _wooting = WootingInterface.InitializeWootingSdk();
                if (_wooting != null)
                {
                    WootingSdk = true;
                    WootingSdkCalled = 1;
                    wootingFirstSet = true;
                    WriteConsole(ConsoleTypes.Wooting, @"Wooting SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Wooting");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Wooting, @"Wooting SDK failed to load.");
                }
            }

            if (_SDKAsus && AsusSdkCalled == 0)
            {
                WriteConsole(ConsoleTypes.Asus, @"Attempting to load Asus Aura SDK..");
                _asus = AsusInterface.InitializeAsusSdk();
                if (_asus != null)
                {
                    AsusSdk = true;
                    AsusSdkCalled = 1;
                    AsusFirstSet = true;
                    WriteConsole(ConsoleTypes.Asus, @"Asus Aura SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Asus");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Asus, @"Asus Aura SDK failed to load. Please make sure AURA is installed but NOT running.");
                }
            }

            if (_SDKLifx && LifxSdkCalled == 0)
            {
                //Load LIFX SDK
                _lifx = LifxInterface.InitializeLifxsdk();
                if (_lifx != null)
                {
                    LifxSdk = true;
                    LifxSdkCalled = 1;
                    lifxFirstSet = true;
                    //WriteConsole(ConsoleTypes.LIFX, "LIFX SDK Loaded");

                    if (_lifx.LifxBulbs > 0)
                    {
                        if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                        {
                            AutoMeasurement.Client.TrackScreenView("LIFX");
                        }
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Lifx, @"LIFX SDK failed to load.");
                }
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
            //GlobalResetDevices();
        }

        public void ReInitializeSdk()
        {
            if (_SDKRazer && RazerSdkCalled == 0 && razerFirstSet)
            {
                WriteConsole(ConsoleTypes.Razer, @"Attempting to load Razer SDK..");
                _razer = RazerInterface.InitializeRazerSdk();
                if (_razer != null)
                {
                    RazerSdk = true;
                    RazerSdkCalled = 1;
                    //WriteConsole(ConsoleTypes.Razer, @"Razer SDK Loaded");
                    _razer.InitializeLights(ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Razer");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Razer, @"Razer SDK failed to load.");
                }
            }

            if (_SDKLogitech && LogitechSdkCalled == 0 && logitechFirstSet)
            {
                WriteConsole(ConsoleTypes.Logitech, @"Attempting to load Logitech SDK..");
                _logitech = LogitechInterface.InitializeLogitechSdk();
                if (_logitech != null)
                {
                    LogitechSdk = true;
                    LogitechSdkCalled = 1;
                    WriteConsole(ConsoleTypes.Logitech, @"Logitech SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Logitech");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Logitech, @"Logitech SDK failed to load. Please make sure LGS is running.");
                }
            }

            if (_SDKCorsair && CorsairSdkCalled == 0 && corsairFirstSet)
            {
                WriteConsole(ConsoleTypes.Corsair, @"Attempting to load Corsair SDK..");
                _corsair = CorsairInterface.InitializeCorsairSdk(Color.BlueViolet);
                if (_corsair != null)
                {
                    CorsairSdk = true;
                    CorsairSdkCalled = 1;
                    //WriteConsole(ConsoleTypes.Corsair, @"Corsair SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Corsair");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Corsair, @"CUE SDK failed to load. Please make sure CUE2 or iCUE is running.");
                }
            }

            if (_SDKCooler && CoolermasterSdkCalled == 0 && coolermasterFirstSet)
            {
                _coolermaster = CoolermasterInterface.InitializeCoolermasterSdk();
                if (_coolermaster != null)
                {
                    CoolermasterSdk = true;
                    CoolermasterSdkCalled = 1;
                    WriteConsole(ConsoleTypes.Coolermaster, @"Coolermaster SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Coolermaster");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Coolermaster, @"Coolermaster SDK failed to load.");
                }
            }

            if (_SDKSteelSeries && SteelSdkCalled == 0 && steelFirstSet)
            {
                WriteConsole(ConsoleTypes.Steel, @"Attempting to load SteelSeries GameSense SDK..");
                _steel = SteelSeriesInterface.InitializeSteelSdk();
                if (_steel != null)
                {
                    SteelSdk = true;
                    SteelSdkCalled = 1;
                
                    WriteConsole(ConsoleTypes.Steel, @"SteelSeries SDK Loaded.");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("SteelSeries");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Steel, @"SteelSeries SDK failed to load. Please make sure SteelSeries Engine is running.");
                }
            }

            if (_SDKWooting && WootingSdkCalled == 0 && wootingFirstSet)
            {
                _wooting = WootingInterface.InitializeWootingSdk();
                if (_wooting != null)
                {
                    WootingSdk = true;
                    WootingSdkCalled = 1;
                    WriteConsole(ConsoleTypes.Wooting, @"Wooting SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Wooting");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Wooting, @"Wooting SDK failed to load.");
                }
            }

            if (_SDKAsus && AsusSdkCalled == 0 && AsusFirstSet)
            {
                _asus = AsusInterface.InitializeAsusSdk();
                if (_asus != null)
                {
                    AsusSdk = true;
                    AsusSdkCalled = 1;
                    WriteConsole(ConsoleTypes.Asus, @"Asus Aura SDK Loaded");

                    if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                    {
                        AutoMeasurement.Client.TrackScreenView("Asus");
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Asus, @"Asus Aura SDK failed to load.");
                }
            }

            if (_SDKLifx && LifxSdkCalled == 0 && lifxFirstSet)
            {
                //Load LIFX SDK
                _lifx = LifxInterface.InitializeLifxsdk();
                if (_lifx != null)
                {
                    LifxSdk = true;
                    LifxSdkCalled = 1;
                    //WriteConsole(ConsoleTypes.LIFX, "LIFX SDK Loaded");

                    if (_lifx.LifxBulbs > 0)
                    {
                        if (ChromaticsSettings.ChromaticsSettingsDebugOpt)
                        {
                            AutoMeasurement.Client.TrackScreenView("LIFX");
                        }
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.Lifx, @"LIFX SDK failed to load.");
                }
            }
            
            //ResetDeviceDataGrid();
        }

        public void ShutDownDevices()
        {
            if (RazerSdkCalled == 1)
            {
                _razer.ShutdownSdk();
                _razer = null;
                RazerSdkCalled = 0;
            }

            if (LogitechSdkCalled == 1)
            {
                _logitech.ShutdownSdk();
                _logitech = null;
                LogitechSdkCalled = 0;
            }

            if (CorsairSdkCalled == 1)
            {
                _corsair.ShutdownSdk();
                _corsair = null;
                CorsairSdkCalled = 0;
            }

            if (CoolermasterSdkCalled == 1)
            {
                _coolermaster.Shutdown();
                _coolermaster = null;
                CoolermasterSdkCalled = 0;
            }

            if (SteelSdkCalled == 1)
            {
                _steel.Shutdown();
                _steel = null;
                SteelSdkCalled = 0;
            }

            if (WootingSdkCalled == 1)
            {
                _wooting.Shutdown();
                _wooting = null;
                WootingSdkCalled = 0;
            }

            if (AsusSdkCalled == 1)
            {
                _asus.ShutdownSdk();
                _asus = null;
                AsusSdkCalled = 0;
            }
        }

        public void GlobalCheckCrash()
        {
            //Check Razer library for crash
            /*
            if (RazerSdkCalled == 1)
            {
                if (_razer.CheckCrash())
                {
                    WriteConsole(ConsoleTypes.Error, @"Razer SDK has crashed.");
                    _razer.ShutdownSdk();
                    RazerSdkCalled = 0;
                    RazerSdk = false;

                    _rzFl1Cts.Cancel();
                    MemoryTasks.Remove(_rzFlash);
                    _rzFl2Cts.Cancel();
                    MemoryTasks.Remove(_rzFl2);
                    _rzFl3Cts.Cancel();
                    MemoryTasks.Remove(_rzFl3);
                    _rzFl4Cts.Cancel();
                    MemoryTasks.Remove(_rzFl4);
                    _rzPartCts.Cancel();
                    MemoryTasks.Remove(_rzPart);
                    _rzCycleCts.Cancel();
                    MemoryTasks.Remove(_rzCycle);
                    
                    _razer = null;

                    _razer = RazerInterface.InitializeRazerSdk();
                    if (_razer != null)
                    {
                        RazerSdk = true;
                        RazerSdkCalled = 1;
                    }
                    else
                    {
                        WriteConsole(ConsoleTypes.Razer, @"Razer SDK failed to load.");
                    }
                }
            }
            */
        }

        public void GlobalResetDevices()
        {
            var baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor);

            if (RazerSdkCalled == 1)
                _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse, _razerDeviceMousepad,
                    _razerDeviceHeadset, _razerDeviceChromaLink, baseColor);

            if (LogitechSdkCalled == 1)
                _logitech.ResetLogitechDevices(_logitechDeviceKeyboard, baseColor);

            if (CorsairSdkCalled == 1)
                _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                    _corsairDeviceMousepad, _corsairDeviceHeadset, _razerDeviceChromaLink, baseColor);

            if (CoolermasterSdkCalled == 1)
                _coolermaster.ResetCoolermasterDevices(_coolermasterDeviceKeyboard, _coolermasterDeviceMouse, baseColor);

            if (SteelSdkCalled == 1)
                _steel.ResetSteelSeriesDevices(_steelDeviceKeyboard, _steelDeviceMouse, _steelDeviceHeadset, baseColor);

            if (WootingSdkCalled == 1)
                _wooting.ResetWootingDevices(_steelDeviceKeyboard, baseColor);

            if (AsusSdkCalled == 1)
                _asus.ResetAsusDevices(_asusDeviceKeyboard, _asusDeviceMouse, _asusDeviceHeadset, _asusDeviceOther, baseColor);

            //ResetDeviceDataGrid();
        }
        
        public void GlobalSetWave()
        {
            if (RazerSdkCalled == 1)
                _razer.SetWave();

            if (LogitechSdkCalled == 1)
                _logitech.SetWave(_baseColor);

            if (CorsairSdkCalled == 1)
                return;

            if (CoolermasterSdkCalled == 1)
                _coolermaster.SetWave();
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
            {
                if (mode != BulbModeTypes.Disabled)
                    if (mode == BulbModeTypes.Standby)
                        _lifx.LifxUpdateStateBrightness(mode, Color.Black, brightness, transition);
                    else
                        _lifx.LifxUpdateStateBrightness(mode, col, brightness, transition);
            }

            if (HueSdkCalled == 1)
            {
                if (mode != BulbModeTypes.Disabled)
                    if (mode == BulbModeTypes.Standby)
                        _hue.HueUpdateStateBrightness(mode, Color.Black, brightness, transition);
                    else
                        _hue.HueUpdateStateBrightness(mode, col, brightness, transition);
            }
        }

        //_lifx.LIFXUpdateStateBrightness(9, col_tpfull, (ushort) pol_TPZ, 250);

        public void GlobalKeyboardUpdate()
        {
            if (!HoldReader)
            {
                if (RazerSdkCalled == 1)
                    _razer.DeviceUpdate();

                if (SteelSdkCalled == 1)
                    _steel.DeviceUpdate();

                if (WootingSdkCalled == 1)
                    _wooting.DeviceUpdate();

                if (AsusSdkCalled == 1)
                    _asus.DeviceUpdate();
            }
        }

        public void GlobalApplyAllDeviceLighting(Color col)
        {
            if (_KeysSingleKeyModeEnabled) GlobalApplySingleZoneLighting(col);
            if (_KeysMultiKeyModeEnabled) GlobalApplyMultiZoneLighting(col, "All");

            if (RazerSdkCalled == 1)
            {
                _razer.SetAllLights(col);
            }

            if (LogitechSdkCalled == 1)
            {
                _logitech.SetAllLights(col);
            }
            
            if (CorsairSdkCalled == 1)
            {
                _corsair.SetAllLights(col);
            }
            
            if (CoolermasterSdkCalled == 1)
            {
                _coolermaster.SetAllLights(col);
                //_coolermaster.ApplyMapMouseLighting("", col, false);
            }

            if (SteelSdkCalled == 1)
            {
                _steel.SetAllLights(col);
            }

            if (WootingSdkCalled == 1)
            {
                _wooting.SetAllLights(col);
            }

            if (AsusSdkCalled == 1)
            {
                _asus.SetAllLights(col);
            }
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
            {
                _coolermaster.SetLights(col);
            }

            if (SteelSdkCalled == 1)
            {
                _steel.SetLights(col);
            }

            if (WootingSdkCalled == 1)
            {
                _wooting.SetLights(col);
            }

            if (AsusSdkCalled == 1)
            {
                _asus.SetLights(col);
            }
        }

        public void GlobalApplySingleZoneLighting(Color col)
        {
            if (!_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
                _razer.ApplyMapSingleLighting(col);

            if (LogitechSdkCalled == 1)
                _logitech.ApplyMapSingleLighting(col);

            if (CorsairSdkCalled == 1)
                _corsair.SetLights(col);

            if (CoolermasterSdkCalled == 1)
                _coolermaster.SetLights(col);

            if (SteelSdkCalled == 1)
                _steel.ApplyMapSingleLighting(col);

            if (WootingSdkCalled == 1)
                _wooting.ApplyMapSingleLighting(col);

            if (AsusSdkCalled == 1)
                _asus.ApplyMapSingleLighting(col);
        }

        public void GlobalApplyMultiZoneLighting(Color col, string region)
        {
            if (!_KeysMultiKeyModeEnabled || _KeysSingleKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
                _razer.ApplyMapMultiLighting(col, region);

            if (LogitechSdkCalled == 1)
                _logitech.ApplyMapSingleLighting(col);

            if (CorsairSdkCalled == 1)
                _corsair.SetLights(col);

            if (CoolermasterSdkCalled == 1)
                _coolermaster.SetLights(col);

            if (SteelSdkCalled == 1)
                _steel.ApplyMapMultiLighting(col, region);

            if (WootingSdkCalled == 1)
                _wooting.ApplyMapMultiLighting(col, region);

            if (AsusSdkCalled == 1)
                _asus.ApplyMapMultiLighting(col, region);
        }

        //Send a lighting command to a specific Keyboard LED
        public void GlobalApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
                _razer.ApplyMapKeyLighting(Localization.LocalizeKey(key), col, clear, bypasswhitelist);

            if (LogitechSdkCalled == 1)
            {
                if (key == "Macro1")
                    _logitech.ApplyMapKeyLighting(Localization.LocalizeKey("Macro1"), col, clear, bypasswhitelist);
                else if (key == "Macro2")
                    _logitech.ApplyMapKeyLighting(Localization.LocalizeKey("Macro4"), col, clear, bypasswhitelist);
                else if (key == "Macro3")
                    _logitech.ApplyMapKeyLighting(Localization.LocalizeKey("Macro7"), col, clear, bypasswhitelist);
                else if (key == "Macro4")
                    _logitech.ApplyMapKeyLighting(Localization.LocalizeKey("Macro10"), col, clear, bypasswhitelist);
                else if (key == "Macro5")
                    _logitech.ApplyMapKeyLighting(Localization.LocalizeKey("Macro13"), col, clear, bypasswhitelist);
                else
                    _logitech.ApplyMapKeyLighting(Localization.LocalizeKey(key), col, clear, bypasswhitelist);

                _logitech.ApplyMapKeyLighting(Localization.LocalizeKey(key), col, clear, bypasswhitelist);
            }

            if (CorsairSdkCalled == 1)
                if (key == "Macro1")
                {
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro1"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro2"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro3"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro2")
                {
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro4"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro5"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro6"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro3")
                {
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro7"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro8"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro9"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro4")
                {
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro10"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro11"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro12"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro5")
                {
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro13"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro14"), col, clear, bypasswhitelist);
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey("Macro15"), col, clear, bypasswhitelist);
                }
                else
                {
                    _corsair.ApplyMapKeyLighting(Localization.LocalizeKey(key), col, clear, bypasswhitelist);
                }

            if (CoolermasterSdkCalled == 1)
                _coolermaster.ApplyMapKeyLighting(Localization.LocalizeKey(key), col, clear, bypasswhitelist);

            if (SteelSdkCalled == 1)
            {
                if (key == "Macro1")
                {
                    _steel.ApplyMapKeyLighting(Localization.LocalizeKey("Macro1"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro2")
                {
                    _steel.ApplyMapKeyLighting(Localization.LocalizeKey("Macro2"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro3")
                {
                    _steel.ApplyMapKeyLighting(Localization.LocalizeKey("Macro3"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro4")
                {
                    _steel.ApplyMapKeyLighting(Localization.LocalizeKey("Macro4"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro5")
                {
                    _steel.ApplyMapKeyLighting(Localization.LocalizeKey("Macro5"), col, clear, bypasswhitelist);
                }
                else
                {
                    _steel.ApplyMapKeyLighting(Localization.LocalizeKey(key), col, clear, bypasswhitelist);
                }
            }

            if (WootingSdkCalled == 1)
            {
                if (key == "Macro1")
                {
                    _wooting.ApplyMapKeyLighting(Localization.LocalizeKey("Macro1"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro2")
                {
                    _wooting.ApplyMapKeyLighting(Localization.LocalizeKey("Macro2"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro3")
                {
                    _wooting.ApplyMapKeyLighting(Localization.LocalizeKey("Macro3"), col, clear, bypasswhitelist);
                }
                else if (key == "Macro4")
                {
                    _wooting.ApplyMapKeyLighting(Localization.LocalizeKey("Macro4"), col, clear, bypasswhitelist);
                }
                else
                {
                    _wooting.ApplyMapKeyLighting(Localization.LocalizeKey(key), col, clear, bypasswhitelist);
                }
            }

            if (AsusSdkCalled == 1)
            {
                _asus.ApplyMapKeyLighting(Localization.LocalizeKey(key), col, clear, bypasswhitelist);
            }
        }

        public void GlobalApplyMapLightbarLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                return;

            if (CorsairSdkCalled == 1)
                _corsair.ApplyMapKeyLighting(key, col, clear, bypasswhitelist);
        }

        public void GlobalApplyKeySingleLighting(DevModeTypes mode, Color col)
        {
            if (!_KeysSingleKeyModeEnabled || mode == DevModeTypes.Disabled || mode != _KeysSingleKeyMode) return;

            GlobalApplySingleZoneLighting(col);
        }

        public void GlobalApplyKeyMultiLighting(DevMultiModeTypes mode, Color col, string region)
        {
            if (!_KeysMultiKeyModeEnabled || mode == DevMultiModeTypes.Disabled || mode != _KeysMultiKeyMode) return;

            GlobalApplyMultiZoneLighting(col, region);
        }

        public void GlobalApplyKeySingleLightingBrightness(DevModeTypes mode, Color colMin, Color colMax, double val)
        {
            if (!_KeysSingleKeyModeEnabled || mode == DevModeTypes.Disabled || mode != _KeysSingleKeyMode) return;

            //var c2 = ControlPaint.Dark(col, 100 - Convert.ToSingle(val));

            var c2 = ColorInterpolator.InterpolateBetween(colMin, colMax, val);
            
            GlobalApplySingleZoneLighting(c2);
        }

        public void GlobalApplyKeyMultiLightingBrightness(DevMultiModeTypes mode, Color colMin, Color colMax, double val)
        {
            if (!_KeysMultiKeyModeEnabled || mode == DevMultiModeTypes.Disabled || mode != _KeysMultiKeyMode) return;

            //var c2 = ControlPaint.Dark(col, 100 - Convert.ToSingle(val));
            var c2 = ColorInterpolator.InterpolateBetween(colMin, colMax, val);

            GlobalApplyMultiZoneLighting(c2, "All");
        }

        //Send a lighting command to a specific Keyboard LED outside of MapKey scope
        public void GlobalApplyMapLogoLighting(string key, Color col, bool clear)
        {
            if (RazerSdkCalled == 1)
                _razer.ApplyMapLogoLighting(Localization.LocalizeKey(key), col, clear);

            if (LogitechSdkCalled == 1)
            {
                //
            }

            if (CorsairSdkCalled == 1)
                _corsair.ApplyMapLogoLighting(Localization.LocalizeKey(key), col, clear);

            if (SteelSdkCalled == 1)
            {
                //
            }

            if (WootingSdkCalled == 1)
            {
                //
            }

            if (AsusSdkCalled == 1)
            {
                //
            }
        }

        //Send a lighting command to a specific Mouse LED
        public void GlobalApplyMapMouseLighting(DevModeTypes mode, Color col, bool clear)
        {
            if (mode == DevModeTypes.Disabled) return;
            if (mode != _MouseStrip1Mode && mode != _MouseZone2Mode && mode != _MouseZone3Mode) return;
            
            //Logo
            if (mode == _MouseZone1Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapMouseLighting("Logo", col, clear);

                if (LogitechSdkCalled == 1)
                {
                    _logitech.ApplyMapMouseLighting("0", col);
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsair.ApplyMapMouseLighting("MouseLogo", col, clear);
                    _corsair.ApplyMapMouseLighting("MouseFront", col, clear);
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermaster.ApplyMapMouseLighting("MouseFront", col, clear);
                }

                if (SteelSdkCalled == 1)
                {
                    _steel.ApplyMapMouseLighting("MouseFront", col);
                }

                if (AsusSdkCalled == 1)
                {
                    _asus.ApplyMapMouseLighting("Logo", col);
                }
            }

            //Scroll
            if (mode == _MouseZone2Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapMouseLighting("ScrollWheel", col, clear);

                if (LogitechSdkCalled == 1)
                {
                    _logitech.ApplyMapMouseLighting("1", col);
                }

                if (CorsairSdkCalled == 1)
                    _corsair.ApplyMapMouseLighting("MouseScroll", col, clear);

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermaster.ApplyMapMouseLighting("MouseScroll", col, clear);
                }

                if (SteelSdkCalled == 1)
                {
                    _steel.ApplyMapMouseLighting("MouseScroll", col);
                }

                if (AsusSdkCalled == 1)
                {
                    _asus.ApplyMapMouseLighting("ScrollWheel", col);
                }
            }

            //Other
            if (mode == _MouseZone3Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapMouseLighting("Backlight", col, clear);

                if (LogitechSdkCalled == 1)
                {
                    _logitech.ApplyMapMouseLighting("2", col);
                }

                if (CorsairSdkCalled == 1)
                    _corsair.ApplyMapMouseLighting("MouseSide", col, clear);

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermaster.ApplyMapMouseLighting("MouseSide", col, clear);
                }

                if (SteelSdkCalled == 1)
                {
                    _steel.ApplyMapMouseLighting("MouseLogo", col);
                }

                if (AsusSdkCalled == 1)
                {
                    _asus.ApplyMapMouseLighting("FrontLight", col);
                }

            }
        }

        public void GlobalApplyStripMouseLighting(DevModeTypes mode, string region1, string region2, Color col, bool clear)
        {
            if (mode == DevModeTypes.Disabled) return;
            if (mode != _MouseStrip1Mode && mode != _MouseStrip2Mode) return;

            //Logo
            if (mode == _MouseStrip1Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapMouseLighting(region1, col, clear);

                if (LogitechSdkCalled == 1)
                {
                    //
                }

                if (CorsairSdkCalled == 1)
                {
                    //
                }
            }

            //Scroll
            if (mode == _MouseStrip2Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapMouseLighting(region2, col, clear);

                if (LogitechSdkCalled == 1)
                {
                    //
                }

                if (CorsairSdkCalled == 1)
                {
                    //
                }
            }
        }

        public void GlobalApplyMapMouseLightingBrightness(DevModeTypes mode, Color colMin, Color colMax, bool clear, double val)
        {
            if (mode == DevModeTypes.Disabled) return;
            if (mode != _MouseStrip1Mode && mode != _MouseZone2Mode && mode != _MouseZone3Mode) return;

            //var c2 = ControlPaint.Dark(col, 100 - Convert.ToSingle(val));

            var c2 = ColorInterpolator.InterpolateBetween(colMin, colMax, val);

            GlobalApplyMapMouseLighting(mode, c2, clear);
        }

        //Send a lighting command to a specific Headset LED
        public void GlobalApplyMapHeadsetLighting(DevModeTypes mode, Color col, bool clear)
        {
            if (mode == DevModeTypes.Disabled) return;
            if (mode != _HeadsetZone1Mode && mode != _HeadsetZone2Mode) return;

            //Logo
            if (mode == _HeadsetZone1Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapHeadsetLighting(col, clear);

                if (LogitechSdkCalled == 1)
                {
                    _logitech.ApplyMapHeadsetLighting("0", col);
                    _logitech.ApplyMapPadSpeakers("0", col);
                    _logitech.ApplyMapPadSpeakers("2", col);
                }

                if (CorsairSdkCalled == 1)
                {
                    _corsair.ApplyMapHeadsetLighting(col, clear);
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //
                }

                if (SteelSdkCalled == 1)
                {
                    _steel.ApplyMapHeadsetLighting(col);
                }

                if (AsusSdkCalled == 1)
                {
                    _asus.ApplyMapHeadsetLighting("0", col);
                }

            }

            if (mode == _HeadsetZone2Mode)
            {
                if (RazerSdkCalled == 1)
                {
                    _razer.ApplyMapHeadsetLighting(col, clear);
                }

                if (LogitechSdkCalled == 1)
                {
                    _logitech.ApplyMapHeadsetLighting("1", col);
                    _logitech.ApplyMapPadSpeakers("1", col);
                    _logitech.ApplyMapPadSpeakers("3", col);
                }

                if (CorsairSdkCalled == 1)
                {
                    //
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //
                }

                if (SteelSdkCalled == 1)
                {
                    //
                }

                if (AsusSdkCalled == 1)
                {
                    _asus.ApplyMapHeadsetLighting("1", col);
                }

            }
        }

        public void GlobalApplyMapHeadsetLightingBrightness(DevModeTypes mode, Color colMin, Color colMax, bool clear, double val)
        {
            if (mode == DevModeTypes.Disabled) return;
            if (mode != _HeadsetZone1Mode) return;

            //var c2 = ControlPaint.Dark(col, 100 - Convert.ToSingle(val));
            var c2 = ColorInterpolator.InterpolateBetween(colMin, colMax, val);

            GlobalApplyMapHeadsetLighting(mode, c2, clear);
        }

        //Send a lighting command to a specific Keypad LED
        public void GlobalApplyMapKeypadLighting(DevMultiModeTypes mode, Color col, bool clear, string region)
        {
            
            if (mode == DevMultiModeTypes.Disabled) return;
            if (mode != _KeypadZone1Mode) return;

            //Logo
            if (mode == _KeypadZone1Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapKeypadLighting(col, clear, region);

                if (LogitechSdkCalled == 1)
                {
                    //
                }

                if (CorsairSdkCalled == 1)
                {
                    //
                }

                if (CoolermasterSdkCalled == 1)
                {
                    //
                }

                if (SteelSdkCalled == 1)
                {
                    //
                }
            }
            
        }

        public void GlobalApplyMapKeypadLightingBrightness(DevMultiModeTypes mode, Color colMin, Color colMax, bool clear, double val)
        {
            if (mode == DevMultiModeTypes.Disabled) return;
            if (mode != _KeypadZone1Mode) return;

            //var c2 = ControlPaint.Dark(col, 100 - Convert.ToSingle(val));
            var c2 = ColorInterpolator.InterpolateBetween(colMin, colMax, val);

            GlobalApplyMapKeypadLighting(mode, c2, clear, "All");
        }

        //Send a lighting command to a specific Keypad LED
        public void GlobalApplyMapChromaLinkLighting(DevModeTypes mode, Color col)
        {
            if (mode == DevModeTypes.Disabled) return;
            if (mode != _CLZone1Mode && mode != _CLZone2Mode && mode != _CLZone3Mode && mode != _CLZone4Mode && mode != _CLZone5Mode && mode != _CLZone6Mode) return;

            
            
            if (mode == _CLZone1Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapChromaLinkLighting(col, 0);

                if (CorsairSdkCalled == 1)
                    _corsair.ApplyMapOtherLighting(col, 1, _ChromaLinkLEDCountZ1, _ChromaLinkLEDCountZ2, _ChromaLinkLEDCountZ3, _ChromaLinkLEDCountZ4, _ChromaLinkLEDCountZ5, _ChromaLinkLEDCountZ6);

                if (AsusSdkCalled == 1)
                    _asus.ApplyMapOtherLighting(1, col);
            }

            if (mode == _CLZone2Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapChromaLinkLighting(col, 1);

                if (CorsairSdkCalled == 1)
                    _corsair.ApplyMapOtherLighting(col, 2, _ChromaLinkLEDCountZ1, _ChromaLinkLEDCountZ2, _ChromaLinkLEDCountZ3, _ChromaLinkLEDCountZ4, _ChromaLinkLEDCountZ5, _ChromaLinkLEDCountZ6);

                if (AsusSdkCalled == 1)
                    _asus.ApplyMapOtherLighting(2, col);
            }

            if (mode == _CLZone3Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapChromaLinkLighting(col, 2);

                if (CorsairSdkCalled == 1)
                    _corsair.ApplyMapOtherLighting(col, 3, _ChromaLinkLEDCountZ1, _ChromaLinkLEDCountZ2, _ChromaLinkLEDCountZ3, _ChromaLinkLEDCountZ4, _ChromaLinkLEDCountZ5, _ChromaLinkLEDCountZ6);

                if (AsusSdkCalled == 1)
                    _asus.ApplyMapOtherLighting(3, col);
            }

            if (mode == _CLZone4Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapChromaLinkLighting(col, 3);

                if (CorsairSdkCalled == 1)
                    _corsair.ApplyMapOtherLighting(col, 4, _ChromaLinkLEDCountZ1, _ChromaLinkLEDCountZ2, _ChromaLinkLEDCountZ3, _ChromaLinkLEDCountZ4, _ChromaLinkLEDCountZ5, _ChromaLinkLEDCountZ6);

                if (AsusSdkCalled == 1)
                    _asus.ApplyMapOtherLighting(4, col);
            }

            if (mode == _CLZone5Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapChromaLinkLighting(col, 4);

                if (CorsairSdkCalled == 1)
                    _corsair.ApplyMapOtherLighting(col, 5, _ChromaLinkLEDCountZ1, _ChromaLinkLEDCountZ2, _ChromaLinkLEDCountZ3, _ChromaLinkLEDCountZ4, _ChromaLinkLEDCountZ5, _ChromaLinkLEDCountZ6);

                if (AsusSdkCalled == 1)
                    _asus.ApplyMapOtherLighting(5, col);
            }

            if (mode == _CLZone6Mode)
            {
                if (CorsairSdkCalled == 1)
                    _corsair.ApplyMapOtherLighting(col, 6, _ChromaLinkLEDCountZ1, _ChromaLinkLEDCountZ2, _ChromaLinkLEDCountZ3, _ChromaLinkLEDCountZ4, _ChromaLinkLEDCountZ5, _ChromaLinkLEDCountZ6);

            }
        }

        public void GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes mode, Color colMin, Color colMax, double val)
        {
            if (mode == DevModeTypes.Disabled) return;
            if (mode != _CLZone1Mode && mode != _CLZone2Mode && mode != _CLZone3Mode && mode != _CLZone4Mode && mode != _CLZone5Mode && mode != _CLZone6Mode) return;

            //var c2 = ControlPaint.Dark(col, 100 - Convert.ToSingle(val));
            var c2 = ColorInterpolator.InterpolateBetween(colMin, colMax, val);

            GlobalApplyMapChromaLinkLighting(mode, c2);
        }

        //Send a lighting command to a specific Mousepad or HUE/LIFX LED
        public void GlobalApplyMapPadLighting(DevModeTypes mode, int region1, int region2, int region3, Color col, bool clear)
        {
            if (mode == DevModeTypes.Disabled) return;
            if (mode != _PadZone1Mode && mode != _PadZone2Mode && mode != _PadZone3Mode) return;

            if (mode == _PadZone1Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapPadLighting(region1, col, clear);

                if (LogitechSdkCalled == 1)
                {
                    _logitech.ApplyMapPadLighting("0", col);
                }

                if (CorsairSdkCalled == 1)
                {
                    var corsairPadRegion = "Pad1";
                    var corsairStandRegion = "HeadsetStandZone1";

                    switch (region1)
                    {
                        case 0:
                            corsairPadRegion = "Pad15";
                            corsairStandRegion = "HeadsetStandZone3";
                            break;
                        case 1:
                            corsairPadRegion = "Pad14";
                            corsairStandRegion = "HeadsetStandZone3";
                            break;
                        case 2:
                            corsairPadRegion = "Pad13";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 3:
                            corsairPadRegion = "Pad12";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 4:
                            corsairPadRegion = "Pad11";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 5:
                            corsairPadRegion = "Pad10";
                            corsairStandRegion = "HeadsetStandZone5";
                            break;
                        case 6:
                            corsairPadRegion = "Pad9";
                            corsairStandRegion = "HeadsetStandZone5";
                            break;
                        case 7:
                            corsairPadRegion = "Pad8";
                            corsairStandRegion = "HeadsetStandZone6";
                            break;
                        case 8:
                            corsairPadRegion = "Pad7";
                            corsairStandRegion = "HeadsetStandZone6";
                            break;
                        case 9:
                            corsairPadRegion = "Pad6";
                            corsairStandRegion = "HeadsetStandZone7";
                            break;
                        case 10:
                            corsairPadRegion = "Pad5";
                            corsairStandRegion = "HeadsetStandZone7";
                            break;
                        case 11:
                            corsairPadRegion = "Pad4";
                            corsairStandRegion = "HeadsetStandZone8";
                            break;
                        case 12:
                            corsairPadRegion = "Pad3";
                            corsairStandRegion = "HeadsetStandZone8";
                            break;
                        case 13:
                            corsairPadRegion = "Pad2";
                            corsairStandRegion = "HeadsetStandZone9";
                            break;
                        case 14:
                            corsairPadRegion = "Pad1";
                            corsairStandRegion = "HeadsetStandZone9";
                            break;
                    }

                    _corsair.ApplyMapPadLighting(corsairPadRegion, col, clear);
                    _corsair.ApplyMapStandLighting(corsairStandRegion, col, clear);
                }

                if (SteelSdkCalled == 1)
                {
                    //
                }
            }

            if (mode == _PadZone2Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapPadLighting(region2, col, clear);

                if (LogitechSdkCalled == 1)
                {
                    _logitech.ApplyMapPadLighting("1", col);
                }

                if (CorsairSdkCalled == 1)
                {
                    var corsairPadRegion = "Pad1";
                    var corsairStandRegion = "HeadsetStandZone1";

                    switch (region2)
                    {
                        case 0:
                            corsairPadRegion = "Pad15";
                            corsairStandRegion = "HeadsetStandZone3";
                            break;
                        case 1:
                            corsairPadRegion = "Pad14";
                            corsairStandRegion = "HeadsetStandZone3";
                            break;
                        case 2:
                            corsairPadRegion = "Pad13";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 3:
                            corsairPadRegion = "Pad12";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 4:
                            corsairPadRegion = "Pad11";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 5:
                            corsairPadRegion = "Pad10";
                            corsairStandRegion = "HeadsetStandZone5";
                            break;
                        case 6:
                            corsairPadRegion = "Pad9";
                            corsairStandRegion = "HeadsetStandZone5";
                            break;
                        case 7:
                            corsairPadRegion = "Pad8";
                            corsairStandRegion = "HeadsetStandZone6";
                            break;
                        case 8:
                            corsairPadRegion = "Pad7";
                            corsairStandRegion = "HeadsetStandZone6";
                            break;
                        case 9:
                            corsairPadRegion = "Pad6";
                            corsairStandRegion = "HeadsetStandZone7";
                            break;
                        case 10:
                            corsairPadRegion = "Pad5";
                            corsairStandRegion = "HeadsetStandZone7";
                            break;
                        case 11:
                            corsairPadRegion = "Pad4";
                            corsairStandRegion = "HeadsetStandZone8";
                            break;
                        case 12:
                            corsairPadRegion = "Pad3";
                            corsairStandRegion = "HeadsetStandZone8";
                            break;
                        case 13:
                            corsairPadRegion = "Pad2";
                            corsairStandRegion = "HeadsetStandZone9";
                            break;
                        case 14:
                            corsairPadRegion = "Pad1";
                            corsairStandRegion = "HeadsetStandZone9";
                            break;
                    }

                    _corsair.ApplyMapPadLighting(corsairPadRegion, col, clear);
                    _corsair.ApplyMapStandLighting(corsairStandRegion, col, clear);
                }

                if (SteelSdkCalled == 1)
                {
                    //
                }
            }

            if (mode == _PadZone3Mode)
            {
                if (RazerSdkCalled == 1)
                    _razer.ApplyMapPadLighting(region3, col, clear);

                if (LogitechSdkCalled == 1)
                {
                    _logitech.ApplyMapPadLighting("2", col);
                }

                if (CorsairSdkCalled == 1)
                {
                    var corsairPadRegion = "Pad1";
                    var corsairStandRegion = "HeadsetStandZone1";

                    switch (region3)
                    {
                        case 0:
                            corsairPadRegion = "Pad15";
                            corsairStandRegion = "HeadsetStandZone3";
                            break;
                        case 1:
                            corsairPadRegion = "Pad14";
                            corsairStandRegion = "HeadsetStandZone3";
                            break;
                        case 2:
                            corsairPadRegion = "Pad13";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 3:
                            corsairPadRegion = "Pad12";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 4:
                            corsairPadRegion = "Pad11";
                            corsairStandRegion = "HeadsetStandZone4";
                            break;
                        case 5:
                            corsairPadRegion = "Pad10";
                            corsairStandRegion = "HeadsetStandZone5";
                            break;
                        case 6:
                            corsairPadRegion = "Pad9";
                            corsairStandRegion = "HeadsetStandZone5";
                            break;
                        case 7:
                            corsairPadRegion = "Pad8";
                            corsairStandRegion = "HeadsetStandZone6";
                            break;
                        case 8:
                            corsairPadRegion = "Pad7";
                            corsairStandRegion = "HeadsetStandZone6";
                            break;
                        case 9:
                            corsairPadRegion = "Pad6";
                            corsairStandRegion = "HeadsetStandZone7";
                            break;
                        case 10:
                            corsairPadRegion = "Pad5";
                            corsairStandRegion = "HeadsetStandZone7";
                            break;
                        case 11:
                            corsairPadRegion = "Pad4";
                            corsairStandRegion = "HeadsetStandZone8";
                            break;
                        case 12:
                            corsairPadRegion = "Pad3";
                            corsairStandRegion = "HeadsetStandZone8";
                            break;
                        case 13:
                            corsairPadRegion = "Pad2";
                            corsairStandRegion = "HeadsetStandZone9";
                            break;
                        case 14:
                            corsairPadRegion = "Pad1";
                            corsairStandRegion = "HeadsetStandZone9";
                            break;
                    }

                    _corsair.ApplyMapPadLighting(corsairPadRegion, col, clear);
                    _corsair.ApplyMapStandLighting(corsairStandRegion, col, clear);
                }

                if (SteelSdkCalled == 1)
                {
                    //
                }
            }

            
        }

        //Send a ripple effect to a Keyboard that reverts back to the base colour once completed
        public void GlobalRipple1(Color burstcol, int speed, Color baseColor)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
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

            if (SteelSdkCalled == 1)
            {
                var rippleTask = _steel.Ripple1(burstcol, speed, baseColor);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (WootingSdkCalled == 1)
            {
                var rippleTask = _wooting.Ripple1(burstcol, speed, baseColor);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (AsusSdkCalled == 1)
            {
                var rippleTask = _asus.Ripple1(burstcol, speed, baseColor);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }
        }

        public void GlobalMultiRipple1(Color burstcol, int speed, Color baseColor)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
            {
                var rippleTask = _razer.MultiRipple1(burstcol, speed, _KeysMultiKeyModeEnabled, _deviceKeypad);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (LogitechSdkCalled == 1)
            {
                var rippleTask = _logitech.MultiRipple1(burstcol, speed);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (CorsairSdkCalled == 1)
            {
                //
            }

            if (CoolermasterSdkCalled == 1)
            {
                //
            }

            if (SteelSdkCalled == 1)
            {
                //
            }

            if (WootingSdkCalled == 1)
            {
                //
            }

            if (AsusSdkCalled == 1)
            {
                //
            }
        }

        //Send a ripple effect to a Keyboard that keeps its new colour
        public void GlobalRipple2(Color burstcol, int speed)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
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

            if (SteelSdkCalled == 1)
            {
                var rippleTask2 = _steel.Ripple2(burstcol, speed);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }

            if (WootingSdkCalled == 1)
            {
                var rippleTask2 = _wooting.Ripple2(burstcol, speed);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }

            if (AsusSdkCalled == 1)
            {
                var rippleTask2 = _asus.Ripple2(burstcol, speed);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }
        }

        public void GlobalMultiRipple2(Color burstcol, int speed)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled)
                return;

            if (RazerSdkCalled == 1)
            {
                var rippleTask2 = _razer.MultiRipple2(burstcol, speed, _KeysMultiKeyModeEnabled, _deviceKeypad);
                MemoryTasks.Add(rippleTask2);
                MemoryTasks.Run(rippleTask2);
            }

            if (LogitechSdkCalled == 1)
            {
                var rippleTask = _logitech.MultiRipple2(burstcol, speed);
                MemoryTasks.Add(rippleTask);
                MemoryTasks.Run(rippleTask);
            }

            if (CorsairSdkCalled == 1)
            {
                //
            }

            if (CoolermasterSdkCalled == 1)
            {
                //
            }

            if (SteelSdkCalled == 1)
            {
                //
            }

            if (WootingSdkCalled == 1)
            {
                //
            }

            if (AsusSdkCalled == 1)
            {
                //
            }
        }

        public void GlobalFlash1(Color burstcol, int speed, string[] regions)
        {
            MemoryTasks.Cleanup();
            
            if (RazerSdkCalled == 1)
            {
                _rzFlash = null;
                _rzFl1Cts = new CancellationTokenSource();

                _rzFlash = new Task(() =>
                {
                    HoldReader = true;
                    if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                    {
                        _razer.SingleFlash1(burstcol, speed, regions);
                    }
                    else
                    {
                        _razer.Flash1(burstcol, speed, regions);
                    }
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
                    if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                    {
                        _logitech.SingleFlash1(burstcol, speed, regions);
                    }
                    else
                    {
                        _logitech.Flash1(burstcol, speed, regions);
                    }
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

            if (SteelSdkCalled == 1)
            {
                _steelFlash = null;
                _steelFl1Cts = new CancellationTokenSource();

                _steelFlash = new Task(() =>
                {
                    HoldReader = true;
                    _steel.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _steelFl1Cts.Token);
                MemoryTasks.Add(_steelFlash);
                MemoryTasks.Run(_steelFlash);
            }

            if (WootingSdkCalled == 1)
            {
                _wootingFlash = null;
                _wootingFl1Cts = new CancellationTokenSource();

                _wootingFlash = new Task(() =>
                {
                    HoldReader = true;
                    _wooting.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _wootingFl1Cts.Token);
                MemoryTasks.Add(_wootingFlash);
                MemoryTasks.Run(_wootingFlash);
            }

            if (AsusSdkCalled == 1)
            {
                _asusFlash = null;
                _asusFl1Cts = new CancellationTokenSource();

                _asusFlash = new Task(() =>
                {
                    HoldReader = true;
                    _asus.Flash1(burstcol, speed, regions);
                    HoldReader = false;
                }, _asusFl1Cts.Token);
                MemoryTasks.Add(_asusFlash);
                MemoryTasks.Run(_asusFlash);
            }
        }

        public void GlobalFlash2(Color burstcol, int speed, string[] template)
        {
            MemoryTasks.Cleanup();
            
            if (!_globalFlash2Running)
            {
                if (RazerSdkCalled == 1)
                {
                    _rzFl2 = null;
                    _rzFl2Cts = new CancellationTokenSource();
                    _rzFl2 = new Task(() =>
                        {
                            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                            {
                                _razer.SingleFlash2(burstcol, speed, _rzFl2Cts.Token, template);
                            }
                            else
                            {
                                _razer.Flash2(burstcol, speed, _rzFl2Cts.Token, template);
                            }
                        },
                        _rzFl2Cts.Token);
                    MemoryTasks.Add(_rzFl2);
                    MemoryTasks.Run(_rzFl2);
                }

                if (LogitechSdkCalled == 1)
                {
                    _logiFl2 = null;
                    _logiFl2Cts = new CancellationTokenSource();
                    _logiFl2 = new Task(() =>
                        {
                            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                            {
                                _logitech.SingleFlash2(burstcol, speed, _logiFl2Cts.Token, template);
                            }
                            else
                            {
                                _logitech.Flash2(burstcol, speed, _logiFl2Cts.Token, template);
                            }
                            
                        },
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

                if (SteelSdkCalled == 1)
                {
                    _steelFl2 = null;
                    _steelFl2Cts = new CancellationTokenSource();
                    _steelFl2 =
                        new Task(() => { _steel.Flash2(burstcol, speed, _steelFl2Cts.Token, template); },
                            _steelFl2Cts.Token);
                    MemoryTasks.Add(_steelFl2);
                    MemoryTasks.Run(_steelFl2);
                }

                if (WootingSdkCalled == 1)
                {
                    _wootingFl2 = null;
                    _wootingFl2Cts = new CancellationTokenSource();
                    _wootingFl2 =
                        new Task(() => { _wooting.Flash2(burstcol, speed, _wootingFl2Cts.Token, template); },
                            _wootingFl2Cts.Token);
                    MemoryTasks.Add(_wootingFl2);
                    MemoryTasks.Run(_wootingFl2);
                }

                if (AsusSdkCalled == 1)
                {
                    _asusFl2 = null;
                    _asusFl2Cts = new CancellationTokenSource();
                    _asusFl2 =
                        new Task(() => { _asus.Flash2(burstcol, speed, _asusFl2Cts.Token, template); },
                            _asusFl2Cts.Token);
                    MemoryTasks.Add(_asusFl2);
                    MemoryTasks.Run(_asusFl2);
                }

                _globalFlash2Running = true;
            }
        }

        public void ToggleGlobalFlash2(bool toggle)
        {
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

                if (SteelSdkCalled == 1)
                {
                    _steelFl2Cts.Cancel();
                    MemoryTasks.Remove(_steelFl2);
                }

                if (WootingSdkCalled == 1)
                {
                    _wootingFl2Cts.Cancel();
                    MemoryTasks.Remove(_wootingFl2);
                }

                if (AsusSdkCalled == 1)
                {
                    _asusFl2Cts.Cancel();
                    MemoryTasks.Remove(_asusFl2);
                }

                //Debug.WriteLine("Stopping Flash 2");

                MemoryTasks.Cleanup();
            }

            MemoryTasks.Cleanup();
        }

        public void GlobalFlash3(Color burstcol, int speed)
        {
            MemoryTasks.Cleanup();

            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
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

                if (SteelSdkCalled == 1)
                {
                    _steelFl3 = null;
                    _steelFl3Cts = new CancellationTokenSource();
                    _steelFl3 =
                        new Task(() => { _steel.Flash3(burstcol, speed, _steelFl3Cts.Token); },
                            _steelFl3Cts.Token);
                    MemoryTasks.Add(_steelFl3);
                    MemoryTasks.Run(_steelFl3);
                }

                if (WootingSdkCalled == 1)
                {
                    _wootingFl3 = null;
                    _wootingFl3Cts = new CancellationTokenSource();
                    _wootingFl3 =
                        new Task(() => { _wooting.Flash3(burstcol, speed, _wootingFl3Cts.Token); },
                            _steelFl3Cts.Token);
                    MemoryTasks.Add(_wootingFl3);
                    MemoryTasks.Run(_wootingFl3);
                }

                if (AsusSdkCalled == 1)
                {
                    _asusFl3 = null;
                    _asusFl3Cts = new CancellationTokenSource();
                    _asusFl3 =
                        new Task(() => { _asus.Flash3(burstcol, speed, _asusFl3Cts.Token); },
                            _steelFl3Cts.Token);
                    MemoryTasks.Add(_asusFl3);
                    MemoryTasks.Run(_asusFl3);
                }

                _globalFlash3Running = true;
            }
        }

        public void ToggleGlobalFlash3(bool toggle)
        {
            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
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
                    _logiFl3Cts.Cancel();
                    MemoryTasks.Remove(_logiFl3);
                }

                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterFl3Cts.Cancel();
                    MemoryTasks.Remove(_coolermasterFl3);
                }

                if (SteelSdkCalled == 1)
                {
                    _steelFl3Cts.Cancel();
                    MemoryTasks.Remove(_steelFl3);
                }

                if (WootingSdkCalled == 1)
                {
                    _wootingFl3Cts.Cancel();
                    MemoryTasks.Remove(_wootingFl3);
                }

                if (AsusSdkCalled == 1)
                {
                    _asusFl3Cts.Cancel();
                    MemoryTasks.Remove(_asusFl3);
                }

                //Debug.WriteLine("Stopping Flash 3");

                MemoryTasks.Cleanup();
            }

            MemoryTasks.Cleanup();
        }

        public void GlobalFlash4(Color basecol, Color burstcol, int speed, string[] template)
        {
            MemoryTasks.Cleanup();
            
            if (!_globalFlash4Running)
            {
                if (ChromaticsSettings.ChromaticsSettingsDfBellToggle)
                {
                    if (RazerSdkCalled == 1)
                    {
                        _rzFl4 = null;
                        _rzFl4Cts = new CancellationTokenSource();
                        _rzFl4 = new Task(() =>
                            {
                                if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                                {
                                    _razer.SingleFlash4(burstcol, speed, _rzFl4Cts.Token, template);
                                }
                                else
                                {
                                    _razer.Flash4(burstcol, speed, _rzFl4Cts.Token, template);
                                }
                            },
                            _rzFl4Cts.Token);
                        MemoryTasks.Add(_rzFl4);
                        MemoryTasks.Run(_rzFl4);
                    }

                    if (LogitechSdkCalled == 1)
                    {
                        _logiFl4 = null;
                        _logiFl4Cts = new CancellationTokenSource();
                        _logiFl4 = new Task(() =>
                            {
                                if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                                {
                                    _logitech.SingleFlash4(burstcol, speed, _logiFl4Cts.Token, template);
                                }
                                else
                                {
                                    _logitech.Flash4(burstcol, speed, _logiFl4Cts.Token, template);
                                }
                                
                            },
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

                    if (SteelSdkCalled == 1)
                    {
                        _steelFl4 = null;
                        _steelFl4Cts = new CancellationTokenSource();
                        _steelFl4 =
                            new Task(
                                () => { _steel.Flash4(burstcol, speed, _steelFl4Cts.Token, template); },
                                _steelFl4Cts.Token);
                        MemoryTasks.Add(_steelFl4);
                        MemoryTasks.Run(_steelFl4);
                    }

                    if (WootingSdkCalled == 1)
                    {
                        _wootingFl4 = null;
                        _wootingFl4Cts = new CancellationTokenSource();
                        _wootingFl4 =
                            new Task(
                                () => { _wooting.Flash4(burstcol, speed, _wootingFl4Cts.Token, template); },
                                _wootingFl4Cts.Token);
                        MemoryTasks.Add(_wootingFl4);
                        MemoryTasks.Run(_wootingFl4);
                    }

                    if (AsusSdkCalled == 1)
                    {
                        _asusFl4 = null;
                        _asusFl4Cts = new CancellationTokenSource();
                        _asusFl4 =
                            new Task(
                                () => { _asus.Flash4(burstcol, speed, _asusFl4Cts.Token, template); },
                                _asusFl4Cts.Token);
                        MemoryTasks.Add(_asusFl4);
                        MemoryTasks.Run(_asusFl4);
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

                    if (SteelSdkCalled == 1)
                    {
                        _steelFl4Cts.Cancel();
                        MemoryTasks.Remove(_steelFl4);
                    }

                    if (WootingSdkCalled == 1)
                    {
                        _wootingFl4Cts.Cancel();
                        MemoryTasks.Remove(_wootingFl4);
                    }

                    if (AsusSdkCalled == 1)
                    {
                        _asusFl4Cts.Cancel();
                        MemoryTasks.Remove(_asusFl4);
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

                Console.WriteLine(@"Stopping Flash 4 (A)");

                MemoryTasks.Cleanup();
            }

            MemoryTasks.Cleanup();
        }

        public void GlobalParticleEffects(Color[] toColor, string[] regions, uint interval = 20, int speed = 50)
        {
            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                return;

            if (regions == null)
            {
                regions = DeviceEffects.GlobalKeysAll;
            }

            MemoryTasks.Cleanup();

            if (RazerSdkCalled == 1)
            {
                _rzPart = null;
                _rzPartCts = new CancellationTokenSource();
                _rzPart = new Task(() =>
                {
                    _razer.ParticleEffect(toColor, regions, interval, _rzPartCts, speed);
                }, _rzPartCts.Token);

                MemoryTasks.Add(_rzPart);
                MemoryTasks.Run(_rzPart);
            }

            if (LogitechSdkCalled == 1)
            {
                _logiPart = null;
                _logiPartCts = new CancellationTokenSource();
                _logiPart = new Task(() =>
                {
                    _logitech.ParticleEffect(toColor, regions, interval, _logiPartCts, speed);
                }, _logiPartCts.Token);

                MemoryTasks.Add(_logiPart);
                MemoryTasks.Run(_logiPart);
            }
            
            if (CorsairSdkCalled == 1)
            {
                Thread.Sleep(100);
                GlobalApplyAllKeyLighting(toColor[0]);
                _corsairPart = null;
                _corsairPartCts = new CancellationTokenSource();
                _corsairPart = new Task(() =>
                {
                    _corsair.ParticleEffect(toColor, regions, interval, _corsairPartCts, speed);
                }, _corsairPartCts.Token);

                MemoryTasks.Add(_corsairPart);
                MemoryTasks.Run(_corsairPart);
            }
            
            if (CoolermasterSdkCalled == 1)
            {
                _coolermasterPart = null;
                _coolermasterPartCts = new CancellationTokenSource();
                _coolermasterPart = new Task(() =>
                {
                    _coolermaster.ParticleEffect(toColor, regions, interval, _coolermasterPartCts, speed);
                }, _coolermasterPartCts.Token);

                MemoryTasks.Add(_coolermasterPart);
                MemoryTasks.Run(_coolermasterPart);
            }

            if (SteelSdkCalled == 1)
            {
                _steelPart = null;
                _steelPartCts = new CancellationTokenSource();
                _steelPart = new Task(() =>
                {
                    _steel.ParticleEffect(toColor, regions, interval, _steelPartCts, speed);
                }, _steelPartCts.Token);

                MemoryTasks.Add(_steelPart);
                MemoryTasks.Run(_steelPart);
            }

            if (WootingSdkCalled == 1)
            {
                _wootingPart = null;
                _wootingPartCts = new CancellationTokenSource();
                _wootingPart = new Task(() =>
                {
                    _wooting.ParticleEffect(toColor, regions, interval, _wootingPartCts, speed);
                }, _wootingPartCts.Token);

                MemoryTasks.Add(_wootingPart);
                MemoryTasks.Run(_wootingPart);
            }

            if (AsusSdkCalled == 1)
            {
                _asusPart = null;
                _asusPartCts = new CancellationTokenSource();
                _asusPart = new Task(() =>
                {
                    _asus.ParticleEffect(toColor, regions, interval, _asusPartCts, speed);
                }, _asusPartCts.Token);

                MemoryTasks.Add(_asusPart);
                MemoryTasks.Run(_asusPart);
            }

            _globalParticleRunning = true;
        }

        public void GlobalStopParticleEffects()
        {
            if (!_globalParticleRunning) return;

            if (RazerSdkCalled == 1)
            {
                _rzPartCts.Cancel();
                MemoryTasks.Remove(_rzPart);
            }

            if (LogitechSdkCalled == 1)
            {
                _logiPartCts.Cancel();
                MemoryTasks.Remove(_logiPart);
            }

            if (CorsairSdkCalled == 1)
            {
                _corsairPartCts.Cancel();
                MemoryTasks.Remove(_corsairPart);
            }

            if (CoolermasterSdkCalled == 1)
            {
                _coolermasterPartCts.Cancel();
                MemoryTasks.Remove(_coolermasterPart);
            }

            if (SteelSdkCalled == 1)
            {
                _steelPartCts.Cancel();
                MemoryTasks.Remove(_steelPart);
            }

            if (WootingSdkCalled == 1)
            {
                _wootingPartCts.Cancel();
                MemoryTasks.Remove(_wootingPart);
            }

            if (AsusSdkCalled == 1)
            {
                _asusPartCts.Cancel();
                MemoryTasks.Remove(_asusPart);
            }

        }

        public void GlobalColorCycle(int interval = 100)
        {
            //if (!_KeysSingleKeyModeEnabled && !_KeysMultiKeyModeEnabled) return;

            MemoryTasks.Cleanup();

            _rzCycle = null;
            _rzCycleCts = new CancellationTokenSource();
            _rzCycle = new Task(() => { GlobalStepCycleEffect(interval, _rzCycleCts); }, _rzCycleCts.Token);

            MemoryTasks.Add(_rzCycle);
            MemoryTasks.Run(_rzCycle);

            /*
                if (RazerSdkCalled == 1)
                {
                    _rzCycle = null;
                    _rzCycleCts = new CancellationTokenSource();
                    _rzCycle = new Task(() =>
                    {
                        //_razer.CycleEffect(interval, _rzCycleCts);
                    }, _rzCycleCts.Token);
    
                    MemoryTasks.Add(_rzCycle);
                    MemoryTasks.Run(_rzCycle);
                }
    
                if (LogitechSdkCalled == 1)
                {
                    _logiCycle = null;
                    _logiCycleCts = new CancellationTokenSource();
                    _logiCycle = new Task(() =>
                    {
                        _logitech.CycleEffect(interval, _logiCycleCts);
                    }, _logiCycleCts.Token);
    
                    MemoryTasks.Add(_logiCycle);
                    MemoryTasks.Run(_logiCycle);
                }
    
                if (CorsairSdkCalled == 1)
                {
                    _corsairCycle = null;
                    _corsairCycleCts = new CancellationTokenSource();
                    _corsairCycle = new Task(() =>
                    {
                        _corsair.CycleEffect(interval, _corsairCycleCts);
                    }, _corsairCycleCts.Token);
    
                    MemoryTasks.Add(_corsairCycle);
                    MemoryTasks.Run(_corsairCycle);
                }
    
                if (CoolermasterSdkCalled == 1)
                {
                    _coolermasterCycle = null;
                    _coolermasterCycleCts = new CancellationTokenSource();
                    _coolermasterCycle = new Task(() =>
                    {
                        _coolermaster.CycleEffect(interval, _coolermasterCycleCts);
                    }, _coolermasterCycleCts.Token);
    
                    MemoryTasks.Add(_coolermasterCycle);
                    MemoryTasks.Run(_coolermasterCycle);
                }
    
                if (SteelSdkCalled == 1)
                {
                    _steelCycle = null;
                    _steelCycleCts = new CancellationTokenSource();
                    _steelCycle = new Task(() =>
                    {
                        _steel.CycleEffect(interval, _steelCycleCts);
                    }, _steelCycleCts.Token);
    
                    MemoryTasks.Add(_steelCycle);
                    MemoryTasks.Run(_steelCycle);
                }
    
                if (WootingSdkCalled == 1)
                {
                    _wootingCycle = null;
                    _wootingCycleCts = new CancellationTokenSource();
                    _wootingCycle = new Task(() =>
                    {
                        _wooting.CycleEffect(interval, _wootingCycleCts);
                    }, _wootingCycleCts.Token);
    
                    MemoryTasks.Add(_wootingCycle);
                    MemoryTasks.Run(_wootingCycle);
                }
                */
            _globalCycleRunning = true;
        }

        public void GlobalStopCycleEffects()
        {
            if (!_globalCycleRunning) return;

            _rzCycleCts.Cancel();
            MemoryTasks.Remove(_rzCycle);

            /*
            if (RazerSdkCalled == 1)
            {
                _rzCycleCts.Cancel();
                MemoryTasks.Remove(_rzCycle);
            }

            if (LogitechSdkCalled == 1)
            {
                _logiCycleCts.Cancel();
                MemoryTasks.Remove(_logiCycle);
            }

            if (CorsairSdkCalled == 1)
            {
                _corsairCycleCts.Cancel();
                MemoryTasks.Remove(_corsairCycle);
            }

            if (CoolermasterSdkCalled == 1)
            {
                _coolermasterCycleCts.Cancel();
                MemoryTasks.Remove(_coolermasterCycle);
            }

            if (SteelSdkCalled == 1)
            {
                _steelCycleCts.Cancel();
                MemoryTasks.Remove(_steelCycle);
            }

            if (WootingSdkCalled == 1)
            {
                _wootingCycleCts.Cancel();
                MemoryTasks.Remove(_wootingCycle);
            }
            */
        }

        private readonly object lockObject = new object();
        private readonly object lockCycle = new object();
        public void GlobalStepCycleEffect(int interval, CancellationTokenSource token)
        {
            //if (!_KeysSingleKeyModeEnabled && !_KeysMultiKeyModeEnabled) return;

            while (true)
            {
                lock (lockObject)
                {
                    var _lifxCycleStep = new int[6] {0, 0, 0, 0, 0, 0};

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;

                        lock (lockCycle)
                        {
                            Thread.Sleep(10);

                            var col = Color.FromArgb((int) Math.Ceiling((double) (250 * 100) / 255),
                                (int) Math.Ceiling((double) (x * 100) / 255), 0);

                            if (_KeysSingleKeyModeEnabled)
                                GlobalApplySingleZoneLighting(col);

                            if (_KeysMultiKeyModeEnabled)
                                GlobalApplyMultiZoneLighting(col, "All");

                            if (LifxSdkCalled == 1 && _lifxCycleStep[0] != 1)
                            {
                                _lifx.LifxUpdateState(BulbModeTypes.DefaultColor, col, interval * 2);
                                _lifx.LifxUpdateState(BulbModeTypes.HighlightColor, col, interval * 2);
                                _lifxCycleStep[0] = 1;
                            }
                        }

                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;

                        lock (lockCycle)
                        {
                            Thread.Sleep(10);

                            Color col = Color.FromArgb((int) Math.Ceiling((double) (x * 100) / 255),
                                (int) Math.Ceiling((double) (250 * 100) / 255), 0);

                            if (_KeysSingleKeyModeEnabled)
                                GlobalApplySingleZoneLighting(col);

                            if (_KeysMultiKeyModeEnabled)
                                GlobalApplyMultiZoneLighting(col, "All");

                            if (LifxSdkCalled == 1 && _lifxCycleStep[1] != 1)
                            {
                                _lifx.LifxUpdateState(BulbModeTypes.DefaultColor, col, interval * 2);
                                _lifx.LifxUpdateState(BulbModeTypes.HighlightColor, col, interval * 2);
                                _lifxCycleStep[1] = 1;
                            }
                        }
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;

                        lock (lockCycle)
                        {
                            Thread.Sleep(10);

                            Color col = Color.FromArgb((int) Math.Ceiling((double) (x * 100) / 255),
                                (int) Math.Ceiling((double) (250 * 100) / 255), 0);

                            if (_KeysSingleKeyModeEnabled)
                                GlobalApplySingleZoneLighting(col);

                            if (_KeysMultiKeyModeEnabled)
                                GlobalApplyMultiZoneLighting(col, "All");

                            if (LifxSdkCalled == 1 && _lifxCycleStep[2] != 1)
                            {
                                _lifx.LifxUpdateState(BulbModeTypes.DefaultColor, col, interval * 2);
                                _lifx.LifxUpdateState(BulbModeTypes.HighlightColor, col, interval * 2);
                                _lifxCycleStep[2] = 1;
                            }
                        }
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;

                        lock (lockCycle)
                        {
                            Thread.Sleep(10);

                            Color col = Color.FromArgb(0, (int) Math.Ceiling((double) (x * 100) / 255),
                                (int) Math.Ceiling((double) (250 * 100) / 255));

                            if (_KeysSingleKeyModeEnabled)
                                GlobalApplySingleZoneLighting(col);

                            if (_KeysMultiKeyModeEnabled)
                                GlobalApplyMultiZoneLighting(col, "All");

                            if (LifxSdkCalled == 1 && _lifxCycleStep[3] != 1)
                            {
                                _lifx.LifxUpdateState(BulbModeTypes.DefaultColor, col, interval * 2);
                                _lifx.LifxUpdateState(BulbModeTypes.HighlightColor, col, interval * 2);
                                _lifxCycleStep[3] = 1;
                            }
                        }
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;

                        lock (lockCycle)
                        {
                            Thread.Sleep(10);

                            Color col = Color.FromArgb((int) Math.Ceiling((double) (x * 100) / 255), 0,
                                (int) Math.Ceiling((double) (250 * 100) / 255));

                            if (_KeysSingleKeyModeEnabled)
                                GlobalApplySingleZoneLighting(col);

                            if (_KeysMultiKeyModeEnabled)
                                GlobalApplyMultiZoneLighting(col, "All");

                            if (LifxSdkCalled == 1 && _lifxCycleStep[4] != 1)
                            {
                                _lifx.LifxUpdateState(BulbModeTypes.DefaultColor, col, interval * 2);
                                _lifx.LifxUpdateState(BulbModeTypes.HighlightColor, col, interval * 2);
                                _lifxCycleStep[4] = 1;
                            }
                        }
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;

                        lock (lockCycle)
                        {
                            Thread.Sleep(10);

                            Color col = Color.FromArgb((int) Math.Ceiling((double) (250 * 100) / 255), 0,
                                (int) Math.Ceiling((double) (x * 100) / 255));

                            if (_KeysSingleKeyModeEnabled)
                                GlobalApplySingleZoneLighting(col);

                            if (_KeysMultiKeyModeEnabled)
                                GlobalApplyMultiZoneLighting(col, "All");

                            if (LifxSdkCalled == 1 && _lifxCycleStep[5] != 1)
                            {
                                _lifx.LifxUpdateState(BulbModeTypes.DefaultColor, col, interval * 2);
                                _lifx.LifxUpdateState(BulbModeTypes.HighlightColor, col, interval * 2);
                                _lifxCycleStep[5] = 1;
                            }
                        }
                    }

                    if (token.IsCancellationRequested) break;
                }
            }
            Thread.Sleep(interval);
        }

        public void GlobalFadeAllLights(Color toColor, Color fromColor, uint interval = 20)
        {
            if (_KeysSingleKeyModeEnabled || _KeysMultiKeyModeEnabled)
                return;

            MemoryTasks.Cleanup();

            if (RazerSdkCalled == 1)
            {
                _razer.FadeColourAll(toColor, fromColor, interval);
            }
        }
    }
}