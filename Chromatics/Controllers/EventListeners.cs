using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromatics.Controllers;

namespace Chromatics
{
    public partial class Chromatics
    {
        protected override void SetVisibleCore(bool value)
        {
            if (!_allowVisible)
            {
                value = false;
                if (!IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        private void Kh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            {
                _keyCtrl = true;
                //Console.WriteLine("CONTROL");
            }

            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            {
                _keyShift = true;
                //Console.WriteLine("SHIFT");
            }

            if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
            {
                _keyAlt = true;
                //Console.WriteLine("ALT");
            }
        }

        private void Kh_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
                _keyCtrl = false;

            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
                _keyShift = false;

            if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
                _keyAlt = false;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (ChromaticsSettings.ChromaticsSettingsQuickClose)
            {
                if (!_allowClose)
                {
                    notify_master.Visible = true;
                    _allowVisible = false;
                    Hide();
                    e.Cancel = true;
                    return;
                }
            }

            FinalFormClosing(sender);
            //base.OnFormClosing(e);
        }

        private void FinalFormClosing(object sender)
        {
            _exit = true;
            HoldReader = true;
            _attachcts.Cancel();
            _ffxiVcts.Cancel();
            ShutDownDevices();

            if (LCCStatus && LogitechSdkCalled == 1)
                ToggleLccMode(false, false);

            if (LifxSdkCalled == 1)
            {
                //{ new Task(() => { AttachFFXIV(); }).Start(); };
                var lifxRestore = new Task(() => { _lifx.LifxRestoreState(); });
                CriticalTasks.Add(lifxRestore);
                CriticalTasks.Run(lifxRestore);
                CriticalTasks.WaitOnExit();
            }

            BeginInvoke(new MethodInvoker(Close));
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _allowClose = true;
            notify_master.Dispose();
            
            Application.Exit();
        }
        
        private void OnApplicationExit(object sender, EventArgs e)
        {
            //
        }

        private void ChromaticsForm_Resize(object sender, EventArgs e)
        {
            CenterPictureBox(pB_logo1, pB_logo1.Image);

            if (FormWindowState.Minimized == WindowState)
            {
                notify_master.Visible = true;
                _allowVisible = false;
                Hide();
            }
        }


    }
}