using Chromatics.Helpers;
using RGB.NET.Core;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Gradients
{
    class RectangularGradient : AbstractGradient
    {
        public readonly System.Drawing.Color topLeft;
        public readonly System.Drawing.Color topRight;
        public readonly System.Drawing.Color bottomLeft;
        public readonly System.Drawing.Color bottomRight;

        public RectangularGradient(System.Drawing.Color topLeft, System.Drawing.Color topRight, System.Drawing.Color bottomLeft, System.Drawing.Color bottomRight) {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomLeft = bottomLeft;
            this.bottomRight = bottomRight;
        }

        public override Color GetColor(float value) 
        {
            var x = value % 1;
            var y = (int)value / 1;
            System.Drawing.Color c1 = System.Drawing.Color.FromArgb(
                (int) (topLeft.R + (topRight.R - topLeft.R) * x),
                (int) (topLeft.G + (topRight.G - topLeft.G) * x),
                (int) (topLeft.B + (topRight.B - topLeft.B) * x)
            );

            System.Drawing.Color c2 = System.Drawing.Color.FromArgb(
                (int) (bottomLeft.R + (bottomRight.R - bottomLeft.R) * x),
                (int) (bottomLeft.G + (bottomRight.G - bottomLeft.G) * x),
                (int) (bottomLeft.B + (bottomRight.B - bottomLeft.B) * x)
            );

            var final = System.Drawing.Color.FromArgb(
                (int) (c1.R + (c2.R - c1.R) * y),
                (int) (c1.G + (c2.G - c1.G) * y),
                (int) (c1.B + (c2.B - c1.B) * y)
            );

            return ColorHelper.ColorToRGBColor(final);
        }
    }
}
