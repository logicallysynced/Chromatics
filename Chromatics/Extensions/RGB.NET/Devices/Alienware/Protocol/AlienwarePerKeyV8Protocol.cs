using System;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol
{
    // V8 wire format for per-key external Alienware keyboards (Chicony,
    // VID 0x04f2). Covers the AW510K, AW920K, AW768, AW410K and any
    // other Chicony-made AlienFX keyboard with OutputReportByteLength==65.
    //
    // Reverse-engineered by T-Troll (alienfx-tools, MIT licensed). We
    // re-implement in managed code via HidSharp rather than P/Invoking
    // a native bridge, so there's no precompiled DLL to ship.
    //
    // Frame protocol (must run in order each paint frame):
    //
    //   1. Announce — feature report tells the device how many lights
    //      will follow in this batch.
    //   2. Data packets — write reports carrying up to four lights
    //      (15 bytes each) per 65-byte report. Send as many as needed
    //      to cover all dirty lights.
    //
    // Each frame's worth of data packets must follow one announce.
    // No explicit Loop / Commit / ExecuteColors call is required for V8 —
    // the firmware applies colours as soon as the data packets land.
    internal static class AlienwarePerKeyV8Protocol
    {
        public const int ReportLength = 65;
        public const byte ReportId = 0x01;

        // Maximum lights per data packet. 65-byte report minus 5-byte
        // header leaves 60 bytes for light blocks; each light block is
        // 15 bytes. (65 - 5) / 15 = 4.
        public const int MaxLightsPerPacket = 4;
        public const int LightBlockSize = 15;
        public const int DataHeaderSize = 5;

        // Static-colour opcode. T-Troll's `v8OpCodes` table also defines
        // pulse (0x82), morph (0x83), breathing (0x87), spectrum (0x88),
        // rainbow (0x84) — we only ship Color for the RGB.NET path.
        private const byte OpCodeColor = 0x81;

        // Build a 65-byte feature-report buffer that announces a colour
        // batch of `lightCount` lights. Sent via HidStream.SetFeature
        // BEFORE the first data packet of a frame. The firmware uses this
        // to size its incoming-buffer expectation.
        public static void BuildAnnounceBatch(Span<byte> buffer, byte lightCount)
        {
            EnsureLength(buffer);
            buffer.Clear();
            buffer[0] = ReportId;
            buffer[1] = 0x0e; // COMMV8_readyToColor[0]
            buffer[2] = lightCount;
            buffer[3] = 0x00;
            buffer[4] = 0x01;
        }

        // Build a 65-byte write-report buffer carrying up to four lights.
        // `batchCounter` increments per packet within a frame (1, 2, 3...).
        // `lights` is the list of (index, R, G, B) tuples for this packet —
        // up to MaxLightsPerPacket entries; trailing slots stay zero.
        public static void BuildDataPacket(
            Span<byte> buffer,
            byte batchCounter,
            ReadOnlySpan<(byte index, byte r, byte g, byte b)> lights)
        {
            EnsureLength(buffer);
            if (lights.Length > MaxLightsPerPacket)
                throw new ArgumentException($"V8 data packet holds at most {MaxLightsPerPacket} lights (got {lights.Length}).", nameof(lights));

            buffer.Clear();
            buffer[0] = ReportId;
            buffer[1] = 0x0e; // COMMV8_readyToColor[0]
            buffer[2] = 0x01;
            buffer[3] = 0x00;
            buffer[4] = batchCounter;

            int offset = DataHeaderSize;
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                buffer[offset + 0]  = l.index;
                buffer[offset + 1]  = OpCodeColor;
                buffer[offset + 2]  = 0;     // tempo (irrelevant for static)
                buffer[offset + 3]  = 0xa5;
                buffer[offset + 4]  = 0;     // time (irrelevant for static)
                buffer[offset + 5]  = 0x0a;
                buffer[offset + 6]  = l.r;   // primary RGB
                buffer[offset + 7]  = l.g;
                buffer[offset + 8]  = l.b;
                buffer[offset + 9]  = l.r;   // secondary RGB (same as primary for static)
                buffer[offset + 10] = l.g;
                buffer[offset + 11] = l.b;
                buffer[offset + 12] = 0x02;
                buffer[offset + 13] = 0;
                buffer[offset + 14] = 0;
                offset += LightBlockSize;
            }
        }

        private static void EnsureLength(Span<byte> buffer)
        {
            if (buffer.Length < ReportLength)
                throw new ArgumentException($"V8 buffer must be at least {ReportLength} bytes (got {buffer.Length}).");
        }
    }
}
