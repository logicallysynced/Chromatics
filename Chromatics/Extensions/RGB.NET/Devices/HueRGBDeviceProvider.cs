using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using NLog.Common;
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
            if (_instance != null) throw new InvalidOperationException($"There can be only one instance of type {nameof(HueRGBDeviceProvider)}");
            _instance = this;
        }

        #endregion

        #region Properties & Fields

        private static HueRGBDeviceProvider _instance;

        public static HueRGBDeviceProvider Instance => _instance ?? new HueRGBDeviceProvider();
        public List<HueClientDefinition> ClientDefinitions { get; } = new();

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override void InitializeSDK()
        {
            // Each client definition has its own connection initialized in LoadDevices
        }

        /// <inheritdoc />
        protected override IEnumerable<IRGBDevice> LoadDevices()
        {
            HueDeviceUpdateTrigger updateTrigger = (HueDeviceUpdateTrigger)GetUpdateTrigger();

            foreach (HueClientDefinition clientDefinition in ClientDefinitions)
            {
                // Create a temporary for this definition 
                ILocalHueClient client = new LocalHueClient(clientDefinition.Ip);
                client.Initialize(clientDefinition.AppKey);

                // Get the entertainment groups, no point continuing without any entertainment groups
                IReadOnlyList<Group> entertainmentGroups = AsyncHelper.RunSync(client.GetEntertainmentGroups);
                if (!entertainmentGroups.Any())
                    continue;

                // Get all lights once, all devices can use this list to identify themselves
                List<Light> lights = AsyncHelper.RunSync(client.GetLightsAsync).ToList();

                foreach (Group entertainmentGroup in entertainmentGroups.OrderBy(g => int.Parse(g.Id)))
                {
                    StreamingHueClient streamingClient = new(clientDefinition.Ip, clientDefinition.AppKey, clientDefinition.ClientKey);
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
        }

        /// <inheritdoc />
        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
        {
            return new HueDeviceUpdateTrigger();
        }

        #endregion
    }
}
