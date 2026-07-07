using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class ReactiveKeyboardEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly LedId startKey;
        private readonly double speed;
        private readonly double ringWidth;
        private readonly bool oneShot;
        private readonly Color[] colors;
        private readonly Color baseColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _maxExtent;
        private double _radius;
        private int _colorIndex;
        private double _originR;
        private double _originC;
        private bool _originResolved;
        private bool _finished;

        // True once a one-shot wave has left the board. Owners poll this to
        // remove the decorator, same as OneShotPulseEffect. Always false in
        // repeating mode.
        public bool IsFinished => _finished;

        public ReactiveKeyboardEffect(ListLedGroup _ledGroup, LedId startKey, double speed, double ringWidth, Color[] colors, RGBSurface surface, Color baseColor = default, bool oneShot = false) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.startKey = startKey;
            this.speed = Math.Max(0.1, speed);
            this.ringWidth = Math.Max(0.3, ringWidth);
            this.oneShot = oneShot;
            this.colors = colors is { Length: > 0 } ? colors : [new Color(0, 200, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
            _maxExtent = Math.Sqrt(_maxRow * _maxRow + _maxCol * _maxCol);
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
            _originResolved = false;
            _finished = false;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                if (!_originResolved)
                {
                    // The configured start key anchors the wave. Falls back to
                    // grid centre when the key isn't on this device (numpadless
                    // boards, mice, strips).
                    if (_grid.TryGetValue(startKey, out var origin))
                    {
                        _originR = origin[0];
                        _originC = origin[1];
                    }
                    else
                    {
                        _originR = _maxRow / 2.0;
                        _originC = _maxCol / 2.0;
                    }
                    _originResolved = true;
                }

                if (!_finished)
                {
                    _radius += speed * deltaTime;
                    if (_radius > _maxExtent + ringWidth * 2)
                    {
                        if (oneShot)
                        {
                            _finished = true;
                        }
                        else
                        {
                            _radius = 0;
                            _colorIndex++;
                        }
                    }
                }

                var waveColor = colors[_colorIndex % colors.Length];

                foreach (var led in ledGroup)
                {
                    if (_finished || !_grid.TryGetValue(led.Id, out var pos)) { led.Color = baseColor; continue; }

                    double dr = pos[0] - _originR;
                    double dc = pos[1] - _originC;
                    double dist = Math.Sqrt(dr * dr + dc * dc);

                    double ringDist = Math.Abs(dist - _radius);
                    if (ringDist < ringWidth)
                    {
                        float intensity = (float)(1.0 - ringDist / ringWidth);
                        // The wave loses energy as it travels so the far
                        // corners flash dimmer than keys near the origin.
                        intensity *= (float)(1.0 - Math.Clamp(_radius / (_maxExtent + ringWidth), 0, 0.6));
                        led.Color = LerpColor(baseColor, waveColor, Math.Min(intensity, 1f));
                    }
                    else
                    {
                        led.Color = baseColor;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReactiveKeyboardEffect: {ex.Message}");
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
