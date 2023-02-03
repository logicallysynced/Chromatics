using Chromatics.Core;
using Chromatics.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.Forms
{
    public partial class Uc_Console : UserControl
    {
        public Uc_Console()
        {
            InitializeComponent();

            Logger.OnConsoleLogged += new OnConsoleLoggedEventHandler(OnConsoleLogged);
            rtb_console.TextChanged += rtb_console_TextChanged;
        }

        private void OnConsoleLogged(object sender, OnConsoleLoggedEventArgs e)
        {
            if (InvokeRequired)
            {
                rtb_console.Invoke((Action)delegate 
                { 
                    rtb_console.SelectionColor = e.Color;
                    rtb_console.AppendText(e.Message + Environment.NewLine);
                    rtb_console.SelectionStart = rtb_console.Text.Length;
                    rtb_console.ScrollToCaret();
                });
            }
            else
            {
                rtb_console.SelectionColor = e.Color;
                rtb_console.AppendText(e.Message + Environment.NewLine);
                rtb_console.SelectionStart = rtb_console.Text.Length;
                rtb_console.ScrollToCaret();
            }

            #if DEBUG
                Debug.WriteLine(e.Message);
            #endif
        }

        private void rtb_console_TextChanged(object sender, EventArgs e)
        {
            rtb_console.SelectionStart = rtb_console.Text.Length;
            rtb_console.ScrollToCaret();
        }
    }
}
