using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices.EVision.Protocol;
using HidSharp;
using RGB.NET.Core;
using System;
using System.Threading;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Extensions.RGB.NET.Devices.EVision
{
    // Per-keyboard update queue. One frame is nine HID output reports:
    // Begin, seven 54-byte data chunks (126 LEDs * 3 bytes = 378 bytes),
    // and End. Each report is followed by a Read() so the firmware ACK
    // arrives before the next write — without that round-trip the
    // device drops packets mid-frame.
    //
    // The colour cache is the 378-byte flat RGB buffer the protocol
    // expects on the wire. Per-frame the queue patches changed LED
    // indices into the cache and skips the HID writes entirely if no
    // LED moved since the previous frame. This is the only mitigation
    // we have against the V1 firmware's flash-write-per-frame
    // behaviour: layers that paint a single colour stop generating any
    // wire traffic once they've settled.
    public class EVisionUpdateQueue : UpdateQueue
    {
        #region Properties & Fields

        private readonly EVisionClientDefinition _def;
        private readonly HidStream _stream;
        private readonly Lock _lock = new();
        private volatile bool _shuttingDown;
        private volatile bool _perDeviceDisable;

        private PerDeviceBrightnessCorrection _perDeviceBrightness;

        // Persistent 378-byte colour buffer in protocol wire order
        // (LED 0 R, LED 0 G, LED 0 B, LED 1 R, ...). Patched per
        // dirty LED, then sent in seven 54-byte chunks.
        private readonly byte[] _colorBuffer = new byte[EVisionKeyboardProtocol.ColorBufferSize];

        // ACK buffer reused across reads.
        private readonly byte[] _ackBuffer = new byte[EVisionKeyboardProtocol.ReportLength];

        #endregion

        #region Constructors

        public EVisionUpdateQueue(IDeviceUpdateTrigger trigger, EVisionClientDefinition def, HidStream stream)
            : base(trigger)
        {
            _def = def;
            _stream = stream;
        }

        #endregion

        #region Lifecycle

        public void BeginShutdown() => _shuttingDown = true;
        public void SetPerDeviceDisabled(bool disabled) => _perDeviceDisable = disabled;

        public void ResetCache()
        {
            lock (_lock)
            {
                Array.Clear(_colorBuffer, 0, _colorBuffer.Length);
            }
        }

        public void SetPerDeviceBrightness(PerDeviceBrightnessCorrection correction)
            => _perDeviceBrightness = correction;

        #endregion

        #region Update

        protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
        {
            lock (_lock)
            {
                if (_shuttingDown || _perDeviceDisable) return true;
                if (dataSet.IsEmpty) return true;

                try
                {
                    int globalPct = GlobalBrightnessCorrection.Instance.BrightnessPercent;
                    int perDevicePct = _perDeviceBrightness?.BrightnessPercent ?? 100;
                    double brightnessScale = (globalPct / 100.0) * (perDevicePct / 100.0);

                    bool anyChanged = false;
                    foreach (var (key, color) in dataSet)
                    {
                        int idx = ResolveLedIndex(key);
                        if (idx < 0 || idx >= EVisionKeyboardProtocol.TotalLeds) continue;

                        ToRgb255(color, brightnessScale, out byte r, out byte g, out byte b);
                        int off = idx * 3;
                        if (_colorBuffer[off] == r && _colorBuffer[off + 1] == g && _colorBuffer[off + 2] == b)
                            continue;

                        _colorBuffer[off] = r;
                        _colorBuffer[off + 1] = g;
                        _colorBuffer[off + 2] = b;
                        anyChanged = true;
                    }

                    if (!anyChanged) return true;

                    return SendFrame();
                }
                catch (Exception ex)
                {
                    EVisionRGBDeviceProvider.Instance?.Throw(ex);
                    return false;
                }
            }
        }

        // The wire ordering EVisionDevice exposes its LedIds in is the
        // same flat 0..125 sequence the firmware expects, so the LedId
        // → buffer mapping is identity. Custom1..Custom126 map directly
        // to wire slots 0..125.
        private static int ResolveLedIndex(object key)
        {
            if (key is LedId id) return LedIdToWireSlot(id);
            if (key is Led led) return LedIdToWireSlot(led.Id);
            return -1;
        }

        private static int LedIdToWireSlot(LedId id)
        {
            // Custom1 = 1 in the LedId enum, Custom126 is the highest
            // slot we expose. Map (Custom1..Custom126) → (0..125).
            int delta = (int)id - (int)LedId.Custom1;
            if (delta < 0 || delta >= EVisionKeyboardProtocol.TotalLeds) return -1;
            return delta;
        }

        #endregion

        #region HID I/O

        // Begin → 7 data chunks → End. Every write is followed by a
        // synchronous Read() because the firmware sends an ACK after
        // each packet and ignores the next write if the previous ACK
        // is still in the device-side queue.
        private bool SendFrame()
        {
            Span<byte> buf = stackalloc byte[EVisionKeyboardProtocol.ReportLength];

            EVisionKeyboardProtocol.BuildBegin(buf);
            if (!WriteAndAck(buf)) return false;

            for (int chunk = 0; chunk < EVisionKeyboardProtocol.ChunksPerFrame; chunk++)
            {
                EVisionKeyboardProtocol.BuildDataChunk(buf, _colorBuffer, chunk);
                if (!WriteAndAck(buf)) return false;
            }

            EVisionKeyboardProtocol.BuildEnd(buf);
            return WriteAndAck(buf);
        }

        private bool WriteAndAck(ReadOnlySpan<byte> buf)
        {
            byte[] arr = buf.ToArray();
            try
            {
                _stream.Write(arr);
                // ReadExactly because the firmware returns a fixed 64-byte
                // ACK per packet; partial reads would mean we'd accept the
                // next write before the device finished signalling.
                _stream.ReadExactly(_ackBuffer, 0, _ackBuffer.Length);
                return true;
            }
            catch (System.IO.IOException ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[EVision] {_def.Product}: HID write/ack failed ({ex.Message}); pausing queue.",
                    forwardToSentry: false);
                _shuttingDown = true;
                return false;
            }
            catch (TimeoutException)
            {
                // ACK timed out — almost always means another app stole
                // the interface mid-frame. Pause the queue and let the
                // provider's hot-plug reconcile clean up.
                _shuttingDown = true;
                return false;
            }
            catch (ObjectDisposedException)
            {
                _shuttingDown = true;
                return false;
            }
        }

        #endregion

        #region Helpers

        private static void ToRgb255(Color rgb, double brightnessScale, out byte r, out byte g, out byte b)
        {
            double scale = Math.Clamp(brightnessScale, 0.0, 1.0);
            r = (byte)Math.Round(Math.Clamp(rgb.R, 0.0, 1.0) * 255 * scale);
            g = (byte)Math.Round(Math.Clamp(rgb.G, 0.0, 1.0) * 255 * scale);
            b = (byte)Math.Round(Math.Clamp(rgb.B, 0.0, 1.0) * 255 * scale);
        }

        #endregion
    }
}
