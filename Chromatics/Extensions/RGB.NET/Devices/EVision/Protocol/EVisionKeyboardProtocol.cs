using System;

namespace Chromatics.Extensions.RGB.NET.Devices.EVision.Protocol
{
    // Wire format for OpenRGB's EVisionKeyboardController (V1). One
    // session's worth of LED state updates is nine HID output reports:
    //
    //   1.  Begin packet                  (Cmd 0x01)
    //   2-8. Seven 54-byte data chunks    (Cmd 0x11) — covers 126 LEDs
    //   9.  End packet                    (Cmd 0x02)
    //
    // Every packet is exactly 64 bytes with report-id byte 0x04 at
    // offset 0. Begin and End are fixed payloads with no checksum;
    // every Cmd 0x11 data packet computes a 16-bit checksum over
    // bytes [3..63] and writes it little-endian into [1..2].
    //
    // The protocol is a flash-write per frame (no Direct streaming
    // mode like the V2 variant). Effects that paint at full update
    // rate continuously will wear the device's lighting flash over
    // years of use; one practical mitigation is to skip transmission
    // when the colour-buffer cache hasn't changed (done in
    // EVisionUpdateQueue) so static layers stop writing entirely.
    internal static class EVisionKeyboardProtocol
    {
        public const int ReportLength = 64;
        public const byte ReportPrefix = 0x04;
        public const int  TotalLeds    = 126;
        public const int  ColorBufferSize = TotalLeds * 3;   // 378 bytes
        public const int  ChunksPerFrame  = 7;
        public const int  MaxPayloadBytes = 0x36;            // 54 bytes per data chunk

        public const byte Cmd_Begin  = 0x01;
        public const byte Cmd_End    = 0x02;
        public const byte Cmd_Data   = 0x11;
        public const byte Cmd_Param  = 0x06;

        public const byte Mode_Static = 0x06;
        public const byte Mode_Custom = 0x14;

        // The 6x23 matrix below mirrors OpenRGB's RGBController_EVisionKeyboard.cpp
        // matrix map. Cells holding 0xFF correspond to OpenRGB's NA sentinel —
        // physical position not addressable. The LED index at each
        // populated cell is the wire-order slot (0..125) that the firmware
        // expects in the data-chunk payload.
        //
        // EVisionDevice reads this to derive synthetic visual placement
        // when building its layout. Without per-model key-name data
        // from OpenRGB (its driver labels every LED "Keyboard LED N"),
        // the matrix is the only positional information we have to work
        // with — Chromatics still surfaces every slot as a Custom_N LedId
        // so users can re-map keys to semantic ids on the Mapping tab.
        public const byte NA = 0xFF;
        public static readonly byte[,] Matrix = new byte[6, 23]
        {
            {  0, NA,  1,  2,  3,  4, NA,  5,  6,  7,  8, NA,  9, 10, 11, 12, 14, 15, 16, NA, NA, NA, NA },
            { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, NA, 32, 33, 34, NA, 35, 36, 37, 38, 39, 40, 41 },
            { 42, NA, 43, 44, 45, 46, NA, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62 },
            { 63, NA, 64, 65, 66, 67, NA, 68, 69, 70, 71, 72, 73, 74, 76, NA, NA, NA, NA, 80, 81, 82, NA },
            { 84, NA, 86, 87, 88, 89, NA, 90, NA, 91, 92, 93, 94, 95, 97, NA, NA, 99, NA,101,102,103,104 },
            {105,106,107, NA, NA, NA, NA,108, NA, NA, NA, NA,109,110,111,113,119,120,121,123, NA,124, NA },
        };

        // Begin packet — no payload, no checksum.
        // [0x00] = 0x04, [0x01] = 0x01, [0x02] = 0x00, [0x03] = 0x01.
        public static void BuildBegin(Span<byte> buf)
        {
            if (buf.Length < ReportLength) throw new ArgumentException("buffer too small", nameof(buf));
            buf.Clear();
            buf[0] = ReportPrefix;
            buf[1] = Cmd_Begin;
            buf[3] = Cmd_Begin;
        }

        // End packet — same shape as Begin but Cmd_End.
        public static void BuildEnd(Span<byte> buf)
        {
            if (buf.Length < ReportLength) throw new ArgumentException("buffer too small", nameof(buf));
            buf.Clear();
            buf[0] = ReportPrefix;
            buf[1] = Cmd_End;
            buf[3] = Cmd_End;
        }

        // One data chunk of the seven that make up a frame.
        // chunkIndex selects which 54-byte slice of the 378-byte
        // colour buffer is sent; chunkIndex is 0..6 inclusive.
        public static void BuildDataChunk(Span<byte> buf, ReadOnlySpan<byte> colorBuffer, int chunkIndex)
        {
            if (buf.Length < ReportLength) throw new ArgumentException("buffer too small", nameof(buf));
            if (colorBuffer.Length < ColorBufferSize) throw new ArgumentException("colour buffer too small", nameof(colorBuffer));
            if ((uint)chunkIndex >= ChunksPerFrame) throw new ArgumentOutOfRangeException(nameof(chunkIndex));

            buf.Clear();
            buf[0] = ReportPrefix;
            buf[3] = Cmd_Data;
            buf[4] = MaxPayloadBytes;

            int offset = chunkIndex * MaxPayloadBytes;
            buf[5] = (byte)(offset & 0xFF);
            buf[6] = (byte)((offset >> 8) & 0xFF);
            // [7] stays zero (unused header padding).

            colorBuffer.Slice(offset, MaxPayloadBytes).CopyTo(buf.Slice(8, MaxPayloadBytes));

            // 16-bit checksum over [3..63], little-endian at [1..2].
            ushort checksum = 0;
            for (int i = 3; i < ReportLength; i++)
                checksum += buf[i];
            buf[1] = (byte)(checksum & 0xFF);
            buf[2] = (byte)((checksum >> 8) & 0xFF);
        }

        // Mode-change packet. We never send this in the per-frame
        // path — the Begin/Data/End sequence overrides any stored
        // mode the moment it lands — but it's the way to leave the
        // device on a firmware effect when Chromatics goes away.
        // Currently unused; kept for parity with OpenRGB so the
        // shutdown path can hand the keyboard back to its on-device
        // mode if we ever decide to.
        public static void BuildSetMode(Span<byte> buf, byte mode, byte brightness, byte speed, byte direction,
                                        byte r, byte g, byte b)
        {
            if (buf.Length < ReportLength) throw new ArgumentException("buffer too small", nameof(buf));
            buf.Clear();
            buf[0] = ReportPrefix;
            buf[3] = Cmd_Param;
            buf[4] = 0x08;
            // [5] is parameter id 0 (mode) — already zero from Clear().
            buf[8]  = mode;
            buf[9]  = brightness;
            buf[10] = speed;
            buf[11] = direction;
            buf[12] = 0;        // random flag
            buf[13] = r;
            buf[14] = g;
            buf[15] = b;

            ushort checksum = 0;
            for (int i = 3; i < ReportLength; i++)
                checksum += buf[i];
            buf[1] = (byte)(checksum & 0xFF);
            buf[2] = (byte)((checksum >> 8) & 0xFF);
        }
    }
}
