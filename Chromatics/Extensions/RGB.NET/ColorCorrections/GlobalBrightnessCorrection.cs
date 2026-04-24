using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.ColorCorrections
{
    public sealed class GlobalBrightnessCorrection : IColorCorrection
    {
        public static GlobalBrightnessCorrection Instance { get; } = new GlobalBrightnessCorrection();

        private volatile int _brightnessPercent = 100;

        public int BrightnessPercent
        {
            get => _brightnessPercent;
            set
            {
                if (value < 0) value = 0;
                else if (value > 100) value = 100;
                _brightnessPercent = value;
            }
        }

        public void ApplyTo(ref Color color)
        {
            int pct = _brightnessPercent;
            if (pct >= 100) return;
            if (pct <= 0)
            {
                color = new Color(color.A, 0f, 0f, 0f);
                return;
            }

            float scale = pct / 100f;
            color = new Color(color.A, color.R * scale, color.G * scale, color.B * scale);
        }
    }
}
