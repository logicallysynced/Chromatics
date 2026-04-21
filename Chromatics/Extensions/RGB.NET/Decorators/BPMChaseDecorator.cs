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
    // A colour packet orbiting the keyboard perimeter. On each beat the
    // packet's colour advances; between beats it smoothly moves around
    // the edge. Creates a "light chaser" / marquee effect synced to BPM.
    public class BPMChaseDecorator : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly int bpm;
        private readonly double tailLength;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _perimeter;
        private double _timing;
        private double _beatTimer;
        private double _beatInterval;
        private double _position;
        private int _colorIndex;

        public BPMChaseDecorator(ListLedGroup _ledGroup, int bpm, double tailLength, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.tailLength = Math.Max(1, tailLength);
            this.colors = colors.Length > 0 ? colors : [new Color(255, 0, 100)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            _beatInterval = 60.0 / this.bpm;
            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
            _perimeter = 2.0 * (_maxRow + _maxCol);
            if (_perimeter < 1) _perimeter = 1;
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
                _timing += deltaTime;
                _beatTimer += deltaTime;

                // Advance colour on each beat
                if (_beatTimer >= _beatInterval)
                {
                    _beatTimer -= _beatInterval;
                    _colorIndex++;
                }

                // Move position: one full lap per 4 beats
                double speed = _perimeter / (_beatInterval * 4);
                _position += speed * deltaTime;
                if (_position > _perimeter) _position -= _perimeter;

                var currentColor = colors[_colorIndex % colors.Length];

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    double perimPos = PointToPerimeter(pos[0], pos[1]);
                    double dist = PerimeterDistance(perimPos, _position);

                    if (dist < tailLength)
                    {
                        float intensity = (float)(1.0 - dist / tailLength);
                        intensity *= intensity;
                        led.Color = LerpColor(baseColor, currentColor, intensity);
                    }
                    else
                    {
                        led.Color = baseColor;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMChaseDecorator: {ex.Message}");
            }
        }

        // Map a grid position to a distance along the perimeter (clockwise from top-left)
        private double PointToPerimeter(int row, int col)
        {
            // Closest edge determines the perimeter position
            double distTop = row;
            double distBottom = _maxRow - row;
            double distLeft = col;
            double distRight = _maxCol - col;
            double minDist = Math.Min(Math.Min(distTop, distBottom), Math.Min(distLeft, distRight));

            if (minDist == distTop)       return col;                              // top edge
            if (minDist == distRight)      return _maxCol + row;                   // right edge
            if (minDist == distBottom)      return _maxCol + _maxRow + (_maxCol - col); // bottom edge
            return 2 * _maxCol + _maxRow + (_maxRow - row);                         // left edge
        }

        private double PerimeterDistance(double a, double b)
        {
            double d = Math.Abs(a - b);
            return Math.Min(d, _perimeter - d);
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
