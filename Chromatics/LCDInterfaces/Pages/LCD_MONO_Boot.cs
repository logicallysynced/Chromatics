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
    public partial class LCD_MONO_Boot : Logitech_LCD.Applets.BaseAppletM
    {
        public LCD_MONO_Boot()
        {
            InitializeComponent();
        }

        public void Shutdown()
        {
            //
        }

        protected override void OnDataUpdate(object sender, EventArgs e)
        {
            //
        }
    }
}
