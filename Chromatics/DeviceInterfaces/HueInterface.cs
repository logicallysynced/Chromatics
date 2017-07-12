using GalaSoft.MvvmLight.Ioc;
using Q42.HueApi;
using Q42.HueApi.ColorConverters.Original;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Bridge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/* Contains all Philips Hue SDK code for detection, initilization, states and effects.
* 
* 
*/

namespace Chromatics.DeviceInterfaces
{
    public class HueInterface
    {
        public static HueLib InitializeHueSDK(string HUEDefault)
        {
            HueLib hue = null;
            hue = new HueLib();

            var hueinit = hue.InitializeSDK(HUEDefault);
            hueinit.Wait();
            var huestat = hueinit.Result;

            if (!huestat)
                return null;

            return hue;
        }
    }

    public class HueSdkWrapper
    {
        //
    }

    public interface IHueSdk
    {
        Task<bool> InitializeSDK(string HUEDefault);
        void HueRestoreState();
        Task<State> GetLightStateAsync(Light light);
        Task<string> GetDeviceVersionAsync(Light light);
        void SetColorAsync(Light light, int? Hue, int? Saturation, int Brightness, int? ColorTemperature, TimeSpan ts);
        void HUEUpdateState(int mode, System.Drawing.Color col, int transition);
        void HUEUpdateStateBrightness(int mode, System.Drawing.Color col, int? brightness, int transition);

        int HueBulbs { get; set; }
        Dictionary<string, int> HueModeMemory { get; }
        Dictionary<string, Light> HueDevices { get; }
        Dictionary<Light, int> HueBulbsDat { get; }
        Dictionary<Light, State> HueBulbsRestore { get; }
        Dictionary<string, int> HueStateMemory { get; }
    }

    public class HueLib : IHueSdk
    {
        private static ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private ILocalHueClient client;
        private static int _HueBulbs;
        private static readonly Dictionary<string, int> _HueModeMemory = new Dictionary<string, int>();
        private static readonly Dictionary<string, Light> _HueDevices = new Dictionary<string, Light>();
        private static readonly Dictionary<string, int> _HueStateMemory = new Dictionary<string, int>();
        private static readonly Dictionary<Light, int> _HueBulbsDat = new Dictionary<Light, int>();
        private static readonly Dictionary<Light, State> _HueBulbsRestore =
            new Dictionary<Light, State>();

        private static Task _HUEpendingUpdateColor;
        private Action _HUEpendingUpdateColorAction;
        private Action _HUEpendingUpdateColorActionBright;
        private Task _HUEpendingUpdateColorBright;

        public async Task<bool> InitializeSDK(string HUEDefault)
        {
            write.WriteConsole(ConsoleTypes.HUE, "Attempting to load HUE SDK..");

            try
            {
                IBridgeLocator locator = new HttpBridgeLocator();
                Dictionary<string, LocatedBridge> _devices = new Dictionary<string, LocatedBridge>();

                var bridgeIPs = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
                foreach(var bridge in bridgeIPs)
                {
                    write.WriteConsole(ConsoleTypes.HUE, "Found HUE Bridge (" + bridge.BridgeId + ") at " + bridge.IpAddress);
                    _devices.Add(bridge.BridgeId, bridge);
                }

                var _selectdevice = "";

                if (_devices.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(HUEDefault))
                    {
                        if (_devices.ContainsKey(HUEDefault))
                        {
                            _selectdevice = _devices[HUEDefault].BridgeId;
                            write.WriteConsole(ConsoleTypes.HUE, "Connected to preferred HUE Bridge (" + _devices[HUEDefault].BridgeId + ") at " + _devices[HUEDefault].IpAddress);
                        }
                        else
                        {
                            _selectdevice = _devices.FirstOrDefault().Key;
                            write.WriteConsole(ConsoleTypes.HUE, "Unable to find your preferred HUE Bridge.");
                            write.WriteConsole(ConsoleTypes.HUE, "Connected to HUE Bridge (" + _devices.FirstOrDefault().Value.BridgeId + ") at " + _devices.FirstOrDefault().Value.IpAddress);
                        }
                    }
                    else
                    {
                        _selectdevice = _devices.FirstOrDefault().Key;
                        write.WriteConsole(ConsoleTypes.HUE, "Connected to HUE Bridge (" + _devices.FirstOrDefault().Value.BridgeId + ") at " + _devices.FirstOrDefault().Value.IpAddress);
                    }
                }
                else
                {
                    write.WriteConsole(ConsoleTypes.HUE, "Unable to find any HUE Bridges.");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(_selectdevice))
                {
                    client = new LocalHueClient(_devices[_selectdevice].IpAddress);
                    var appKey = await client.RegisterAsync("Chromatics", "Chromatics_Bridge");

                    client.Initialize("Chromatics");

                    //Get lights
                    var _lights = await client.GetLightsAsync();

                    foreach (var light in _lights)
                    {
                        var defaultmode = 1;

                        if (!_HueModeMemory.ContainsKey(light.UniqueId))
                        {
                            //Save to devices.chromatics
                            _HueModeMemory.Add(light.UniqueId, defaultmode);
                            _HueStateMemory.Add(light.UniqueId, 1);

                            write.SaveDevices();
                        }
                        else
                        {
                            //Load from devices.chromatics
                            defaultmode = _HueModeMemory[light.UniqueId];
                        }

                        _HueDevices.Add(light.UniqueId, light);
                        _HueBulbsDat.Add(light, defaultmode);
                        _HueBulbsRestore.Add(light, light.State);

                        _HueBulbs++;

                        write.WriteConsole(ConsoleTypes.HUE, "HUE Bulb Found: " + light.Name + " (" + light.Id + ").");

                        write.ResetDeviceDataGrid();
                    }

                    return true;
                }
                else
                {
                    write.WriteConsole(ConsoleTypes.HUE, "HUE SDK Failed to Load. Error: Bridge Scan Error");
                    return false;
                }
            }
            catch(Exception ex)
            {
                write.WriteConsole(ConsoleTypes.HUE, "HUE SDK Failed to Load. Error: " + ex.Message);
                return false;
            }
        }

        public async void HueRestoreState()
        {
            var connect = await client.CheckConnection();

            if (connect)
            {
                foreach (var d in _HueBulbsRestore)
                {
                    try
                    {
                        var state = d.Value;
                        var command = new LightCommand();
                        command.Brightness = state.Brightness;
                        command.ColorCoordinates = state.ColorCoordinates;
                        command.ColorTemperature = state.ColorTemperature;
                        command.Hue = state.Hue;
                        command.On = state.On;
                        command.Saturation = state.Saturation;

                        await client.SendCommandAsync(command, new List<string> { d.Key.Id }); //Unsure if Id or UniqueId should be used
                        
                        write.WriteConsole(ConsoleTypes.HUE, "Restoring HUE Bulb " + d.Key.Name);
                        //Thread.Sleep(500);
                    }
                    catch (Exception ex)
                    {
                        write.WriteConsole(ConsoleTypes.HUE, "An Error occurred while restoring HUE Bulb " + d.Key.Name + ". Error: " + ex.Message);
                    }
                }
            }
            else
            {
                write.WriteConsole(ConsoleTypes.HUE, "Unable to connect to HUE Hub.");
            }

        }

        public async void HUEUpdateState(int mode, System.Drawing.Color col, int transition)
        {
            var connect = await client.CheckConnection();

            if (connect && _HueBulbs > 0)
            {
                if (_HUEpendingUpdateColor != null)
                {
                    _HUEpendingUpdateColorAction = () => HUEUpdateState(mode, col, transition);
                    return;
                }

                var _transition = TimeSpan.FromMilliseconds(transition);
                var _col = new Q42.HueApi.ColorConverters.RGBColor(col.R, col.G, col.B);

                //ushort _hue = Convert.ToUInt16(col.GetHue());
                //ushort _sat = Convert.ToUInt16(col.GetSaturation());
                //ushort _bright = Convert.ToUInt16(col.GetBrightness());
                //ushort _kelvin = 2700;

                foreach (var d in _HueBulbsDat)
                {
                    if (d.Value == mode || mode == 100)
                    {
                        if (_HueStateMemory[d.Key.UniqueId] == 0) { return; }
                        var state = await client.GetLightAsync(d.Key.Id); //Unsure if Id or UniqueId should be used

                        var command = new LightCommand();
                        command.On = true;
                        command.SetColor(_col, d.Key.ModelId);

                        var setColorTask = client.SendCommandAsync(command, new List<string> { d.Key.Id }); //Unsure if Id or UniqueId should be used

                        var throttleTask = Task.Delay(50);
                        //Ensure task takes minimum 50 ms (no more than 20 messages per second)
                        _HUEpendingUpdateColor = Task.WhenAll(setColorTask, throttleTask);
                    }
                }

                _HUEpendingUpdateColor = null;
                if (_HUEpendingUpdateColorAction != null)
                {
                    var a = _HUEpendingUpdateColorAction;
                    _HUEpendingUpdateColorAction = null;
                    a();
                }
            }
        }

        public async void HUEUpdateStateBrightness(int mode, System.Drawing.Color col, int? brightness, int transition)
        {
            var connect = await client.CheckConnection();

            if (connect && _HueBulbs > 0)
            {
                if (_HUEpendingUpdateColorBright != null)
                {
                    _HUEpendingUpdateColorActionBright =
                        () => HUEUpdateStateBrightness(mode, col, brightness, transition);
                    return;
                }

                var _transition = TimeSpan.FromMilliseconds(transition);
                var _col = new Q42.HueApi.ColorConverters.RGBColor(col.R, col.G, col.B);

                var _hue = (int?)col.GetHue();
                var _sat = (int?)col.GetSaturation();
                var _bright = (int?)col.GetBrightness();

                //if (mode == 10) _kelvin = 6000;

                foreach (var d in _HueBulbsDat)
                    if (d.Value == mode || mode == 100)
                    {
                        if (_HueStateMemory[d.Key.Id] == 0) { return; } //Unsure if Id or UniqueId should be used

                        var state = await client.GetLightAsync(d.Key.Id); //Unsure if Id or UniqueId should be used

                        var command = new LightCommand();
                        command.Hue = _hue;
                        command.Saturation = _sat;
                        command.Brightness = (byte)brightness;
                        command.On = true;
                        command.SetColor(_col, d.Key.ModelId);

                        var setColorTask = client.SendCommandAsync(command, new List<string> { d.Key.Id }); //Unsure if Id or UniqueId should be used
                        
                        var throttleTask = Task.Delay(50);
                        //Ensure task takes minimum 50 ms (no more than 20 messages per second)
                        _HUEpendingUpdateColorBright = Task.WhenAll(setColorTask, throttleTask);
                    }

                _HUEpendingUpdateColorBright = null;
                if (_HUEpendingUpdateColorActionBright != null)
                {
                    var a = _HUEpendingUpdateColorActionBright;
                    _HUEpendingUpdateColorActionBright = null;
                    a();
                }
            }
        }

        public async Task<State> GetLightStateAsync(Light light)
        {
            var result = await client.GetLightAsync(light.Id);
            return result.State;
        }

        public async Task<string> GetDeviceVersionAsync(Light light)
        {
            var result = await client.GetLightAsync(light.Id);
            return result.SoftwareVersion;
        }

        public async void SetColorAsync(Light light, int? Hue, int? Saturation, int Brightness, int? ColorTemperature, TimeSpan ts)
        {
            var connect = await client.CheckConnection();

            if (connect)
            {
                var command = new LightCommand();
                command.Brightness = (byte)Brightness;
                command.ColorTemperature = ColorTemperature;
                command.Hue = Hue;
                command.On = true;
                command.Saturation = Saturation;

                await client.SendCommandAsync(command, new List<string> { light.Id }); //Unsure if Id or UniqueId should be used
            }
            else
            {
                write.WriteConsole(ConsoleTypes.HUE, "Unable to connect to HUE Hub.");
            }
        }


        #region setters

        public int HueBulbs
        {
            get
            {
                return _HueBulbs;
            }
            set
            {
                _HueBulbs = value;
            }
        }

        public Dictionary<string, int> HueStateMemory
        {
            get
            {
                return _HueStateMemory;
            }
        }

        public Dictionary<string, int> HueModeMemory
        {
            get
            {
                return _HueModeMemory;
            }
        }

        public Dictionary<string, Light> HueDevices
        {
            get
            {
                return _HueDevices;
            }
        }

        public Dictionary<Light, int> HueBulbsDat
        {
            get
            {
                return _HueBulbsDat;
            }
        }

        public Dictionary<Light, State> HueBulbsRestore
        {
            get
            {
                return _HueBulbsRestore;
            }
        }

        #endregion
    }
 }