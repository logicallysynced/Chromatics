using RGB.NET.Core;
using System;
using System.Diagnostics;
using System.Threading;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    // 30Hz trigger with 50ms idle wake. Matches the QMK / Yeelight /
    // Alienware triggers — the idle refresh keeps per-device brightness
    // slider changes propagating when devices are sitting on a static
    // base layer.
    //
    // Dynamic Lighting devices report their min update interval via
    // LampArray.MinUpdateInterval. Most current devices report somewhere
    // between 4ms and 33ms; we pick 33ms (30Hz) as a conservative cap
    // that comfortably covers the documented range.
    public class DynamicLightingUpdateTrigger : DeviceUpdateTrigger
    {
        private const int WaitOneTimeoutMs = 50;

        public DynamicLightingUpdateTrigger() { }

        public DynamicLightingUpdateTrigger(double updateRateHardLimit)
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
