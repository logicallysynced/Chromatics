using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class BPMSnowstormDecorator : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly int flakeCount;
        private readonly int bpm;
        private readonly double beatsPerCycle;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly SnowstormDecorator.SnowDirection direction;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _beatInterval;
        private readonly List<Flake> _flakes = [];
        private double _time;

        private class Flake
        {
            public double R { get; set; }
            public double C { get; set; }
            public double SpeedScale { get; set; }
            public double SwayPhase { get; set; }
            public Color Color { get; set; }
        }

        public BPMSnowstormDecorator(ListLedGroup _ledGroup, int bpm, double beatsPerCycle, int flakeCount, Color[] colors, RGBSurface surface, SnowstormDecorator.SnowDirection direction, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.beatsPerCycle = Math.Max(0.05, beatsPerCycle);
            this.flakeCount = Math.Max(1, flakeCount);
            this.colors = colors is { Length: > 0 } ? colors : [new Color(255, 255, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;
            this.direction = direction;
            _beatInterval = 60.0 / this.bpm;

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
            _flakes.Clear();
            _time = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;
                _time += deltaTime;

                while (_flakes.Count < flakeCount)
                    _flakes.Add(SpawnFlake());

                var (dr, dc) = DirectionVector();

                // Derive speed: a flake crosses the travel axis in
                // (beatsPerCycle * beatInterval) seconds, same model as the
                // other BPM decorators (speed = extent / time-per-cycle).
                double extent = Math.Abs(dr) >= Math.Abs(dc) ? _maxRow + 4 : _maxCol + 4;
                double speedPerSec = extent / (beatsPerCycle * _beatInterval);

                for (int i = _flakes.Count - 1; i >= 0; i--)
                {
                    var f = _flakes[i];
                    double v = speedPerSec * f.SpeedScale * deltaTime;
                    f.R += dr * v;
                    f.C += dc * v;

                    double sway = Math.Sin(_time * 2.0 + f.SwayPhase) * 0.6 * deltaTime;
                    if (Math.Abs(dr) >= Math.Abs(dc)) f.C += sway;
                    else f.R += sway;

                    if (f.R < -2 || f.R > _maxRow + 2 || f.C < -2 || f.C > _maxCol + 2)
                    {
                        _flakes.RemoveAt(i);
                    }
                }

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos)) { led.Color = baseColor; continue; }

                    float best = 0;
                    Color bestColor = baseColor;
                    foreach (var f in _flakes)
                    {
                        double dist = Math.Sqrt((pos[0] - f.R) * (pos[0] - f.R) + (pos[1] - f.C) * (pos[1] - f.C));
                        if (dist < 1.2)
                        {
                            float intensity = (float)(1.0 - dist / 1.2);
                            if (intensity > best)
                            {
                                best = intensity;
                                bestColor = f.Color;
                            }
                        }
                    }

                    led.Color = best > 0.01f ? LerpColor(baseColor, bestColor, Math.Min(best, 1f)) : baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMSnowstormDecorator: {ex.Message}");
            }
        }

        private (double dr, double dc) DirectionVector()
        {
            const double diag = 0.7071;
            return direction switch
            {
                SnowstormDecorator.SnowDirection.TopToBottom          => (1, 0),
                SnowstormDecorator.SnowDirection.BottomToTop          => (-1, 0),
                SnowstormDecorator.SnowDirection.LeftToRight          => (0, 1),
                SnowstormDecorator.SnowDirection.RightToLeft          => (0, -1),
                SnowstormDecorator.SnowDirection.TopLeftToBottomRight => (diag, diag),
                SnowstormDecorator.SnowDirection.TopRightToBottomLeft => (diag, -diag),
                SnowstormDecorator.SnowDirection.BottomLeftToTopRight => (-diag, diag),
                SnowstormDecorator.SnowDirection.BottomRightToTopLeft => (-diag, -diag),
                _                                                     => (1, 0),
            };
        }

        private Flake SpawnFlake()
        {
            var (dr, dc) = DirectionVector();
            double r, c;

            if (dr > 0) r = -random.NextDouble() * 2;
            else if (dr < 0) r = _maxRow + random.NextDouble() * 2;
            else r = random.NextDouble() * _maxRow;

            if (dc > 0) c = -random.NextDouble() * 2;
            else if (dc < 0) c = _maxCol + random.NextDouble() * 2;
            else c = random.NextDouble() * _maxCol;

            if (dr != 0 && dc != 0 && random.Next(2) == 0)
            {
                r = random.NextDouble() * _maxRow;
            }
            else if (dr != 0 && dc != 0)
            {
                c = random.NextDouble() * _maxCol;
            }

            return new Flake
            {
                R = r,
                C = c,
                SpeedScale = 0.6 + random.NextDouble() * 0.8,
                SwayPhase = random.NextDouble() * Math.PI * 2,
                Color = colors[random.Next(colors.Length)],
            };
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
