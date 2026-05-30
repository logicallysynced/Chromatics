using System;

namespace Chromatics.Extensions.RGB.NET.Devices.Redragon.Protocol
{
    // Wire format for the 9 Redragon mice OpenRGB drives via the shared
    // RedragonMouseController. All buffers are 16 bytes total; byte 0 is
    // the HID report id (0x02), bytes 1-7 are the command header, bytes
    // 8-15 are the payload.
    //
    // Three commands cover everything Chromatics needs:
    //
    //   Address-write (0xF3) — set N payload bytes at a 16-bit address.
    //     [0x02, 0xF3, addrLo, addrHi, len, 0, 0, 0, payload[0..len]...]
    //   Apply/commit (0xF1)  — flush staged writes to the LED hardware.
    //     [0x02, 0xF1, 0x02, 0x04, 0, ..., 0]
    //
    // Three addresses cover the LED state machine:
    //
    //   0x002C  — active profile slot (1 byte: 0..4). Init writes 0x00
    //             so the rest of our writes land in the same profile slot
    //             across reboots.
    //   0x044C  — effect mode block: [enable, speed, mode_byte].
    //             Mode=0x02 is the firmware's Static colour mode — that's
    //             the one we set at init so subsequent 0x0449 writes paint
    //             a real RGB triplet rather than restart a hardware
    //             animation.
    //   0x0449  — current colour: [R, G, B].
    //
    // Per-frame, all we send is an address-write to 0x0449 followed by an
    // apply. Two HID feature reports per dirty colour change.
    internal static class RedragonMouseProtocol
    {
        public const int ReportLength = 16;
        public const byte ReportId    = 0x02;

        public const byte Cmd_WriteAddress = 0xF3;
        public const byte Cmd_Apply        = 0xF1;

        public const ushort Addr_Profile = 0x002C;
        public const ushort Addr_Mode    = 0x044C;
        public const ushort Addr_Color   = 0x0449;

        // Firmware effect mode byte values. Static is the only one
        // Chromatics drives directly; the others are useful as a "fallback
        // when the user has Chromatics disabled but the mouse should still
        // light up" pattern, but the current scope is just Static.
        public const byte Mode_Wave             = 0x00;
        public const byte Mode_RandomBreathing  = 0x01;
        public const byte Mode_Static           = 0x02;
        public const byte Mode_Breathing        = 0x04;
        public const byte Mode_Rainbow          = 0x08;
        public const byte Mode_Flashing         = 0x10;

        // Build an address-write command into the buffer.
        // buf must be ReportLength bytes long. payload may be 0..8 bytes.
        public static void BuildAddressWrite(Span<byte> buf, ushort address, ReadOnlySpan<byte> payload)
        {
            if (buf.Length < ReportLength) throw new ArgumentException("buffer too small", nameof(buf));
            if (payload.Length > 8) throw new ArgumentException("payload > 8 bytes", nameof(payload));

            buf.Clear();
            buf[0] = ReportId;
            buf[1] = Cmd_WriteAddress;
            buf[2] = (byte)(address & 0xFF);          // low byte first — LE
            buf[3] = (byte)((address >> 8) & 0xFF);
            buf[4] = (byte)payload.Length;
            // bytes 5-7 stay zero
            if (payload.Length > 0)
                payload.CopyTo(buf.Slice(8, payload.Length));
        }

        // Build the commit/apply command. No address, no payload — the
        // firmware reads the staged register state and pushes it to the
        // LEDs. Required after any of the address writes above.
        public static void BuildApply(Span<byte> buf)
        {
            if (buf.Length < ReportLength) throw new ArgumentException("buffer too small", nameof(buf));
            buf.Clear();
            buf[0] = ReportId;
            buf[1] = Cmd_Apply;
            buf[2] = 0x02;
            buf[3] = 0x04;
            // bytes 4-15 stay zero
        }

        // Convenience: write the LED colour. Caller still has to send the
        // apply report afterwards to push it out of the firmware's stage
        // buffer.
        public static void BuildSetColor(Span<byte> buf, byte r, byte g, byte b)
        {
            Span<byte> payload = stackalloc byte[3] { r, g, b };
            BuildAddressWrite(buf, Addr_Color, payload);
        }

        // Convenience: set the effect mode block. Used once at init so the
        // firmware leaves Static mode selected and our colour writes land
        // on real LEDs instead of advancing a hardware animation frame.
        public static void BuildSetMode(Span<byte> buf, byte mode, byte speed = 0)
        {
            Span<byte> payload = stackalloc byte[3] { 0x01, speed, mode };
            BuildAddressWrite(buf, Addr_Mode, payload);
        }

        // Convenience: select profile slot. Init pins this to 0 so we
        // don't drift across hardware profiles on subsequent launches.
        public static void BuildSetProfile(Span<byte> buf, byte profileIndex)
        {
            Span<byte> payload = stackalloc byte[1] { profileIndex };
            BuildAddressWrite(buf, Addr_Profile, payload);
        }
    }
}
