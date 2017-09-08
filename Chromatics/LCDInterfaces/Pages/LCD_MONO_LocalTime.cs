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
    public partial class LCD_MONO_LocalTime : Logitech_LCD.Applets.BaseAppletM
    {
        public LCD_MONO_LocalTime()
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
            
            var localtime = DateTime.Now.ToString("hh:mm tt");

            if (lbl_lt_test.Disposing) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired)
                {
                    lbl_lt_test.Invoke((Action) delegate { lbl_lt_test.Text = localtime; });
                }
                else
                {
                    lbl_lt_test.Text = localtime;
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
