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

        public void SuspendWrites() => _updateQueue.SuspendWrites();
        public void Shutdown(bool sendOffFrame = true) => _updateQueue.Shutdown(sendOffFrame);

        // DualSense LED layout (left→right when looking at the controller):
        //   - The lightbar runs along the bottom edge of the touchpad in two
        //     mirrored strips. Modelled as one wide rectangle (Custom1).
        //   - The 5 player indicator LEDs sit in a row directly below the
        //     touchpad. Bit 0 = leftmost, bit 4 = rightmost from the player's
        //     POV (matches Linux's player_leds bit ordering).
        //
        // Note: the mic-mute LED is intentionally NOT exposed. The controller
        // firmware drives that LED to track mic-mute toggle state — pressing
        // the mute button mutes the microphone AND lights the LED, regardless
        // of any host involvement. Taking host control of the LED would only
        // suppress that visual feedback for an action that still happens, so
        // we leave the firmware default in place. See DualSenseUpdateQueue
        // header for the protocol detail (we deliberately don't set the
        // MIC_MUTE_LED_CONTROL_ENABLE bit in valid_flag1).
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
        }
    }
}
