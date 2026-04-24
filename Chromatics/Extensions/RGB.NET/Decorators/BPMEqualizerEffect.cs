using Chromatics.Core;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // Audio-EQ vibe driven by BPM. Each column behaves like a frequency
    // bar that punches up to a randomised height on every beat then decays
    // back down. Bar heights are independent per column so the device
    // looks like an EQ display with bars dancing in time with the music.
    // Lower rows are coloured with the first slot, higher rows with the
    // later slots, producing the classic green→yellow→red EQ gradient.
    public class BPMEqualizerEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly int bpm;
        private readonly double decay; // seconds for a bar to fall full height
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly double _beatInterval;
        private double _beatTimer;
        private double[] _barHeight = [];

        public BPMEqualizerEffect(ListLedGroup _ledGroup, int bpm, double decay, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.decay = Math.Max(0.05, decay);
            this.colors = colors.Length > 0 ? colors : [new Color(0, 255, 0), new Color(255, 200, 0), new Color(255, 0, 0)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
            _beatInterval = 60.0 / this.bpm;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
            _barHeight = new double[_maxCol + 1];
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _barHeight = [];
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null || _barHeight.Length == 0) return;

                _beatTimer += deltaTime;
                if (_beatTimer >= _beatInterval)
                {
                    _beatTimer -= _beatInterval;
                    // Punch each column up to a fresh randomised height. We
                    // bias toward higher hits in the centre of the device so
                    // the EQ has a natural midfield-loudest shape.
                    for (int c = 0; c <= _maxCol; c++)
                    {
                        double centreBias = 1.0 - Math.Abs(c - _maxCol / 2.0) / Math.Max(1, _maxCol / 2.0);
                        double target = (0.4 + centreBias * 0.4 + random.NextDouble() * 0.4);
                        if (target > _barHeight[c])
                            _barHeight[c] = Math.Min(1.0, target);
                    }
                }

                double decayPerSec = 1.0 / decay;
                for (int c = 0; c < _barHeight.Length; c++)
                    _barHeight[c] = Math.Max(0, _barHeight[c] - deltaTime * decayPerSec);

                int rowCount = _maxRow + 1;
                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    int row = _maxRow - pos[0]; // 0 = bottom, _maxRow = top
                    int col = pos[1];
                    if (col < 0 || col >= _barHeight.Length)
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    double barTop = _barHeight[col] * rowCount;
                    if (row < barTop)
                    {
                        // Row colour: lower rows = first colour, top rows = last.
                        double rowFrac = rowCount > 1 ? (double)row / (rowCount - 1) : 0.0;
                        Color col1, col2;
                        float lerp;
                        if (colors.Length == 1)
                        {
                            led.Color = colors[0];
                            continue;
                        }
                        double scaled = rowFrac * (colors.Length - 1);
                        int idx = (int)scaled;
                        if (idx >= colors.Length - 1)
                        {
                            led.Color = colors[^1];
                            continue;
                        }
                        col1 = colors[idx];
                        col2 = colors[idx + 1];
                        lerp = (float)(scaled - idx);
                        led.Color = LerpColor(col1, col2, lerp);
                    }
                    else
                    {
                        led.Color = baseColor;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMEqualizerEffect: {ex.Message}");
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
