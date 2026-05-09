using System;
using System.Diagnostics;
using System.Threading;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX
{
    // 50ms wake-up so the queue still receives idle refreshes when the LED
    // colour stops changing — same trick HueDeviceUpdateTrigger uses, just at
    // 20Hz instead of Hue's 1Hz because LIFX devices accept sustained 20Hz
    // without the rate-limiter throttling we have to dance around for the
    // bridge.
    //
    // Idle refreshes are how the per-device brightness slider stays
    // responsive on a static mapping: HasDataEvent is only signalled when
    // RGB.NET commits a colour that DIFFERS from the previous frame, so a
    // bulb sitting on a static base layer would stop receiving Update()
    // calls without a periodic re-fire.
    public class LifxDeviceUpdateTrigger : DeviceUpdateTrigger
    {
        private const int WaitOneTimeoutMs = 50;

        public LifxDeviceUpdateTrigger() { }

        public LifxDeviceUpdateTrigger(double updateRateHardLimit)
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
