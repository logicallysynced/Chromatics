using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Chromatics.Controllers;

namespace Chromatics
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // don't allow multiple instances of Chromatics to run at once
            if(!ThereCanOnlyBeOne())
            {
                if(Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(Locator.ChromaticsInstance);
        }

        private static bool ThereCanOnlyBeOne()
        {
            var thisprocessname = Process.GetCurrentProcess().ProcessName;
            var otherProcesses = Process.GetProcesses()
                .Where(p => p.ProcessName == thisprocessname)
                .Where(p => p.Id != Process.GetCurrentProcess().Id);

            var enumerable = otherProcesses.ToList();
            if (enumerable.Any())
            {
                if (MessageBox.Show(@"Another instance of Chromatics is currently running, and only one can run at a time. Would you like to close the other instance and use this one?", @"Already running", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach(var process in enumerable)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}