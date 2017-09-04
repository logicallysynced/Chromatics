using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromatics.FFXIVInterfaces;

namespace Chromatics.LCDInterfaces
{
    public partial class LCD_MONO_EorzeaTime : Logitech_LCD.Applets.BaseAppletM
    {
        public LCD_MONO_EorzeaTime()
        {
            InitializeComponent();
        }

        public void Shutdown()
        {
            //
        }

        protected override void OnDataUpdate(object sender, EventArgs e)
        {
            if (!IsActive) return;
            
            var _et = FFXIVHelpers.FetchEorzeaTime();
            var eorzeatime = _et.ToString("hh:mm tt");

            if (lbl_et_test.Disposing) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired)
                {
                    lbl_et_test.Invoke((Action) delegate { lbl_et_test.Text = eorzeatime; });
                }
                else
                {
                    lbl_et_test.Text = eorzeatime;
                }
            }
            catch (InvalidOperationException ex)
            {
                if (IsHandleCreated) throw;
            }
        }
    }
}
