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
    }
}
