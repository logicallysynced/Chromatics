using System;
using System.Buffers.Binary;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol
{
    // VIA "Custom" protocol command IDs and frame builders. Reference:
    // QMK firmware quantum/via.h + quantum/via.c on master.
    //
    // RGB Matrix lighting in modern VIA-compatible QMK firmware goes
    // through the channel-based id_custom_set_value protocol — NOT the
    // older flat id_lighting_set_value path some older docs describe.
    // The on-wire layout is:
    //
    //   data[0] = 0x07         (id_custom_set_value)   — top-level command
    //   data[1] = 0x03         (id_qmk_rgb_matrix_channel) — channel selector
    //   data[2] = value_id     (1=brightness, 2=effect, 3=speed, 4=color)
    //   data[3..] = payload    (effect index, hue, sat, etc.)
    //
    // Pre-4.2.35 builds were sending data[0]=0x07 then immediately
    // data[1]=sub-id (0x81..0x84) with no channel byte — the firmware's
    // via_qmk_rgb_matrix_command never matched the channel, the packet
    // was silently dropped, and nothing visible happened on the board.
    internal static class ViaProtocol
    {
        // Top-level command IDs (data[0]).
        public const byte Id_GetProtocolVersion   = 0x01;
        public const byte Id_GetKeyboardValue     = 0x02;
        public const byte Id_SetKeyboardValue     = 0x03;
        public const byte Id_CustomSetValue       = 0x07;
        public const byte Id_CustomGetValue       = 0x08;
        public const byte Id_CustomSave           = 0x09;

        // Channel selector (data[1]) for the RGB Matrix subsystem.
        // QMK also defines id_qmk_backlight_channel (0x00),
        // id_qmk_rgblight_channel (0x01), id_qmk_led_matrix_channel (0x02),
        // and id_qmk_audio_channel (0x04) — we only target rgb_matrix here
        // because that's the per-key-RGB lighting subsystem.
        public const byte Channel_QmkRgbMatrix    = 0x03;

        // Value IDs (data[2]) within the RGB Matrix channel. From
        // qmk_firmware/quantum/via.h enum via_qmk_rgb_matrix_value.
        public const byte ValueId_RgbMatrixBrightness  = 1;
        public const byte ValueId_RgbMatrixEffect      = 2;
        public const byte ValueId_RgbMatrixEffectSpeed = 3;
        public const byte ValueId_RgbMatrixColor       = 4;

        // RGB Matrix effect indices. Only SolidColor is hard-required for
        // Tier 1; the rest are useful as escape hatches for game-driven
        // mode switches (e.g. Vegas → CycleAll, idle → SolidReactiveCross).
        public const byte Effect_SolidColor           = 1;
        public const byte Effect_AlphaMods            = 2;
        public const byte Effect_GradientUpDown       = 3;
        public const byte Effect_Breathing            = 6;
        public const byte Effect_CycleAll             = 9;
        public const byte Effect_RainbowMovingChevron = 11;
        public const byte Effect_DualBeacon           = 16;
        public const byte Effect_RainbowBeacon        = 17;
        public const byte Effect_RainbowPinwheels     = 18;
        public const byte Effect_SolidSplash          = 30;

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
            payload[0] = Id_CustomSetValue;
            payload[1] = Channel_QmkRgbMatrix;
            payload[2] = ValueId_RgbMatrixEffect;
            payload[3] = effectIndex;
        }

        public static void BuildSetRgbMatrixBrightness(Span<byte> payload, byte brightness0to255)
        {
            payload.Clear();
            payload[0] = Id_CustomSetValue;
            payload[1] = Channel_QmkRgbMatrix;
            payload[2] = ValueId_RgbMatrixBrightness;
            payload[3] = brightness0to255;
        }

        // Hue + saturation in a single packet — the RGB Matrix Color
        // value carries 2 bytes of payload (hue at value_data[0],
        // saturation at value_data[1]). Both 0-255.
        public static void BuildSetRgbMatrixColor(Span<byte> payload, byte hue0to255, byte sat0to255)
        {
            payload.Clear();
            payload[0] = Id_CustomSetValue;
            payload[1] = Channel_QmkRgbMatrix;
            payload[2] = ValueId_RgbMatrixColor;
            payload[3] = hue0to255;
            payload[4] = sat0to255;
        }

        // Persist the current RGB matrix settings to EEPROM so they
        // survive a reboot. Not needed every frame — call sparingly
        // when the user explicitly wants their config saved.
        public static void BuildLightingSave(Span<byte> payload)
        {
            payload.Clear();
            payload[0] = Id_CustomSave;
            payload[1] = Channel_QmkRgbMatrix;
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
