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
        private ConcurrentDictionary<int, Block> activeBlocks;
        private double Timing;
        private Direction fallDirection;

        public enum Direction
        {
            TopToBottom,
            BottomToTop,
            LeftToRight,
            RightToLeft
        }

        private class Block
        {
            public int[] Position { get; set; }
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
            this.baseColor = baseColor == default(Color) ? new Color(0, 0, 0) : baseColor; // Default to black if not specified
            this.fallDirection = fallDirection;

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
                    if (KeyLocalization.QWERTY_Grid.TryGetValue(led.Id, out var position) && activeBlocks.Values.Any(b => b.Position.SequenceEqual(position)))
                    {
                        var block = activeBlocks.Values.First(b => b.Position.SequenceEqual(position));
                        led.Color = block.Color;
                    }
                    else
                    {
                        led.Color = baseColor;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"Exception: {ex.Message}");
#endif
            }
        }

        private void CreateNewBlocks()
        {
            var availableLeds = ledGroup.Where(led => KeyLocalization.QWERTY_Grid.ContainsKey(led.Id));
            var selectedLeds = availableLeds.OrderBy(x => Guid.NewGuid()).Take(numberOfBlocks);

            foreach (var led in selectedLeds)
            {
                if (!KeyLocalization.QWERTY_Grid.TryGetValue(led.Id, out var position)) continue;
                var colorIndex = random.Next(colors.Length);
                var startPosition = GetStartPosition(position);

                var block = new Block
                {
                    Position = startPosition,
                    Color = colors[colorIndex],
                    SpawnTime = Timing
                };
                activeBlocks.TryAdd(activeBlocks.Count, block);
            }
        }

        private int[] GetStartPosition(int[] position)
        {
            switch (fallDirection)
            {
                case Direction.TopToBottom:
                    return new int[] { -blockSize, position[1] };
                case Direction.BottomToTop:
                    return new int[] { KeyLocalization.QWERTY_Grid.Values.Max(p => p[0]) + blockSize, position[1] };
                case Direction.LeftToRight:
                    return new int[] { position[0], -blockSize };
                case Direction.RightToLeft:
                    return new int[] { position[0], KeyLocalization.QWERTY_Grid.Values.Max(p => p[1]) + blockSize };
                default:
                    return position;
            }
        }

        private void UpdateBlocks(double deltaTime)
        {
            var blocksToRemove = new List<int>();

            foreach (var kvp in activeBlocks)
            {
                var block = kvp.Value;

                switch (fallDirection)
                {
                    case Direction.TopToBottom:
                        block.Position[0] += (int)(fallSpeed * deltaTime);
                        break;
                    case Direction.BottomToTop:
                        block.Position[0] -= (int)(fallSpeed * deltaTime);
                        break;
                    case Direction.LeftToRight:
                        block.Position[1] += (int)(fallSpeed * deltaTime);
                        break;
                    case Direction.RightToLeft:
                        block.Position[1] -= (int)(fallSpeed * deltaTime);
                        break;
                }

                if (IsOutOfBounds(block.Position))
                {
                    blocksToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in blocksToRemove)
            {
                activeBlocks.TryRemove(key, out _);
            }
        }

        private bool IsOutOfBounds(int[] position)
        {
            var maxRow = KeyLocalization.QWERTY_Grid.Values.Max(p => p[0]);
            var maxCol = KeyLocalization.QWERTY_Grid.Values.Max(p => p[1]);

            return position[0] < -blockSize || position[0] > maxRow + blockSize || position[1] < -blockSize || position[1] > maxCol + blockSize;
        }

        private class IntArrayEqualityComparer : IEqualityComparer<int[]>
        {
            public bool Equals(int[] x, int[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(int[] obj)
            {
                return obj.Aggregate(17, (current, value) => current * 23 + value.GetHashCode());
            }
        }
    }
}