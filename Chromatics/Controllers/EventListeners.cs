using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics
{
    public partial class Chromatics
    {
        protected override void SetVisibleCore(bool value)
        {
            if (!allowVisible)
            {
                value = false;
                if (!this.IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        private void Kh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            {
                KeyCtrl = true;
                //e.Handled = true;
            }

            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            {
                KeyShift = true;
                //e.Handled = true;
            }

            if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
            {
                KeyAlt = true;
                //e.Handled = true;
            }
        }

        private void Kh_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            {
                KeyCtrl = false;
                //e.Handled = true;
            }

            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            {
                KeyShift = false;
                //e.Handled = true;
            }

            if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
            {
                KeyAlt = false;
                //e.Handled = true;
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!allowClose)
            {
                Hide();
                e.Cancel = true;
                return;
            }

            FinalFormClosing(sender);
            //base.OnFormClosing(e);

        }

        private void FinalFormClosing(object sender)
        {
            HoldReader = true;
            FFXIVcts.Cancel();
            Attachcts.Cancel();
            ShutDownDevices();

            if (LifxSDKCalled == 1)
            {
                //{ new Task(() => { AttachFFXIV(); }).Start(); };
                var _LIFXRestore = new Task(() => { _lifx.LIFXRestoreState(); });
                CriticalTasks.Add(_LIFXRestore);
                CriticalTasks.Run(_LIFXRestore);
                CriticalTasks.WaitOnExit();
            }

            BeginInvoke(new MethodInvoker(Close));
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            allowClose = true;
            Application.Exit();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            if (chk_lccauto.Checked)
            {
                if (LogitechSDKCalled == 1)
                {
                    ToggleLCCMode(false, true);
                }
            }

            notify_master.Dispose();
        }

        private void ChromaticsForm_Resize(object sender, EventArgs e)
        {
            CenterPictureBox(pB_logo1, pB_logo1.Image);

            if (FormWindowState.Minimized == WindowState)
            {
                notify_master.Visible = true;
                allowVisible = false;
                Hide();
            }
        }
    }
}