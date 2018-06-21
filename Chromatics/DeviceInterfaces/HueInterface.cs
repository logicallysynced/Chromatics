using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.Original;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Bridge;

/* Contains all Philips Hue SDK code for detection, initilization, states and effects.
* 
* 
*/

namespace Chromatics.DeviceInterfaces
{
    public class HueInterface
    {
        public static HueLib InitializeHueSdk(string hueDefault)
        {
            HueLib hue = null;
            hue = new HueLib();

            var hueinit = hue.InitializeSdk(hueDefault);
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
        int HueBulbs { get; set; }
        Dictionary<string, BulbModeTypes> HueModeMemory { get; }
        Dictionary<string, Light> HueDevices { get; }
        Dictionary<Light, BulbModeTypes> HueBulbsDat { get; }
        Dictionary<Light, State> HueBulbsRestore { get; }
        Dictionary<string, int> HueStateMemory { get; }
        Task<bool> InitializeSdk(string hueDefault);
        void HueRestoreState();
        Task<State> GetLightStateAsync(Light light);
        Task<string> GetDeviceVersionAsync(Light light);
        void SetColorAsync(Light light, int? hue, int? saturation, int brightness, int? colorTemperature, TimeSpan ts);
        void HueUpdateState(BulbModeTypes mode, Color col, int transition);
        void HueUpdateStateBrightness(BulbModeTypes mode, Color col, int? brightness, int transition);
        void Flash4(Color basecol, Color burstcol, int speed, CancellationToken cts);
    }

    public class HueLib : IHueSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();
        private static int _hueBulbs;

        private static readonly Dictionary<string, BulbModeTypes> _HueModeMemory =
            new Dictionary<string, BulbModeTypes>();

        private static readonly Dictionary<string, Light> _HueDevices = new Dictionary<string, Light>();
        private static readonly Dictionary<string, int> _HueStateMemory = new Dictionary<string, int>();

        private static readonly Dictionary<Light, BulbModeTypes> _HueBulbsDat =
            new Dictionary<Light, BulbModeTypes>();

        private static readonly Dictionary<Light, State> _HueBulbsRestore =
            new Dictionary<Light, State>();

        private static Task _huEpendingUpdateColor;

        private static int _flash4Step;
        private static bool _flash4Running;
        private static readonly object _Flash4 = new object();
        private Action _huEpendingUpdateColorAction;
        private Action _huEpendingUpdateColorActionBright;
        private Task _huEpendingUpdateColorBright;

        private ILocalHueClient _client;

        public async Task<bool> InitializeSdk(string hueDefault)
        {
            Write.WriteConsole(ConsoleTypes.Hue, @"Attempting to load HUE SDK..");

            try
            {
                IBridgeLocator locator = new HttpBridgeLocator();
                var devices = new Dictionary<string, LocatedBridge>();

                var bridgeIPs = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
                foreach (var bridge in bridgeIPs)
                {
                    Write.WriteConsole(ConsoleTypes.Hue,
                        "Found HUE Bridge (" + bridge.BridgeId + ") at " + bridge.IpAddress);
                    devices.Add(bridge.BridgeId, bridge);
                }

                var selectdevice = "";

                if (devices.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(hueDefault))
                    {
                        if (devices.ContainsKey(hueDefault))
                        {
                            selectdevice = devices[hueDefault].BridgeId;
                            Write.WriteConsole(ConsoleTypes.Hue,
                                "Connected to preferred HUE Bridge (" + devices[hueDefault].BridgeId + ") at " +
                                devices[hueDefault].IpAddress);
                        }
                        else
                        {
                            selectdevice = devices.FirstOrDefault().Key;
                            Write.WriteConsole(ConsoleTypes.Hue, @"Unable to find your preferred HUE Bridge.");
                            Write.WriteConsole(ConsoleTypes.Hue,
                                "Connected to HUE Bridge (" + devices.FirstOrDefault().Value.BridgeId + ") at " +
                                devices.FirstOrDefault().Value.IpAddress);
                        }
                    }
                    else
                    {
                        selectdevice = devices.FirstOrDefault().Key;
                        Write.WriteConsole(ConsoleTypes.Hue,
                            "Connected to HUE Bridge (" + devices.FirstOrDefault().Value.BridgeId + ") at " +
                            devices.FirstOrDefault().Value.IpAddress);
                    }
                }
                else
                {
                    Write.WriteConsole(ConsoleTypes.Hue, @"Unable to find any HUE Bridges.");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(selectdevice))
                {
                    _client = new LocalHueClient(devices[selectdevice].IpAddress);
                    var appKey = await _client.RegisterAsync("Chromatics", "Chromatics_Bridge");

                    _client.Initialize("Chromatics");

                    //Get lights
                    var lights = await _client.GetLightsAsync();

                    foreach (var light in lights)
                    {
                        var defaultmode = BulbModeTypes.Standby;

                        if (!_HueModeMemory.ContainsKey(light.UniqueId))
                        {
                            //Save to devices.chromatics
                            _HueModeMemory.Add(light.UniqueId, defaultmode);
                            _HueStateMemory.Add(light.UniqueId, 1);

                            Write.SaveDevices();
                        }
                        else
                        {
                            //Load from devices.chromatics
                            defaultmode = _HueModeMemory[light.UniqueId];
                        }

                        _HueDevices.Add(light.UniqueId, light);
                        _HueBulbsDat.Add(light, defaultmode);
                        _HueBulbsRestore.Add(light, light.State);

                        _hueBulbs++;

                        Write.WriteConsole(ConsoleTypes.Hue, @"HUE Bulb Found: " + light.Name + " (" + light.Id + ").");

                        Write.ResetDeviceDataGrid();
                    }

                    return true;
                }
                Write.WriteConsole(ConsoleTypes.Hue, @"HUE SDK Failed to Load. Error: Bridge Scan Error");
                return false;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Hue, @"HUE SDK Failed to Load. Error: " + ex.Message);
                return false;
            }
        }

        public async void HueRestoreState()
        {
            var connect = await _client.CheckConnection();

            if (connect)
                foreach (var d in _HueBulbsRestore)
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

                        await _client.SendCommandAsync(command,
                            new List<string> {d.Key.Id}); //Unsure if Id or UniqueId should be used

                        Write.WriteConsole(ConsoleTypes.Hue, @"Restoring HUE Bulb " + d.Key.Name);
                        //Thread.Sleep(500);
                    }
                    catch (Exception ex)
                    {
                        Write.WriteConsole(ConsoleTypes.Hue,
                            "An Error occurred while restoring HUE Bulb " + d.Key.Name + ". Error: " + ex.Message);
                    }
            else
                Write.WriteConsole(ConsoleTypes.Hue, @"Unable to connect to HUE Hub.");
        }

        public async void HueUpdateState(BulbModeTypes mode, Color col, int transition)
        {
            var connect = await _client.CheckConnection();

            if (connect && _hueBulbs > 0)
            {
                if (_huEpendingUpdateColor != null)
                {
                    _huEpendingUpdateColorAction = () => HueUpdateState(mode, col, transition);
                    return;
                }

                var _transition = TimeSpan.FromMilliseconds(transition);
                var _col = new RGBColor(col.R, col.G, col.B);

                //ushort _hue = Convert.ToUInt16(col.GetHue());
                //ushort _sat = Convert.ToUInt16(col.GetSaturation());
                //ushort _bright = Convert.ToUInt16(col.GetBrightness());
                //ushort _kelvin = 2700;

                foreach (var d in _HueBulbsDat)
                    if (d.Value == mode || mode == BulbModeTypes.Unknown)
                    {
                        if (_HueStateMemory[d.Key.UniqueId] == 0) return;
                        var state = await _client.GetLightAsync(d.Key.Id); //Unsure if Id or UniqueId should be used

                        var command = new LightCommand();
                        command.On = true;
                        command.SetColor(_col, d.Key.ModelId);

                        var setColorTask =
                            _client.SendCommandAsync(command,
                                new List<string> {d.Key.Id}); //Unsure if Id or UniqueId should be used

                        var throttleTask = Task.Delay(50);
                        //Ensure task takes minimum 50 ms (no more than 20 messages per second)
                        _huEpendingUpdateColor = Task.WhenAll(setColorTask, throttleTask);
                    }

                _huEpendingUpdateColor = null;
                if (_huEpendingUpdateColorAction != null)
                {
                    var a = _huEpendingUpdateColorAction;
                    _huEpendingUpdateColorAction = null;
                    a();
                }
            }
        }

        public async void HueUpdateStateBrightness(BulbModeTypes mode, Color col, int? brightness, int transition)
        {
            var connect = await _client.CheckConnection();

            if (connect && _hueBulbs > 0)
            {
                if (_huEpendingUpdateColorBright != null)
                {
                    _huEpendingUpdateColorActionBright =
                        () => HueUpdateStateBrightness(mode, col, brightness, transition);
                    return;
                }

                var _transition = TimeSpan.FromMilliseconds(transition);
                var _col = new RGBColor(col.R, col.G, col.B);

                var hue = (int?) col.GetHue();
                var sat = (int?) col.GetSaturation();
                var bright = (int?) col.GetBrightness();

                //if (mode == 10) _kelvin = 6000;

                foreach (var d in _HueBulbsDat)
                    if (d.Value == mode || mode == BulbModeTypes.Unknown)
                    {
                        if (_HueStateMemory[d.Key.Id] == 0) return;

                        var state = await _client.GetLightAsync(d.Key.Id); //Unsure if Id or UniqueId should be used

                        var command = new LightCommand();
                        command.Hue = hue;
                        command.Saturation = sat;
                        command.Brightness = (byte) brightness;
                        command.On = true;
                        command.SetColor(_col, d.Key.ModelId);

                        var setColorTask =
                            _client.SendCommandAsync(command,
                                new List<string> {d.Key.Id}); //Unsure if Id or UniqueId should be used

                        var throttleTask = Task.Delay(50);
                        //Ensure task takes minimum 50 ms (no more than 20 messages per second)
                        _huEpendingUpdateColorBright = Task.WhenAll(setColorTask, throttleTask);
                    }

                _huEpendingUpdateColorBright = null;
                if (_huEpendingUpdateColorActionBright != null)
                {
                    var a = _huEpendingUpdateColorActionBright;
                    _huEpendingUpdateColorActionBright = null;
                    a();
                }
            }
        }

        public async Task<State> GetLightStateAsync(Light light)
        {
            var result = await _client.GetLightAsync(light.Id);
            return result.State;
        }

        public async Task<string> GetDeviceVersionAsync(Light light)
        {
            var result = await _client.GetLightAsync(light.Id);
            return result.SoftwareVersion;
        }

        public async void SetColorAsync(Light light, int? hue, int? saturation, int brightness, int? colorTemperature,
            TimeSpan ts)
        {
            var connect = await _client.CheckConnection();

            if (connect)
            {
                var command = new LightCommand();
                command.Brightness = (byte) brightness;
                command.ColorTemperature = colorTemperature;
                command.Hue = hue;
                command.On = true;
                command.Saturation = saturation;

                await _client.SendCommandAsync(command,
                    new List<string> {light.Id}); //Unsure if Id or UniqueId should be used
            }
            else
            {
                Write.WriteConsole(ConsoleTypes.Hue, @"Unable to connect to HUE Hub.");
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
                                HueUpdateState(BulbModeTypes.DutyFinder, basecol, 1000);
                                break;
                            }

                            if (_flash4Step == 0)
                            {
                                HueUpdateState(BulbModeTypes.DutyFinder, burstcol, 0);
                                _flash4Step = 1;
                            }
                            else if (_flash4Step == 1)
                            {
                                HueUpdateState(BulbModeTypes.DutyFinder, basecol, 0);
                                _flash4Step = 0;
                            }

                            Thread.Sleep(speed);
                        }
                }
            }
            catch
            {
                //
            }
        }


        #region setters

        public int HueBulbs
        {
            get => _hueBulbs;
            set => _hueBulbs = value;
        }

        public Dictionary<string, int> HueStateMemory => _HueStateMemory;

        public Dictionary<string, BulbModeTypes> HueModeMemory => _HueModeMemory;

        public Dictionary<string, Light> HueDevices => _HueDevices;

        public Dictionary<Light, BulbModeTypes> HueBulbsDat => _HueBulbsDat;

        public Dictionary<Light, State> HueBulbsRestore => _HueBulbsRestore;

        #endregion
    }
}