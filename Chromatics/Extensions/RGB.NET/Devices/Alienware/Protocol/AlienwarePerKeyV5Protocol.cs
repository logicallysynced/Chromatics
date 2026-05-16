using System;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol
{
    // V5 wire format for per-key Alienware notebook keyboards (Darfon,
    // VID 0x0d62). Covers the Area51m-R2, x17R2, m15R3 / R4 / R5, m15R6,
    // x15R2, m17R3 and any other notebook with the standard AlienFX V5
    // HID interface (Usage 0xcc, FeatureReportByteLength populated).
    //
    // Reverse-engineered by T-Troll (alienfx-tools, MIT licensed). Re-
    // implemented in managed code via HidSharp's HidStream.SetFeature.
    //
    // Frame protocol (must run in order each paint frame):
    //
    //   1. One or more colour-block reports — each carries up to 15
    //      lights packed as 4-byte (id+1, R, G, B) tuples.
    //   2. Loop report — marks the end of the colour-block sequence.
    //   3. Update report — commits the frame; firmware applies the
    //      colours after this lands.
    //
    // V5 uses HID feature reports (HidD_SetFeature on the native side),
    // not write reports. HidSharp exposes this via HidStream.SetFeature.
    internal static class AlienwarePerKeyV5Protocol
    {
        // Default report length the SDK uses for V5. Some boards report
        // a larger FeatureReportByteLength via HidP_GetCaps; callers can
        // size the buffer to the device's reported value but 65 is the
        // documented baseline and matches every confirmed-working board.
        public const int ReportLength = 65;
        public const byte ReportId = 0xCC;

        // Maximum lights packed into one colour-set report. Each light is
        // 4 bytes (id+1, R, G, B); the report header eats 4 bytes; the
        // remaining 61 bytes hold (61 / 4) = 15 lights with 1 trailing byte
        // padding.
        public const int MaxLightsPerReport = 15;
        public const int LightBlockSize = 4;
        public const int ColorReportHeaderSize = 4;

        // Build the colour-set report carrying up to 15 lights. Sent via
        // HidStream.SetFeature. Trailing buffer bytes stay zero.
        public static void BuildColorSet(
            Span<byte> buffer,
            ReadOnlySpan<(byte hardwareId, byte r, byte g, byte b)> lights)
        {
            EnsureLength(buffer);
            if (lights.Length > MaxLightsPerReport)
                throw new ArgumentException($"V5 colour-set report holds at most {MaxLightsPerReport} lights (got {lights.Length}).", nameof(lights));

            buffer.Clear();
            buffer[0] = ReportId;
            buffer[1] = 0x8C; // COMMV5_colorSet[0]
            buffer[2] = 0x02; // COMMV5_colorSet[1]
            buffer[3] = 0x00;

            int offset = ColorReportHeaderSize;
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                // Light index is stored 1-based in the V5 wire format.
                // The HID firmware treats id 0 as "no light" and skips it,
                // so we shift everything up by one.
                buffer[offset + 0] = (byte)(l.hardwareId + 1);
                buffer[offset + 1] = l.r;
                buffer[offset + 2] = l.g;
                buffer[offset + 3] = l.b;
                offset += LightBlockSize;
            }
        }

        // Loop report — marks the end of a sequence of colour-set reports.
        // Required between the last SetColor and the Update; without it
        // the firmware silently drops the frame.
        public static void BuildLoop(Span<byte> buffer)
        {
            EnsureLength(buffer);
            buffer.Clear();
            buffer[0] = ReportId;
            buffer[1] = 0x8C; // COMMV5_loop[0]
            buffer[2] = 0x13; // COMMV5_loop[1]
        }

        // Update report — commits the frame. Sent once after the Loop
        // marker; firmware applies the freshly-set colours when it lands.
        public static void BuildUpdate(Span<byte> buffer)
        {
            EnsureLength(buffer);
            buffer.Clear();
            buffer[0] = ReportId;
            buffer[1] = 0x8B; // COMMV5_update[0]
            buffer[2] = 0x01;
            buffer[3] = 0xFF;
        }

        private static void EnsureLength(Span<byte> buffer)
        {
            if (buffer.Length < ReportLength)
                throw new ArgumentException($"V5 buffer must be at least {ReportLength} bytes (got {buffer.Length}).");
        }
    }
}
