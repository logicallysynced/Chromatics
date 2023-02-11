using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms.Design;
using Chromatics.Core;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Groups;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Models;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue;

public class HueRGBDeviceProvider : AbstractRGBDeviceProvider
{
    #region Constructors

    public HueRGBDeviceProvider()
    {
        if (_instance != null) throw new InvalidOperationException($"There can be only one instance of type {nameof(HueRGBDeviceProvider)}");
        _instance = this;
    }

    #endregion

    #region Properties & Fields

    private static HueRGBDeviceProvider _instance;

    public static HueRGBDeviceProvider Instance => _instance ?? new HueRGBDeviceProvider();
    public List<HueClientDefinition> ClientDefinitions { get; } = new();

    private string bridgeIP;
    private string bridgeAppKey;
    private string clientKey;
    private bool init;

    #endregion

    #region Methods

    /// <inheritdoc />
    protected override void InitializeSDK()
    {
        // Each client definition has its own connection initialized in LoadDevices
        var appSettings = AppSettings.GetSettings();

        bridgeIP = appSettings.deviceHueBridgeIP;
        bridgeAppKey = appSettings.deviceHueBridgeKey;
        clientKey = appSettings.deviceHueBridgeStreamingKey;
                
        if (bridgeIP == "" || bridgeIP == "127.0.0.1")
        {
            Logger.WriteConsole(Enums.LoggerTypes.Devices, @"Looking for Hue bridges for 10 seconds..");
            var discovery = HueBridgeDiscovery.FastDiscoveryAsync(new TimeSpan(0,0,10)).GetAwaiter().GetResult();

            if (discovery.Count > 0)
            {
                bridgeIP = discovery.FirstOrDefault().IpAddress;
                appSettings.deviceHueBridgeIP = bridgeIP;
                AppSettings.SaveSettings(appSettings);
            }
            else
            {
                Logger.WriteConsole(Enums.LoggerTypes.Devices, @"No Hue bridges can be discovered.");
                _instance.Dispose();
                return;
            }
        }
        
        var client = new LocalHueClient(bridgeIP);
        client.Initialize(bridgeAppKey);

        bool success = client.CheckConnection().GetAwaiter().GetResult();
        if (!success)
        {
            Logger.WriteConsole(Enums.LoggerTypes.Error, $"Failed to connect to Hue bridge at {bridgeIP}.");
            _instance.Dispose();
            return;
        }
        else
        {
            if (bridgeAppKey == "" || clientKey == "")
            {
                try
                {
                    var regResult = client.RegisterAsync(bridgeIP, "Chromatics", true).GetAwaiter().GetResult();
                    bridgeAppKey = regResult.Username;
                    clientKey = regResult.StreamingClientKey;

                    appSettings.deviceHueBridgeKey = bridgeAppKey;
                    appSettings.deviceHueBridgeStreamingKey = clientKey;
                    AppSettings.SaveSettings(appSettings);
                }
                catch
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"Hue bridge at {bridgeIP} needs to be registered. Please push the button on the hub and restart Chromatics!");
                    _instance.Dispose();
                    return;
                }
            }
            
            init = true;

        }
        
    }

    /// <inheritdoc />
    protected override IEnumerable<IRGBDevice> LoadDevices()
    {
        if (!init) yield break;

        HueDeviceUpdateTrigger updateTrigger = (HueDeviceUpdateTrigger) GetUpdateTrigger();
        
        // Create a temporary for this definition 
        var client = new LocalHueClient(bridgeIP);
        client.Initialize(bridgeAppKey);

        // Get the entertainment groups, no point continuing without any entertainment groups
        IReadOnlyList<Group> entertainmentGroups = AsyncHelper.RunSync(client.GetEntertainmentGroups);

        // Get all lights once, all devices can use this list to identify themselves
        List<Light> lights = AsyncHelper.RunSync(client.GetLightsAsync).ToList();

        foreach (Group entertainmentGroup in entertainmentGroups.OrderBy(g => int.Parse(g.Id)))
        {
            StreamingHueClient streamingClient = new(bridgeIP, bridgeAppKey, clientKey);
            StreamingGroup streamingGroup = new(entertainmentGroup.Locations);
            AsyncHelper.RunSync(async () => await streamingClient.Connect(entertainmentGroup.Id));

            updateTrigger.ClientGroups.Add(streamingClient, streamingGroup);
            foreach (string lightId in entertainmentGroup.Lights.OrderBy(int.Parse))
            {
                HueDeviceInfo deviceInfo = new(entertainmentGroup, lightId, lights);
                HueDevice device = new(deviceInfo, new HueUpdateQueue(updateTrigger, lightId, streamingGroup));
                yield return device;
            }
        }
    }

    /// <inheritdoc />
    protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
    {
        return new HueDeviceUpdateTrigger();
    }

    #endregion
}