using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class BPMSINDecorator : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new Random();
        private readonly double waveFrequency;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly int groupSize;
        private readonly List<Led> leds;
        private ConcurrentDictionary<Led, double> ledPositions;
        private Dictionary<Led, Color> currentColors;
        private Dictionary<Led, Color> targetColors;
        private Dictionary<Led, double> transitionStartTimes;
        private double Timing;
        private double nextBeatTime;
        private double interval;
        private int bpm;
        private double fadeTime;

        public BPMSINDecorator(ListLedGroup _ledGroup, int bpm, double waveFrequency, Color[] colors, double fadeTime, int groupSize, RGBSurface surface, bool updateIfDisabled = false, Color baseColor = default(Color)) : base(surface, updateIfDisabled)
        {
            this.ledGroup = _ledGroup;
            this.bpm = bpm;
            this.waveFrequency = waveFrequency;
            this.colors = colors;
            this.baseColor = baseColor == default(Color) ? Color.Transparent : baseColor;
            this.fadeTime = fadeTime;
            this.groupSize = groupSize;

            leds = ledGroup.OrderBy(led => led.Id).ToList(); // Ensure the list is ordered correctly
            CalculateInterval();
            ledPositions = new ConcurrentDictionary<Led, double>();
            currentColors = new Dictionary<Led, Color>();
            targetColors = new Dictionary<Led, Color>();
            transitionStartTimes = new Dictionary<Led, double>();
            Timing = 0;
            nextBeatTime = interval; // Initialize nextBeatTime
        }

        private void CalculateInterval()
        {
            // Convert BPM to interval in seconds (interval = 60 / BPM)
            interval = 60.0 / bpm;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();

            Debug.WriteLine("Arena Light Show Decorator Attached");

            foreach (var led in leds)
            {
                ledPositions.TryAdd(led, random.NextDouble() * Math.PI * 2);
                var initialColor = colors[random.Next(colors.Length)];
                currentColors[led] = initialColor;
                targetColors[led] = initialColor;
                transitionStartTimes[led] = 0;
            }
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            ledPositions.Clear();
            currentColors.Clear();
            targetColors.Clear();
            transitionStartTimes.Clear();
            Timing = 0;
            nextBeatTime = interval;

            Debug.WriteLine("Arena Light Show Decorator Detached");
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                Timing += deltaTime;

                // Check if it's time for the next beat
                if (Timing >= nextBeatTime)
                {
                    nextBeatTime += interval; // Schedule next beat

                    foreach (var led in leds)
                    {
                        int groupIndex = groupSize == 0 ? random.Next(leds.Count) : (int)(leds.IndexOf(led) % groupSize);
                        int colorIndex = (groupIndex + (int)(Timing / interval)) % colors.Length;

                        targetColors[led] = colors[colorIndex];
                        transitionStartTimes[led] = Timing;
                    }
                }

                foreach (var led in leds)
                {
                    if (!ledPositions.ContainsKey(led)) continue;

                    double timeSinceTransitionStart = Timing - transitionStartTimes[led];
                    float transitionProgress = fadeTime > 0 ? (float)Math.Min(timeSinceTransitionStart / fadeTime, 1.0) : 1.0f;
                    float sineProgress = (float)Math.Sin(transitionProgress * Math.PI * 0.5); // Use sine for smooth transitions

                    var color = fadeTime == 0 ? targetColors[led] : Lerp(currentColors[led], targetColors[led], sineProgress);
                    led.Color = color;

                    // Update the current color if the transition is complete
                    if (transitionProgress >= 1.0)
                    {
                        currentColors[led] = targetColors[led];
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

        private static Color Lerp(Color start, Color end, float amount)
        {
            float r = start.R + (end.R - start.R) * amount;
            float g = start.G + (end.G - start.G) * amount;
            float b = start.B + (end.B - start.B) * amount;

            return new Color((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}
