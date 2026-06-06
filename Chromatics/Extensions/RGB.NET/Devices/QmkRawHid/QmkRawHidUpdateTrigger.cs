using RGB.NET.Core;
using System;
using System.Diagnostics;
using System.Threading;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // 30Hz trigger with idle wake every 50ms so per-device brightness
    // slider changes propagate to a board sitting on a static layer.
    // Matches the LifxDeviceUpdateTrigger shape — refresh tick fires
    // CustomUpdateData("refresh") so the queue's OnUpdate can no-op
    // gracefully on empty datasets without missing slider-driven
    // re-sends if we later add brightness/effect mode change support.
    public class QmkRawHidUpdateTrigger : DeviceUpdateTrigger
    {
        private const int WaitOneTimeoutMs = 50;

        public QmkRawHidUpdateTrigger() { }

        public QmkRawHidUpdateTrigger(double updateRateHardLimit)
            : base(updateRateHardLimit) { }

        protected override void UpdateLoop()
        {
            OnStartup();

            while (!UpdateToken.IsCancellationRequested)
            {
                if (HasDataEvent.WaitOne(WaitOneTimeoutMs))
                {
                    long preUpdateTicks = Stopwatch.GetTimestamp();
                    OnUpdate();

                    if (UpdateFrequency > 0)
                    {
                        double elapsedMs = (Stopwatch.GetTimestamp() - preUpdateTicks) / (double)TimeSpan.TicksPerMillisecond;
                        int sleep = (int)(UpdateFrequency * 1000.0 - elapsedMs);
                        if (sleep > 0) Thread.Sleep(sleep);
                    }
                }
                else
                {
                    OnUpdate(new CustomUpdateData(("refresh", true)));
                }
            }
        }
    }
}
