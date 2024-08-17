using HueApi;
using HueApi.ColorConverters.Original.Extensions;
using HueApi.Models;
using HueApi.Models.Requests;
using RGB.NET.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue
{
    public class HueUpdateQueue : UpdateQueue
    {
        #region Properties & Fields

        private readonly Light _light;
        private readonly LocalHueApi _client;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        #endregion

        #region Constructors

        public HueUpdateQueue(IDeviceUpdateTrigger updateTrigger, string lightId, LocalHueApi client)
            : base(updateTrigger)
        {
            _client = client;
            _light = _client.GetLightsAsync().Result.Data.FirstOrDefault(l => l.IdV1 == lightId);
        }

        #endregion

        #region Methods

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            _semaphore.WaitAsync().GetAwaiter().GetResult(); // Wait asynchronously within the synchronous context
            try
            {
                if (_light == null)
                {
                    HueRGBDeviceProvider.Instance.Throw(new Exception("Light not found."));
                    return false;
                }

                Color color = dataSet[0].color;
                var rgbColorHue = new HueApi.ColorConverters.RGBColor(color.R, color.G, color.B);
                var brightness = color.A * 100;

                if (color.R == 0 && color.G == 0 && color.B == 0)
                {
                    brightness = 0;
                }

                // Create the light update command
                var req = new UpdateLight()
                    .SetSpeed(0)
                    .SetDuration(250)
                    .TurnOn()
                    .SetBrightness(brightness)
                    .SetColor(rgbColorHue);

                // Send the update command to the light
                if (req != null)
                {
                    try
                    {
                        var result = _client.UpdateLightAsync(_light.Id, req).GetAwaiter().GetResult(); // Execute the async method synchronously
                    }
                    catch (JsonException aggEx)
                    {
                        Debug.WriteLine($"JSON Exception: {aggEx.Message}");
                    }
                    catch (AggregateException aggEx)
                    {
                        foreach (var innerEx in aggEx.InnerExceptions)
                        {
                            if (innerEx is JsonException)
                            {
                                Debug.WriteLine($"JSON Exception: {aggEx.Message}");
                            }
                            else
                            {
                                // Handle other types of exceptions
                                Debug.WriteLine($"Other Exception: {innerEx.Message}");
                                HueRGBDeviceProvider.Instance.Throw(innerEx);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                HueRGBDeviceProvider.Instance.Throw(ex);
            }
            finally
            {
                _semaphore.Release(); // Ensure the semaphore is always released
            }

            return false;
        }

        #endregion
    }
}
