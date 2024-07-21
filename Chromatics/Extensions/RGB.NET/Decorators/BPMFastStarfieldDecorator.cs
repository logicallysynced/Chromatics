using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class BPMFastStarfieldDecorator : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new Random();
        private readonly int numberOfLeds;
        private readonly double fadeSpeed;
        private readonly double densityMultiplier;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private ConcurrentDictionary<Led, Color> fadingInLeds;
        private ConcurrentDictionary<Led, Color> fadingOutLeds;
        private Dictionary<Led, float> currentBrightness;
        private Dictionary<Led, Color> currentColors;
        private Dictionary<Led, double> startTimes;
        private double Timing;
        private double startDelay;
        private double updateCounter = 0;
        private double interval;

        public BPMFastStarfieldDecorator(ListLedGroup _ledGroup, int numberOfLeds, int bpm, double fadeSpeed, Color[] colors, RGBSurface surface, double densityMultiplier = 1.0, bool updateIfDisabled = false, Color baseColor = default(Color)) : base(surface, updateIfDisabled)
        {
            this.ledGroup = _ledGroup;
            this.numberOfLeds = numberOfLeds;
            this.fadeSpeed = fadeSpeed;
            this.colors = colors;
            this.baseColor = baseColor == default(Color) ? Color.Transparent : baseColor;
            this.densityMultiplier = densityMultiplier;

            CalculateInterval(bpm);
            startTimes = new Dictionary<Led, double>();
            fadingInLeds = new ConcurrentDictionary<Led, Color>();
            fadingOutLeds = new ConcurrentDictionary<Led, Color>();
            currentBrightness = new Dictionary<Led, float>();
            currentColors = new Dictionary<Led, Color>();
            Timing = 0;
            startDelay = 0;
        }

        private void CalculateInterval(int bpm)
        {
            // Convert BPM to interval in seconds (interval = 60 / BPM)
            interval = 60.0 / bpm;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            currentBrightness.Clear();
            fadingInLeds.Clear();
            fadingOutLeds.Clear();
            currentColors.Clear();
            startTimes.Clear();
            Timing = 0;
            startDelay = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null || fadingInLeds == null || fadingOutLeds == null) return;

                Timing += deltaTime;

                var minBrightness = 0;
                var maxBrightness = 1;

                if (Timing >= startDelay)
                {
                    var availableLeds = ledGroup.Where(led => !fadingInLeds.ContainsKey(led) && !fadingOutLeds.ContainsKey(led));
                    var selectedLeds = availableLeds.OrderBy(x => Guid.NewGuid()).Take((int)(numberOfLeds * densityMultiplier));

                    foreach (var led in selectedLeds)
                    {
                        fadingInLeds.TryAdd(led, baseColor);
                        var colorIndex = random.Next(colors.Length);

                        if (!currentColors.ContainsKey(led))
                            currentColors.Add(led, colors[colorIndex]);

                        if (!startTimes.ContainsKey(led))
                        {
                            var rng = Timing + GetRandomStartTime();
                            startTimes.Add(led, rng);
                        }
                    }

                    startDelay = Timing + (((fadeSpeed * 2) - 50) / 1000);
                }

                foreach (var led in fadingInLeds)
                {
                    if (Timing >= startTimes[led.Key])
                    {
                        if (!currentBrightness.ContainsKey(led.Key))
                        {
                            currentBrightness.Add(led.Key, minBrightness);
                        }

                        currentBrightness[led.Key] += (float)(deltaTime * (maxBrightness - minBrightness) / (fadeSpeed / 1000));

                        if (currentBrightness[led.Key] >= maxBrightness)
                        {
                            fadingOutLeds.TryAdd(led.Key, currentColors[led.Key]);
                            fadingInLeds.TryRemove(led);
                        }
                        else
                        {
                            var lerpAmount = Map(currentBrightness[led.Key], minBrightness, maxBrightness, 0, 1);
                            var color = Lerp(baseColor, currentColors[led.Key], lerpAmount);
                            fadingInLeds[led.Key] = color;
                        }
                    }
                }

                foreach (var led in fadingOutLeds)
                {
                    if (!currentBrightness.ContainsKey(led.Key))
                    {
                        currentBrightness.Add(led.Key, maxBrightness);
                    }

                    currentBrightness[led.Key] -= (float)(deltaTime * (maxBrightness - minBrightness) / (fadeSpeed / 1000));

                    if (currentBrightness[led.Key] <= minBrightness)
                    {
                        fadingOutLeds.TryRemove(led);
                        currentBrightness.Remove(led.Key);
                        currentColors.Remove(led.Key);
                        startTimes.Remove(led.Key);
                    }
                    else
                    {
                        var lerpAmount = Map(currentBrightness[led.Key], minBrightness, maxBrightness, 0, 1);
                        var color = Lerp(baseColor, currentColors[led.Key], lerpAmount);
                        fadingOutLeds[led.Key] = color;
                    }
                }

                foreach (var led in ledGroup)
                {
                    if (fadingInLeds != null && fadingInLeds.ContainsKey(led))
                    {
                        led.Color = fadingInLeds[led];
                    }
                    else if (fadingOutLeds != null && fadingOutLeds.ContainsKey(led))
                    {
                        led.Color = fadingOutLeds[led];
                    }
                    else
                    {
                        led.Color = baseColor;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"Exception: {ex.Message}");
#endif
            }
        }

        private double GetRandomStartTime()
        {
            return random.NextDouble() * (interval * 0.05);
        }

        private float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
        }

        private static Color Lerp(Color start, Color end, float amount)
        {
            float r = start.R + (end.R - start.R) * amount;
            float g = start.G + (end.G - start.G) * amount;
            float b = start.B + (end.B - start.B) * amount;

            return new Color((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }
    }
}
