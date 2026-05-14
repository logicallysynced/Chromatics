using System;
using System.Buffers.Binary;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol
{
    // VIA protocol command IDs and frame builders. Reference:
    // https://www.caniusevia.com/docs/specification — the live spec
    // for VIA's keyboard configuration protocol. VIA-compatible firmware
    // is the lowest-common-denominator: most stock QMK boards ship with
    // it enabled, and it exposes RGB matrix mode + base hue/sat/val
    // controls even when the firmware doesn't have OpenRGB's per-key
    // plugin built in.
    internal static class ViaProtocol
    {
        // Top-level command IDs (byte 0 of the request payload).
        public const byte Id_GetProtocolVersion       = 0x01;
        public const byte Id_GetKeyboardValue         = 0x02;
        public const byte Id_SetKeyboardValue         = 0x03;
        public const byte Id_LightingSetValue         = 0x07;
        public const byte Id_LightingGetValue         = 0x08;
        public const byte Id_LightingSave             = 0x09;

        // Lighting sub-commands (byte 1 of request when command = 0x07/0x08).
        // The "QMK RGB Matrix" naming reflects firmware-side terminology;
        // VIA-compatible firmware exposes these on RGB matrix builds.
        public const byte SubId_QmkRgbMatrixBrightness = 0x81;
        public const byte SubId_QmkRgbMatrixEffect     = 0x82;
        public const byte SubId_QmkRgbMatrixEffectSpeed = 0x83;
        public const byte SubId_QmkRgbMatrixColor      = 0x84;

        // RGB Matrix effect indices. Only SolidColor is hard-required for
        // Tier 1; the rest are useful as escape hatches for game-driven
        // mode switches (e.g. Vegas → CycleAll, idle → SolidReactiveCross).
        public const byte Effect_SolidColor        = 1;
        public const byte Effect_AlphaMods         = 2;
        public const byte Effect_GradientUpDown    = 3;
        public const byte Effect_Breathing         = 6;
        public const byte Effect_CycleAll          = 9;
        public const byte Effect_RainbowMovingChevron = 11;
        public const byte Effect_DualBeacon        = 16;
        public const byte Effect_RainbowBeacon     = 17;
        public const byte Effect_RainbowPinwheels  = 18;
        public const byte Effect_SolidSplash       = 30;

        // ── Frame builders ────────────────────────────────────────────
        // All builders write into a caller-supplied 32-byte payload span
        // (no allocation). Caller is responsible for the leading report-id
        // byte the HidSharp output buffer needs.

        public static void BuildGetProtocolVersion(Span<byte> payload)
        {
            payload.Clear();
            payload[0] = Id_GetProtocolVersion;
        }

        public static void BuildSetRgbMatrixEffect(Span<byte> payload, byte effectIndex)
        {
            payload.Clear();
            payload[0] = Id_LightingSetValue;
            payload[1] = SubId_QmkRgbMatrixEffect;
            payload[2] = effectIndex;
        }

        public static void BuildSetRgbMatrixBrightness(Span<byte> payload, byte brightness0to255)
        {
            payload.Clear();
            payload[0] = Id_LightingSetValue;
            payload[1] = SubId_QmkRgbMatrixBrightness;
            payload[2] = brightness0to255;
        }

        // Hue + saturation in a single packet — VIA's
        // QMK_RGB_MATRIX_COLOR sub-command takes 2 bytes after the
        // sub-id. Both values are 0-255.
        public static void BuildSetRgbMatrixColor(Span<byte> payload, byte hue0to255, byte sat0to255)
        {
            payload.Clear();
            payload[0] = Id_LightingSetValue;
            payload[1] = SubId_QmkRgbMatrixColor;
            payload[2] = hue0to255;
            payload[3] = sat0to255;
        }

        public static void BuildLightingSave(Span<byte> payload)
        {
            payload.Clear();
            payload[0] = Id_LightingSave;
        }

        // Reply parser for GetProtocolVersion. Returns 0 if the reply
        // doesn't echo the command id (firmware doesn't speak VIA, or
        // request collided with another consumer on the same HID interface).
        public static ushort TryParseProtocolVersion(ReadOnlySpan<byte> reply)
        {
            if (reply.Length < 3) return 0;
            if (reply[0] != Id_GetProtocolVersion) return 0;
            return BinaryPrimitives.ReadUInt16BigEndian(reply.Slice(1, 2));
        }
    }
}
