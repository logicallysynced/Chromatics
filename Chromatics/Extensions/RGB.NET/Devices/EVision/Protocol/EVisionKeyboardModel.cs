namespace Chromatics.Extensions.RGB.NET.Devices.EVision.Protocol
{
    // Keyboards on OpenRGB's EVisionKeyboardController (V1). One firmware
    // family (Sonix VS11K28A controller) speaks one HID protocol across
    // 13 OEM-rebranded keyboards. Glorious's GMMK TKL and Redragon's
    // K552-2 are the same hardware as each other; the controller in
    // OpenRGB does not branch per device beyond display-name selection.
    //
    // Each entry pairs a (VID, PID) at the wire and a friendly display
    // name. PIDs are unique within their VID so the enum-value carries
    // both VID and PID (high 16 bits = VID, low 16 bits = PID) — see
    // EVisionDiscovery for the unpack.
    //
    // The K530 Draconic Pro and K568 Dark Avenger are NOT present in
    // OpenRGB's detection table even though they're often listed as
    // EVision-family by community wikis. Without a confirmed PID, they
    // can't safely be added here — the same VID is reused by Redragon
    // for non-EVision keyboards, and a misclassified PID would brick
    // the wrong board.
    // Underlying type is uint so values like 0x320F_5064 (840,167,524) can be
    // declared without `unchecked` casts and so EVisionDiscovery can pass a
    // uint VID/PID composite straight into Enum.IsDefined. The default
    // underlying type (int) accepts these values too, but Enum.IsDefined
    // throws ArgumentException("Enum underlying type and the object must be
    // same type") when the argument's runtime type doesn't match the enum's
    // underlying type, so the discovery uint had to be reboxed to int every
    // call. Making it uint here removes that conversion entirely.
    public enum EVisionKeyboardModel : uint
    {
        Unknown = 0,

        // VID 0x0C45 (Sonix/Microchip)
        Redragon_K552       = 0x0C45_5104,
        Redragon_K556       = 0x0C45_5004,
        Redragon_K550       = 0x0C45_5204,
        Tecware_PhantomElite= 0x0C45_652F,
        Womier_K66          = 0x0C45_7698,
        Warrior_KaneTC235   = 0x0C45_8520,
        Gamepower_OgreRGB   = 0x0C45_7672,

        // VID 0x320F
        Glorious_GMMK_TKL   = 0x320F_5064,
        Redragon_K552_V2    = 0x320F_5000,
        MarsGaming_MKMini   = 0x320F_5078,
        Womier_K87          = 0x320F_502A,
        Skillkorp_K5        = 0x320F_505B,
        DEXP_Blaze          = 0x320F_5084,
    }

    public static class EVisionKeyboardModelExtensions
    {
        public static int Vid(this EVisionKeyboardModel model) => (int)((uint)model >> 16);
        public static int Pid(this EVisionKeyboardModel model) => (int)((uint)model & 0xFFFF);

        public static string DisplayName(this EVisionKeyboardModel model, int vid, int pid)
        {
            return model switch
            {
                EVisionKeyboardModel.Redragon_K552        => "Redragon K552 Kumara",
                EVisionKeyboardModel.Redragon_K556        => "Redragon K556 Devarajas",
                EVisionKeyboardModel.Redragon_K550        => "Redragon K550 Yama",
                EVisionKeyboardModel.Redragon_K552_V2     => "Redragon K552-2 Kumara V2",
                EVisionKeyboardModel.Glorious_GMMK_TKL    => "Glorious GMMK TKL",
                EVisionKeyboardModel.Tecware_PhantomElite => "Tecware Phantom Elite",
                EVisionKeyboardModel.Womier_K66           => "Womier K66",
                EVisionKeyboardModel.Womier_K87           => "Womier K87",
                EVisionKeyboardModel.MarsGaming_MKMini    => "Mars Gaming MKMini",
                EVisionKeyboardModel.Skillkorp_K5         => "Skillkorp K5",
                EVisionKeyboardModel.DEXP_Blaze           => "DEXP Blaze",
                EVisionKeyboardModel.Warrior_KaneTC235    => "Warrior Kane TC235",
                EVisionKeyboardModel.Gamepower_OgreRGB    => "Gamepower Ogre RGB",
                _ => $"EVision Keyboard ({vid:X4}:{pid:X4})",
            };
        }
    }
}
