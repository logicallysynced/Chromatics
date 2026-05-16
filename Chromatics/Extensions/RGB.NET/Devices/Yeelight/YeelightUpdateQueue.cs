using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.Yeelight.Protocol;
using RGB.NET.Core;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight
{
    public class YeelightUpdateQueue : UpdateQueue
    {
        #region Properties & Fields

        private readonly YeelightClientDefinition _def;
        private readonly YeelightConnection _connection;
        private readonly Lock _lock = new();

        private volatile bool _shuttingDown;
        // Per-device disable gate — parity with LifxUpdateQueue. Toggled
        // from RGBController.RemoveDevice / AddDevice so paint frames are
        // dropped while the user has the bulb disabled in the Mapping tab.
        private volatile bool _perDeviceDisable;

        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // Throttle: Yeelight LAN protocol caps outbound commands at 60 per
        // minute per bulb (1Hz). Music Mode removes the cap and lets us
        // send at any rate. Either way 35ms (28Hz) is a sensible upper
        // bound — faster doesn't translate to perceptible improvement,
        // and on the non-music path it'd burn the per-minute budget in
        // seconds.
        private const int MinIntervalMs = 35;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private long _lastSendMs;

        // Same-frame dedup so a static layer doesn't pelt the bulb with
        // identical set_rgb commands. Per-channel because main and bg
        // are independent destinations.
        private uint _lastSentRgbMain = uint.MaxValue;
        private int _lastSentBrightnessMain = -1;
        private uint _lastSentRgbBg = uint.MaxValue;
        private int _lastSentBrightnessBg = -1;
        private long _lastKeepAliveMs;

        // Yeelight bulbs auto-power-off if they don't receive a command for
        // ~5 minutes when in Music Mode. Send a no-op keep-alive every 60s
        // (set_rgb to the cached value) to keep them awake on slow-changing
        // layers like Static.
        private const int KeepAliveIntervalMs = 60_000;

        #endregion

        #region Constructors

        public YeelightUpdateQueue(IDeviceUpdateTrigger updateTrigger, YeelightClientDefinition def, YeelightConnection connection)
            : base(updateTrigger)
        {
            _def = def;
            _connection = connection;
        }

        #endregion

        #region Methods

        public void BeginShutdown() => _shuttingDown = true;

        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        public void ResetCache()
        {
            lock (_lock)
            {
                _lastSentRgbMain = uint.MaxValue;
                _lastSentBrightnessMain = -1;
                _lastSentRgbBg = uint.MaxValue;
                _lastSentBrightnessBg = -1;
                _lastKeepAliveMs = 0;
            }
        }

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _perDeviceBrightness = correction;

        // Best-effort restore on disable. Yeelight doesn't have a clean
        // "original state" capture path the way LIFX does (the LAN protocol
        // exposes `get_prop` but parsing the response requires a read pump
        // we don't otherwise need), so for the v1 Beta we just stop
        // sending. The bulb stays on whatever colour Chromatics last set.
        // Users can re-establish their preferred state via the Yeelight
        // app. Tracked as a 4.2.x patch follow-up.
        public Task RestoreOriginalStateAsync() => Task.CompletedTask;

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            lock (_lock)
            {
                if (_shuttingDown || _perDeviceDisable) return true;
                if (dataSet.IsEmpty) return true;

                long now = _stopwatch.ElapsedMilliseconds;
                if (now - _lastSendMs < MinIntervalMs) return true;

                try
                {
                    int globalPct = GlobalBrightnessCorrection.Instance.BrightnessPercent;
                    int perDevicePct = _perDeviceBrightness?.BrightnessPercent ?? 100;
                    double brightnessScale = (globalPct / 100.0) * (perDevicePct / 100.0);

                    // Split the data set into main vs background based on
                    // each LED's LayoutMetadata. Single-light bulbs only
                    // ever have main entries. The brightest LED in each
                    // bucket wins (PickRepresentativeColor) so multiple
                    // Chromatics LEDs mapped to the same channel still
                    // produce one sensible colour per channel.
                    Color? mainColor = null;
                    Color? bgColor = null;
                    PickPerChannelColors(dataSet, ref mainColor, ref bgColor);

                    bool sentSomething = false;

                    if (mainColor.HasValue)
                    {
                        SendChannel(mainColor.Value, brightnessScale,
                            isBackground: false,
                            ref _lastSentRgbMain, ref _lastSentBrightnessMain,
                            out bool sentMain);
                        sentSomething |= sentMain;
                    }
                    if (bgColor.HasValue)
                    {
                        SendChannel(bgColor.Value, brightnessScale,
                            isBackground: true,
                            ref _lastSentRgbBg, ref _lastSentBrightnessBg,
                            out bool sentBg);
                        sentSomething |= sentBg;
                    }

                    if (sentSomething)
                    {
                        _lastSendMs = now;
                        _lastKeepAliveMs = now;
                    }
                    else if (now - _lastKeepAliveMs >= KeepAliveIntervalMs)
                    {
                        // Nothing changed but we need to keep the bulb awake.
                        // Re-send the cached main RGB. Cheaper than get_prop
                        // and equivalent for the firmware's idle-shutoff
                        // timer.
                        _lastKeepAliveMs = now;
                        if (_lastSentRgbMain != uint.MaxValue)
                        {
                            byte r = (byte)((_lastSentRgbMain >> 16) & 0xFF);
                            byte g = (byte)((_lastSentRgbMain >> 8) & 0xFF);
                            byte b = (byte)(_lastSentRgbMain & 0xFF);
                            _ = _connection.SetRgbAsync(r, g, b);
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    YeelightRGBDeviceProvider.Instance?.Throw(ex);
                    return false;
                }
            }
        }

        // Split the data set into a main-channel and bg-channel representative
        // colour. Brightest LED in each bucket wins so multiple Chromatics
        // LEDs mapped to one channel still produce a sensible single value.
        // Updates `mainColor` and `bgColor` by reference; either or both may
        // remain null if the corresponding bucket was empty.
        private static void PickPerChannelColors(
            ReadOnlySpan<(object key, Color color)> dataSet,
            ref Color? mainColor, ref Color? bgColor)
        {
            double bestMainL = -1;
            double bestBgL = -1;
            for (int i = 0; i < dataSet.Length; i++)
            {
                var entry = dataSet[i];
                double l = entry.color.R + entry.color.G + entry.color.B;

                // The key is whatever object the surface bound this entry to.
                // For RGB.NET devices that's the Led — its LayoutMetadata
                // carries our YeelightDevice.LightChannel role enum.
                var channel = ResolveChannel(entry.key);
                if (channel == YeelightDevice.LightChannel.BackgroundLight)
                {
                    if (l > bestBgL) { bestBgL = l; bgColor = entry.color; }
                }
                else
                {
                    if (l > bestMainL) { bestMainL = l; mainColor = entry.color; }
                }
            }
        }

        private static YeelightDevice.LightChannel ResolveChannel(object key)
        {
            // Surface uses the Led instance as the key; LayoutMetadata is
            // the channel enum we stamped in YeelightDevice.InitializeLayout.
            if (key is Led led && led.LayoutMetadata is YeelightDevice.LightChannel c)
                return c;
            return YeelightDevice.LightChannel.MainLight;
        }

        // Send one channel (main or bg). Returns through `sent` whether any
        // network traffic actually fired; caller uses this for the keep-alive
        // and lastSendMs bookkeeping.
        private void SendChannel(Color color, double brightnessScale, bool isBackground,
                                 ref uint lastSentRgb, ref int lastSentBrightness, out bool sent)
        {
            ToRgb255(color, brightnessScale, out byte r, out byte g, out byte b);
            uint packed = (uint)((r << 16) | (g << 8) | b);
            int brightnessPct = Math.Max(1, (int)(Math.Max(r, Math.Max(g, b)) * 100.0 / 255.0));

            bool changedRgb = packed != lastSentRgb;
            bool changedBright = brightnessPct != lastSentBrightness;
            sent = false;

            if (changedRgb)
            {
                if (isBackground) _ = _connection.SetBackgroundRgbAsync(r, g, b);
                else              _ = _connection.SetRgbAsync(r, g, b);
                lastSentRgb = packed;
                sent = true;
            }
            if (changedBright)
            {
                if (isBackground) _ = _connection.SetBackgroundBrightnessAsync(brightnessPct);
                else              _ = _connection.SetBrightnessAsync(brightnessPct);
                lastSentBrightness = brightnessPct;
                sent = true;
            }
        }

        private static void ToRgb255(Color rgb, double brightnessScale, out byte r, out byte g, out byte b)
        {
            double scale = Math.Clamp(brightnessScale, 0.0, 1.0);
            r = (byte)Math.Round(Math.Clamp(rgb.R, 0.0, 1.0) * 255 * scale);
            g = (byte)Math.Round(Math.Clamp(rgb.G, 0.0, 1.0) * 255 * scale);
            b = (byte)Math.Round(Math.Clamp(rgb.B, 0.0, 1.0) * 255 * scale);
        }

        #endregion
    }
}
