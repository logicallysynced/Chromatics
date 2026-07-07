using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{

    public static class SystemMonitorHelper
    {
        private static readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
        private static int _maxCpuUsage;

        public static float GetCurrentCpuUsage()
        {
            return _cpuCounter.NextValue();
        }

        public static int GetMaxCpuUsage()
        {
            _maxCpuUsage = (int)_cpuCounter.NextValue();
            return _maxCpuUsage;
        }
    }


}
