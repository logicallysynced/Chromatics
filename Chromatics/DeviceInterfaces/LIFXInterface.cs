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
    public class LIFXInterface
    {
        public static LIFXLib InitializeLIFXSDK()
        {
            LIFXLib lifx = null;
            lifx = new LIFXLib();

            var lifxstat = lifx.InitializeSDK();

            if (!lifxstat)
                return null;

            return lifx;
        }
    }

    public class LIFXSdkWrapper
    {
        //
    }

    public interface ILIFXSdk
    {
        int LifxBulbs { get; set; }

        Dictionary<string, int> LifxStateMemory { get; }
        Dictionary<string, DeviceModeTypes> LifxModeMemory { get; }
        Dictionary<uint, string> LIFXproductids { get; }
        Dictionary<string, LightBulb> LifxDevices { get; }
        Dictionary<LightBulb, DeviceModeTypes> LifxBulbsDat { get; }
        Dictionary<LightBulb, LightStateResponse> LifxBulbsRestore { get; }
        bool InitializeSDK();
        void LIFXRestoreState();
        void LIFXUpdateState(DeviceModeTypes mode, Color col, int transition);
        void LIFXUpdateStateBrightness(DeviceModeTypes mode, Color col, ushort brightness, int transition);
        Task<LightStateResponse> GetLightStateAsync(LightBulb id);
        Task<StateVersionResponse> GetDeviceVersionAsync(LightBulb id);
        void SetColorAsync(LightBulb id, ushort Hue, ushort Saturation, ushort Brightness, ushort Kelvin, TimeSpan ts);
        void Flash4(Color basecol, Color burstcol, int speed, CancellationToken cts);
    }

    public class LIFXLib : ILIFXSdk
    {
        private static readonly ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private static int _LifxBulbs;

        private static Task _LIFXpendingUpdateColor;
        private static bool LifxSDK;

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

        private static int Flash4Step;
        private static bool Flash4Running;
        private static readonly object _Flash4 = new object();
        private LifxClient _client;

        private Action _LIFXpendingUpdateColorAction;

        private Action _LIFXpendingUpdateColorActionBright;
        private Task _LIFXpendingUpdateColorBright;

        public int LifxBulbs
        {
            get => _LifxBulbs;
            set => _LifxBulbs = value;
        }

        public Dictionary<string, int> LifxStateMemory => _LifxStateMemory;

        public Dictionary<string, DeviceModeTypes> LifxModeMemory => _LifxModeMemory;

        public Dictionary<uint, string> LIFXproductids => _LIFXproductids;

        public Dictionary<string, LightBulb> LifxDevices => _LifxDevices;

        public Dictionary<LightBulb, DeviceModeTypes> LifxBulbsDat => _LifxBulbsDat;

        public Dictionary<LightBulb, LightStateResponse> LifxBulbsRestore => _LifxBulbsRestore;

        public bool InitializeSDK()
        {
            write.WriteConsole(ConsoleTypes.LIFX, "Attempting to load LIFX SDK..");

            try
            {
                var task = LifxClient.CreateAsync();
                task.Wait();
                _client = task.Result;
                _client.DeviceDiscovered += LIFXClient_DeviceDiscovered;
                _client.DeviceLost += LIFXClient_DeviceLost;
                _client.StartDeviceDiscovery();

                write.WriteConsole(ConsoleTypes.LIFX, "LIFX SDK Loaded");
                return true;
            }
            catch (Exception ex)
            {
                write.WriteConsole(ConsoleTypes.LIFX, "LIFX SDK Failed to Load. Error: " + ex.Message);
                return false;
            }
        }

        public async void LIFXRestoreState()
        {
            foreach (var d in _LifxBulbsRestore)
            {
                var state = d.Value;
                await _client.SetColorAsync(d.Key, state.Hue, state.Saturation, state.Brightness, state.Kelvin,
                    TimeSpan.FromMilliseconds(1000));
                write.WriteConsole(ConsoleTypes.LIFX, "Restoring LIFX Bulb " + state.Label);
                //Thread.Sleep(500);
            }
        }

        public async void LIFXUpdateState(DeviceModeTypes mode, Color col, int transition)
        {
            if (LifxSDK && _LifxBulbs > 0)
            {
                if (_LIFXpendingUpdateColor != null)
                {
                    _LIFXpendingUpdateColorAction = () => LIFXUpdateState(mode, col, transition);
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
                ushort _kelvin = 2700;

                foreach (var d in _LifxBulbsDat)
                    if (d.Value == mode || mode == DeviceModeTypes.UNKNOWN) //100
                    {
                        if (_LifxStateMemory[d.Key.MacAddressName] == 0) return;
                        var state = await _client.GetLightStateAsync(d.Key);
                        var setColorTask = _client.SetColorAsync(d.Key, _col, _kelvin, _transition);
                        var throttleTask = Task.Delay(50);
                        //Ensure task takes minimum 50 ms (no more than 20 messages per second)
                        _LIFXpendingUpdateColor = Task.WhenAll(setColorTask, throttleTask);
                    }

                _LIFXpendingUpdateColor = null;
                if (_LIFXpendingUpdateColorAction != null)
                {
                    var a = _LIFXpendingUpdateColorAction;
                    _LIFXpendingUpdateColorAction = null;
                    a();
                }
            }
        }

        public async void LIFXUpdateStateBrightness(DeviceModeTypes mode, Color col, ushort brightness, int transition)
        {
            if (LifxSDK && _LifxBulbs > 0)
            {
                if (_LIFXpendingUpdateColorBright != null)
                {
                    _LIFXpendingUpdateColorActionBright =
                        () => LIFXUpdateStateBrightness(mode, col, brightness, transition);
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

                ushort _kelvin = 2700;

                if (mode == DeviceModeTypes.CASTBAR) _kelvin = 6000;

                foreach (var d in _LifxBulbsDat)
                    if (d.Value == mode || mode == DeviceModeTypes.UNKNOWN) //100
                    {
                        if (_LifxStateMemory[d.Key.MacAddressName] == 0) return;
                        var state = await _client.GetLightStateAsync(d.Key);
                        var setColorTask = _client.SetColorAsync(d.Key, (ushort) hue, (ushort) sat, brightness, _kelvin,
                            _transition);
                        var throttleTask = Task.Delay(50);
                        //Ensure task takes minimum 50 ms (no more than 20 messages per second)
                        _LIFXpendingUpdateColorBright = Task.WhenAll(setColorTask, throttleTask);
                    }

                _LIFXpendingUpdateColorBright = null;
                if (_LIFXpendingUpdateColorActionBright != null)
                {
                    var a = _LIFXpendingUpdateColorActionBright;
                    _LIFXpendingUpdateColorActionBright = null;
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
                    if (!Flash4Running)
                    {
                        Flash4Running = true;
                        Flash4Step = 0;
                    }

                    if (Flash4Running)
                        while (Flash4Running)
                        {
                            if (cts.IsCancellationRequested)
                            {
                                LIFXUpdateState(DeviceModeTypes.DUTY_FINDER, basecol, 1000);
                                break;
                            }

                            if (Flash4Step == 0)
                            {
                                LIFXUpdateState(DeviceModeTypes.DUTY_FINDER, burstcol, 0);
                                Flash4Step = 1;
                            }
                            else if (Flash4Step == 1)
                            {
                                LIFXUpdateState(DeviceModeTypes.DUTY_FINDER, basecol, 0);
                                Flash4Step = 0;
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

        public async void SetColorAsync(LightBulb id, ushort Hue, ushort Saturation, ushort Brightness, ushort Kelvin,
            TimeSpan ts)
        {
            await _client.SetColorAsync(id, Hue, Saturation, Brightness, Kelvin, ts);
        }

        private void LIFXClient_DeviceLost(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            write.WriteConsole(ConsoleTypes.LIFX,
                "LIFX Device Lost: " + e.Device.HostName + " (" + e.Device.MacAddress + ")");
            _LifxBulbsDat.Remove(e.Device as LightBulb);
            _LifxBulbsRestore.Remove(e.Device as LightBulb);
            _LifxDevices.Remove(e.Device.MacAddressName);

            if (_LifxBulbs > 0)
                _LifxBulbs--;

            if (_LifxBulbs == 0)
            {
                LifxSDK = false;
                //LifxSDKCalled = 0;
                write.WriteConsole(ConsoleTypes.LIFX, "LIFX SDK Disabled (No Devices Found)");
            }

            write.ResetDeviceDataGrid();
        }

        private async void LIFXClient_DeviceDiscovered(object sender, LifxClient.DeviceDiscoveryEventArgs e)
        {
            var version = await _client.GetDeviceVersionAsync(e.Device);
            var state = await _client.GetLightStateAsync(e.Device as LightBulb);
            var defaultmode = DeviceModeTypes.STANDBY;

            if (!_LifxModeMemory.ContainsKey(e.Device.MacAddressName))
            {
                //Save to devices.chromatics
                _LifxModeMemory.Add(e.Device.MacAddressName, defaultmode);
                _LifxStateMemory.Add(e.Device.MacAddressName, 1);
                write.SaveDevices();
            }
            else
            {
                //Load from devices.chromatics
                defaultmode = _LifxModeMemory[e.Device.MacAddressName];
            }

            _LifxBulbsDat.Add(e.Device as LightBulb, defaultmode);
            _LifxBulbsRestore.Add(e.Device as LightBulb, state);
            _LifxDevices.Add(e.Device.MacAddressName, e.Device as LightBulb);


            _LifxBulbs++;

            if (LifxSDK == false && _LifxBulbs > 0)
            {
                LifxSDK = true;
                //LifxSDKCalled = 1;
                write.WriteConsole(ConsoleTypes.LIFX, "LIFX SDK Enabled");
            }

            write.WriteConsole(ConsoleTypes.LIFX,
                "LIFX Bulb Found: " + state.Label + " (" + e.Device.MacAddressName + ")");

            write.ResetDeviceDataGrid();
        }
    }
}