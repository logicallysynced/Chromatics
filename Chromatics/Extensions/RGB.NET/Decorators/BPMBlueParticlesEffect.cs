using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class BPMBlueParticlesEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly int bpm;
        private readonly double beatsPerCycle;
        private readonly int particlesPerBeat;
        private readonly Color[] colors;
        private readonly Color baseColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _beatInterval;
        private readonly List<Particle> _particles = [];
        private double _beatTimer;

        private class Particle
        {
            public double R { get; set; }
            public double C { get; set; }
            public double Age { get; set; }
            public double Lifetime { get; set; }
            public double Drift { get; set; }
            public Color Color { get; set; }
        }

        public BPMBlueParticlesEffect(ListLedGroup _ledGroup, int bpm, double beatsPerCycle, int particlesPerBeat, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.beatsPerCycle = Math.Max(0.05, beatsPerCycle);
            this.particlesPerBeat = Math.Max(1, particlesPerBeat);
            this.colors = colors is { Length: > 0 } ? colors : [new Color(0, 120, 255), new Color(80, 200, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 40) : baseColor;
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
            _particles.Clear();
            _beatTimer = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                // Burst spawn on the beat; each particle lives exactly one
                // cycle so the field breathes with the music instead of
                // streaming continuously.
                _beatTimer += deltaTime;
                if (_beatTimer >= _beatInterval)
                {
                    _beatTimer -= _beatInterval;
                    double lifetime = beatsPerCycle * _beatInterval;
                    for (int i = 0; i < particlesPerBeat; i++)
                    {
                        _particles.Add(new Particle
                        {
                            R = random.NextDouble() * _maxRow,
                            C = random.NextDouble() * _maxCol,
                            Age = 0,
                            Lifetime = lifetime,
                            Drift = (random.NextDouble() - 0.5) * 0.8,
                            Color = colors[random.Next(colors.Length)],
                        });
                    }
                }

                double rise = (_maxRow + 2) / (beatsPerCycle * _beatInterval) * 0.25;
                for (int i = _particles.Count - 1; i >= 0; i--)
                {
                    var p = _particles[i];
                    p.Age += deltaTime;
                    p.R -= rise * deltaTime;
                    p.C += p.Drift * deltaTime;

                    if (p.Age >= p.Lifetime || p.R < -1.5)
                        _particles.RemoveAt(i);
                }

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos)) { led.Color = baseColor; continue; }

                    float best = 0;
                    Color bestColor = baseColor;
                    foreach (var p in _particles)
                    {
                        double dist = Math.Sqrt((pos[0] - p.R) * (pos[0] - p.R) + (pos[1] - p.C) * (pos[1] - p.C));
                        if (dist >= 1.5) continue;

                        double lifeT = p.Age / p.Lifetime;
                        double envelope = lifeT < 0.15 ? lifeT / 0.15 : 1.0 - (lifeT - 0.15) / 0.85;

                        float intensity = (float)((1.0 - dist / 1.5) * envelope);
                        if (intensity > best)
                        {
                            best = intensity;
                            bestColor = p.Color;
                        }
                    }

                    led.Color = best > 0.01f ? LerpColor(baseColor, bestColor, Math.Min(best, 1f)) : baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMBlueParticlesEffect: {ex.Message}");
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
