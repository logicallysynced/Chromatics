/*
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
using AuraServiceLib;

/* Contains all Asus SDK code for detection, initialization, states and effects.
 * 
 * 

namespace Chromatics.DeviceInterfaces
{
    public class AsusInterface
    {
        private static ILogWrite _write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static Asus InitializeAsusSdk()
        {
            Asus Asus = null;
            if (Process.GetProcessesByName("LCore").Length > 0 || Process.GetProcessesByName("lghub").Length > 0)
            {
                Asus = new Asus();
                var result = Asus.InitializeLights();
                if (!result)
                    return null;
            }
            return Asus;
        }
    }

    public class AsusSdkWrapper
    {
        //LED SDK

        public enum DeviceTypes
        {
            All = 0x00000000,
            Motherboard = 0x00010000,
            MotherboardLED = 0x00011000,
            AIO = 0x00012000,
            VGA = 0x00020000,
            Display = 0x00030000,
            Headset = 0x00040000,
            Microphone = 0x00050000,
            HDD = 0x00060000,
            BDD = 0x00061000,
            DRAM = 0x00070000,
            Keyboard = 0x00080000,
            Notebook = 0x00081000,
            NotebookZones = 0x00081001,
            Mouse = 0x00090000,
            Chassis = 0x000B0000,
            Projector = 0x000C0000
        }
    }

    public interface IAsusSdk
    {
        bool InitializeLights();

        void ShutdownSdk();
        void SetLights(Color color);
        void StopEffects();
        void SetWave(Color col);
        void ColorCycle(Color color, CancellationToken token);
        void Pulse(Color color, int milliSecondsDuration, int milliSecondsInterval);
        void ResetAsusDevices(bool AsusDeviceKeyboard, Color basecol);
        void SetAllLights(Color col);
        void ApplyMapSingleLighting(Color col);
        void ApplyMapMultiLighting(Color col, string region);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapMouseLighting(string key, Color col);
        void ApplyMapHeadsetLighting(string key, Color col);
        void ApplyMapPadLighting(string key, Color col);
        void ApplyMapPadSpeakers(string key, Color col);

        void UpdateState(string type, Color col, bool disablekeys,
            [Optional] Color col2, [Optional] bool direction, [Optional] int speed);

        Task Ripple1(Color burstcol, int speed, Color baseColor);
        Task Ripple2(Color burstcol, int speed);
        Task MultiRipple1(Color burstcol, int speed);
        Task MultiRipple2(Color burstcol, int speed);
        void Flash1(Color burstcol, int speed, string[] regions);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
        void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void SingleFlash1(Color burstcol, int speed, string[] regions);
        void SingleFlash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void SingleFlash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50);
        void CycleEffect(int interval, CancellationTokenSource token);
        Color GetCurrentKeyColor(string key);
        Color GetCurrentMouseColor(string region);
        Color GetCurrentPadColor(string region);
    }

    public class Asus : IAsusSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private IAuraSdk sdk;

        private static int _AsusFlash2Step;
        private static bool _AsusFlash2Running;
        private static readonly object _Flash2 = new object();

        private static int _AsusFlash3Step;
        private static bool _AsusFlash3Running;
        private static readonly object _Flash3 = new object();

        private static int _AsusFlash4Step;
        private static bool _AsusFlash4Running;
        private static readonly object _Flash4 = new object();

        private CancellationTokenSource _cancellationTokenSource;

        private bool _AsusDeviceKeyboard = true;
        private bool _AsusDeviceMouse = true;
        private bool _AsusDeviceHeadset = true;
        private bool _AsusDeviceMousepad = true;
        private bool _AsusDeviceSpeakers = true;

        private static Dictionary<KeyboardNames, Color> keyMappings = new Dictionary<KeyboardNames, Color>();
        
        private static Dictionary<string, Color> mouseMappings = new Dictionary<string, Color>
        {
            {"0", Color.Black },
            {"1", Color.Black },
            {"2", Color.Black },
            {"3", Color.Black },
        };

        private static Dictionary<string, Color> padMappings = new Dictionary<string, Color>
        {
            {"0", Color.Black },
            {"1", Color.Black },
            {"2", Color.Black },
            {"3", Color.Black },
        };

        public void ShutdownSdk()
        {
            try
            {
                AsusSdkWrapper.AsusLedShutdown();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        public void ApplyMapKeyLighting(string key, Color color, bool clear, [Optional] bool bypasswhitelist)
        {
            if (!_AsusDeviceKeyboard)
                return;

            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;

            //
        }

        public void SetAllLights(Color color)
        {
            AsusSdkWrapper.AsusLedSetLighting((int) Math.Ceiling((double) (color.R * 100) / 255),
                (int) Math.Ceiling((double) (color.G * 100) / 255),
                (int) Math.Ceiling((double) (color.B * 100) / 255));

        }

        public void ApplyMapSingleLighting(Color color)
        {
            if (!_AsusDeviceKeyboard)
                return;

            AsusSdkWrapper.AsusLedSetTargetDevice(AsusSdkWrapper.AsusDevicetypeRgb | AsusSdkWrapper.AsusDevicetypePerkeyRgb);

            AsusSdkWrapper.AsusLedSetLighting((int)Math.Ceiling((double)(color.R * 100) / 255),
                (int)Math.Ceiling((double)(color.G * 100) / 255),
                (int)Math.Ceiling((double)(color.B * 100) / 255));

        }

        public void ApplyMapMultiLighting(Color color, string region)
        {
            if (!_AsusDeviceKeyboard)
                return;

            AsusSdkWrapper.AsusLedSetTargetDevice(AsusSdkWrapper.AsusDevicetypeRgb);

            switch (region)
            {
                case "All":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 0, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 1, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 2, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 3, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 4, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 5, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 6, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "0":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 0, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "1":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 1, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "2":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 2, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "3":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 3, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "4":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Keyboard, 4, (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
            }
        }

        public Color GetCurrentKeyColor(string key)
        {
            if (!_AsusDeviceKeyboard)
                return Color.Black;

            try
            {
                KeyboardNames keyName;
                if (Enum.TryParse(key, out keyName))
                {
                    if (keyMappings.ContainsKey(keyName))
                    {
                        return keyMappings[keyName];
                    }

                    return Color.Black;
                }

                return Color.Black;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, @"(" + key + "): " + ex.Message);
                return Color.Black;
            }
        }

        public void ApplyMapMouseLighting(string key, Color color)
        {
            if (!_AsusDeviceMouse)
                return;

            AsusSdkWrapper.AsusLedSetTargetDevice(AsusSdkWrapper.AsusDevicetypeAll);

            switch (key)
            {
                case "0":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Mouse, 0,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "1":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Mouse, 1,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "2":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Mouse, 2,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
            }
        }

        public Color GetCurrentMouseColor(string region)
        {
            if (!_AsusDeviceMouse)
                return Color.Black;

            if (mouseMappings.ContainsKey(region))
            {
                return mouseMappings[region];
            }

            return Color.Black;
        }

        public void ApplyMapHeadsetLighting(string key, Color color)
        {
            if (!_AsusDeviceHeadset)
                return;

            AsusSdkWrapper.AsusLedSetTargetDevice(AsusSdkWrapper.AsusDevicetypeAll);

            switch (key)
            {
                case "0":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Headset, 0,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "1":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Headset, 1,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "2":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Headset, 2,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
            }
        }

        public void ApplyMapPadLighting(string key, Color color)
        {
            if (!_AsusDeviceMousepad)
                return;

            AsusSdkWrapper.AsusLedSetTargetDevice(AsusSdkWrapper.AsusDevicetypeAll);

            switch (key)
            {
                case "0":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Mousemat, 0,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "1":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Mousemat, 1,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "2":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Mousemat, 2,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
            }
        }

        public Color GetCurrentPadColor(string region)
        {
            if (!_AsusDeviceMousepad)
                return Color.Black;

            if (padMappings.ContainsKey(region))
            {
                return padMappings[region];
            }

            return Color.Black;
        }

        public void ApplyMapPadSpeakers(string key, Color color)
        {
            if (!_AsusDeviceSpeakers)
                return;

            AsusSdkWrapper.AsusLedSetTargetDevice(AsusSdkWrapper.AsusDevicetypeAll);

            switch (key)
            {
                case "0":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Speaker, 0,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "1":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Speaker, 1,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "2":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Speaker, 2,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
                case "3":
                    AsusSdkWrapper.AsusLedSetLightingForTargetZone(DeviceType.Speaker, 3,
                        (int)Math.Ceiling((double)(color.R * 100) / 255),
                        (int)Math.Ceiling((double)(color.G * 100) / 255),
                        (int)Math.Ceiling((double)(color.B * 100) / 255));
                    break;
            }
        }

        public void ResetAsusDevices(bool deviceKeyboard, Color basecol)
        {
            if (_AsusDeviceKeyboard && !deviceKeyboard)
                SetLights(basecol);

            _AsusDeviceKeyboard = deviceKeyboard;

            
        }

        public void ColorCycle(Color color, CancellationToken token)
        {
            if (_AsusDeviceKeyboard)
                AsusSdkWrapper.AsusColourCycle(color, new object(), token);
        }

        public bool InitializeLights()
        {
            var result = true;
            try
            {
                sdk = new AuraSdk();
                
                if (sdk == null)
                {
                    return false;
                }

                sdk.SwitchMode();
                var devices = sdk.Enumerate(0);
                
                foreach (IAuraSyncDevice dev in devices)
                {
                    if (dev.Type == 0x00080000 || dev.Type == 0x00081000 || dev.Type == 0x00081001) //Keyboard
                    {
                        _AsusDeviceKeyboard = true;
                    }

                    if (dev.Type == 0x00090000) //Mouse
                    {
                        _AsusDeviceMouse = true;
                    }

                    if (dev.Type == 0x00040000) //Headset
                    {
                        _AsusDeviceHeadset = true;
                    }
                }

            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public void Pulse(Color color, int milliSecondsDuration, int milliSecondsInterval)
        {
            if (_AsusDeviceKeyboard)
                AsusSdkWrapper.AsusLedPulseLighting((int) Math.Ceiling((double) (color.R * 100) / 255),
                    (int) Math.Ceiling((double) (color.G * 100) / 255),
                    (int) Math.Ceiling((double) (color.B * 100) / 255),
                    AsusSdkWrapper.AsusLedDurationInfinite,
                    60);
        }

        public void SetLights(Color color)
        {
            if (_AsusDeviceKeyboard)
                AsusSdkWrapper.AsusLedSetLighting((int) Math.Ceiling((double) (color.R * 100) / 255),
                    (int) Math.Ceiling((double) (color.G * 100) / 255),
                    (int) Math.Ceiling((double) (color.B * 100) / 255));

        }

        public void StopEffects()
        {
            AsusSdkWrapper.AsusLedStopEffects();
            MemoryTasks.Cleanup();
            _cancellationTokenSource?.Cancel();
        }

        public void SetWave(Color col)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var rzSt = new Task(() =>
            {
                StopEffects();
                Thread.Sleep(100);

                ColorCycle(col, _cancellationTokenSource.Token);
            }, _cancellationTokenSource.Token);
            MemoryTasks.Add(rzSt);
            MemoryTasks.Run(rzSt);
        }

        public void UpdateState(string type, Color col, bool disablekeys,
            [Optional] Color col2, [Optional] bool direction, [Optional] int speed)
        {
            if (!_AsusDeviceKeyboard)
                return;


            MemoryTasks.Cleanup();
            _cancellationTokenSource?.Cancel();

            if (type == "reset")
            {
                StopEffects();
                Thread.Sleep(100);

                SetLights(Color.FromArgb(0, 0, 0));
            }
            else if (type == "static")
            {
                var rzSt = new Task(() =>
                {
                    StopEffects();
                    Thread.Sleep(100);

                    SetLights(col);
                });
                MemoryTasks.Add(rzSt);
                MemoryTasks.Run(rzSt);
            }
            else if (type == "transition")
            {
                var rzSt = new Task(() =>
                {
                    StopEffects();
                    Thread.Sleep(100);

                    SetLights(col);
                });
                MemoryTasks.Add(rzSt);
                MemoryTasks.Run(rzSt);
            }
            else if (type == "wave")
            {
                _cancellationTokenSource = new CancellationTokenSource();

                var rzSt = new Task(() =>
                {
                    StopEffects();
                    Thread.Sleep(100);

                    ColorCycle(col, _cancellationTokenSource.Token);
                }, _cancellationTokenSource.Token);
                MemoryTasks.Add(rzSt);
                MemoryTasks.Run(rzSt);
            }
            else if (type == "breath")
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var rzSt = new Task(() =>
                {
                    //TODO: Does this need to be canceled since it is infinite?
                    //Roxas: I'm not using the breath effect currently so as I don't know what purpose it will serve, having a cancellation token is probably a good idea.
                    Pulse(col, AsusSdkWrapper.AsusLedDurationInfinite, 60);
                }, _cancellationTokenSource.Token);
                MemoryTasks.Add(rzSt);
                MemoryTasks.Run(rzSt);
            }
            else if (type == "pulse")
            {
                var rzSt = new Task(() =>
                {
                    StopEffects();
                    Thread.Sleep(100);

                    SetLights(col);
                });
                MemoryTasks.Add(rzSt);
                MemoryTasks.Run(rzSt);
            }

            MemoryTasks.Cleanup();
        }

        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                if (!_AsusDeviceKeyboard)
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
                                    AsusSdkWrapper.AsusLedSaveLightingForKey(keyName);
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
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
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
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
                        }
                    }
                    else if (i == 9)
                    {
                        //Spin down

                        foreach (var key in DeviceEffects.GlobalKeys)
                            AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));

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
                if (!_AsusDeviceKeyboard) return;
                var presetsA = new Dictionary<int, Color>();
                var zones = 5;
                AsusSdkWrapper.AsusLedSetTargetDevice(AsusSdkWrapper.AsusDevicetypeRgb);

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        AsusSdkWrapper.AsusLedSaveCurrentLighting();

                        continue;
                    }

                    if (i == 1 || i == 3 || i == 5 || i == 7)
                    {
                        for (int x1 = 0; x1 < zones; x1++)
                        {
                            //ApplyMapMultiLighting(burstcol, x1.ToString()); //preset
                            AsusSdkWrapper.AsusLedRestoreLighting();
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
                        AsusSdkWrapper.AsusLedRestoreLighting();
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
                if (!_AsusDeviceKeyboard)
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
                if (!_AsusDeviceKeyboard) return;
                var presetsA = new Dictionary<int, Color>();
                var zones = 5;
                AsusSdkWrapper.AsusLedSetTargetDevice(AsusSdkWrapper.AsusDevicetypeRgb);

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        AsusSdkWrapper.AsusLedSaveCurrentLighting();

                        continue;
                    }

                    if (i == 1 || i == 3 || i == 5 || i == 7)
                    {
                        for (int x1 = 0; x1 < zones; x1++)
                        {
                            //ApplyMapMultiLighting(burstcol, x1.ToString()); //preset
                            AsusSdkWrapper.AsusLedRestoreLighting();
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
                        //AsusSdkWrapper.AsusLedRestoreLighting();
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
            if (!_AsusDeviceKeyboard)
                return;

            for (var i = 0; i <= 8; i++)
            {
                if (i == 0)
                    foreach (var key in regions)
                        AsusSdkWrapper.AsusLedSaveLightingForKey(ToKeyboardNames(key));
                else if (i == 1)
                    foreach (var key in regions)
                        ApplyMapKeyLighting(key, burstcol, false);
                else if (i == 2)
                    foreach (var key in regions)
                        AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
                else if (i == 3)
                    foreach (var key in regions)
                        ApplyMapKeyLighting(key, burstcol, false);
                else if (i == 4)
                    foreach (var key in regions)
                        AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
                else if (i == 5)
                    foreach (var key in regions)
                        ApplyMapKeyLighting(key, burstcol, false);
                else if (i == 6)
                    foreach (var key in regions)
                        AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));
                else if (i == 7)
                    foreach (var key in regions)
                        ApplyMapKeyLighting(key, burstcol, false);
                else if (i == 8)
                    foreach (var key in regions)
                        AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));

                if (i < 8)
                    Thread.Sleep(speed);
            }
        }

        public void SingleFlash1(Color burstcol, int speed, string[] regions)
        {
            if (!_AsusDeviceKeyboard)
                return;

            for (var i = 0; i <= 8; i++)
            {
                if (i == 0)
                    AsusSdkWrapper.AsusLedSaveCurrentLighting();

                else if (i == 1)
                    ApplyMapSingleLighting(burstcol);

                else if (i == 2)
                    AsusSdkWrapper.AsusLedRestoreLighting();

                else if (i == 3)
                    ApplyMapSingleLighting(burstcol);

                else if (i == 4)
                    AsusSdkWrapper.AsusLedRestoreLighting();

                else if (i == 5)
                    ApplyMapSingleLighting(burstcol);

                else if (i == 6)
                    AsusSdkWrapper.AsusLedRestoreLighting();

                else if (i == 7)
                    ApplyMapSingleLighting(burstcol);

                else if (i == 8)
                    AsusSdkWrapper.AsusLedRestoreLighting();
                
                if (i < 8)
                    Thread.Sleep(speed);
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (!_AsusDeviceKeyboard)
                return;

            if (!_AsusFlash2Running)
            {
                foreach (var key in regions)
                    AsusSdkWrapper.AsusLedSaveLightingForKey(ToKeyboardNames(key));

                _AsusFlash2Running = true;
                _AsusFlash2Step = 0;
            }

            if (_AsusFlash2Running)
                while (_AsusFlash2Running)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    if (_AsusFlash2Step == 0)
                    {
                        if (_AsusDeviceKeyboard)
                            foreach (var key in regions)
                                ApplyMapKeyLighting(key, burstcol, false);

                        _AsusFlash2Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_AsusFlash2Step == 1)
                    {
                        if (_AsusDeviceKeyboard)
                            foreach (var key in regions)
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));

                        _AsusFlash2Step = 0;

                        Thread.Sleep(speed);
                    }
                }
        }

        public void SingleFlash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (!_AsusDeviceKeyboard)
                return;

            if (!_AsusFlash2Running)
            {
                AsusSdkWrapper.AsusLedSaveCurrentLighting();

                _AsusFlash2Running = true;
                _AsusFlash2Step = 0;
            }

            if (_AsusFlash2Running)
                while (_AsusFlash2Running)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    if (_AsusFlash2Step == 0)
                    {
                        if (_AsusDeviceKeyboard)
                            ApplyMapSingleLighting(burstcol);

                        _AsusFlash2Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_AsusFlash2Step == 1)
                    {
                        if (_AsusDeviceKeyboard)
                            AsusSdkWrapper.AsusLedRestoreLighting();


                        _AsusFlash2Step = 0;

                        Thread.Sleep(speed);
                    }
                }
        }

        public void Flash3(Color burstcol, int speed, CancellationToken cts)
        {
            if (!_AsusDeviceKeyboard)
                return;

            try
            {
                lock (_Flash3)
                {
                    var presets = new Dictionary<string, Color>();
                    _AsusFlash3Running = true;
                    _AsusFlash3Step = 0;

                    if (_AsusFlash3Running)
                        while (_AsusFlash3Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_AsusFlash3Step == 0)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.NumFlash, key);
                                    if (pos > -1)
                                        ApplyMapKeyLighting(key, burstcol, false);
                                }
                                _AsusFlash3Step = 1;
                            }
                            else if (_AsusFlash3Step == 1)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.NumFlash, key);
                                    if (pos > -1)
                                        ApplyMapKeyLighting(key, Color.Black, false);
                                }

                                _AsusFlash3Step = 0;
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
            if (!_AsusDeviceKeyboard)
                return;

            if (!_AsusFlash4Running)
            {
                foreach (var key in regions)
                    AsusSdkWrapper.AsusLedSaveLightingForKey(ToKeyboardNames(key));

                _AsusFlash4Running = true;
                _AsusFlash4Step = 0;
            }

            if (_AsusFlash4Running)
                while (_AsusFlash4Running)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    if (_AsusFlash4Step == 0)
                    {
                        if (_AsusDeviceKeyboard)
                            foreach (var key in regions)
                                ApplyMapKeyLighting(key, burstcol, false);

                        _AsusFlash4Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_AsusFlash4Step == 1)
                    {
                        if (_AsusDeviceKeyboard)
                            foreach (var key in regions)
                                AsusSdkWrapper.AsusLedRestoreLightingForKey(ToKeyboardNames(key));

                        _AsusFlash4Step = 0;

                        Thread.Sleep(speed);
                    }
                }
        }

        public void SingleFlash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (!_AsusDeviceKeyboard)
                return;

            if (!_AsusFlash4Running)
            {
                AsusSdkWrapper.AsusLedSaveCurrentLighting();

                _AsusFlash4Running = true;
                _AsusFlash4Step = 0;
            }

            if (_AsusFlash4Running)
                while (_AsusFlash4Running)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    if (_AsusFlash4Step == 0)
                    {
                        if (_AsusDeviceKeyboard)
                            ApplyMapSingleLighting(burstcol);
                        
                        _AsusFlash4Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_AsusFlash4Step == 1)
                    {
                        if (_AsusDeviceKeyboard)
                            AsusSdkWrapper.AsusLedRestoreLighting();
                        
                        _AsusFlash4Step = 0;

                        Thread.Sleep(speed);
                    }
                }
        }

        public void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50)
        {
            if (!_AsusDeviceKeyboard) return;
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

                    AsusSdkWrapper.AsusLedSaveLightingForKey(ToKeyboardNames(key));
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
            if (!_AsusDeviceKeyboard) return;

            while (true)
            {
                lock (lockObject)
                {
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        AsusSdkWrapper.AsusLedSetLighting((int) Math.Ceiling((double) (250 * 100) / 255),
                            (int) Math.Ceiling((double) (x * 100) / 255), 0);
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        AsusSdkWrapper.AsusLedSetLighting((int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255), 0);
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        AsusSdkWrapper.AsusLedSetLighting((int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255), 0);
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        AsusSdkWrapper.AsusLedSetLighting(0, (int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255));
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        AsusSdkWrapper.AsusLedSetLighting((int) Math.Ceiling((double) (x * 100) / 255), 0,
                            (int) Math.Ceiling((double) (250 * 100) / 255));
                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        AsusSdkWrapper.AsusLedSetLighting((int) Math.Ceiling((double) (250 * 100) / 255), 0,
                            (int) Math.Ceiling((double) (x * 100) / 255));
                    }

                    if (token.IsCancellationRequested) break;
                }
            }
            Thread.Sleep(interval);
        }


        private static KeyboardNames ToKeyboardNames(string key)
        {
            KeyboardNames keyName;
            if (Enum.TryParse(key, out keyName))
                return keyName;
            return KeyboardNames.GBadge;
        }

        
    }

    public enum DeviceType
    {
        Keyboard = 0x0,
        Mouse = 0x3,
        Mousemat = 0x4,
        Headset = 0x8,
        Speaker = 0xe
    }

    public enum KeyboardNames
    {
        Esc = 0x01,
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
        RightWindows = 0x15C,
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
        G6 = 0xFFF6,
        G7 = 0xFFF7,
        G8 = 0xFFF8,
        G9 = 0xFFF9,
        GLogo = 0xFFFF1,
        GBadge = 0xFFFF2
    }
}
*/