using System;
using System.Diagnostics;
using System.Threading;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    // Idle-refresh trigger, same shape as LifxDeviceUpdateTrigger: a periodic
    // wake so the queue keeps receiving Update() calls when the LED colour
    // stops changing. This is what keeps the per-device brightness slider
    // responsive on a static mapping, and it's what drives the once-per-
    // second keep-alive that holds the controller in streaming mode.
    public class NanoleafDeviceUpdateTrigger : DeviceUpdateTrigger
    {
        private const int WaitOneTimeoutMs = 50;

        public NanoleafDeviceUpdateTrigger() { }

        public NanoleafDeviceUpdateTrigger(double updateRateHardLimit)
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
