using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static System.Drawing.Color GetInterpolatedColor<T>(T current, T min, T max, System.Drawing.Color color1, System.Drawing.Color color2)
        {
            var lambda = (Convert.ToDouble(current) - Convert.ToDouble(min)) / (Convert.ToDouble(max) - Convert.ToDouble(min));
            //var lambda2 = (1 - Math.Cos(lambda * Math.PI)) / 2;
            return ColorInterpolator.InterpolateBetween(color1, color2, lambda);
        }

        public static RGB.NET.Core.Color GetInterpolatedColor<T>(T current, T min, T max, RGB.NET.Core.Color color1, RGB.NET.Core.Color color2)
        {
            var lambda = (Convert.ToDouble(current) - Convert.ToDouble(min)) / (Convert.ToDouble(max) - Convert.ToDouble(min));
            //var lambda2 = (1 - Math.Cos(lambda * Math.PI)) / 2;
            return ColorToRGBColor(ColorInterpolator.InterpolateBetween(RGBColorToColor(color1), RGBColorToColor(color2), lambda));
        }
    }

    public class ColorInterpolator
    {
        public static System.Drawing.Color InterpolateBetween(System.Drawing.Color endPoint1, System.Drawing.Color endPoint2, double lambda)
        {
            // Convert colors to HSL
            var hsl1 = HSLColor.FromRGB(endPoint1);
            var hsl2 = HSLColor.FromRGB(endPoint2);

            // Interpolate HSL components
            float _lambda = Convert.ToSingle(lambda);

            var hue = hsl1.Hue + (hsl2.Hue - hsl1.Hue + 360) % 360 * _lambda;
            hue %= 360f;
            if(hue < 0f) hue += 360f;

            var saturation = hsl1.Saturation + (hsl2.Saturation - hsl1.Saturation) * _lambda;
            var lightness = hsl1.Luminosity + (hsl2.Luminosity - hsl1.Luminosity) * _lambda;

            // Convert HSL back to RGB
            var newColor = new HSLColor(hue, saturation, lightness).ToRGB();

            return newColor;
        }

        
    }

    #pragma warning disable 649,1823

    public class HSLColor
    {
        public float Hue;
        public float Saturation;
        public float Luminosity;

        public HSLColor(float H, float S, float L)
        {
            Hue = H;
            Saturation = S;
            Luminosity = L;
        }

        public static HSLColor FromRGB(System.Drawing.Color Clr)
        {
            return FromRGB(Clr.R, Clr.G, Clr.B);
        }

        public static HSLColor FromRGB(Byte R, Byte G, Byte B)
        {
            R = (byte)(R * 255);
            G = (byte)(G * 255);
            B = (byte)(B * 255);

            float _R = (R / 255.0f);
            float _G = (G / 255.0f);
            float _B = (B / 255.0f);

            float _Min = Math.Min(Math.Min(_R, _G), _B);
            float _Max = Math.Max(Math.Max(_R, _G), _B);
            float _Delta = _Max - _Min;

            float H = 0;
            float S, L;
            L = (Math.Max(_R, Math.Max(_G, _B)) + Math.Min(_R, Math.Min(_G, _B))) / 2;

            if (_Delta == 0)
            {
                S = 0;
            }
            else
            {
                if (L < 0.5f)
                {
                    S = _Delta / (_Max + _Min);
                }
                else
                {
                    S = _Delta / (2 - _Max - _Min);
                }

                if (_R == _Max)
                {
                    H = (_G - _B) / _Delta;
                }
                else if (_G == _Max)
                {
                    H = 2 + (_B - _R) / _Delta;
                }
                else if (_B == _Max)
                {
                    H = 4 + (_R - _G) / _Delta;
                }

                H *= 60f;
                if (H < 0) H += 360f;
            }

            return new HSLColor(H, S, L);
        }

        public System.Drawing.Color ToRGB()
        {
            byte r, g, b;
            if (Saturation == 0)
            {
                r = (byte)(Luminosity * 255);
                g = (byte)(Luminosity * 255);
                b = (byte)(Luminosity * 255);
            }
            else
            {
                double t1, t2;
                double th = Hue / 360f;

                if (Luminosity < 0.5d)
                {
                    t2 = Luminosity * (1d + Saturation);
                }
                else
                {
                    t2 = (Luminosity + Saturation) - (Luminosity * Saturation);
                }
                t1 = 2d * Luminosity - t2;

                double tr, tg, tb;
                tr = th + (1.0d / 3.0d);
                tg = th;
                tb = th - (1.0d / 3.0d);

                tr = ColorCalc(tr, t1, t2);
                tg = ColorCalc(tg, t1, t2);
                tb = ColorCalc(tb, t1, t2);
                r = Convert.ToByte(tr * 255);
                g = Convert.ToByte(tg * 255);
                b = Convert.ToByte(tb * 255);
            }
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        private static double ColorCalc(double c, double t1, double t2)
        {

            if (c < 0) c += 1d;
            if (c > 1) c -= 1d;
            if (6.0d * c < 1.0d) return t1 + (t2 - t1) * 6.0d * c;
            if (2.0d * c < 1.0d) return t2;
            if (3.0d * c < 2.0d) return t1 + (t2 - t1) * (2.0d / 3.0d - c) * 6.0d;
            return t1;
        }
    }

    #pragma warning restore 649,1823
}
