using Gma.System.MouseKeyHook;
using HidSharp.Reports.Units;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.Core
{
    public static class KeyController
    {
        private static IKeyboardMouseEvents _mGlobalHook;
        private static bool _keyCtrl;
        private static bool _keyShift;
        private static bool _keyAlt;

        public static void Setup()
        {
            //Hook to Key Listener
            _mGlobalHook = Hook.GlobalEvents();

            _mGlobalHook.KeyDown += Kh_KeyDown;
            _mGlobalHook.KeyUp += Kh_KeyUp;
        }

        public static void Stop()
        {
            if (_mGlobalHook != null)
            {
                _mGlobalHook.KeyDown -= Kh_KeyDown;
                _mGlobalHook.KeyUp -= Kh_KeyUp;
            }
            
            _mGlobalHook.Dispose();
        }

        public static bool IsCtrlPressed()
        {
            return _keyCtrl;
        }

        public static bool IsShiftPressed()
        {
            return _keyShift;
        }
        public static bool IsAltPressed()
        {
            return _keyAlt;
        }

        private static void Kh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            {
                _keyCtrl = true;
            }

            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            {
                _keyShift = true;
            }

            if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
            {
                _keyAlt = true;
            }
        }

        private static void Kh_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
                _keyCtrl = false;

            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
                _keyShift = false;

            if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
                _keyAlt = false;
        }
    }
}
