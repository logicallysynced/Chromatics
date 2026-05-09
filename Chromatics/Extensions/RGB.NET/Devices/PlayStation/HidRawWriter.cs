using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Chromatics.Extensions.RGB.NET.Devices.PlayStation
{
    // Direct Win32 WriteFile wrapper for HID output reports.
    //
    // HidSharp's HidStream.Write throws IOException synchronously on any
    // failure path — invalidated handle, mid-write disconnect, partial
    // transfer, etc. Even with a per-frame "is the device still alive"
    // pre-check, the trigger thread can race a hardware unplug that
    // happens after the check passes but before WriteFile completes,
    // which still produces a debugger first-chance break.
    //
    // To eliminate the throw entirely, we open OUR OWN kernel handle
    // alongside HidSharp's. WriteFile returns BOOL — false on failure,
    // we surface that as a return value, no exception. HidSharp keeps
    // the handle it opens during TryOpen (which we keep using to detect
    // exclusive-access conflicts via DS4Windows / reWASD). Two handles
    // per controller is fine — Sony HID gamepads accept shared writes
    // by default on Windows.
    //
    // The class is intentionally minimal: open with shared read/write
    // access, write a buffer, dispose. No reads, no overlapped I/O,
    // no internal locking (the caller's UpdateQueue already serialises
    // via _writeLock).
    public sealed class HidRawWriter : IDisposable
    {
        // ── Win32 ────────────────────────────────────────────────────

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteFile(
            SafeFileHandle hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        // ── State ────────────────────────────────────────────────────

        private SafeFileHandle _handle;
        private volatile bool _closed;

        public HidRawWriter(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
                throw new ArgumentException("Device path is required.", nameof(devicePath));

            // Shared read/write so we coexist with HidSharp's handle and any
            // other app (Steam Input, game native lighting, etc.) that may
            // also be opening the device. dwFlagsAndAttributes = 0 → synchronous
            // I/O. We don't need overlapped — writes are small (78 bytes max)
            // and our caller is already on a dedicated trigger thread.
            _handle = CreateFileW(
                devicePath,
                GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (_handle == null || _handle.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                throw new IOException($"CreateFile failed for HID device ({devicePath}). Win32 error: {err}");
            }
        }

        // True on successful write, false on any failure (handle invalid,
        // device gone, partial write, etc.). Never throws — that's the
        // whole point. Caller checks the return value and can decide
        // whether to log, retry, or self-suspend the queue.
        //
        // The first byte of `buffer` must be the HID report ID, matching
        // the convention HidStream.Write uses.
        public bool TryWrite(byte[] buffer)
        {
            if (_closed) return false;
            if (buffer == null || buffer.Length == 0) return false;

            try
            {
                if (_handle == null || _handle.IsClosed || _handle.IsInvalid)
                    return false;

                bool ok = WriteFile(_handle, buffer, (uint)buffer.Length, out _, IntPtr.Zero);
                return ok;
            }
            catch
            {
                // P/Invoke marshalling could conceivably fault on a
                // pathological handle state; swallow and report failure
                // rather than escape the contract.
                return false;
            }
        }

        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            try { _handle?.Dispose(); } catch { }
        }
    }
}
