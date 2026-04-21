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
    public class FireEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly double intensity;
        private readonly double flickerSpeed;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly Dictionary<Led, double> _heatMap = new();
        private double _timing;

        public FireEffect(ListLedGroup _ledGroup, double intensity, double flickerSpeed, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.intensity = Math.Clamp(intensity, 0.1, 3.0);
            this.flickerSpeed = Math.Clamp(flickerSpeed, 0.5, 10.0);
            this.colors = colors.Length > 0 ? colors : [new Color(255, 60, 0)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(ledGroup);

            foreach (var led in ledGroup)
                _heatMap[led] = 0;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _heatMap.Clear();
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;
                _timing += deltaTime;

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    int row = pos[0];
                    int col = pos[1];

                    double rowHeat = _maxRow > 0 ? (double)row / _maxRow : 0;
                    double baseHeat = Math.Pow(rowHeat, 0.6) * intensity;

                    double flicker = Math.Sin(_timing * flickerSpeed * 3.7 + col * 0.8) * 0.3
                                   + Math.Sin(_timing * flickerSpeed * 7.1 + col * 1.3 + row * 0.5) * 0.2
                                   + Math.Sin(_timing * flickerSpeed * 11.3 + col * 2.1) * 0.15
                                   + (random.NextDouble() - 0.5) * 0.15;

                    double heat = Math.Clamp(baseHeat + flicker * intensity * 0.5, 0, 1);

                    double current = _heatMap.GetValueOrDefault(led, 0);
                    double smoothed = current + (heat - current) * Math.Min(1.0, deltaTime * 12.0);
                    _heatMap[led] = smoothed;

                    if (smoothed < 0.05)
                    {
                        led.Color = baseColor;
                    }
                    else
                    {
                        double idx = smoothed * (colors.Length - 1);
                        int lo = Math.Clamp((int)idx, 0, colors.Length - 1);
                        int hi = Math.Min(lo + 1, colors.Length - 1);
                        double frac = idx - lo;
                        led.Color = LerpColor(colors[lo], colors[hi], (float)frac);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FireEffect: {ex.Message}");
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
