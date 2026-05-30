using RGB.NET.Core;
using System;
using System.Diagnostics;
using System.Threading;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon
{
    // 30Hz trigger with 50ms idle wake — identical cadence + shape to the
    // QMK / Alienware / Yeelight triggers. The idle refresh keeps per-device
    // brightness slider changes propagating when the mouse is sitting on a
    // static base layer (HasDataEvent only signals when a colour changed).
    public class RedragonUpdateTrigger : DeviceUpdateTrigger
    {
        private const int WaitOneTimeoutMs = 50;

        public RedragonUpdateTrigger() { }

        public RedragonUpdateTrigger(double updateRateHardLimit)
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
