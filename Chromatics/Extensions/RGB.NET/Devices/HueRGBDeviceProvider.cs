using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Groups;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Models;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices
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
            HueDeviceUpdateTrigger updateTrigger = (HueDeviceUpdateTrigger)GetUpdateTrigger();
            List<IRGBDevice> devices = new List<IRGBDevice>();

            foreach (HueClientDefinition clientDefinition in ClientDefinitions)
            {
                try
                {
                    ILocalHueClient client = new LocalHueClient(clientDefinition.Ip);
                    client.Initialize(clientDefinition.AppKey);

                    IReadOnlyList<Group> entertainmentGroups = null;
                    try
                    {
                        entertainmentGroups = await client.GetEntertainmentGroups();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Hue] Exception caught while getting entertainment groups for IP {clientDefinition.Ip}: {ex.Message}");
                        ThrowHueError(10, true, $"[Hue] Error getting entertainment groups for IP {clientDefinition.Ip}: {ex.Message}");
                        continue; // Skip to the next clientDefinition
                    }

                    if (entertainmentGroups == null || !entertainmentGroups.Any())
                    {
                        ThrowHueError(7, true, $"[Hue] No entertainment groups found for IP: {clientDefinition.Ip}");
                        continue;
                    }

                    List<Light> lights = (await client.GetLightsAsync()).ToList();

                    foreach (Group entertainmentGroup in entertainmentGroups.OrderBy(g => int.Parse(g.Id)))
                    {
                        try
                        {
                            StreamingHueClient streamingClient = new(clientDefinition.Ip, clientDefinition.AppKey, clientDefinition.ClientKey);
                            StreamingGroup streamingGroup = new(entertainmentGroup.Locations);

                            await streamingClient.Connect(entertainmentGroup.Id);

                            updateTrigger.ClientGroups.Add(streamingClient, streamingGroup);
                            foreach (string lightId in entertainmentGroup.Lights.OrderBy(int.Parse))
                            {
                                HueDeviceInfo deviceInfo = new(entertainmentGroup, lightId, lights);
                                HueDevice device = new(deviceInfo, new HueUpdateQueue(updateTrigger, lightId, streamingGroup));
                                devices.Add(device);
                            }
                        }
                        catch (Exception ex)
                        {
                            ThrowHueError(8, true, $"[Hue] Error setting up streaming group for group {entertainmentGroup.Id}: {ex.Message}");
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
