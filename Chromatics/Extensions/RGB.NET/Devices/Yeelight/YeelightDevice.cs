using Chromatics.Extensions.RGB.NET.ColorCorrections;
using RGB.NET.Core;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight
{
    public class YeelightDevice : AbstractRGBDevice<YeelightDeviceInfo>
    {
        // Logical LED roles for a dual-light bulb. Single-light bulbs only
        // use MainLight. Stored as the LED's CustomData so the UpdateQueue
        // can dispatch a paint frame to set_rgb (main) vs bg_set_rgb (bg)
        // without rebuilding a per-LedId mapping table.
        public enum LightChannel { MainLight, BackgroundLight }

        private readonly YeelightUpdateQueue _updateQueue;
        private readonly YeelightClientDefinition _def;

        public YeelightDevice(YeelightDeviceInfo info, YeelightUpdateQueue updateQueue, YeelightClientDefinition def)
            : base(info, updateQueue)
        {
            _updateQueue = updateQueue;
            _def = def;
            InitializeLayout();
        }

        public YeelightClientDefinition Definition => _def;

        public void BeginShutdown() => _updateQueue.BeginShutdown();
        public Task RestoreOriginalStateAsync() => _updateQueue.RestoreOriginalStateAsync();

        public void SetPerDeviceDisabled(bool disabled) => _updateQueue.SetPerDeviceDisabled(disabled);
        public void ResetCache() => _updateQueue.ResetCache();

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _updateQueue.SetPerDeviceBrightness(correction);

        // Layout decisions:
        //   - Single-light bulbs / strips / ceiling lights → 1 LED at Custom1
        //     driving the main light element via set_rgb.
        //   - Dual-light bulbs (Bedside Lamp 2 and any other with bg_set_rgb
        //     in their SSDP support list) → 2 LEDs: Custom1 = main, Custom2
        //     = background. The UpdateQueue reads each LED's CustomData to
        //     pick which set_rgb variant to send.
        //
        // Strips / matrix devices intentionally still get 1 LED — Yeelight's
        // LAN protocol can't address per-zone on those, and exposing N
        // synthetic LEDs that all paint to the same set_rgb would just
        // surprise users into thinking they had per-LED control they
        // don't have. The Mapping tab still lets them position the single
        // LED however they want.
        private void InitializeLayout()
        {
            var main = AddLed(LedId.Custom1, new Point(0, 0), new Size(80));
            if (main != null)
            {
                main.Shape = Shape.Circle;
                main.LayoutMetadata = LightChannel.MainLight;
            }

            if (_def.HasBackgroundLight)
            {
                var bg = AddLed(LedId.Custom2, new Point(100, 0), new Size(80));
                if (bg != null)
                {
                    bg.Shape = Shape.Circle;
                    bg.LayoutMetadata = LightChannel.BackgroundLight;
                }
            }
        }
    }
}
