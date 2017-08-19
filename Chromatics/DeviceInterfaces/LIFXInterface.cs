using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using LifxNet;
using Color = System.Drawing.Color;

/* Contains all Lifx SDK code for detection, initilization, states and effects.
 */

namespace Chromatics.DeviceInterfaces
{
    public class LifxInterface
    {
        public static LifxLib InitializeLifxsdk()
        {
            LifxLib lifx = null;
            lifx = new LifxLib();

            var lifxstat = lifx.InitializeSdk();

            if (!lifxstat)
                return null;

            return lifx;
        }
    }

    public class LifxSdkWrapper
    {
        //
    }

    public interface ILifxSdk
    {
        int LifxBulbs { get; set; }

        Dictionary<string, int> LifxStateMemory { get; }
        Dictionary<string, DeviceModeTypes> LifxModeMemory { get; }
        Dictionary<uint, string> LifXproductids { get; }
        Dictionary<string, LightBulb> LifxDevices { get; }
        Dictionary<LightBulb, DeviceModeTypes> LifxBulbsDat { get; }
        Dictionary<LightBulb, LightStateResponse> LifxBulbsRestore { get; }
        bool InitializeSdk();
        void LifxRestoreState();
        void LifxUpdateState(DeviceModeTypes mode, Color col, int transition);
        void LifxUpdateStateBrightness(DeviceModeTypes mode, Color col, ushort brightness, int transition);
        Task<LightStateResponse> GetLightStateAsync(LightBulb id);
        Task<StateVersionResponse> GetDeviceVersionAsync(LightBulb id);
        void SetColorAsync(LightBulb id, ushort hue, ushort saturation, ushort brightness, ushort kelvin, TimeSpan ts);
        void Flash4(Color basecol, Color burstcol, int speed, CancellationToken cts);
    }

    public class LifxLib : ILifxSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private static int _lifxBulbs;

        private static Task _lifXpendingUpdateColor;
        private static bool _lifxSdk;

        private static readonly Dictionary<LightBulb, DeviceModeTypes> _LifxBulbsDat =
            new Dictionary<LightBulb, DeviceModeTypes>();

        private static readonly Dictionary<LightBulb, LightStateResponse> _LifxBulbsRestore =
            new Dictionary<LightBulb, LightStateResponse>();

        private static readonly Dictionary<string, LightBulb> _LifxDevices = new Dictionary<string, LightBulb>();

        private static readonly Dictionary<string, DeviceModeTypes> _LifxModeMemory =
            new Dictionary<string, DeviceModeTypes>();

        private static readonly Dictionary<uint, string> _LIFXproductids = new Dictionary<uint, string>
        {
            //Keys
            {1, "LIFX Original 1000"},
            {3, "LIFX Color 650"},
            {10, "LIFX White 800"},
            {11, "LIFX White 800"},
            {18, "LIFX White 900 BR30"},
            {20, "LIFX Color 1000 BR30"},
            {22, "LIFX Color 1000"},
            {27, "LIFX A19"},
            {28, "LIFX BR30"},
            {29, "LIFX+ A19"},
            {30, "LIFX+ BR30"},
            {31, "LIFX Z"}
        };

        private static readonly Dictionary<string, int> _LifxStateMemory = new Dictionary<string, int>();

        private static int _flash4Step;
        private static bool _flash4Running;
        private static readonly object _Flash4 = new object();
        private LifxClient _client;

        private Action _lifXpendingUpdateColorAction;

        private Action _lifXpendingUpdateColorActionBright;
        private Task _lifXpendingUpdateColorBright;

        public int LifxBulbs
        {
            get => _lifxBulbs;
            set => _lifxBulbs = value;
        }

        public Dictionary<string, int> LifxStateMemory => _LifxStateMemory;

        public Dictionary<string, DeviceModeTypes> LifxModeMemory => _LifxModeMemory;

        public Dictionary<uint, string> LifXproductids => _LIFXproductids;

        public Dictionary<string, LightBulb> LifxDevices => _LifxDevices;

        public Dictionary<LightBulb, DeviceModeTypes> LifxBulbsDat => _LifxBulbsDat;

        public Dictionary<LightBulb, LightStateResponse> LifxBulbsRestore => _LifxBulbsRestore;

        public bool InitializeSdk()
        {
            Write.WriteConsole(ConsoleTypes.Lifx, "Attempting to load LIFX SDK..");

            try
            {
                var task = LifxClient.CreateAsync();
                task.Wait();
                _client = task.Result;
                _client.DeviceDiscovered += LIFXClient_DeviceDiscovered;
                _client.DeviceLost += LIFXClient_DeviceLost;
                _client.StartDeviceDiscovery();

                Write.WriteConsole(ConsoleTypes.Lifx, "LIFX SDK Loaded");
                return true;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Lifx, "LIFX SDK Failed to Load. Error: " + ex.Message);
                return false;
            }
        }

        public async void LifxRestoreState()
        {
            foreach (var d in _LifxBulbsRestore)
            {
                var state = d.Value;
                await _client.SetColorAsync(d.Key, state.Hue, state.Saturation, state.Brightness, state.Kelvin,
                    TimeSpan.FromMilliseconds(1000));
                Write.WriteConsole(ConsoleTypes.Lifx, "Restoring LIFX Bulb " + state.Label);
                //Thread.Sleep(500);
            }
        }

        public async void LifxUpdateState(DeviceModeTypes mode, Color col, int transition)
        {
            if (_lifxSdk && _lifxBulbs > 0)
            {
                if (_lifXpendingUpdateColor != null)
                {
                    _lifXpendingUpdateColorAction = () => LifxUpdateState(mode, col, transition);
                    return;
                }

                var _transition = TimeSpan.FromMilliseconds(transition);
                var _col = new LifxNet.Color();
                _col.R = col.R;
                _col.G = col.G;
                _col.B = col.B;

                //ushort _hue = Convert.ToUInt16(col.GetHue());
                //ushort _sat = Convert.ToUInt16(col.GetSaturation());
                //ushort _bright = Convert.ToUInt16(col.GetBrightness());
                ushort kelvin = 2700;

                foreach (var d in _LifxBulbsDat)
                    if (d.Value == mode || mode == DeviceModeTypes.Unknown) //100
                    {
                        if (_LifxStateMemory[d.Key.MacAddressName] == 0) return;
                        var state = await _client.GetLightStateAsync(d.Key);
                        var setColorTask = _client.SetColorAsync(d.Key, _col, kelvin, _transition);
                        var throttleTask = Task.Delay(50);
                        //Ensure task takes minimum 50 ms (no more than 20 messages per second)
                        _lifXpendingUpdateColor = Task.WhenAll(setColorTask, throttleTask);
                    }

                _lifXpendingUpdateColor = null;
                if (_lifXpendingUpdateColorAction != null)
                {
                    var a = _lifXpendingUpdateColorAction;
                    _lifXpendingUpdateColorAction = null;
                    a();
                }
            }
        }

        public async void LifxUpdateStateBrightness(DeviceModeTypes mode, Color col, ushort brightness, int transition)
        {
            if (_lifxSdk && _lifxBulbs > 0)
            {
                if (_lifXpendingUpdateColorBright != null)
                {
                    _lifXpendingUpdateColorActionBright =
                        () => LifxUpdateStateBrightness(mode, col, brightness, transition);
                    return;
                }

                var _transition = TimeSpan.FromMilliseconds(transition);

                var _hue = col.GetHue();
                var _sat = col.GetSaturation();
                var _bright = col.GetBrightness();

                var hue =
                    (_hue - Convert.ToUInt16(0f)) * (65535 - 0) / (Convert.ToUInt16(360f) - Convert.ToUInt16(0f)) +
                    0;
                var sat = (_sat - Convert.ToUInt16(0f)) * (65535 - 0) / (Convert.ToUInt16(1f) - Convert.ToUInt16(0f)) +
                          0;
                var bright = (_bright - Convert.ToUInt16(0f)) * (65535 - 0) /
                             (Convert.ToUInt16(1f) - Convert.ToUInt16(0f)) + 0;

                ushort kelvin = 2700;

                if (mode == DeviceModeTypes.Castbar) kelvin = 6000;

                foreach (var d in _LifxBulbsDat)
                    if (d.Value == mode || mode == DeviceModeTypes.Unknown) //100
                    {
                        if (_LifxStateMemory[d.Key.MacAddressName] == 0) return;
                        var state = await _client.GetLightStateAsync(d.Key);
                        var setColorTask = _client.SetColorAsync(d.Key, (ushort) hue, (ushort) sat, brightness, kelvin,
                            _transition);
                        var throttleTask = Task.Delay(50);
                        //Ensure task takes minimum 50 ms (no more than 20 messages per second)
                        _lifXpendingUpdateColorBright = Task.WhenAll(setColorTask, throttleTask);
                    }

                _lifXpendingUpdateColorBright = null;
                if (_lifXpendingUpdateColorActionBright != null)
                {
                    var a = _lifXpendingUpdateColorActionBright;
                    _lifXpendingUpdateColorActionBright = null;
                    a();
                }
            }
        }

        public void Flash4(Color basecol, Color burstcol, int speed, CancellationToken cts)
        {
            try
            {
                lock (_Flash4)
                {
                    if (!_flash4Running)
                    {
                        _flash4Running = true;
                        _flash4Step = 0;
                    }

                    if (_flash4Running)
                        while (_flash4Running)
                        {
                            if (cts.IsCancellationRequested)
                            {
                                LifxUpdateState(DeviceModeTypes.DutyFinder, basecol, 1000);
                                break;
                            }

                            if (_flash4Step == 0)
                            {
                                LifxUpdateState(DeviceModeTypes.DutyFinder, burstcol, 0);
                                _flash4Step = 1;
                            }
                            else if (_flash4Step == 1)
                            {
                                LifxUpdateState(DeviceModeTypes.DutyFinder, basecol, 0);
                                _flash4Step = 0;
                            }

                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
            }
        }

        public async Task<LightStateResponse> GetLightStateAsync(LightBulb id)
        {
            return await _client.GetLightStateAsync(id);
        }

        public async Task<StateVersionResponse> GetDeviceVersionAsync(LightBulb id)
        {
            return await _client.GetDeviceVersionAsync(id);
        }

        public async void SetColorAsync(LightBulb id, ushort hue, ushort saturation, ushort brightness, ushort kelvin,
            TimeSpan ts)
        {
            await _client.SetColorAsync(id, hue, saturation, brightness, kelvin, ts);
        }

        private void LIFXClient_DeviceLost(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            Write.WriteConsole(ConsoleTypes.Lifx,
                "LIFX Device Lost: " + e.Device.HostName + " (" + e.Device.MacAddress + ")");
            _LifxBulbsDat.Remove(e.Device as LightBulb);
            _LifxBulbsRestore.Remove(e.Device as LightBulb);
            _LifxDevices.Remove(e.Device.MacAddressName);

            if (_lifxBulbs > 0)
                _lifxBulbs--;

            if (_lifxBulbs == 0)
            {
                _lifxSdk = false;
                //LifxSDKCalled = 0;
                Write.WriteConsole(ConsoleTypes.Lifx, "LIFX SDK Disabled (No Devices Found)");
            }

            Write.ResetDeviceDataGrid();
        }

        private async void LIFXClient_DeviceDiscovered(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            var version = await _client.GetDeviceVersionAsync(e.Device);
            var state = await _client.GetLightStateAsync(e.Device as LightBulb);
            var defaultmode = DeviceModeTypes.Standby;

            if (!_LifxModeMemory.ContainsKey(e.Device.MacAddressName))
            {
                //Save to devices.chromatics
                _LifxModeMemory.Add(e.Device.MacAddressName, defaultmode);
                _LifxStateMemory.Add(e.Device.MacAddressName, 1);
                Write.SaveDevices();
            }
            else
            {
                //Load from devices.chromatics
                defaultmode = _LifxModeMemory[e.Device.MacAddressName];
            }

            _LifxBulbsDat.Add(e.Device as LightBulb, defaultmode);
            _LifxBulbsRestore.Add(e.Device as LightBulb, state);
            _LifxDevices.Add(e.Device.MacAddressName, e.Device as LightBulb);


            _lifxBulbs++;

            if (_lifxSdk == false && _lifxBulbs > 0)
            {
                _lifxSdk = true;
                //LifxSDKCalled = 1;
                Write.WriteConsole(ConsoleTypes.Lifx, "LIFX SDK Enabled");
            }

            Write.WriteConsole(ConsoleTypes.Lifx,
                "LIFX Bulb Found: " + state.Label + " (" + e.Device.MacAddressName + ")");

            Write.ResetDeviceDataGrid();
        }
    }
}