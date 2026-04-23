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
        // Hardware model id (e.g. "LCA001") used by HueColorConverter to pick the
        // correct gamut triangle when computing xy chromaticity from RGB. Default
        // gamut "LCT001" (used when this is empty) only matches first-gen A19
        // bulbs — modern wide-gamut lights (LCA series, light strips, etc.) get
        // visibly desaturated/wrong colors without their real model id.
        private readonly string _modelId;
        private readonly Lock _lock = new();

        #endregion

        #region Constructors

        public HueUpdateQueue(IDeviceUpdateTrigger updateTrigger, Light light, string modelId, LocalHueApi client)
            : base(updateTrigger)
        {
            _client = client;
            // The provider has already enumerated lights asynchronously before
            // constructing us, so take the Light directly. The old shape blocked
            // on GetAllAsync().Result inside the constructor, which could deadlock
            // if the Hue bridge was unreachable.
            _light = light;
            _modelId = modelId ?? "";
        }

        #endregion

        #region Methods

        // Called by HueRGBDeviceProvider.Dispose (inside Task.Run) before the
        // trigger/client are torn down so the bridge returns to a known-off state
        // on provider unload or app close. Callers are responsible for swallowing
        // exceptions so a dead bridge never blocks shutdown.
        public Task TurnOffAsync()
        {
            if (_light == null || _client == null) return Task.CompletedTask;
            try
            {
                return _client.Light.UpdateAsync(_light.Id, new UpdateLight().TurnOff());
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            // Update() is called sequentially by the trigger thread, but guard
            // anyway in case the trigger is shared by multiple queues.
            lock (_lock)
            {
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

                    // Refresh per-tick: the user can adjust bridge brightness in
                    // Settings while Chromatics is running. A snapshot at construction
                    // time would never reflect those changes.
                    var appSettings = AppSettings.GetSettings();

                    Color color = dataSet[0].color;
                    var rgbColorHue = new HueApi.ColorConverters.RGBColor(color.R, color.G, color.B);
                    double brightness;

                    if (appSettings.deviceHueBridgeBrightness == -1)
                    {
                        brightness = color.A * 100;
                    }
                    else if (appSettings.deviceHueBridgeBrightness < 0)
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

                    bool isBlack = color.R == 0 && color.G == 0 && color.B == 0;

                    // Bridge firmware rejects on:true with brightness 0 — minimum
                    // brightness in CLIP v2 is ~1%. Send an explicit off instead so
                    // black LEDs actually turn the bulb off rather than snapping to
                    // minimum brightness or returning an error.
                    UpdateLight req;
                    if (isBlack || brightness <= 0)
                    {
                        req = new UpdateLight().TurnOff();
                    }
                    else
                    {
                        req = new UpdateLight()
                            .SetSpeed(0)
                            .TurnOn()
                            .SetBrightness(brightness);

                        if (!string.IsNullOrEmpty(_modelId))
                            req.SetColor(rgbColorHue, _modelId);
                        else
                            req.SetColor(rgbColorHue);
                    }

                    try
                    {
                        _client.Light.UpdateAsync(_light.Id, req).GetAwaiter().GetResult();
                    }
                    catch (JsonException jsonEx)
                    {
                        Logger.WriteConsole(LoggerTypes.Error, $"[Hue] JSON Exception: {jsonEx.Message}");
                    }
                    catch (AggregateException aggEx)
                    {
                        foreach (var innerEx in aggEx.InnerExceptions)
                        {
                            if (innerEx is JsonException)
                            {
                                Logger.WriteConsole(LoggerTypes.Error, $"[Hue] JSON Exception: {innerEx.Message}");
                            }
                            else
                            {
                                Logger.WriteConsole(LoggerTypes.Error, $"[Hue] Exception: {innerEx.Message}");
                                HueRGBDeviceProvider.Instance.Throw(innerEx);
                            }
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    HueRGBDeviceProvider.Instance.Throw(ex);
                    return false;
                }
            }
        }

        #endregion
    }
}
