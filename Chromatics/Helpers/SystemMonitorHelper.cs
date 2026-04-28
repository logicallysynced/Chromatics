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
            var counter = new PerformanceCounter("Memory", "Available Mbytes");
            var memUsage = counter.NextValue();
            var maxCpuUsage = (int)(100 - memUsage);
            _maxCpuUsage = maxCpuUsage;

            return _maxCpuUsage;
        }
    }


}
