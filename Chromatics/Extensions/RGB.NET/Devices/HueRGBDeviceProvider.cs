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
            // 100ms per bulb = 10 Hz. Without an explicit UpdateFrequency
            // the trigger's Thread.Sleep throttle is skipped (gated on
            // UpdateFrequency > 0) and every decorator tick produces an
            // HTTP request, which blows past the bridge's ~10 req/s budget
            // and causes it to reply with HTML error pages. JSON parsing
            // then chokes and the `[Hue] JSON Exception` log fires. 10Hz/bulb
            // keeps a single-bulb setup at the limit and multi-bulb setups
            // at a manageable overshoot that the bridge usually absorbs.
            return new HueDeviceUpdateTrigger(0.1);
        }

        // Before tearing down the provider, send TurnOff to every bulb so the
        // bridge returns to a clean off state when the user disables Hue in
        // Settings or closes Chromatics.
        //
        // Sequential, not parallel: the Hue bridge throttles concurrent CLIP v2
        // PUTs aggressively (and the HueApi LocalHueApi instance is shared across
        // all bulbs), so firing N requests at once silently dropped all but the
        // first one. Sequential with a short pacing delay matches the bridge's
        // ~10 req/s budget and gets every bulb.
        //
        // All work runs inside Task.Run so HueApi's HTTP continuations don't
        // capture the Avalonia dispatcher (which the outer .Wait would block).
        // Per-bulb timeout is 2s; total budget grows with device count, capped
        // so a dead bridge can't hang shutdown forever.
        //
        // After the off-pass, clearing _instance lets the next Instance access
        // construct a fresh provider (prevents ObjectDisposedException on
        // HueBridgeDialog reconnect or Settings re-toggle).
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    var devices = Devices.OfType<HueDevice>().ToList();
                    if (devices.Count > 0)
                    {
                        // Step 1: gate every queue so the trigger thread stops sending
                        // colour updates before we issue TurnOff. Without this gate the
                        // trigger could fire one more Update() per bulb after we've
                        // already sent the off command, racing the bridge into the
                        // wrong final state — which manifested as "sometimes only one
                        // bulb actually turned off".
                        foreach (var d in devices)
                            d.BeginShutdown();

                        int totalBudgetSec = Math.Min(15, 2 + devices.Count);
                        Task.Run(async () =>
                        {
                            // Step 2: brief grace window so any Update() that was already
                            // mid-flight (holding _lock) finishes its HTTP send before we
                            // start TurnOff. New Update() calls hit the _shuttingDown
                            // gate and return immediately.
                            await Task.Delay(150).ConfigureAwait(false);

                            // Step 3: sequential TurnOff with pacing — Hue bridge throttles
                            // concurrent CLIP v2 PUTs and will silently drop most of them
                            // when fired in parallel.
                            foreach (var d in devices)
                            {
                                try
                                {
                                    var off = d.TurnOffAsync();
                                    var completed = await Task.WhenAny(off, Task.Delay(2000)).ConfigureAwait(false);
                                    if (completed != off)
                                        Logger.WriteConsole(Enums.LoggerTypes.Devices, $"[Hue] TurnOff timed out for {d.DeviceInfo.DeviceName}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"[Hue] TurnOff failed for {d.DeviceInfo.DeviceName}: {ex.Message}");
                                }
                                await Task.Delay(150).ConfigureAwait(false);
                            }
                        }).Wait(TimeSpan.FromSeconds(totalBudgetSec));
                    }
                }
                catch { }
            }

            base.Dispose(disposing);

            if (ReferenceEquals(_instance, this))
                _instance = null;
        }

        #endregion
    }

    

    
}
