using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Models;
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
        private SettingsModel appSettings;

        #endregion

        #region Constructors

        public HueUpdateQueue(IDeviceUpdateTrigger updateTrigger, Light light, LocalHueApi client)
            : base(updateTrigger)
        {
            _client = client;
            // The provider has already enumerated lights asynchronously before
            // constructing us, so take the Light directly. The old shape blocked
            // on GetAllAsync().Result inside the constructor, which could deadlock
            // if the Hue bridge was unreachable.
            _light = light;
            appSettings = AppSettings.GetSettings();
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

                // RGB.NET occasionally flushes an empty dataSet (device initialised
                // with no LEDs mapped yet, or a mid-reconnect tick). Indexing into
                // an empty span would throw IndexOutOfRange on the caller thread.
                if (dataSet.IsEmpty) return true;

                Color color = dataSet[0].color;
                var rgbColorHue = new HueApi.ColorConverters.RGBColor(color.R, color.G, color.B);
                double brightness = 100;

                if (appSettings.deviceHueBridgeBrightness == -1)
                {
                    brightness = color.A * 100;
                }
                else
                {
                    if (appSettings.deviceHueBridgeBrightness < -1)
                    {
                        brightness = 0;
                    }
                    else if (appSettings.deviceHueBridgeBrightness > 100)
                    {
                        brightness = 100;
                    }
                    else
                    {
                        brightness = appSettings.deviceHueBridgeBrightness;
                    }
                }

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
                        var result = _client.Light.UpdateAsync(_light.Id, req).GetAwaiter().GetResult(); // Execute the async method synchronously
                    }
                    catch (JsonException aggEx)
                    {
                        Logger.WriteConsole(LoggerTypes.Error, $"[Hue] JSON Exception: {aggEx.Message}");
                    }
                    catch (AggregateException aggEx)
                    {
                        foreach (var innerEx in aggEx.InnerExceptions)
                        {
                            if (innerEx is JsonException)
                            {
                                Logger.WriteConsole(LoggerTypes.Error, $"[Hue] JSON Exception: {aggEx.Message}");
                            }
                            else
                            {
                                Logger.WriteConsole(LoggerTypes.Error, $"[Hue] Exception: {innerEx.Message}");
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
