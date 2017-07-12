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
            var thisprocessname = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
                return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Application.Run(Locator.ChromaticsInstance);
        }
    }
}