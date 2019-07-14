using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromatics.FFXIVInterfaces;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

namespace Chromatics.LCDInterfaces
{
    public partial class LCD_COL_Main : Logitech_LCD.Applets.BaseAppletC
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
        
        public ActorItem PlayerInfo { get; set; }
        public ActorItem TargetInfo { get; set; }
        //public PlayerEntity PlayerEntity { get; set; }

        private string _jobmem = "";

        public LCD_COL_Main()
        {
            InitializeComponent();
            var placeholder = new Bitmap(16, 16, PixelFormat.Format32bppRgb);

            if (InvokeRequired)
            {
                pB_job.Invoke((System.Action)delegate { pB_job.Visible = false; });
                pB_buff_1.Invoke((System.Action)delegate { pB_buff_1.Visible = false; });
                pB_buff_2.Invoke((System.Action)delegate { pB_buff_2.Visible = false; });
                pB_buff_3.Invoke((System.Action)delegate { pB_buff_3.Visible = false; });
                pB_buff_4.Invoke((System.Action)delegate { pB_buff_4.Visible = false; });
                pB_buff_5.Invoke((System.Action)delegate { pB_buff_5.Visible = false; });
                pB_buff_6.Invoke((System.Action)delegate { pB_buff_6.Visible = false; });
                pB_buff_7.Invoke((System.Action)delegate { pB_buff_7.Visible = false; });
                pB_buff_8.Invoke((System.Action)delegate { pB_buff_8.Visible = false; });
                pB_buff_9.Invoke((System.Action)delegate { pB_buff_9.Visible = false; });
                pB_buff_10.Invoke((System.Action)delegate { pB_buff_10.Visible = false; });
                pB_buff_11.Invoke((System.Action)delegate { pB_buff_11.Visible = false; });
                pB_buff_12.Invoke((System.Action)delegate { pB_buff_12.Visible = false; });
                pB_debuff_1.Invoke((System.Action)delegate { pB_debuff_1.Visible = false; });
                pB_debuff_2.Invoke((System.Action)delegate { pB_debuff_2.Visible = false; });
                pB_debuff_3.Invoke((System.Action)delegate { pB_debuff_3.Visible = false; });
                pB_debuff_4.Invoke((System.Action)delegate { pB_debuff_4.Visible = false; });
                pB_debuff_5.Invoke((System.Action)delegate { pB_debuff_5.Visible = false; });
                pB_debuff_6.Invoke((System.Action)delegate { pB_debuff_6.Visible = false; });
                pB_debuff_7.Invoke((System.Action)delegate { pB_debuff_7.Visible = false; });
                pB_debuff_8.Invoke((System.Action)delegate { pB_debuff_8.Visible = false; });
                pB_debuff_9.Invoke((System.Action)delegate { pB_debuff_9.Visible = false; });
                pB_debuff_10.Invoke((System.Action)delegate { pB_debuff_10.Visible = false; });
                pB_debuff_11.Invoke((System.Action)delegate { pB_debuff_11.Visible = false; });
                pB_debuff_12.Invoke((System.Action)delegate { pB_debuff_12.Visible = false; });
            }
            else
            {
                pB_job.Visible = false;
                pB_buff_1.Visible = false;
                pB_buff_2.Visible = false;
                pB_buff_3.Visible = false;
                pB_buff_4.Visible = false;
                pB_buff_5.Visible = false;
                pB_buff_6.Visible = false;
                pB_buff_7.Visible = false;
                pB_buff_8.Visible = false;
                pB_buff_9.Visible = false;
                pB_buff_10.Visible = false;
                pB_buff_11.Visible = false;
                pB_buff_12.Visible = false;
                pB_debuff_1.Visible = false;
                pB_debuff_2.Visible = false;
                pB_debuff_3.Visible = false;
                pB_debuff_4.Visible = false;
                pB_debuff_5.Visible = false;
                pB_debuff_6.Visible = false;
                pB_debuff_7.Visible = false;
                pB_debuff_8.Visible = false;
                pB_debuff_9.Visible = false;
                pB_debuff_10.Visible = false;
                pB_debuff_11.Visible = false;
                pB_debuff_12.Visible = false;
            }

            _pingTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = true
            };

            _pingTimer.Elapsed += async (sender, args) =>
            {
                if (IsActive)
                {
                    var r = await PingTimeAverage(serverList[currentServer - 1][1], 1);
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

        public void Shutdown()
        {
            //
        }

        protected override void OnDataUpdate(object sender, EventArgs e)
        {
            if (!IsActive) return;

            ProcessStance();
            ProcessET();
            ProcessLatency();
            ProcessBase();
            ProcessEffects();
            
        }

        private void ProcessStance()
        {
            if (!IsHandleCreated) return;
            if (Disposing) return;

            if (PlayerInfo.InCombat)
            {
                if (InvokeRequired)
                {
                    Invoke((System.Action)delegate { BackgroundImage = Properties.Resources.col_main_back_battle; });
                }
                else
                {
                    BackgroundImage = Properties.Resources.col_main_back_battle;
                }
            }
            else
            {
                if (InvokeRequired)
                {
                    Invoke((System.Action)delegate { BackgroundImage = Properties.Resources.col_main_back; });
                }
                else
                {
                    BackgroundImage = Properties.Resources.col_main_back;
                }
            }
        }
        
        private void ProcessBase()
        {
            if (!IsHandleCreated) return;
            if (lbl_lvl.Disposing) return;
            if (lbl_job.Disposing) return;
            if (prog_hp.Disposing) return;
            if (pB_job.Disposing) return;
            
            try
            {
                //Info
                var lvl = @"Lv. " + PlayerInfo.Level;
                var job = PlayerInfo.Job.ToString();
                var hp = (PlayerInfo.HPCurrent - 0) * (100 - 0) / (PlayerInfo.HPMax - 0) + 0;
                //var hp = Convert.ToInt32(PlayerInfo.HPPercent * 100);
                
                if (InvokeRequired)
                {
                    lbl_lvl.Invoke((System.Action)delegate { lbl_et.Text = lvl; });
                    lbl_job.Invoke((System.Action)delegate { lbl_job.Text = job; });
                    prog_hp.Invoke((System.Action)delegate { prog_hp.Value = hp - 1; });
                }
                else
                {
                    lbl_lvl.Text = lvl;
                    lbl_job.Text = job;
                    prog_hp.Value = hp - 1;
                }

                //Job Picture
                string ptJob;

                switch (PlayerInfo.Job)
                {
                    case Actor.Job.FSH:
                        ptJob = "Fisher";
                        break;
                    case Actor.Job.BTN:
                        ptJob = "Botanist";
                        break;
                    case Actor.Job.MIN:
                        ptJob = "Miner";
                        break;
                    case Actor.Job.ALC:
                        ptJob = "Alchemist";
                        break;
                    case Actor.Job.ARM:
                        ptJob = "Armorer";
                        break;
                    case Actor.Job.BSM:
                        ptJob = "Blacksmith";
                        break;
                    case Actor.Job.CPT:
                        ptJob = "Carpenter";
                        break;
                    case Actor.Job.CUL:
                        ptJob = "Culinarian";
                        break;
                    case Actor.Job.GSM:
                        ptJob = "Goldsmith";
                        break;
                    case Actor.Job.LTW:
                        ptJob = "Leatherworker";
                        break;
                    case Actor.Job.WVR:
                        ptJob = "Weaver";
                        break;
                    case Actor.Job.ARC:
                        ptJob = "Archer";
                        break;
                    case Actor.Job.LNC:
                        ptJob = "Lancer";
                        break;
                    case Actor.Job.CNJ:
                        ptJob = "Conjurer";
                        break;
                    case Actor.Job.GLD:
                        ptJob = "Gladiator";
                        break;
                    case Actor.Job.MRD:
                        ptJob = "Marauder";
                        break;
                    case Actor.Job.PGL:
                        ptJob = "Pugilist";
                        break;
                    case Actor.Job.ROG:
                        ptJob = "Rouge";
                        break;
                    case Actor.Job.THM:
                        ptJob = "Thaumaturge";
                        break;
                    case Actor.Job.ACN:
                        ptJob = "Arcanist";
                        break;
                    case Actor.Job.AST:
                        ptJob = "Astrologian";
                        break;
                    case Actor.Job.BRD:
                        ptJob = "Bard";
                        break;
                    case Actor.Job.BLM:
                        ptJob = "Black_Mage";
                        break;
                    case Actor.Job.DRK:
                        ptJob = "Dark_Knight";
                        break;
                    case Actor.Job.DRG:
                        ptJob = "Dragoon";
                        break;
                    case Actor.Job.MCH:
                        ptJob = "Machinist";
                        break;
                    case Actor.Job.MNK:
                        ptJob = "Monk";
                        break;
                    case Actor.Job.NIN:
                        ptJob = "Ninja";
                        break;
                    case Actor.Job.PLD:
                        ptJob = "Paladin";
                        break;
                    case Actor.Job.SCH:
                        ptJob = "Scholar";
                        break;
                    case Actor.Job.SMN:
                        ptJob = "Summoner";
                        break;
                    case Actor.Job.WHM:
                        ptJob = "White_Mage";
                        break;
                    case Actor.Job.WAR:
                        ptJob = "Warrior";
                        break;
                    case Actor.Job.SAM:
                        ptJob = "Samurai";
                        break;
                    case Actor.Job.RDM:
                        ptJob = "Red_Mage";
                        break;
                    case Actor.Job.DNC:
                        ptJob = "Dancer";
                        break;
                    case Actor.Job.GNB:
                        ptJob = "Gunbreaker";
                        break;
                    case Actor.Job.BLU:
                        ptJob = "Bluemage";
                        break;
                    default:
                        ptJob = "Chocobo";
                        break;
                }

                if (ptJob != _jobmem)
                {
                    var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    var path = enviroment + @"/core/img/hd/";
                    var img = ptJob + @"_Icon_3.png";
                    var filepath = path + img;

                    if (File.Exists(filepath))
                    {
                        if (InvokeRequired)
                        {
                            pB_job.Invoke((System.Action) delegate { pB_job.ImageLocation = filepath; pB_job.Visible = true; });
                        }
                        else
                        {
                            pB_job.ImageLocation = filepath;
                            pB_job.Visible = true;
                        }
                    }
                    _jobmem = ptJob;
                }
            }
            catch (InvalidOperationException ex)
            {
                if (IsHandleCreated) throw;
                Console.WriteLine(ex.InnerException);
            }

        }

        private void ProcessEffects()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = enviroment + @"/core/img/status/";

            var statEffects = PlayerInfo?.StatusItems;
            if (statEffects == null)
            {
                for (var ci = 1; ci < 25; ci++)
                {
                    var pbx = ReturnBuff(ci);
                    if (pbx == null) continue;

                    if (InvokeRequired)
                    {
                        pbx.Invoke((System.Action)delegate { pbx.Visible = true; });
                    }
                    else
                    {
                        pbx.Visible = true;
                    }
                }

                return;
            }

            var battleSystem = statEffects.Where(p => !p.IsCompanyAction).Take(24).ToList();

            //System.Actions
            for (var ci = 1; ci < 25; ci++)
            {
                var pb = ReturnBuff(ci);
                
                if (pb == null) continue;

                if (ci <= battleSystem.Count)
                {
                    var action = battleSystem[ci - 1].StatusName.Replace(" ", "_").ToLower() + @".png";

                    var filename = path + action;
                    if (!File.Exists(filename))
                    {
                        if (InvokeRequired)
                        {
                            pb.Invoke((System.Action)delegate { pb.Visible = false; });
                        }
                        else
                        {
                            pb.Visible = false;
                        }
                        Console.WriteLine(action + @" not found in images.");
                    }

                    if (InvokeRequired)
                    {
                        pb.Invoke((System.Action)delegate { pb.Visible = true; pb.ImageLocation = filename; });
                    }
                    else
                    {
                        pb.Visible = true;
                        pb.ImageLocation = filename;
                    }
                        
                }
                else
                {
                    if (InvokeRequired)
                    {
                        pb.Invoke((System.Action)delegate { pb.Visible = false; });
                    }
                    else
                    {
                        pb.Visible = false;
                    }
                }
                
            }
        }

        private PictureBox ReturnBuff(int i)
        {
            if (i > 24) return null;

            switch (i)
            {
                case 1:
                    return pB_buff_1;
                case 2:
                    return pB_buff_2;
                case 3:
                    return pB_buff_3;
                case 4:
                    return pB_buff_4;
                case 5:
                    return pB_buff_5;
                case 6:
                    return pB_buff_6;
                case 7:
                    return pB_buff_7;
                case 8:
                    return pB_buff_8;
                case 9:
                    return pB_buff_9;
                case 10:
                    return pB_buff_10;
                case 11:
                    return pB_buff_11;
                case 12:
                    return pB_buff_12;
                case 13:
                    return pB_debuff_1;
                case 14:
                    return pB_debuff_2;
                case 15:
                    return pB_debuff_3;
                case 16:
                    return pB_debuff_4;
                case 17:
                    return pB_debuff_5;
                case 18:
                    return pB_debuff_6;
                case 19:
                    return pB_debuff_7;
                case 20:
                    return pB_debuff_8;
                case 21:
                    return pB_debuff_9;
                case 22:
                    return pB_debuff_10;
                case 23:
                    return pB_debuff_11;
                case 24:
                    return pB_debuff_12;
                default:
                    return null;
            }
        }

        /*
        private PictureBox ReturnDebuff(int i)
        {
            if (i > 12) return null;

            switch (i)
            {
                case 1:
                    return pB_debuff_1;
                case 2:
                    return pB_debuff_2;
                case 3:
                    return pB_debuff_3;
                case 4:
                    return pB_debuff_4;
                case 5:
                    return pB_debuff_5;
                case 6:
                    return pB_debuff_6;
                case 7:
                    return pB_debuff_7;
                case 8:
                    return pB_debuff_8;
                case 9:
                    return pB_debuff_9;
                case 10:
                    return pB_debuff_10;
                case 11:
                    return pB_debuff_11;
                case 12:
                    return pB_debuff_12;
                default:
                    return null;
            }
        }
        */

        private void ProcessET()
        {
            if (!IsActive) return;

            var _et = FFXIVHelpers.FetchEorzeaTime();
            var eorzeatime = @"ET " + _et.ToString("hh:mm");

            if (lbl_et.Disposing) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired)
                {
                    lbl_et.Invoke((System.Action)delegate { lbl_et.Text = eorzeatime; });
                }
                else
                {
                    lbl_et.Text = eorzeatime;
                }
            }
            catch (InvalidOperationException ex)
            {
                if (IsHandleCreated) throw;
                Console.WriteLine(ex.InnerException);
            }
        }

        private void ProcessLatency()
        {
            if (_pingAvg <= 0) return;

            var result = _pingAvg + @"ms";

            if (lbl_latency.Disposing) return;
            if (lbl_server.Disposing) return;
            if (!IsHandleCreated) return;

            try
            {
                if (InvokeRequired)
                {
                    lbl_latency.Invoke((System.Action)delegate { lbl_latency.Text = result; });
                    lbl_server.Invoke((System.Action)delegate { lbl_server.Text = serverList[currentServer - 1][0]; });
                }
                else
                {
                    lbl_latency.Text = result;
                    lbl_server.Text = serverList[currentServer - 1][0];
                }
            }
            catch (InvalidOperationException ex)
            {
                if (IsHandleCreated) throw;
                Console.WriteLine(ex.InnerException);
            }
        }
        
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

    }
}
