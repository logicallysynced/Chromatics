using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;

namespace Chromatics_Updater
{
    public class Updater
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                MessageBox.Show("You cannot run the updater directly. Please use ACT.", "Chromatics Updater");
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                bool retry = true;
                while (retry == true)
                {
                    if (Process.GetProcessesByName("Advanced Combat Tracker").Length > 0)
                    {
                        DialogResult result = MessageBox.Show("Please close Advanced Combat Tracker to update Chromatics", "Advanced Combat Tracker running", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation);
                        if (result == DialogResult.Cancel)
                        {
                            retry = false;
                            System.Windows.Forms.Application.Exit();
                            break;
                        }
                        retry = true;
                    }
                    else
                    {
                        retry = false;
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new Updater_Form());
                    }
                }
                
            }
        }
    }
}
