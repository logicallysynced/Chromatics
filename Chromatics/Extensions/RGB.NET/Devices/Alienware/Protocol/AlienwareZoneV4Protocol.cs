using System;

namespace Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol
{
    // V4 wire format for AlienFX zone-based chassis (Aurora R7-R14
    // desktops, m15R1-R6 zone laptops, m17R1, Dell G7/G5/G5SE).
    // VID 0x187C, output reports of 34 bytes via HidStream.Write
    // (HidD_SetOutputReport on the native side).
    //
    // Reverse-engineered by T-Troll (alienfx-tools, MIT licensed).
    // V4 light IDs are opaque per-device integers — different chassis
    // expose different counts and physical-zone mappings. We expose
    // them as flat Custom1..N LEDs and let users position them via
    // the Mapping tab.
    //
    // Frame protocol (must run in order each paint frame):
    //
    //   1. Remove existing sequence + Start new sequence (these are
    //      collectively the "Reset" pair).
    //   2. SetColor for each light or batch of lights painting the
    //      same colour.
    //   3. Commit (control type "finish and play").
    //
    // Status polling between commands is required by T-Troll's
    // reference flow but in practice the firmware tolerates
    // back-to-back writes for static colours; we skip the polling
    // dance and rely on HID interrupt-OUT pacing.
    internal static class AlienwareZoneV4Protocol
    {
        public const int ReportLength = 34;
        public const byte ReportId = 0x00;

        // V4 device-status bytes returned in buffer[2] of an input report.
        public const byte StatusReady       = 0x21;
        public const byte StatusBusy        = 0x22;
        public const byte StatusWaitColor   = 0x23;
        public const byte StatusWaitUpdate  = 0x24;
        public const byte StatusWasOn       = 0x26;

        // Control-command sub-types (byte 4 of COMMV4_control).
        public const byte ControlStartNew      = 0x01;
        public const byte ControlFinishSave    = 0x02;
        public const byte ControlFinishPlay    = 0x03; // commit / "execute colors"
        public const byte ControlRemove        = 0x04;
        public const byte ControlPlay          = 0x05;
        public const byte ControlSetDefault    = 0x06;
        public const byte ControlSetStartup    = 0x07;

        // Build a control command (Remove / Start / Commit etc.).
        // COMMV4_control template: {0x03, 0x21, 0x00, controlType, 0x00, 0xFF}
        // at offsets 1..6, with byte[0] = 0 (report ID).
        public static void BuildControl(Span<byte> buffer, byte controlType)
        {
            EnsureLength(buffer);
            buffer.Clear();
            buffer[0] = ReportId;
            buffer[1] = 0x03;
            buffer[2] = 0x21;
            buffer[3] = 0x00;
            buffer[4] = controlType;
            buffer[5] = 0x00;
            buffer[6] = 0xFF;
        }

        // Convenience: the two-step Reset pair the device expects before
        // the first SetColor of a frame. Caller sends Remove first, then
        // Start.
        public static void BuildReset_Remove(Span<byte> buffer) => BuildControl(buffer, ControlRemove);
        public static void BuildReset_Start (Span<byte> buffer) => BuildControl(buffer, ControlStartNew);

        // Commit the frame (firmware applies the freshly-set colours).
        public static void BuildCommit(Span<byte> buffer) => BuildControl(buffer, ControlFinishPlay);

        // Set one or more lights to the same RGB colour. Batches lights
        // that share a colour into a single HID write. T-Troll's
        // COMMV4_setOneColor template: {0x03, 0x27} at offsets 1..2,
        // then payload at offset 3+: {R, G, B, 0x00, count, id0, id1,
        // ...}.
        //
        // `lightIds` is the opaque per-device integer light id list.
        // The protocol packs them as one byte each starting at offset 8.
        public static void BuildSetColor(Span<byte> buffer, byte r, byte g, byte b, ReadOnlySpan<byte> lightIds)
        {
            EnsureLength(buffer);
            if (lightIds.Length == 0)
                throw new ArgumentException("V4 SetColor requires at least one light id.", nameof(lightIds));
            // 8 bytes header (incl. count) + N bytes for ids; protocol caps
            // at the buffer's remaining capacity.
            int maxIds = ReportLength - 8;
            if (lightIds.Length > maxIds)
                throw new ArgumentException($"V4 SetColor packs at most {maxIds} light ids per command (got {lightIds.Length}).", nameof(lightIds));

            buffer.Clear();
            buffer[0] = ReportId;
            buffer[1] = 0x03;
            buffer[2] = 0x27;
            buffer[3] = r;
            buffer[4] = g;
            buffer[5] = b;
            buffer[6] = 0x00;
            buffer[7] = (byte)lightIds.Length;
            for (int i = 0; i < lightIds.Length; i++)
                buffer[8 + i] = lightIds[i];
        }

        // Brightness command. Operates per-frame, separate from RGB.
        // Wire format: {0x03, 0x26} at offsets 1..2; payload at offset 3+:
        // {(0x64 - brightnessPercent), 0x00, count, id0, id1, ...}.
        // NOTE: brightness is INVERTED — 0 = full bright, 100 = off.
        public static void BuildBrightness(Span<byte> buffer, int brightnessPercent, ReadOnlySpan<byte> lightIds)
        {
            EnsureLength(buffer);
            int maxIds = ReportLength - 6;
            if (lightIds.Length > maxIds)
                throw new ArgumentException($"V4 SetBrightness packs at most {maxIds} light ids per command (got {lightIds.Length}).", nameof(lightIds));

            int pct = Math.Clamp(brightnessPercent, 0, 100);
            byte invertedPct = (byte)(0x64 - pct);

            buffer.Clear();
            buffer[0] = ReportId;
            buffer[1] = 0x03;
            buffer[2] = 0x26;
            buffer[3] = invertedPct;
            buffer[4] = 0x00;
            buffer[5] = (byte)lightIds.Length;
            for (int i = 0; i < lightIds.Length; i++)
                buffer[6 + i] = lightIds[i];
        }

        // Status query — caller reads the device's input report and
        // inspects buffer[2] for one of the Status* constants.
        public static void BuildStatusQuery(Span<byte> buffer)
        {
            // V4 status is read via HidD_GetInputReport with no preceding
            // write — caller just reads. We expose this stub for symmetry
            // with the other protocol files; it isn't actually sent.
            EnsureLength(buffer);
            buffer.Clear();
            buffer[0] = ReportId;
        }

        private static void EnsureLength(Span<byte> buffer)
        {
            if (buffer.Length < ReportLength)
                throw new ArgumentException($"V4 buffer must be at least {ReportLength} bytes (got {buffer.Length}).");
        }
    }
}
