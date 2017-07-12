using System.Collections.Generic;
using System.Drawing;
using CUE.NET.Brushes;
using CUE.NET.Devices.Generic;
using CUE.NET.Devices.Generic.Enums;

namespace Chromatics.CorsairLibs
{
    public class KeyMapBrush : AbstractBrush
    {
        private readonly Dictionary<CorsairLedId, Color> _keyMap = new Dictionary<CorsairLedId, Color>();

        public void ClearKeyMap()
        {
            _keyMap.Clear();
        }

        // depending on how you call this method you might need to lock the dictionary in all the methods to prevent weird exceptions due to concurrent accesses from different threads.
        public void CorsairApplyMapKeyLighting(CorsairLedId key, Color col)
        {
            _keyMap[key] = col;
        }

        //CorsairApplyAllKeyLighting
        public void CorsairApplyAllKeyLighting(CorsairLed[] keys, Color col)
        {
            foreach (var key in keys)
                _keyMap[key] = col;
        }

        public CorsairColor CorsairGetColorReference(CorsairLedId key)
        {
            Color color;
            if (_keyMap.TryGetValue(key, out color))
                return color;

            return CorsairColor.Transparent;
        }

        // If you're able to include your logic which key to color here this might get easier than working with the dictionary and stuff.
        protected override CorsairColor GetColorAtPoint(RectangleF rectangle, BrushRenderTarget renderTarget)
        {
            var ledId = renderTarget.LedId;

            Color color;
            if (_keyMap.TryGetValue(ledId, out color))
                return color;

            return CorsairColor.Transparent;
        }
    }
}