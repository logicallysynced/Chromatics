using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    public static class MathHelper
    {
        public static class LinearInterpolation
        {
            public static T Interpolate<T>(T current, T min, T max, T targetLow, T targetHigh)
            {
                dynamic c = current;
                dynamic mn = min;
                dynamic mx = max;
                dynamic th = targetHigh;
                dynamic tl = targetLow;

                return (T)((c - mn) * (th - tl) / (mx - mn) + tl);
            }
        }

        public static double CalculatePercentage<T>(T current, T max) where T : IComparable
        {
            double c = Convert.ToDouble(current);
            double mx = Convert.ToDouble(max);

            return (c / mx) * 100;
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
            {
                return min;
            }
            else if (value.CompareTo(max) > 0)
            {
                return max;
            }
            else
            {
                return value;
            }
        }
    }
}
