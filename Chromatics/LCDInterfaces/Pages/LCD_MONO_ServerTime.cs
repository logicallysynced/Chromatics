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

namespace Chromatics.LCDInterfaces
{
    public partial class LCD_MONO_ServerTime : Logitech_LCD.Applets.BaseAppletM
    {
        public LCD_MONO_ServerTime()
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

            var servertime = DateTime.UtcNow.ToString("hh:mm tt");

            if (lbl_st_test.Disposing) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired)
                {
                    lbl_st_test.Invoke((Action) delegate { lbl_st_test.Text = servertime; });
                }
                else
                {
                    lbl_st_test.Text = servertime;
                }
            }
            catch (InvalidOperationException ex)
            {
                if (IsHandleCreated) throw;
                Console.WriteLine(ex.InnerException);
            }
        }
    }
}
