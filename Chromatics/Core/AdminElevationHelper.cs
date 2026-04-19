using Chromatics.Models;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Chromatics.Core
{
    internal static class AdminElevationHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        private const uint MB_YESNO        = 0x4;
        private const uint MB_ICONWARNING  = 0x30;
        private const uint MB_ICONQUESTION = 0x20;
        private const int  IDYES           = 6;

        public static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// If not running as admin, prompts the user to relaunch elevated.
        /// When alwaysRunAsAdmin is set, skips the dialog and relaunches silently.
        /// Saves the alwaysRunAsAdmin preference if the user opts in.
        /// </summary>
        public static void CheckAndElevateIfNeeded(SettingsModel settings)
        {
            if (IsRunningAsAdmin()) return;

            if (settings.alwaysRunAsAdmin)
            {
                RelaunchAsAdmin();
                return;
            }

            int relaunch = MessageBoxW(IntPtr.Zero,
                "Chromatics requires administrator privileges to read FFXIV process memory.\n\n" +
                "Would you like to relaunch Chromatics as Administrator?",
                "Chromatics — Administrator Required",
                MB_YESNO | MB_ICONWARNING);

            if (relaunch != IDYES) return;

            int always = MessageBoxW(IntPtr.Zero,
                "Would you like Chromatics to always relaunch as Administrator automatically?\n\n" +
                "You can turn this off in Settings → Advanced.",
                "Chromatics — Always Run as Administrator?",
                MB_YESNO | MB_ICONQUESTION);

            if (always == IDYES)
            {
                settings.alwaysRunAsAdmin = true;
                AppSettings.SaveSettings(settings);
            }

            RelaunchAsAdmin();
        }

        private static void RelaunchAsAdmin()
        {
            try
            {
                var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;
                Process.Start(new ProcessStartInfo(exePath) { Verb = "runas", UseShellExecute = true });
                Environment.Exit(0);
            }
            catch
            {
                // User cancelled the UAC prompt — continue without admin.
            }
        }
    }
}
