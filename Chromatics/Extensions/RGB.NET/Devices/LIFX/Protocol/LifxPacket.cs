using System;
using System.Buffers.Binary;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol
{
    // LIFX LAN binary packet codec.
    //
    // Header is 36 bytes split into three blocks (frame=8, frame address=16,
    // protocol header=12). All numeric fields little-endian.
    //
    //   Frame (bytes 0..7):
    //     0..1   size      uint16   total packet size including header + payload
    //     2..3   protocol  uint16   bits 0..11 = protocol (=1024); bit 12 = addressable (=1);
    //                                bit 13 = tagged; bits 14..15 = origin (=0)
    //     4..7   source    uint32   client identifier (avoid 0/1)
    //
    //   Frame Address (bytes 8..23):
    //     8..15   target        8 bytes  device serial (MAC), zero-padded; 0x00..0x00 = broadcast
    //     16..21  reserved      6 bytes  zeros
    //     22      flags         1 byte   bit 0 = res_required, bit 1 = ack_required, bits 2..7 reserved
    //     23      sequence      uint8
    //
    //   Protocol Header (bytes 24..35):
    //     24..31  reserved   8 bytes  zeros
    //     32..33  type       uint16   message type (see LifxMessageTypes)
    //     34..35  reserved   2 bytes  zeros
    internal static class LifxPacket
    {
        public const int HeaderSize = 36;
        private const ushort ProtocolNumber = 1024;

        // Encodes a LIFX message into a freshly-allocated byte[]. Payload may be
        // empty for query messages (e.g. GetService).
        public static byte[] Build(
            ushort messageType,
            byte[] target,
            uint source,
            byte sequence,
            ReadOnlySpan<byte> payload,
            bool tagged = false,
            bool ackRequired = false,
            bool resRequired = false)
        {
            int total = HeaderSize + payload.Length;
            byte[] buf = new byte[total];

            // Frame
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(0, 2), (ushort)total);

            ushort protoField = ProtocolNumber & 0x0FFF;
            protoField |= 1 << 12;             // addressable
            if (tagged) protoField |= 1 << 13; // tagged
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(2, 2), protoField);

            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), source);

            // Frame address
            if (target == null || target.Length == 0)
            {
                // broadcast — leave zero
            }
            else
            {
                int copy = Math.Min(8, target.Length);
                Array.Copy(target, 0, buf, 8, copy);
            }

            byte flags = 0;
            if (resRequired) flags |= 0x01;
            if (ackRequired) flags |= 0x02;
            buf[22] = flags;
            buf[23] = sequence;

            // Protocol header
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(32, 2), messageType);

            // Payload
            if (payload.Length > 0)
                payload.CopyTo(buf.AsSpan(HeaderSize));

            return buf;
        }

        // Parses the header out of a received UDP datagram. Returns false if
        // the buffer is shorter than the header or if the protocol field is
        // wrong. Caller still has to validate the message type and payload
        // length matches what they expect.
        public static bool TryReadHeader(
            ReadOnlySpan<byte> buf,
            out LifxHeader header)
        {
            header = default;
            if (buf.Length < HeaderSize) return false;

            ushort size = BinaryPrimitives.ReadUInt16LittleEndian(buf[..2]);
            ushort protoField = BinaryPrimitives.ReadUInt16LittleEndian(buf.Slice(2, 2));
            ushort proto = (ushort)(protoField & 0x0FFF);
            if (proto != ProtocolNumber) return false;

            uint source = BinaryPrimitives.ReadUInt32LittleEndian(buf.Slice(4, 4));

            byte[] target = new byte[8];
            buf.Slice(8, 8).CopyTo(target);

            byte flags = buf[22];
            byte sequence = buf[23];

            ushort messageType = BinaryPrimitives.ReadUInt16LittleEndian(buf.Slice(32, 2));

            header = new LifxHeader
            {
                Size = size,
                Source = source,
                Target = target,
                ResRequired = (flags & 0x01) != 0,
                AckRequired = (flags & 0x02) != 0,
                Sequence = sequence,
                MessageType = messageType,
            };
            return true;
        }

        public static ReadOnlySpan<byte> Payload(ReadOnlySpan<byte> buf)
        {
            return buf.Length <= HeaderSize ? ReadOnlySpan<byte>.Empty : buf[HeaderSize..];
        }
    }

    internal struct LifxHeader
    {
        public ushort Size;
        public uint Source;
        public byte[] Target;       // 8 bytes; first 6 are MAC, last 2 are zero
        public bool ResRequired;
        public bool AckRequired;
        public byte Sequence;
        public ushort MessageType;

        // First 6 bytes of Target as a colon-separated MAC string. Used as
        // the canonical device identity across discovery / persistence.
        public string TargetMac => MacFromTarget(Target);

        public static string MacFromTarget(byte[] target)
        {
            if (target == null || target.Length < 6) return "";
            return $"{target[0]:X2}:{target[1]:X2}:{target[2]:X2}:{target[3]:X2}:{target[4]:X2}:{target[5]:X2}";
        }

        public static byte[] TargetFromMac(string mac)
        {
            byte[] target = new byte[8];
            if (string.IsNullOrEmpty(mac)) return target;
            string[] parts = mac.Split(':', '-');
            int n = Math.Min(6, parts.Length);
            for (int i = 0; i < n; i++)
                if (byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out var b))
                    target[i] = b;
            return target;
        }
    }
}
