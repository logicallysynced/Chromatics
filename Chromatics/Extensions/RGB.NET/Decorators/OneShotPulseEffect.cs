using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // Single radial pulse from the device centre. Unlike PulseDecorator,
    // which walks hardcoded per-key step lists that stop short of the
    // numpad and edge clusters, this resolves every LED through
    // DeviceGridHelper, so the ring crosses the full physical board
    // (numpad, nav cluster, media keys) and works on non-keyboards too.
    public class OneShotPulseEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly double speed;
        private readonly double ringWidth;
        private readonly Color[] colors;
        private readonly Color baseColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _maxExtent;
        private double _radius;
        private bool _finished;

        // Owners poll this to remove the decorator once the ring has left
        // the board - mirrors how ShotFlashDecorator consumers manage
        // lifetime.
        public bool IsFinished => _finished;

        public OneShotPulseEffect(ListLedGroup _ledGroup, double speed, double ringWidth, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.speed = Math.Max(0.1, speed);
            this.ringWidth = Math.Max(0.3, ringWidth);
            this.colors = colors is { Length: > 0 } ? colors : [new Color(255, 255, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
            double centerR = _maxRow / 2.0;
            double centerC = _maxCol / 2.0;
            _maxExtent = Math.Sqrt(centerR * centerR + centerC * centerC);
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
            _radius = 0;
            _finished = false;
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _radius = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                if (!_finished)
                {
                    _radius += speed * deltaTime;
                    if (_radius > _maxExtent + ringWidth * 2)
                        _finished = true;
                }

                double centerR = _maxRow / 2.0;
                double centerC = _maxCol / 2.0;

                foreach (var led in ledGroup)
                {
                    if (_finished || !_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    double dr = pos[0] - centerR;
                    double dc = pos[1] - centerC;
                    double dist = Math.Sqrt(dr * dr + dc * dc);

                    double ringDist = Math.Abs(dist - _radius);
                    if (ringDist < ringWidth)
                    {
                        float intensity = (float)(1.0 - ringDist / ringWidth);
                        // Colour bands across the ring thickness when more
                        // than one colour is supplied: leading edge takes the
                        // first slot, trailing edge the last.
                        double band = (dist - _radius + ringWidth) / (ringWidth * 2);
                        int ci = Math.Clamp((int)(band * colors.Length), 0, colors.Length - 1);
                        led.Color = LerpColor(baseColor, colors[ci], Math.Min(intensity, 1f));
                    }
                    else
                    {
                        led.Color = baseColor;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OneShotPulseEffect: {ex.Message}");
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
