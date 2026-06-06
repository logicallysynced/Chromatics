namespace Chromatics.Extensions.RGB.NET.Devices.Redragon.Protocol
{
    // Known Redragon mouse models on the shared OpenRGB protocol family.
    //
    // All entries here are 13 mice that speak the same 16-byte HID feature
    // report protocol (RedragonMouseProtocol). USB vendor id is 0x04D9 for
    // every one of them. The HID interface is interface 2 with usage page
    // 0xFFA0 — RedragonDiscovery uses the VID match and verifies the
    // interface descriptor before adopting.
    //
    // Source: OpenRGB Controllers/RedragonController + dokutan/mouse_m908.
    // The first 9 (Confirmed) have merged OpenRGB drivers. The remaining
    // 4 (Compatible) are confirmed protocol-compatible by dokutan but not
    // upstreamed to OpenRGB — they should work identically but flag in the
    // log so users know the test surface is smaller.
    //
    // Models with VID 0x25A7 (M913 Impact Elite, M686 Vampire) use a
    // different protocol family and are intentionally excluded from this
    // table. They need their own protocol class once we have the source.
    public enum RedragonMouseModel
    {
        Unknown = 0,

        // OpenRGB-confirmed — merged drivers in Controllers/RedragonController.
        M711_Cobra        = 0xFC30,
        M715_Dagger       = 0xFC39,
        M716_Inquisitor   = 0xFC3A,
        M602_Griffin      = 0xFC38,
        M808_Storm        = 0xFC5F,
        M801_Sniper       = 0xFC58,
        M810_Taipan       = 0xFA7E,
        M908_Impact       = 0xFC4D,
        M987_Reaping      = 0xFC69,

        // Compatible — same protocol per dokutan/mouse_m908, not in OpenRGB.
        M719_Invader      = 0xFC4F,
        M990_Legend       = 0xFC41,
        M709_Tiger        = 0xFC2A,
        M721Pro_Lonewolf2 = 0xFC5C,
    }

    public static class RedragonMouseModelExtensions
    {
        // Human-readable label for log lines and the Mapping-tab device list.
        // Returns "Redragon Mouse (0xPID)" for any PID we don't have a friendly
        // name for — covers future hardware that ships with the same OpenRGB
        // protocol on a different PID before we get a chance to add it here.
        public static string DisplayName(this RedragonMouseModel model, int productId)
        {
            return model switch
            {
                RedragonMouseModel.M711_Cobra        => "Redragon M711 Cobra",
                RedragonMouseModel.M715_Dagger       => "Redragon M715 Dagger",
                RedragonMouseModel.M716_Inquisitor   => "Redragon M716 Inquisitor",
                RedragonMouseModel.M602_Griffin      => "Redragon M602 Griffin",
                RedragonMouseModel.M808_Storm        => "Redragon M808 Storm",
                RedragonMouseModel.M801_Sniper       => "Redragon M801 Sniper",
                RedragonMouseModel.M810_Taipan       => "Redragon M810 Taipan",
                RedragonMouseModel.M908_Impact       => "Redragon M908 Impact",
                RedragonMouseModel.M987_Reaping      => "Redragon M987 Reaping",
                RedragonMouseModel.M719_Invader      => "Redragon M719 Invader",
                RedragonMouseModel.M990_Legend       => "Redragon M990 Legend",
                RedragonMouseModel.M709_Tiger        => "Redragon M709 Tiger",
                RedragonMouseModel.M721Pro_Lonewolf2 => "Redragon M721-Pro Lonewolf 2",
                _                                    => $"Redragon Mouse (0x{productId:X4})",
            };
        }
    }
}
