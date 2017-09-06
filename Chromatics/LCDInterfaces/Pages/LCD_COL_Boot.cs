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
    public partial class LCD_COL_Boot : Logitech_LCD.Applets.BaseAppletC
    {
        private string _boottext = @"";

        public string SetBootText
        {
            get => _boottext;
            set => _boottext = value;
        }

        public LCD_COL_Boot()
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
            

            if (lbl_boot_txt.Disposing) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired)
                {
                    lbl_boot_txt.Invoke((Action)delegate { lbl_boot_txt.Text = _boottext; });
                }
                else
                {
                    lbl_boot_txt.Text = _boottext;
                }
            }
            catch (InvalidOperationException ex)
            {
                if (IsHandleCreated) throw;
            }
        }
    }
}
