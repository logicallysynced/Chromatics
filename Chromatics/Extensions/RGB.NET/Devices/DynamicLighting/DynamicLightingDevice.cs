using Chromatics.Extensions.RGB.NET.ColorCorrections;
using RGB.NET.Core;
using System.Collections.Generic;
using Windows.Devices.Lights;
using WinColor = Windows.UI.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    public class DynamicLightingDevice : AbstractRGBDevice<DynamicLightingDeviceInfo>
    {
        private readonly DynamicLightingUpdateQueue _updateQueue;
        private readonly DynamicLightingClientDefinition _def;

        // LedId → LampArray lamp index. Built once at construction so the
        // per-frame Update doesn't have to rebuild the map. The UpdateQueue
        // reads this to translate Chromatics LedId paint events into
        // LampArray.SetSingleColor / SetColorsForIndices calls.
        private readonly Dictionary<LedId, int> _ledIndexByLedId = new();

        public DynamicLightingDevice(DynamicLightingDeviceInfo info, DynamicLightingUpdateQueue updateQueue, DynamicLightingClientDefinition def)
            : base(info, updateQueue)
        {
            _updateQueue = updateQueue;
            _def = def;
            InitializeLayout();
            _updateQueue.SetLedIndexMap(_ledIndexByLedId);
        }

        public DynamicLightingClientDefinition Definition => _def;

        public void BeginShutdown() => _updateQueue.BeginShutdown();
        public void SetPerDeviceDisabled(bool disabled) => _updateQueue.SetPerDeviceDisabled(disabled);
        public void ResetCache() => _updateQueue.ResetCache();

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _updateQueue.SetPerDeviceBrightness(correction);

        // Layout pulls per-lamp metadata from the WinRT LampArray:
        //
        //   - For keyboard-kind devices, LampInfo.GetPurposes() identifies
        //     control lamps (the actual keys); we map each by its
        //     VirtualKey association via LampArray.GetIndicesForKey() so
        //     the Highlight / Keybind layers light the right physical
        //     keys without manual setup.
        //
        //   - For non-keyboard devices (mice, chassis, headsets, etc.),
        //     we expose each lamp as Custom1..N and use LampInfo.Position
        //     for the RGB.NET Point so spatial decorators (radial pulses)
        //     can compute distances correctly.
        //
        // LampArray.MinUpdateInterval is the OEM's claimed cap on update
        // rate; we don't enforce it client-side because the trigger's
        // 30Hz cadence is already inside the documented envelope for
        // every shipping LampArray device family.
        private void InitializeLayout()
        {
            var lampArray = _def.LampArray;
            if (lampArray == null) return;

            int lampCount = lampArray.LampCount;
            if (lampCount <= 0) return;

            // Bounding box is in millimetres. RGB.NET uses arbitrary
            // units, but ratios are what matter for decorators — so
            // multiply by a fixed scale that keeps positions in the
            // same numerical range as our other providers (Custom1..N
            // grids use a 30-unit cell). 10 units per mm produces a
            // ~430-unit-wide keyboard, which sits comfortably alongside
            // the QMK and Alienware layouts at default cell sizes.
            const float MmToUnits = 10f;

            // Try VirtualKey-based semantic mapping first for keyboards.
            // GetIndicesForKey returns the lamp indices the OS associates
            // with that virtual key; for the standard ANSI 104 we walk
            // each LedId.Keyboard_* and look it up.
            bool isKeyboard = lampArray.LampArrayKind == LampArrayKind.Keyboard;
            var assigned = new HashSet<int>();

            if (isKeyboard)
            {
                foreach (var (ledId, vk) in DynamicLightingKeyMap.LedIdToVirtualKey)
                {
                    int[] indices;
                    try { indices = lampArray.GetIndicesForKey(vk); }
                    catch { indices = System.Array.Empty<int>(); }

                    if (indices == null || indices.Length == 0) continue;
                    int lampIndex = indices[0];
                    if (lampIndex < 0 || lampIndex >= lampCount) continue;
                    if (assigned.Contains(lampIndex)) continue;

                    AddLedFromLampInfo(lampArray, lampIndex, ledId, MmToUnits);
                    _ledIndexByLedId[ledId] = lampIndex;
                    assigned.Add(lampIndex);
                }
            }

            // Anything not claimed by the semantic mapping (non-keyboard
            // devices, plus keyboard lamps that aren't standard ANSI keys
            // — function row extras, status LEDs, logos) gets Custom1..N
            // in lamp-index order. This guarantees every lamp the OS
            // exposes is addressable from Chromatics.
            int customCounter = 0;
            for (int i = 0; i < lampCount; i++)
            {
                if (assigned.Contains(i)) continue;
                var ledId = (LedId)((int)LedId.Custom1 + customCounter);
                AddLedFromLampInfo(lampArray, i, ledId, MmToUnits);
                _ledIndexByLedId[ledId] = i;
                customCounter++;
            }
        }

        private void AddLedFromLampInfo(LampArray lampArray, int lampIndex, LedId ledId, float mmToUnits)
        {
            LampInfo info;
            try { info = lampArray.GetLampInfo(lampIndex); }
            catch { info = null; }

            // Lamp positions are Vector3 in millimetres relative to the
            // device bounding box origin. We project to 2D by dropping Z
            // (depth) since RGB.NET decorators operate in 2D space; for
            // every shipping LampArray device the keys are coplanar so
            // dropping Z loses no information.
            Point location;
            Size size = new Size(20, 20);
            if (info != null)
            {
                location = new Point(info.Position.X * mmToUnits, info.Position.Y * mmToUnits);
            }
            else
            {
                // Fallback: arrange in a wider-than-tall grid like the
                // synthetic layouts in DeviceGridHelper.
                int cols = System.Math.Max(1, (int)System.Math.Ceiling(System.Math.Sqrt(lampArray.LampCount * 4.0 / 3.0)));
                int row = lampIndex / cols;
                int col = lampIndex % cols;
                location = new Point(col * 30f, row * 30f);
            }

            var led = AddLed(ledId, location, size);
            if (led != null) led.Shape = Shape.Rectangle;
        }
    }
}
