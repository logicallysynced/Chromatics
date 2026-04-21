using Chromatics.Core;
using Chromatics.Localization;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class CircularPulseEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly double pulseSpeed;
        private readonly double pulseRadius;
        private readonly double pulseInterval;
        private readonly double fadeWidth;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly List<Pulse> _pulses = [];
        private double _spawnTimer;
        private int _colorIndex;

        private class Pulse
        {
            public double CenterR { get; set; }
            public double CenterC { get; set; }
            public double CurrentRadius { get; set; }
            public double MaxRadius { get; set; }
            public Color Color { get; set; }
        }

        public CircularPulseEffect(ListLedGroup _ledGroup, double pulseSpeed, double pulseRadius, double pulseInterval, double fadeWidth, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.pulseSpeed = Math.Max(0.5, pulseSpeed);
            this.pulseRadius = Math.Max(1, pulseRadius);
            this.pulseInterval = Math.Max(0.05, pulseInterval);
            this.fadeWidth = Math.Max(0.3, fadeWidth);
            this.colors = colors.Length > 0 ? colors : [new Color(0, 200, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _pulses.Clear();
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;
                _spawnTimer += deltaTime;

                if (_spawnTimer >= pulseInterval)
                {
                    _spawnTimer = 0;
                    _pulses.Add(new Pulse
                    {
                        CenterR = random.Next(0, _maxRow + 1),
                        CenterC = random.Next(0, _maxCol + 1),
                        CurrentRadius = 0,
                        MaxRadius = pulseRadius,
                        Color = colors[_colorIndex++ % colors.Length],
                    });
                }

                // Expand and cull pulses
                for (int i = _pulses.Count - 1; i >= 0; i--)
                {
                    _pulses[i].CurrentRadius += pulseSpeed * deltaTime;
                    if (_pulses[i].CurrentRadius > _pulses[i].MaxRadius + fadeWidth * 2)
                        _pulses.RemoveAt(i);
                }

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    float bestIntensity = 0;
                    Color bestColor = baseColor;

                    foreach (var pulse in _pulses)
                    {
                        double dr = pos[0] - pulse.CenterR;
                        double dc = pos[1] - pulse.CenterC;
                        double dist = Math.Sqrt(dr * dr + dc * dc);

                        // Ring intensity: brightest at the expanding edge, fading inside and outside
                        double ringDist = Math.Abs(dist - pulse.CurrentRadius);
                        if (ringDist < fadeWidth)
                        {
                            float ringIntensity = (float)(1.0 - ringDist / fadeWidth);

                            // Fade out as the ring approaches its max radius
                            double lifeFade = 1.0 - Math.Clamp(pulse.CurrentRadius / pulse.MaxRadius, 0, 1);
                            ringIntensity *= (float)lifeFade;

                            if (ringIntensity > bestIntensity)
                            {
                                bestIntensity = ringIntensity;
                                bestColor = pulse.Color;
                            }
                        }
                    }

                    if (bestIntensity > 0.01f)
                        led.Color = LerpColor(baseColor, bestColor, Math.Min(bestIntensity, 1f));
                    else
                        led.Color = baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CircularPulseEffect: {ex.Message}");
            }
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            return new Color(
                (byte)((a.R + (b.R - a.R) * t) * 255),
                (byte)((a.G + (b.G - a.G) * t) * 255),
                (byte)((a.B + (b.B - a.B) * t) * 255));
        }
    }
}
