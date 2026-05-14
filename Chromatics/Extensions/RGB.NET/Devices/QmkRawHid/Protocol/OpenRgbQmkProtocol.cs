using System;
using System.Buffers.Binary;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol
{
    // OpenRGB QMK plugin command set (master branch reference, see
    // https://gitlab.com/CalcProgrammer1/OpenRGB → Controllers/QMKOpenRGBController).
    // This sits ON TOP of the same Raw HID transport that VIA uses;
    // VIA's commands occupy id 0x01-0x09, OpenRGB-QMK's start at 0x20
    // so the two can coexist on one HID interface. Firmware support
    // requires the qmk_openrgb fork or patch — we detect at handshake
    // and gracefully fall back to ViaProtocol if absent.
    //
    // Wire format: every request is `cmd_byte | request_payload[31]`.
    // Replies echo cmd_byte in byte 0; mismatched echoes mean the
    // firmware doesn't speak this command.
    internal static class OpenRgbQmkProtocol
    {
        public const byte Cmd_GetProtocolVersion   = 0x20;
        public const byte Cmd_GetQmkVersion        = 0x21;
        public const byte Cmd_GetDeviceInfo        = 0x22;
        public const byte Cmd_GetLedInfo           = 0x23;
        public const byte Cmd_GetEnabledModes      = 0x24;
        public const byte Cmd_GetLedMatrixSize     = 0x25;
        public const byte Cmd_SetLedRange          = 0x27;
        public const byte Cmd_SetSingleLed         = 0x28;
        public const byte Cmd_SetMode              = 0x29;
        public const byte Cmd_Save                 = 0x2A;

        // Each Cmd_SetLedRange packet carries (start_idx u16 | count u8 |
        // RGB triplets). Header overhead = 1 byte cmd + 2 bytes start +
        // 1 byte count = 4 bytes, leaving 28 bytes / 3 = 9 LEDs per packet.
        // Caller chunks the strip and paces packets to stay within the
        // firmware's RX queue (typical RX queue is 4-8 deep at full speed,
        // hence the 1ms inter-packet pacing we use in the queue).
        public const int  MaxLedsPerSetRange = 9;

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

        // Cmd_GetLedInfo takes (start_idx u16) and returns matrix
        // (column, row) + flags for up to N LEDs starting at start_idx.
        // The firmware decides N based on remaining payload room
        // (typically 9 LEDs * 3 bytes per record = 27 + 1 byte echo).
        public static void BuildGetLedInfo(Span<byte> payload, ushort startIndex)
        {
            payload.Clear();
            payload[0] = Cmd_GetLedInfo;
            BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(1, 2), startIndex);
        }

        public static void BuildGetLedMatrixSize(Span<byte> payload)
        {
            payload.Clear();
            payload[0] = Cmd_GetLedMatrixSize;
        }

        // Bulk set: pack RGB triplets starting at byte 4. Caller must
        // ensure rgbBytes.Length == count * 3 and count <= MaxLedsPerSetRange.
        public static void BuildSetLedRange(Span<byte> payload, ushort startIndex, byte count, ReadOnlySpan<byte> rgbBytes)
        {
            if (count > MaxLedsPerSetRange) throw new ArgumentOutOfRangeException(nameof(count));
            if (rgbBytes.Length != count * 3) throw new ArgumentException("rgbBytes length must equal count*3", nameof(rgbBytes));
            payload.Clear();
            payload[0] = Cmd_SetLedRange;
            BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(1, 2), startIndex);
            payload[3] = count;
            rgbBytes.CopyTo(payload.Slice(4, count * 3));
        }

        public static void BuildSetSingleLed(Span<byte> payload, ushort index, byte r, byte g, byte b)
        {
            payload.Clear();
            payload[0] = Cmd_SetSingleLed;
            BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(1, 2), index);
            payload[3] = r;
            payload[4] = g;
            payload[5] = b;
        }

        // Switch the firmware to direct host-driven mode (mode 0 in the
        // OpenRGB-QMK convention) or back to a built-in effect index.
        // Direct mode suspends the firmware's own RGB matrix animations
        // so our Set commands aren't fighting them every tick.
        public static void BuildSetMode(Span<byte> payload, byte modeIndex)
        {
            payload.Clear();
            payload[0] = Cmd_SetMode;
            payload[1] = modeIndex;
        }

        // ── Reply parsers ─────────────────────────────────────────────

        public static ushort TryParseProtocolVersion(ReadOnlySpan<byte> reply)
        {
            if (reply.Length < 3) return 0;
            if (reply[0] != Cmd_GetProtocolVersion) return 0;
            return BinaryPrimitives.ReadUInt16LittleEndian(reply.Slice(1, 2));
        }

        // GetDeviceInfo reply layout:
        //   byte 0     = Cmd_GetDeviceInfo (echo)
        //   bytes 1-2  = total LED count (uint16 LE)
        //   bytes 3-4  = vendor id (uint16 LE)
        //   bytes 5-6  = product id (uint16 LE)
        //   byte 7     = device type (informational)
        //   bytes 8+   = null-terminated device name
        public static bool TryParseDeviceInfo(ReadOnlySpan<byte> reply, out ushort ledCount, out ushort vendorId, out ushort productId, out string deviceName)
        {
            ledCount = 0; vendorId = 0; productId = 0; deviceName = string.Empty;
            if (reply.Length < 8) return false;
            if (reply[0] != Cmd_GetDeviceInfo) return false;
            ledCount  = BinaryPrimitives.ReadUInt16LittleEndian(reply.Slice(1, 2));
            vendorId  = BinaryPrimitives.ReadUInt16LittleEndian(reply.Slice(3, 2));
            productId = BinaryPrimitives.ReadUInt16LittleEndian(reply.Slice(5, 2));

            int nameEnd = 8;
            while (nameEnd < reply.Length && reply[nameEnd] != 0) nameEnd++;
            if (nameEnd > 8)
                deviceName = System.Text.Encoding.UTF8.GetString(reply.Slice(8, nameEnd - 8));
            return true;
        }

        // GetLedInfo reply layout (variable LED records, 3 bytes each):
        //   byte 0     = Cmd_GetLedInfo (echo)
        //   byte 1     = record count for this packet
        //   then records: column (u8) | row (u8) | flags (u8)
        // Caller iterates start_index for additional batches.
        public static bool TryParseLedInfoBatch(ReadOnlySpan<byte> reply, out int recordCount, out ReadOnlySpan<byte> records)
        {
            recordCount = 0; records = default;
            if (reply.Length < 2) return false;
            if (reply[0] != Cmd_GetLedInfo) return false;
            recordCount = reply[1];
            int recordBytes = recordCount * 3;
            if (reply.Length < 2 + recordBytes) return false;
            records = reply.Slice(2, recordBytes);
            return true;
        }

        public static bool TryParseLedMatrixSize(ReadOnlySpan<byte> reply, out byte columns, out byte rows)
        {
            columns = 0; rows = 0;
            if (reply.Length < 3) return false;
            if (reply[0] != Cmd_GetLedMatrixSize) return false;
            columns = reply[1];
            rows = reply[2];
            return true;
        }

        public readonly struct LedRecord
        {
            public readonly byte Column;
            public readonly byte Row;
            public readonly byte Flags;
            public LedRecord(byte column, byte row, byte flags) { Column = column; Row = row; Flags = flags; }
        }
    }
}
