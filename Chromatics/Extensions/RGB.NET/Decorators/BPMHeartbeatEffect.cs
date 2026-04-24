using Chromatics.Core;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // Heartbeat: a "lub-dub" double-pulse synced to BPM. Each beat fires
    // two ring expansions out from the grid's centre — a strong primary
    // and a slightly delayed weaker secondary — mimicking the systolic /
    // diastolic feel of a heartbeat. Ring radius travels outward and the
    // intensity gaussian-falls off either side of the ring perimeter.
    public class BPMHeartbeatEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly int bpm;
        private readonly double ringWidth;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _beatInterval;
        private readonly double _maxRadius;
        private readonly List<Pulse> _pulses = [];
        private double _beatTimer;
        private int _beatIndex;

        private class Pulse
        {
            public double Age;
            public double Duration;
            public double Strength;
            public Color Color;
        }

        public BPMHeartbeatEffect(ListLedGroup _ledGroup, int bpm, double ringWidth, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.ringWidth = Math.Max(0.5, ringWidth);
            this.colors = colors.Length > 0 ? colors : [new Color(255, 30, 50)];
            this.baseColor = baseColor == default ? new Color(20, 0, 0) : baseColor;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
            _beatInterval = 60.0 / this.bpm;
            _maxRadius = Math.Sqrt(_maxRow * _maxRow + _maxCol * _maxCol) + ringWidth;
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

                _beatTimer += deltaTime;
                if (_beatTimer >= _beatInterval)
                {
                    _beatTimer -= _beatInterval;
                    var color = colors[_beatIndex++ % colors.Length];
                    // Primary pulse: full strength, traverses in ~0.6 of beat.
                    _pulses.Add(new Pulse
                    {
                        Age = 0,
                        Duration = _beatInterval * 0.6,
                        Strength = 1.0,
                        Color = color,
                    });
                    // Secondary "dub" pulse: weaker, slower, slightly delayed.
                    _pulses.Add(new Pulse
                    {
                        Age = -_beatInterval * 0.18,
                        Duration = _beatInterval * 0.55,
                        Strength = 0.55,
                        Color = color,
                    });
                }

                for (int i = _pulses.Count - 1; i >= 0; i--)
                {
                    _pulses[i].Age += deltaTime;
                    if (_pulses[i].Age > _pulses[i].Duration + 0.1)
                        _pulses.RemoveAt(i);
                }

                double centreR = _maxRow / 2.0;
                double centreC = _maxCol / 2.0;

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    double dr = pos[0] - centreR;
                    double dc = pos[1] - centreC;
                    double dist = Math.Sqrt(dr * dr + dc * dc);

                    float bestIntensity = 0;
                    Color bestColor = baseColor;

                    foreach (var pulse in _pulses)
                    {
                        if (pulse.Age < 0) continue;
                        double t = Math.Min(1.0, pulse.Age / pulse.Duration);
                        double radius = t * _maxRadius;
                        double offset = Math.Abs(dist - radius);
                        if (offset > ringWidth) continue;

                        // Gaussian-ish falloff across the ring band.
                        double bell = Math.Exp(-(offset * offset) / (ringWidth * ringWidth * 0.5));
                        // Pulse fades over its lifetime.
                        double envelope = (1.0 - t) * pulse.Strength;
                        float intensity = (float)(bell * envelope);
                        if (intensity > bestIntensity)
                        {
                            bestIntensity = intensity;
                            bestColor = pulse.Color;
                        }
                    }

                    led.Color = bestIntensity > 0.01f
                        ? LerpColor(baseColor, bestColor, bestIntensity)
                        : baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMHeartbeatEffect: {ex.Message}");
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
