using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Controllers
{
    public class Helpers
    {
        public static class FFXIVInterpolation
        {
            public static int Interpolate_Int(int current, int min, int max, int targetHigh, int targetLow)
            {
                return (current - min) * (targetHigh - targetLow) / (max - min) + targetLow;
            }

            public static double Interpolate_Double(double current, double min, double max, int targetHigh, int targetLow)
            {
                return (current - min) * (targetHigh - targetLow) / (max - min) + targetLow;
            }

            public static long Interpolate_Long(long current, long min, long max, long targetHigh, long targetLow)
            {
                return (current - min) * (targetHigh - targetLow) / (max - min) + targetLow;
            }
        }

        public static class EnumUtil
        {
            public static IEnumerable<T> GetValues<T>()
            {
                return Enum.GetValues(typeof(T)).Cast<T>();
            }
        }

        public static string GetCsvData(string url, string csvPath)
        {
            var client = new WebClient();
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/" + csvPath;

            client.DownloadFile(new Uri(url), path);

            if (File.Exists(path))
            {
                return path;
            }

            Console.WriteLine(@"An error occurred downloading the file " + csvPath + @" from URI: " + url);
            return string.Empty;
        }

        public static string RemoveMatchingBraces(string s)
        {
            var stack = new Stack<char>();
            int count = 0;
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '(':
                        count += 1;
                        stack.Push(ch);
                        break;
                    case ')':
                        if (count == 0)
                            stack.Push(ch);
                        else
                        {
                            char popped;
                            do
                            {
                                popped = stack.Pop();
                            } while (popped != '(');
                            count -= 1;
                        }
                        break;
                    default:
                        stack.Push(ch);
                        break;
                }
            }
            return string.Join("", stack.Reverse());
        }

        public static int ToPercentage(double d)
        {
            return (int)(d * 100);
        }

        //Y = ( ( X - X1 )( Y2 - Y1) / ( X2 - X1) ) + Y1
        public static double LinIntDouble(int ValMin, int ValMax, int Val, int OutMin, int OutMax)
        {
            return (Val - ValMin) * (OutMax - OutMin) / (ValMax - ValMin) + OutMin;
        }

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
                case DevModeTypes.ACTTracker:
                    return "ACTTracker";
            }
            
            return "Disabled";
        }

        public static string ConvertDevMultiModeToString(DevMultiModeTypes mode)
        {
            switch (mode)
            {
                case DevMultiModeTypes.Disabled:
                    return "Disabled";
                case DevMultiModeTypes.DefaultColor:
                    return "DefaultColor";
                case DevMultiModeTypes.HighlightColor:
                    return "HighlightColor";
                case DevMultiModeTypes.EnmityTracker:
                    return "EnmityTracker";
                case DevMultiModeTypes.TargetHp:
                    return "TargetHp";
                case DevMultiModeTypes.HpTracker:
                    return "HpTracker";
                case DevMultiModeTypes.MpTracker:
                    return "MpTracker";
                case DevMultiModeTypes.TpTracker:
                    return "TpTracker";
                case DevMultiModeTypes.Castbar:
                    return "Castbar";
                case DevMultiModeTypes.DutyFinder:
                    return "DutyFinder";
                case DevMultiModeTypes.ReactiveWeather:
                    return "ReactiveWeather";
                case DevMultiModeTypes.StatusEffects:
                    return "StatusEffects";
                case DevMultiModeTypes.ACTTracker:
                    return "ACTTracker";
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
                case "ACTTracker":
                    return DevModeTypes.ACTTracker;
            }
            
            return DevModeTypes.Disabled;
        }

        public static DevMultiModeTypes ConvertStringToDevMultiMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return DevMultiModeTypes.Disabled;
                case "DefaultColor":
                    return DevMultiModeTypes.DefaultColor;
                case "HighlightColor":
                    return DevMultiModeTypes.HighlightColor;
                case "EnmityTracker":
                    return DevMultiModeTypes.EnmityTracker;
                case "TargetHp":
                    return DevMultiModeTypes.TargetHp;
                case "HpTracker":
                    return DevMultiModeTypes.HpTracker;
                case "MpTracker":
                    return DevMultiModeTypes.MpTracker;
                case "TpTracker":
                    return DevMultiModeTypes.TpTracker;
                case "Castbar":
                    return DevMultiModeTypes.Castbar;
                case "DutyFinder":
                    return DevMultiModeTypes.DutyFinder;
                case "ReactiveWeather":
                    return DevMultiModeTypes.ReactiveWeather;
                case "StatusEffects":
                    return DevMultiModeTypes.StatusEffects;
                case "ACTTracker":
                    return DevMultiModeTypes.ACTTracker;
            }

            return DevMultiModeTypes.Disabled;
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
                case "ACT Tracker":
                    return DevModeTypes.ACTTracker;
            }

            return DevModeTypes.Disabled;
        }

        public static DevMultiModeTypes ConvertCBToDevMultiMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return DevMultiModeTypes.Disabled;
                case "Default Color":
                    return DevMultiModeTypes.DefaultColor;
                case "Highlight Color":
                    return DevMultiModeTypes.HighlightColor;
                case "Enmity Tracker":
                    return DevMultiModeTypes.EnmityTracker;
                case "Target HP":
                    return DevMultiModeTypes.TargetHp;
                case "HP Tracker":
                    return DevMultiModeTypes.HpTracker;
                case "MP Tracker":
                    return DevMultiModeTypes.MpTracker;
                case "TP Tracker":
                    return DevMultiModeTypes.TpTracker;
                case "Castbar":
                    return DevMultiModeTypes.Castbar;
                case "Duty Finder Bell":
                    return DevMultiModeTypes.DutyFinder;
                case "Reactive Weather":
                    return DevMultiModeTypes.ReactiveWeather;
                case "Status Effects":
                    return DevMultiModeTypes.StatusEffects;
                case "ACT Tracker":
                    return DevMultiModeTypes.ACTTracker;
            }

            return DevMultiModeTypes.Disabled;
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
                case DevModeTypes.ACTTracker:
                    return "ACT Tracker";
            }

            return "Disabled";
        }

        public static string ConvertDevMultiModeToCB(DevMultiModeTypes mode)
        {
            switch (mode)
            {
                case DevMultiModeTypes.Disabled:
                    return "Disabled";
                case DevMultiModeTypes.DefaultColor:
                    return "Default Color";
                case DevMultiModeTypes.HighlightColor:
                    return "Highlight Color";
                case DevMultiModeTypes.EnmityTracker:
                    return "Enmity Tracker";
                case DevMultiModeTypes.TargetHp:
                    return "Target HP";
                case DevMultiModeTypes.HpTracker:
                    return "HP Tracker";
                case DevMultiModeTypes.MpTracker:
                    return "MP Tracker";
                case DevMultiModeTypes.TpTracker:
                    return "TP Tracker";
                case DevMultiModeTypes.Castbar:
                    return "Castbar";
                case DevMultiModeTypes.DutyFinder:
                    return "Duty Finder Bell";
                case DevMultiModeTypes.ReactiveWeather:
                    return "Reactive Weather";
                case DevMultiModeTypes.StatusEffects:
                    return "Status Effects";
                case DevMultiModeTypes.ACTTracker:
                    return "ACT Tracker";
            }

            return "Disabled";
        }

        public static string ConvertLightbarModeToString(LightbarMode mode)
        {
            switch (mode)
            {
                case LightbarMode.Disabled:
                    return "Disabled";
                case LightbarMode.DefaultColor:
                    return "DefaultColor";
                case LightbarMode.HighlightColor:
                    return "HighlightColor";
                case LightbarMode.EnmityTracker:
                    return "EnmityTracker";
                case LightbarMode.TargetHp:
                    return "TargetHp";
                case LightbarMode.HpTracker:
                    return "HpTracker";
                case LightbarMode.MpTracker:
                    return "MpTracker";
                case LightbarMode.TpTracker:
                    return "TpTracker";
                case LightbarMode.Castbar:
                    return "Castbar";
                case LightbarMode.DutyFinder:
                    return "DutyFinder";
                case LightbarMode.CurrentExp:
                    return "CurrentExp";
                case LightbarMode.JobGauge:
                    return "JobGauge";
                case LightbarMode.PullCountdown:
                    return "PullCountdown";
                case LightbarMode.ACTTracker:
                    return "ACTTracker";
                case LightbarMode.ACTEnrage:
                    return "ACTEnrage";
            }

            return "Disabled";
        }

        public static LightbarMode ConvertStringToLightbarMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return LightbarMode.Disabled;
                case "DefaultColor":
                    return LightbarMode.DefaultColor;
                case "HighlightColor":
                    return LightbarMode.HighlightColor;
                case "EnmityTracker":
                    return LightbarMode.EnmityTracker;
                case "TargetHp":
                    return LightbarMode.TargetHp;
                case "HpTracker":
                    return LightbarMode.HpTracker;
                case "MpTracker":
                    return LightbarMode.MpTracker;
                case "TpTracker":
                    return LightbarMode.TpTracker;
                case "Castbar":
                    return LightbarMode.Castbar;
                case "DutyFinder":
                    return LightbarMode.DutyFinder;
                case "CurrentExp":
                    return LightbarMode.CurrentExp;
                case "JobGauge":
                    return LightbarMode.JobGauge;
                case "PullCountdown":
                    return LightbarMode.PullCountdown;
                case "ACTTracker":
                    return LightbarMode.ACTTracker;
                case "ACTEnrage":
                    return LightbarMode.ACTEnrage;
            }

            return LightbarMode.Disabled;
        }

        public static LightbarMode ConvertCBToLightbarMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return LightbarMode.Disabled;
                case "Default Color":
                    return LightbarMode.DefaultColor;
                case "Highlight Color":
                    return LightbarMode.HighlightColor;
                case "Enmity Tracker":
                    return LightbarMode.EnmityTracker;
                case "Target HP":
                    return LightbarMode.TargetHp;
                case "HP Tracker":
                    return LightbarMode.HpTracker;
                case "MP Tracker":
                    return LightbarMode.MpTracker;
                case "TP Tracker":
                    return LightbarMode.TpTracker;
                case "Castbar":
                    return LightbarMode.Castbar;
                case "Duty Finder Bell":
                    return LightbarMode.DutyFinder;
                case "Experience Tracker":
                    return LightbarMode.CurrentExp;
                case "Job Gauge":
                    return LightbarMode.JobGauge;
                case "Pull Countdown":
                    return LightbarMode.PullCountdown;
                case "ACT Tracker":
                    return LightbarMode.ACTTracker;
                case "ACT Enrage":
                    return LightbarMode.ACTEnrage;
            }

            return LightbarMode.Disabled;
        }

        public static string ConvertLightbarModeToCB(LightbarMode mode)
        {
            switch (mode)
            {
                case LightbarMode.Disabled:
                    return "Disabled";
                case LightbarMode.DefaultColor:
                    return "Default Color";
                case LightbarMode.HighlightColor:
                    return "Highlight Color";
                case LightbarMode.EnmityTracker:
                    return "Enmity Tracker";
                case LightbarMode.TargetHp:
                    return "Target HP";
                case LightbarMode.HpTracker:
                    return "HP Tracker";
                case LightbarMode.MpTracker:
                    return "MP Tracker";
                case LightbarMode.TpTracker:
                    return "TP Tracker";
                case LightbarMode.Castbar:
                    return "Castbar";
                case LightbarMode.DutyFinder:
                    return "Duty Finder Bell";
                case LightbarMode.CurrentExp:
                    return "Experience Tracker";
                case LightbarMode.JobGauge:
                    return "Job Gauge";
                case LightbarMode.PullCountdown:
                    return "Pull Countdown";
                case LightbarMode.ACTTracker:
                    return "ACT Tracker";
                case LightbarMode.ACTEnrage:
                    return "ACT Enrage";
            }

            return "Disabled";
        }

        public static string ConvertFKeyModeToString(FKeyMode mode)
        {
            switch (mode)
            {
                case FKeyMode.Disabled:
                    return "Disabled";
                case FKeyMode.DefaultColor:
                    return "DefaultColor";
                case FKeyMode.HighlightColor:
                    return "HighlightColor";
                case FKeyMode.EnmityTracker:
                    return "EnmityTracker";
                case FKeyMode.TargetHp:
                    return "TargetHp";
                case FKeyMode.HpTracker:
                    return "HpTracker";
                case FKeyMode.MpTracker:
                    return "MpTracker";
                case FKeyMode.TpTracker:
                    return "TpTracker";
                case FKeyMode.HpMpTp:
                    return "HpMpTp";
                case FKeyMode.CurrentExp:
                    return "CurrentExp";
                case FKeyMode.JobGauge:
                    return "JobGauge";
                case FKeyMode.PullCountdown:
                    return "PullCountdown";
                case FKeyMode.ACTTracker:
                    return "ACTTracker";
                case FKeyMode.ACTEnrage:
                    return "ACTEnrage";
            }

            return "HpMpTp";
        }

        public static FKeyMode ConvertStringToFKeyMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return FKeyMode.Disabled;
                case "DefaultColor":
                    return FKeyMode.DefaultColor;
                case "HighlightColor":
                    return FKeyMode.HighlightColor;
                case "EnmityTracker":
                    return FKeyMode.EnmityTracker;
                case "TargetHp":
                    return FKeyMode.TargetHp;
                case "HpTracker":
                    return FKeyMode.HpTracker;
                case "MpTracker":
                    return FKeyMode.MpTracker;
                case "TpTracker":
                    return FKeyMode.TpTracker;
                case "HpMpTp":
                    return FKeyMode.HpMpTp;
                case "CurrentExp":
                    return FKeyMode.CurrentExp;
                case "JobGauge":
                    return FKeyMode.JobGauge;
                case "PullCountdown":
                    return FKeyMode.PullCountdown;
                case "ACTTracker":
                    return FKeyMode.ACTTracker;
                case "ACTEnrage":
                    return FKeyMode.ACTEnrage;
            }

            return FKeyMode.HpMpTp;
        }

        public static FKeyMode ConvertCBToFKeyMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return FKeyMode.Disabled;
                case "Default Color":
                    return FKeyMode.DefaultColor;
                case "Highlight Color":
                    return FKeyMode.HighlightColor;
                case "Enmity Tracker":
                    return FKeyMode.EnmityTracker;
                case "Target HP":
                    return FKeyMode.TargetHp;
                case "HP Tracker":
                    return FKeyMode.HpTracker;
                case "MP Tracker":
                    return FKeyMode.MpTracker;
                case "TP Tracker":
                    return FKeyMode.TpTracker;
                case "HP/MP/TP":
                    return FKeyMode.HpMpTp;
                case "Experience Tracker":
                    return FKeyMode.CurrentExp;
                case "Job Gauge":
                    return FKeyMode.JobGauge;
                case "Pull Countdown":
                    return FKeyMode.PullCountdown;
                case "ACT Tracker":
                    return FKeyMode.ACTTracker;
                case "ACT Enrage":
                    return FKeyMode.ACTEnrage;
            }

            return FKeyMode.HpMpTp;
        }

        public static string ConvertFKeyModeToCB(FKeyMode mode)
        {
            switch (mode)
            {
                case FKeyMode.Disabled:
                    return "Disabled";
                case FKeyMode.DefaultColor:
                    return "Default Color";
                case FKeyMode.HighlightColor:
                    return "Highlight Color";
                case FKeyMode.EnmityTracker:
                    return "Enmity Tracker";
                case FKeyMode.TargetHp:
                    return "Target HP";
                case FKeyMode.HpTracker:
                    return "HP Tracker";
                case FKeyMode.MpTracker:
                    return "MP Tracker";
                case FKeyMode.TpTracker:
                    return "TP Tracker";
                case FKeyMode.HpMpTp:
                    return "HP/MP/TP";
                case FKeyMode.CurrentExp:
                    return "Experience Tracker";
                case FKeyMode.JobGauge:
                    return "Job Gauge";
                case FKeyMode.PullCountdown:
                    return "Pull Countdown";
                case FKeyMode.ACTTracker:
                    return "ACT Tracker";
                case FKeyMode.ACTEnrage:
                    return "ACT Enrage";
            }

            return "HpMpTp";
        }

        public static readonly Dictionary<int, int> ExperienceTable = new Dictionary<int, int>()
        {
            {0, 300},
            {1, 300},
            {2, 600},
            {3, 1100},
            {4, 1700},
            {5, 2300},
            {6, 4200},
            {7, 6000},
            {8, 7350},
            {9, 9930},
            {10, 11800},
            {11, 15600},
            {12, 19600},
            {13, 23700},
            {14, 26400},
            {15, 30500},
            {16, 35400},
            {17, 40500},
            {18, 45700},
            {19, 51000},
            {20, 56600},
            {21, 63900},
            {22, 71400},
            {23, 79100},
            {24, 87100},
            {25, 95200},
            {26, 109800},
            {27, 124800},
            {28, 140200},
            {29, 155900},
            {30, 162500},
            {31, 175900},
            {32, 189600},
            {33, 203500},
            {34, 217900},
            {35, 232320},
            {36, 249900},
            {37, 267800},
            {38, 286200},
            {39, 304900},
            {40, 324000},
            {41, 340200},
            {42, 356800},
            {43, 373700},
            {44, 390800},
            {45, 408200},
            {46, 437600},
            {47, 467500},
            {48, 498000},
            {49, 529000},
            {50, 864000},
            {51, 1058400},
            {52, 1267200},
            {53, 1555200},
            {54, 1872000},
            {55, 2217600},
            {56, 2592000},
            {57, 2995200},
            {58, 3427200},
            {59, 3888000},
            {60, 4470000},
            {61, 4873000},
            {62, 5316000},
            {63, 5809000},
            {64, 6364000},
            {65, 6995000},
            {66, 7722000},
            {67, 8575000},
            {68, 9593000},
            {69, 10826000},
            {70, 0}
        };

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

    public class ColorInterpolator
    {
        delegate byte ComponentSelector(System.Drawing.Color color);
        static ComponentSelector _redSelector = color => color.R;
        static ComponentSelector _greenSelector = color => color.G;
        static ComponentSelector _blueSelector = color => color.B;

        public static System.Drawing.Color InterpolateBetween(
            System.Drawing.Color endPoint1,
            System.Drawing.Color endPoint2,
            double lambda)
        {
            if (lambda < 0 || lambda > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lambda));
            }
            System.Drawing.Color color = System.Drawing.Color.FromArgb(
                InterpolateComponent(endPoint1, endPoint2, lambda, _redSelector),
                InterpolateComponent(endPoint1, endPoint2, lambda, _greenSelector),
                InterpolateComponent(endPoint1, endPoint2, lambda, _blueSelector)
            );

            return color;
        }

        static byte InterpolateComponent(
            System.Drawing.Color endPoint1,
            System.Drawing.Color endPoint2,
            double lambda,
            ComponentSelector selector)
        {
            return (byte)(selector(endPoint1)
                          + (selector(endPoint2) - selector(endPoint1)) * lambda);
        }
    }
}
