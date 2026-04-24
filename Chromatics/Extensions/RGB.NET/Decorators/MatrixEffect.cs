using Chromatics.Core;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // The Matrix-style cascade. Each "drop" is a column travelling along
    // FallDirection with a glowing head and a fading tail. Drops continually
    // spawn at the leading edge to keep the cascade dense. Each lit cell
    // also flickers its opacity over time so the cascade reads as alive
    // rather than a smooth gradient.
    public class MatrixEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly double fallSpeed;
        private readonly double flickerOpacity;
        private readonly double flickerSpeed;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly MatrixDirection direction;
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;
        private readonly List<Drop> _drops = [];
        private readonly Dictionary<LedId, double> _flickerPhase = [];
        private double _spawnTimer;
        private const double TailLengthCells = 6.0;

        public enum MatrixDirection
        {
            Down,
            Up,
            Left,
            Right,
        }

        private class Drop
        {
            public double Lane;     // column for vertical, row for horizontal
            public double Position; // distance along the fall axis
            public double Speed;
            public Color Color;
        }

        public MatrixEffect(ListLedGroup _ledGroup, double fallSpeed, double flickerOpacity, double flickerSpeed, Color[] colors, RGBSurface surface, MatrixDirection direction = MatrixDirection.Down, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.fallSpeed = Math.Max(0.5, fallSpeed);
            this.flickerOpacity = Math.Clamp(flickerOpacity, 0.0, 1.0);
            this.flickerSpeed = Math.Max(0.0, flickerSpeed);
            this.colors = colors.Length > 0 ? colors : [new Color(0, 255, 100)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;
            this.direction = direction;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();

            // Seed each LED with a random flicker phase so the cascade
            // doesn't all flicker in lockstep on the first frame.
            foreach (var led in ledGroup)
                _flickerPhase[led.Id] = random.NextDouble() * Math.PI * 2;
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _drops.Clear();
            _flickerPhase.Clear();
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                bool vertical = direction is MatrixDirection.Down or MatrixDirection.Up;
                int laneCount = vertical ? _maxCol + 1 : _maxRow + 1;
                int axisLength = vertical ? _maxRow + 1 : _maxCol + 1;

                // Spawn new drops periodically — target density is roughly
                // half the lane count active at any time.
                _spawnTimer += deltaTime;
                double spawnInterval = (axisLength / fallSpeed) / Math.Max(1, laneCount);
                while (_spawnTimer >= spawnInterval && _drops.Count < laneCount * 2)
                {
                    _spawnTimer -= spawnInterval;
                    _drops.Add(new Drop
                    {
                        Lane = random.Next(0, laneCount),
                        Position = -TailLengthCells,
                        Speed = fallSpeed * (0.7 + random.NextDouble() * 0.6),
                        Color = colors[random.Next(colors.Length)],
                    });
                }

                double maxPosition = axisLength + TailLengthCells;
                for (int i = _drops.Count - 1; i >= 0; i--)
                {
                    _drops[i].Position += _drops[i].Speed * deltaTime;
                    if (_drops[i].Position > maxPosition)
                        _drops.RemoveAt(i);
                }

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos))
                    {
                        led.Color = baseColor;
                        continue;
                    }

                    int laneIndex = vertical ? pos[1] : pos[0];
                    int axisIndex = vertical ? pos[0] : pos[1];
                    if (direction is MatrixDirection.Up or MatrixDirection.Left)
                        axisIndex = axisLength - 1 - axisIndex;

                    float bestIntensity = 0;
                    Color bestColor = baseColor;

                    foreach (var drop in _drops)
                    {
                        if ((int)drop.Lane != laneIndex) continue;
                        double distFromHead = drop.Position - axisIndex;
                        if (distFromHead < 0 || distFromHead > TailLengthCells) continue;

                        // Head is brightest, tail fades quadratically.
                        float intensity = (float)(1.0 - distFromHead / TailLengthCells);
                        intensity *= intensity;
                        if (intensity > bestIntensity)
                        {
                            bestIntensity = intensity;
                            bestColor = drop.Color;
                        }
                    }

                    if (bestIntensity > 0)
                    {
                        // Per-LED opacity flicker. Sin-based so it's smooth
                        // rather than jittery. Phase advances per tick so the
                        // flicker continues even when LEDs aren't moving.
                        if (flickerOpacity > 0 && flickerSpeed > 0)
                        {
                            if (!_flickerPhase.TryGetValue(led.Id, out var phase))
                                phase = random.NextDouble() * Math.PI * 2;
                            phase += deltaTime * flickerSpeed;
                            _flickerPhase[led.Id] = phase;

                            // Map sin [-1,1] → [1-flickerOpacity, 1] so we
                            // never go above full brightness, only dim down.
                            float flicker = (float)(1.0 - flickerOpacity * (0.5 - 0.5 * Math.Sin(phase)));
                            bestIntensity *= flicker;
                        }

                        led.Color = LerpColor(baseColor, bestColor, bestIntensity);
                    }
                    else
                    {
                        led.Color = baseColor;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MatrixEffect: {ex.Message}");
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
