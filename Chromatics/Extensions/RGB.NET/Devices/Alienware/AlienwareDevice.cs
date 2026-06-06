using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware
{
    public class AlienwareDevice : AbstractRGBDevice<AlienwareDeviceInfo>
    {
        private readonly AlienwareUpdateQueue _updateQueue;
        private readonly AlienwareClientDefinition _def;

        public AlienwareDevice(AlienwareDeviceInfo info, AlienwareUpdateQueue updateQueue, AlienwareClientDefinition def)
            : base(info, updateQueue)
        {
            _updateQueue = updateQueue;
            _def = def;
            InitializeLayout();
        }

        public AlienwareClientDefinition Definition => _def;

        public void BeginShutdown() => _updateQueue.BeginShutdown();
        public void SetPerDeviceDisabled(bool disabled) => _updateQueue.SetPerDeviceDisabled(disabled);
        public void ResetCache() => _updateQueue.ResetCache();

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _updateQueue.SetPerDeviceBrightness(correction);

        // Layout decision is per dialect:
        //
        //   Per-key keyboards (V5 notebook, V8 external) — apply the
        //   default ANSI 104 QWERTY keymap so Highlight / Keybind layers
        //   light approximately the right keys out of the box. Lights
        //   past the first 104 surface as Custom* in a tail row below
        //   the keyboard. Where the firmware's enumeration order doesn't
        //   match standard matrix-scan order the user can remap via
        //   the Mapping tab. See AlienwareDefaultKeymap for the
        //   reasoning + tradeoff.
        //
        //   Zone-based chassis (V4) — each light is an opaque per-board
        //   zone (Front, Rear, Left, Right, Logo, etc.) with no
        //   universal layout. Surface as Custom1..N in a synthetic
        //   grid; user positions them in the Mapping tab.
        private void InitializeLayout()
        {
            int count = System.Math.Max(1, _def.LightCount);

            if (_def.ApiVersion == AlienwareApiVersion.PerKeyV5
                || _def.ApiVersion == AlienwareApiVersion.PerKeyV8)
            {
                var keymap = AlienwareDefaultKeymap.BuildAnsi104(count);
                foreach (var entry in keymap)
                {
                    var led = AddLed(entry.LedId, entry.Location, entry.Size);
                    if (led != null) led.Shape = Shape.Rectangle;
                }
                return;
            }

            // Zone-based fallback: synthetic wider-than-tall grid.
            const float cell = 30f;
            int cols = System.Math.Max(1, (int)System.Math.Ceiling(System.Math.Sqrt(count * 4.0 / 3.0)));
            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                var led = AddLed(
                    (LedId)((int)LedId.Custom1 + i),
                    new Point(col * cell, row * cell),
                    new Size(cell, cell));
                if (led != null) led.Shape = Shape.Rectangle;
            }
        }
    }
}
