using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.Controllers;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;
using GalaSoft.MvvmLight.Ioc;
using Wooting;

namespace Chromatics.DeviceInterfaces
{
    public class WootingInterface
    {
        public static WootingLib InitializeWootingSdk()
        {
            WootingLib wooting = null;

            wooting = new WootingLib();
            var result = wooting.InitializeLights();

            if (!result)
                return null;

            return wooting;
        }
    }

    public class WootingSdkWrapper
    {
        //
    }

    public interface IWootingSdk
    {
        bool InitializeLights();
        void DeviceUpdate();
        void Shutdown();
        void ResetWootingDevices(bool deviceKeyboard, Color basecol);
        void SetAllLights(Color col);
        void SetLights(Color col);
        void ApplyMapSingleLighting(Color col);
        void ApplyMapMultiLighting(Color col, string region);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
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
    }

    public class WootingLib : IWootingSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private readonly object action_lock = new object();

        private string devicename = "Wooting";
        private bool isInitialized;
        private bool keyboard_updated;

        private Dictionary<string, Color> prevKeyboard = new Dictionary<string, Color>();
        private List<string> KeyboardHIDs = new List<string>();
        private Dictionary<int, Color> KeyboardZones = new Dictionary<int, Color>();

        private Stopwatch watch = new Stopwatch();
        private Stopwatch keepaliveTimer = new Stopwatch();
        private long lastUpdateTime = 0;

        private bool _wootingKeyboard = true;

        private static readonly object WootingRipple1 = new object();
        private static readonly object WootingRipple2 = new object();
        private static readonly object WootingFlash1 = new object();
        private static int _wootingFlash2Step;
        private static bool _wootingFlash2Running;
        private static Dictionary<string, Color> _flashpresets = new Dictionary<string, Color>();
        private static readonly object WootingFlash2 = new object();
        private static int _wootingFlash3Step;
        private static bool _wootingFlash3Running;
        private static readonly object WootingFlash3 = new object();
        private static int _wootingFlash4Step;
        private static bool _wootingFlash4Running;
        private static Dictionary<string, Color> _flashpresets4 = new Dictionary<string, Color>();
        private static readonly object WootingFlash4 = new object();

        public bool InitializeLights()
        {
            Write.WriteConsole(ConsoleTypes.Wooting, @"Attempting to load Wooting SDK..");

            lock (action_lock)
            {
                if (!isInitialized)
                {
                    try
                    {
                        foreach (var hid in KeyMap)
                        {
                            if (!prevKeyboard.ContainsKey(hid.Key))
                            {
                                prevKeyboard.Add(hid.Key, Color.Black);
                            }

                            if (!KeyboardHIDs.Contains(hid.Key))
                            {
                                KeyboardHIDs.Add(hid.Key);
                            }
                        }

                        KeyboardZones.Add(0, Color.Black);

                        if (RGBControl.IsConnected())
                        {
                            isInitialized = true;
                            return true;
                        }

                        Write.WriteConsole(ConsoleTypes.Wooting, @"No Wooting devices found.");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Write.WriteConsole(ConsoleTypes.Wooting, @"Wooting SDK failed to load. EX: " + ex);

                        isInitialized = false;
                        return false;
                    }
                }

                return isInitialized;
            }
        }


        public void Shutdown()
        {
            lock (action_lock)
            {
                try
                {
                    if (isInitialized)
                    {
                        RGBControl.Reset();
                        isInitialized = false;
                    }
                }
                catch (Exception ex)
                {
                    Write.WriteConsole(ConsoleTypes.Wooting, @"There was an error shutting down Wooting SDK. EX: " + ex);
                    isInitialized = false;
                }
            }
        }

        public void ResetWootingDevices(bool deviceKeyboard, Color basecol)
        {
            _wootingKeyboard = deviceKeyboard;
        }

        public void SetAllLights(Color col)
        {
            if (!isInitialized) return;

            keyboard_updated = false;

            try
            {
                if (_wootingKeyboard)
                {
                    foreach (var hid in KeyboardHIDs)
                    {
                        if (prevKeyboard.ContainsKey(hid) && prevKeyboard[hid] != col)
                        {
                            RGBControl.SetKey(DeviceKeyToWootingKey(hid), col.R, col.G, col.B);
                            prevKeyboard[hid] = col;
                        }
                    }

                    keyboard_updated = true;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Wooting, @"Wooting SDK: Error while updating devices. EX: " + ex);
            }
        }

        public void DeviceUpdate()
        {
            if (!isInitialized) return;

            try
            {
                if (_wootingKeyboard)
                {
                    foreach (var updates in prevKeyboard)
                    {
                        RGBControl.SetKey(DeviceKeyToWootingKey(updates.Key), updates.Value.R, updates.Value.G, updates.Value.B);
                    }

                    keyboard_updated = true;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Wooting,
                    "Wooting SDK: Error while updating keyboard. EX: " + ex);
            }
        }

        public void SetLights(Color col)
        {
            if (!isInitialized) return;

            keyboard_updated = false;

            try
            {
                if (_wootingKeyboard)
                {
                    List<string> hids = new List<string>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    foreach (var hid in KeyboardHIDs)
                    {
                        if (prevKeyboard.ContainsKey(hid) && prevKeyboard[hid] != col)
                        {
                            hids.Add(hid);
                            colors.Add(Tuple.Create(col.R, col.G, col.B));
                            prevKeyboard[hid] = col;
                        }
                    }

                    //Console.WriteLine(@"Tag");
                    //gameSenseSDK.setKeyboardColors(hids, colors);
                    keyboard_updated = true;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Wooting,
                    "Wooting SDK error while updating keyboard. EX: " + ex);
            }
        }

        public void ApplyMapSingleLighting(Color col)
        {
            if (!isInitialized) return;

            keyboard_updated = false;

            try
            {
                if (_wootingKeyboard)
                {
                    if (KeyboardZones[0] == col) return;

                    SetAllLights(col);
                    KeyboardZones[0] = col;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Wooting,
                    "Wooting SDK error while updating keyboard (single). EX: " + ex);
            }
        }

        public void ApplyMapMultiLighting(Color col, string region)
        {
            //
        }

        public void ApplyMapKeyLighting(string key, Color col, bool clear, bool bypasswhitelist = false)
        {
            if (!isInitialized) return;

            keyboard_updated = false;
            if (!_wootingKeyboard) return;

            try
            {
                var keyid = key;
                if (!prevKeyboard.ContainsKey(keyid) || prevKeyboard[keyid] == col) return;
                

                if (clear)
                {
                    RGBControl.SetKey(DeviceKeyToWootingKey(key), col.R, col.G, col.B);
                    keyboard_updated = true;
                }

                prevKeyboard[keyid] = col;

            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Wooting, @"Wooting SDK error while updating key. EX: " + ex);
            }

        }

        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                if (!isInitialized || !_wootingKeyboard)
                    return;

                var presets = new Dictionary<string, Color>();
                List<string> hids = new List<string>();
                List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                for (var i = 0; i <= 9; i++)
                {
                    if (i == 0)
                    {
                        //Setup

                        foreach (var key in DeviceEffects.GlobalKeys)
                            try
                            {
                                if (prevKeyboard.ContainsKey(key))
                                {
                                    presets.Add(key, prevKeyboard[key]);
                                }
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
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                            //ApplyMapKeyLighting(key, pos > -1 ? burstcol : presets[key], true);
                            if (pos > -1)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            }
                            else
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(presets[key].R, presets[key].G, presets[key].B));
                            }
                        }
                    }
                    else if (i == 9)
                    {
                        //Spin down

                        foreach (var key in DeviceEffects.GlobalKeys)
                        {
                            hids.Add(key);
                            colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            //ApplyMapKeyLighting(key, presets[key], true);
                        }

                        hids.Add("D1");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D2");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D3");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D4");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D5");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D6");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D7");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D8");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D9");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("D0");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("OemMinus");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));
                        hids.Add("OemEquals");
                        colors.Add(Tuple.Create(baseColor.R, baseColor.G, baseColor.B));

                        /*
                        ApplyMapKeyLighting("D1", baseColor, true);
                        ApplyMapKeyLighting("D2", baseColor, true);
                        ApplyMapKeyLighting("D3", baseColor, true);
                        ApplyMapKeyLighting("D4", baseColor, true);
                        ApplyMapKeyLighting("D5", baseColor, true);
                        ApplyMapKeyLighting("D6", baseColor, true);
                        ApplyMapKeyLighting("D7", baseColor, true);
                        ApplyMapKeyLighting("D8", baseColor, true);
                        ApplyMapKeyLighting("D9", baseColor, true);
                        ApplyMapKeyLighting("D0", baseColor, true);
                        ApplyMapKeyLighting("OemMinus", baseColor, true);
                        ApplyMapKeyLighting("OemEquals", baseColor, true);
                        */

                        presets.Clear();
                    }

                    if (i < 9)
                    {
                        Thread.Sleep(speed);
                    }

                    EffectKeyUpdate(hids, colors);
                    hids.Clear();
                    colors.Clear();
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                if (!isInitialized || !_wootingKeyboard)
                    return;

                var safeKeys = DeviceEffects.GlobalKeys.Except(FfxivHotbar.Keybindwhitelist);

                lock (WootingRipple2)
                {
                    var previousValues = new Dictionary<string, Color>();
                    var enumerable = safeKeys.ToList();
                    List<string> hids = new List<string>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    for (var i = 0; i <= 9; i++)
                    {

                        if (i == 0)
                        {
                            //Setup

                            foreach (var key in enumerable)
                                try
                                {
                                    if (prevKeyboard.ContainsKey(key))
                                    {
                                        previousValues.Add(key, prevKeyboard[key]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Write.WriteConsole(ConsoleTypes.Error, @"(" + key + "): " + ex.Message);
                                }
                        }
                        else if (i == 1)
                        {
                            //Step 0
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                if (pos > -1)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 2)
                        {
                            //Step 1
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                if (pos > -1)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
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
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 4)
                        {
                            //Step 3
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                if (pos > -1)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 5)
                        {
                            //Step 4
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                if (pos > -1)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 6)
                        {
                            //Step 5
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                if (pos > -1)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 7)
                        {
                            //Step 6
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                if (pos > -1)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 8)
                        {
                            //Step 7
                            foreach (var key in enumerable)
                            {
                                var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                if (pos > -1)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }
                            }
                        }
                        else if (i == 9)
                        {
                            //Spin down

                            foreach (var key in previousValues.Keys)
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(previousValues[key].R, previousValues[key].G, previousValues[key].B));
                                //ApplyMapKeyLighting(key, previousValues[key], true);
                            }

                            previousValues.Clear();
                        }

                        if (i < 9)
                        {
                            Thread.Sleep(speed);
                        }

                        EffectKeyUpdate(hids, colors);
                        hids.Clear();
                        colors.Clear();
                    }
                }
            });
        }

        public Task MultiRipple1(Color burstcol, int speed)
        {
            throw new NotImplementedException();
        }

        public Task MultiRipple2(Color burstcol, int speed)
        {
            throw new NotImplementedException();
        }

        public void Flash1(Color burstcol, int speed, string[] regions)
        {
            lock (WootingFlash1)
            {
                if (!isInitialized || !_wootingKeyboard)
                    return;

                var previousValues = new Dictionary<string, Color>();
                List<string> hids = new List<string>();
                List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                for (var i = 0; i <= 8; i++)
                {

                    if (i == 0)
                    {
                        //Setup

                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(key))
                            {
                                previousValues.Add(key, prevKeyboard[key]);
                            }
                        }
                    }
                    else if (i % 2 == 1)
                    {
                        //Step 1, 3, 5, 7
                        foreach (var key in regions)
                        {
                            hids.Add(key);
                            colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                            //ApplyMapKeyLighting(key, burstcol, true);
                        }
                    }
                    else if (i % 2 == 0)
                    {
                        //Step 2, 4, 6, 8
                        foreach (var key in regions)
                        {
                            hids.Add(key);
                            colors.Add(Tuple.Create(previousValues[key].R, previousValues[key].G, previousValues[key].B));
                            //ApplyMapKeyLighting(key, previousValues[key], true);
                        }
                    }

                    if (i < 8)
                    {
                        Thread.Sleep(speed);
                    }

                    EffectKeyUpdate(hids, colors);
                    hids.Clear();
                    colors.Clear();
                }
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                if (!isInitialized || !_wootingKeyboard)
                    return;

                lock (WootingFlash2)
                {
                    var previousValues = new Dictionary<string, Color>();
                    List<string> hids = new List<string>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    if (!_wootingFlash2Running)
                    {
                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(key))
                            {
                                previousValues.Add(key, prevKeyboard[key]);
                            }
                        }

                        _wootingFlash2Running = true;
                        _wootingFlash2Step = 0;
                        _flashpresets = previousValues;
                    }

                    if (_wootingFlash2Running)
                        while (_wootingFlash2Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_wootingFlash2Step == 0)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }

                                _wootingFlash2Step = 1;
                            }
                            else if (_wootingFlash2Step == 1)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(_flashpresets[key].R, _flashpresets[key].G, _flashpresets[key].B));
                                    //ApplyMapKeyLighting(key, _flashpresets[key], true);
                                }

                                _wootingFlash2Step = 0;
                            }

                            EffectKeyUpdate(hids, colors);
                            hids.Clear();
                            colors.Clear();
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
                if (!isInitialized || !_wootingKeyboard)
                    return;

                lock (WootingFlash3)
                {
                    //var previousValues = new Dictionary<string, Color>();
                    _wootingFlash3Running = true;
                    _wootingFlash3Step = 0;

                    List<string> hids = new List<string>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    if (_wootingFlash3Running == false)
                    {
                        //
                    }
                    else
                    {
                        while (_wootingFlash3Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_wootingFlash3Step == 0)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    if (prevKeyboard.ContainsKey(key))
                                    {
                                        hids.Add(key);
                                        colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                        //ApplyMapKeyLighting(key, burstcol, true);
                                    }

                                }


                                _wootingFlash3Step = 1;
                            }
                            else if (_wootingFlash3Step == 1)
                            {
                                foreach (var key in DeviceEffects.NumFlash)
                                {
                                    if (prevKeyboard.ContainsKey(key))
                                    {
                                        hids.Add(key);
                                        colors.Add(Tuple.Create(Color.Black.R, Color.Black.G, Color.Black.B));
                                        //ApplyMapKeyLighting(key, Color.Black, true);
                                    }
                                }

                                _wootingFlash3Step = 0;
                            }

                            EffectKeyUpdate(hids, colors);
                            hids.Clear();
                            colors.Clear();
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

        public void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            try
            {
                if (!isInitialized || !_wootingKeyboard)
                    return;

                lock (WootingFlash4)
                {
                    var flashpresets = new Dictionary<string, Color>();
                    List<string> hids = new List<string>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    if (!_wootingFlash4Running)
                    {
                        foreach (var key in regions)
                        {
                            if (prevKeyboard.ContainsKey(key))
                                flashpresets.Add(key, prevKeyboard[key]);
                        }


                        _wootingFlash4Running = true;
                        _wootingFlash4Step = 0;
                        _flashpresets4 = flashpresets;
                    }

                    if (_wootingFlash4Running)
                        while (_wootingFlash4Running)
                        {
                            if (cts.IsCancellationRequested)
                                break;

                            if (_wootingFlash4Step == 0)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(burstcol.R, burstcol.G, burstcol.B));
                                    //ApplyMapKeyLighting(key, burstcol, true);
                                }

                                _wootingFlash4Step = 1;
                            }
                            else if (_wootingFlash4Step == 1)
                            {
                                foreach (var key in regions)
                                {
                                    hids.Add(key);
                                    colors.Add(Tuple.Create(_flashpresets4[key].R, _flashpresets4[key].G, _flashpresets4[key].B));
                                    //ApplyMapKeyLighting(key, _flashpresets4[key], true);
                                }

                                _wootingFlash4Step = 0;
                            }

                            EffectKeyUpdate(hids, colors);
                            hids.Clear();
                            colors.Clear();

                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
                //
            }
        }

        public void SingleFlash1(Color burstcol, int speed, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void SingleFlash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void SingleFlash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            throw new NotImplementedException();
        }

        public void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50)
        {
            if (!isInitialized || !_wootingKeyboard) return;
            if (cts.IsCancellationRequested) return;

            Dictionary<string, ColorFader> colorFaderDict = new Dictionary<string, ColorFader>();

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

                    var rndCol = toColor[rnd.Next(toColor.Length)];

                    colorFaderDict.Add(key, new ColorFader(toColor[0], rndCol, interval));
                }

                Task t = Task.Factory.StartNew(() =>
                {
                    //Thread.Sleep(500);

                    var _regions = regions.OrderBy(x => rnd.Next()).ToArray();

                    List<string> hids = new List<string>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

                    foreach (var key in _regions)
                    {
                        if (cts.IsCancellationRequested) return;

                        foreach (var color in colorFaderDict[key].Fade())
                        {
                            if (cts.IsCancellationRequested) return;

                            if (prevKeyboard.ContainsKey(key))
                            {
                                hids.Add(key);
                                colors.Add(Tuple.Create(color.R, color.G, color.B));
                            }
                        }

                        EffectKeyUpdate(hids, colors);
                        hids.Clear();
                        colors.Clear();

                        Thread.Sleep(speed);
                    }
                });

                Thread.Sleep(colorFaderDict.Count * speed);
            }
        }

        public void CycleEffect(int interval, CancellationTokenSource token)
        {
            if (!isInitialized || !_wootingKeyboard)
                return;

            List<string> hids = new List<string>();
            List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();

            while (true)
            {
                for (var x = 0; x <= 250; x += 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    foreach (var hid in KeyboardHIDs)
                    {
                        hids.Add(hid);
                        colors.Add(Tuple.Create((byte)Math.Ceiling((double)(250 * 100) / 255), (byte)Math.Ceiling((double)(x * 100) / 255), (byte)0));
                    }
                }
                for (var x = 250; x >= 5; x -= 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    foreach (var hid in KeyboardHIDs)
                    {
                        hids.Add(hid);
                        colors.Add(Tuple.Create((byte)Math.Ceiling((double)(x * 100) / 255), (byte)Math.Ceiling((double)(250 * 100) / 255), (byte)0));
                    }
                }
                for (var x = 0; x <= 250; x += 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    foreach (var hid in KeyboardHIDs)
                    {
                        hids.Add(hid);
                        colors.Add(Tuple.Create((byte)Math.Ceiling((double)(x * 100) / 255), (byte)Math.Ceiling((double)(250 * 100) / 255), (byte)0));
                    }
                }
                for (var x = 250; x >= 5; x -= 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    foreach (var hid in KeyboardHIDs)
                    {
                        hids.Add(hid);
                        colors.Add(Tuple.Create((byte)0, (byte)Math.Ceiling((double)(x * 100) / 255), (byte)Math.Ceiling((double)(250 * 100) / 255)));
                    }
                }
                for (var x = 0; x <= 250; x += 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    foreach (var hid in KeyboardHIDs)
                    {
                        hids.Add(hid);
                        colors.Add(Tuple.Create((byte)Math.Ceiling((double)(x * 100) / 255), (byte)0, (byte)Math.Ceiling((double)(250 * 100) / 255)));
                    }

                }
                for (var x = 250; x >= 5; x -= 5)
                {
                    if (token.IsCancellationRequested) break;
                    Thread.Sleep(10);
                    foreach (var hid in KeyboardHIDs)
                    {
                        hids.Add(hid);
                        colors.Add(Tuple.Create((byte)Math.Ceiling((double)(250 * 100) / 255), (byte)0, (byte)Math.Ceiling((double)(x * 100) / 255)));
                    }

                }
                if (token.IsCancellationRequested) break;

                EffectKeyUpdate(hids, colors);
                hids.Clear();
                colors.Clear();
            }
            Thread.Sleep(interval);
        }

        private void EffectKeyUpdate(List<string> hids, List<Tuple<byte, byte, byte>> cols)
        {
            if (!isInitialized) return;

            try
            {
                if (_wootingKeyboard)
                {
                    var i = 0;
                    foreach (var hid in hids)
                    {
                        RGBControl.SetKey(DeviceKeyToWootingKey(hid), cols[i].Item1, cols[i].Item2, cols[i].Item3);
                        i++;
                    }
                    
                    keyboard_updated = true;
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Wooting,
                    "Wooting SDK: Error while updating keyboard. EX: " + ex);
            }
        }

        private string GetDeviceUpdatePerformance()
        {
            return (isInitialized ? lastUpdateTime + " ms" : "");
        }

        private string GetDeviceDetails()
        {
            if (isInitialized)
            {
                string devString = devicename + ": ";
                devString += "Connected";
                return devString;
            }

            return devicename + ": Not initialized";
        }

        private string GetDeviceName()
        {
            return devicename;
        }

        private void Reset()
        {
            if (isInitialized && keyboard_updated)
            {
                keyboard_updated = false;
            }
        }

        private static WootingKey.Keys DeviceKeyToWootingKey(string key)
        {
            if (KeyMap.TryGetValue(key, out WootingKey.Keys w_key))
                return w_key;

            return WootingKey.Keys.None;
        }

        private static Dictionary<string, WootingKey.Keys> KeyMap = new Dictionary<string, WootingKey.Keys> {
            //Row 0
            { "Escape", WootingKey.Keys.Esc },
            { "F1", WootingKey.Keys.F1 },
            { "F2", WootingKey.Keys.F2 },
            { "F3", WootingKey.Keys.F3 },
            { "F4", WootingKey.Keys.F4 },
            { "F5", WootingKey.Keys.F5 },
            { "F6", WootingKey.Keys.F6 },
            { "F7", WootingKey.Keys.F7 },
            { "F8", WootingKey.Keys.F8 },
            { "F9", WootingKey.Keys.F9 },
            { "F10", WootingKey.Keys.F10 },
            { "F11", WootingKey.Keys.F11 },
            { "F12", WootingKey.Keys.F12 },
            { "PrintScreen", WootingKey.Keys.PrintScreen },
            { "Pause", WootingKey.Keys.PauseBreak },
            { "Scroll", WootingKey.Keys.Mode_ScrollLock },
            { "Macro1", WootingKey.Keys.A1 },
            { "Macro2",  WootingKey.Keys.A2 },
            { "Macro3", WootingKey.Keys.A3 },
            { "Macro4", WootingKey.Keys.Mode },

            //Row 1
            { "OemTilde", WootingKey.Keys.Tilda },
            { "D1", WootingKey.Keys.N1 },
            { "D2", WootingKey.Keys.N2 },
            { "D3", WootingKey.Keys.N3 },
            { "D4", WootingKey.Keys.N4 },
            { "D5", WootingKey.Keys.N5 },
            { "D6", WootingKey.Keys.N6 },
            { "D7", WootingKey.Keys.N7 },
            { "D8", WootingKey.Keys.N8 },
            { "D9", WootingKey.Keys.N9 },
            { "D0", WootingKey.Keys.N0 },
            { "OemMinus", WootingKey.Keys.Minus },
            { "OemEquals", WootingKey.Keys.Equals },
            { "Backspace", WootingKey.Keys.Backspace },
            { "Insert", WootingKey.Keys.Insert },
            { "Home", WootingKey.Keys.Home },
            { "PageUp", WootingKey.Keys.PageUp },
            { "NumLock", WootingKey.Keys.NumLock },
            { "NumDivide", WootingKey.Keys.NumSlash },
            { "NumMultiply", WootingKey.Keys.NumMulti },
            { "NumSubtract", WootingKey.Keys.NumMinus },

            //Row2
            { "Tab", WootingKey.Keys.Tab },
            { "Q", WootingKey.Keys.Q },
            { "W", WootingKey.Keys.W },
            { "E", WootingKey.Keys.E },
            { "R", WootingKey.Keys.R},
            { "T", WootingKey.Keys.T },
            { "Y", WootingKey.Keys.Y },
            { "U", WootingKey.Keys.U },
            { "I", WootingKey.Keys.I },
            { "O", WootingKey.Keys.O },
            { "P", WootingKey.Keys.P },
            { "OemLeftBracket", WootingKey.Keys.OpenBracket },
            { "OemRightBracket", WootingKey.Keys.CloseBracket },
            { "OemBackslash", WootingKey.Keys.ANSI_Backslash },
            { "Delete", WootingKey.Keys.Delete },
            { "End", WootingKey.Keys.End },
            { "PageDown", WootingKey.Keys.PageDown },
            { "Num7", WootingKey.Keys.Num7 },
            { "Num8", WootingKey.Keys.Num8 },
            { "Num9", WootingKey.Keys.Num9 },
            { "NumAdd", WootingKey.Keys.NumPlus },

            //Row3
            { "CapsLock", WootingKey.Keys.CapsLock },
            { "A", WootingKey.Keys.A },
            { "S", WootingKey.Keys.S },
            { "D", WootingKey.Keys.D },
            { "F", WootingKey.Keys.F },
            { "G", WootingKey.Keys.G },
            { "H", WootingKey.Keys.H },
            { "J", WootingKey.Keys.J },
            { "K", WootingKey.Keys.K },
            { "L", WootingKey.Keys.L },
            { "OemSemicolon", WootingKey.Keys.SemiColon },
            { "OemApostrophe", WootingKey.Keys.Apostophe },
            { "EurPound", WootingKey.Keys.ISO_Hash },
            { "Enter", WootingKey.Keys.Enter },
            { "Num4", WootingKey.Keys.Num4 },
            { "Num5", WootingKey.Keys.Num5 },
            { "Num6", WootingKey.Keys.Num6 },

            //Row4
            { "LeftShift", WootingKey.Keys.LeftShift },
            { "Z", WootingKey.Keys.Z },
            { "X", WootingKey.Keys.X },
            { "C", WootingKey.Keys.C },
            { "V", WootingKey.Keys.V },
            { "B", WootingKey.Keys.B },
            { "N", WootingKey.Keys.N },
            { "M", WootingKey.Keys.M },
            { "OemComma", WootingKey.Keys.Comma },
            { "OemPeriod", WootingKey.Keys.Period },
            { "OemSlash", WootingKey.Keys.Slash },

            { "RightShift", WootingKey.Keys.RightShift },

            { "Up", WootingKey.Keys.Up },

            { "Num1", WootingKey.Keys.Num1 },
            { "Num2", WootingKey.Keys.Num2 },
            { "Num3", WootingKey.Keys.Num3 },
            { "NumEnter", WootingKey.Keys.NumEnter },

            //Row5
            { "LeftControl", WootingKey.Keys.LeftCtrl },
            { "LeftWindows", WootingKey.Keys.LeftWin },
            { "LeftAlt", WootingKey.Keys.LeftAlt },



            { "Space", WootingKey.Keys.Space },



            { "RightAlt", WootingKey.Keys.RightAlt },
            { "RightMenu", WootingKey.Keys.RightWin },
            { "Function", WootingKey.Keys.Function },
            { "RightControl", WootingKey.Keys.RightControl },
            { "Left", WootingKey.Keys.Left },
            { "Down", WootingKey.Keys.Down },
            { "Right", WootingKey.Keys.Right },

            { "Num0", WootingKey.Keys.Num0 },
            { "NumDecimal", WootingKey.Keys.NumPeriod },
        };


    }
}
