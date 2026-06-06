using Chromatics.Extensions.RGB.NET.ColorCorrections;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    public class QmkRawHidDevice : AbstractRGBDevice<QmkRawHidDeviceInfo>
    {
        private readonly QmkRawHidUpdateQueue _updateQueue;
        private readonly QmkRawHidClientDefinition _def;

        public QmkRawHidDevice(QmkRawHidDeviceInfo info, QmkRawHidUpdateQueue updateQueue, QmkRawHidClientDefinition def)
            : base(info, updateQueue)
        {
            _updateQueue = updateQueue;
            _def = def;
            InitializeLayout();
        }

        public QmkRawHidClientDefinition Definition => _def;

        // Per-device lifecycle parity with LifxDevice / HueDevice — the
        // queue gate flag is the primary mechanism for stopping paint
        // frames on per-device disable in the Mapping tab.
        public void SetPerDeviceDisabled(bool disabled) => _updateQueue.SetPerDeviceDisabled(disabled);
        public void ResetCache() => _updateQueue.ResetCache();
        public void BeginShutdown() => _updateQueue.BeginShutdown();

        // Per-device brightness — Hue uses xy chromaticity so it needs a
        // separate channel; QMK's RGB matrix is plain RGB so the global
        // brightness correction is sufficient. Method kept for parity in
        // case future firmware exposes a bridge-style brightness query.
        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _updateQueue.SetPerDeviceBrightness(correction);

        // Layout: VIA-only devices show up as a single representative LED
        // (LedId.Custom1) since VIA can't drive per-key from the host.
        // OpenRGB-QMK devices iterate the Layout entries the provider built
        // — these carry either LedId.Keyboard_* semantic IDs (when a VIA
        // keymap was fetchable from the via-keyboards repo) or LedId.Custom1+i
        // synthetic ids as a fallback. RGB.NET treats both identically; the
        // semantic IDs are just what lets Chromatics' Highlight / Keybinds
        // layers light the right physical keys out of the box.
        private void InitializeLayout()
        {
            if (_def.Protocol == QmkRawHidProtocolMode.ViaOnly || _def.Layout.Count == 0)
            {
                var single = AddLed(LedId.Custom1, new Point(0, 0), new Size(60, 60));
                if (single != null) single.Shape = Shape.Rectangle;
                return;
            }

            // OpenRGB-QMK: one LED per firmware index, ordered by FirmwareIndex
            // so the UpdateQueue's bulk-set path can pack rgbBytes in the
            // canonical strip order without a re-sort per frame.
            for (int i = 0; i < _def.Layout.Count; i++)
            {
                var entry = _def.Layout[i];
                var led = AddLed(entry.PreferredLedId, entry.Location, entry.Size);
                if (led != null) led.Shape = Shape.Rectangle;
            }
        }
    }
}
