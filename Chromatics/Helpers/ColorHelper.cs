using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    public class ColorHelper
    {
        public static System.Drawing.Color RGBColorToColor(RGB.NET.Core.Color col)
        {
            return System.Drawing.Color.FromArgb((int)col.A, (int)col.R, (int)col.G, (int)col.B);
        }

        public static RGB.NET.Core.Color ColorToRGBColor(System.Drawing.Color col)
        {
            return new RGB.NET.Core.Color(col.A, col.R, col.G, col.B);
        }

    }
}
