using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.EVision.Protocol;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.EVision
{
    // Per-keyboard device. Layout is built from the 6x23 firmware matrix
    // OpenRGB's RGBController_EVisionKeyboard publishes. The wire still
    // expects every one of the 126 firmware LED slots in sequence even
    // on physically-smaller keyboards (TKL, 80%, 60%) - the firmware
    // silently ignores the slots that aren't connected to a physical
    // LED on the lower-key-count variants.
    //
    // LedId mapping is purely positional: each populated matrix cell
    // becomes a Custom_N LedId where N is the firmware slot. We can't
    // map to semantic ids like LedId.Keyboard_A because OpenRGB's
    // driver doesn't carry per-model key-name tables - the same
    // firmware powers everything from 60% Mars Gaming MKMini to a
    // full-size Womier K87, and the slot-to-physical-key mapping
    // differs per board. Users assign semantic mappings on the
    // Mappings tab.
    public class EVisionDevice : AbstractRGBDevice<EVisionDeviceInfo>
    {
        private readonly EVisionUpdateQueue _updateQueue;
        private readonly EVisionClientDefinition _def;

        public EVisionDevice(EVisionDeviceInfo info, EVisionUpdateQueue updateQueue, EVisionClientDefinition def)
            : base(info, updateQueue)
        {
            _updateQueue = updateQueue;
            _def = def;
            InitializeLayout();
        }

        public EVisionClientDefinition Definition => _def;

        public void BeginShutdown() => _updateQueue.BeginShutdown();
        public void SetPerDeviceDisabled(bool disabled) => _updateQueue.SetPerDeviceDisabled(disabled);
        public void ResetCache() => _updateQueue.ResetCache();

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _updateQueue.SetPerDeviceBrightness(correction);

        // Visual key sizing for the Mappings tab. Wider than tall to
        // match a standard keyboard footprint when laid out across
        // 23 columns / 6 rows.
        private const float Cell = 30f;

        private void InitializeLayout()
        {
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 23; col++)
                {
                    byte slot = EVisionKeyboardProtocol.Matrix[row, col];
                    if (slot == EVisionKeyboardProtocol.NA) continue;

                    var ledId = (LedId)((int)LedId.Custom1 + slot);
                    var led = AddLed(ledId, new Point(col * Cell, row * Cell), new Size(Cell, Cell));
                    if (led != null) led.Shape = Shape.Rectangle;
                }
            }
        }
    }
}
