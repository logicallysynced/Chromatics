namespace Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol
{
    // LIFX LAN protocol message type IDs. Numbers match
    // lan.developer.lifx.com/docs/packet-types.
    internal static class LifxMessageTypes
    {
        public const ushort GetService              = 2;
        public const ushort StateService            = 3;

        public const ushort GetHostFirmware         = 14;
        public const ushort StateHostFirmware       = 15;

        public const ushort GetPower                = 20;
        public const ushort SetPower                = 21;
        public const ushort StatePower              = 22;

        public const ushort GetLabel                = 23;
        public const ushort StateLabel              = 25;

        public const ushort GetVersion              = 32;
        public const ushort StateVersion            = 33;

        public const ushort GetColor                = 101;
        public const ushort SetColor                = 102;
        public const ushort SetWaveform             = 103;
        public const ushort LightState              = 107;

        public const ushort GetLightPower           = 116;
        public const ushort SetLightPower           = 117;
        public const ushort StateLightPower         = 118;

        public const ushort GetColorZones           = 502;
        public const ushort SetColorZones           = 501;
        public const ushort StateZone               = 503;
        public const ushort StateMultiZone          = 506;

        public const ushort GetExtendedColorZones   = 511;
        public const ushort SetExtendedColorZones   = 510;
        public const ushort StateExtendedColorZones = 512;

        public const ushort SetTileState64          = 715;
        public const ushort GetTileState64          = 707;
        public const ushort StateTileState64        = 711;
    }
}
