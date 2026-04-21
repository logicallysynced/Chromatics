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
    // Spawns an expanding ripple ring from the center of the device on each
    // beat. Multiple overlapping ripples create a layered wave pattern.
    public class BPMRippleDecorator : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly int bpm;
        private readonly double beatsPerCycle;
        private readonly double fadeWidth;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly double _centerR;
        private readonly double _centerC;
        private readonly double _maxDist;
        private readonly List<Ripple> _ripples = [];
        private double _timing;
        private double _beatTimer;
        private double _beatInterval;
        private int _colorIndex;

        private class Ripple
        {
            public double Radius { get; set; }
            public double MaxRadius { get; set; }
            public Color Color { get; set; }
        }

        public BPMRippleDecorator(ListLedGroup _ledGroup, int bpm, double beatsPerCycle, double fadeWidth, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.beatsPerCycle = Math.Max(0.25, beatsPerCycle);
            this.fadeWidth = Math.Max(0.5, fadeWidth);
            this.colors = colors.Length > 0 ? colors : [new Color(0, 150, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            _beatInterval = 60.0 / this.bpm;
            int maxRow, maxCol;
            (_grid, maxRow, maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
            _centerR = maxRow / 2.0;
            _centerC = maxCol / 2.0;
            _maxDist = Math.Sqrt(_centerR * _centerR + _centerC * _centerC) + 3;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _ripples.Clear();
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;
                _timing += deltaTime;
                _beatTimer += deltaTime;

                if (_beatTimer >= _beatInterval)
                {
                    _beatTimer -= _beatInterval;
                    _ripples.Add(new Ripple
                    {
                        Radius = 0,
                        MaxRadius = _maxDist,
                        Color = colors[_colorIndex++ % colors.Length],
                    });
                }

                // Derive speed: ripple reaches max distance in (beatsPerCycle * _beatInterval) seconds
                double speedPerSec = _maxDist / (beatsPerCycle * _beatInterval);
                for (int i = _ripples.Count - 1; i >= 0; i--)
                {
                    _ripples[i].Radius += speedPerSec * deltaTime;
                    if (_ripples[i].Radius > _ripples[i].MaxRadius + fadeWidth * 2)
                        _ripples.RemoveAt(i);
                }

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    double dr = pos[0] - _centerR;
                    double dc = pos[1] - _centerC;
                    double dist = Math.Sqrt(dr * dr + dc * dc);

                    float bestIntensity = 0;
                    Color bestColor = baseColor;

                    foreach (var ripple in _ripples)
                    {
                        double ringDist = Math.Abs(dist - ripple.Radius);
                        if (ringDist < fadeWidth)
                        {
                            float intensity = (float)(1.0 - ringDist / fadeWidth);
                            double life = 1.0 - Math.Clamp(ripple.Radius / ripple.MaxRadius, 0, 1);
                            intensity *= (float)Math.Pow(life, 0.5);
                            if (intensity > bestIntensity)
                            {
                                bestIntensity = intensity;
                                bestColor = ripple.Color;
                            }
                        }
                    }

                    led.Color = bestIntensity > 0.01f
                        ? LerpColor(baseColor, bestColor, Math.Min(bestIntensity, 1f))
                        : baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMRippleDecorator: {ex.Message}");
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
