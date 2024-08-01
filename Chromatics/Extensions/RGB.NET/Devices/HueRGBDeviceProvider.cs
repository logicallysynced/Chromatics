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
            HueDeviceUpdateTrigger updateTrigger = (HueDeviceUpdateTrigger)GetUpdateTrigger();
            List<IRGBDevice> devices = new List<IRGBDevice>();

            foreach (HueClientDefinition clientDefinition in ClientDefinitions)
            {
                try
                {
                    ILocalHueClient client = new LocalHueClient(clientDefinition.Ip);
                    client.Initialize(clientDefinition.AppKey);

                    if (!client.CheckConnection().GetAwaiter().GetResult())
                    {
                        ThrowHueError(1, true, $"[Hue] Unable to connect to Hue hub at {clientDefinition.Ip}");
                        break;
                    }

                    var results = client.GetEntertainmentGroups();
                    IReadOnlyList<Group> entertainmentGroups = null;

                    try
                    {
                        var awaiter = results.GetAwaiter();
                        entertainmentGroups = awaiter.GetResult();
                    }
                    catch (HttpRequestException ex)
                    {
                        ThrowHueError(2, true, $"[Hue] HTTP request error: {ex.Message}");
                        if (ex.InnerException is SocketException socketEx)
                        {
                            ThrowHueError(3, true, $"SocketException: {ex.Message}");
                        }
                    }
                    catch (AggregateException ex)
                    {
                        ThrowHueError(4, true, $"[Hue] AggregateException: {ex.Message}");
                        foreach (var innerException in ex.InnerExceptions)
                        {
                            ThrowHueError(5, true, $"[Hue] Inner Exception: {innerException.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ThrowHueError(6, true, $"[Hue] Exception: {ex.Message}");
                    }

                    
                    if (entertainmentGroups == null)
                    {
                        if (!entertainmentGroups.Any())
                        {
                            ThrowHueError(7, true, $"[Hue] No entertainment groups found for IP: {clientDefinition.Ip}");
                            continue;
                        }

                        List<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult().ToList();

                        foreach (Group entertainmentGroup in entertainmentGroups.OrderBy(g => int.Parse(g.Id)))
                        {
                            try
                            {
                                StreamingHueClient streamingClient = new(clientDefinition.Ip, clientDefinition.AppKey, clientDefinition.ClientKey);
                                StreamingGroup streamingGroup = new(entertainmentGroup.Locations);

                                streamingClient.Connect(entertainmentGroup.Id).GetAwaiter().GetResult();

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
                    
                    
                }
                catch (Exception ex)
                {
                    ThrowHueError(9, true, $"[Hue] Unhandled error loading devices for client {clientDefinition.Ip}: {ex.Message}");
                }
            }

            foreach (var device in devices)
            {
                yield return device;
            }
        }

        protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
        {
            return new HueDeviceUpdateTrigger();
        }

        #endregion
    }
}
