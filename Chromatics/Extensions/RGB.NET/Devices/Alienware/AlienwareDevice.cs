using Chromatics.Extensions.RGB.NET.ColorCorrections;
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

        // Layout: one LED per addressable light, mapped to LedId.Custom1..N.
        // Per-key Alienware boards (V5/V8) don't ship with a (row, col) key
        // map in T-Troll's open source — the GUI builds those per-board
        // from user-provided registry data. Without that table we can't
        // emit semantic LedId.Keyboard_* values, so users position keys
        // via the Mapping tab drag UX. Same model as QMK boards that
        // come up without a VIA keymap.
        //
        // Zone-based devices (V4) have a fixed small set of named zones;
        // each gets a Custom* LED at a position that hints at its
        // physical location (left-zone, right-zone, etc.). Synthetic
        // grid only — DeviceGridHelper falls back to its own grid when
        // decorators want 2D coordinates.
        private void InitializeLayout()
        {
            int count = System.Math.Max(1, _def.LightCount);
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
