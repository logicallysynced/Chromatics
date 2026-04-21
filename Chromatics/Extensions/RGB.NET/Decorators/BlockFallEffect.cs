using Chromatics.Core;
using Chromatics.Localization;
using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class BlockFallEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new Random();
        private readonly int blockSize;
        private readonly int numberOfBlocks;
        private readonly double fallSpeed;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly Direction fallDirection;

        // Cached once in the constructor — GetActiveGrid is expensive to call per-tick.
        private readonly Dictionary<LedId, int[]> _grid;
        private readonly int _maxRow;
        private readonly int _maxCol;

        private ConcurrentDictionary<int, Block> activeBlocks;
        private int _nextBlockKey;
        private double Timing;

        public enum Direction
        {
            TopToBottom,
            BottomToTop,
            LeftToRight,
            RightToLeft
        }

        private class Block
        {
            public double PositionR { get; set; }
            public double PositionC { get; set; }
            // Integer grid coords derived from the float position for rendering/bounds checks.
            public int Row => (int)Math.Floor(PositionR);
            public int Col => (int)Math.Floor(PositionC);
            public Color Color { get; set; }
            public double SpawnTime { get; set; }
        }

        public BlockFallEffect(ListLedGroup _ledGroup, int numberOfBlocks, int blockSize, double fallSpeed, Color[] colors, RGBSurface surface, Direction fallDirection, Color baseColor = default(Color)) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.numberOfBlocks = numberOfBlocks;
            this.blockSize = blockSize;
            this.fallSpeed = fallSpeed;
            this.colors = colors;
            this.baseColor = baseColor == default(Color) ? new Color(0, 0, 0) : baseColor;
            this.fallDirection = fallDirection;

            (_grid, _maxRow, _maxCol) = DeviceGridHelper.GetGrid(_ledGroup);

            activeBlocks = new ConcurrentDictionary<int, Block>();
            Timing = 0;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            activeBlocks.Clear();
            Timing = 0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null || activeBlocks == null) return;

                Timing += deltaTime;

                CreateNewBlocks();
                UpdateBlocks(deltaTime);

                foreach (var led in ledGroup)
                {
                    if (!_grid.TryGetValue(led.Id, out var pos)) { led.Color = baseColor; continue; }
                    var ledRow = pos[0];
                    var ledCol = pos[1];

                    var hit = activeBlocks.Values.FirstOrDefault(b =>
                        Math.Abs(b.Row - ledRow) < blockSize &&
                        Math.Abs(b.Col - ledCol) < blockSize);

                    led.Color = hit != null ? hit.Color : baseColor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
            }
        }

        private void CreateNewBlocks()
        {
            var needed = numberOfBlocks - activeBlocks.Count;
            if (needed <= 0) return;

            var availableLeds = ledGroup.Where(led => _grid.ContainsKey(led.Id)).ToList();
            var selected = availableLeds.OrderBy(_ => Guid.NewGuid()).Take(needed);

            foreach (var led in selected)
            {
                if (!_grid.TryGetValue(led.Id, out var pos)) continue;
                var (startR, startC) = GetStartPositionF(pos);
                var block = new Block
                {
                    PositionR  = startR,
                    PositionC  = startC,
                    Color      = colors[random.Next(colors.Length)],
                    SpawnTime  = Timing,
                };
                activeBlocks.TryAdd(_nextBlockKey++, block);
            }
        }

        private (double r, double c) GetStartPositionF(int[] position)
        {
            return fallDirection switch
            {
                Direction.TopToBottom => (-blockSize,              position[1]),
                Direction.BottomToTop => (_maxRow + blockSize,     position[1]),
                Direction.LeftToRight => (position[0],             -blockSize),
                Direction.RightToLeft => (position[0],             _maxCol + blockSize),
                _                     => (position[0],             position[1]),
            };
        }

        private void UpdateBlocks(double deltaTime)
        {
            var toRemove = new List<int>();

            foreach (var kvp in activeBlocks)
            {
                var b = kvp.Value;
                switch (fallDirection)
                {
                    case Direction.TopToBottom: b.PositionR += fallSpeed * deltaTime; break;
                    case Direction.BottomToTop: b.PositionR -= fallSpeed * deltaTime; break;
                    case Direction.LeftToRight: b.PositionC += fallSpeed * deltaTime; break;
                    case Direction.RightToLeft: b.PositionC -= fallSpeed * deltaTime; break;
                }

                if (IsOutOfBounds(b)) toRemove.Add(kvp.Key);
            }

            foreach (var key in toRemove)
                activeBlocks.TryRemove(key, out _);
        }

        private bool IsOutOfBounds(Block b)
            => b.PositionR < -blockSize || b.PositionR > _maxRow + blockSize
            || b.PositionC < -blockSize || b.PositionC > _maxCol + blockSize;
    }
}
