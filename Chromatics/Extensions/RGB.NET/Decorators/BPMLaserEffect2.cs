using Chromatics.Core;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // Multi-beam BPM-driven laser. Maintains `simultaneousBeams` lasers
    // travelling across the device at once. BPM controls travel speed via
    // beatsPerCycle (1.0 = full sweep per beat). New beams spawn whenever
    // an active beam falls off the edge so the count stays constant.
    public class BPMLaserEffect2 : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly int bpm;
        private readonly double width;
        private readonly double beatsPerCycle;
        private readonly int simultaneousBeams;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly LaserEffect.LaserDirection _direction;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly List<Beam> _beams = [];
        private int _colorIndex;

        private class Beam
        {
            public double OriginR;
            public double OriginC;
            public double DirR;
            public double DirC;
            public double Travel;
            public double Speed;
            public Color Color;
            public double Width;
        }

        public BPMLaserEffect2(ListLedGroup _ledGroup, int bpm, double beatsPerCycle, double width, int simultaneousBeams, Color[] colors, RGBSurface surface, LaserEffect.LaserDirection direction = LaserEffect.LaserDirection.RandomAll, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.beatsPerCycle = Math.Max(0.25, beatsPerCycle);
            this.width = Math.Max(0.3, width);
            this.simultaneousBeams = Math.Max(1, simultaneousBeams);
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
                LaserEffect.LaserDirection.DiagonalForward  => (1, 1, -2, random.Next(-_maxRow, _maxCol + 1)),
                LaserEffect.LaserDirection.DiagonalBackward => (1, -1, -2, random.Next(0, _maxCol + _maxRow + 1)),
                _ => (0, 1, random.Next(0, _maxRow + 1), -2),
            };
        }

        private void SpawnBeam()
        {
            var (dirR, dirC, originR, originC) = PickBeamVector();
            double fullExtent = Math.Max(_maxRow, _maxCol) + 4;
            double speed = fullExtent / (beatsPerCycle * (60.0 / bpm));

            _beams.Add(new Beam
            {
                OriginR = originR,
                OriginC = originC,
                DirR = dirR,
                DirC = dirC,
                Travel = 0,
                Speed = speed,
                Color = colors[_colorIndex++ % colors.Length],
                Width = width,
            });
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                while (_beams.Count < simultaneousBeams)
                    SpawnBeam();

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
                        if (len <= 0) continue;
                        double perpDist = Math.Abs(beam.DirC * (pos[0] - beam.OriginR) - beam.DirR * (pos[1] - beam.OriginC)) / len;
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
                Debug.WriteLine($"BPMLaserEffect2: {ex.Message}");
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
