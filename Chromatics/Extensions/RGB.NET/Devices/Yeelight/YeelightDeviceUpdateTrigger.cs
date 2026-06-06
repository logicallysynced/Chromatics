using RGB.NET.Core;
using System;
using System.Diagnostics;
using System.Threading;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight
{
    // 30Hz trigger with a 50ms idle wake so per-device brightness changes
    // still propagate when a bulb is sitting on a static base layer
    // (HasDataEvent is only signalled when RGB.NET commits a colour that
    // differs from the previous frame — without the idle refresh, slider
    // changes wouldn't reach the bulb until something else dirtied an LED).
    //
    // Same shape as LifxDeviceUpdateTrigger / QmkRawHidUpdateTrigger.
    public class YeelightDeviceUpdateTrigger : DeviceUpdateTrigger
    {
        private const int WaitOneTimeoutMs = 50;

        public YeelightDeviceUpdateTrigger() { }

        public YeelightDeviceUpdateTrigger(double updateRateHardLimit)
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
