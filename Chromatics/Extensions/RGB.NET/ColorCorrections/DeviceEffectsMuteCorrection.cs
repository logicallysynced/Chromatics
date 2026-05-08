using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.ColorCorrections
{
    // Per-device colour correction that blacks out every LED when the user
    // has unticked the EffectLayer enable checkbox for the device on the
    // Mappings tab.
    //
    // We already gate layer-dispatch on the same flag in GameController.Update
    // so the per-layer processors never run, but layer dispatch isn't the only
    // source of paint — tagged effects (startup animation, title screen
    // animation) and the raid-effect overlay attach ListLedGroups directly to
    // the surface, bypassing the layer system entirely. They'd keep painting
    // even with effects "disabled".
    //
    // IColorCorrection runs as the final transformation on every Color before
    // it reaches the device's UpdateQueue, so muting here covers ALL paint
    // sources uniformly. The flag is refreshed once per frame in
    // RGBController.Surface_Updating from MappingLayers.IsDeviceEffectsEnabled,
    // so toggling the checkbox takes effect on the next render tick.
    public sealed class DeviceEffectsMuteCorrection : IColorCorrection
    {
        private volatile bool _isMuted;

        public bool IsMuted
        {
            get => _isMuted;
            set => _isMuted = value;
        }

        public void ApplyTo(ref Color color)
        {
            if (!_isMuted) return;
            color = new Color(color.A, 0f, 0f, 0f);
        }
    }
}
