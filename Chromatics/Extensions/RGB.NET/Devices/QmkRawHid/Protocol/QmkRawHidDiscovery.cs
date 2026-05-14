using Chromatics.Core;
using Chromatics.Enums;
using HidSharp;
using HidSharp.Reports;
using System;
using System.Collections.Generic;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol
{
    // Enumerates the host's HID devices, picks out the ones exposing the
    // QMK Raw HID interface (usage page 0xFF60, usage 0x61), and
    // classifies each by which protocol(s) the firmware actually
    // implements via the handshake probe.
    internal static class QmkRawHidDiscovery
    {
        public enum ProtocolSupport
        {
            None,        // Discovered but neither VIA nor OpenRGB-QMK responded — skip.
            ViaOnly,     // Tier 1: drive base hue/sat/val via VIA's RGB matrix sub-commands.
            OpenRgbQmk,  // Tier 2: per-key control via OpenRGB-QMK direct mode.
        }

        public readonly struct Candidate
        {
            public readonly HidDevice Hid;
            public readonly ProtocolSupport Protocol;
            public readonly int LedCount;        // Populated when Protocol == OpenRgbQmk.
            public readonly byte MatrixColumns;  // Optional hint from OpenRGB-QMK GetLedMatrixSize.
            public readonly byte MatrixRows;
            public readonly string FirmwareDeviceName;

            public Candidate(HidDevice hid, ProtocolSupport protocol, int ledCount, byte columns, byte rows, string firmwareDeviceName)
            {
                Hid = hid; Protocol = protocol; LedCount = ledCount;
                MatrixColumns = columns; MatrixRows = rows;
                FirmwareDeviceName = firmwareDeviceName ?? string.Empty;
            }
        }

        // Walks HidSharp's enumeration, filters to interfaces whose
        // report descriptor advertises the QMK Raw HID usage, and runs
        // the handshake on each. Output is ordered by VID:PID for
        // stable adoption-dialog presentation across launches.
        public static IReadOnlyList<Candidate> Discover()
        {
            var results = new List<Candidate>();
            HidDevice[] all;
            try { all = DeviceList.Local.GetHidDevices() as HidDevice[] ?? new List<HidDevice>(DeviceList.Local.GetHidDevices()).ToArray(); }
            catch (Exception ex)
            {
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] HID enumeration failed: {ex.Message}",
                    forwardToSentry: false);
                return results;
            }

            Logger.WriteConsole(LoggerTypes.Devices,
                $"[QMK] Enumerated {all.Length} HID device(s) on the USB bus; scanning for the Raw HID interface (usage page 0x{QmkRawHidConstants.RawHidUsagePage:X4}, usage 0x{QmkRawHidConstants.RawHidUsage:X2})...");

            int rawHidCandidates = 0;
            int openFailures = 0;
            int handshakeMisses = 0;

            foreach (HidDevice hid in all)
            {
                if (!ExposesRawHidUsage(hid)) continue;
                rawHidCandidates++;

                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] Candidate: VID=0x{hid.VendorID:X4} PID=0x{hid.ProductID:X4} ({SafeProductName(hid)} / {SafeManufacturer(hid)})");

                if (!TryHandshake(hid, out var protocol, out int ledCount, out byte cols, out byte rows, out string fwName, out string failureReason))
                {
                    openFailures++;
                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"[QMK]   handshake skipped — {failureReason}",
                        forwardToSentry: false);
                    continue;
                }

                if (protocol == ProtocolSupport.None)
                {
                    handshakeMisses++;
                    Logger.WriteConsole(LoggerTypes.Devices,
                        "[QMK]   neither VIA nor OpenRGB-QMK responded; firmware probably doesn't have Raw HID enabled.");
                    continue;
                }

                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK]   handshake OK — protocol: {protocol}, LEDs: {ledCount}, matrix: {cols}x{rows}");
                results.Add(new Candidate(hid, protocol, ledCount, cols, rows, fwName));
            }

            results.Sort((a, b) =>
            {
                int v = a.Hid.VendorID.CompareTo(b.Hid.VendorID);
                if (v != 0) return v;
                return a.Hid.ProductID.CompareTo(b.Hid.ProductID);
            });

            Logger.WriteConsole(LoggerTypes.Devices,
                $"[QMK] Discovery done: {all.Length} HID devices total, {rawHidCandidates} with Raw HID interface, " +
                $"{openFailures} could not be opened, {handshakeMisses} did not respond to handshake, {results.Count} usable.");

            return results;
        }

        private static string SafeManufacturer(HidDevice hid)
        { try { return hid.GetManufacturer() ?? ""; } catch { return "?"; } }
        private static string SafeProductName(HidDevice hid)
        { try { return hid.GetProductName() ?? ""; } catch { return "?"; } }

        // Returns true if any of the HidDevice's top-level collections
        // declares usage page 0xFF60, usage 0x61. Multi-interface USB
        // devices expose each interface as a separate HidDevice, so
        // this check naturally selects only the Raw HID interface and
        // leaves the keyboard/consumer interfaces alone.
        private static bool ExposesRawHidUsage(HidDevice hid)
        {
            ReportDescriptor desc;
            try { desc = hid.GetReportDescriptor(); }
            catch { return false; }

            foreach (var item in desc.DeviceItems)
            {
                foreach (uint usage in item.Usages.GetAllValues())
                {
                    ushort page  = (ushort)(usage >> 16);
                    ushort usageLo = (ushort)(usage & 0xFFFF);
                    if (page == QmkRawHidConstants.RawHidUsagePage && usageLo == QmkRawHidConstants.RawHidUsage)
                        return true;
                }
            }
            return false;
        }

        // Open the candidate's HID stream and try VIA first, then
        // OpenRGB-QMK. OpenRGB wins when both respond — it's the
        // strictly more capable protocol. Stream is disposed before
        // we return; the provider will reopen it for the device's
        // permanent UpdateQueue.
        private static bool TryHandshake(HidDevice hid, out ProtocolSupport protocol, out int ledCount, out byte cols, out byte rows, out string firmwareDeviceName, out string failureReason)
        {
            protocol = ProtocolSupport.None;
            ledCount = 0; cols = 0; rows = 0; firmwareDeviceName = string.Empty;
            failureReason = string.Empty;

            HidStream stream;
            try
            {
                if (!hid.TryOpen(out stream))
                {
                    failureReason = "could not open the Raw HID interface (likely held exclusively by another app — close VIA / Vial / OpenRGB and try again)";
                    return false;
                }
            }
            catch (Exception ex)
            {
                failureReason = $"open threw: {ex.Message}";
                return false;
            }

            try
            {
                stream.ReadTimeout  = QmkRawHidConstants.ResponseTimeoutMs;
                stream.WriteTimeout = QmkRawHidConstants.ResponseTimeoutMs;

                Span<byte> outBuf = stackalloc byte[QmkRawHidConstants.OutputReportBytes];
                Span<byte> payload = outBuf.Slice(1, QmkRawHidConstants.ReportPayloadBytes);
                byte[] inBuf = new byte[QmkRawHidConstants.ReportPayloadBytes + 1];

                bool viaOk = false;
                ViaProtocol.BuildGetProtocolVersion(payload);
                if (SendAndReceive(stream, outBuf, inBuf))
                {
                    if (ViaProtocol.TryParseProtocolVersion(StripReportId(inBuf)) > 0)
                        viaOk = true;
                }

                bool openRgbOk = false;
                OpenRgbQmkProtocol.BuildGetProtocolVersion(payload);
                if (SendAndReceive(stream, outBuf, inBuf))
                {
                    if (OpenRgbQmkProtocol.TryParseProtocolVersion(StripReportId(inBuf)) > 0)
                        openRgbOk = true;
                }

                if (openRgbOk)
                {
                    OpenRgbQmkProtocol.BuildGetDeviceInfo(payload);
                    if (SendAndReceive(stream, outBuf, inBuf) &&
                        OpenRgbQmkProtocol.TryParseDeviceInfo(StripReportId(inBuf),
                            out ushort count, out _, out _, out string name))
                    {
                        ledCount = count;
                        firmwareDeviceName = name;
                    }

                    OpenRgbQmkProtocol.BuildGetLedMatrixSize(payload);
                    if (SendAndReceive(stream, outBuf, inBuf))
                    {
                        OpenRgbQmkProtocol.TryParseLedMatrixSize(StripReportId(inBuf), out cols, out rows);
                    }

                    protocol = ProtocolSupport.OpenRgbQmk;
                    return true;
                }

                if (viaOk)
                {
                    protocol = ProtocolSupport.ViaOnly;
                    return true;
                }

                return true;
            }
            finally
            {
                try { stream.Dispose(); } catch { /* ignore */ }
            }
        }

        private static bool SendAndReceive(HidStream stream, ReadOnlySpan<byte> outBuf, byte[] inBuf)
        {
            try
            {
                stream.Write(outBuf.ToArray());
                int n = stream.Read(inBuf, 0, inBuf.Length);
                return n > 0;
            }
            catch { return false; }
        }

        // Strips the leading report-id byte HidSharp prepends to inputs
        // on Windows; QMK Raw HID always uses report id 0 so the strip
        // is unconditional.
        private static ReadOnlySpan<byte> StripReportId(byte[] inBuf) =>
            new ReadOnlySpan<byte>(inBuf, 1, inBuf.Length - 1);
    }
}
