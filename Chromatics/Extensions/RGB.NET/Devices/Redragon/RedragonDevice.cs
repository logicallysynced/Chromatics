using Chromatics.Extensions.RGB.NET.ColorCorrections;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon
{
    // Single-LED mouse device. The firmware exposes one addressable colour
    // zone (the logo + DPI underglow share the same register) so we model
    // the device with a single LedId.Mouse1 LED placed at the origin of a
    // tiny synthetic grid. The Mapping tab places it visually wherever the
    // user wants — Chromatics doesn't infer a physical mouse layout.
    public class RedragonDevice : AbstractRGBDevice<RedragonDeviceInfo>
    {
        private readonly RedragonUpdateQueue _updateQueue;
        private readonly RedragonClientDefinition _def;

        public RedragonDevice(RedragonDeviceInfo info, RedragonUpdateQueue updateQueue, RedragonClientDefinition def)
            : base(info, updateQueue)
        {
            _updateQueue = updateQueue;
            _def = def;
            InitializeLayout();
        }

        public RedragonClientDefinition Definition => _def;

        public void BeginShutdown() => _updateQueue.BeginShutdown();
        public void SetPerDeviceDisabled(bool disabled) => _updateQueue.SetPerDeviceDisabled(disabled);
        public void ResetCache() => _updateQueue.ResetCache();

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _updateQueue.SetPerDeviceBrightness(correction);

        private void InitializeLayout()
        {
            const float cell = 30f;
            var led = AddLed(LedId.Mouse1, new Point(0, 0), new Size(cell, cell));
            if (led != null) led.Shape = Shape.Rectangle;
        }
    }
}
