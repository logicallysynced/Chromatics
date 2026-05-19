using Chromatics.Core;
using Chromatics.Enums;
using HidSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            OpenRgbQmk,  // Tier 2: per-LED control via OpenRGB-QMK direct mode.
        }

        public readonly struct Candidate
        {
            public readonly HidDevice Hid;
            public readonly ProtocolSupport Protocol;
            public readonly int LedCount;
            public readonly byte MatrixColumns;
            public readonly byte MatrixRows;
            public readonly string FirmwareDeviceName;
            public readonly bool HasKeyboardSibling;
            public readonly int InputReportByteLength;
            public readonly int OutputReportByteLength;

            public Candidate(HidDevice hid, ProtocolSupport protocol, int ledCount, byte columns, byte rows, string firmwareDeviceName, bool hasKeyboardSibling, int inputReportByteLength, int outputReportByteLength)
            {
                Hid = hid; Protocol = protocol; LedCount = ledCount;
                MatrixColumns = columns; MatrixRows = rows;
                FirmwareDeviceName = firmwareDeviceName ?? string.Empty;
                HasKeyboardSibling = hasKeyboardSibling;
                InputReportByteLength = inputReportByteLength;
                OutputReportByteLength = outputReportByteLength;
            }
        }

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
                if (!TryGetHidCaps(hid.DevicePath, out var caps)) continue;
                if (caps.UsagePage != QmkRawHidConstants.RawHidUsagePage) continue;
                if (caps.Usage != QmkRawHidConstants.RawHidUsage) continue;
                rawHidCandidates++;

                int inLen = caps.InputReportByteLength;
                int outLen = caps.OutputReportByteLength;
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK] Candidate: VID=0x{hid.VendorID:X4} PID=0x{hid.ProductID:X4} ({SafeProductName(hid)} / {SafeManufacturer(hid)}); report sizes in={inLen}, out={outLen}");

                if (!TryHandshake(hid, inLen, outLen, out var protocol, out int ledCount, out byte cols, out byte rows, out string fwName, out string failureReason))
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

                bool hasKeyboardSibling = HasKeyboardInterface(all, hid.VendorID, hid.ProductID);
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK]   handshake OK — protocol: {protocol}, LEDs: {ledCount}, matrix: {cols}x{rows}, keyboardSibling: {hasKeyboardSibling}");
                results.Add(new Candidate(hid, protocol, ledCount, cols, rows, fwName, hasKeyboardSibling, inLen, outLen));
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

        private const ushort GenericDesktopUsagePage = 0x01;
        private const ushort KeyboardUsage           = 0x06;
        private static bool HasKeyboardInterface(HidDevice[] all, int vid, int pid)
        {
            foreach (HidDevice sibling in all)
            {
                if (sibling.VendorID != vid || sibling.ProductID != pid) continue;
                if (TryGetHidCaps(sibling.DevicePath, out var caps))
                {
                    if (caps.UsagePage == GenericDesktopUsagePage && caps.Usage == KeyboardUsage)
                        return true;
                }
            }
            return false;
        }

        // Win32 HIDP_CAPS layout. We bypass HidSharp.GetReportDescriptor on
        // Windows because Windows doesn't expose the raw descriptor bytes —
        // HidSharp reconstructs them from preparsed data and throws
        // NotSupportedException on QMK firmwares whose descriptors include
        // items it doesn't recognise (Keychron C3 Pro 8K with OpenRGB-QMK
        // confirmed). The top-level Usage + UsagePage and the report-size
        // fields we need are all in HIDP_CAPS, so query that directly.
        [StructLayout(LayoutKind.Sequential)]
        private struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFileW(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool HidD_GetPreparsedData(IntPtr hidDeviceObject, out IntPtr preparsedData);

        [DllImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);

        private const uint FILE_SHARE_READ  = 0x1;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint OPEN_EXISTING    = 3;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private const int HIDP_STATUS_SUCCESS = 0x00110000;

        private static bool TryGetHidCaps(string devicePath, out HIDP_CAPS caps)
        {
            caps = default;
            if (string.IsNullOrEmpty(devicePath)) return false;

            IntPtr handle = CreateFileW(
                devicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handle == INVALID_HANDLE_VALUE) return false;

            try
            {
                if (!HidD_GetPreparsedData(handle, out IntPtr preparsed) || preparsed == IntPtr.Zero)
                    return false;
                try
                {
                    return HidP_GetCaps(preparsed, out caps) == HIDP_STATUS_SUCCESS;
                }
                finally
                {
                    HidD_FreePreparsedData(preparsed);
                }
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        // Open the candidate's HID stream and probe both protocols. VIA's
        // id_get_protocol_version and OpenRGB-QMK's GetProtocolVersion are
        // BOTH command 0x01 — only one raw_hid_receive() handler is compiled
        // in at a time (OpenRGB-QMK replaces VIA's when OPENRGB_ENABLE is
        // set), so a single 0x01 probe tells us which firmware is running.
        // We distinguish by the OpenRGB-QMK END_OF_MESSAGE terminator (0x64)
        // the firmware writes at the last byte of every reply — VIA never
        // sets that byte to 0x64.
        private static bool TryHandshake(HidDevice hid, int inputReportByteLength, int outputReportByteLength,
            out ProtocolSupport protocol, out int ledCount, out byte cols, out byte rows, out string firmwareDeviceName, out string failureReason)
        {
            protocol = ProtocolSupport.None;
            ledCount = 0; cols = 0; rows = 0; firmwareDeviceName = string.Empty;
            failureReason = string.Empty;

            if (outputReportByteLength <= 1 || inputReportByteLength <= 1)
            {
                failureReason = $"unusable report sizes (in={inputReportByteLength}, out={outputReportByteLength})";
                return false;
            }

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

                int payloadOut = outputReportByteLength - 1; // strip leading report id
                int payloadIn  = inputReportByteLength - 1;

                byte[] outBuf = new byte[outputReportByteLength];
                byte[] inBuf  = new byte[inputReportByteLength];

                // Probe 0x01 (GetProtocolVersion in both protocols). The firmware
                // routes this to whichever handler is compiled in.
                ClearReport(outBuf);
                OpenRgbQmkProtocol.BuildGetProtocolVersion(new Span<byte>(outBuf, 1, payloadOut));
                if (!SendAndReceive(stream, outBuf, inBuf, out int rxBytes, out string probeError))
                {
                    failureReason = $"protocol-version probe failed ({probeError})";
                    return true; // not an open failure — just a non-responsive device
                }

                bool openRgbTerminator = rxBytes >= inputReportByteLength
                    && inBuf[inputReportByteLength - 1] == OpenRgbQmkProtocol.Response_EndOfMessage;

                ReadOnlySpan<byte> reply = StripReportId(inBuf);

                if (openRgbTerminator && OpenRgbQmkProtocol.TryParseProtocolVersion(reply) > 0)
                {
                    // Pull device info (LED count + name) while we have the stream.
                    ClearReport(outBuf);
                    OpenRgbQmkProtocol.BuildGetDeviceInfo(new Span<byte>(outBuf, 1, payloadOut));
                    if (SendAndReceive(stream, outBuf, inBuf, out _, out _) &&
                        OpenRgbQmkProtocol.TryParseDeviceInfo(StripReportId(inBuf),
                            out byte fwLedCount, out byte matrixSize, out string fwProduct, out string _))
                    {
                        ledCount = fwLedCount;
                        firmwareDeviceName = fwProduct;
                        // Best-effort split of matrixSize into cols/rows by
                        // sqrt — the firmware only reports the product, not
                        // the dimensions. Caller can override via mapping
                        // overrides if this guess is wrong.
                        if (matrixSize > 0)
                        {
                            int approx = (int)Math.Round(Math.Sqrt(matrixSize));
                            cols = (byte)Math.Max(1, approx);
                            rows = (byte)Math.Max(1, (matrixSize + cols - 1) / cols);
                        }
                    }
                    protocol = ProtocolSupport.OpenRgbQmk;
                    return true;
                }

                // Treat any non-OpenRGB reply with a valid VIA protocol-version
                // payload as VIA. ViaProtocol.TryParseProtocolVersion expects
                // the reply layout VIA actually uses (echo at [0], version at
                // [1..2]) — when the firmware is OpenRGB-QMK without the
                // terminator (older builds), this still falls through to None.
                ushort viaVersion = ViaProtocol.TryParseProtocolVersion(reply);
                if (viaVersion > 0)
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

        private static void ClearReport(byte[] buf) => Array.Clear(buf, 0, buf.Length);

        private static bool SendAndReceive(HidStream stream, byte[] outBuf, byte[] inBuf, out int rxBytes, out string error)
        {
            rxBytes = 0;
            error = string.Empty;
            try
            {
                stream.Write(outBuf);
            }
            catch (Exception ex)
            {
                error = $"write threw: {ex.Message}";
                return false;
            }
            try
            {
                rxBytes = stream.Read(inBuf, 0, inBuf.Length);
                return rxBytes > 0;
            }
            catch (TimeoutException)
            {
                error = $"read timed out after {QmkRawHidConstants.ResponseTimeoutMs}ms (firmware likely doesn't recognise this command)";
                return false;
            }
            catch (Exception ex)
            {
                error = $"read threw: {ex.Message}";
                return false;
            }
        }

        // HidStream prepends a report-id byte to inputs on Windows; QMK Raw
        // HID always uses report id 0 so we strip it unconditionally.
        private static ReadOnlySpan<byte> StripReportId(byte[] inBuf) =>
            new ReadOnlySpan<byte>(inBuf, 1, inBuf.Length - 1);
    }
}
