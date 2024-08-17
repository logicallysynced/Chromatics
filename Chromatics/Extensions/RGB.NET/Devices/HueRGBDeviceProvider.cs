using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HueApi;
using RGB.NET.Core;
using RGBColor = HueApi.ColorConverters.RGBColor;
using HueApi.Models;
using HueApi.Models.Clip;
using System.Windows.Controls;
using Chromatics.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue
{
    public class HueRGBDeviceProvider : AbstractRGBDeviceProvider
    {
        #region Constructors

        public HueRGBDeviceProvider()
        {
            if (_instance != null) ThrowHueError(1, true, $"There can be only one instance of type {nameof(HueRGBDeviceProvider)}");
            _instance = this;
        }

        #endregion

        #region Properties & Fields

        private static HueRGBDeviceProvider _instance;

        public static HueRGBDeviceProvider Instance => _instance ?? new HueRGBDeviceProvider();
        public List<HueClientDefinition> ClientDefinitions { get; } = new();

        #endregion

        #region Methods

        private void ThrowHueError(int errorCode, bool isCritical, string message = null) => Throw(new Exception(message), isCritical);

        protected override void InitializeSDK()
        {
            // Each client definition has its own connection initialized in LoadDevices
        }

        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            return LoadDevicesAsync().GetAwaiter().GetResult();
        }

        private async Task<IEnumerable<IRGBDevice>> LoadDevicesAsync()
        {
            List<IRGBDevice> devices = new List<IRGBDevice>();
            var appSettings = AppSettings.GetSettings();

            foreach (HueClientDefinition clientDefinition in ClientDefinitions)
            {
                try
                {
                    // Initialize the Hue client with the IP and App Key
                    
                    if (appSettings.deviceHueBridgeClientKey == null || appSettings.deviceHueBridgeClientKey == "")
                    {
                        RegisterEntertainmentResult regResult = null;

                        try
                        {
                            regResult = await LocalHueApi.RegisterAsync(clientDefinition.Ip, clientDefinition.AppKey, "RGB.NET");
                        }
                        catch (HueApi.Models.Exceptions.LinkButtonNotPressedException ex)
                        {
                            ThrowHueError(99, true, $"[Hue] Button must be pressed on Hue Bridge. Please press the button and restart Chromatics.");
                            break;
                        }

                        if (regResult != null)
                        {
                            appSettings.deviceHueBridgeClientKey = regResult.Username;
                            AppSettings.SaveSettings(appSettings);
                        }

                    }


                    if (appSettings.deviceHueBridgeClientKey != null && appSettings.deviceHueBridgeClientKey != "")
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Registered with Hue Bridge: {appSettings.deviceHueBridgeClientKey}");

                        var localHueApi = new LocalHueApi(clientDefinition.Ip, appSettings.deviceHueBridgeClientKey);

                        // Discover lights on the bridge
                        var lights = await localHueApi.GetLightsAsync();

                        foreach (var light in lights.Data)
                        {
                            try
                            {
                                Debug.WriteLine($"Found Light: {light.Metadata.Name}");

                                // Create the device info and device objects
                                HueDeviceInfo deviceInfo = new HueDeviceInfo(light);
                                HueDevice device = new HueDevice(deviceInfo, new HueUpdateQueue(GetUpdateTrigger(), light.IdV1, localHueApi));
                                devices.Add(device);
                            }
                            catch (Exception ex)
                            {
                                ThrowHueError(8, true, $"[Hue] Error setting up device for light {light.Id}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ThrowHueError(9, true, $"[Hue] Unhandled error loading devices for client {clientDefinition.Ip}: {ex.Message}");
                }
            }

            return devices;
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
        {
            return new HueDeviceUpdateTrigger();
        }

        #endregion
    }

    

    
}
