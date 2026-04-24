using Chromatics.Core;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // A rotating wedge / radar sweep anchored to the grid centre. One full
    // 360° rotation completes every `beatsPerCycle` beats, so high BPM =
    // fast spin. Wedge width is in degrees; LEDs within that angular slice
    // of the current sweep angle light up, falling off smoothly across the
    // wedge edges. Multiple slot colours cycle as the sweep crosses LEDs.
    public class BPMSpinnerEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly int bpm;
        private readonly double beatsPerCycle;
        private readonly double wedgeDegrees;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _beatInterval;
        private double _angle; // radians

        public BPMSpinnerEffect(ListLedGroup _ledGroup, int bpm, double beatsPerCycle, double wedgeDegrees, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.beatsPerCycle = Math.Max(0.25, beatsPerCycle);
            this.wedgeDegrees = Math.Clamp(wedgeDegrees, 5, 180);
            this.colors = colors.Length > 0 ? colors : [new Color(0, 200, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
            _beatInterval = 60.0 / this.bpm;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                // angularSpeed in radians/sec: full revolution every (beatsPerCycle * beatInterval) seconds.
                double angularSpeed = (Math.PI * 2) / (beatsPerCycle * _beatInterval);
                _angle += deltaTime * angularSpeed;
                if (_angle > Math.PI * 2) _angle -= Math.PI * 2;

                double centreR = _maxRow / 2.0;
                double centreC = _maxCol / 2.0;
                double wedgeRad = wedgeDegrees * Math.PI / 180.0;

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    double dr = pos[0] - centreR;
                    double dc = pos[1] - centreC;
                    if (Math.Abs(dr) < 0.01 && Math.Abs(dc) < 0.01)
                    {
                        // Centre cell — always lit with the lead colour so
                        // the spin has a visible pivot.
                        led.Color = colors[0];
                        continue;
                    }

                    double ledAngle = Math.Atan2(dr, dc);
                    if (ledAngle < 0) ledAngle += Math.PI * 2;

                    // Shortest angular distance between sweep angle and LED angle.
                    double diff = Math.Abs(ledAngle - _angle);
                    if (diff > Math.PI) diff = Math.PI * 2 - diff;

                    if (diff > wedgeRad / 2)
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    // Intensity falls off across the wedge edges.
                    float intensity = (float)(1.0 - diff / (wedgeRad / 2));
                    intensity *= intensity;

                    // Pick colour by angle so the sweep paints different
                    // colours as it rotates around.
                    int colorIdx = (int)(_angle / (Math.PI * 2) * colors.Length) % colors.Length;
                    if (colorIdx < 0) colorIdx += colors.Length;
                    led.Color = LerpColor(baseColor, colors[colorIdx], intensity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMSpinnerEffect: {ex.Message}");
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
