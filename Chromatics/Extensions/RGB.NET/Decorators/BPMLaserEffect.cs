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
    public class BPMLaserEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly int bpm;
        private readonly double width;
        private readonly double beatsPerCycle;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly LaserEffect.LaserDirection _direction;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly List<Beam> _beams = [];
        private double _beatTimer;
        private readonly double _beatInterval;
        private int _colorIndex;

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

        public BPMLaserEffect(ListLedGroup _ledGroup, int bpm, double beatsPerCycle, double width, Color[] colors, RGBSurface surface, LaserEffect.LaserDirection direction = LaserEffect.LaserDirection.RandomAll, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.beatsPerCycle = Math.Max(0.25, beatsPerCycle);
            this.width = Math.Max(0.3, width);
            this.colors = colors.Length > 0 ? colors : [new Color(0, 255, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;
            _direction = direction;
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
            _beams.Clear();
        }

        private (double dirR, double dirC, double originR, double originC) PickBeamVector()
        {
            var kind = _direction;
            if (kind == LaserEffect.LaserDirection.RandomHV)
                kind = random.NextDouble() > 0.5 ? LaserEffect.LaserDirection.Horizontal : LaserEffect.LaserDirection.Vertical;
            else if (kind == LaserEffect.LaserDirection.RandomDiagonal)
                kind = random.NextDouble() > 0.5 ? LaserEffect.LaserDirection.DiagonalForward : LaserEffect.LaserDirection.DiagonalBackward;
            else if (kind == LaserEffect.LaserDirection.RandomAll)
                kind = (LaserEffect.LaserDirection)random.Next(4);

            return kind switch
            {
                LaserEffect.LaserDirection.Horizontal       => (0, 1, random.Next(0, _maxRow + 1), -2),
                LaserEffect.LaserDirection.Vertical         => (1, 0, -2, random.Next(0, _maxCol + 1)),
                LaserEffect.LaserDirection.DiagonalForward   => (1, 1, -2, random.Next(-_maxRow, _maxCol + 1)),
                LaserEffect.LaserDirection.DiagonalBackward  => (1, -1, -2, random.Next(0, _maxCol + _maxRow + 1)),
                _ => (0, 1, random.Next(0, _maxRow + 1), -2),
            };
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;
                _beatTimer += deltaTime;

                if (_beatTimer >= _beatInterval)
                {
                    _beatTimer -= _beatInterval;
                    var (dirR, dirC, originR, originC) = PickBeamVector();
                    // Derive speed: beam traverses the full extent in (beatsPerCycle * _beatInterval seconds)
                    double extent = Math.Max(_maxRow, _maxCol) * 2 + 10;
                    double speedPerSec = extent / (beatsPerCycle * _beatInterval);
                    _beams.Add(new Beam
                    {
                        OriginR = originR, OriginC = originC,
                        DirR = dirR, DirC = dirC,
                        Travel = 0,
                        Speed = speedPerSec * (0.9 + random.NextDouble() * 0.2),
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
                        double len = Math.Sqrt(beam.DirR * beam.DirR + beam.DirC * beam.DirC);
                        double perpDist = len > 0
                            ? Math.Abs(beam.DirC * (pos[0] - beam.OriginR) - beam.DirR * (pos[1] - beam.OriginC)) / len
                            : 0;
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
                Debug.WriteLine($"BPMLaserEffect: {ex.Message}");
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
