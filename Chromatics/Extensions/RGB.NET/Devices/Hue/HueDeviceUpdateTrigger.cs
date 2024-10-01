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

        private const long FLUSH_TIMER = 5 * 1000 * TimeSpan.TicksPerMillisecond; // flush the device every 5 seconds to prevent timeouts

        #endregion

        #region Properties & Fields

        private long _lastUpdateTimestamp;
        public Dictionary<LocalHueApi, List<string>> ClientLights { get; }

        #endregion

        #region IDisposable

        public override void Dispose()
        {
            base.Dispose();
            ClientLights.Clear();
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="HueDeviceUpdateTrigger" /> class.
        /// </summary>
        public HueDeviceUpdateTrigger()
        {
            ClientLights = new Dictionary<LocalHueApi, List<string>>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HueDeviceUpdateTrigger" /> class.
        /// </summary>
        /// <param name="updateRateHardLimit">The hard limit of the update rate of this trigger.</param>
        public HueDeviceUpdateTrigger(double updateRateHardLimit)
            : base(updateRateHardLimit)
        {
            ClientLights = new Dictionary<LocalHueApi, List<string>>();
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override void UpdateLoop()
        {
            OnStartup();

            while (!UpdateToken.IsCancellationRequested)
            {
                if (HasDataEvent.WaitOne(Timeout))
                {
                    long preUpdateTicks = Stopwatch.GetTimestamp();

                    OnUpdate();

                    if (UpdateFrequency > 0)
                    {
                        double lastUpdateTime = (_lastUpdateTimestamp - preUpdateTicks) / (double)TimeSpan.TicksPerMillisecond;
                        int sleep = (int)(UpdateFrequency * 1000.0 - lastUpdateTime);
                        if (sleep > 0)
                            Thread.Sleep(sleep);
                    }
                }
                else if (_lastUpdateTimestamp > 0 && Stopwatch.GetTimestamp() - _lastUpdateTimestamp > FLUSH_TIMER)
                {
                    OnUpdate(new CustomUpdateData(("refresh", true)));
                }
            }
        }

        /// <inheritdoc />
        protected override void OnUpdate(CustomUpdateData updateData = null)
        {
            base.OnUpdate(updateData);
            _lastUpdateTimestamp = Stopwatch.GetTimestamp();

            if (ClientLights == null)
                return;

            foreach (var clientLights in ClientLights)
            {
                var client = clientLights.Key;
                var lightIds = clientLights.Value;
            }
        }

        #endregion
    }
}
