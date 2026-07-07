using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol
{
    // extControl v2 frame encoder. One UDP datagram carries every panel's
    // colour for one tick.
    //
    // Wire format (all big-endian):
    //   uint16  panelCount
    //   repeated panelCount times:
    //     uint16  panelId
    //     uint8   R
    //     uint8   G
    //     uint8   B
    //     uint8   W          (white channel, always 0 - RGB panels)
    //     uint16  transitionTime  (in 100ms units; 1 = 100ms)
    //
    // The v1 format used single-byte counts and a different field order;
    // every controller with firmware new enough to matter speaks v2, which
    // is what the streaming handshake negotiates (see NanoleafRestClient).
    public static class NanoleafStreamProtocol
    {
        // transitionTime is expressed in 100ms units on the wire. We pass a
        // 1-unit (100ms) transition so consecutive frames blend rather than
        // stepping, matching the Hue "duration" smoothing. At 20Hz the next
        // frame arrives in 50ms, so the eased motion stays ahead of the eye
        // without visible lag.
        public const ushort DefaultTransition = 1;

        public static byte[] EncodeFrame(IReadOnlyList<(int panelId, byte r, byte g, byte b)> panels, ushort transition = DefaultTransition)
        {
            int count = panels.Count;
            var buffer = new byte[2 + count * 8];
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), (ushort)count);

            int offset = 2;
            for (int i = 0; i < count; i++)
            {
                var (panelId, r, g, b) = panels[i];
                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset, 2), (ushort)panelId);
                buffer[offset + 2] = r;
                buffer[offset + 3] = g;
                buffer[offset + 4] = b;
                buffer[offset + 5] = 0; // white channel
                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 6, 2), transition);
                offset += 8;
            }

            return buffer;
        }
    }
}
