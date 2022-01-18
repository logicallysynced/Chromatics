using Chromatics.Helpers;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Decorators
{
    public class ParticleDecorator : AbstractUpdateAwareDecorator, IBrushDecorator
    {
        public Color[] Colors { get; set; }
        public int Speed { get; set; } = 10;

        private IEnumerable<Led> _keys;

        private Color[] _colors;

        private int _speed;

        private Random rnd = new Random();

        private Dictionary<Led, ColorFader> _leds = new Dictionary<Led, ColorFader>();

        public ParticleDecorator(RGBSurface surface, bool updateIfDisabled = false) : base(surface, updateIfDisabled)
        {
            _keys = surface.Leds;
            //_colors = Colors;

            foreach (var led in surface.Leds)
            {
                var fader = new ColorFader(ColorHelper.RGBColorToColor(led.Color), ColorHelper.RGBColorToColor(_colors[1]), 10);

                if (!_leds.ContainsKey(led))
                    _leds.Add(led, fader);
            }
            
        }

        public override void OnAttached(IDecoratable decoratable)
        {
            base.OnAttached(decoratable);

            _colors = Colors;
            _speed = Speed;
        }

        public void ManipulateColor(in Rectangle rectangle, in RenderTarget renderTarget, ref Color color)
        {
            color = ColorHelper.ColorToRGBColor(_leds[renderTarget.Led].Fade().First());

        }

        protected override void Update(double deltaTime)
        {
            
            foreach (var led in _leds)
            {
                /*
                var r = rnd.Next(_colors.Length);
                var rndCol = _colors[r];
                */

                
                

                
                /*
                foreach (var c in fader.Fade())
                {
                    if (_leds.ContainsKey(led.Key))
                    {
                        led.Value.Color = ColorHelper.ColorToRGBColor(c);
                    }
                        
                }
                */

            }

            
        }
    }

    internal class ColorFader
    {
        private readonly System.Drawing.Color _From;
        private readonly System.Drawing.Color _To;

        private readonly double _StepR;
        private readonly double _StepG;
        private readonly double _StepB;

        private readonly uint _Steps;

        public ColorFader(System.Drawing.Color from, System.Drawing.Color to, uint steps)
        {
            if (steps == 0)
                throw new ArgumentException("Steps must be a positive number");

            _From = from;
            _To = to;
            _Steps = steps;

            _StepR = (double)(_To.R - _From.R) / _Steps;
            _StepG = (double)(_To.G - _From.G) / _Steps;
            _StepB = (double)(_To.B - _From.B) / _Steps;
        }

        public IEnumerable<System.Drawing.Color> Fade()
        {
            for (uint i = 0; i < _Steps; ++i)
            {
                yield return System.Drawing.Color.FromArgb((int)(_From.R + i * _StepR), (int)(_From.G + i * _StepG), (int)(_From.B + i * _StepB));
            }
            yield return _To; // make sure we always return the exact target color last
        }
    }
}
