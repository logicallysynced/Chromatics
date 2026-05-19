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
            // True when the same VID/PID exposes a Keyboard HID interface
            // alongside the Raw HID one. Lets the provider distinguish a
            // VIA-running keyboard (which gets a synthetic ANSI-104 layout
            // so Chromatics's keyboard layers can paint it) from a VIA-
            // running macropad / knob / specialty board (which sticks to a
            // single Custom1 LED).
            public readonly bool HasKeyboardSibling;

            public Candidate(HidDevice hid, ProtocolSupport protocol, int ledCount, byte columns, byte rows, string firmwareDeviceName, bool hasKeyboardSibling)
            {
                Hid = hid; Protocol = protocol; LedCount = ledCount;
                MatrixColumns = columns; MatrixRows = rows;
                FirmwareDeviceName = firmwareDeviceName ?? string.Empty;
                HasKeyboardSibling = hasKeyboardSibling;
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

                bool hasKeyboardSibling = HasKeyboardInterface(all, hid.VendorID, hid.ProductID);
                Logger.WriteConsole(LoggerTypes.Devices,
                    $"[QMK]   handshake OK — protocol: {protocol}, LEDs: {ledCount}, matrix: {cols}x{rows}, keyboardSibling: {hasKeyboardSibling}");
                results.Add(new Candidate(hid, protocol, ledCount, cols, rows, fwName, hasKeyboardSibling));
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

        // True if any HID interface on the same USB device (matched by VID/PID)
        // declares the standard USB HID Keyboard usage (Generic Desktop page
        // 0x01, usage 0x06). USB composite devices expose each HID interface
        // as a separate HidDevice; QMK keyboards always expose a Keyboard
        // interface alongside their Raw HID one, while VIA-running macropads
        // / knob boards / one-handed boards typically don't.
        private const ushort GenericDesktopUsagePage = 0x01;
        private const ushort KeyboardUsage           = 0x06;
        private static bool HasKeyboardInterface(HidDevice[] all, int vid, int pid)
        {
            foreach (HidDevice sibling in all)
            {
                if (sibling.VendorID != vid || sibling.ProductID != pid) continue;
                if (TryGetTopLevelUsage(sibling.DevicePath, out ushort page, out ushort usage))
                {
                    if (page == GenericDesktopUsagePage && usage == KeyboardUsage)
                        return true;
                }
            }
            return false;
        }

        // Returns true if the HidDevice's top-level collection declares
        // usage page 0xFF60, usage 0x61 (the QMK Raw HID identifier).
        // Multi-interface USB devices expose each interface as a separate
        // HidDevice, so this check naturally selects only the Raw HID
        // interface and leaves the keyboard / consumer interfaces alone.
        //
        // We bypass HidSharp's GetReportDescriptor / GetRawReportDescriptor
        // entirely on Windows. Windows doesn't expose the raw HID report
        // descriptor bytes — HidSharp reconstructs them from
        // HidD_GetPreparsedData + HidP_GetValueCaps + HidP_GetButtonCaps,
        // and that reconstruction throws NotSupportedException on QMK
        // firmwares whose descriptors include items HidSharp's parser
        // doesn't recognise (Keychron C3 Pro 8K with current QMK
        // confirmed). The top-level UsagePage + Usage we actually need
        // are right there in HIDP_CAPS — querying that struct directly via
        // P/Invoke skips the reconstruction entirely, identifies the QMK
        // interface reliably, and avoids triggering a first-chance
        // exception that breaks under VS debug.
        private static bool ExposesRawHidUsage(HidDevice hid)
        {
            try
            {
                if (TryGetTopLevelUsage(hid.DevicePath, out ushort page, out ushort usage))
                {
                    return page == QmkRawHidConstants.RawHidUsagePage
                        && usage == QmkRawHidConstants.RawHidUsage;
                }
            }
            catch { /* discovery is best-effort */ }
            return false;
        }

        // Win32 HIDP_CAPS layout. Only UsagePage / Usage are read; the rest
        // of the struct must still be present for HidP_GetCaps to fill it
        // correctly. See https://learn.microsoft.com/windows-hardware/drivers/ddi/hidpi/ns-hidpi-hidp_caps.
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

        // Opens the HID device with DEVICE_QUERY_ACCESS (dwDesiredAccess = 0)
        // so the call doesn't conflict with VIA / Vial / OpenRGB holding
        // the interface open exclusively for read/write, then pulls the
        // top-level Usage + UsagePage out of HIDP_CAPS.
        private static bool TryGetTopLevelUsage(string devicePath, out ushort usagePage, out ushort usage)
        {
            usagePage = 0;
            usage = 0;
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
                    if (HidP_GetCaps(preparsed, out HIDP_CAPS caps) != HIDP_STATUS_SUCCESS)
                        return false;
                    usagePage = caps.UsagePage;
                    usage = caps.Usage;
                    return true;
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
