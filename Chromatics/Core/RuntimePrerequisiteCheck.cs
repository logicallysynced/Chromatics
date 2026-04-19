using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Chromatics.Core
{
    /// <summary>
    /// Lightweight startup probe for optional native prerequisites that some RGB.NET
    /// device SDKs (Razer/Corsair/Logitech) link against. Runs only on the ZIP-portable
    /// launch path — the Velopack Setup.exe installer already declares these via the
    /// --framework flag and installs them before first run.
    /// </summary>
    internal static class RuntimePrerequisiteCheck
    {
        private const string VcRedistDownloadUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";

        // HKLM\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64 → Installed (DWORD) = 1
        // Version 14.x covers 2015/2017/2019/2022 — all binary-compatible.
        private const string VcRedistRegPath = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        private const uint MB_YESNO        = 0x4;
        private const uint MB_ICONWARNING  = 0x30;
        private const int  IDYES           = 6;

        public static void WarnIfMissing()
        {
            if (IsVcRedistInstalled()) return;

            int result = MessageBoxW(IntPtr.Zero,
                "The Visual C++ 2015-2022 x64 Runtime was not detected on this system.\n\n" +
                "Some RGB device providers (Razer, Corsair, Logitech, etc.) require it and may fail to initialize without it.\n\n" +
                "Would you like to open the Microsoft download page now?",
                "Chromatics — Missing Prerequisite",
                MB_YESNO | MB_ICONWARNING);

            if (result == IDYES)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(VcRedistDownloadUrl) { UseShellExecute = true });
                }
                catch
                {
                    // Opening the default browser can fail in stripped-down environments;
                    // nothing more we can do — user can copy the URL from the dialog above.
                }
            }
        }

        private static bool IsVcRedistInstalled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(VcRedistRegPath);
                if (key?.GetValue("Installed") is int installed)
                    return installed == 1;
            }
            catch
            {
                // Registry unavailable — assume present rather than nagging on every launch.
            }
            return true;
        }
    }
}
