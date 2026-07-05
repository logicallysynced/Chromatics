using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol;
using RGB.NET.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    public class NanoleafDevice : AbstractRGBDevice<NanoleafDeviceInfo>
    {
        private readonly NanoleafUpdateQueue _updateQueue;
        private readonly NanoleafClientDefinition _def;
        private readonly IReadOnlyList<NanoleafPanelPosition> _panels;
        private readonly IReadOnlyList<int> _slotTable;

        public NanoleafDevice(NanoleafDeviceInfo info, NanoleafUpdateQueue updateQueue, NanoleafClientDefinition def, IReadOnlyList<NanoleafPanelPosition> panels, IReadOnlyList<int> slotTable)
            : base(info, updateQueue)
        {
            _updateQueue = updateQueue;
            _def = def;
            _panels = panels;
            _slotTable = slotTable;
            InitializeLayout();
        }

        public NanoleafClientDefinition Definition => _def;

        public void BeginShutdown() => _updateQueue.BeginShutdown();
        public Task CaptureAndStartAsync(bool turnOnIfOff = true) => _updateQueue.CaptureAndStartAsync(turnOnIfOff);
        public Task RestoreOriginalStateAsync() => _updateQueue.RestoreOriginalStateAsync();
        public Task EnsureStreamingAsync() => _updateQueue.EnsureStreamingAsync();
        public void ResetCache() => _updateQueue.ResetCache();
        public void SetPerDeviceDisabled(bool disabled) => _updateQueue.SetPerDeviceDisabled(disabled);
        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction) => _updateQueue.SetPerDeviceBrightness(correction);

        // One LED per live panel at its real layout centroid, with the LedId
        // taken from the panel's SLOT in the persisted slot table - not its
        // enumeration order - so layer assignments survive panels being
        // added or removed from the wall. Tombstoned slots (panels currently
        // absent) simply create no LED; their LedId stays reserved for a
        // return. Nanoleaf coordinates are y-up; we flip to y-down to match
        // RGB.NET / screen space, and normalise so the minimum corner sits
        // at the origin.
        private void InitializeLayout()
        {
            if (_panels == null || _panels.Count == 0)
            {
                var single = AddLed(LedId.Custom1, new Point(0, 0), new Size(60));
                if (single != null) single.Shape = Shape.Circle;
                return;
            }

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var p in _panels)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            // Nanoleaf units are relative panel-size lengths; scale so a
            // typical panel spacing reads as a sensible pixel size on the
            // Mappings canvas. Panel side lengths hover around 100 units, so
            // 0.6 keeps a big wall inside the canvas while staying legible.
            const float scale = 0.6f;
            float spanY = (maxY - minY);

            var byId = new Dictionary<int, NanoleafPanelPosition>();
            foreach (var p in _panels) byId[p.PanelId] = p;

            for (int slot = 0; slot < _slotTable.Count; slot++)
            {
                if (!byId.TryGetValue(_slotTable[slot], out var p)) continue;
                float x = (p.X - minX) * scale;
                float y = (spanY - (p.Y - minY)) * scale; // flip y-up to y-down
                var led = AddLed((LedId)((int)LedId.Custom1 + slot), new Point(x, y), new Size(60));
                if (led != null) led.Shape = Shape.Rectangle;
            }
        }
    }
}
