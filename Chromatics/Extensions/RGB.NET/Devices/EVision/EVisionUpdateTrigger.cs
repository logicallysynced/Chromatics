using RGB.NET.Core;
using System;
using System.Diagnostics;
using System.Threading;

namespace Chromatics.Extensions.RGB.NET.Devices.EVision
{
    // 10Hz trigger with 50ms idle wake. Slower than the standard 30Hz
    // cadence used by Alienware/QMK/Redragon because EVision V1's
    // protocol writes to the keyboard's firmware flash on every
    // accepted frame (no Direct/streaming mode like the V2 chip
    // family). 10Hz gives smooth-enough motion for the cycling and
    // breathing effects that dominate real-world use while cutting
    // flash wear two-thirds against the 30Hz baseline.
    //
    // The idle 50ms wake keeps per-device brightness slider changes
    // propagating when the keyboard is sitting on a static base layer
    // (HasDataEvent only signals when a colour changed).
    public class EVisionUpdateTrigger : DeviceUpdateTrigger
    {
        private const int WaitOneTimeoutMs = 50;

        public EVisionUpdateTrigger() { }

        public EVisionUpdateTrigger(double updateRateHardLimit)
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
