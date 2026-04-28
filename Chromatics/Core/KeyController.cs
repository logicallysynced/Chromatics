using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        public static void Setup()
        {
            if (_hookId != IntPtr.Zero) return;

            _hookCallback = HookCallback;
            using var process = Process.GetCurrentProcess();
            using var module  = process.MainModule;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(module?.ModuleName), 0);
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
        }

        public static bool IsCtrlPressed()  => _keyCtrl;
        public static bool IsShiftPressed() => _keyShift;
        public static bool IsAltPressed()   => _keyAlt;

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool down  = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
                bool up    = wParam == WM_KEYUP   || wParam == WM_SYSKEYUP;

                if (down || up)
                {
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
