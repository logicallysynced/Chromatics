using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
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
        private volatile bool _shuttingDown;
        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // Rate-limit error logs GLOBALLY across every HueUpdateQueue
        // instance. The bridge replies with HTML (e.g. an error page) when
        // we exceed its ~10 req/s budget; that breaks JSON parsing and
        // would otherwise spam Logger every tick. Per-bulb rate limiting
        // produced N logs per window on multi-bulb setups; making the
        // throttle static means the user sees at most one line per window
        // total, regardless of how many bulbs are connected.
        private static DateTime _lastJsonErrorLog = DateTime.MinValue;
        private static DateTime _lastGenericErrorLog = DateTime.MinValue;
        private static readonly System.Threading.Lock _errorLogLock = new();
        private static readonly TimeSpan ErrorLogInterval = TimeSpan.FromSeconds(15);

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

        // Set by the provider before sending TurnOff so any in-flight or queued
        // Update() calls from the trigger thread become no-ops. Without this, the
        // trigger could fire one more color update for some bulbs after we've
        // already sent TurnOff, racing the bridge into the wrong final state.
        public void BeginShutdown() => _shuttingDown = true;

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _perDeviceBrightness = correction;

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
                // Provider has begun teardown — drop the update so it can't race
                // ahead of (or behind) the explicit TurnOff sequence.
                if (_shuttingDown) return true;

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

                    // Start brightness at the legacy hard cap (0-100) when the
                    // user has set one, otherwise start at full and let the
                    // slider multipliers below drive the whole value.
                    //
                    // Previously this path did `brightness = color.A * 100`
                    // which looked sensible but broke the slider: several
                    // RGB.NET brush / decorator paths leave `color.A` at 0
                    // while still painting a visible R/G/B (Hue only sees xy
                    // anyway, so brush code doesn't bother setting alpha).
                    // A=0 → brightness=0 → the `brightness <= 0` guard below
                    // hits TurnOff() every tick, and the sliders have nothing
                    // to modulate. Starting at 100 makes the slider the
                    // authoritative source of bulb luminance.
                    double brightness;
                    if (appSettings.deviceHueBridgeBrightness == -1)
                    {
                        brightness = 100.0;
                    }
                    else
                    {
                        brightness = Math.Clamp(appSettings.deviceHueBridgeBrightness, 0, 100);
                    }

                    // Hue maps RGB to xy chromaticity + a separate brightness value,
                    // so the IColorCorrection chain (which scales R/G/B uniformly)
                    // doesn't change the bulb's perceived brightness on its own —
                    // xy is invariant to uniform RGB scaling. Apply both the
                    // global multiplier and the per-device multiplier to the
                    // bridge brightness here so the slider drives bulb luminance.
                    int globalPct = GlobalBrightnessCorrection.Instance.BrightnessPercent;
                    int perDevicePct = _perDeviceBrightness?.BrightnessPercent ?? 100;
                    brightness *= globalPct / 100.0;
                    brightness *= perDevicePct / 100.0;

                    bool isBlack = color.R == 0 && color.G == 0 && color.B == 0;

                    // Bridge firmware rejects on:true with brightness 0 — minimum
                    // brightness in CLIP v2 is ~1%. Send an explicit off instead so
                    // black LEDs actually turn the bulb off rather than snapping to
                    // minimum brightness or returning an error. Clamp to [1, 100]
                    // otherwise: the bridge silently rejects fractional values
                    // below 1%, which at very-low slider values would manifest as
                    // "bulb stays at last-seen brightness" (the slider looks dead).
                    UpdateLight req;
                    if (isBlack || brightness <= 0)
                    {
                        req = new UpdateLight().TurnOff();
                    }
                    else
                    {
                        double finalBrightness = Math.Clamp(brightness, 1.0, 100.0);

                        // Set the request fields DIRECTLY instead of via
                        // SetBrightness/SetColor extension methods so we can
                        // see the exact payload being assembled. The extension
                        // methods each do `req.Dimming = new Dimming{...}` or
                        // `req.Color = new ColorInfo{...}` and return the req;
                        // inlining removes any chance of extension-method
                        // ordering or return-value discarding hiding a bug.
                        req = new UpdateLight
                        {
                            On = new On { IsOn = true },
                            Dynamics = new Dynamics { Speed = 0 },
                            Dimming = new Dimming { Brightness = finalBrightness },
                        };

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
                        LogJsonError(jsonEx.Message);
                    }
                    catch (AggregateException aggEx)
                    {
                        foreach (var innerEx in aggEx.InnerExceptions)
                        {
                            if (innerEx is JsonException)
                            {
                                LogJsonError(innerEx.Message);
                            }
                            else
                            {
                                LogGenericError(innerEx.Message);
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

        // Rate-limited error logging. The bridge can return HTML error
        // pages on every request when over its req/s budget; without
        // throttling we'd flood Logger with one line per tick (~20/sec).
        // Static + locked so every HueUpdateQueue instance shares the
        // same 15s window — a user with 4 bulbs shouldn't see 4 lines
        // per window.
        private static void LogJsonError(string message)
        {
            lock (_errorLogLock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastJsonErrorLog < ErrorLogInterval) return;
                _lastJsonErrorLog = now;
                Logger.WriteConsole(LoggerTypes.Error,
                    $"[Hue] JSON Exception (suppressed for {ErrorLogInterval.TotalSeconds:F0}s): {message}");
            }
        }

        private static void LogGenericError(string message)
        {
            lock (_errorLogLock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastGenericErrorLog < ErrorLogInterval) return;
                _lastGenericErrorLog = now;
                Logger.WriteConsole(LoggerTypes.Error,
                    $"[Hue] Exception (suppressed for {ErrorLogInterval.TotalSeconds:F0}s): {message}");
            }
        }

        #endregion
    }
}
