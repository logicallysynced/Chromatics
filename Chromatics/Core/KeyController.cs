using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Chromatics.Core
{
    /// <summary>
    /// Tracks modifier-key state (Ctrl / Shift / Alt) via a global low-level keyboard
    /// hook (WH_KEYBOARD_LL) so the state is current even when the game window has focus.
    /// </summary>
    public static class KeyController
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN     = 0x0100;
        private const int WM_KEYUP       = 0x0101;
        private const int WM_SYSKEYDOWN  = 0x0104;
        private const int WM_SYSKEYUP    = 0x0105;
        private const int VK_LCONTROL    = 0xA2;
        private const int VK_RCONTROL    = 0xA3;
        private const int VK_LSHIFT      = 0xA0;
        private const int VK_RSHIFT      = 0xA1;
        private const int VK_LMENU       = 0xA4;  // left Alt
        private const int VK_RMENU       = 0xA5;  // right Alt

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPTStr)] string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Stored in a field so the delegate is not garbage-collected while the hook is active.
        private static LowLevelKeyboardProc _hookCallback;
        private static IntPtr _hookId = IntPtr.Zero;

        private static bool _keyCtrl;
        private static bool _keyShift;
        private static bool _keyAlt;
        private static int _eventsObserved;

        public static void Setup()
        {
            if (_hookId != IntPtr.Zero) return;

            _hookCallback = HookCallback;
            using var process = Process.GetCurrentProcess();
            using var module  = process.MainModule;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(module?.ModuleName), 0);

            // The result went unchecked, so a refused hook left every modifier
            // reading false for the session with nothing said about it. Modified
            // hotbar keybinds then never light and the cause is invisible.
            if (_hookId == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"Windows refused the keyboard hook (error {error}). Hotbar keybinds that use Ctrl, Alt or Shift will not light up. Restarting Chromatics usually clears this.",
                    forwardToSentry: false);
            }
        }

        public static void Stop()
        {
            // Guard against Stop() called before Setup() or after a prior Stop().
            if (_hookId == IntPtr.Zero) return;

            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;

            _keyCtrl  = false;
            _keyShift = false;
            _keyAlt   = false;
            Volatile.Write(ref _eventsObserved, 0);
        }

        public static bool IsCtrlPressed()  => _keyCtrl;
        public static bool IsShiftPressed() => _keyShift;
        public static bool IsAltPressed()   => _keyAlt;

        /// <summary>True once the hook has been installed.</summary>
        public static bool IsHookInstalled => _hookId != IntPtr.Zero;

        /// <summary>
        /// True once the hook has actually delivered a keystroke. An installed
        /// hook that never fires is the shape of the silent failures: Windows
        /// drops hooks that exceed LowLevelHooksTimeout without telling us, and
        /// an unelevated process never sees input going to an elevated game.
        /// </summary>
        public static bool HasObservedInput => Volatile.Read(ref _eventsObserved) > 0;

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool down  = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
                bool up    = wParam == WM_KEYUP   || wParam == WM_SYSKEYUP;

                if (down || up)
                {
                    if (Volatile.Read(ref _eventsObserved) == 0)
                        Volatile.Write(ref _eventsObserved, 1);

                    bool pressed = down;
                    switch (vkCode)
                    {
                        case VK_LCONTROL:
                        case VK_RCONTROL: _keyCtrl  = pressed; break;
                        case VK_LSHIFT:
                        case VK_RSHIFT:   _keyShift = pressed; break;
                        case VK_LMENU:
                        case VK_RMENU:    _keyAlt   = pressed; break;
                    }
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}
