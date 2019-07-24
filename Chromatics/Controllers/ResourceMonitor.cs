using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Controllers
{
    public static class ResourceMonitor
    {
        private static PerformanceCounter cpuCounter;
        private static PerformanceCounter ramCounter;
        //private static CancellationTokenSource RMcts = new CancellationTokenSource();
        private static bool _IsRunning = false;
        private static float _cpuUsage = 0;

        public static void Initialize()
        {
            if (_IsRunning) return;

            cpuCounter = new PerformanceCounter("Process", "% Processor Time",
                Process.GetCurrentProcess().ProcessName, true);
            _IsRunning = true;
            CycleCPUCounter();
        }

        public static void Stop()
        {
            if (!_IsRunning) return;

            //RMcts.Cancel();
            _IsRunning = false;
            cpuCounter = null;
            //RMcts = null;
        }

        public static float GetCPUUsage()
        {
            if (!_IsRunning) return 0;

            return _cpuUsage;
        }

        private static async void CycleCPUCounter()
        {
            if (!_IsRunning) return;

            while (_IsRunning)
            {
                _cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;
                await Task.Delay(1500);
            }

            /*
            Task.Run(() =>
            {
                while (_IsRunning)
                {
                    _cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;
                    Thread.Sleep(1500);
                }
            });
            */

            /*
            Task t = Task.Factory.StartNew(
        async () => {
            while (_IsRunning)
            {
                RMcts.Token.ThrowIfCancellationRequested();
                try
                {
                    _cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;
                    await Task.Delay(1000, RMcts.Token);
                }
                catch (TaskCanceledException ex) { }
            } }, RMcts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            */
        }
    }
}
