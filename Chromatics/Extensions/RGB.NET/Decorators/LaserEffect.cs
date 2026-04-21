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
    public class LaserEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly double speed;
        private readonly double width;
        private readonly double interval;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly LaserDirection _direction;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly List<Beam> _beams = [];
        private double _spawnTimer;
        private int _colorIndex;

        public enum LaserDirection
        {
            Horizontal,
            Vertical,
            DiagonalForward,
            DiagonalBackward,
            RandomHV,
            RandomDiagonal,
            RandomAll
        }

        private class Beam
        {
            public double OriginR { get; set; }
            public double OriginC { get; set; }
            public double DirR { get; set; }
            public double DirC { get; set; }
            public double Travel { get; set; }
            public double Speed { get; set; }
            public Color Color { get; set; }
            public double Width { get; set; }
        }

        public LaserEffect(ListLedGroup _ledGroup, double speed, double width, double interval, Color[] colors, RGBSurface surface, LaserDirection direction = LaserDirection.RandomAll, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.speed = speed;
            this.width = Math.Max(0.3, width);
            this.interval = Math.Max(0.05, interval);
            this.colors = colors.Length > 0 ? colors : [new Color(0, 255, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;
            _direction = direction;

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
            _beams.Clear();
        }

        private (double dirR, double dirC, double originR, double originC) PickBeamVector()
        {
            var kind = _direction;
            if (kind == LaserDirection.RandomHV)
                kind = random.NextDouble() > 0.5 ? LaserDirection.Horizontal : LaserDirection.Vertical;
            else if (kind == LaserDirection.RandomDiagonal)
                kind = random.NextDouble() > 0.5 ? LaserDirection.DiagonalForward : LaserDirection.DiagonalBackward;
            else if (kind == LaserDirection.RandomAll)
                kind = (LaserDirection)random.Next(4); // 0-3: H, V, DiagFwd, DiagBack

            return kind switch
            {
                LaserDirection.Horizontal      => (0, 1, random.Next(0, _maxRow + 1), -2),
                LaserDirection.Vertical        => (1, 0, -2, random.Next(0, _maxCol + 1)),
                LaserDirection.DiagonalForward  => (1, 1, -2, random.Next(-_maxRow, _maxCol + 1)),
                LaserDirection.DiagonalBackward => (1, -1, -2, random.Next(0, _maxCol + _maxRow + 1)),
                _ => (0, 1, random.Next(0, _maxRow + 1), -2),
            };
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;
                _spawnTimer += deltaTime;

                if (_spawnTimer >= interval)
                {
                    _spawnTimer = 0;
                    var (dirR, dirC, originR, originC) = PickBeamVector();
                    _beams.Add(new Beam
                    {
                        OriginR = originR,
                        OriginC = originC,
                        DirR = dirR,
                        DirC = dirC,
                        Travel = 0,
                        Speed = speed * (0.8 + random.NextDouble() * 0.4),
                        Color = colors[_colorIndex++ % colors.Length],
                        Width = width,
                    });
                }

                double maxExtent = Math.Max(_maxRow, _maxCol) * 2 + 10;
                for (int i = _beams.Count - 1; i >= 0; i--)
                {
                    _beams[i].Travel += _beams[i].Speed * deltaTime;
                    if (_beams[i].Travel > maxExtent)
                        _beams.RemoveAt(i);
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

                    foreach (var beam in _beams)
                    {
                        double headR = beam.OriginR + beam.DirR * beam.Travel;
                        double headC = beam.OriginC + beam.DirC * beam.Travel;

                        // Perpendicular distance from LED to the beam line
                        double len = Math.Sqrt(beam.DirR * beam.DirR + beam.DirC * beam.DirC);
                        double perpDist = len > 0
                            ? Math.Abs(beam.DirC * (pos[0] - beam.OriginR) - beam.DirR * (pos[1] - beam.OriginC)) / len
                            : 0;

                        // Longitudinal: only show near the beam head
                        double projDist = (beam.DirR * (pos[0] - beam.OriginR) + beam.DirC * (pos[1] - beam.OriginC)) / len;
                        double headDist = Math.Abs(projDist - beam.Travel);

                        if (perpDist < beam.Width && headDist < beam.Width * 2)
                        {
                            float intensity = (float)((1.0 - perpDist / beam.Width) * (1.0 - headDist / (beam.Width * 2)));
                            intensity = Math.Max(0, intensity * intensity);
                            if (intensity > bestIntensity)
                            {
                                bestIntensity = intensity;
                                bestColor = beam.Color;
                            }
                        }
                    }

                    led.Color = bestIntensity > 0.01f
                        ? LerpColor(baseColor, bestColor, bestIntensity)
                        : baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LaserEffect: {ex.Message}");
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
