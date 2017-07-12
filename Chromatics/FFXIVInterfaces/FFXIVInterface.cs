using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interactivity;
using Sharlayan;
using Sharlayan.Models;
using Chromatics.LCDInterfaces;
using System.Collections.Concurrent;
using System.Net;
using System.IO;
using System.Windows.Forms;


/* Contains the code to read the FFXIV Memory Stream, parse the data and convert to lighting commands
 * FFXIVAPP-Memory.dll is used to access Actor and target information.
 * https://github.com/Icehunter/ffxivapp-memory
 */

namespace Chromatics
{
    partial class Chromatics
    {
        private Color _BaseColor = Color.Black;
        private string _CurrentStatus = "";
        private int _hp;

        /* Parse FFXIV Function
         * Read the data from Sharlayan and call lighting functions according
         */

        private bool lastcast;
        private bool successcast;

        /* Attatch to FFXIV process after determining if running DX9 or DX11.
         * DX11 is currently not supported by SharlayanReader however support is being built currently,
         * Therefore I have implemented code to setup DX11 when it becomes available shortly.
        */

        public bool InitiateMemory()
        {
            var _initiated = false;

            /*
            string currentVersion = ChromaticsSettings.FinalFantasyXIVVersion.Replace(".", string.Empty);
            string newVersion = currentVersion;
            var webRequest = WebRequest.Create(@"https://chromaticsffxiv.com/chromatics2/update/patch.txt");

            using (var response = webRequest.GetResponse())
            using (var content = response.GetResponseStream())
            using (var reader = new StreamReader(content))
            {
                string newVersionA = reader.ReadToEnd();
                newVersion = newVersionA.Replace(".", string.Empty);
            }

            int cV = int.Parse(currentVersion);
            int nV = int.Parse(newVersion);

            if (nV > cV)
            {
                //
            }
            */

            try
            {

                var processes9 = Process.GetProcessesByName("ffxiv");
                var processes11 = Process.GetProcessesByName("ffxiv_dx11");

                // DX9
                if (processes9.Length > 0)
                {
                    WriteConsole(ConsoleTypes.FFXIV, "Attempting Attach..");

                    if (init)
                    {
                        WriteConsole(ConsoleTypes.FFXIV, "Chromatics already attached.");
                        return true;
                    }

                    init = false;
                    // supported: English, Chinese, Japanese, French, German, Korean
                    var gameLanguage = "English";
                    bool ignoreJSONCache = ChromaticsSettings.ChromaticsSettings_MemoryCache ? false : true;
                    // patchVersion of game, or latest
                    string patchVersion = "latest";
                    var process = processes9[0];
                    var processModel = new ProcessModel
                    {
                        Process = process,
                        IsWin64 = true
                    };
                    Sharlayan.MemoryHandler.Instance.SetProcess(processModel, gameLanguage, patchVersion, ignoreJSONCache);
                    _initiated = true;
                    init = true;
                    isDX11 = false;

                    WriteConsole(ConsoleTypes.FFXIV, "DX9 Initiated");
                    WriteConsole(ConsoleTypes.ERROR, "DX9 support has been phased out from Chromatics. Please use DX11 when using Chromatics.");
                }

                // DX11
                else if (processes11.Length > 0)
                {
                    WriteConsole(ConsoleTypes.FFXIV, "Attempting Attach..");

                    if (init)
                    {
                        return true;
                    }


                    init = false;
                    // supported: English, Chinese, Japanese, French, German, Korean
                    var gameLanguage = "English";
                    bool ignoreJSONCache = ChromaticsSettings.ChromaticsSettings_MemoryCache ? false : true;
                    // patchVersion of game, or latest
                    string patchVersion = "latest";
                    var process = processes11[0];
                    var processModel = new ProcessModel
                    {
                        Process = process,
                        IsWin64 = true
                    };
                    Sharlayan.MemoryHandler.Instance.SetProcess(processModel, gameLanguage, patchVersion, ignoreJSONCache);
                    _initiated = true;
                    init = true;
                    isDX11 = true;

                    WriteConsole(ConsoleTypes.FFXIV, "DX11 Initiated");


                    //WriteConsole(ConsoleTypes.ERROR, "DX11 Currently not Supported. Please use DX9.");
                    //notify_master.Text = @"DX11 is currently not Supported. Please use DX9.";
                    //_initiated = false; //DX11 Not Supported
                    //init = false;
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.ERROR, "Error: " + ex.Message);
                WriteConsole(ConsoleTypes.ERROR, "Internal Error: " + ex.StackTrace);
            }
            
            return _initiated;
        }

        /* Memory Loop */

        private async Task CallFFXIVMemory(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(300);
                ReadFFXIVMemory();
            }
        }

        public void FFXIVGameStop()
        {
            if (attatched == 0) return;

            //Console.WriteLine("Debug trip");

            HoldReader = true;

            MemoryTasks.Cleanup();
            Watchdog.WatchdogStop();
            GameResetCatch.Enabled = false;
            WriteConsole(ConsoleTypes.FFXIV, "Game stopped");
            attatched = 0;
            ArxState = 0;

            HoldReader = false;
        }

        //private static readonly object _ReadFFXIVMemory = new object();
        private Sharlayan.Core.ActorEntity PlayerInfo = new Sharlayan.Core.ActorEntity();
        private ConcurrentDictionary<uint, Sharlayan.Core.ActorEntity> PlayerInfoX = new ConcurrentDictionary<uint, Sharlayan.Core.ActorEntity>();
        private bool MenuNotify = false;

        public void ReadFFXIVMemory()
        {
            try
            {
                /*
                    *0 - Not Attached
                    *1 - Game Not Running
                    *2 - Game in Menu
                    *3 - Game Running
                */

                if (attatched > 0)
                {
                    //Get Data

                    PlayerInfoX = Reader.GetActors()?.PCEntities;
                    PlayerInfo = Sharlayan.Core.ActorEntity.CurrentUser;
                    
                    //Console.WriteLine(PlayerInfo.Name);
                    //Action
                    if (attatched == 3)
                    {
                        //Game is Running
                        //Check if game has stopped by checking Character data for a null value.
                        
                        if (PlayerInfo != null && PlayerInfo.Name == "")
                        {
                            //End Game Running if timed out
                            if (!GameResetCatch.Enabled)
                            {
                                //Console.WriteLine("Debug on");
                                GameResetCatch.Enabled = true;
                                GameResetCatch.AutoReset = false;
                            }
                        }
                        else
                        {
                            GameResetCatch.Enabled = false;
                        }
                        


                        //Call function to parse FFXIV data
                        if (HoldReader == false)
                            ProcessFFXIVData();
                    }
                    else
                    {
                        //Game Not Running
                        if (attatched == 1)
                        {
                            state = 6;
                            GlobalUpdateState("wave", Color.Magenta, false, Color.MediumSeaGreen, true, 40);
                            attatched = 2;
                        }

                        //Game in Menu
                        else if (attatched == 2)
                        {
                            if (PlayerInfo != null && PlayerInfo.Name != "")
                            {
                                //Set Game Active
                                WriteConsole(ConsoleTypes.FFXIV, "Game Running (" + PlayerInfo.Name + ")");


                                if (ArxSDKCalled == 1 && ArxState == 0)
                                {
                                    _arx.ArxUpdateInfo("Game Running (" + PlayerInfo.Name + ")");
                                }

                                MenuNotify = false;
                                Setbase = false;
                                //GlobalUpdateState("static", Color.Red, false);
                                //GlobalUpdateBulbState(100, System.Drawing.Color.Red, 100);
                                Watchdog.WatchdogGo();
                                attatched = 3;

                                if (ArxSDKCalled == 1)
                                {
                                    if (ArxState != 0) return;
                                    if (cb_arx_mode.SelectedIndex < 4)
                                    {
                                        ArxState = cb_arx_mode.SelectedIndex + 1;

                                        switch (ArxState)
                                        {
                                            case 1:
                                                _arx.ArxSetIndex("playerhud.html");
                                                break;
                                            case 2:
                                                _arx.ArxSetIndex("partylist.html");
                                                break;
                                            case 3:
                                                _arx.ArxSetIndex("mapdata.html");
                                                break;
                                            case 4:
                                                _arx.ArxSetIndex("act.html");
                                                var changed = txt_arx_actip.Text;
                                                if (changed.EndsWith("/"))
                                                {
                                                    changed = changed.Substring(0, changed.Length - 1);
                                                }

                                                _arx.ArxSendACTInfo(changed, 8085);
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        var getPlugin = cb_arx_mode.SelectedItem + ".html";
                                        ArxState = 100;
                                        _arx.ArxSetIndex(getPlugin);
                                    }
                                }

                                state = 0;
                            }
                            else
                            {
                                //Main Menu Still Active
                                if (!MenuNotify)
                                {
                                    WriteConsole(ConsoleTypes.FFXIV, "Main Menu is still active.");

                                    if (ArxSDKCalled == 1 && ArxState == 0)
                                    {
                                        _arx.ArxUpdateInfo("Main Menu is still active");
                                    }

                                    MenuNotify = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    //Not Attached
                    attatched = 0;
                    ArxState = 0;

                    if (ArxSDKCalled == 1 && ArxState == 0)
                    {
                        _arx.ArxSetIndex("info.html");
                    }

                    GlobalUpdateState("static", Color.DeepSkyBlue, false);
                    //GlobalApplyMapMouseLighting("All", Color.DeepSkyBlue, false);
                    GlobalUpdateBulbState(1, Color.DeepSkyBlue, 0);

                    //Console.WriteLine("Debug C");

                    Attachcts = new CancellationTokenSource();

                    var _MemoryTask = new Task(() =>
                    {
                        var _call = CallFFXIVAttach(Attachcts.Token);
                    }, MemoryTask.Token);

                    MemoryTasks.Add(_MemoryTask);
                    MemoryTasks.Run(_MemoryTask);
                    FFXIVcts.Cancel();
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.ERROR, "Init Error: " + ex.Message);
                WriteConsole(ConsoleTypes.ERROR, "Internal Error: " + ex.StackTrace);
            }
        }
        
        private FFXIVInterfaces.Cooldowns.CardTypes _CurrentCard;
        private bool playgroundonce = false;
        private void ProcessFFXIVData()
        {
            MemoryTasks.Cleanup();

            try
            {

                //Get Data
                Sharlayan.Core.ActorEntity TargetInfo = new Sharlayan.Core.ActorEntity();
                List<Sharlayan.Core.EnmityEntry> TargetEmnityInfo = new List<Sharlayan.Core.EnmityEntry>();
                ConcurrentDictionary<uint, Sharlayan.Core.PartyEntity> PartyInfo = new ConcurrentDictionary<uint, Sharlayan.Core.PartyEntity>();
                List<uint> PartyListNew = new List<uint>();
                Dictionary<uint, uint> PartyListOld = new Dictionary<uint, uint>();
                

                PlayerInfoX = Reader.GetActors()?.PCEntities;
                PlayerInfo = Sharlayan.Core.ActorEntity.CurrentUser;
                
                try
                {
                    if (PlayerInfo.Name != "" && PlayerInfo.TargetID > 0)
                    {
                        TargetInfo = Reader.GetTargetInfo()?.TargetEntity?.CurrentTarget;
                        TargetEmnityInfo = Reader.GetTargetInfo()?.TargetEntity?.EnmityEntries;
                    }

                    PartyInfo = Reader.GetPartyMembers()?.PartyEntities;

                    PartyListNew = Reader.GetPartyMembers()?.NewParty;
                    PartyListOld = Reader.GetPartyMembers()?.RemovedParty;
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.ERROR, "Parser B: " + ex.Message);
                }
                
                
                if (PlayerInfo != null && PlayerInfo.Name != "")
                {
                    //Debug.WriteLine(PlayerInfo.Name);
                    Watchdog.WatchdogReset();
                }
                

                if (PlayerInfo != null)
                {
                    
                    


                    if (!playgroundonce)
                    {
                        
                        playgroundonce = true;
                    }

                    /*
                    var mapname = Sharlayan.Helpers.ZoneHelper.MapInfo(PlayerInfo.MapID).Name;
                    Debug.WriteLine(mapname.English);
                    */

                    //End Playground

                    var max_HP = 0;
                    var current_HP = 0;
                    var max_MP = 0;
                    var current_MP = 0;
                    var max_TP = 0;
                    var current_TP = 0;
                    var hp_perc = PlayerInfo.HPPercent;
                    var mp_perc = PlayerInfo.MPPercent;
                    var tp_perc = PlayerInfo.TPPercent;
                    var c_class = "battle";

                    //Set colour variables

                    var BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor);
                    var HighlightColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_HighlightColor);

                    var col_hpfull = ColorTranslator.FromHtml(ColorMappings.ColorMapping_HPFull);
                    var col_hpempty = ColorTranslator.FromHtml(ColorMappings.ColorMapping_HPEmpty);
                    var col_hpcritical = ColorTranslator.FromHtml(ColorMappings.ColorMapping_HPCritical);
                    var col_mpfull = ColorTranslator.FromHtml(ColorMappings.ColorMapping_MPFull);
                    var col_mpempty = ColorTranslator.FromHtml(ColorMappings.ColorMapping_MPEmpty);
                    var col_tpfull = ColorTranslator.FromHtml(ColorMappings.ColorMapping_TPFull);
                    var col_tpempty = ColorTranslator.FromHtml(ColorMappings.ColorMapping_TPEmpty);
                    var col_castcharge = ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeFull);

                    var col_em0 = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Emnity0);
                    var col_em1 = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Emnity1);
                    var col_em2 = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Emnity2);
                    var col_em3 = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Emnity3);
                    var col_em4 = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Emnity4);

                    Console.WriteLine(PlayerInfo.Job);

                    //Get Battle, Crafting or Gathering Data

                    if (PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.ALC || PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.ARM || PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.BSM ||
                        PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.CPT || PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.CUL || PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.GSM ||
                        PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.LTW || PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.WVR)
                    {
                        max_HP = PlayerInfo.HPMax;
                        current_HP = PlayerInfo.HPCurrent;
                        max_MP = PlayerInfo.CPMax;
                        current_MP = PlayerInfo.CPCurrent;
                        max_TP = PlayerInfo.TPMax;
                        current_TP = PlayerInfo.TPCurrent;
                        mp_perc = PlayerInfo.CPPercent;
                        c_class = "craft";

                        col_mpfull = ColorTranslator.FromHtml(ColorMappings.ColorMapping_CPFull);
                        col_mpempty = ColorTranslator.FromHtml(ColorMappings.ColorMapping_CPEmpty);
                    }
                    else if (PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.FSH || PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.BTN || PlayerInfo.Job == Sharlayan.Core.Enums.Actor.Job.MIN)
                    {
                        max_HP = PlayerInfo.HPMax;
                        current_HP = PlayerInfo.HPCurrent;
                        max_MP = PlayerInfo.GPMax;
                        current_MP = PlayerInfo.GPCurrent;
                        max_TP = PlayerInfo.TPMax;
                        current_TP = PlayerInfo.TPCurrent;
                        mp_perc = PlayerInfo.GPPercent;
                        c_class = "gather";

                        col_mpfull = ColorTranslator.FromHtml(ColorMappings.ColorMapping_GPFull);
                        col_mpempty = ColorTranslator.FromHtml(ColorMappings.ColorMapping_GPEmpty);
                    }
                    else
                    {
                        max_HP = PlayerInfo.HPMax;
                        current_HP = PlayerInfo.HPCurrent;
                        max_MP = PlayerInfo.MPMax;
                        current_MP = PlayerInfo.MPCurrent;
                        max_TP = PlayerInfo.TPMax;
                        current_TP = PlayerInfo.TPCurrent;
                        mp_perc = PlayerInfo.MPPercent;
                        c_class = "battle";

                        col_mpfull = ColorTranslator.FromHtml(ColorMappings.ColorMapping_MPFull);
                        col_mpempty = ColorTranslator.FromHtml(ColorMappings.ColorMapping_MPEmpty);
                    }

                    //Update ARX
                    if (PartyInfo != null) 
                    {
                        //Console.WriteLine(PartyInfo[0]);
                        //var partyx = PartyList[1];
                        //Console.WriteLine(PartyListNew.Count);
                    }

                    if (ArxSDKCalled == 1)
                    {
                        if (ArxState == 1)
                        {
                            hp_perc = PlayerInfo.HPPercent;
                            tp_perc = PlayerInfo.TPPercent;

                            var arx_hudmd = "normal";
                            double target_percent = 0;
                            int target_hpcurrent = 0;
                            int target_hpmax = 0;
                            string target_name = "target";
                            int target_engaged = 0;

                            if (TargetInfo != null && TargetInfo.Type == Sharlayan.Core.Enums.Actor.Type.Monster)
                            {
                                arx_hudmd = "target";
                                target_percent = TargetInfo.HPPercent;
                                target_hpcurrent = TargetInfo.HPCurrent;
                                target_hpmax = TargetInfo.HPMax;
                                target_name = TargetInfo.Name + " (Lv. " + TargetInfo.Level + ")";
                                target_engaged = 0;

                                if (TargetInfo.IsClaimed)
                                {
                                    target_engaged = 1;
                                }
                            }

                            _arx.ArxUpdateFfxivStats(hp_perc, mp_perc, tp_perc, current_HP, current_MP, current_TP, PlayerInfo.MapID, c_class, arx_hudmd, target_percent, target_hpcurrent, target_hpmax, target_name, target_engaged);
                        }
                        else if (ArxState == 2)
                        {
                            Console.WriteLine(PartyInfo.Count);
                            var datastring = new string[10];
                            for (uint i = 0; i < 10; i++)
                            {
                                //Console.WriteLine(i);
                                //PlayerInfo.Job == "ALC" || PlayerInfo.Job == "ARM" || PlayerInfo.Job == "BSM" || PlayerInfo.Job == "CPT" || PlayerInfo.Job == "CUL" || PlayerInfo.Job == "GSM" || PlayerInfo.Job == "LTW" || PlayerInfo.Job == "WVR")
                                if (PartyInfo != null && i < PartyInfo.Count)
                                {
                                    //Console.WriteLine(i);
                                    var pid = PartyListNew[Convert.ToInt32(i)];
                                    var pt_type = "";
                                    var pt_tpcurrent = "";
                                    var pt_tppercent = "";
                                    var pt_emnityno = "";
                                    var pt_job = "";

                                    if (TargetInfo != null && TargetInfo.Type == Sharlayan.Core.Enums.Actor.Type.Monster && TargetInfo.IsClaimed)
                                    {
                                        //Collect Emnity Table
                                        //var TargetEmnity = TargetEmnityInfo.Count;
                                        var _emnitytableX = new List<KeyValuePair<uint, uint>>();


                                        for (var g = 0; g < TargetEmnityInfo.Count; g++)
                                            _emnitytableX.Add(new KeyValuePair<uint, uint>(TargetEmnityInfo[g].ID,
                                                TargetEmnityInfo[g].Enmity));

                                        //Sort emnity by highest holder
                                        _emnitytableX.OrderBy(kvp => kvp.Value);

                                        //Get your index in the list
                                        pt_emnityno = _emnitytableX.FindIndex(a => a.Key == PartyInfo[pid].ID).ToString();

                                    }
                                    else
                                    {
                                        pt_emnityno = "0";
                                    }

                                    switch (PartyInfo[pid].Job)
                                    {
                                        case Sharlayan.Core.Enums.Actor.Job.FSH:
                                            pt_type = "player";
                                            pt_job = "Fisher";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.BTN:
                                            pt_type = "player";
                                            pt_job = "Botanist";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.MIN:
                                            pt_type = "player";
                                            pt_job = "Miner";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.ALC:
                                            pt_type = "player";
                                            pt_job = "Alchemist";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.ARM:
                                            pt_type = "player";
                                            pt_job = "Armorer";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.BSM:
                                            pt_type = "player";
                                            pt_job = "Blacksmith";
                                            break;
                                            case Sharlayan.Core.Enums.Actor.Job.CPT:
                                            pt_type = "player";
                                            pt_job = "Carpenter";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.CUL:
                                            pt_type = "player";
                                            pt_job = "Culinarian";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.GSM:
                                            pt_type = "player";
                                            pt_job = "Goldsmith";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.LTW:
                                            pt_type = "player";
                                            pt_job = "Leatherworker";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.WVR:
                                            pt_type = "player";
                                            pt_job = "Weaver";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.ARC:
                                            pt_type = "player";
                                            pt_job = "Archer";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.LNC:
                                            pt_type = "player";
                                            pt_job = "Lancer";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.CNJ:
                                            pt_type = "player";
                                            pt_job = "Conjurer";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.GLD:
                                            pt_type = "player";
                                            pt_job = "Gladiator";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.MRD:
                                            pt_type = "player";
                                            pt_job = "Marauder";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.PGL:
                                            pt_type = "player";
                                            pt_job = "Pugilist";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.ROG:
                                            pt_type = "player";
                                            pt_job = "Rouge";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.THM:
                                            pt_type = "player";
                                            pt_job = "Thaumaturge";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.ACN:
                                            pt_type = "player";
                                            pt_job = "Arcanist";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.AST:
                                            pt_type = "player";
                                            pt_job = "Astrologian";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.BRD:
                                            pt_type = "player";
                                            pt_job = "Bard";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.BLM:
                                            pt_type = "player";
                                            pt_job = "Black_Mage";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.DRK:
                                            pt_type = "player";
                                            pt_job = "Dark_Knight";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.DRG:
                                            pt_type = "player";
                                            pt_job = "Dragoon";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.MCH:
                                            pt_type = "player";
                                            pt_job = "Machinist";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.MNK:
                                            pt_type = "player";
                                            pt_job = "Monk";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.NIN:
                                            pt_type = "player";
                                            pt_job = "Ninja";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.PLD:
                                            pt_type = "player";
                                            pt_job = "Paladin";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.SCH:
                                            pt_type = "player";
                                            pt_job = "Scholar";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.SMN:
                                            pt_type = "player";
                                            pt_job = "Summoner";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.WHM:
                                            pt_type = "player";
                                            pt_job = "White_Mage";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.WAR:
                                            pt_type = "player";
                                            pt_job = "Warrior";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.SAM:
                                            pt_type = "player";
                                            pt_job = "Samurai";
                                            break;
                                        case Sharlayan.Core.Enums.Actor.Job.RDM:
                                            pt_type = "player";
                                            pt_job = "Red_Mage";
                                            break;
                                        default:
                                            pt_type = "unknown";
                                            pt_job = "Chocobo";
                                            break;
                                    }

                                    if (i == 0)
                                    {
                                        pt_tppercent = tp_perc.ToString("#0%");
                                        pt_tpcurrent = current_TP.ToString();
                                    }
                                    else
                                    {
                                        pt_tppercent = "100%";
                                        pt_tpcurrent = "1000";
                                    }

                                    datastring[i] = "1," + pt_type + "," + PartyInfo[pid].Name + "," + PartyInfo[pid].HPPercent.ToString("#0%") + "," + PartyInfo[pid].HPCurrent + "," + PartyInfo[pid].MPPercent.ToString("#0%") + "," + PartyInfo[pid].MPCurrent + "," + pt_tppercent + "," + pt_tpcurrent + "," + pt_emnityno + "," + pt_job;
                                    Console.WriteLine(i + @": " + datastring[i]);
                                }
                                else
                                {
                                    datastring[i] = "0,0,0,0,0,0,0,0,0";
                                    //Console.WriteLine(i + @": " + datastring[i]);
                                }
                            }

                            _arx.ArxUpdateFfxivParty(datastring[1], datastring[2], datastring[3], datastring[4], datastring[5], datastring[6], datastring[7], datastring[8], datastring[9]);

                        }
                        else if (ArxState == 100)
                        {
                            hp_perc = PlayerInfo.HPPercent;
                            tp_perc = PlayerInfo.TPPercent;

                            int hp_max = PlayerInfo.HPMax;
                            int mp_max = PlayerInfo.MPMax;
                            var playerposX = PlayerInfo.X;
                            var playerposY = PlayerInfo.Y;
                            var playerposZ = PlayerInfo.Z;
                            var actionstatus = PlayerInfo.ActionStatus;
                            var castperc = PlayerInfo.CastingPercentage;
                            var castprogress = PlayerInfo.CastingProgress;
                            var casttime = PlayerInfo.CastingTime;
                            var castingtoggle = PlayerInfo.IsCasting;
                            var hitboxrad = PlayerInfo.HitBoxRadius;
                            var playerclaimed = PlayerInfo.IsClaimed;
                            var playerjob = PlayerInfo.Job;
                            var mapid = PlayerInfo.MapID;
                            var mapindex = PlayerInfo.MapIndex;
                            var mapterritory = PlayerInfo.MapTerritory;
                            var playername = PlayerInfo.Name;
                            var targettype = PlayerInfo.TargetType;
                            
                            var arx_hudmd = "normal";
                            double target_percent = 0;
                            int target_hpcurrent = 0;
                            int target_hpmax = 0;
                            string target_name = "target";
                            int target_engaged = 0;

                            if (TargetInfo != null && TargetInfo.Type == Sharlayan.Core.Enums.Actor.Type.Monster)
                            {
                                arx_hudmd = "target";
                                target_percent = TargetInfo.HPPercent;
                                target_hpcurrent = TargetInfo.HPCurrent;
                                target_hpmax = TargetInfo.HPMax;
                                target_name = TargetInfo.Name + " (Lv. " + TargetInfo.Level + ")";
                                target_engaged = 0;

                                if (TargetInfo.IsClaimed)
                                {
                                    target_engaged = 1;
                                }
                            }

                            _arx.ArxUpdateFfxivPlugin(hp_perc, mp_perc, tp_perc, current_HP, current_MP, current_TP, PlayerInfo.MapID, c_class, arx_hudmd, target_percent, target_hpcurrent, target_hpmax, target_name, target_engaged, hp_max, mp_max, playerposX, playerposY, playerposZ, actionstatus, castperc, castprogress, casttime, castingtoggle, hitboxrad, playerclaimed, playerjob, mapid, mapindex, mapterritory, playername, targettype);
                        }
                        else if (ArxState == 0)
                        {
                            if (attatched == 3)
                            {
                                ArxState = 1;
                                Thread.Sleep(100);
                                _arx.ArxSetIndex("playerhud.html");
                            }
                        }
                    }

                    //Console.WriteLine("Map ID: " + PlayerInfo.MapID);

                    //Parse Data

                    //Set Base Keyboard lighting. 
                    //Other LED's are built above this base layer.
                    if (Setbase == false)
                    {
                        _BaseColor = BaseColor;
                        GlobalUpdateState("static", _BaseColor, false);
                        GlobalUpdateBulbState(2, _BaseColor, 500);
                        GlobalUpdateBulbState(5, _BaseColor, 500);
                        GlobalUpdateBulbState(6, _BaseColor, 500);
                        GlobalUpdateBulbState(10, _BaseColor, 500);
                        Setbase = true;
                    }

                    //Highlight critical FFXIV keybinds

                    if (ChromaticsSettings.ChromaticsSettings_KeyHighlights)
                    {
                        if (ChromaticsSettings.ChromaticsSettings_AZERTYMode)
                        {
                            GlobalApplyMapKeyLighting("Z", HighlightColor, false);
                            GlobalApplyMapKeyLighting("Q", HighlightColor, false);
                            GlobalApplyMapKeyLighting("S", HighlightColor, false);
                            GlobalApplyMapKeyLighting("D", HighlightColor, false);
                            GlobalApplyMapKeyLighting("LeftShift", HighlightColor, false);
                            GlobalApplyMapKeyLighting("LeftControl", HighlightColor, false);
                            GlobalApplyMapKeyLighting("Space", HighlightColor, false);

                            GlobalApplyMapKeyLighting("W", _BaseColor, false);
                            GlobalApplyMapKeyLighting("A", _BaseColor, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("W", HighlightColor, false);
                            GlobalApplyMapKeyLighting("A", HighlightColor, false);
                            GlobalApplyMapKeyLighting("S", HighlightColor, false);
                            GlobalApplyMapKeyLighting("D", HighlightColor, false);
                            GlobalApplyMapKeyLighting("LeftShift", HighlightColor, false);
                            GlobalApplyMapKeyLighting("LeftControl", HighlightColor, false);
                            GlobalApplyMapKeyLighting("Space", HighlightColor, false);

                            GlobalApplyMapKeyLighting("Z", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Q", _BaseColor, false);
                        }

                        GlobalUpdateBulbState(3, HighlightColor, 100);
                    }
                    else
                    {
                        GlobalApplyMapKeyLighting("W", _BaseColor, false);
                        GlobalApplyMapKeyLighting("A", _BaseColor, false);
                        GlobalApplyMapKeyLighting("S", _BaseColor, false);
                        GlobalApplyMapKeyLighting("D", _BaseColor, false);
                        GlobalApplyMapKeyLighting("LeftShift", _BaseColor, false);
                        GlobalApplyMapKeyLighting("LeftControl", _BaseColor, false);
                        GlobalApplyMapKeyLighting("Space", _BaseColor, false);
                        GlobalApplyMapKeyLighting("Z", _BaseColor, false);
                        GlobalApplyMapKeyLighting("Q", _BaseColor, false);
                    }

                    if (TargetInfo == null)
                    {
                        GlobalApplyMapKeyLighting("PrintScreen",
                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                        GlobalUpdateBulbState(4, ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), 250);
                        GlobalApplyMapKeyLighting("Scroll",
                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity),
                            false);
                        GlobalApplyMapKeyLighting("Pause", ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity),
                            false);
                        GlobalApplyMapKeyLighting("Macro16",
                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity),
                            false);
                        GlobalApplyMapKeyLighting("Macro17",
                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity),
                            false);
                        GlobalApplyMapKeyLighting("Macro18",
                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity),
                            false);

                        GlobalApplyMapLogoLighting("", HighlightColor, false);
                        GlobalApplyMapMouseLighting("Logo", HighlightColor, false);
                    }


                    //Debuff Status Effects

                    //if (PlayerInfo.IsClaimed)
                    //{
                    var stat_effects = PlayerInfo.StatusEntries;

                    if (stat_effects.Count > 0)
                    {
                        var Status = stat_effects.Last();
                        if (Status.IsCompanyAction == false && Status.TargetName == PlayerInfo.Name)
                            if (_CurrentStatus != Status.StatusName)
                            {
                                if (Status.StatusName == "Bind")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Bind), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Bind);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Petrification")
                                {
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Petrification);
                                    GlobalUpdateState("static", _BaseColor, false);
                                    GlobalUpdateBulbState(6, _BaseColor, 250);
                                }
                                else if (Status.StatusName == "Old")
                                {
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Slow);
                                    GlobalUpdateState("static", _BaseColor, false);
                                    GlobalUpdateBulbState(6, _BaseColor, 250);
                                }
                                else if (Status.StatusName == "Slow")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Slow), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Slow);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Stun")
                                {
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Stun);
                                    GlobalUpdateState("static", _BaseColor, false);
                                    GlobalUpdateBulbState(6, _BaseColor, 250);
                                }
                                else if (Status.StatusName == "Silence")
                                {
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Silence);
                                    GlobalUpdateState("static", _BaseColor, false);
                                    GlobalUpdateBulbState(6, _BaseColor, 250);
                                }
                                else if (Status.StatusName == "Poison")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Poison), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Poison);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Pollen")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Pollen), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Pollen);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Pox")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Pox), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Pox);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Paralysis")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Paralysis), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Paralysis);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Leaden")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Leaden), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Leaden);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Incapacitation")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Incapacitation), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Incapacitation);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Dropsy")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Dropsy), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Dropsy);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Amnesia")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Amnesia), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Amnesia);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Bleed")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Bleed), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Bleed);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Misery")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Misery), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Misery);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Sleep")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Sleep), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Sleep);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Daze")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Daze), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Daze);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Heavy")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Heavy), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Heavy);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Infirmary")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Infirmary), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Infirmary);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Burns")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_Burns), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_Burns);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Deep Freeze")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_DeepFreeze), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_DeepFreeze);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else if (Status.StatusName == "Damage Down")
                                {
                                    GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_DamageDown), 100);
                                    _BaseColor = ColorTranslator.FromHtml(ColorMappings.ColorMapping_DamageDown);
                                    GlobalUpdateBulbState(6, _BaseColor, 1000);
                                }
                                else
                                {
                                    _BaseColor = BaseColor;
                                    GlobalUpdateState("static", _BaseColor, false);
                                    GlobalUpdateBulbState(6, _BaseColor, 500);

                                    GlobalApplyMapKeyLighting("W", HighlightColor, false);
                                    GlobalApplyMapKeyLighting("A", HighlightColor, false);
                                    GlobalApplyMapKeyLighting("S", HighlightColor, false);
                                    GlobalApplyMapKeyLighting("D", HighlightColor, false);
                                    GlobalApplyMapKeyLighting("LeftShift", HighlightColor, false);
                                    GlobalApplyMapKeyLighting("LeftControl", HighlightColor, false);
                                    GlobalApplyMapKeyLighting("Space", HighlightColor, false);

                                    if (TargetInfo == null)
                                    {
                                        GlobalApplyMapKeyLighting("PrintScreen",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                                        GlobalApplyMapKeyLighting("Scroll",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                                        GlobalApplyMapKeyLighting("Pause",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                                        GlobalUpdateBulbState(4,
                                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity),
                                            250);
                                        GlobalApplyMapKeyLighting("Macro16",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                                        GlobalApplyMapKeyLighting("Macro17",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                                        GlobalApplyMapKeyLighting("Macro18",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);

                                        GlobalApplyMapLogoLighting("", HighlightColor, false);
                                        GlobalApplyMapMouseLighting("Logo", HighlightColor, false);
                                    }
                                }

                                _CurrentStatus = Status.StatusName;
                            }
                        //}
                    }


                    //Target
                    if (TargetInfo != null)
                    {
                        if (TargetInfo.Type == Sharlayan.Core.Enums.Actor.Type.Monster)
                        {
                            //Debug.WriteLine("Claimed: " + TargetInfo.ClaimedByID);
                            //Debug.WriteLine("Claimed: " + TargetInfo.);

                            //Debug.WriteLine(TargetInfo.IsClaimed);

                            //Target HP
                            var current_THP = TargetInfo.HPCurrent;
                            var max_THP = TargetInfo.HPMax;
                            var pol_TargetHP = (current_THP - 0) * (5 - 0) / (max_THP - 0) + 0;
                            var pol_TargetHPX = (current_THP - 0) * (65535 - 0) / (max_THP - 0) + 0;

                            if (TargetInfo.IsClaimed)
                                _lifx.LIFXUpdateStateBrightness(5,
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed),
                                    (ushort) pol_TargetHPX, 250);
                            else
                                _lifx.LIFXUpdateStateBrightness(5,
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle),
                                    (ushort) pol_TargetHPX, 250);

                            if (pol_TargetHP == 0)
                            {
                                GlobalApplyMapKeyLighting("Macro1",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                GlobalApplyMapKeyLighting("Macro2",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                GlobalApplyMapKeyLighting("Macro3",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                GlobalApplyMapKeyLighting("Macro4",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                GlobalApplyMapKeyLighting("Macro5",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                            }
                            else if (pol_TargetHP == 1)
                            {
                                if (TargetInfo.IsClaimed)
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                }
                                else
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                }
                            }
                            else if (pol_TargetHP == 2)
                            {
                                if (TargetInfo.IsClaimed)
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                }
                                else
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                }
                            }
                            else if (pol_TargetHP == 3)
                            {
                                if (TargetInfo.IsClaimed)
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                }
                                else
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                }
                            }
                            else if (pol_TargetHP == 4)
                            {
                                if (TargetInfo.IsClaimed)
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                }
                                else
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                }
                            }
                            else if (pol_TargetHP == 5)
                            {
                                if (TargetInfo.IsClaimed)
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPClaimed), false);
                                }
                                else
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetHPIdle), false);
                                }
                            }


                            //Emnity/Casting
                                                        
                            if (TargetInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("PrintScreen",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                GlobalUpdateBulbState(4,
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting),
                                    0);
                                GlobalApplyMapKeyLighting("Scroll",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                GlobalApplyMapKeyLighting("Pause",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                GlobalApplyMapKeyLighting("Macro16",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                GlobalApplyMapKeyLighting("Macro17",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                GlobalApplyMapKeyLighting("Macro18",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                GlobalApplyMapMouseLighting("Logo",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                GlobalApplyMapLogoLighting("",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);

                                //ToggleGlobalFlash2(true);
                                //GlobalFlash2(ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), 200);

                                //castalert = 1;
                            }
                            else
                            {
                                //ToggleGlobalFlash2(false);
                                

                                if (TargetInfo.IsClaimed)
                                {
                                    //Collect Emnity Table
                                    //var TargetEmnity = TargetEmnityInfo.Count;
                                    var _emnitytable = new List<KeyValuePair<uint, uint>>();


                                    for (var i = 0; i < TargetEmnityInfo.Count; i++)
                                        _emnitytable.Add(new KeyValuePair<uint, uint>(TargetEmnityInfo[i].ID,
                                            TargetEmnityInfo[i].Enmity));

                                    //Sort emnity by highest holder
                                    _emnitytable.OrderBy(kvp => kvp.Value);

                                    //Get your index in the list
                                    var PersonalID = PlayerInfo.ID;
                                    var EmnityPosition = _emnitytable.FindIndex(a => a.Key == PersonalID);

                                    //Debug.WriteLine("Em Position: " + EmnityPosition);

                                    //_emnitytable.Clear();

                                    if (EmnityPosition == -1)
                                    {
                                        //Engaged/No Aggro
                                        GlobalApplyMapKeyLighting("PrintScreen", col_em0, false);
                                        GlobalUpdateBulbState(4, col_em0, 1000);
                                        GlobalApplyMapKeyLighting("Scroll", col_em0, false);
                                        GlobalApplyMapKeyLighting("Pause", col_em0, false);
                                        GlobalApplyMapKeyLighting("Macro16", col_em0, false);
                                        GlobalApplyMapKeyLighting("Macro17", col_em0, false);
                                        GlobalApplyMapKeyLighting("Macro18", col_em0, false);
                                        GlobalApplyMapMouseLighting("Logo", col_em0, false);
                                        GlobalApplyMapLogoLighting("", col_em0, false);
                                    }
                                    else if (EmnityPosition > 4 && EmnityPosition <= 8)
                                    {
                                        //Low Aggro
                                        GlobalApplyMapKeyLighting("PrintScreen", col_em1, false);
                                        GlobalUpdateBulbState(4, col_em1, 1000);
                                        GlobalApplyMapKeyLighting("Scroll", col_em1, false);
                                        GlobalApplyMapKeyLighting("Pause", col_em1, false);
                                        GlobalApplyMapKeyLighting("Macro16", col_em1, false);
                                        GlobalApplyMapKeyLighting("Macro17", col_em1, false);
                                        GlobalApplyMapKeyLighting("Macro18", col_em1, false);
                                        GlobalApplyMapMouseLighting("Logo", col_em1, false);
                                        GlobalApplyMapLogoLighting("", col_em1, false);
                                    }
                                    else if (EmnityPosition > 1 && EmnityPosition <= 4)
                                    {
                                        //Moderate Aggro
                                        GlobalApplyMapKeyLighting("PrintScreen", col_em2, false);
                                        GlobalUpdateBulbState(4, col_em2, 1000);
                                        GlobalApplyMapKeyLighting("Scroll", col_em2, false);
                                        GlobalApplyMapKeyLighting("Pause", col_em2, false);
                                        GlobalApplyMapKeyLighting("Macro16", col_em2, false);
                                        GlobalApplyMapKeyLighting("Macro17", col_em2, false);
                                        GlobalApplyMapKeyLighting("Macro18", col_em2, false);
                                        GlobalApplyMapMouseLighting("Logo", col_em2, false);
                                        GlobalApplyMapLogoLighting("", col_em2, false);
                                    }
                                    else if (EmnityPosition == 1)
                                    {
                                        //Partial Aggro
                                        GlobalApplyMapKeyLighting("PrintScreen", col_em3, false);
                                        GlobalUpdateBulbState(4, col_em3, 1000);
                                        GlobalApplyMapKeyLighting("Scroll", col_em3, false);
                                        GlobalApplyMapKeyLighting("Pause", col_em3, false);
                                        GlobalApplyMapKeyLighting("Macro16", col_em3, false);
                                        GlobalApplyMapKeyLighting("Macro17", col_em3, false);
                                        GlobalApplyMapKeyLighting("Macro18", col_em3, false);
                                        GlobalApplyMapMouseLighting("Logo", col_em3, false);
                                        GlobalApplyMapLogoLighting("", col_em3, false);
                                    }
                                    else if (EmnityPosition == 0)
                                    {
                                        //Full Aggro
                                        GlobalApplyMapKeyLighting("PrintScreen", col_em4, false);
                                        GlobalUpdateBulbState(4, col_em4, 1000);
                                        GlobalApplyMapKeyLighting("Scroll", col_em4, false);
                                        GlobalApplyMapKeyLighting("Pause", col_em4, false);
                                        GlobalApplyMapKeyLighting("Macro16", col_em4, false);
                                        GlobalApplyMapKeyLighting("Macro17", col_em4, false);
                                        GlobalApplyMapKeyLighting("Macro18", col_em4, false);
                                        GlobalApplyMapMouseLighting("Logo", col_em4, false);
                                        GlobalApplyMapLogoLighting("", col_em4, false);
                                    }
                                }
                                else
                                {
                                    //Not Engaged/No aggro
                                    GlobalApplyMapKeyLighting("PrintScreen", col_em0, false);
                                    GlobalUpdateBulbState(4, col_em0, 1000);
                                    GlobalApplyMapKeyLighting("Scroll", col_em0, false);
                                    GlobalApplyMapKeyLighting("Pause", col_em0, false);
                                    GlobalApplyMapKeyLighting("Macro16", col_em0, false);
                                    GlobalApplyMapKeyLighting("Macro17", col_em0, false);
                                    GlobalApplyMapKeyLighting("Macro18", col_em0, false);
                                    GlobalApplyMapMouseLighting("Logo", col_em0, false);
                                    GlobalApplyMapLogoLighting("", col_em0, false);
                                }

                                //if (castalert == 1) { castalert = 0; }
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("PrintScreen",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                            GlobalUpdateBulbState(4, ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), 250);
                            GlobalApplyMapKeyLighting("Scroll",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                            GlobalApplyMapKeyLighting("Pause",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity),
                                false);
                            GlobalApplyMapKeyLighting("Macro16",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                            GlobalApplyMapKeyLighting("Macro17",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                            GlobalApplyMapKeyLighting("Macro18",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_NoEmnity), false);
                            GlobalApplyMapMouseLighting("Logo", HighlightColor, false);
                            GlobalApplyMapLogoLighting("", HighlightColor, false);

                            GlobalApplyMapKeyLighting("Macro1", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Macro2", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Macro3", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Macro4", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Macro5", _BaseColor, false);
                            GlobalUpdateBulbState(4, _BaseColor, 1000);
                        }
                    }
                    else
                    {
                        GlobalApplyMapKeyLighting("Macro1", _BaseColor, false);
                        GlobalApplyMapKeyLighting("Macro2", _BaseColor, false);
                        GlobalApplyMapKeyLighting("Macro3", _BaseColor, false);
                        GlobalApplyMapKeyLighting("Macro4", _BaseColor, false);
                        GlobalApplyMapKeyLighting("Macro5", _BaseColor, false);
                        GlobalUpdateBulbState(5, _BaseColor, 1000);
                    }


                    //Castbar
                    var CastPercentage = PlayerInfo.CastingPercentage;
                    var pol_CastX = (CastPercentage - 0) * (12 - 0) / (1.0 - 0.0) + 0;
                    var pol_Cast = Convert.ToInt32(pol_CastX);
                    var _pol_CastZ = (CastPercentage - 0) * (65535 - 0) / (1.0 - 0.0) + 0;
                    var pol_CastZ = Convert.ToInt32(_pol_CastZ);


                    if (PlayerInfo.IsCasting)
                    {
                        //Console.WriteLine(CastPercentage);
                        lastcast = true;

                        _lifx.LIFXUpdateStateBrightness(10, col_castcharge, (ushort) pol_CastZ, 250);

                        if (pol_Cast <= 1 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F2",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F3",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F4",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F5",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F6",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F7",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F8",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F9",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 2 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F3",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F4",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F5",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F6",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F7",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F8",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F9",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 3 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F4",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F5",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F6",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F7",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F8",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F9",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 4 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F5",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F6",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F7",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F8",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F9",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 5 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F5", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F6",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F7",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F8",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F9",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 6 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F5", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F6", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F7",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F8",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F9",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 7 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F5", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F6", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F7", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F8",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F9",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 8 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F5", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F6", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F7", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F8", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F9",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 9 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F5", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F6", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F7", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F8", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F9", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F10",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 10 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F5", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F6", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F7", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F8", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F9", col_castcharge, false);
                            GlobalApplyMapKeyLighting("F10", col_castcharge, false);
                            /*
                            GlobalApplyMapKeyLighting("F11",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                            GlobalApplyMapKeyLighting("F12",
                                ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                */
                        }
                        else if (pol_Cast == 11 && ChromaticsSettings.ChromaticsSettings_CastToggle)
                        {
                            if (ChromaticsSettings.ChromaticsSettings_CastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F5", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F6", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F7", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F8", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F9", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F10", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F11", col_castcharge, false);
                                /*
                                GlobalApplyMapKeyLighting("F12",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                    */
                            }
                            successcast = true;
                        }
                        else if (pol_Cast >= 12)
                        {
                            if (ChromaticsSettings.ChromaticsSettings_CastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F2", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F3", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F4", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F5", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F6", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F7", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F8", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F9", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F10", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F11", col_castcharge, false);
                                GlobalApplyMapKeyLighting("F12", col_castcharge, false);
                            }
                            successcast = true;
                        }
                    }
                    else
                    {
                        if (lastcast)
                        {
                            if (ChromaticsSettings.ChromaticsSettings_CastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F2", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F3", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F4", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F5", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F6", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F7", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F8", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F9", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F10", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F11", _BaseColor, false);
                                GlobalApplyMapKeyLighting("F12", _BaseColor, false);

                            }

                            var _cBulbRip1 = new Task(() => { GlobalUpdateBulbState(10, _BaseColor, 500); });
                            MemoryTasks.Add(_cBulbRip1);
                            MemoryTasks.Run(_cBulbRip1);
                            

                            if (successcast && ChromaticsSettings.ChromaticsSettings_CastAnimate)
                                GlobalRipple1(col_castcharge, 80, _BaseColor);

                            lastcast = false;
                            successcast = false;
                        }
                    }

                    //HP
                    if (max_HP != 0)
                    {
                        var pol_HP = (current_HP - 0) * (40 - 0) / (max_HP - 0) + 0;
                        var pol_HPX = (current_HP - 0) * (70 - 0) / (max_HP - 0) + 0;
                        var pol_HPZ = (current_HP - 0) * (65535 - 0) / (max_HP - 0) + 0;

                        if (pol_HP <= 10)
                            _lifx.LIFXUpdateStateBrightness(7, col_hpempty, (ushort) pol_HPZ, 250);
                        else
                            _lifx.LIFXUpdateStateBrightness(7, col_hpfull, (ushort) pol_HPZ, 250);

                        if (pol_HP <= 40 && pol_HP > 30)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F1", col_hpfull, false);
                                GlobalApplyMapKeyLighting("F2", col_hpfull, false);
                                GlobalApplyMapKeyLighting("F3", col_hpfull, false);
                                GlobalApplyMapKeyLighting("F4", col_hpfull, false);
                            }
                            GlobalApplyMapPadLighting(14, col_hpfull, false);
                            GlobalApplyMapPadLighting(13, col_hpfull, false);
                            GlobalApplyMapPadLighting(12, col_hpfull, false);
                            GlobalApplyMapPadLighting(11, col_hpfull, false);
                            GlobalApplyMapPadLighting(10, col_hpfull, false);
                        }
                        else if (pol_HP <= 30 && pol_HP > 20)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F1", col_hpfull, false);
                                GlobalApplyMapKeyLighting("F2", col_hpfull, false);
                                GlobalApplyMapKeyLighting("F3", col_hpfull, false);
                                GlobalApplyMapKeyLighting("F4", col_hpempty, false);
                            }
                            GlobalApplyMapPadLighting(14, col_hpempty, false);
                            GlobalApplyMapPadLighting(13, col_hpempty, false);
                            GlobalApplyMapPadLighting(12, col_hpfull, false);
                            GlobalApplyMapPadLighting(11, col_hpfull, false);
                            GlobalApplyMapPadLighting(10, col_hpfull, false);
                        }
                        else if (pol_HP <= 20 && pol_HP > 10)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F1", col_hpfull, false);
                                GlobalApplyMapKeyLighting("F2", col_hpfull, false);
                                GlobalApplyMapKeyLighting("F3", col_hpempty, false);
                                GlobalApplyMapKeyLighting("F4", col_hpempty, false);
                            }
                            GlobalApplyMapPadLighting(14, col_hpempty, false);
                            GlobalApplyMapPadLighting(13, col_hpempty, false);
                            GlobalApplyMapPadLighting(12, col_hpempty, false);
                            GlobalApplyMapPadLighting(11, col_hpfull, false);
                            GlobalApplyMapPadLighting(10, col_hpfull, false);
                        }
                        else if (pol_HP <= 10 && pol_HP > 0)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F1", col_hpcritical, false);
                                GlobalApplyMapKeyLighting("F2", col_hpempty, false);
                                GlobalApplyMapKeyLighting("F3", col_hpempty, false);
                                GlobalApplyMapKeyLighting("F4", col_hpempty, false);
                            }
                            GlobalApplyMapPadLighting(14, col_hpempty, false);
                            GlobalApplyMapPadLighting(13, col_hpempty, false);
                            GlobalApplyMapPadLighting(12, col_hpempty, false);
                            GlobalApplyMapPadLighting(11, col_hpempty, false);
                            GlobalApplyMapPadLighting(10, col_hpcritical, false);
                        }
                        else if (pol_HP == 0)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F1", col_hpcritical, false);
                                GlobalApplyMapKeyLighting("F2", col_hpcritical, false);
                                GlobalApplyMapKeyLighting("F3", col_hpcritical, false);
                                GlobalApplyMapKeyLighting("F4", col_hpcritical, false);
                            }

                            GlobalApplyMapPadLighting(14, col_hpcritical, false);
                            GlobalApplyMapPadLighting(13, col_hpcritical, false);
                            GlobalApplyMapPadLighting(12, col_hpcritical, false);
                            GlobalApplyMapPadLighting(11, col_hpcritical, false);
                            GlobalApplyMapPadLighting(10, col_hpcritical, false);
                        }

                        //Mouse
                        if (pol_HPX <= 70 && pol_HPX > 60)
                        {
                            GlobalApplyMapMouseLighting("Strip7", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip6", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip5", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip4", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip3", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip2", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip1", col_hpfull, false);
                        }
                        else if (pol_HPX <= 60 && pol_HPX > 50)
                        {
                            GlobalApplyMapMouseLighting("Strip7", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip6", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip5", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip4", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip3", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip2", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip1", col_hpempty, false);
                        }
                        else if (pol_HPX <= 50 && pol_HPX > 40)
                        {
                            GlobalApplyMapMouseLighting("Strip7", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip6", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip5", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip4", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip3", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip2", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip1", col_hpempty, false);
                        }
                        else if (pol_HPX <= 40 && pol_HPX > 30)
                        {
                            GlobalApplyMapMouseLighting("Strip7", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip6", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip5", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip4", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip3", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip2", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip1", col_hpempty, false);
                        }
                        else if (pol_HPX <= 30 && pol_HPX > 20)
                        {
                            GlobalApplyMapMouseLighting("Strip7", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip6", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip5", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip4", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip3", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip2", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip1", col_hpempty, false);
                        }
                        else if (pol_HPX <= 20 && pol_HPX > 10)
                        {
                            GlobalApplyMapMouseLighting("Strip7", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip6", col_hpfull, false);
                            GlobalApplyMapMouseLighting("Strip5", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip4", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip3", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip2", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip1", col_hpempty, false);
                        }
                        else if (pol_HPX <= 10 && pol_HPX > 0)
                        {
                            GlobalApplyMapMouseLighting("Strip7", col_hpcritical, false);
                            GlobalApplyMapMouseLighting("Strip6", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip5", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip4", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip3", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip2", col_hpempty, false);
                            GlobalApplyMapMouseLighting("Strip1", col_hpempty, false);
                        }
                        else if (pol_HPX == 0)
                        {
                            GlobalApplyMapMouseLighting("Strip7", col_hpcritical, false);
                            GlobalApplyMapMouseLighting("Strip6", col_hpcritical, false);
                            GlobalApplyMapMouseLighting("Strip5", col_hpcritical, false);
                            GlobalApplyMapMouseLighting("Strip4", col_hpcritical, false);
                            GlobalApplyMapMouseLighting("Strip3", col_hpcritical, false);
                            GlobalApplyMapMouseLighting("Strip2", col_hpcritical, false);
                            GlobalApplyMapMouseLighting("Strip1", col_hpcritical, false);
                        }
                    }

                    //MP
                    if (max_MP != 0)
                    {
                        var pol_MP = (current_MP - 0) * (40 - 0) / (max_MP - 0) + 0;
                        var pol_MPX = (current_MP - 0) * (70 - 0) / (max_MP - 0) + 0;
                        var pol_MPZ = (current_MP - 0) * (65535 - 0) / (max_MP - 0) + 0;

                        _lifx.LIFXUpdateStateBrightness(8, col_mpfull, (ushort) pol_MPZ, 250);

                        if (pol_MP <= 40 && pol_MP > 30)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F5", col_mpfull, false);
                                GlobalApplyMapKeyLighting("F6", col_mpfull, false);
                                GlobalApplyMapKeyLighting("F7", col_mpfull, false);
                                GlobalApplyMapKeyLighting("F8", col_mpfull, false);
                            }
                            GlobalApplyMapPadLighting(5, col_mpfull, false);
                            GlobalApplyMapPadLighting(6, col_mpfull, false);
                            GlobalApplyMapPadLighting(7, col_mpfull, false);
                            GlobalApplyMapPadLighting(8, col_mpfull, false);
                            GlobalApplyMapPadLighting(9, col_mpfull, false);
                        }
                        else if (pol_MP <= 30 && pol_MP > 20)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F5", col_mpfull, false);
                                GlobalApplyMapKeyLighting("F6", col_mpfull, false);
                                GlobalApplyMapKeyLighting("F7", col_mpfull, false);
                                GlobalApplyMapKeyLighting("F8", col_mpempty, false);
                            }
                            GlobalApplyMapPadLighting(5, col_mpempty, false);
                            GlobalApplyMapPadLighting(6, col_mpempty, false);
                            GlobalApplyMapPadLighting(7, col_mpfull, false);
                            GlobalApplyMapPadLighting(8, col_mpfull, false);
                            GlobalApplyMapPadLighting(9, col_mpfull, false);
                        }
                        else if (pol_MP <= 20 && pol_MP > 10)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F5", col_mpfull, false);
                                GlobalApplyMapKeyLighting("F6", col_mpfull, false);
                                GlobalApplyMapKeyLighting("F7", col_mpempty, false);
                                GlobalApplyMapKeyLighting("F8", col_mpempty, false);
                            }
                            GlobalApplyMapPadLighting(5, col_mpempty, false);
                            GlobalApplyMapPadLighting(6, col_mpempty, false);
                            GlobalApplyMapPadLighting(7, col_mpempty, false);
                            GlobalApplyMapPadLighting(8, col_mpfull, false);
                            GlobalApplyMapPadLighting(9, col_mpfull, false);
                        }
                        else if (pol_MP <= 10 && pol_MP > 0)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F5", col_mpempty, false);
                                GlobalApplyMapKeyLighting("F6", col_mpempty, false);
                                GlobalApplyMapKeyLighting("F7", col_mpempty, false);
                                GlobalApplyMapKeyLighting("F8", col_mpempty, false);
                            }
                            GlobalApplyMapPadLighting(5, col_mpempty, false);
                            GlobalApplyMapPadLighting(6, col_mpempty, false);
                            GlobalApplyMapPadLighting(7, col_mpempty, false);
                            GlobalApplyMapPadLighting(8, col_mpempty, false);
                            GlobalApplyMapPadLighting(9, col_mpempty, false);
                        }
                        else if (pol_MP == 0)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F5", col_mpempty, false);
                                GlobalApplyMapKeyLighting("F6", col_mpempty, false);
                                GlobalApplyMapKeyLighting("F7", col_mpempty, false);
                                GlobalApplyMapKeyLighting("F8", col_mpempty, false);
                            }
                            GlobalApplyMapPadLighting(5, col_mpempty, false);
                            GlobalApplyMapPadLighting(6, col_mpempty, false);
                            GlobalApplyMapPadLighting(7, col_mpempty, false);
                            GlobalApplyMapPadLighting(8, col_mpempty, false);
                            GlobalApplyMapPadLighting(9, col_mpempty, false);
                        }

                        //Mouse
                        if (MouseToggle == 0)
                            if (pol_MPX <= 70 && pol_MPX > 60)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip10", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip9", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip8", col_mpfull, false);
                            }
                            else if (pol_MPX <= 60 && pol_MPX > 50)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip10", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip9", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip8", col_mpempty, false);
                            }
                            else if (pol_MPX <= 50 && pol_MPX > 40)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip10", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip9", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_mpempty, false);
                            }
                            else if (pol_MPX <= 40 && pol_MPX > 30)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip10", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_mpempty, false);
                            }
                            else if (pol_MPX <= 30 && pol_MPX > 20)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip10", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_mpempty, false);
                            }
                            else if (pol_MPX <= 20 && pol_MPX > 10)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_mpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip11", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip10", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_mpempty, false);
                            }
                            else if (pol_MPX <= 10 && pol_MPX > 0)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip13", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip12", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip11", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip10", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_mpempty, false);
                            }
                            else if (pol_MPX == 0)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip13", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip12", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip11", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip10", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_mpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_mpempty, false);
                            }
                    }

                    //TP
                    if (max_TP != 0)
                    {
                        var pol_TP = (current_TP - 0) * (40 - 0) / (max_TP - 0) + 0;
                        var pol_TPX = (current_TP - 0) * (70 - 0) / (max_TP - 0) + 0;
                        var pol_TPZ = (current_TP - 0) * (65535 - 0) / (max_TP - 0) + 0;

                        _lifx.LIFXUpdateStateBrightness(9, col_tpfull, (ushort) pol_TPZ, 250);

                        if (pol_TP <= 40 && pol_TP > 30)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F9", col_tpfull, false);
                                GlobalApplyMapKeyLighting("F10", col_tpfull, false);
                                GlobalApplyMapKeyLighting("F11", col_tpfull, false);
                                GlobalApplyMapKeyLighting("F12", col_tpfull, false);
                            }
                            GlobalApplyMapPadLighting(0, col_tpfull, false);
                            GlobalApplyMapPadLighting(1, col_tpfull, false);
                            GlobalApplyMapPadLighting(2, col_tpfull, false);
                            GlobalApplyMapPadLighting(3, col_tpfull, false);
                            GlobalApplyMapPadLighting(4, col_tpfull, false);
                        }
                        else if (pol_TP <= 30 && pol_TP > 20)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F9", col_tpfull, false);
                                GlobalApplyMapKeyLighting("F10", col_tpfull, false);
                                GlobalApplyMapKeyLighting("F11", col_tpfull, false);
                                GlobalApplyMapKeyLighting("F12", col_tpempty, false);
                            }
                            GlobalApplyMapPadLighting(0, col_tpempty, false);
                            GlobalApplyMapPadLighting(1, col_tpempty, false);
                            GlobalApplyMapPadLighting(2, col_tpfull, false);
                            GlobalApplyMapPadLighting(3, col_tpfull, false);
                            GlobalApplyMapPadLighting(4, col_tpfull, false);
                        }
                        else if (pol_TP <= 20 && pol_TP > 10)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F9", col_tpfull, false);
                                GlobalApplyMapKeyLighting("F10", col_tpfull, false);
                                GlobalApplyMapKeyLighting("F11", col_tpempty, false);
                                GlobalApplyMapKeyLighting("F12", col_tpempty, false);
                            }
                            GlobalApplyMapPadLighting(0, col_tpempty, false);
                            GlobalApplyMapPadLighting(1, col_tpempty, false);
                            GlobalApplyMapPadLighting(2, col_tpempty, false);
                            GlobalApplyMapPadLighting(3, col_tpfull, false);
                            GlobalApplyMapPadLighting(4, col_tpfull, false);
                        }
                        else if (pol_TP <= 10 && pol_TP > 0)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F9", col_tpempty, false);
                                GlobalApplyMapKeyLighting("F10", col_tpempty, false);
                                GlobalApplyMapKeyLighting("F11", col_tpempty, false);
                                GlobalApplyMapKeyLighting("F12", col_tpempty, false);
                            }
                            GlobalApplyMapPadLighting(0, col_tpempty, false);
                            GlobalApplyMapPadLighting(1, col_tpempty, false);
                            GlobalApplyMapPadLighting(2, col_tpempty, false);
                            GlobalApplyMapPadLighting(3, col_tpempty, false);
                            GlobalApplyMapPadLighting(4, col_tpempty, false);
                        }
                        else if (pol_TP == 0)
                        {
                            if (!PlayerInfo.IsCasting)
                            {
                                GlobalApplyMapKeyLighting("F9", col_tpempty, false);
                                GlobalApplyMapKeyLighting("F10", col_tpempty, false);
                                GlobalApplyMapKeyLighting("F11", col_tpempty, false);
                                GlobalApplyMapKeyLighting("F12", col_tpempty, false);
                            }
                            GlobalApplyMapPadLighting(0, col_tpempty, false);
                            GlobalApplyMapPadLighting(1, col_tpempty, false);
                            GlobalApplyMapPadLighting(2, col_tpempty, false);
                            GlobalApplyMapPadLighting(3, col_tpempty, false);
                            GlobalApplyMapPadLighting(4, col_tpempty, false);
                        }

                        //Mouse
                        if (MouseToggle == 1)
                            if (pol_TPX <= 70 && pol_TPX > 60)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip10", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip9", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip8", col_tpfull, false);
                            }
                            else if (pol_TPX <= 60 && pol_TPX > 50)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip10", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip9", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip8", col_tpempty, false);
                            }
                            else if (pol_TPX <= 50 && pol_TPX > 40)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip10", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip9", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_tpempty, false);
                            }
                            else if (pol_TPX <= 40 && pol_TPX > 30)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip10", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_tpempty, false);
                            }
                            else if (pol_TPX <= 30 && pol_TPX > 20)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip11", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip10", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_tpempty, false);
                            }
                            else if (pol_TPX <= 20 && pol_TPX > 10)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip13", col_tpfull, false);
                                GlobalApplyMapMouseLighting("Strip12", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip11", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip10", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_tpempty, false);
                            }
                            else if (pol_TPX <= 10 && pol_TPX > 0)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip13", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip12", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip11", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip10", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_tpempty, false);
                            }
                            else if (pol_TPX == 0)
                            {
                                GlobalApplyMapMouseLighting("Strip14", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip13", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip12", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip11", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip10", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip9", col_tpempty, false);
                                GlobalApplyMapMouseLighting("Strip8", col_tpempty, false);
                            }

                        //Action Alerts
                        if (TargetInfo != null && TargetInfo.Type == Sharlayan.Core.Enums.Actor.Type.Monster)
                        {
                            if (TargetInfo.IsClaimed)
                            {
                                if (_hp != 0 && current_HP < _hp)
                                {
                                    _RzFl1CTS.Cancel();
                                    _CorsairF12CTS.Cancel();
                                    GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMapping_HPLoss), 100);
                                }
                            }
                        }

                        _hp = current_HP;

                    }

                    //DX11 Effects

                    if (isDX11)
                    {
                        FFXIVInterfaces.Cooldowns.refreshData();


                        //Hotbars        

                        ActionReadResult hotbars = new ActionReadResult();

                        hotbars = Reader.GetActions();

                        if (ChromaticsSettings.ChromaticsSettings_KeybindToggle)
                        {
                            FFXIVInterfaces.FFXIVHotbar.keybindwhitelist.Clear();

                            foreach (var hotbar in hotbars.ActionEntities)
                            {
                                if (hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_HOTBAR_1 || hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_HOTBAR_2 || hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_HOTBAR_3 ||
                                    hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_HOTBAR_4 || hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_HOTBAR_5 || hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_HOTBAR_6 ||
                                    hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_HOTBAR_7 || hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_HOTBAR_8 || hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.PETBAR ||
                                    hotbar.Type == Sharlayan.Core.Enums.HotBarRecast.Container.CROSS_PETBAR) continue;

                                foreach (var action in hotbar.Actions)
                                {
                                    if (!action.IsKeyBindAssigned) continue;

                                    //Collect Modifier Info
                                    var modsactive = action.Modifiers.Count;
                                    var _modsactive = modsactive;

                                    if (!FFXIVInterfaces.FFXIVHotbar.keybindwhitelist.Contains(action.ActionKey))
                                    {
                                        FFXIVInterfaces.FFXIVHotbar.keybindwhitelist.Add(action.ActionKey);
                                    }


                                    if (modsactive > 0)
                                    {
                                        foreach (var modifier in action.Modifiers)
                                        {
                                            if (modsactive == 0) break;

                                            if (modifier == "Ctrl")
                                            {
                                                if (KeyCtrl)
                                                {
                                                    _modsactive--;
                                                }
                                                else
                                                {
                                                    if (_modsactive < modsactive)
                                                        _modsactive++;
                                                }
                                            }

                                            if (modifier == "Alt")
                                            {
                                                if (KeyAlt)
                                                {
                                                    _modsactive--;
                                                }
                                                else
                                                {
                                                    if (_modsactive < modsactive)
                                                        _modsactive++;
                                                }
                                            }

                                            if (modifier == "Shift")
                                            {
                                                if (KeyShift)
                                                {
                                                    _modsactive--;
                                                }
                                                else
                                                {
                                                    if (_modsactive < modsactive)
                                                        _modsactive++;
                                                }
                                            }

                                        }
                                    }
                                    
                                    //Assign Lighting

                                    if (FFXIVInterfaces.FFXIVHotbar.keybindtranslation.ContainsKey(action.ActionKey))
                                    {
                                        var keyid = FFXIVInterfaces.FFXIVHotbar.keybindtranslation[action.ActionKey];

                                        if (_modsactive == 0)
                                        {

                                            if (action.IsAvailable || PlayerInfo.IsCasting)
                                            {
                                                if (action.InRange)
                                                {
                                                    if (action.IsProcOrCombo)
                                                    {
                                                        //Action Proc'd
                                                        GlobalApplyMapKeyLighting(keyid, ColorTranslator.FromHtml(ColorMappings.ColorMapping_HotbarProc), false, true);
                                                    }

                                                    else
                                                    {
                                                        if (action.CoolDownPercent > 0)
                                                        {
                                                            //Action Cooling Down
                                                            GlobalApplyMapKeyLighting(keyid, ColorTranslator.FromHtml(ColorMappings.ColorMapping_HotbarCD), false, true);
                                                        }
                                                        else
                                                        {
                                                            //Action Ready
                                                            GlobalApplyMapKeyLighting(keyid, ColorTranslator.FromHtml(ColorMappings.ColorMapping_HotbarReady), false, true);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    //Action Not In Range
                                                    GlobalApplyMapKeyLighting(keyid, ColorTranslator.FromHtml(ColorMappings.ColorMapping_HotbarOutRange), false, true);
                                                }
                                            }
                                            else
                                            {
                                                //Action Not Available
                                                GlobalApplyMapKeyLighting(keyid, ColorTranslator.FromHtml(ColorMappings.ColorMapping_HotbarNotAvailable), false, true);
                                            }
                                        }
                                    }

                                }
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("OemTilde", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D1", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D2", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D3", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D4", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D5", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D6", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D7", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D8", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D9", _BaseColor, false);
                            GlobalApplyMapKeyLighting("D0", _BaseColor, false);
                            GlobalApplyMapKeyLighting("OemMinus", _BaseColor, false);
                            GlobalApplyMapKeyLighting("OemEquals", _BaseColor, false);
                        }
                        

                        //Cooldowns
                        var gcd_hot = ColorTranslator.FromHtml(ColorMappings.ColorMapping_GCDHot);
                        var gcd_ready = ColorTranslator.FromHtml(ColorMappings.ColorMapping_GCDReady);
                        var gcd_empty = ColorTranslator.FromHtml(ColorMappings.ColorMapping_GCDEmpty);

                        var gcd_total = FFXIVInterfaces.Cooldowns.globalCooldownTotal;
                        var gcd_remain = FFXIVInterfaces.Cooldowns.globalCooldownRemaining;
                        var pol_gcd = (gcd_remain - 0) * (30 - 0) / (gcd_total - 0) + 0;

                        if (ChromaticsSettings.ChromaticsSettings_GCDCountdown)
                        {
                            if (!FFXIVInterfaces.Cooldowns.globalCooldownReady)
                            {
                                if (pol_gcd <= 30 && pol_gcd > 20)
                                {
                                    GlobalApplyMapKeyLighting("PageUp", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("PageDown", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("Home", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("End", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("Insert", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("Delete", gcd_hot, false);
                                }
                                else if (pol_gcd <= 20 && pol_gcd > 10)
                                {
                                    GlobalApplyMapKeyLighting("PageUp", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("PageDown", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("Home", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("End", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("Insert", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("Delete", gcd_hot, false);
                                }
                                else if (pol_gcd <= 10 && pol_gcd > 0)
                                {
                                    GlobalApplyMapKeyLighting("PageUp", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("PageDown", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("Home", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("End", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("Insert", gcd_hot, false);
                                    GlobalApplyMapKeyLighting("Delete", gcd_hot, false);
                                }
                                else if (pol_gcd == 0)
                                {
                                    GlobalApplyMapKeyLighting("PageUp", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("PageDown", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("Home", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("End", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("Insert", gcd_empty, false);
                                    GlobalApplyMapKeyLighting("Delete", gcd_empty, false);
                                }
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("PageUp", gcd_ready, false);
                                GlobalApplyMapKeyLighting("PageDown", gcd_ready, false);
                                GlobalApplyMapKeyLighting("Home", gcd_ready, false);
                                GlobalApplyMapKeyLighting("End", gcd_ready, false);
                                GlobalApplyMapKeyLighting("Insert", gcd_ready, false);
                                GlobalApplyMapKeyLighting("Delete", gcd_ready, false);
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("PageUp", gcd_ready, false);
                            GlobalApplyMapKeyLighting("PageDown", gcd_ready, false);
                            GlobalApplyMapKeyLighting("Home", gcd_ready, false);
                            GlobalApplyMapKeyLighting("End", gcd_ready, false);
                            GlobalApplyMapKeyLighting("Insert", gcd_ready, false);
                            GlobalApplyMapKeyLighting("Delete", gcd_ready, false);
                        }

                        //Job Gauges

                        if (ChromaticsSettings.ChromaticsSettings_JobGaugeToggle)
                        {

                            switch (PlayerInfo.Job)
                            {
                                case Sharlayan.Core.Enums.Actor.Job.WAR:
                                    var burstwarcol = Color.Orange;
                                    var maxwarcol = Color.Red;
                                    var negwarcol = Color.Black;
                                    var wrath = FFXIVInterfaces.Cooldowns.Wrath;
                                    var pol_wrath = (wrath - 0) * (50 - 0) / (100 - 0) + 0;

                                    if (wrath > 0)
                                    {
                                        if (pol_wrath >= 50)
                                        {
                                            //Flash
                                            ToggleGlobalFlash3(true);
                                            GlobalFlash3(maxwarcol, 150);
                                        }
                                        else if (pol_wrath < 50 && pol_wrath > 40)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", burstwarcol, false);

                                            GlobalApplyMapKeyLighting("Num7", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num8", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num9", burstwarcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstwarcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstwarcol, false);
                                        }
                                        else if (pol_wrath <= 40 && pol_wrath > 30)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num7", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num8", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num9", burstwarcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstwarcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstwarcol, false);
                                        }
                                        else if (pol_wrath <= 30 && pol_wrath > 20)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstwarcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstwarcol, false);
                                        }
                                        else if (pol_wrath <= 20 && pol_wrath > 10)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num4", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num5", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num6", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstwarcol, false);
                                        }
                                        else if (pol_wrath <= 10 && pol_wrath > 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num4", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num5", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num6", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstwarcol, false);
                                        }
                                        else if (pol_wrath == 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num4", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num5", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num6", negwarcol, false);

                                            GlobalApplyMapKeyLighting("Num1", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num2", negwarcol, false);
                                            GlobalApplyMapKeyLighting("Num3", negwarcol, false);
                                        }
                                    }
                                    else
                                    {
                                        ToggleGlobalFlash3(false);

                                        GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                        GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                        GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                        GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                        GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                        GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                        GlobalApplyMapKeyLighting("Num4", negwarcol, false);
                                        GlobalApplyMapKeyLighting("Num5", negwarcol, false);
                                        GlobalApplyMapKeyLighting("Num6", negwarcol, false);

                                        GlobalApplyMapKeyLighting("Num1", negwarcol, false);
                                        GlobalApplyMapKeyLighting("Num2", negwarcol, false);
                                        GlobalApplyMapKeyLighting("Num3", negwarcol, false);
                                    }

                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.PLD:
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.MNK:
                                    var greased = FFXIVInterfaces.Cooldowns.GreasedLightningStacks;
                                    var grease_remaining = FFXIVInterfaces.Cooldowns.GreasedLightningTimeRemaining;
                                    var burstmnkcol = Color.Aqua;
                                    var burstmnkempty = Color.Black;

                                    if (greased > 0)
                                    {
                                        if (grease_remaining > 0 && grease_remaining <= 5)
                                        {
                                            ToggleGlobalFlash3(true);
                                            GlobalFlash3(burstmnkcol, 150);
                                        }
                                        else
                                        {
                                            ToggleGlobalFlash3(false);

                                            switch (greased)
                                            {
                                                case 3:
                                                    GlobalApplyMapKeyLighting("Num9", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num6", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num3", burstmnkcol, false);

                                                    GlobalApplyMapKeyLighting("Num8", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num5", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num2", burstmnkcol, false);

                                                    GlobalApplyMapKeyLighting("Num7", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num4", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num1", burstmnkcol, false);
                                                    break;
                                                case 2:
                                                    GlobalApplyMapKeyLighting("Num9", burstmnkempty, false);
                                                    GlobalApplyMapKeyLighting("Num6", burstmnkempty, false);
                                                    GlobalApplyMapKeyLighting("Num3", burstmnkempty, false);

                                                    GlobalApplyMapKeyLighting("Num8", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num5", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num2", burstmnkcol, false);

                                                    GlobalApplyMapKeyLighting("Num7", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num4", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num1", burstmnkcol, false);
                                                    break;
                                                case 1:
                                                    GlobalApplyMapKeyLighting("Num9", burstmnkempty, false);
                                                    GlobalApplyMapKeyLighting("Num6", burstmnkempty, false);
                                                    GlobalApplyMapKeyLighting("Num3", burstmnkempty, false);

                                                    GlobalApplyMapKeyLighting("Num8", burstmnkempty, false);
                                                    GlobalApplyMapKeyLighting("Num5", burstmnkempty, false);
                                                    GlobalApplyMapKeyLighting("Num2", burstmnkempty, false);

                                                    GlobalApplyMapKeyLighting("Num7", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num4", burstmnkcol, false);
                                                    GlobalApplyMapKeyLighting("Num1", burstmnkcol, false);
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        ToggleGlobalFlash3(false);

                                        GlobalApplyMapKeyLighting("Num9", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num6", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num3", burstmnkempty, false);

                                        GlobalApplyMapKeyLighting("Num8", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num5", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num2", burstmnkempty, false);

                                        GlobalApplyMapKeyLighting("Num7", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num4", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num1", burstmnkempty, false);
                                    }

                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.DRG:

                                    var burstdrgcol = Color.Red;
                                    var negdrgcol = Color.Black;
                                    var bloodremain = FFXIVInterfaces.Cooldowns.BloodOfTheDragonTimeRemaining;
                                    var pol_blood = (bloodremain - 0) * (50 - 0) / (30 - 0) + 0;

                                    if (bloodremain > 0)
                                    {
                                        if (pol_blood <= 50 && pol_blood > 40)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", burstdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num7", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num8", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num9", burstdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                                        }
                                        else if (pol_blood <= 40 && pol_blood > 30)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num7", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num8", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num9", burstdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                                        }
                                        else if (pol_blood <= 30 && pol_blood > 20)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                                        }
                                        else if (pol_blood <= 20 && pol_blood > 10)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num4", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num5", negdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num6", negdrgcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                                        }
                                        else if (pol_blood <= 10 && pol_blood > 0)
                                        {
                                            //Flash
                                            ToggleGlobalFlash3(true);
                                            GlobalFlash3(burstdrgcol, 150);
                                        }
                                    }
                                    else
                                    {
                                        ToggleGlobalFlash3(false);

                                        GlobalApplyMapKeyLighting("NumLock", negdrgcol, false);
                                        GlobalApplyMapKeyLighting("NumDivide", negdrgcol, false);
                                        GlobalApplyMapKeyLighting("NumMultiply", negdrgcol, false);

                                        GlobalApplyMapKeyLighting("Num7", negdrgcol, false);
                                        GlobalApplyMapKeyLighting("Num8", negdrgcol, false);
                                        GlobalApplyMapKeyLighting("Num9", negdrgcol, false);

                                        GlobalApplyMapKeyLighting("Num4", negdrgcol, false);
                                        GlobalApplyMapKeyLighting("Num5", negdrgcol, false);
                                        GlobalApplyMapKeyLighting("Num6", negdrgcol, false);

                                        GlobalApplyMapKeyLighting("Num1", negdrgcol, false);
                                        GlobalApplyMapKeyLighting("Num2", negdrgcol, false);
                                        GlobalApplyMapKeyLighting("Num3", negdrgcol, false);
                                    }
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.BRD:
                                    //Bard Songs
                                    var burstcol = Color.Black;
                                    var negcol = Color.Black;

                                    if (FFXIVInterfaces.Cooldowns.Song != FFXIVInterfaces.Cooldowns.BardSongs.None)
                                    {
                                        var songremain = FFXIVInterfaces.Cooldowns.SongTimeRemaining;
                                        var pol_song = (songremain - 0) * (50 - 0) / (30 - 0) + 0;

                                        switch (FFXIVInterfaces.Cooldowns.Song)
                                        {
                                            case FFXIVInterfaces.Cooldowns.BardSongs.ArmysPaeon:
                                                burstcol = Color.Orange;
                                                negcol = Color.Black;
                                                break;
                                            case FFXIVInterfaces.Cooldowns.BardSongs.MagesBallad:
                                                burstcol = Color.MediumSlateBlue;
                                                negcol = Color.Black;
                                                break;
                                            case FFXIVInterfaces.Cooldowns.BardSongs.WanderersMinuet:
                                                burstcol = Color.MediumSpringGreen;
                                                negcol = Color.Black;
                                                break;
                                        }

                                        if (pol_song <= 50 && pol_song > 40)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", burstcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", burstcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", burstcol, false);

                                            GlobalApplyMapKeyLighting("Num7", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num8", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num9", burstcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstcol, false);
                                        }
                                        else if (pol_song <= 40 && pol_song > 30)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negcol, false);

                                            GlobalApplyMapKeyLighting("Num7", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num8", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num9", burstcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstcol, false);
                                        }
                                        else if (pol_song <= 30 && pol_song > 20)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negcol, false);

                                            GlobalApplyMapKeyLighting("Num4", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstcol, false);
                                        }
                                        else if (pol_song <= 20 && pol_song > 10)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negcol, false);

                                            GlobalApplyMapKeyLighting("Num4", negcol, false);
                                            GlobalApplyMapKeyLighting("Num5", negcol, false);
                                            GlobalApplyMapKeyLighting("Num6", negcol, false);

                                            GlobalApplyMapKeyLighting("Num1", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstcol, false);
                                        }
                                        else if (pol_song <= 10 && pol_song > 0)
                                        {
                                            //Flash
                                            ToggleGlobalFlash3(true);
                                            GlobalFlash3(burstcol, 150);
                                        }
                                    }
                                    else
                                    {
                                        ToggleGlobalFlash3(false);

                                        GlobalApplyMapKeyLighting("NumLock", negcol, false);
                                        GlobalApplyMapKeyLighting("NumDivide", negcol, false);
                                        GlobalApplyMapKeyLighting("NumMultiply", negcol, false);

                                        GlobalApplyMapKeyLighting("Num7", negcol, false);
                                        GlobalApplyMapKeyLighting("Num8", negcol, false);
                                        GlobalApplyMapKeyLighting("Num9", negcol, false);

                                        GlobalApplyMapKeyLighting("Num4", negcol, false);
                                        GlobalApplyMapKeyLighting("Num5", negcol, false);
                                        GlobalApplyMapKeyLighting("Num6", negcol, false);

                                        GlobalApplyMapKeyLighting("Num1", negcol, false);
                                        GlobalApplyMapKeyLighting("Num2", negcol, false);
                                        GlobalApplyMapKeyLighting("Num3", negcol, false);
                                    }
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.WHM:
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.BLM:
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.SMN:
                                    var aetherflowsmn = FFXIVInterfaces.Cooldowns.AetherflowCount;

                                    var burstsmncol = Color.Orchid;
                                    var burstsmnempty = Color.Black;

                                    if (aetherflowsmn > 0)
                                    {
                                        switch (aetherflowsmn)
                                        {
                                            case 3:
                                                GlobalApplyMapKeyLighting("Num9", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num6", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num3", burstsmncol, false);

                                                GlobalApplyMapKeyLighting("Num8", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num5", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num2", burstsmncol, false);

                                                GlobalApplyMapKeyLighting("Num7", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num4", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num1", burstsmncol, false);
                                                break;
                                            case 2:
                                                GlobalApplyMapKeyLighting("Num9", burstsmnempty, false);
                                                GlobalApplyMapKeyLighting("Num6", burstsmnempty, false);
                                                GlobalApplyMapKeyLighting("Num3", burstsmnempty, false);

                                                GlobalApplyMapKeyLighting("Num8", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num5", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num2", burstsmncol, false);

                                                GlobalApplyMapKeyLighting("Num7", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num4", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num1", burstsmncol, false);
                                                break;
                                            case 1:
                                                GlobalApplyMapKeyLighting("Num9", burstsmnempty, false);
                                                GlobalApplyMapKeyLighting("Num6", burstsmnempty, false);
                                                GlobalApplyMapKeyLighting("Num3", burstsmnempty, false);

                                                GlobalApplyMapKeyLighting("Num8", burstsmnempty, false);
                                                GlobalApplyMapKeyLighting("Num5", burstsmnempty, false);
                                                GlobalApplyMapKeyLighting("Num2", burstsmnempty, false);

                                                GlobalApplyMapKeyLighting("Num7", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num4", burstsmncol, false);
                                                GlobalApplyMapKeyLighting("Num1", burstsmncol, false);
                                                break;
                                        }
                                    }
                                    else
                                    {

                                        GlobalApplyMapKeyLighting("Num9", burstsmnempty, false);
                                        GlobalApplyMapKeyLighting("Num6", burstsmnempty, false);
                                        GlobalApplyMapKeyLighting("Num3", burstsmnempty, false);

                                        GlobalApplyMapKeyLighting("Num8", burstsmnempty, false);
                                        GlobalApplyMapKeyLighting("Num5", burstsmnempty, false);
                                        GlobalApplyMapKeyLighting("Num2", burstsmnempty, false);

                                        GlobalApplyMapKeyLighting("Num7", burstsmnempty, false);
                                        GlobalApplyMapKeyLighting("Num4", burstsmnempty, false);
                                        GlobalApplyMapKeyLighting("Num1", burstsmnempty, false);
                                    }

                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.SCH:

                                    var aetherflowsch = FFXIVInterfaces.Cooldowns.AetherflowCount;

                                    var burstschcol = Color.Orchid;
                                    var burstschempty = Color.Black;

                                    if (aetherflowsch > 0)
                                    {
                                        switch (aetherflowsch)
                                        {
                                            case 3:
                                                GlobalApplyMapKeyLighting("Num9", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num6", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num3", burstschcol, false);

                                                GlobalApplyMapKeyLighting("Num8", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num5", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num2", burstschcol, false);

                                                GlobalApplyMapKeyLighting("Num7", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num4", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num1", burstschcol, false);
                                                break;
                                            case 2:
                                                GlobalApplyMapKeyLighting("Num9", burstschempty, false);
                                                GlobalApplyMapKeyLighting("Num6", burstschempty, false);
                                                GlobalApplyMapKeyLighting("Num3", burstschempty, false);

                                                GlobalApplyMapKeyLighting("Num8", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num5", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num2", burstschcol, false);

                                                GlobalApplyMapKeyLighting("Num7", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num4", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num1", burstschcol, false);
                                                break;
                                            case 1:
                                                GlobalApplyMapKeyLighting("Num9", burstschempty, false);
                                                GlobalApplyMapKeyLighting("Num6", burstschempty, false);
                                                GlobalApplyMapKeyLighting("Num3", burstschempty, false);

                                                GlobalApplyMapKeyLighting("Num8", burstschempty, false);
                                                GlobalApplyMapKeyLighting("Num5", burstschempty, false);
                                                GlobalApplyMapKeyLighting("Num2", burstschempty, false);

                                                GlobalApplyMapKeyLighting("Num7", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num4", burstschcol, false);
                                                GlobalApplyMapKeyLighting("Num1", burstschcol, false);
                                                break;
                                        }
                                    }
                                    else
                                    {

                                        GlobalApplyMapKeyLighting("Num9", burstschempty, false);
                                        GlobalApplyMapKeyLighting("Num6", burstschempty, false);
                                        GlobalApplyMapKeyLighting("Num3", burstschempty, false);

                                        GlobalApplyMapKeyLighting("Num8", burstschempty, false);
                                        GlobalApplyMapKeyLighting("Num5", burstschempty, false);
                                        GlobalApplyMapKeyLighting("Num2", burstschempty, false);

                                        GlobalApplyMapKeyLighting("Num7", burstschempty, false);
                                        GlobalApplyMapKeyLighting("Num4", burstschempty, false);
                                        GlobalApplyMapKeyLighting("Num1", burstschempty, false);
                                    }

                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.NIN:
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.DRK:
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.AST:
                                    var burstastcol = Color.Black;

                                    if (FFXIVInterfaces.Cooldowns.CurrentCard != FFXIVInterfaces.Cooldowns.CardTypes.None)
                                    {
                                        switch (FFXIVInterfaces.Cooldowns.CurrentCard)
                                        {
                                            case FFXIVInterfaces.Cooldowns.CardTypes.Arrow:
                                                burstastcol = Color.Lime;
                                                break;
                                            case FFXIVInterfaces.Cooldowns.CardTypes.Balance:
                                                burstastcol = Color.Crimson;
                                                break;
                                            case FFXIVInterfaces.Cooldowns.CardTypes.Bole:
                                                burstastcol = Color.Orange;
                                                break;
                                            case FFXIVInterfaces.Cooldowns.CardTypes.Ewer:
                                                burstastcol = Color.MediumBlue;
                                                break;
                                            case FFXIVInterfaces.Cooldowns.CardTypes.Spear:
                                                burstastcol = Color.Turquoise;
                                                break;
                                            case FFXIVInterfaces.Cooldowns.CardTypes.Spire:
                                                burstastcol = Color.SlateBlue;
                                                break;
                                        }

                                        if (FFXIVInterfaces.Cooldowns.CurrentCard != _CurrentCard)
                                        {
                                            if (FFXIVInterfaces.Cooldowns.CurrentCard != FFXIVInterfaces.Cooldowns.CardTypes.None)
                                            {
                                                GlobalRipple1(burstastcol, 80, _BaseColor);
                                            }

                                            _CurrentCard = FFXIVInterfaces.Cooldowns.CurrentCard;
                                        }

                                        GlobalApplyMapKeyLighting("NumLock", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumDivide", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumMultiply", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumSubtract", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num7", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num8", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num9", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumPlus", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num4", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num5", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num6", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumEnter", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num1", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num2", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num3", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num0", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumDecimal", burstastcol, false);
                                    }
                                    else
                                    {
                                        GlobalApplyMapKeyLighting("NumLock", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumDivide", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumMultiply", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumSubtract", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num7", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num8", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num9", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumPlus", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num4", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num5", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num6", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumEnter", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num1", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num2", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num3", burstastcol, false);
                                        GlobalApplyMapKeyLighting("Num0", burstastcol, false);
                                        GlobalApplyMapKeyLighting("NumDecimal", burstastcol, false);
                                    }
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.MCH:
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.SAM:
                                    break;
                                case Sharlayan.Core.Enums.Actor.Job.RDM:
                                    var blackmana = FFXIVInterfaces.Cooldowns.BlackMana;
                                    var whitemana = FFXIVInterfaces.Cooldowns.WhiteMana;
                                    var pol_black = (blackmana - 0) * (40 - 0) / (100 - 0) + 0;
                                    var pol_white = (whitemana - 0) * (40 - 0) / (100 - 0) + 0;

                                    var blackburst = Color.Red;
                                    var whiteburst = Color.White;
                                    var negburst = Color.Black;

                                    GlobalApplyMapKeyLighting("NumDivide", Color.Black, false);
                                    GlobalApplyMapKeyLighting("Num8", Color.Black, false);
                                    GlobalApplyMapKeyLighting("Num5", Color.Black, false);
                                    GlobalApplyMapKeyLighting("Num2", Color.Black, false);

                                    if (pol_black <= 40 && pol_black > 30)
                                    {
                                        GlobalApplyMapKeyLighting("NumMultiply", blackburst, false);
                                        GlobalApplyMapKeyLighting("Num9", blackburst, false);
                                        GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                        GlobalApplyMapKeyLighting("Num3", blackburst, false);
                                    }
                                    else if (pol_black <= 30 && pol_black > 20)
                                    {
                                        GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                        GlobalApplyMapKeyLighting("Num9", blackburst, false);
                                        GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                        GlobalApplyMapKeyLighting("Num3", blackburst, false);
                                    }
                                    else if (pol_black <= 20 && pol_black > 10)
                                    {
                                        GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                        GlobalApplyMapKeyLighting("Num9", negburst, false);
                                        GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                        GlobalApplyMapKeyLighting("Num3", blackburst, false);

                                    }
                                    else if (pol_black <= 10 && pol_black > 0)
                                    {
                                        GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                        GlobalApplyMapKeyLighting("Num9", negburst, false);
                                        GlobalApplyMapKeyLighting("Num6", negburst, false);
                                        GlobalApplyMapKeyLighting("Num3", blackburst, false);
                                    }
                                    else if (pol_black == 0)
                                    {
                                        GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                        GlobalApplyMapKeyLighting("Num9", negburst, false);
                                        GlobalApplyMapKeyLighting("Num6", negburst, false);
                                        GlobalApplyMapKeyLighting("Num3", negburst, false);
                                    }


                                    if (pol_white <= 40 && pol_white > 30)
                                    {
                                        GlobalApplyMapKeyLighting("NumLock", whiteburst, false);
                                        GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                                        GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                        GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                                    }
                                    else if (pol_white <= 30 && pol_white > 20)
                                    {
                                        GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                        GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                                        GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                        GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                                    }
                                    else if (pol_white <= 20 && pol_white > 10)
                                    {
                                        GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                        GlobalApplyMapKeyLighting("Num7", negburst, false);
                                        GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                        GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                                    }
                                    else if (pol_white <= 10 && pol_white > 0)
                                    {
                                        GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                        GlobalApplyMapKeyLighting("Num7", negburst, false);
                                        GlobalApplyMapKeyLighting("Num4", negburst, false);
                                        GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                                    }
                                    else if (pol_white == 0)
                                    {
                                        GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                        GlobalApplyMapKeyLighting("Num7", negburst, false);
                                        GlobalApplyMapKeyLighting("Num4", negburst, false);
                                        GlobalApplyMapKeyLighting("Num1", negburst, false);
                                    }

                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            ToggleGlobalFlash3(false);
                            GlobalApplyMapKeyLighting("NumLock", _BaseColor, false);
                            GlobalApplyMapKeyLighting("NumDivide", _BaseColor, false);
                            GlobalApplyMapKeyLighting("NumMultiply", _BaseColor, false);
                            GlobalApplyMapKeyLighting("NumSubtract", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num7", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num8", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num9", _BaseColor, false);
                            GlobalApplyMapKeyLighting("NumPlus", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num4", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num5", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num6", _BaseColor, false);
                            GlobalApplyMapKeyLighting("NumEnter", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num1", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num2", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num3", _BaseColor, false);
                            GlobalApplyMapKeyLighting("Num0", _BaseColor, false);
                            GlobalApplyMapKeyLighting("NumDecimal", _BaseColor, false);
                        }
                    }

                    if (CorsairSDKCalled == 1)
                    {
                        //CorsairUpdateLED();
                    }

                    GlobalKeyboardUpdate();
                    MemoryTasks.Cleanup();
                }
                else
                {
                    //Throw Main Menu
                    //FFXIVGameStop();

                    //Debug.WriteLine("Jump");
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.ERROR, "Parse Error: " + ex.Message);
                WriteConsole(ConsoleTypes.ERROR, "Internal Error: " + ex.StackTrace);
            }
        }
    }
}