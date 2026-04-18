using Gma.System.MouseKeyHook;
using System.Windows.Forms;

namespace Chromatics.Core
{
    /// <summary>
    /// Thin wrapper around a global keyboard/mouse hook that tracks the current
    /// modifier-key state (Ctrl / Shift / Alt) for other subsystems to query.
    /// </summary>
    public static class KeyController
    {
        private static IKeyboardMouseEvents _globalHook;
        private static bool _keyCtrl;
        private static bool _keyShift;
        private static bool _keyAlt;

        public static void Setup()
        {
            if (_globalHook != null) return;

            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += OnKeyDown;
            _globalHook.KeyUp += OnKeyUp;
        }

        public static void Stop()
        {
            // Guard against Stop() being called before Setup() or after a prior Stop().
            // Previously this threw NullReferenceException on the unconditional Dispose call.
            if (_globalHook == null) return;

            _globalHook.KeyDown -= OnKeyDown;
            _globalHook.KeyUp -= OnKeyUp;
            _globalHook.Dispose();
            _globalHook = null;
        }

        public static IKeyboardMouseEvents GetKeyController() => _globalHook;

        public static bool IsCtrlPressed() => _keyCtrl;

        public static bool IsShiftPressed() => _keyShift;

        public static bool IsAltPressed() => _keyAlt;

        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey) _keyCtrl = true;
            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey) _keyShift = true;
            if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu) _keyAlt = true;
        }

        private static void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey) _keyCtrl = false;
            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey) _keyShift = false;
            if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu) _keyAlt = false;
        }
    }
}
