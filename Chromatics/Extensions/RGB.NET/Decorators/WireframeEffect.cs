using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class WireframeEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly double speed;
        private readonly double spawnInterval;
        private readonly double beamWidth;
        private readonly PillarMode mode;
        private readonly Color baseColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly List<Pillar> _pillars = [];
        private double _spawnTimer;
        private double _hue;
        private bool _lastVertical;

        public enum PillarMode
        {
            // Each spawn picks vertical or horizontal at random.
            Random,
            // Vertical pillars only - columns sweeping left and right.
            Vertical,
            // Horizontal pillars only - rows sweeping up and down.
            Horizontal,
            // Strict vertical / horizontal alternation per spawn.
            Alternating
        }

        private class Pillar
        {
            public bool Vertical { get; set; }
            // Offset from centre along the travel axis; mirrored pair renders
            // at centre + offset AND centre - offset so each spawn fans out
            // to both edges at once.
            public double Offset { get; set; }
            public double Hue { get; set; }
        }

        public WireframeEffect(ListLedGroup _ledGroup, double speed, double spawnInterval, double beamWidth, RGBSurface surface, PillarMode mode = PillarMode.Random, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.speed = Math.Max(0.1, speed);
            this.spawnInterval = Math.Max(0.05, spawnInterval);
            this.beamWidth = Math.Max(0.3, beamWidth);
            this.mode = mode;
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
        }

        internal static bool NextPillarVertical(PillarMode mode, Random random, ref bool lastVertical)
        {
            bool vertical = mode switch
            {
                PillarMode.Vertical => true,
                PillarMode.Horizontal => false,
                PillarMode.Alternating => !lastVertical,
                _ => random.Next(2) == 0,
            };
            lastVertical = vertical;
            return vertical;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _pillars.Clear();
            _spawnTimer = 0;
            _hue = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                // Hue wheel advances continuously; each spawned pillar locks
                // the hue it was born with, so the fan-out carries a rainbow
                // spread across the in-flight pillars.
                _hue += 40.0 * deltaTime;
                if (_hue >= 360.0) _hue -= 360.0;

                _spawnTimer += deltaTime;
                if (_spawnTimer >= spawnInterval)
                {
                    _spawnTimer -= spawnInterval;
                    _pillars.Add(new Pillar
                    {
                        Vertical = NextPillarVertical(mode, random, ref _lastVertical),
                        Offset = 0,
                        Hue = _hue,
                    });
                }

                double centerR = _maxRow / 2.0;
                double centerC = _maxCol / 2.0;

                for (int i = _pillars.Count - 1; i >= 0; i--)
                {
                    var p = _pillars[i];
                    p.Offset += speed * deltaTime;
                    double extent = p.Vertical ? centerC : centerR;
                    if (p.Offset > extent + beamWidth * 2)
                        _pillars.RemoveAt(i);
                }

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos)) { led.Color = baseColor; continue; }

                    float best = 0;
                    double bestHue = 0;
                    foreach (var p in _pillars)
                    {
                        double axisPos = p.Vertical ? pos[1] : pos[0];
                        double center = p.Vertical ? centerC : centerR;

                        double d = Math.Min(
                            Math.Abs(axisPos - (center + p.Offset)),
                            Math.Abs(axisPos - (center - p.Offset)));

                        if (d < beamWidth)
                        {
                            float intensity = (float)(1.0 - d / beamWidth);
                            if (intensity > best)
                            {
                                best = intensity;
                                bestHue = p.Hue;
                            }
                        }
                    }

                    led.Color = best > 0.01f
                        ? LerpColor(baseColor, HsvToColor(bestHue, 1.0, 1.0), Math.Min(best, 1f))
                        : baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WireframeEffect: {ex.Message}");
            }
        }

        internal static Color HsvToColor(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs(h / 60.0 % 2 - 1));
            double m = v - c;

            (double r, double g, double b) = ((int)(h / 60.0) % 6) switch
            {
                0 => (c, x, 0.0),
                1 => (x, c, 0.0),
                2 => (0.0, c, x),
                3 => (0.0, x, c),
                4 => (x, 0.0, c),
                _ => (c, 0.0, x),
            };

            return new Color((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
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
