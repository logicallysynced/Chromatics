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
        private const int MaxConsoleChars = 200_000;
        private const int TrimChunk = 50_000;

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
                    AppendAndTrim(e);
                });
            }
            else
            {
                AppendAndTrim(e);
            }

            #if DEBUG
                Debug.WriteLine(e.Message);
            #endif
        }

        private void AppendAndTrim(OnConsoleLoggedEventArgs e)
        {
            rtb_console.SelectionColor = e.Color;
            rtb_console.AppendText(e.Message + Environment.NewLine);

            // Prevent unbounded growth over long sessions. When we exceed the cap,
            // drop the oldest chunk so the control trims in batches rather than on
            // every append.
            if (rtb_console.TextLength > MaxConsoleChars)
            {
                rtb_console.Select(0, TrimChunk);
                rtb_console.SelectedText = string.Empty;
            }

            rtb_console.SelectionStart = rtb_console.TextLength;
            rtb_console.ScrollToCaret();
        }

        private void rtb_console_TextChanged(object sender, EventArgs e)
        {
            rtb_console.SelectionStart = rtb_console.Text.Length;
            rtb_console.ScrollToCaret();
        }
    }
}
