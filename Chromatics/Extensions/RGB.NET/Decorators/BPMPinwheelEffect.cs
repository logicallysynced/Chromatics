using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class BPMPinwheelEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly int bpm;
        private readonly double beatsPerCycle;
        private readonly int fanCount;
        private readonly double twist;
        private readonly Color[] colors;
        private readonly Color baseColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private double _rotation;

        public BPMPinwheelEffect(ListLedGroup _ledGroup, int bpm, double beatsPerCycle, int fanCount, double twist, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.beatsPerCycle = Math.Max(0.05, beatsPerCycle);
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

                // One blade-to-blade step per beatsPerCycle beats keeps the
                // wheel landing a fresh blade on every beat rather than one
                // full revolution per beat, which reads as a blur at club BPM.
                double beatInterval = 60.0 / bpm;
                double radiansPerSec = (Math.PI * 2 / fanCount) / (beatsPerCycle * beatInterval);
                _rotation += radiansPerSec * deltaTime;
                if (_rotation > Math.PI * 2) _rotation -= Math.PI * 2;

                double centerR = _maxRow / 2.0;
                double centerC = _maxCol / 2.0;
                double fanWidth = Math.PI * 2 / fanCount;

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos)) { led.Color = baseColor; continue; }

                    double dr = pos[0] - centerR;
                    double dc = pos[1] - centerC;
                    double dist = Math.Sqrt(dr * dr + dc * dc);
                    double angle = Math.Atan2(dr * 2.0, dc);

                    // Rotation subtracted so bands radiate outward from the
                    // centre as the wheel turns - same flow model as the
                    // non-BPM PinwheelEffect.
                    double a = angle - _rotation + twist * dist;
                    a %= Math.PI * 2;
                    if (a < 0) a += Math.PI * 2;

                    int fan = (int)(a / fanWidth) % fanCount;
                    led.Color = colors[fan % colors.Length];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMPinwheelEffect: {ex.Message}");
            }
        }
    }
}
