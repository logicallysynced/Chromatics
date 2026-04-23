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
                        catch (HueApi.Models.Exceptions.LinkButtonNotPressedException)
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

                        // CLIP v2 splits Device from Light: the hardware model id (used by
                        // HueDevice's per-bulb layout switch and by HueUpdateQueue's gamut
                        // calculation) lives on Device.ProductData.ModelId, not Light. Each
                        // Light has Owner.Rid pointing to its parent Device, so we fetch
                        // devices once and build a Guid→modelId map for the join.
                        Dictionary<Guid, string> modelIdByDeviceId = new();
                        try
                        {
                            var devicesResponse = await localHueApi.Device.GetAllAsync();
                            foreach (var d in devicesResponse.Data)
                                modelIdByDeviceId[d.Id] = d.ProductData?.ModelId ?? "";
                        }
                        catch (Exception ex)
                        {
                            // Non-fatal — falls back to light.Type below, which only loses
                            // accurate per-bulb layout/gamut. The bridge would be non-CLIP-v2
                            // for this to fail, which is unsupported anyway.
                            Logger.WriteConsole(Enums.LoggerTypes.Error, $"[Hue] Failed to enumerate devices for model id lookup: {ex.Message}");
                        }

                        // Discover lights on the bridge
                        var lights = await localHueApi.Light.GetAllAsync();

                        foreach (var light in lights.Data)
                        {
                            try
                            {
                                string modelId = "";
                                if (light.Owner != null && modelIdByDeviceId.TryGetValue(light.Owner.Rid, out var m))
                                    modelId = m;

                                HueDeviceInfo deviceInfo = new HueDeviceInfo(light, modelId);
                                HueDevice device = new HueDevice(deviceInfo, new HueUpdateQueue(GetUpdateTrigger(), light, modelId, localHueApi));
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

        // RGBController.UnloadDeviceProvider calls provider.Dispose(), but the
        // singleton's static _instance still pointed at the disposed object —
        // any later access (HueBridgeDialog reconnect, Settings re-toggle,
        // surface.Attach iterating provider.Devices) hit ObjectDisposedException
        // because AbstractRGBDeviceProvider's internal state checks throw on
        // Devices/etc. once disposed. Clearing _instance lets the next Instance
        // access construct a fresh provider.
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (ReferenceEquals(_instance, this))
                _instance = null;
        }

        #endregion
    }

    

    
}
