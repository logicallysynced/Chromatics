using System;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol
{
    // OpenRGB-QMK firmware module command set. Reference firmware lives at
    // quantum/openrgb.{c,h} in the Kasper24/qmk_firmware develop-openrgb
    // tree. The firmware REPLACES VIA's raw_hid_receive when OPENRGB_ENABLE
    // is set, so command id 0x01 (id_get_protocol_version in VIA, get
    // protocol version in OpenRGB-QMK) goes to the OpenRGB handler. We
    // distinguish the two protocols at handshake time by the
    // END_OF_MESSAGE terminator that OpenRGB-QMK writes at the last byte
    // of every reply.
    //
    // Wire format: every request is `cmd_byte | request_payload`. Replies
    // echo cmd_byte in byte 0. Raw HID endpoint size is firmware-defined —
    // OpenRGB-QMK overrides it to 64 bytes via `#define RAW_EPSIZE 64`.
    // Pass slices sized to the device's actual OutputReportByteLength minus
    // the leading report-id byte; callers in this codebase get that size
    // from the HidP_GetCaps probe at discovery time.
    internal static class OpenRgbQmkProtocol
    {
        public const byte Cmd_GetProtocolVersion       = 0x01;
        public const byte Cmd_GetQmkVersion            = 0x02;
        public const byte Cmd_GetDeviceInfo            = 0x03;
        public const byte Cmd_GetModeInfo              = 0x04;
        public const byte Cmd_GetLedInfo               = 0x05;
        public const byte Cmd_GetEnabledModes          = 0x06;
        public const byte Cmd_SetMode                  = 0x07;
        public const byte Cmd_DirectModeSetSingleLed   = 0x08;
        public const byte Cmd_DirectModeSetLeds        = 0x09;

        public const byte Response_Failure       = 25;
        public const byte Response_Success       = 50;
        public const byte Response_EndOfMessage  = 100;

        // Each Cmd_DirectModeSetLeds packet header is 3 bytes (cmd | first_led
        // u8 | number_leds u8). Each LED contributes 3 RGB bytes. For a 64-byte
        // raw HID endpoint that leaves (64 - 3) / 3 = 20 LEDs per packet.
        public const int MaxLedsPerSetLeds_RawEpSize64 = (64 - 3) / 3;

        // The firmware encodes LED info as a 7-byte record per LED:
        //   x | y | flags | r | g | b | keycode
        public const int LedInfoRecordBytes = 7;
        // Budget = payloadBytes - 1 (cmd echo at [0]) - 1 (END_OF_MESSAGE
        // terminator at [payloadBytes-1]). The firmware writes the terminator
        // *after* the LED records, so a record whose 7th byte (keycode)
        // lands on [payloadBytes-1] gets clobbered — every 9th LED would
        // come back with keycode 0x64 if we asked for 9 records per packet.
        public const int MaxLedRecordsPerGetLedInfo_RawEpSize64 = (64 - 2) / LedInfoRecordBytes;

        // ── Frame builders ────────────────────────────────────────────

        public static void BuildGetProtocolVersion(Span<byte> payload)
        {
            payload.Clear();
            payload[0] = Cmd_GetProtocolVersion;
        }

        public static void BuildGetDeviceInfo(Span<byte> payload)
        {
            payload.Clear();
            payload[0] = Cmd_GetDeviceInfo;
        }

        public static void BuildGetLedInfo(Span<byte> payload, byte firstLed, byte numberLeds)
        {
            payload.Clear();
            payload[0] = Cmd_GetLedInfo;
            payload[1] = firstLed;
            payload[2] = numberLeds;
        }

        // Switch the firmware's active rgb_matrix effect. To paint individual
        // LEDs via DirectModeSetLeds, the active mode must be the OPENRGB_DIRECT
        // custom effect — the firmware writes SET_LEDS payload bytes into
        // g_openrgb_direct_mode_colors[] regardless of active mode, but those
        // colors only reach the hardware when OPENRGB_DIRECT is the active
        // mode. The mode index for OPENRGB_DIRECT depends on which other
        // built-in effects are compiled in; callers must discover it via
        // Cmd_GetEnabledModes (it's the highest index not in the enabled-modes
        // list) or set it indirectly via the firmware's keymap.
        public static void BuildSetMode(Span<byte> payload, byte hue, byte sat, byte val, byte mode, byte speed, bool save)
        {
            payload.Clear();
            payload[0] = Cmd_SetMode;
            payload[1] = hue;
            payload[2] = sat;
            payload[3] = val;
            payload[4] = mode;
            payload[5] = speed;
            payload[6] = (byte)(save ? 1 : 0);
        }

        public static void BuildDirectModeSetSingleLed(Span<byte> payload, byte ledIndex, byte r, byte g, byte b)
        {
            payload.Clear();
            payload[0] = Cmd_DirectModeSetSingleLed;
            payload[1] = ledIndex;
            payload[2] = r;
            payload[3] = g;
            payload[4] = b;
        }

        // Bulk set: pack RGB triplets starting at byte 3. The firmware reads
        // r,g,b in that order from data[3..]. Caller must ensure
        // rgbBytes.Length == count * 3 and count <= max-leds-per-packet for
        // the device's actual report size.
        public static void BuildDirectModeSetLeds(Span<byte> payload, byte firstLed, byte count, ReadOnlySpan<byte> rgbBytes)
        {
            if (rgbBytes.Length != count * 3)
                throw new ArgumentException("rgbBytes length must equal count * 3", nameof(rgbBytes));
            payload.Clear();
            payload[0] = Cmd_DirectModeSetLeds;
            payload[1] = firstLed;
            payload[2] = count;
            rgbBytes.CopyTo(payload.Slice(3, count * 3));
        }

        public static void BuildGetEnabledModes(Span<byte> payload)
        {
            payload.Clear();
            payload[0] = Cmd_GetEnabledModes;
        }

        // ── Reply parsers ─────────────────────────────────────────────

        // Firmware reply: [0]=echo, [1]=OPENRGB_PROTOCOL_VERSION (single byte
        // — 0x0B or 0x0C in the trees we've seen). We accept any non-zero
        // version as "this is OpenRGB-QMK speaking" — the END_OF_MESSAGE
        // terminator already disambiguates from VIA at the discovery layer.
        public static byte TryParseProtocolVersion(ReadOnlySpan<byte> reply)
        {
            if (reply.Length < 2) return 0;
            if (reply[0] != Cmd_GetProtocolVersion) return 0;
            return reply[1];
        }

        // Reply layout (firmware quantum/openrgb.c openrgb_get_device_info):
        //   [0] = Cmd_GetDeviceInfo (echo)
        //   [1] = DRIVER_LED_TOTAL (uint8)
        //   [2] = MATRIX_COLS * MATRIX_ROWS (uint8)
        //   [3..] = PRODUCT string, null-terminated
        //   then  = MANUFACTURER string until end of buffer
        public static bool TryParseDeviceInfo(ReadOnlySpan<byte> reply, out byte ledCount, out byte matrixSize, out string productName, out string manufacturer)
        {
            ledCount = 0; matrixSize = 0; productName = string.Empty; manufacturer = string.Empty;
            if (reply.Length < 3) return false;
            if (reply[0] != Cmd_GetDeviceInfo) return false;
            ledCount   = reply[1];
            matrixSize = reply[2];

            int i = 3;
            int productStart = i;
            while (i < reply.Length && reply[i] != 0) i++;
            if (i > productStart)
                productName = System.Text.Encoding.UTF8.GetString(reply.Slice(productStart, i - productStart));

            if (i < reply.Length) i++; // skip null terminator
            int manufacturerStart = i;
            while (i < reply.Length && reply[i] != 0) i++;
            if (i > manufacturerStart)
                manufacturer = System.Text.Encoding.UTF8.GetString(reply.Slice(manufacturerStart, i - manufacturerStart));

            return true;
        }

        // Reply layout (firmware openrgb_get_led_info):
        //   [0] = Cmd_GetLedInfo (echo)
        //   then 7 bytes per LED record: x | y | flags | r | g | b | keycode
        // Caller knows how many records it asked for; the firmware doesn't
        // include a count byte in the reply. flags byte equals
        // OPENRGB_FAILURE (25) when the led index was out of range, which
        // tells the caller to stop iterating.
        public static bool TryParseLedInfoRecord(ReadOnlySpan<byte> reply, int recordIndex,
            out byte x, out byte y, out byte flags, out byte keycode)
        {
            x = 0; y = 0; flags = 0; keycode = 0;
            if (reply.Length < 1) return false;
            if (reply[0] != Cmd_GetLedInfo) return false;
            int off = 1 + recordIndex * LedInfoRecordBytes;
            if (off + LedInfoRecordBytes > reply.Length) return false;
            x       = reply[off + 0];
            y       = reply[off + 1];
            flags   = reply[off + 2];
            // r,g,b at off+3..+5 — current direct-mode colour, not interesting here.
            keycode = reply[off + 6];
            return true;
        }

        // Per-device "max LEDs per packet" calc — header is 3 bytes
        // (cmd | first_led | number_leds), payload is RGB triplets.
        public static int MaxLedsPerSetLeds(int payloadBytes)
        {
            int budget = payloadBytes - 3;
            return budget > 0 ? budget / 3 : 0;
        }

        public static int MaxLedRecordsPerGetLedInfo(int payloadBytes)
        {
            int budget = payloadBytes - 2;
            return budget > 0 ? budget / LedInfoRecordBytes : 0;
        }
    }
}
