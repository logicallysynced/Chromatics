using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using HueApi.Models.Requests;
using HueApi;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue
{
    public class HueDeviceUpdateTrigger : DeviceUpdateTrigger
    {
        #region Constants

        // HasDataEvent.WaitOne wake-up interval. MUST be finite — the base
        // class's inherited `Timeout` defaults to Timeout.Infinite, which
        // means WaitOne blocks forever if no new data arrives. RGB.NET only
        // signals HasDataEvent when the surface commits a colour that
        // DIFFERS from the last one pushed to the device. For a Hue bulb
        // sitting on a static mapping (e.g. a fixed base layer), colours
        // stop changing after the first frame and WaitOne never returns —
        // the idle-refresh branch was unreachable, so live brightness
        // slider moves never propagated to the bridge because our Update()
        // was never called again. A 1s wake-up interval lets the refresh
        // branch fire, which re-renders and re-reads the slider value.
        // Also respects the bridge's ~10 req/s budget even with several
        // bulbs (3 bulbs × 1 req/s idle = 3 req/s).
        private const int WaitOneTimeoutMs = 1000;

        #endregion

        #region Properties & Fields

        private long _lastUpdateTimestamp;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="HueDeviceUpdateTrigger" /> class.
        /// </summary>
        public HueDeviceUpdateTrigger()
        { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HueDeviceUpdateTrigger" /> class.
        /// </summary>
        /// <param name="updateRateHardLimit">The hard limit of the update rate of this trigger.</param>
        public HueDeviceUpdateTrigger(double updateRateHardLimit)
            : base(updateRateHardLimit)
        { }

        #endregion

        #region Methods

        /// <inheritdoc />
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
                        // Operands were swapped here previously (lastUpdateTimestamp - preUpdateTicks),
                        // which yielded a negative or zero elapsed value and caused the throttle
                        // to always sleep the full UpdateFrequency window.
                        double elapsedMs = (Stopwatch.GetTimestamp() - preUpdateTicks) / (double)TimeSpan.TicksPerMillisecond;
                        int sleep = (int)(UpdateFrequency * 1000.0 - elapsedMs);
                        if (sleep > 0)
                            Thread.Sleep(sleep);
                    }
                }
                else
                {
                    // WaitOne timed out without receiving a new HasDataEvent
                    // signal. Treat the whole timeout window as an idle
                    // period and fire a refresh. This is the path that
                    // drives slider responsiveness for static Hue
                    // mappings — without it, brightness / per-device
                    // changes would never reach the bridge after the very
                    // first frame. The WaitOneTimeoutMs (1s) gate keeps
                    // this well inside the bridge's ~10 req/s budget.
                    OnUpdate(new CustomUpdateData(("refresh", true)));
                }
            }
        }

        /// <inheritdoc />
        protected override void OnUpdate(CustomUpdateData updateData = null)
        {
            base.OnUpdate(updateData);
            _lastUpdateTimestamp = Stopwatch.GetTimestamp();
        }

        #endregion
    }
}
