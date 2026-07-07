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
        // Set true while the device is disabled in the Mapping tab so any
        // buffered LED data that arrives via OnUpdate after surface.Detach
        // gets dropped instead of racing the restore-to-original UpdateAsync.
        // Cleared on re-enable in RGBController.AddDevice.
        private volatile bool _perDeviceDisable;
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

        // Bridge-side interpolation window between successive color updates.
        // Without this, every Update() snaps the bulb to the new color
        // instantly and rapid colour changes (Vegas effect, cutscene
        // animation, weather transitions) read as strobe-like pops on
        // bulbs that lack hardware fade. Hue Play has a built-in fade in
        // firmware so it looks fine without; software lights (LST, LCA,
        // E27 colour) need the bridge to do the interpolation for them.
        //
        // 150ms is 1.5x the per-bulb update interval (100ms — see
        // CreateUpdateTrigger) so successive frames overlap and the
        // bridge retargets mid-fade, producing a continuous gradient
        // rather than a fade-then-hold staircase. Going higher feels
        // laggy on dynamic effects; going lower stops smoothing rapid
        // changes since the fade completes between frames.
        private const int FadeDurationMs = 150;

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

        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _perDeviceBrightness = correction;

        // Called by HueRGBDeviceProvider.Dispose (inside Task.Run) before the
        // trigger/client are torn down so the bridge returns to a known-off state
        // on provider unload or app close. Callers are responsible for swallowing
        // exceptions so a dead bridge never blocks shutdown.
        //
        // Kept for source-history continuity; new callers should prefer
        // RestoreOriginalStateAsync below, which restores the full pre-
        // Chromatics state (colour, brightness, on/off) instead of
        // unconditionally turning the bulb off.
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

        // Captured state from before Chromatics first painted the bulb.
        // We store the live Light object directly because Hue's CLIP v2
        // response types (Color, Dimming, On, ColorTemperature) implement
        // the same IUpdate* interfaces UpdateLight expects, so we can
        // round-trip them straight through without translating to and from
        // an intermediate struct. Restored on disable / dispose / per-
        // device disable in the Mapping tab so the user returns to
        // whatever they had pre-adoption.
        private Light _capturedLight;

        // Snapshot of the bulb's current state before the trigger ramps up.
        // Three GET attempts with backoff: a single failed GET at adoption
        // (bridge busy during the provider's burst of per-bulb calls) used
        // to leave _capturedLight null, which made the shutdown restore a
        // silent no-op for that bulb. Failures after the retries are logged
        // but non-fatal.
        public async Task CaptureOriginalStateAsync(CancellationToken ct = default)
        {
            if (_light == null || _client == null) return;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var resp = await _client.Light.GetByIdAsync(_light.Id).ConfigureAwait(false);
                    if (resp?.Data is { Count: > 0 })
                    {
                        _capturedLight = resp.Data[0];
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == 2)
                    {
                        Logger.WriteConsole(LoggerTypes.Devices, $"[Hue] {_light?.Metadata?.Name ?? ""}: failed to capture original state ({ex.Message}).");
                        return;
                    }
                }
                await Task.Delay(300 * (attempt + 1), ct).ConfigureAwait(false);
            }
        }

        // Push the captured state back to the bulb. No-op when no state was
        // captured (offline at adoption time, etc.) — safer than guessing.
        // Two sequential updates: colour + brightness first, power as a
        // separate call so the bridge processes the visual change before
        // toggling on/off (a single combined update with on=false sometimes
        // skips the colour change entirely on certain firmware). Each PUT
        // retries up to three times: the bridge silently sheds CLIP v2
        // calls under load, and a single dropped PUT used to leave the bulb
        // at the last effect colour. A verify GET afterwards re-sends the
        // power state when the bridge accepted the PUT but didn't apply it.
        public async Task RestoreOriginalStateAsync(CancellationToken ct = default)
        {
            if (_light == null || _client == null || _capturedLight == null) return;
            try
            {
                var update = new UpdateLight
                {
                    Color = _capturedLight.Color,
                    Dimming = _capturedLight.Dimming,
                    ColorTemperature = _capturedLight.ColorTemperature,
                    Dynamics = new Dynamics { Duration = FadeDurationMs },
                };
                await PutWithRetryAsync(update, ct).ConfigureAwait(false);

                if (_capturedLight.On != null)
                {
                    await Task.Delay(100, ct).ConfigureAwait(false);
                    var powerUpdate = new UpdateLight
                    {
                        On = _capturedLight.On,
                        Dynamics = new Dynamics { Duration = FadeDurationMs },
                    };
                    await PutWithRetryAsync(powerUpdate, ct).ConfigureAwait(false);

                    // Verify + repair: read the light back and re-assert power
                    // if the bridge dropped the PUT after acknowledging it.
                    await Task.Delay(250, ct).ConfigureAwait(false);
                    try
                    {
                        var check = await _client.Light.GetByIdAsync(_light.Id).ConfigureAwait(false);
                        var liveOn = check?.Data is { Count: > 0 } ? check.Data[0].On?.IsOn : null;
                        if (liveOn != null && liveOn != _capturedLight.On.IsOn)
                        {
                            await PutWithRetryAsync(powerUpdate, ct).ConfigureAwait(false);
                        }
                    }
                    catch { /* verification is best-effort */ }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices, $"[Hue] {_light?.Metadata?.Name ?? ""}: failed to restore original state ({ex.Message}).");
            }
        }

        private async Task PutWithRetryAsync(UpdateLight update, CancellationToken ct)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    await _client.Light.UpdateAsync(_light.Id, update).ConfigureAwait(false);
                    return;
                }
                catch
                {
                    if (attempt == 2) throw;
                    await Task.Delay(250 * (attempt + 1), ct).ConfigureAwait(false);
                }
            }
        }

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            // Update() is called sequentially by the trigger thread, but guard
            // anyway in case the trigger is shared by multiple queues.
            lock (_lock)
            {
                // Provider has begun teardown — drop the update so it can't race
                // ahead of (or behind) the explicit TurnOff sequence. Same
                // for per-device disable from the Mapping tab so a buffered
                // colour frame can't race the restore-to-original send.
                if (_shuttingDown || _perDeviceDisable) return true;

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
                        // Fade to off rather than snapping. A black frame mid-effect
                        // (e.g. between two Vegas pulses) used to read as a hard cut;
                        // with Duration the bulb dims smoothly into the off state.
                        req = new UpdateLight
                        {
                            On = new On { IsOn = false },
                            Dynamics = new Dynamics { Duration = FadeDurationMs },
                        };
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
                            Dynamics = new Dynamics { Duration = FadeDurationMs },
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
