using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

        public static class DeviceHelpers
        {
            public static int[] RazerKeypadCoordinates(int key)
            {
                switch (key)
                {
                    case 1:
                        return new int[] {0, 0};
                    case 2:
                        return new int[] {0, 1};
                    case 3:
                        return new int[] {0, 2};
                    case 4:
                        return new int[] {0, 3};
                    case 5:
                        return new int[] {0, 4};
                    case 6:
                        return new int[] {1, 0};
                    case 7:
                        return new int[] {1, 1};
                    case 8:
                        return new int[] {1, 2};
                    case 9:
                        return new int[] {1, 3};
                    case 10:
                        return new int[] {1, 4};
                    case 11:
                        return new int[] {2, 0};
                    case 12:
                        return new int[] {2, 1};
                    case 13:
                        return new int[] {2, 2};
                    case 14:
                        return new int[] {2, 3};
                    case 15:
                        return new int[] {2, 4};
                    case 16:
                        return new int[] {3, 0};
                    case 17:
                        return new int[] {3, 1};
                    case 18:
                        return new int[] {3, 2};
                    case 19:
                        return new int[] {3, 3};
                    case 20:
                        return new int[] {3, 4};
                    default:
                        return new int[] {0, 0};
                }
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
                case DevModeTypes.BaseMode:
                    return "BaseMode";
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
                case DevModeTypes.Castbar:
                    return "Castbar";
                case DevModeTypes.DutyFinder:
                    return "DutyFinder";
                case DevModeTypes.ACTTracker:
                    return "ACTTracker";
                case DevModeTypes.ReactiveWeather:
                    return "ReactiveWeather";
                case DevModeTypes.BattleStance:
                    return "BattleStance";
                case DevModeTypes.JobClass:
                    return "JobClass";
            }
            
            return "Disabled";
        }

        public static string ConvertDevMultiModeToString(DevMultiModeTypes mode)
        {
            switch (mode)
            {
                case DevMultiModeTypes.Disabled:
                    return "Disabled";
                case DevMultiModeTypes.BaseMode:
                    return "BaseMode";
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
                case "BaseMode":
                    return DevModeTypes.BaseMode;
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
                case "Castbar":
                    return DevModeTypes.Castbar;
                case "DutyFinder":
                    return DevModeTypes.DutyFinder;
                case "ACTTracker":
                    return DevModeTypes.ACTTracker;
                case "ReactiveWeather":
                    return DevModeTypes.ReactiveWeather;
                case "BattleStance":
                    return DevModeTypes.BattleStance;
                case "JobClass":
                    return DevModeTypes.JobClass;
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
                case "BaseMode":
                    return DevMultiModeTypes.BaseMode;
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
                case "Base Mode":
                    return DevModeTypes.BaseMode;
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
                case "Castbar":
                    return DevModeTypes.Castbar;
                case "Duty Finder Bell":
                    return DevModeTypes.DutyFinder;
                case "ACT Tracker":
                    return DevModeTypes.ACTTracker;
                case "Reactive Weather":
                    return DevModeTypes.ReactiveWeather;
                case "Battle Stance":
                    return DevModeTypes.BattleStance;
                case "Job Class":
                    return DevModeTypes.JobClass;
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
                case "Base Mode":
                    return DevMultiModeTypes.BaseMode;
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
                case DevModeTypes.BaseMode:
                    return "Base Mode";
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
                case DevModeTypes.Castbar:
                    return "Castbar";
                case DevModeTypes.DutyFinder:
                    return "Duty Finder Bell";
                case DevModeTypes.ACTTracker:
                    return "ACT Tracker";
                case DevModeTypes.ReactiveWeather:
                    return "Reactive Weather";
                case DevModeTypes.BattleStance:
                    return "Battle Stance";
                case DevModeTypes.JobClass:
                    return "Job Class";
            }

            return "Disabled";
        }

        public static string ConvertDevMultiModeToCB(DevMultiModeTypes mode)
        {
            switch (mode)
            {
                case DevMultiModeTypes.Disabled:
                    return "Disabled";
                case DevMultiModeTypes.BaseMode:
                    return "Base Mode";
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
                case LightbarMode.BaseMode:
                    return "Base Mode";
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
                case LightbarMode.ReactiveWeather:
                    return "ReactiveWeather";
                case LightbarMode.BattleStance:
                    return "BattleStance";
                case LightbarMode.JobClass:
                    return "JobClass";
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
                case "BaseMode":
                    return LightbarMode.BaseMode;
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
                case "ReactiveWeather":
                    return LightbarMode.ReactiveWeather;
                case "BattleStance":
                    return LightbarMode.BattleStance;
                case "JobClass":
                    return LightbarMode.JobClass;
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
                case "Base Mode":
                    return LightbarMode.BaseMode;
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
                case "Reactive Weather":
                    return LightbarMode.ReactiveWeather;
                case "Battle Stance":
                    return LightbarMode.BattleStance;
                case "Job Class":
                    return LightbarMode.JobClass;

            }

            return LightbarMode.Disabled;
        }

        public static string ConvertLightbarModeToCB(LightbarMode mode)
        {
            switch (mode)
            {
                case LightbarMode.Disabled:
                    return "Disabled";
                case LightbarMode.BaseMode:
                    return "Base Mode";
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
                case LightbarMode.ReactiveWeather:
                    return "Reactive Weather";
                case LightbarMode.BattleStance:
                    return "Battle Stance";
                case LightbarMode.JobClass:
                    return "Job Class";
            }

            return "Disabled";
        }

        public static string ConvertFKeyModeToString(FKeyMode mode)
        {
            switch (mode)
            {
                case FKeyMode.Disabled:
                    return "Disabled";
                case FKeyMode.BaseMode:
                    return "BaseMode";
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
                case FKeyMode.HpMp:
                    return "HpMp";
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
                case FKeyMode.ReactiveWeather:
                    return "ReactiveWeather";
                case FKeyMode.BattleStance:
                    return "BattleStance";
                case FKeyMode.JobClass:
                    return "JobClass";
            }

            return "HpMp";
        }

        public static FKeyMode ConvertStringToFKeyMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return FKeyMode.Disabled;
                case "BaseMode":
                    return FKeyMode.BaseMode;
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
                case "HpMpTp":
                case "HpMp":
                    return FKeyMode.HpMp;
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
                case "ReactiveWeather":
                    return FKeyMode.ReactiveWeather;
                case "BattleStance":
                    return FKeyMode.BattleStance;
                case "JobClass":
                    return FKeyMode.JobClass;
            }

            return FKeyMode.HpMp;
        }

        public static FKeyMode ConvertCBToFKeyMode(string mode)
        {
            switch (mode)
            {
                case "Disabled":
                    return FKeyMode.Disabled;
                case "Default Color":
                case "Base Mode":
                    return FKeyMode.BaseMode;
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
                case "HP/MP/TP":
                case "HP/MP":
                    return FKeyMode.HpMp;
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
                case "Reactive Weather":
                    return FKeyMode.ReactiveWeather;
                case "Battle Stance":
                    return FKeyMode.BattleStance;
                case "Job Class":
                    return FKeyMode.JobClass;

            }

            return FKeyMode.HpMp;
        }

        public static string ConvertFKeyModeToCB(FKeyMode mode)
        {
            switch (mode)
            {
                case FKeyMode.Disabled:
                    return "Disabled";
                case FKeyMode.BaseMode:
                    return "Base Mode";
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
                case FKeyMode.HpMp:
                    return "HP/MP";
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
                case FKeyMode.ReactiveWeather:
                    return "Reactive Weather";
                case FKeyMode.BattleStance:
                    return "Battle Stance";
                case FKeyMode.JobClass:
                    return "Job Class";
            }

            return "HpMp";
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
                return Color.Black;
                //throw new ArgumentOutOfRangeException(nameof(lambda));
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
