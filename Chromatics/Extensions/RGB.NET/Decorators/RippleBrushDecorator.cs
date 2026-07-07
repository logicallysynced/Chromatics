using RGB.NET.Core;
using System;
using System.Collections.Generic;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // Brush-pipeline counterpart to ReactiveKeyboardEffect /
    // OneShotPulseEffect. Those decorators detach their group and write LED
    // colors directly, which works in the harness (no other layers) but
    // loses to every attached base / dynamic brush in the real render
    // stack. This one renders the same expanding ring through
    // IBrushDecorator.ManipulateColor on an ATTACHED group's transparent
    // brush, so it composites at the group's ZIndex like the other effect
    // layers and bleeds through outside the ring (alpha 0 is a Color+
    // no-op).
    public class RippleBrushDecorator : AbstractUpdateAwareDecorator, IBrushDecorator
    {
        private readonly double speed;
        private readonly double ringWidth;
        private readonly Color ringColor;

        private readonly Dictionary<LedId, int[]> _grid;
        private readonly double _maxExtent;
        private readonly double _originR;
        private readonly double _originC;
        private double _radius;
        private bool _finished;

        // Owners poll this each tick and strip the decorator from the brush
        // once the ring has left the board.
        public bool IsFinished => _finished;

        // startKey anchors the ring; pass null to launch from the grid
        // centre. A startKey missing from this device's grid (numpadless
        // boards, mice, strips) also falls back to centre.
        public RippleBrushDecorator(ListLedGroup ledGroup, RGBSurface surface, LedId? startKey, double speed, double ringWidth, Color ringColor) : base(surface, updateIfDisabled: false)
        {
            this.speed = Math.Max(0.1, speed);
            this.ringWidth = Math.Max(0.3, ringWidth);
            this.ringColor = ringColor;

            (_grid, var maxRow, var maxCol) = DeviceGridHelper.GetGrid(ledGroup);

            if (startKey.HasValue && _grid.TryGetValue(startKey.Value, out var origin))
            {
                _originR = origin[0];
                _originC = origin[1];
            }
            else
            {
                _originR = maxRow / 2.0;
                _originC = maxCol / 2.0;
            }

            // Farthest corner from the origin, so the ring always clears the
            // whole board even when the origin sits off-centre.
            double farR = Math.Max(_originR, maxRow - _originR);
            double farC = Math.Max(_originC, maxCol - _originC);
            _maxExtent = Math.Sqrt(farR * farR + farC * farC);
        }

        protected override void Update(double deltaTime)
        {
            if (_finished) return;

            _radius += speed * deltaTime;
            if (_radius > _maxExtent + ringWidth * 2)
                _finished = true;
        }

        public void ManipulateColor(global::RGB.NET.Core.Rectangle rectangle, RenderTarget renderTarget, ref Color color)
        {
            if (_finished || !_grid.TryGetValue(renderTarget.Led.Id, out var pos))
            {
                color = Color.Transparent;
                return;
            }

            double dr = pos[0] - _originR;
            double dc = pos[1] - _originC;
            double dist = Math.Sqrt(dr * dr + dc * dc);

            double ringDist = Math.Abs(dist - _radius);
            if (ringDist < ringWidth)
            {
                float intensity = (float)(1.0 - ringDist / ringWidth);
                // The ring loses energy as it travels so the far edge of the
                // board flashes dimmer than keys near the origin.
                intensity *= (float)(1.0 - Math.Clamp(_radius / (_maxExtent + ringWidth), 0, 0.6));
                color = new Color(intensity, ringColor.R, ringColor.G, ringColor.B);
            }
            else
            {
                color = Color.Transparent;
            }
        }
    }
}
