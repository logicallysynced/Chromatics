using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class PinwheelEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly double speed;
        private readonly int fanCount;
        private readonly double twist;
        private readonly Color[] colors;
        private readonly Color baseColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private double _rotation;

        public PinwheelEffect(ListLedGroup _ledGroup, double speed, int fanCount, double twist, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.speed = speed;
            this.fanCount = Math.Max(1, fanCount);
            this.twist = twist;
            this.colors = colors is { Length: > 0 } ? colors : [new Color(255, 0, 0), new Color(0, 0, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

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
            _rotation = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                _rotation += speed * Math.PI / 180.0 * deltaTime;
                if (_rotation > Math.PI * 2) _rotation -= Math.PI * 2;
                if (_rotation < 0) _rotation += Math.PI * 2;

                double centerR = _maxRow / 2.0;
                double centerC = _maxCol / 2.0;
                double fanWidth = Math.PI * 2 / fanCount;

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos)) { led.Color = baseColor; continue; }

                    double dr = pos[0] - centerR;
                    double dc = pos[1] - centerC;
                    double dist = Math.Sqrt(dr * dr + dc * dc);

                    // Keyboard grids are ~3x wider than tall; compressing the
                    // row axis keeps the blades visually even instead of
                    // squashed into horizontal slivers.
                    double angle = Math.Atan2(dr * 2.0, dc);

                    // Adding rotation spins the wheel; adding twist * dist
                    // bends each blade into a spiral arm the further it is
                    // from the centre.
                    double a = angle + _rotation + twist * dist;
                    a %= Math.PI * 2;
                    if (a < 0) a += Math.PI * 2;

                    int fan = (int)(a / fanWidth) % fanCount;
                    led.Color = colors[fan % colors.Length];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PinwheelEffect: {ex.Message}");
            }
        }
    }
}
