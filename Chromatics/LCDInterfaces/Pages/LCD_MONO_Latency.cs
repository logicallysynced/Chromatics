using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.LCDInterfaces
{
    public partial class LCD_MONO_Latency : Logitech_LCD.Applets.BaseAppletM
    {
        private static List<int> pingAvg = new List<int>();
        private System.Timers.Timer _pingTimer;

        public LCD_MONO_Latency()
        {
            InitializeComponent();
            pingAvg.Clear();

            _pingTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = true
            };

            _pingTimer.Elapsed += (sender, args) =>
            {
                if (IsActive)
                {
                    var r = PingTimeAverage("204.2.229.10", 1);
                    pingAvg.Add(Convert.ToInt32(r));
                }
                else
                {
                    _pingTimer.Stop();
                    _pingTimer.Dispose();
                }
            };

            _pingTimer.Start();
        }
        
        private static double PingTimeAverage(string host, int echoNum)
        {
            long totalTime = 0;
            int timeout = 120;
            Ping pingSender = new Ping();

            for (int i = 0; i < echoNum; i++)
            {
                PingReply reply = pingSender.Send(host, timeout);
                if (reply != null && reply.Status == IPStatus.Success)
                {
                    totalTime += reply.RoundtripTime;
                }
            }
            return totalTime / echoNum;
        }

        protected override void OnDataUpdate(object sender, EventArgs e)
        {
            if (!IsActive) return;

            if (pingAvg.Count <= 0) return;

            var result = pingAvg.Average() + @"ms";

            if (lbl_latency_ping.Disposing) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired)
                {
                    lbl_latency_ping.Invoke((Action) delegate { lbl_latency_ping.Text = result; });
                }
                else
                {
                    lbl_latency_ping.Text = result;
                }
            }
            catch (InvalidOperationException ex)
            {
                if (IsHandleCreated) throw;
            }
        }
    }
}
