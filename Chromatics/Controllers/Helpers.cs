using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Controllers
{
    public class Helpers
    {
        public static string ConvertDevModeToString(DevModeTypes mode)
        {
            switch (mode)
            {
                case DevModeTypes.Disabled:
                    return "Disabled";
                case DevModeTypes.DefaultColor:
                    return "DefaultColor";
                case DevModeTypes.HighlightColor:
                    return "HighlightColor";
                case DevModeTypes.EnmityTracker:
                    return "EnmityTracker";
                case DevModeTypes.TargetHp:
                    return "TargetHp";
                case DevModeTypes.HpTracker:
                    return "HpTracker";
                case DevModeTypes.MpTracker:
                    return "MpTracker";
                case DevModeTypes.TpTracker:
                    return "TpTracker";
                case DevModeTypes.Castbar:
                    return "Castbar";
                case DevModeTypes.DutyFinder:
                    return "DutyFinder";
            }
            
            return "Disabled";
        }

        public static DevModeTypes ConvertStringToDevMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return DevModeTypes.Disabled;
                case "DefaultColor":
                    return DevModeTypes.DefaultColor;
                case "HighlightColor":
                    return DevModeTypes.HighlightColor;
                case "EnmityTracker":
                    return DevModeTypes.EnmityTracker;
                case "TargetHp":
                    return DevModeTypes.TargetHp;
                case "HpTracker":
                    return DevModeTypes.HpTracker;
                case "MpTracker":
                    return DevModeTypes.MpTracker;
                case "TpTracker":
                    return DevModeTypes.TpTracker;
                case "Castbar":
                    return DevModeTypes.Castbar;
                case "DutyFinder":
                    return DevModeTypes.DutyFinder;
            }
            
            return DevModeTypes.Disabled;
        }

        public static DevModeTypes ConvertCBToDevMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return DevModeTypes.Disabled;
                case "Default Color":
                    return DevModeTypes.DefaultColor;
                case "Highlight Color":
                    return DevModeTypes.HighlightColor;
                case "Enmity Tracker":
                    return DevModeTypes.EnmityTracker;
                case "Target HP":
                    return DevModeTypes.TargetHp;
                case "HP Tracker":
                    return DevModeTypes.HpTracker;
                case "MP Tracker":
                    return DevModeTypes.MpTracker;
                case "TP Tracker":
                    return DevModeTypes.TpTracker;
                case "Castbar":
                    return DevModeTypes.Castbar;
                case "Duty Finder Bell":
                    return DevModeTypes.DutyFinder;
            }

            return DevModeTypes.Disabled;
        }

        public static string ConvertDevModeToCB(DevModeTypes mode)
        {
            switch (mode)
            {
                case DevModeTypes.Disabled:
                    return "Disabled";
                case DevModeTypes.DefaultColor:
                    return "Default Color";
                case DevModeTypes.HighlightColor:
                    return "Highlight Color";
                case DevModeTypes.EnmityTracker:
                    return "Enmity Tracker";
                case DevModeTypes.TargetHp:
                    return "Target HP";
                case DevModeTypes.HpTracker:
                    return "HP Tracker";
                case DevModeTypes.MpTracker:
                    return "MP Tracker";
                case DevModeTypes.TpTracker:
                    return "TP Tracker";
                case DevModeTypes.Castbar:
                    return "Castbar";
                case DevModeTypes.DutyFinder:
                    return "Duty Finder Bell";
            }

            return "Disabled";
        }

        public struct ColorRGB
        {
            public byte R;
            public byte G;
            public byte B;
            public ColorRGB(System.Drawing.Color value)
            {
                this.R = value.R;
                this.G = value.G;
                this.B = value.B;
            }
            public static implicit operator System.Drawing.Color(ColorRGB rgb)
            {
                System.Drawing.Color c = System.Drawing.Color.FromArgb(rgb.R, rgb.G, rgb.B);
                return c;
            }
            public static explicit operator ColorRGB(System.Drawing.Color c)
            {
                return new ColorRGB(c);
            }
        }

        public static ColorRGB HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;

            r = l; // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int) h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            ColorRGB rgb;
            rgb.R = Convert.ToByte(r * 255.0f);
            rgb.G = Convert.ToByte(g * 255.0f);
            rgb.B = Convert.ToByte(b * 255.0f);
            return rgb;
        }

        public static void RGB2HSL(ColorRGB rgb, out double h, out double s, out double l)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;
            double v;
            double m;
            double vm;
            double r2, g2, b2;

            h = 0; // default to black
            s = 0;
            l = 0;
            v = Math.Max(r, g);
            v = Math.Max(v, b);
            m = Math.Min(r, g);
            m = Math.Min(m, b);
            l = (m + v) / 2.0;
            if (l <= 0.0)
            {
                return;
            }
            vm = v - m;
            s = vm;
            if (s > 0.0)
            {
                s /= (l <= 0.5) ? (v + m) : (2.0 - v - m);
            }
            else
            {
                return;
            }
            r2 = (v - r) / vm;
            g2 = (v - g) / vm;
            b2 = (v - b) / vm;
            if (r == v)
            {
                h = (g == m ? 5.0 + b2 : 1.0 - g2);
            }
            else if (g == v)
            {
                h = (b == m ? 1.0 + r2 : 3.0 - b2);
            }
            else
            {
                h = (r == m ? 3.0 + g2 : 5.0 - r2);
            }

            if (h >= 6f) h -= 6f;
            if (h < 0f) h += 6f;
            h /= 6.0;
        }
    }
}
