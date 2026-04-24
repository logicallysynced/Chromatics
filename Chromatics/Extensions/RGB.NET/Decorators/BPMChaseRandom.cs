using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // Beat-driven random chase. Every beat, picks `groupSize` LEDs that
    // weren't part of the previous beat's selection (no-repeat) and biases
    // toward LEDs that haven't been picked recently (spread). Optional
    // fadeBetween (seconds) cross-fades from the previous selection to the
    // new one — set 0 for an instant snap.
    public class BPMChaseRandom : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly int bpm;
        private readonly double fadeBetween;
        private readonly int groupSize;
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly double _beatInterval;
        private double _beatTimer;
        private HashSet<Led> _previousSelection = [];
        private HashSet<Led> _currentSelection = [];
        private Color _previousColor = default;
        private Color _currentColor = default;
        private double _transitionProgress = 1.0; // 1.0 = transition complete
        private int _colorIndex;
        // Maps each LED → "ticks since last picked". Higher = stronger
        // spread weighting next pick.
        private readonly Dictionary<Led, int> _staleness = new();

        public BPMChaseRandom(ListLedGroup _ledGroup, int bpm, double fadeBetween, int groupSize, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.fadeBetween = Math.Max(0.0, fadeBetween);
            this.groupSize = Math.Clamp(groupSize, 1, 8);
            this.colors = colors.Length > 0 ? colors : [new Color(0, 255, 255)];
            this.baseColor = baseColor == default ? new Color(0, 0, 0) : baseColor;
            _beatInterval = 60.0 / this.bpm;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
            foreach (var led in ledGroup) _staleness[led] = 1;
            _currentColor = colors[0];
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _previousSelection.Clear();
            _currentSelection.Clear();
            _staleness.Clear();
        }

        private void PickNext()
        {
            // Move current → previous so we can avoid immediate repeats.
            _previousSelection = _currentSelection;
            _previousColor = _currentColor;

            // Score every LED by staleness, exclude the previous selection
            // outright so no LED can be picked twice in a row, then take
            // the top `groupSize` weighted by staleness * jitter for spread.
            var pool = ledGroup.Where(l => !_previousSelection.Contains(l)).ToList();
            if (pool.Count == 0) pool = ledGroup.ToList<Led>();

            // Weighted shuffle: each LED's weight = staleness * random(0..1).
            // Take the top `groupSize` by weight.
            var picked = pool
                .Select(l => (led: l, weight: _staleness.GetValueOrDefault(l, 1) * random.NextDouble()))
                .OrderByDescending(x => x.weight)
                .Take(groupSize)
                .Select(x => x.led)
                .ToHashSet();

            _currentSelection = picked;
            _currentColor = colors[_colorIndex++ % colors.Length];

            // Reset staleness for picked LEDs, increment for everything else.
            foreach (var led in ledGroup)
            {
                if (picked.Contains(led)) _staleness[led] = 0;
                else _staleness[led] = _staleness.GetValueOrDefault(led, 1) + 1;
            }

            _transitionProgress = fadeBetween > 0 ? 0.0 : 1.0;
        }

        protected override void Update(double deltaTime)
        {
            try
            {
                if (ledGroup == null) return;

                _beatTimer += deltaTime;
                if (_beatTimer >= _beatInterval)
                {
                    _beatTimer -= _beatInterval;
                    PickNext();
                }

                if (_transitionProgress < 1.0 && fadeBetween > 0)
                {
                    _transitionProgress = Math.Min(1.0, _transitionProgress + deltaTime / fadeBetween);
                }

                float fadeT = (float)_transitionProgress;

                foreach (var led in ledGroup)
                {
                    bool inCurrent = _currentSelection.Contains(led);
                    bool inPrevious = _previousSelection.Contains(led);

                    Color target;
                    if (inCurrent && inPrevious)
                    {
                        // Held over (rare with the no-repeat exclusion, but
                        // possible if the pool was tiny). Stay on current.
                        target = _currentColor;
                    }
                    else if (inCurrent)
                    {
                        target = LerpColor(baseColor, _currentColor, fadeT);
                    }
                    else if (inPrevious)
                    {
                        target = LerpColor(_previousColor, baseColor, fadeT);
                    }
                    else
                    {
                        target = baseColor;
                    }

                    led.Color = target;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMChaseRandom: {ex.Message}");
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
