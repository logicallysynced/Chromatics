using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class CloudsEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly double speed;
        private readonly double opacity;
        private readonly Color[] colors;
        private readonly Color baseColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly int _seed;
        private double _offset;

        public CloudsEffect(ListLedGroup _ledGroup, double speed, double opacity, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.speed = speed;
            this.opacity = Math.Clamp(opacity, 0.0, 1.0);
            this.colors = colors is { Length: > 0 } ? colors : [new Color(255, 255, 255)];
            this.baseColor = baseColor == default ? new Color(40, 90, 160) : baseColor;
            _seed = Environment.TickCount;

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
            _offset = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                _offset += speed * deltaTime;

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos)) { led.Color = baseColor; continue; }

                    // Two octaves of value noise scrolled along the column
                    // axis. The second octave at half amplitude breaks up the
                    // blobby first octave so clouds get ragged edges.
                    double n = ValueNoise(pos[0] * 0.45, pos[1] * 0.30 - _offset)
                             + ValueNoise(pos[0] * 0.90, pos[1] * 0.60 - _offset * 1.7) * 0.5;
                    n /= 1.5;

                    // Remap so roughly the lower half of the noise range is
                    // clear sky; clouds are distinct shapes, not full cover.
                    double cloud = Math.Clamp((n - 0.45) / 0.55, 0, 1);
                    float t = (float)(cloud * opacity);

                    if (t <= 0.01f)
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    // With multiple highlight colours the densest part of a
                    // cloud takes later slots, giving silver-lining banding.
                    int ci = Math.Min((int)(cloud * colors.Length), colors.Length - 1);
                    led.Color = LerpColor(baseColor, colors[ci], Math.Min(t, 1f));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloudsEffect: {ex.Message}");
            }
        }

        // Deterministic lattice hash -> [0,1). Standard value-noise
        // construction: hash the integer lattice corners, smoothstep-blend
        // between them.
        private double Hash(int x, int y)
        {
            unchecked
            {
                int h = _seed;
                h = h * 374761393 + x * 668265263;
                h = h * 374761393 + y * 2147483629;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return (h & 0x7FFFFFFF) / (double)int.MaxValue;
            }
        }

        private double ValueNoise(double x, double y)
        {
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            double fx = x - x0;
            double fy = y - y0;

            double sx = fx * fx * (3 - 2 * fx);
            double sy = fy * fy * (3 - 2 * fy);

            double n00 = Hash(x0, y0);
            double n10 = Hash(x0 + 1, y0);
            double n01 = Hash(x0, y0 + 1);
            double n11 = Hash(x0 + 1, y0 + 1);

            double nx0 = n00 + (n10 - n00) * sx;
            double nx1 = n01 + (n11 - n01) * sx;
            return nx0 + (nx1 - nx0) * sy;
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
