using Chromatics.Helpers;
using RGB.NET.Core;
using RGB.NET.Presets.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Gradients
{
    class RectangularGradientTexture : AbstractGradientTexture
    {
        private readonly RectangularGradient gradient;

        public RectangularGradientTexture(Size size, RectangularGradient gradient) : base(size, gradient) {
            this.gradient = gradient;
        }

        protected override Color GetColor(in Point point) {
            var x = point.X / (float) Size.Width;
            var y = point.Y / (float) Size.Height;
            float lerpX = x;
            float lerpY = y;
            var top = System.Drawing.Color.FromArgb(
                (int) (gradient.topLeft.R + (gradient.topRight.R - gradient.topLeft.R) * lerpX),
                (int) (gradient.topLeft.G + (gradient.topRight.G - gradient.topLeft.G) * lerpX),
                (int) (gradient.topLeft.B + (gradient.topRight.B - gradient.topLeft.B) * lerpX)
            );

            var bottom = System.Drawing.Color.FromArgb(
                (int) (gradient.bottomLeft.R + (gradient.bottomRight.R - gradient.bottomLeft.R) * lerpX),
                (int) (gradient.bottomLeft.G + (gradient.bottomRight.G - gradient.bottomLeft.G) * lerpX),
                (int) (gradient.bottomLeft.B + (gradient.bottomRight.B - gradient.bottomLeft.B) * lerpX)
            );

            var final = System.Drawing.Color.FromArgb(
                (int) (top.R + (bottom.R - top.R) * lerpY),
                (int) (top.G + (bottom.G - top.G) * lerpY),
                (int) (top.B + (bottom.B - top.B) * lerpY)
            );

            return ColorHelper.ColorToRGBColor(final);
        }

    }
}
