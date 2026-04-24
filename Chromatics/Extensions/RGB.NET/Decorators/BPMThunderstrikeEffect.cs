using Chromatics.Core;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    // Rock / metal vibe. Every beat fires a "strike": a hot flash on a
    // random subset of LEDs that slams to full brightness then decays
    // exponentially. On strong beats (every `accentEvery` beats) the strike
    // is wider and brighter to give the cadence a heavier downbeat feel.
    // Underlying base color stays dark/red/orange so the flashes pop.
    public class BPMThunderstrikeEffect : AbstractUpdateAwareDecorator, ILedGroupDecorator
    {
        private readonly ListLedGroup ledGroup;
        private readonly Random random = new();
        private readonly int bpm;
        private readonly int accentEvery;
        private readonly double decay; // seconds to drop from 1.0 to ~0
        private readonly Color[] colors;
        private readonly Color baseColor;
        private readonly double _beatInterval;
        private double _beatTimer;
        private int _beatIndex;
        private readonly Dictionary<Led, double> _strikeIntensity = new();
        private readonly Dictionary<Led, Color> _strikeColor = new();

        public BPMThunderstrikeEffect(ListLedGroup _ledGroup, int bpm, int accentEvery, double decay, Color[] colors, RGBSurface surface, Color baseColor = default) : base(surface, updateIfDisabled: false)
        {
            this.ledGroup = _ledGroup;
            this.bpm = Math.Max(1, bpm);
            this.accentEvery = Math.Max(1, accentEvery);
            this.decay = Math.Max(0.05, decay);
            this.colors = colors.Length > 0 ? colors : [new Color(255, 220, 180)];
            this.baseColor = baseColor == default ? new Color(40, 0, 0) : baseColor;
            _beatInterval = 60.0 / this.bpm;
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);
            ledGroup.Detach();
        }

        public override void OnDetached(IDecoratable decoratable)
        {
            base.OnDetached(decoratable);
            _strikeIntensity.Clear();
            _strikeColor.Clear();
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
                    Strike();
                }

                // Exponential-ish decay per LED.
                double decayPerSec = 1.0 / decay;
                foreach (var led in ledGroup)
                {
                    double i = _strikeIntensity.GetValueOrDefault(led, 0);
                    if (i > 0)
                    {
                        i = Math.Max(0, i - deltaTime * decayPerSec);
                        _strikeIntensity[led] = i;
                    }

                    if (i <= 0)
                    {
                        led.Color = baseColor;
                    }
                    else
                    {
                        var col = _strikeColor.GetValueOrDefault(led, colors[0]);
                        led.Color = LerpColor(baseColor, col, (float)i);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BPMThunderstrikeEffect: {ex.Message}");
            }
        }

        private void Strike()
        {
            _beatIndex++;
            bool accent = (_beatIndex % accentEvery) == 0;
            var pool = ledGroup.ToArray<Led>();
            int total = pool.Length;
            if (total == 0) return;

            // Strike density: regular beat hits ~15% of LEDs, accent hits ~45%.
            double targetFraction = accent ? 0.45 : 0.15;
            int hitCount = Math.Max(1, (int)(total * targetFraction));
            var color = colors[(_beatIndex / accentEvery) % colors.Length];
            // Fisher-Yates partial shuffle
            for (int i = 0; i < hitCount && i < pool.Length; i++)
            {
                int j = i + random.Next(pool.Length - i);
                (pool[i], pool[j]) = (pool[j], pool[i]);
                var led = pool[i];
                _strikeIntensity[led] = accent ? 1.0 : 0.85;
                _strikeColor[led] = color;
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
