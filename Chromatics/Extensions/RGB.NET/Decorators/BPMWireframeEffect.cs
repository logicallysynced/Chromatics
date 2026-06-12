using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class BPMWireframeEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly int bpm;
        private readonly double beatsPerCycle;
        private readonly double beamWidth;
        private readonly Color baseColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _beatInterval;
        private readonly List<Pillar> _pillars = [];
        private double _beatTimer;
        private double _hue;

        private class Pillar
        {
            public bool Vertical { get; set; }
            public double Offset { get; set; }
            public double Hue { get; set; }
        }

        public BPMWireframeEffect(ListLedGroup _ledGroup, int bpm, double beatsPerCycle, double beamWidth, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.beatsPerCycle = Math.Max(0.05, beatsPerCycle);
            this.beamWidth = Math.Max(0.3, beamWidth);
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;
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
            _pillars.Clear();
            _beatTimer = 0;
            _hue = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                _hue += 40.0 * deltaTime;
                if (_hue >= 360.0) _hue -= 360.0;

                // One mirrored pillar pair per beat; each pair reaches the
                // device edge in beatsPerCycle beats, same speed model as the
                // other BPM decorators.
                _beatTimer += deltaTime;
                if (_beatTimer >= _beatInterval)
                {
                    _beatTimer -= _beatInterval;
                    _pillars.Add(new Pillar
                    {
                        Vertical = random.Next(2) == 0,
                        Offset = 0,
                        Hue = _hue,
                    });
                }

                double centerR = _maxRow / 2.0;
                double centerC = _maxCol / 2.0;

                for (int i = _pillars.Count - 1; i >= 0; i--)
                {
                    var p = _pillars[i];
                    double extent = p.Vertical ? centerC : centerR;
                    double speedPerSec = (extent + beamWidth * 2) / (beatsPerCycle * _beatInterval);
                    p.Offset += speedPerSec * deltaTime;
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
                        ? LerpColor(baseColor, WireframeEffect.HsvToColor(bestHue, 1.0, 1.0), Math.Min(best, 1f))
                        : baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMWireframeEffect: {ex.Message}");
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
