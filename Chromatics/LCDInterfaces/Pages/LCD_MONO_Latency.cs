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
using GalaSoft.MvvmLight.Ioc;

namespace Chromatics.LCDInterfaces
{
    public partial class LCD_MONO_Latency : Logitech_LCD.Applets.BaseAppletM
    {
        private static int _pingAvg;
        private System.Timers.Timer _pingTimer;
        private static int currentServer = 1;

        private readonly List<string[]> serverList = new List<string[]>
        {
            new[] { "Primal", "204.2.229.10" },
            new[] { "Aether", "204.2.229.9" },
            new[] { "Chaos", "195.82.50.9" },
            new[] { "Elemental", "124.150.157.158" },
            new[] { "Gaia", "124.150.157.157" },
            new[] { "Mana", "124.150.157.156" }
        };

        public int CurrentServer
        {
            get => currentServer;
            set
            {
                currentServer = value;
                _pingAvg = 0;
            }
        }

        public int MaxServer => serverList.Count;

        public void Shutdown()
        {
            //
        }

        public LCD_MONO_Latency()
        {
            InitializeComponent();
            
            
            _pingTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = true
            };

            _pingTimer.Elapsed += async (sender, args) =>
            {
                if (IsActive)
                {
                    var r = await PingTimeAverage(serverList[currentServer-1][1], 1);
                    _pingAvg = Convert.ToInt32(r);
                }
                else
                {
                    _pingTimer.Stop();
                    _pingTimer.Dispose();
                }
            };

            _pingTimer.Start();
            
        }
        
        private static async Task<double> PingTimeAverage(string host, int echoNum)
        {
            long totalTime = 0;
            int timeout = 50;
            Ping pingSender = new Ping();

            for (int i = 0; i < echoNum; i++)
            {
                var reply = await pingSender.SendPingAsync(host, timeout);

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

            if (_pingAvg <= 0) return;

            var result = _pingAvg + @"ms";

            if (lbl_latency_ping.Disposing) return;
            if (lbl_latency_name.Disposing) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired)
                {
                    lbl_latency_ping.Invoke((Action) delegate { lbl_latency_ping.Text = result; });
                    lbl_latency_name.Invoke((Action) delegate { lbl_latency_name.Text = serverList[currentServer-1][0]; });
                }
                else
                {
                    lbl_latency_ping.Text = result;
                    lbl_latency_name.Text = serverList[currentServer-1][0];
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
