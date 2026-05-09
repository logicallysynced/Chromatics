using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.PlayStation
{
    public class DualShock4Device : AbstractRGBDevice<PlayStationDeviceInfo>
    {
        private readonly DualShock4UpdateQueue _updateQueue;

        public DualShock4Device(PlayStationDeviceInfo deviceInfo, DualShock4UpdateQueue updateQueue)
            : base(deviceInfo, updateQueue)
        {
            _updateQueue = updateQueue;
            InitializeLayout();
        }

        public void SuspendWrites() => _updateQueue.SuspendWrites();
        public void Shutdown(bool sendOffFrame = true) => _updateQueue.Shutdown(sendOffFrame);

        // DS4 has a single RGB lightbar above the touchpad.
        // Custom1 keeps the LED enum stable across DS4 / DS5 — DualSenseDevice's
        // Custom1 is also the lightbar so a user mapping for "Custom 1" carries
        // sensible meaning across both controller types.
        private void InitializeLayout()
        {
            var lightbar = AddLed(LedId.Custom1, new Point(0, 0), new Size(60, 14));
            if (lightbar != null)
                lightbar.Shape = Shape.Rectangle;
        }
    }
}
