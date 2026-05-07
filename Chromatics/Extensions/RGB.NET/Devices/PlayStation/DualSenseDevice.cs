using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.PlayStation
{
    public class DualSenseDevice : AbstractRGBDevice<PlayStationDeviceInfo>
    {
        private readonly DualSenseUpdateQueue _updateQueue;

        public DualSenseDevice(PlayStationDeviceInfo deviceInfo, DualSenseUpdateQueue updateQueue)
            : base(deviceInfo, updateQueue)
        {
            _updateQueue = updateQueue;
            InitializeLayout();
        }

        public void Shutdown() => _updateQueue.Shutdown();

        // DualSense LED layout (left→right when looking at the controller):
        //   - The lightbar runs along the bottom edge of the touchpad in two
        //     mirrored strips. Modelled as one wide rectangle (Custom1).
        //   - The 5 player indicator LEDs sit in a row directly below the
        //     touchpad. Bit 0 = leftmost, bit 4 = rightmost from the player's
        //     POV (matches Linux's player_leds bit ordering).
        //   - The mic-mute LED is a small orange dot in the mic-mute button,
        //     centred between the analogue sticks below the touchpad.
        //
        // Coordinates are arbitrary visual approximations for the Mappings tab —
        // they don't drive any hardware addressing.
        private void InitializeLayout()
        {
            var lightbar = AddLed(LedId.Custom1, new Point(0, 0), new Size(80, 8));
            if (lightbar != null) lightbar.Shape = Shape.Rectangle;

            // Five player indicator dots, evenly spaced beneath the lightbar.
            for (int i = 0; i < 5; i++)
            {
                var led = AddLed((LedId)(LedId.Custom2 + i), new Point(20 + i * 12, 16), new Size(6));
                if (led != null) led.Shape = Shape.Circle;
            }

            // Mic-mute LED — slightly below and centred.
            var mic = AddLed(LedId.Custom7, new Point(40, 28), new Size(6));
            if (mic != null) mic.Shape = Shape.Circle;
        }
    }
}
