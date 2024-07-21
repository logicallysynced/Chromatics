using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class ArenaLightShowDecorator : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new Random();
        private readonly double interval;
        private readonly double waveSpeed;
        private readonly double waveFrequency;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private ConcurrentDictionary<Led, double> ledPositions;
        private Dictionary<Led, float> currentBrightness;
        private Dictionary<Led, Color> currentColors;
        private double Timing;

        public ArenaLightShowDecorator(ListLedGroup _ledGroup, double interval, double waveSpeed, double waveFrequency, Color[] colors, RGBSurface surface, bool updateIfDisabled = false, Color baseColor = default(Color)) : base(surface, updateIfDisabled)
        {
            this.ledGroup = _ledGroup;
            this.interval = interval;
            this.waveSpeed = waveSpeed;
            this.waveFrequency = waveFrequency;
            this.colors = colors;
            this.baseColor = baseColor == default(Color) ? Color.Transparent : baseColor;

            ledPositions = new ConcurrentDictionary<Led, double>();
            currentBrightness = new Dictionary<Led, float>();
            currentColors = new Dictionary<Led, Color>();
            Timing = 0;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();

            Debug.WriteLine("Arena Light Show Decorator Attached");

            foreach (var led in ledGroup)
            {
                ledPositions.TryAdd(led, random.NextDouble() * Math.PI * 2);
                currentColors.TryAdd(led, colors[random.Next(colors.Length)]);
            }
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            ledPositions.Clear();
            currentColors.Clear();
            currentBrightness.Clear();
            Timing = 0;

            Debug.WriteLine("Arena Light Show Decorator Detached");
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                Timing += deltaTime * waveSpeed;

                foreach (var led in ledGroup)
                {
                    if (!ledPositions.ContainsKey(led)) continue;

                    double position = ledPositions[led] + Timing;
                    double intensity = (Math.Sin(position * waveFrequency) + 1) / 2; // Sine wave for smooth transition

                    var colorIndex = (int)(Math.Floor((position / Math.PI) % colors.Length));
                    var nextColorIndex = (colorIndex + 1) % colors.Length;

                    var currentColor = colors[colorIndex];
                    var nextColor = colors[nextColorIndex];

                    var color = Lerp(currentColor, nextColor, (float)intensity);

                    var newCol = new Color(
                        (int)(color.R * 255 * intensity + baseColor.R * (1 - intensity)),
                        (int)(color.G * 255 * intensity + baseColor.G * (1 - intensity)),
                        (int)(color.B * 255 * intensity + baseColor.B * (1 - intensity))
                    );

                    led.Color = newCol;
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

            return new Color((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }
    }
}
