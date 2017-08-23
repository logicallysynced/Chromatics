using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;
using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Core.Enums;
using Sharlayan.Models;


/* Contains the code to read the FFXIV Memory Stream, parse the data and convert to lighting commands
 * Sharlayan is used to access Actor and target information.
 * https://github.com/Icehunter/ffxivapp-memory
 */

namespace Chromatics
{
    partial class Chromatics
    {
        private Color _baseColor = Color.Black;

        private Cooldowns.CardTypes _currentCard;
        private string _currentStatus = "";
        private bool _dfcount;
        private bool _dfpop;
        private int _hp;
        private bool _targeted;
        private bool _castalert;

        /* Parse FFXIV Function
         * Read the data from Sharlayan and call lighting functions according
         */

        private bool _lastcast;
        private bool _menuNotify;

        //private static readonly object _ReadFFXIVMemory = new object();
        
        private ActorEntity _playerInfo = new ActorEntity();
        private PlayerEntity _menuInfo = new PlayerEntity();
        private ConcurrentDictionary<uint, ActorEntity> _playerInfoX = new ConcurrentDictionary<uint, ActorEntity>();

        private bool _playgroundonce;
        private bool _successcast;

        public void FfxivGameStop()
        {
            if (Attatched == 0) return;

            //Debug.WriteLine("Debug trip");

            HoldReader = true;
            _ffxiVcts.Cancel();
            _attachcts.Cancel();

            if (ArxSdkCalled == 1 && ArxState == 0)
                _arx.ArxSetIndex("info.html");

            MemoryTasks.Cleanup();

            //Watchdog.WatchdogStop();
            //_gameResetCatch.Enabled = false;
            WriteConsole(ConsoleTypes.Ffxiv, "Game stopped");


            Attatched = 0;
            ArxState = 0;
            Init = false;
            _gameNotify = false;
            _menuNotify = false;
            SetKeysbase = false;
            SetMousebase = false;

            MemoryHandler.Instance.UnsetProcess();
            _call = null;

            //HoldReader = false;

            GlobalUpdateState("static", Color.DeepSkyBlue, false);
            //GlobalUpdateState("wave", Color.Magenta, false, Color.MediumSeaGreen, true, 40);

            //Debug.WriteLine("Resetting..");

            MemoryTasks.Remove(MemoryTask);

            _attachcts = new CancellationTokenSource();

            MemoryTask = new Task(() =>
            {
                _call = CallFfxivAttach(_attachcts.Token);
            }, _memoryTask.Token);

            MemoryTasks.Add(MemoryTask);
            MemoryTasks.Run(MemoryTask);
        }

        /* Attatch to FFXIV process after determining if running DX9 or DX11.
         * DX9 is not supported by Sharlayan anymore.
        */

        public bool InitiateMemory()
        {
            var initiated = false;

            try
            {
                var processes9 = Process.GetProcessesByName("ffxiv");
                var processes11 = Process.GetProcessesByName("ffxiv_dx11");

                // DX9
                if (processes9.Length > 0)
                {
                    WriteConsole(ConsoleTypes.Ffxiv, "Attempting Attach..");

                    if (Init)
                    {
                        WriteConsole(ConsoleTypes.Ffxiv, "Chromatics already attached.");
                        return true;
                    }

                    Init = false;
                    // supported: English, Chinese, Japanese, French, German, Korean
                    var gameLanguage = "English";
                    var ignoreJsonCache = !ChromaticsSettings.ChromaticsSettingsMemoryCache;
                    // patchVersion of game, or latest
                    var patchVersion = "latest";
                    var process = processes9[0];
                    var processModel = new ProcessModel
                    {
                        Process = process,
                        IsWin64 = true
                    };
                    MemoryHandler.Instance.SetProcess(processModel, gameLanguage, patchVersion, ignoreJsonCache);
                    initiated = true;
                    Init = true;
                    IsDx11 = false;

                    WriteConsole(ConsoleTypes.Ffxiv, "DX9 Initiated");
                    WriteConsole(ConsoleTypes.Error,
                        "DX9 support has been phased out from Chromatics. Please use DX11 when using Chromatics.");
                }

                // DX11
                else if (processes11.Length > 0)
                {
                    WriteConsole(ConsoleTypes.Ffxiv, "Attempting Attach..");

                    if (Init)
                        return true;


                    Init = false;
                    // supported: English, Chinese, Japanese, French, German, Korean
                    var gameLanguage = "English";
                    var ignoreJsonCache = !ChromaticsSettings.ChromaticsSettingsMemoryCache;
                    // patchVersion of game, or latest
                    var patchVersion = "latest";
                    var process = processes11[0];
                    var processModel = new ProcessModel
                    {
                        Process = process,
                        IsWin64 = true
                    };
                    MemoryHandler.Instance.SetProcess(processModel, gameLanguage, patchVersion, ignoreJsonCache);
                    initiated = true;
                    Init = true;
                    IsDx11 = true;

                    WriteConsole(ConsoleTypes.Ffxiv, "DX11 Initiated");
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, "Error: " + ex.Message);
                WriteConsole(ConsoleTypes.Error, "Internal Error: " + ex.StackTrace);
            }

            return initiated;
        }

        /* Memory Loop */

        private async Task CallFfxivMemory(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(300, ct);
                //Debug.WriteLine("Tick C");
                ReadFfxivMemory();
            }
        }

        public void ReadFfxivMemory()
        {
            try
            {
                /*
                    *0 - Not Attached
                    *1 - Game Not Running
                    *2 - Game in Menu
                    *3 - Game Running
                */

                var processes9 = Process.GetProcessesByName("ffxiv");
                var processes11 = Process.GetProcessesByName("ffxiv_dx11");
                

                if (Attatched > 0)
                {
                    //Get Data
                    
                    if (processes11.Length == 0)
                        FfxivGameStop();

                    _playerInfoX = Reader.GetActors().PCEntities;
                    _menuInfo = Reader.GetPlayerInfo().PlayerEntity;
                    
                    
                    if (Attatched == 3)
                    {
                        //Game is Running
                        //Check if game has stopped by checking Character data for a null value.

                        if (_menuInfo != null && _menuInfo.Name == "")
                        {
                            if (processes11.Length != 0)
                            {
                                //Bounce to menu
                                Attatched = 1;
                                _menuNotify = false;
                                HoldReader = true;
                                SetKeysbase = false;
                                SetMousebase = false;

                                GlobalUpdateState("static", Color.DeepSkyBlue, false);

                                WriteConsole(ConsoleTypes.Ffxiv, "Returning to Main Menu..");
                            }
                            else
                            {
                                FfxivGameStop();
                            }
                        }
                        else
                        {
                            //Call function to parse FFXIV data
                            if (HoldReader == false)
                                ProcessFfxivData();
                        }
                    }
                    else
                    {
                        //Game Not Running
                        if (Attatched == 1)
                        {
                            State = 6;
                            GlobalUpdateState("wave", Color.Magenta, false, Color.MediumSeaGreen, true, 40);
                            
                            Attatched = 2;
                        }

                        //Game in Menu
                        else if (Attatched == 2)
                        {
                            if (_menuInfo != null && _menuInfo.Name != "")
                            {
                                //Set Game Active
                                WriteConsole(ConsoleTypes.Ffxiv, "Game Running (" + _menuInfo.Name + ")");


                                if (ArxSdkCalled == 1 && ArxState == 0)
                                    _arx.ArxUpdateInfo("Game Running (" + _menuInfo.Name + ")");

                                _menuNotify = false;
                                SetKeysbase = false;
                                SetMousebase = false;
                                //GlobalUpdateState("static", Color.Red, false);
                                //GlobalUpdateBulbState(100, System.Drawing.Color.Red, 100);
                                //Watchdog.WatchdogGo();
                                Attatched = 3;
                                HoldReader = false;

                                if (ArxSdkCalled == 1)
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
                                                    changed = changed.Substring(0, changed.Length - 1);

                                                _arx.ArxSendActInfo(changed, 8085);
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

                                State = 0;
                            }
                            else
                            {
                                //Main Menu Still Active
                                if (!_menuNotify)
                                {
                                    WriteConsole(ConsoleTypes.Ffxiv, "Main Menu is still active.");

                                    if (ArxSdkCalled == 1 && ArxState == 0)
                                        _arx.ArxUpdateInfo("Main Menu is still active");

                                    _menuNotify = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, "Init Error: " + ex.Message);
                WriteConsole(ConsoleTypes.Error, "Internal Error: " + ex.StackTrace);
            }
        }

        private void ProcessFfxivData()
        {
            MemoryTasks.Cleanup();

            if (HoldReader) return;

            try
            {
                //Get Data
                var targetInfo = new ActorEntity();
                var targetEmnityInfo = new List<EnmityEntry>();
                var partyInfo = new ConcurrentDictionary<uint, PartyEntity>();
                var partyListNew = new List<uint>();
                var partyListOld = new Dictionary<uint, uint>();


                //_playerInfoX = Reader.GetActors()?.PCEntities;
                _playerInfo = ActorEntity.CurrentUser;

                try
                {
                    if (_playerInfo.Name != "" && _playerInfo.TargetID > 0)
                    {
                        targetInfo = Reader.GetTargetInfo()?.TargetEntity?.CurrentTarget;
                        targetEmnityInfo = Reader.GetTargetInfo()?.TargetEntity?.EnmityEntries;
                    }

                    partyInfo = Reader.GetPartyMembers()?.PartyEntities;

                    partyListNew = Reader.GetPartyMembers()?.NewParty;
                    partyListOld = Reader.GetPartyMembers()?.RemovedParty;
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error, "Parser B: " + ex.Message);
                }


                if (_playerInfo != null && _playerInfo.Name != "")
                    //Watchdog.WatchdogReset();


                    if (_playerInfo != null)
                    {
                        if (!_playgroundonce)
                            _playgroundonce = true;

                        /*
                        var mapname = Sharlayan.Helpers.ZoneHelper.MapInfo(PlayerInfo.MapID).Name;
                        Debug.WriteLine(mapname.English);
                        */

                        //End Playground

                        var maxHp = 0;
                        var currentHp = 0;
                        var maxMp = 0;
                        var currentMp = 0;
                        var maxTp = 0;
                        var currentTp = 0;
                        var hpPerc = _playerInfo.HPPercent;
                        var mpPerc = _playerInfo.MPPercent;
                        var tpPerc = _playerInfo.TPPercent;
                        var cClass = "battle";

                        //Set colour variables

                        var baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor);
                        var highlightColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingHighlightColor);

                        var colHpfull = ColorTranslator.FromHtml(ColorMappings.ColorMappingHpFull);
                        var colHpempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingHpEmpty);
                        var colHpcritical = ColorTranslator.FromHtml(ColorMappings.ColorMappingHpCritical);
                        var colMpfull = ColorTranslator.FromHtml(ColorMappings.ColorMappingMpFull);
                        var colMpempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingMpEmpty);
                        var colTpfull = ColorTranslator.FromHtml(ColorMappings.ColorMappingTpFull);
                        var colTpempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingTpEmpty);
                        var colCastcharge = ColorTranslator.FromHtml(ColorMappings.ColorMappingCastChargeFull);
                        var colCastempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingCastChargeEmpty);

                        var colEm0 = ColorTranslator.FromHtml(ColorMappings.ColorMappingEmnity0);
                        var colEm1 = ColorTranslator.FromHtml(ColorMappings.ColorMappingEmnity1);
                        var colEm2 = ColorTranslator.FromHtml(ColorMappings.ColorMappingEmnity2);
                        var colEm3 = ColorTranslator.FromHtml(ColorMappings.ColorMappingEmnity3);
                        var colEm4 = ColorTranslator.FromHtml(ColorMappings.ColorMappingEmnity4);

                        Console.WriteLine(_playerInfo.Job);

                        //Get Battle, Crafting or Gathering Data

                        if (_playerInfo.Job == Actor.Job.ALC || _playerInfo.Job == Actor.Job.ARM ||
                            _playerInfo.Job == Actor.Job.BSM ||
                            _playerInfo.Job == Actor.Job.CPT || _playerInfo.Job == Actor.Job.CUL ||
                            _playerInfo.Job == Actor.Job.GSM ||
                            _playerInfo.Job == Actor.Job.LTW || _playerInfo.Job == Actor.Job.WVR)
                        {
                            maxHp = _playerInfo.HPMax;
                            currentHp = _playerInfo.HPCurrent;
                            maxMp = _playerInfo.CPMax;
                            currentMp = _playerInfo.CPCurrent;
                            maxTp = _playerInfo.TPMax;
                            currentTp = _playerInfo.TPCurrent;
                            mpPerc = _playerInfo.CPPercent;
                            cClass = "craft";

                            colMpfull = ColorTranslator.FromHtml(ColorMappings.ColorMappingCpFull);
                            colMpempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingCpEmpty);
                        }
                        else if (_playerInfo.Job == Actor.Job.FSH || _playerInfo.Job == Actor.Job.BTN ||
                                 _playerInfo.Job == Actor.Job.MIN)
                        {
                            maxHp = _playerInfo.HPMax;
                            currentHp = _playerInfo.HPCurrent;
                            maxMp = _playerInfo.GPMax;
                            currentMp = _playerInfo.GPCurrent;
                            maxTp = _playerInfo.TPMax;
                            currentTp = _playerInfo.TPCurrent;
                            mpPerc = _playerInfo.GPPercent;
                            cClass = "gather";

                            colMpfull = ColorTranslator.FromHtml(ColorMappings.ColorMappingGpFull);
                            colMpempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingGpEmpty);
                        }
                        else
                        {
                            maxHp = _playerInfo.HPMax;
                            currentHp = _playerInfo.HPCurrent;
                            maxMp = _playerInfo.MPMax;
                            currentMp = _playerInfo.MPCurrent;
                            maxTp = _playerInfo.TPMax;
                            currentTp = _playerInfo.TPCurrent;
                            mpPerc = _playerInfo.MPPercent;
                            cClass = "battle";

                            colMpfull = ColorTranslator.FromHtml(ColorMappings.ColorMappingMpFull);
                            colMpempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingMpEmpty);
                        }

                        //Update ARX
                        if (partyInfo != null)
                        {
                            //Console.WriteLine(PartyInfo[0]);
                            //var partyx = PartyList[1];
                            //Console.WriteLine(PartyListNew.Count);
                        }

                        if (ArxSdkCalled == 1)
                            if (ArxState == 1)
                            {
                                hpPerc = _playerInfo.HPPercent;
                                tpPerc = _playerInfo.TPPercent;

                                var arxHudmd = "normal";
                                double targetPercent = 0;
                                var targetHpcurrent = 0;
                                var targetHpmax = 0;
                                var targetName = "target";
                                var targetEngaged = 0;

                                if (targetInfo != null && targetInfo.Type == Actor.Type.Monster)
                                {
                                    arxHudmd = "target";
                                    targetPercent = targetInfo.HPPercent;
                                    targetHpcurrent = targetInfo.HPCurrent;
                                    targetHpmax = targetInfo.HPMax;
                                    targetName = targetInfo.Name + " (Lv. " + targetInfo.Level + ")";
                                    targetEngaged = 0;

                                    if (targetInfo.IsClaimed)
                                        targetEngaged = 1;
                                }

                                _arx.ArxUpdateFfxivStats(hpPerc, mpPerc, tpPerc, currentHp, currentMp, currentTp,
                                    _playerInfo.MapID, cClass, arxHudmd, targetPercent, targetHpcurrent,
                                    targetHpmax,
                                    targetName, targetEngaged);
                            }
                            else if (ArxState == 2)
                            {
                                Console.WriteLine(partyInfo.Count);
                                var datastring = new string[10];
                                for (uint i = 0; i < 10; i++)
                                    //Console.WriteLine(i);
                                    //PlayerInfo.Job == "ALC" || PlayerInfo.Job == "ARM" || PlayerInfo.Job == "BSM" || PlayerInfo.Job == "CPT" || PlayerInfo.Job == "CUL" || PlayerInfo.Job == "GSM" || PlayerInfo.Job == "LTW" || PlayerInfo.Job == "WVR")
                                    if (partyInfo != null && i < partyInfo.Count)
                                    {
                                        //Console.WriteLine(i);
                                        var pid = partyListNew[Convert.ToInt32(i)];
                                        string ptType;
                                        string ptTpcurrent;
                                        string ptTppercent;
                                        string ptEmnityno;
                                        string ptJob;

                                        if (targetInfo != null && targetInfo.Type == Actor.Type.Monster &&
                                            targetInfo.IsClaimed)
                                        {
                                            //Collect Emnity Table
                                            //var TargetEmnity = TargetEmnityInfo.Count;
                                            var emnitytableX = targetEmnityInfo.Select(t => new KeyValuePair<uint, uint>(t.ID, t.Enmity)).ToList();


                                            //Sort emnity by highest holder
                                            emnitytableX.OrderBy(kvp => kvp.Value);

                                            //Get your index in the list
                                            ptEmnityno = emnitytableX.FindIndex(a => a.Key == partyInfo[pid].ID)
                                                .ToString();
                                        }
                                        else
                                        {
                                            ptEmnityno = "0";
                                        }

                                        switch (partyInfo[pid].Job)
                                        {
                                            case Actor.Job.FSH:
                                                ptType = "player";
                                                ptJob = "Fisher";
                                                break;
                                            case Actor.Job.BTN:
                                                ptType = "player";
                                                ptJob = "Botanist";
                                                break;
                                            case Actor.Job.MIN:
                                                ptType = "player";
                                                ptJob = "Miner";
                                                break;
                                            case Actor.Job.ALC:
                                                ptType = "player";
                                                ptJob = "Alchemist";
                                                break;
                                            case Actor.Job.ARM:
                                                ptType = "player";
                                                ptJob = "Armorer";
                                                break;
                                            case Actor.Job.BSM:
                                                ptType = "player";
                                                ptJob = "Blacksmith";
                                                break;
                                            case Actor.Job.CPT:
                                                ptType = "player";
                                                ptJob = "Carpenter";
                                                break;
                                            case Actor.Job.CUL:
                                                ptType = "player";
                                                ptJob = "Culinarian";
                                                break;
                                            case Actor.Job.GSM:
                                                ptType = "player";
                                                ptJob = "Goldsmith";
                                                break;
                                            case Actor.Job.LTW:
                                                ptType = "player";
                                                ptJob = "Leatherworker";
                                                break;
                                            case Actor.Job.WVR:
                                                ptType = "player";
                                                ptJob = "Weaver";
                                                break;
                                            case Actor.Job.ARC:
                                                ptType = "player";
                                                ptJob = "Archer";
                                                break;
                                            case Actor.Job.LNC:
                                                ptType = "player";
                                                ptJob = "Lancer";
                                                break;
                                            case Actor.Job.CNJ:
                                                ptType = "player";
                                                ptJob = "Conjurer";
                                                break;
                                            case Actor.Job.GLD:
                                                ptType = "player";
                                                ptJob = "Gladiator";
                                                break;
                                            case Actor.Job.MRD:
                                                ptType = "player";
                                                ptJob = "Marauder";
                                                break;
                                            case Actor.Job.PGL:
                                                ptType = "player";
                                                ptJob = "Pugilist";
                                                break;
                                            case Actor.Job.ROG:
                                                ptType = "player";
                                                ptJob = "Rouge";
                                                break;
                                            case Actor.Job.THM:
                                                ptType = "player";
                                                ptJob = "Thaumaturge";
                                                break;
                                            case Actor.Job.ACN:
                                                ptType = "player";
                                                ptJob = "Arcanist";
                                                break;
                                            case Actor.Job.AST:
                                                ptType = "player";
                                                ptJob = "Astrologian";
                                                break;
                                            case Actor.Job.BRD:
                                                ptType = "player";
                                                ptJob = "Bard";
                                                break;
                                            case Actor.Job.BLM:
                                                ptType = "player";
                                                ptJob = "Black_Mage";
                                                break;
                                            case Actor.Job.DRK:
                                                ptType = "player";
                                                ptJob = "Dark_Knight";
                                                break;
                                            case Actor.Job.DRG:
                                                ptType = "player";
                                                ptJob = "Dragoon";
                                                break;
                                            case Actor.Job.MCH:
                                                ptType = "player";
                                                ptJob = "Machinist";
                                                break;
                                            case Actor.Job.MNK:
                                                ptType = "player";
                                                ptJob = "Monk";
                                                break;
                                            case Actor.Job.NIN:
                                                ptType = "player";
                                                ptJob = "Ninja";
                                                break;
                                            case Actor.Job.PLD:
                                                ptType = "player";
                                                ptJob = "Paladin";
                                                break;
                                            case Actor.Job.SCH:
                                                ptType = "player";
                                                ptJob = "Scholar";
                                                break;
                                            case Actor.Job.SMN:
                                                ptType = "player";
                                                ptJob = "Summoner";
                                                break;
                                            case Actor.Job.WHM:
                                                ptType = "player";
                                                ptJob = "White_Mage";
                                                break;
                                            case Actor.Job.WAR:
                                                ptType = "player";
                                                ptJob = "Warrior";
                                                break;
                                            case Actor.Job.SAM:
                                                ptType = "player";
                                                ptJob = "Samurai";
                                                break;
                                            case Actor.Job.RDM:
                                                ptType = "player";
                                                ptJob = "Red_Mage";
                                                break;
                                            default:
                                                ptType = "unknown";
                                                ptJob = "Chocobo";
                                                break;
                                        }

                                        if (i == 0)
                                        {
                                            ptTppercent = tpPerc.ToString("#0%");
                                            ptTpcurrent = currentTp.ToString();
                                        }
                                        else
                                        {
                                            ptTppercent = "100%";
                                            ptTpcurrent = "1000";
                                        }

                                        datastring[i] = "1," + ptType + "," + partyInfo[pid].Name + "," +
                                                        partyInfo[pid].HPPercent.ToString("#0%") + "," +
                                                        partyInfo[pid].HPCurrent + "," +
                                                        partyInfo[pid].MPPercent.ToString("#0%") + "," +
                                                        partyInfo[pid].MPCurrent + "," + ptTppercent + "," +
                                                        ptTpcurrent +
                                                        "," + ptEmnityno + "," + ptJob;
                                        Console.WriteLine(i + @": " + datastring[i]);
                                    }
                                    else
                                    {
                                        datastring[i] = "0,0,0,0,0,0,0,0,0";
                                        //Console.WriteLine(i + @": " + datastring[i]);
                                    }

                                _arx.ArxUpdateFfxivParty(datastring[1], datastring[2], datastring[3], datastring[4],
                                    datastring[5], datastring[6], datastring[7], datastring[8], datastring[9]);
                            }
                            else if (ArxState == 100)
                            {
                                hpPerc = _playerInfo.HPPercent;
                                tpPerc = _playerInfo.TPPercent;

                                var hpMax = _playerInfo.HPMax;
                                var mpMax = _playerInfo.MPMax;
                                var playerposX = _playerInfo.X;
                                var playerposY = _playerInfo.Y;
                                var playerposZ = _playerInfo.Z;
                                var actionstatus = _playerInfo.ActionStatus;
                                var castperc = _playerInfo.CastingPercentage;
                                var castprogress = _playerInfo.CastingProgress;
                                var casttime = _playerInfo.CastingTime;
                                var castingtoggle = _playerInfo.IsCasting;
                                var hitboxrad = _playerInfo.HitBoxRadius;
                                var playerclaimed = _playerInfo.IsClaimed;
                                var playerjob = _playerInfo.Job;
                                var mapid = _playerInfo.MapID;
                                var mapindex = _playerInfo.MapIndex;
                                var mapterritory = _playerInfo.MapTerritory;
                                var playername = _playerInfo.Name;
                                var targettype = _playerInfo.TargetType;

                                var arxHudmd = "normal";
                                double targetPercent = 0;
                                var targetHpcurrent = 0;
                                var targetHpmax = 0;
                                var targetName = "target";
                                var targetEngaged = 0;

                                if (targetInfo != null && targetInfo.Type == Actor.Type.Monster)
                                {
                                    arxHudmd = "target";
                                    targetPercent = targetInfo.HPPercent;
                                    targetHpcurrent = targetInfo.HPCurrent;
                                    targetHpmax = targetInfo.HPMax;
                                    targetName = targetInfo.Name + " (Lv. " + targetInfo.Level + ")";
                                    targetEngaged = 0;

                                    if (targetInfo.IsClaimed)
                                        targetEngaged = 1;
                                }

                                _arx.ArxUpdateFfxivPlugin(hpPerc, mpPerc, tpPerc, currentHp, currentMp, currentTp,
                                    _playerInfo.MapID, cClass, arxHudmd, targetPercent, targetHpcurrent,
                                    targetHpmax,
                                    targetName, targetEngaged, hpMax, mpMax, playerposX, playerposY, playerposZ,
                                    actionstatus, castperc, castprogress, casttime, castingtoggle, hitboxrad,
                                    playerclaimed,
                                    playerjob, mapid, mapindex, mapterritory, playername, targettype);
                            }
                            else if (ArxState == 0)
                            {
                                if (Attatched == 3)
                                {
                                    ArxState = 1;
                                    Thread.Sleep(100);
                                    _arx.ArxSetIndex("playerhud.html");
                                }
                            }

                        //Console.WriteLine("Map ID: " + PlayerInfo.MapID);

                        //Parse Data

                        //Set Base Keyboard lighting. 
                        //Other LED's are built above this base layer.
                        if (SetKeysbase == false)
                        {
                            _baseColor = baseColor;
                            GlobalUpdateState("static", _baseColor, false);
                            GlobalUpdateBulbState(BulbModeTypes.DefaultColor, _baseColor, 500);
                            GlobalUpdateBulbState(BulbModeTypes.TargetHp, _baseColor, 500);
                            GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 500);
                            GlobalUpdateBulbState(BulbModeTypes.Castbar, _baseColor, 500);

                            GlobalApplyKeySingleLighting(DevModeTypes.DefaultColor, _baseColor);
                            GlobalApplyKeySingleLighting(DevModeTypes.TargetHp, _baseColor);
                            GlobalApplyKeySingleLighting(DevModeTypes.Castbar, _baseColor);
                            
                            SetKeysbase = true;
                        }

                        if (SetMousebase == false)
                        {
                            GlobalApplyMapMouseLighting(DevModeTypes.DefaultColor, _baseColor, false);
                            GlobalApplyMapMouseLighting(DevModeTypes.TargetHp, _baseColor, false);
                            GlobalApplyMapMouseLighting(DevModeTypes.Castbar, _baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip1", "Strip8", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip2", "Strip9", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip3", "Strip10", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip4", "Strip11", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip5", "Strip12", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip6", "Strip13", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip7", "Strip14", _baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", _baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip1", "Strip8", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip2", "Strip9", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip3", "Strip10", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", _baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", _baseColor, false);

                            SetMousebase = true;
                        }

                        if (SetPadbase == false)
                        {
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 14, 5, 0, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 13, 6, 1, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 12, 7, 2, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 11, 8, 3, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 10, 9, 4, _baseColor, false);

                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, _baseColor, false);

                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, _baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, _baseColor, false);

                            SetPadbase = true;
                        }

                        //Highlight critical FFXIV keybinds

                        if (ChromaticsSettings.ChromaticsSettingsKeyHighlights)
                        {
                            if (ChromaticsSettings.ChromaticsSettingsAzertyMode)
                            {
                                GlobalApplyMapKeyLighting("Z", highlightColor, false);
                                GlobalApplyMapKeyLighting("Q", highlightColor, false);
                                GlobalApplyMapKeyLighting("S", highlightColor, false);
                                GlobalApplyMapKeyLighting("D", highlightColor, false);
                                GlobalApplyMapKeyLighting("LeftShift", highlightColor, false);
                                GlobalApplyMapKeyLighting("LeftControl", highlightColor, false);
                                GlobalApplyMapKeyLighting("Space", highlightColor, false);

                                GlobalApplyMapKeyLighting("W", _baseColor, false);
                                GlobalApplyMapKeyLighting("A", _baseColor, false);
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("W", highlightColor, false);
                                GlobalApplyMapKeyLighting("A", highlightColor, false);
                                GlobalApplyMapKeyLighting("S", highlightColor, false);
                                GlobalApplyMapKeyLighting("D", highlightColor, false);
                                GlobalApplyMapKeyLighting("LeftShift", highlightColor, false);
                                GlobalApplyMapKeyLighting("LeftControl", highlightColor, false);
                                GlobalApplyMapKeyLighting("Space", highlightColor, false);

                                GlobalApplyMapKeyLighting("Z", _baseColor, false);
                                GlobalApplyMapKeyLighting("Q", _baseColor, false);
                            }

                            GlobalUpdateBulbState(BulbModeTypes.HighlightColor, highlightColor, 100);
                            GlobalApplyKeySingleLighting(DevModeTypes.HighlightColor, highlightColor);
                            GlobalApplyMapMouseLighting(DevModeTypes.HighlightColor, highlightColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip1", "Strip8", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip2", "Strip9", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip3", "Strip10", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip4", "Strip11", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip5", "Strip12", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip6", "Strip13", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip7", "Strip14", highlightColor, false);

                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 14, 5, 0, highlightColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 13, 6, 1, highlightColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 12, 7, 2, highlightColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 11, 8, 3, highlightColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 10, 9, 4, highlightColor, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("W", _baseColor, false);
                            GlobalApplyMapKeyLighting("A", _baseColor, false);
                            GlobalApplyMapKeyLighting("S", _baseColor, false);
                            GlobalApplyMapKeyLighting("D", _baseColor, false);
                            GlobalApplyMapKeyLighting("LeftShift", _baseColor, false);
                            GlobalApplyMapKeyLighting("LeftControl", _baseColor, false);
                            GlobalApplyMapKeyLighting("Space", _baseColor, false);
                            GlobalApplyMapKeyLighting("Z", _baseColor, false);
                            GlobalApplyMapKeyLighting("Q", _baseColor, false);
                        }

                        if (targetInfo == null)
                        {
                            GlobalApplyMapKeyLighting("PrintScreen",
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), 250);

                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));
                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                            GlobalApplyMapKeyLighting("Scroll",
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity),
                                false);
                            GlobalApplyMapKeyLighting("Pause",
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity),
                                false);
                            GlobalApplyMapKeyLighting("Macro16",
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity),
                                false);
                            GlobalApplyMapKeyLighting("Macro17",
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity),
                                false);
                            GlobalApplyMapKeyLighting("Macro18",
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity),
                                false);

                            GlobalApplyMapLogoLighting("", highlightColor, false);
                            //GlobalApplyMapMouseLighting("Logo", highlightColor, false);
                        }


                        //Debuff Status Effects

                        //if (PlayerInfo.IsClaimed)
                        //{
                        var statEffects = _playerInfo.StatusEntries;

                        if (statEffects.Count > 0)
                        {
                            var status = statEffects.Last();
                            if (status.IsCompanyAction == false && status.TargetName == _playerInfo.Name)
                                if (_currentStatus != status.StatusName)
                                {
                                    if (status.StatusName == "Bind")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingBind), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBind);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Petrification")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingPetrification);
                                        GlobalUpdateState("static", _baseColor, false);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                    }
                                    else if (status.StatusName == "Old")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSlow);
                                        GlobalUpdateState("static", _baseColor, false);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                    }
                                    else if (status.StatusName == "Slow")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingSlow), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSlow);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Stun")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingStun);
                                        GlobalUpdateState("static", _baseColor, false);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                    }
                                    else if (status.StatusName == "Silence")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSilence);
                                        GlobalUpdateState("static", _baseColor, false);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                    }
                                    else if (status.StatusName == "Poison")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingPoison), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingPoison);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Pollen")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingPollen), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingPollen);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Pox")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingPox), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingPox);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Paralysis")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingParalysis),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingParalysis);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Leaden")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingLeaden), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingLeaden);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Incapacitation")
                                    {
                                        GlobalRipple2(
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingIncapacitation),
                                            100);
                                        _baseColor =
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingIncapacitation);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Dropsy")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingDropsy), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingDropsy);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Amnesia")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingAmnesia),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingAmnesia);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Bleed")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingBleed), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBleed);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Misery")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingMisery), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingMisery);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Sleep")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingSleep), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSleep);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Daze")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingDaze), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingDaze);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Heavy")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingHeavy), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingHeavy);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Infirmary")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingInfirmary),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingInfirmary);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Burns")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingBurns), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBurns);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Deep Freeze")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingDeepFreeze),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingDeepFreeze);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else if (status.StatusName == "Damage Down")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingDamageDown),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingDamageDown);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                    }
                                    else
                                    {
                                        _baseColor = baseColor;
                                        GlobalUpdateState("static", _baseColor, false);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 500);

                                        GlobalApplyMapKeyLighting("W", highlightColor, false);
                                        GlobalApplyMapKeyLighting("A", highlightColor, false);
                                        GlobalApplyMapKeyLighting("S", highlightColor, false);
                                        GlobalApplyMapKeyLighting("D", highlightColor, false);
                                        GlobalApplyMapKeyLighting("LeftShift", highlightColor, false);
                                        GlobalApplyMapKeyLighting("LeftControl", highlightColor, false);
                                        GlobalApplyMapKeyLighting("Space", highlightColor, false);

                                        if (targetInfo == null)
                                        {
                                            GlobalApplyMapKeyLighting("PrintScreen",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapKeyLighting("Scroll",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapKeyLighting("Pause",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker,
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity),
                                                250);

                                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker,
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));

                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                            GlobalApplyMapKeyLighting("Macro16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapKeyLighting("Macro17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapKeyLighting("Macro18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                            GlobalApplyMapLogoLighting("", highlightColor, false);
                                            //GlobalApplyMapMouseLighting("Logo", highlightColor, false);
                                        }
                                    }

                                    _currentStatus = status.StatusName;
                                }
                            //}
                        }


                        //Target
                        if (targetInfo != null)
                        {
                            if (targetInfo.Type == Actor.Type.Monster)
                            {
                                if (!_targeted)
                                    _targeted = true;


                                //Debug.WriteLine("Claimed: " + TargetInfo.ClaimedByID);
                                //Debug.WriteLine("Claimed: " + TargetInfo.);

                                //Debug.WriteLine(TargetInfo.IsClaimed);

                                //Target HP
                                var currentThp = targetInfo.HPCurrent;
                                var maxThp = targetInfo.HPMax;
                                var polTargetHp = (currentThp - 0) * (5 - 0) / (maxThp - 0) + 0;
                                var polTargetHpx = (currentThp - 0) * (65535 - 0) / (maxThp - 0) + 0;
                                var polTargetHpx2 = (currentThp - 0) * (1.0 - 0.0) / (maxThp - 0) + 0.0;

                                GlobalUpdateBulbStateBrightness(BulbModeTypes.TargetHp,
                                    targetInfo.IsClaimed
                                        ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                        : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle),
                                    (ushort) polTargetHpx, 250);

                                GlobalApplyKeySingleLightingBrightness(DevModeTypes.TargetHp, targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), polTargetHpx2);

                                GlobalApplyMapMouseLightingBrightness(DevModeTypes.TargetHp, targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                if (polTargetHp == 0)
                                {
                                    GlobalApplyMapKeyLighting("Macro1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapKeyLighting("Macro5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);

                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);

                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                }
                                else if (polTargetHp == 1)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                    }
                                    else
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                    }
                                }
                                else if (polTargetHp == 2)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                    }
                                    else
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                    }
                                }
                                else if (polTargetHp == 3)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                    }
                                    else
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                    }
                                }
                                else if (polTargetHp == 4)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                    }
                                    else
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                    }
                                }
                                else if (polTargetHp == 5)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                            false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                    }
                                    else
                                    {
                                        GlobalApplyMapKeyLighting("Macro1",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro2",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro3",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro4",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapKeyLighting("Macro5",
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                    }
                                }


                                //Emnity/Casting

                                if (targetInfo.IsCasting)
                                {
                                    GlobalUpdateBulbState(BulbModeTypes.EnmityTracker,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting),
                                        0);

                                    GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting));

                                    GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);

                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);

                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);

                                    /*
                                    GlobalApplyMapKeyLighting("PrintScreen",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                    GlobalApplyMapKeyLighting("Scroll",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                    GlobalApplyMapKeyLighting("Pause",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_TargetCasting), false);
                                    */

                                    GlobalApplyMapKeyLighting("Macro16",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapKeyLighting("Macro17",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapKeyLighting("Macro18",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    //GlobalApplyMapMouseLighting("Logo", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapLogoLighting("",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);

                                    if (!_castalert)
                                    {
                                        ToggleGlobalFlash2(true);
                                        GlobalFlash2(ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting),
                                            200, new[] {"PrintScreen", "Scroll", "Pause"});

                                        _castalert = true;
                                    }
                                }
                                else
                                {
                                    if (_castalert)
                                    {
                                        ToggleGlobalFlash2(false);

                                        _castalert = false;
                                    }

                                    if (targetInfo.IsClaimed)
                                    {
                                        //Collect Emnity Table
                                        //var TargetEmnity = TargetEmnityInfo.Count;
                                        var emnitytable = targetEmnityInfo.Select(t => new KeyValuePair<uint, uint>(t.ID, t.Enmity)).ToList();


                                        //Sort emnity by highest holder
                                        emnitytable.OrderBy(kvp => kvp.Value);

                                        //Get your index in the list
                                        var personalId = _playerInfo.ID;
                                        var emnityPosition = emnitytable.FindIndex(a => a.Key == personalId);

                                        //Debug.WriteLine("Em Position: " + EmnityPosition);

                                        //_emnitytable.Clear();

                                        if (emnityPosition == -1)
                                        {
                                            //Engaged/No Aggro
                                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, colEm0, 1000);
                                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, colEm0);
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm0, false);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", colEm0, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm0, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm0, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm0, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm0, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm0, false);

                                            if (!_castalert)
                                            {
                                                GlobalApplyMapKeyLighting("PrintScreen", colEm0, false);
                                                GlobalApplyMapKeyLighting("Scroll", colEm0, false);
                                                GlobalApplyMapKeyLighting("Pause", colEm0, false);
                                                //GlobalApplyMapMouseLighting("Logo", colEm0, false);
                                                GlobalApplyMapLogoLighting("", colEm0, false);
                                            }

                                            GlobalApplyMapKeyLighting("Macro16", colEm0, false);
                                            GlobalApplyMapKeyLighting("Macro17", colEm0, false);
                                            GlobalApplyMapKeyLighting("Macro18", colEm0, false);
                                        }
                                        else if (emnityPosition > 4 && emnityPosition <= 8)
                                        {
                                            //Low Aggro
                                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, colEm1, 1000);
                                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, colEm1);
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm1, false);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", colEm1, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm1, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm1, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm1, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm1, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm1, false);

                                            if (!_castalert)
                                            {
                                                GlobalApplyMapKeyLighting("PrintScreen", colEm1, false);
                                                GlobalApplyMapKeyLighting("Scroll", colEm1, false);
                                                GlobalApplyMapKeyLighting("Pause", colEm1, false);
                                                //GlobalApplyMapMouseLighting("Logo", colEm1, false);
                                                GlobalApplyMapLogoLighting("", colEm1, false);
                                            }

                                            GlobalApplyMapKeyLighting("Macro16", colEm1, false);
                                            GlobalApplyMapKeyLighting("Macro17", colEm1, false);
                                            GlobalApplyMapKeyLighting("Macro18", colEm1, false);
                                        }
                                        else if (emnityPosition > 1 && emnityPosition <= 4)
                                        {
                                            //Moderate Aggro
                                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, colEm2, 1000);
                                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, colEm2);
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm2, false);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", colEm2, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm2, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm2, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm2, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm2, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm2, false);

                                            if (!_castalert)
                                            {
                                                GlobalApplyMapKeyLighting("PrintScreen", colEm2, false);
                                                GlobalApplyMapKeyLighting("Scroll", colEm2, false);
                                                GlobalApplyMapKeyLighting("Pause", colEm2, false);
                                                //GlobalApplyMapMouseLighting("Logo", colEm2, false);
                                                GlobalApplyMapLogoLighting("", colEm2, false);
                                            }

                                            GlobalApplyMapKeyLighting("Macro16", colEm2, false);
                                            GlobalApplyMapKeyLighting("Macro17", colEm2, false);
                                            GlobalApplyMapKeyLighting("Macro18", colEm2, false);
                                        }
                                        else if (emnityPosition == 1)
                                        {
                                            //Partial Aggro
                                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, colEm3, 1000);
                                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, colEm3);
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm3, false);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", colEm3, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm3, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm3, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm3, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm3, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm3, false);

                                            if (!_castalert)
                                            {
                                                GlobalApplyMapKeyLighting("PrintScreen", colEm3, false);
                                                GlobalApplyMapKeyLighting("Scroll", colEm3, false);
                                                GlobalApplyMapKeyLighting("Pause", colEm3, false);
                                                //GlobalApplyMapMouseLighting("Logo", colEm3, false);
                                                GlobalApplyMapLogoLighting("", colEm3, false);
                                            }

                                            GlobalApplyMapKeyLighting("Macro16", colEm3, false);
                                            GlobalApplyMapKeyLighting("Macro17", colEm3, false);
                                            GlobalApplyMapKeyLighting("Macro18", colEm3, false);
                                        }
                                        else if (emnityPosition == 0)
                                        {
                                            //Full Aggro
                                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, colEm4, 1000);
                                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, colEm4);
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm4, false);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", colEm4, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm4, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm4, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm4, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm4, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm4, false);

                                            if (!_castalert)
                                            {
                                                GlobalApplyMapKeyLighting("PrintScreen", colEm4, false);
                                                GlobalApplyMapKeyLighting("Scroll", colEm4, false);
                                                GlobalApplyMapKeyLighting("Pause", colEm4, false);
                                                //GlobalApplyMapMouseLighting("Logo", colEm4, false);
                                                GlobalApplyMapLogoLighting("", colEm4, false);
                                            }

                                            GlobalApplyMapKeyLighting("Macro16", colEm4, false);
                                            GlobalApplyMapKeyLighting("Macro17", colEm4, false);
                                            GlobalApplyMapKeyLighting("Macro18", colEm4, false);
                                        }
                                    }
                                    else
                                    {
                                        //Not Engaged/No aggro
                                        GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, colEm0, 1000);
                                        GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, colEm0);
                                        GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm0, false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", colEm0, false);

                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm0, false);
                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm0, false);
                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm0, false);
                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm0, false);
                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm0, false);

                                        if (!_castalert)
                                        {
                                            GlobalApplyMapKeyLighting("PrintScreen", colEm0, false);
                                            GlobalApplyMapKeyLighting("Scroll", colEm0, false);
                                            GlobalApplyMapKeyLighting("Pause", colEm0, false);
                                            //GlobalApplyMapMouseLighting("Logo", colEm0, false);
                                            GlobalApplyMapLogoLighting("", colEm0, false);
                                        }

                                        GlobalApplyMapKeyLighting("Macro16", colEm0, false);
                                        GlobalApplyMapKeyLighting("Macro17", colEm0, false);
                                        GlobalApplyMapKeyLighting("Macro18", colEm0, false);
                                    }
                                }
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("PrintScreen",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalUpdateBulbState(BulbModeTypes.EnmityTracker,
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), 250);

                                GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));
                                GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                GlobalApplyMapKeyLighting("Scroll",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapKeyLighting("Pause",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity),
                                    false);
                                GlobalApplyMapKeyLighting("Macro16",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapKeyLighting("Macro17",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapKeyLighting("Macro18",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                //GlobalApplyMapMouseLighting("Logo", highlightColor, false);
                                GlobalApplyMapLogoLighting("", highlightColor, false);

                                GlobalApplyMapKeyLighting("Macro1", _baseColor, false);
                                GlobalApplyMapKeyLighting("Macro2", _baseColor, false);
                                GlobalApplyMapKeyLighting("Macro3", _baseColor, false);
                                GlobalApplyMapKeyLighting("Macro4", _baseColor, false);
                                GlobalApplyMapKeyLighting("Macro5", _baseColor, false);
                                GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, _baseColor, 1000);
                                GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, _baseColor);
                                GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, _baseColor, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", _baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", _baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", _baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", _baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", _baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", _baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", _baseColor, false);

                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, _baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, _baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, _baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, _baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, _baseColor, false);
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("Macro1", _baseColor, false);
                            GlobalApplyMapKeyLighting("Macro2", _baseColor, false);
                            GlobalApplyMapKeyLighting("Macro3", _baseColor, false);
                            GlobalApplyMapKeyLighting("Macro4", _baseColor, false);
                            GlobalApplyMapKeyLighting("Macro5", _baseColor, false);
                            GlobalUpdateBulbState(BulbModeTypes.TargetHp, _baseColor, 1000);
                            GlobalApplyKeySingleLighting(DevModeTypes.TargetHp, _baseColor);
                            GlobalApplyMapMouseLighting(DevModeTypes.TargetHp, _baseColor, false);

                            if (_targeted)
                                _targeted = false;

                            ToggleGlobalFlash2(false);
                            _castalert = false;
                        }


                        //Castbar
                        var castPercentage = _playerInfo.CastingPercentage;
                        var polCastX = (castPercentage - 0) * (12 - 0) / (1.0 - 0.0) + 0;
                        var polCast = Convert.ToInt32(polCastX);
                        var polCastZ = Convert.ToInt32((castPercentage - 0) * (65535 - 0) / (1.0 - 0.0) + 0);
                        //double polCastZ2 = Convert.ToInt32((castPercentage - 0) * (1.0 - 0.0) / (1.0 - 0.0) + 0.0);

                        //Debug.WriteLine(castPercentage);

                        if (_playerInfo.IsCasting)
                        {
                            //Console.WriteLine(CastPercentage);
                            _lastcast = true;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.Castbar, colCastcharge, (ushort) polCastZ,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.Castbar, colCastcharge, castPercentage);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.Castbar, colCastcharge, false, castPercentage);

                            if (polCast <= 1 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);

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
                            else if (polCast == 2 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
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
                            else if (polCast == 3 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F3", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);

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
                            else if (polCast == 4 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F4", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);

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
                            else if (polCast == 5 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F4", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F5", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
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
                            else if (polCast == 6 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F4", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F5", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F6", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
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
                            else if (polCast == 7 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F4", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F5", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F6", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F7", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
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
                            else if (polCast == 8 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F4", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F5", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F6", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F7", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F8", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
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
                            else if (polCast == 9 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F4", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F5", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F6", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F7", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F8", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F9", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip3", "Strip10", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                /*
                                GlobalApplyMapKeyLighting("F10",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                GlobalApplyMapKeyLighting("F11",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                GlobalApplyMapKeyLighting("F12",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                    */
                            }
                            else if (polCast == 10 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F4", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F5", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F6", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F7", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F8", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F9", colCastcharge, false);
                                GlobalApplyMapKeyLighting("F10", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip3", "Strip10", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);

                                /*
                                GlobalApplyMapKeyLighting("F11",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                GlobalApplyMapKeyLighting("F12",
                                    ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                    */
                            }
                            else if (polCast == 11 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                if (ChromaticsSettings.ChromaticsSettingsCastToggle)
                                {
                                    GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F4", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F5", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F6", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F7", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F8", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F9", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F10", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F11", colCastcharge, false);

                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip3", "Strip10", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip2", "Strip9", colCastcharge, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, colCastcharge, false);

                                    /*
                                    GlobalApplyMapKeyLighting("F12",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMapping_CastChargeEmpty), false);
                                        */
                                }
                                _successcast = true;
                            }
                            else if (polCast >= 12)
                            {
                                if (ChromaticsSettings.ChromaticsSettingsCastToggle)
                                {
                                    GlobalApplyMapKeyLighting("F1", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F2", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F3", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F4", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F5", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F6", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F7", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F8", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F9", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F10", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F11", colCastcharge, false);
                                    GlobalApplyMapKeyLighting("F12", colCastcharge, false);

                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip3", "Strip10", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip2", "Strip9", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip1", "Strip8", colCastcharge, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, colCastcharge, false);
                                }
                                _successcast = true;
                            }
                        }
                        else
                        {
                            if (_lastcast)
                            {
                                if (ChromaticsSettings.ChromaticsSettingsCastToggle)
                                {
                                    GlobalApplyMapKeyLighting("F1", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F2", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F3", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F4", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F5", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F6", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F7", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F8", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F9", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F10", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F11", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F12", _baseColor, false);

                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip1", "Strip8", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip2", "Strip9", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip3", "Strip10", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", _baseColor, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, _baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, _baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, _baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, _baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, _baseColor, false);
                                }

                                var cBulbRip1 = new Task(() =>
                                {
                                    GlobalUpdateBulbState(BulbModeTypes.Castbar, _baseColor, 500);
                                    GlobalApplyKeySingleLighting(DevModeTypes.Castbar, _baseColor);
                                    GlobalApplyMapMouseLighting(DevModeTypes.Castbar, _baseColor, false);
                                });
                                MemoryTasks.Add(cBulbRip1);
                                MemoryTasks.Run(cBulbRip1);


                                if (_successcast && ChromaticsSettings.ChromaticsSettingsCastAnimate)
                                    GlobalRipple1(colCastcharge, 80, _baseColor);

                                _lastcast = false;
                                _successcast = false;
                            }
                        }

                        //HP
                        if (maxHp != 0)
                        {
                            var polHp = (currentHp - 0) * (40 - 0) / (maxHp - 0) + 0;
                            var polHpx = (currentHp - 0) * (70 - 0) / (maxHp - 0) + 0;
                            var polHpz = (currentHp - 0) * (65535 - 0) / (maxHp - 0) + 0;
                            var polHpz2 = (currentHp - 0) * (1.0 - 0.0) / (maxHp - 0) + 0.0;

                            //Debug.WriteLine(polHpz2);

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.HpTracker,
                                polHp <= 10 ? colHpempty : colHpfull,
                                (ushort) polHpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.HpTracker, polHp <= 10 ? colHpempty : colHpfull, polHpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.HpTracker, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);

                            if (polHp <= 40 && polHp > 30)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F1", colHpfull, false);
                                    GlobalApplyMapKeyLighting("F2", colHpfull, false);
                                    GlobalApplyMapKeyLighting("F3", colHpfull, false);
                                    GlobalApplyMapKeyLighting("F4", colHpfull, false);
                                }
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 14, 5, 0, colHpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 13, 6, 1, colHpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 12, 7, 2, colHpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 11, 8, 3, colHpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 10, 9, 4, colHpfull, false);
                            }
                            else if (polHp <= 30 && polHp > 20)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F1", colHpfull, false);
                                    GlobalApplyMapKeyLighting("F2", colHpfull, false);
                                    GlobalApplyMapKeyLighting("F3", colHpfull, false);
                                    GlobalApplyMapKeyLighting("F4", colHpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 14, 5, 0, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 13, 6, 1, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 12, 7, 2, colHpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 11, 8, 3, colHpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 10, 9, 4, colHpfull, false);
                            }
                            else if (polHp <= 20 && polHp > 10)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F1", colHpfull, false);
                                    GlobalApplyMapKeyLighting("F2", colHpfull, false);
                                    GlobalApplyMapKeyLighting("F3", colHpempty, false);
                                    GlobalApplyMapKeyLighting("F4", colHpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 14, 5, 0, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 13, 6, 1, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 12, 7, 2, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 11, 8, 3, colHpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 10, 9, 4, colHpfull, false);
                            }
                            else if (polHp <= 10 && polHp > 0)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F1", colHpcritical, false);
                                    GlobalApplyMapKeyLighting("F2", colHpempty, false);
                                    GlobalApplyMapKeyLighting("F3", colHpempty, false);
                                    GlobalApplyMapKeyLighting("F4", colHpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 14, 5, 0, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 13, 6, 1, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 12, 7, 2, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 11, 8, 3, colHpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 10, 9, 4, colHpcritical, false);
                            }
                            else if (polHp == 0)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F1", colHpcritical, false);
                                    GlobalApplyMapKeyLighting("F2", colHpcritical, false);
                                    GlobalApplyMapKeyLighting("F3", colHpcritical, false);
                                    GlobalApplyMapKeyLighting("F4", colHpcritical, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 14, 5, 0, colHpcritical, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 13, 6, 1, colHpcritical, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 12, 7, 2, colHpcritical, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 11, 8, 3, colHpcritical, false);
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 10, 9, 4, colHpcritical, false);
                            }

                            //Mouse
                            if (polHpx <= 70 && polHpx > 60)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip7", "Strip14", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip6", "Strip13", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip5", "Strip12", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip4", "Strip11", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip3", "Strip10", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip2", "Strip9", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip1", "Strip8", colHpfull, false);
                            }
                            else if (polHpx <= 60 && polHpx > 50)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip7", "Strip14", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip6", "Strip13", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip5", "Strip12", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip4", "Strip11", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip3", "Strip10", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip2", "Strip9", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip1", "Strip8", colHpempty, false);
                            }
                            else if (polHpx <= 50 && polHpx > 40)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip7", "Strip14", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip6", "Strip13", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip5", "Strip12", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip4", "Strip11", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip3", "Strip10", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip2", "Strip9", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip1", "Strip8", colHpempty, false);
                            }
                            else if (polHpx <= 40 && polHpx > 30)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip7", "Strip14", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip6", "Strip13", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip5", "Strip12", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip4", "Strip11", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip3", "Strip10", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip2", "Strip9", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip1", "Strip8", colHpempty, false);
                            }
                            else if (polHpx <= 30 && polHpx > 20)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip7", "Strip14", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip6", "Strip13", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip5", "Strip12", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip4", "Strip11", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip3", "Strip10", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip2", "Strip9", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip1", "Strip8", colHpempty, false);
                            }
                            else if (polHpx <= 20 && polHpx > 10)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip7", "Strip14", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip6", "Strip13", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip5", "Strip12", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip4", "Strip11", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip3", "Strip10", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip2", "Strip9", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip1", "Strip8", colHpempty, false);
                            }
                            else if (polHpx <= 10 && polHpx > 0)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip7", "Strip14", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip6", "Strip13", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip5", "Strip12", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip4", "Strip11", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip3", "Strip10", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip2", "Strip9", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip1", "Strip8", colHpempty, false);
                            }
                            else if (polHpx == 0)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip7", "Strip14", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip6", "Strip13", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip5", "Strip12", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip4", "Strip11", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip3", "Strip10", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip2", "Strip9", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "Strip1", "Strip8", colHpcritical, false);
                            }
                        }

                        //MP
                        if (maxMp != 0)
                        {
                            var polMp = (currentMp - 0) * (40 - 0) / (maxMp - 0) + 0;
                            var polMpx = (currentMp - 0) * (70 - 0) / (maxMp - 0) + 0;
                            var polMpz = (currentMp - 0) * (65535 - 0) / (maxMp - 0) + 0;
                            var polMpz2 = (currentMp - 0) * (1.0 - 0.0) / (maxMp - 0) + 0.0;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.MpTracker, colMpfull, (ushort) polMpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.MpTracker, colMpfull, polMpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.MpTracker, colMpfull, false, polMpz2);

                            if (polMp <= 40 && polMp > 30)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F5", colMpfull, false);
                                    GlobalApplyMapKeyLighting("F6", colMpfull, false);
                                    GlobalApplyMapKeyLighting("F7", colMpfull, false);
                                    GlobalApplyMapKeyLighting("F8", colMpfull, false);
                                }
                                
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 14, 5, 0, colMpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 13, 6, 1, colMpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 12, 7, 2, colMpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 11, 8, 3, colMpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 10, 9, 4, colMpfull, false);
                            }
                            else if (polMp <= 30 && polMp > 20)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F5", colMpfull, false);
                                    GlobalApplyMapKeyLighting("F6", colMpfull, false);
                                    GlobalApplyMapKeyLighting("F7", colMpfull, false);
                                    GlobalApplyMapKeyLighting("F8", colMpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 14, 5, 0, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 13, 6, 1, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 12, 7, 2, colMpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 11, 8, 3, colMpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 10, 9, 4, colMpfull, false);
                            }
                            else if (polMp <= 20 && polMp > 10)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F5", colMpfull, false);
                                    GlobalApplyMapKeyLighting("F6", colMpfull, false);
                                    GlobalApplyMapKeyLighting("F7", colMpempty, false);
                                    GlobalApplyMapKeyLighting("F8", colMpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 14, 5, 0, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 13, 6, 1, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 12, 7, 2, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 11, 8, 3, colMpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 10, 9, 4, colMpfull, false);
                            }
                            else if (polMp <= 10 && polMp > 0)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F5", colMpempty, false);
                                    GlobalApplyMapKeyLighting("F6", colMpempty, false);
                                    GlobalApplyMapKeyLighting("F7", colMpempty, false);
                                    GlobalApplyMapKeyLighting("F8", colMpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 14, 5, 0, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 13, 6, 1, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 12, 7, 2, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 11, 8, 3, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 10, 9, 4, colMpempty, false);
                            }
                            else if (polMp == 0)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F5", colMpempty, false);
                                    GlobalApplyMapKeyLighting("F6", colMpempty, false);
                                    GlobalApplyMapKeyLighting("F7", colMpempty, false);
                                    GlobalApplyMapKeyLighting("F8", colMpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 14, 5, 0, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 13, 6, 1, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 12, 7, 2, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 11, 8, 3, colMpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 10, 9, 4, colMpempty, false);
                            }

                            //Mouse
                            if (MouseToggle == 0)
                                if (polMpx <= 70 && polMpx > 60)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpfull, false);
                                }
                                else if (polMpx <= 60 && polMpx > 50)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpempty, false);
                                }
                                else if (polMpx <= 50 && polMpx > 40)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpempty, false);
                                }
                                else if (polMpx <= 40 && polMpx > 30)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpempty, false);
                                }
                                else if (polMpx <= 30 && polMpx > 20)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpempty, false);
                                }
                                else if (polMpx <= 20 && polMpx > 10)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpempty, false);
                                }
                                else if (polMpx <= 10 && polMpx > 0)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpempty, false);
                                }
                                else if (polMpx == 0)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpempty, false);
                                }
                        }

                        //TP
                        if (maxTp != 0)
                        {
                            var polTp = (currentTp - 0) * (40 - 0) / (maxTp - 0) + 0;
                            var polTpx = (currentTp - 0) * (70 - 0) / (maxTp - 0) + 0;
                            var polTpz = (currentTp - 0) * (65535 - 0) / (maxTp - 0) + 0;
                            var polTpz2 = (currentTp - 0) * (1.0 - 0.0) / (maxTp - 0) + 0.0;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.TpTracker, colTpfull, (ushort) polTpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.TpTracker, colTpfull, polTpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.TpTracker, colTpfull, false, polTpz2);

                            if (polTp <= 40 && polTp > 30)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F9", colTpfull, false);
                                    GlobalApplyMapKeyLighting("F10", colTpfull, false);
                                    GlobalApplyMapKeyLighting("F11", colTpfull, false);
                                    GlobalApplyMapKeyLighting("F12", colTpfull, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 14, 5, 0, colTpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 13, 6, 1, colTpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 12, 7, 2, colTpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 11, 8, 3, colTpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 10, 9, 4, colTpfull, false);
                            }
                            else if (polTp <= 30 && polTp > 20)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F9", colTpfull, false);
                                    GlobalApplyMapKeyLighting("F10", colTpfull, false);
                                    GlobalApplyMapKeyLighting("F11", colTpfull, false);
                                    GlobalApplyMapKeyLighting("F12", colTpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 14, 5, 0, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 13, 6, 1, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 12, 7, 2, colTpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 11, 8, 3, colTpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 10, 9, 4, colTpfull, false);
                            }
                            else if (polTp <= 20 && polTp > 10)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F9", colTpfull, false);
                                    GlobalApplyMapKeyLighting("F10", colTpfull, false);
                                    GlobalApplyMapKeyLighting("F11", colTpempty, false);
                                    GlobalApplyMapKeyLighting("F12", colTpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 14, 5, 0, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 13, 6, 1, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 12, 7, 2, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 11, 8, 3, colTpfull, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 10, 9, 4, colTpfull, false);
                            }
                            else if (polTp <= 10 && polTp > 0)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F9", colTpempty, false);
                                    GlobalApplyMapKeyLighting("F10", colTpempty, false);
                                    GlobalApplyMapKeyLighting("F11", colTpempty, false);
                                    GlobalApplyMapKeyLighting("F12", colTpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 14, 5, 0, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 13, 6, 1, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 12, 7, 2, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 11, 8, 3, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 10, 9, 4, colTpempty, false);
                            }
                            else if (polTp == 0)
                            {
                                if (!_playerInfo.IsCasting)
                                {
                                    GlobalApplyMapKeyLighting("F9", colTpempty, false);
                                    GlobalApplyMapKeyLighting("F10", colTpempty, false);
                                    GlobalApplyMapKeyLighting("F11", colTpempty, false);
                                    GlobalApplyMapKeyLighting("F12", colTpempty, false);
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 14, 5, 0, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 13, 6, 1, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 12, 7, 2, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 11, 8, 3, colTpempty, false);
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 10, 9, 4, colTpempty, false);
                            }

                            //Mouse
                            if (MouseToggle == 1)
                                if (polTpx <= 70 && polTpx > 60)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpfull, false);
                                }
                                else if (polTpx <= 60 && polTpx > 50)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpempty, false);
                                }
                                else if (polTpx <= 50 && polTpx > 40)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpempty, false);
                                }
                                else if (polTpx <= 40 && polTpx > 30)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpempty, false);
                                }
                                else if (polTpx <= 30 && polTpx > 20)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpempty, false);
                                }
                                else if (polTpx <= 20 && polTpx > 10)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpempty, false);
                                }
                                else if (polTpx <= 10 && polTpx > 0)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpfull, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpempty, false);
                                }
                                else if (polTpx == 0)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpempty, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpempty, false);
                                }

                            //Action Alerts

                            if (ChromaticsSettings.ChromaticsSettingsImpactToggle)
                            {
                                if (targetInfo != null && targetInfo.Type == Actor.Type.Monster)
                                    if (targetInfo.IsClaimed)
                                        if (_hp != 0 && currentHp < _hp)
                                        {
                                            _rzFl1Cts.Cancel();
                                            _corsairF1Cts.Cancel();
                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingHpLoss),
                                                100,
                                                DeviceEffects.GlobalKeys3);
                                        }

                                _hp = currentHp;
                            }
                        }

                        //DX11 Effects

                        if (IsDx11)
                        {
                            Cooldowns.RefreshData();


                            //Hotbars        

                            var hotbars = Reader.GetActions();

                            if (ChromaticsSettings.ChromaticsSettingsKeybindToggle)
                            {
                                FfxivHotbar.Keybindwhitelist.Clear();

                                foreach (var hotbar in hotbars.ActionEntities)
                                {
                                    if (hotbar.Type == HotBarRecast.Container.CROSS_HOTBAR_1 ||
                                        hotbar.Type == HotBarRecast.Container.CROSS_HOTBAR_2 ||
                                        hotbar.Type == HotBarRecast.Container.CROSS_HOTBAR_3 ||
                                        hotbar.Type == HotBarRecast.Container.CROSS_HOTBAR_4 ||
                                        hotbar.Type == HotBarRecast.Container.CROSS_HOTBAR_5 ||
                                        hotbar.Type == HotBarRecast.Container.CROSS_HOTBAR_6 ||
                                        hotbar.Type == HotBarRecast.Container.CROSS_HOTBAR_7 ||
                                        hotbar.Type == HotBarRecast.Container.CROSS_HOTBAR_8 ||
                                        hotbar.Type == HotBarRecast.Container.PETBAR ||
                                        hotbar.Type == HotBarRecast.Container.CROSS_PETBAR) continue;

                                    foreach (var action in hotbar.Actions)
                                    {
                                        if (!action.IsKeyBindAssigned || string.IsNullOrEmpty(action.Name) ||
                                            string.IsNullOrEmpty(action.KeyBinds) ||
                                            string.IsNullOrEmpty(action.ActionKey)) continue;

                                        //Collect Modifier Info
                                        var modsactive = action.Modifiers.Count;
                                        var _modsactive = modsactive;

                                        if (!FfxivHotbar.Keybindwhitelist.Contains(action.ActionKey))
                                            FfxivHotbar.Keybindwhitelist.Add(action.ActionKey);


                                        if (modsactive > 0)
                                            foreach (var modifier in action.Modifiers)
                                            {
                                                if (modsactive == 0) break;

                                                if (modifier == "Ctrl")
                                                    if (_keyCtrl)
                                                    {
                                                        _modsactive--;
                                                    }
                                                    else
                                                    {
                                                        if (_modsactive < modsactive)
                                                            _modsactive++;
                                                    }

                                                if (modifier == "Alt")
                                                    if (_keyAlt)
                                                    {
                                                        _modsactive--;
                                                    }
                                                    else
                                                    {
                                                        if (_modsactive < modsactive)
                                                            _modsactive++;
                                                    }

                                                if (modifier == "Shift")
                                                    if (_keyShift)
                                                    {
                                                        _modsactive--;
                                                    }
                                                    else
                                                    {
                                                        if (_modsactive < modsactive)
                                                            _modsactive++;
                                                    }
                                            }

                                        //Assign Lighting

                                        if (FfxivHotbar.Keybindtranslation.ContainsKey(action.ActionKey))
                                        {
                                            var keyid = FfxivHotbar.Keybindtranslation[action.ActionKey];

                                            if (_modsactive == 0)
                                                if (action.IsAvailable || _playerInfo.IsCasting)
                                                    if (action.InRange)
                                                        if (action.IsProcOrCombo)
                                                        {
                                                            //Action Proc'd
                                                            GlobalApplyMapKeyLighting(keyid,
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingHotbarProc), false, true);
                                                        }
                                                        else
                                                        {
                                                            if (action.CoolDownPercent > 0)
                                                                GlobalApplyMapKeyLighting(keyid,
                                                                    ColorTranslator.FromHtml(ColorMappings
                                                                        .ColorMappingHotbarCd), false, true);
                                                            else
                                                                GlobalApplyMapKeyLighting(keyid,
                                                                    ColorTranslator.FromHtml(ColorMappings
                                                                        .ColorMappingHotbarReady), false, true);
                                                        }
                                                    else
                                                        GlobalApplyMapKeyLighting(keyid,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingHotbarOutRange), false, true);
                                                else
                                                    GlobalApplyMapKeyLighting(keyid,
                                                        ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingHotbarNotAvailable), false, true);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("OemTilde", _baseColor, false);
                                GlobalApplyMapKeyLighting("D1", _baseColor, false);
                                GlobalApplyMapKeyLighting("D2", _baseColor, false);
                                GlobalApplyMapKeyLighting("D3", _baseColor, false);
                                GlobalApplyMapKeyLighting("D4", _baseColor, false);
                                GlobalApplyMapKeyLighting("D5", _baseColor, false);
                                GlobalApplyMapKeyLighting("D6", _baseColor, false);
                                GlobalApplyMapKeyLighting("D7", _baseColor, false);
                                GlobalApplyMapKeyLighting("D8", _baseColor, false);
                                GlobalApplyMapKeyLighting("D9", _baseColor, false);
                                GlobalApplyMapKeyLighting("D0", _baseColor, false);
                                GlobalApplyMapKeyLighting("OemMinus", _baseColor, false);
                                GlobalApplyMapKeyLighting("OemEquals", _baseColor, false);
                            }


                            //Cooldowns
                            var gcdHot = ColorTranslator.FromHtml(ColorMappings.ColorMappingGcdHot);
                            var gcdReady = ColorTranslator.FromHtml(ColorMappings.ColorMappingGcdReady);
                            var gcdEmpty = ColorTranslator.FromHtml(ColorMappings.ColorMappingGcdEmpty);

                            var gcdTotal = Cooldowns.GlobalCooldownTotal;
                            var gcdRemain = Cooldowns.GlobalCooldownRemaining;
                            var polGcd = (gcdRemain - 0) * (30 - 0) / (gcdTotal - 0) + 0;

                            if (ChromaticsSettings.ChromaticsSettingsGcdCountdown)
                            {
                                if (!Cooldowns.GlobalCooldownReady)
                                {
                                    if (polGcd <= 30 && polGcd > 20)
                                    {
                                        GlobalApplyMapKeyLighting("PageUp", gcdHot, false);
                                        GlobalApplyMapKeyLighting("PageDown", gcdHot, false);
                                        GlobalApplyMapKeyLighting("Home", gcdHot, false);
                                        GlobalApplyMapKeyLighting("End", gcdHot, false);
                                        GlobalApplyMapKeyLighting("Insert", gcdHot, false);
                                        GlobalApplyMapKeyLighting("Delete", gcdHot, false);
                                    }
                                    else if (polGcd <= 20 && polGcd > 10)
                                    {
                                        GlobalApplyMapKeyLighting("PageUp", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("PageDown", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("Home", gcdHot, false);
                                        GlobalApplyMapKeyLighting("End", gcdHot, false);
                                        GlobalApplyMapKeyLighting("Insert", gcdHot, false);
                                        GlobalApplyMapKeyLighting("Delete", gcdHot, false);
                                    }
                                    else if (polGcd <= 10 && polGcd > 0)
                                    {
                                        GlobalApplyMapKeyLighting("PageUp", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("PageDown", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("Home", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("End", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("Insert", gcdHot, false);
                                        GlobalApplyMapKeyLighting("Delete", gcdHot, false);
                                    }
                                    else if (polGcd == 0)
                                    {
                                        GlobalApplyMapKeyLighting("PageUp", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("PageDown", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("Home", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("End", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("Insert", gcdEmpty, false);
                                        GlobalApplyMapKeyLighting("Delete", gcdEmpty, false);
                                    }
                                }
                                else
                                {
                                    GlobalApplyMapKeyLighting("PageUp", gcdReady, false);
                                    GlobalApplyMapKeyLighting("PageDown", gcdReady, false);
                                    GlobalApplyMapKeyLighting("Home", gcdReady, false);
                                    GlobalApplyMapKeyLighting("End", gcdReady, false);
                                    GlobalApplyMapKeyLighting("Insert", gcdReady, false);
                                    GlobalApplyMapKeyLighting("Delete", gcdReady, false);
                                }
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("PageUp", gcdReady, false);
                                GlobalApplyMapKeyLighting("PageDown", gcdReady, false);
                                GlobalApplyMapKeyLighting("Home", gcdReady, false);
                                GlobalApplyMapKeyLighting("End", gcdReady, false);
                                GlobalApplyMapKeyLighting("Insert", gcdReady, false);
                                GlobalApplyMapKeyLighting("Delete", gcdReady, false);
                            }

                            //Job Gauges

                            if (ChromaticsSettings.ChromaticsSettingsJobGaugeToggle)
                            {
                                switch (_playerInfo.Job)
                                {
                                    case Actor.Job.WAR:
                                        var burstwarcol = Color.Orange;
                                        var maxwarcol = Color.Red;
                                        var negwarcol = Color.Black;
                                        var wrath = Cooldowns.Wrath;
                                        var polWrath = (wrath - 0) * (50 - 0) / (100 - 0) + 0;

                                        if (wrath > 0)
                                        {
                                            if (polWrath >= 50)
                                            {
                                                //Flash
                                                ToggleGlobalFlash3(true);
                                                GlobalFlash3(maxwarcol, 150);
                                            }
                                            else if (polWrath < 50 && polWrath > 40)
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
                                            else if (polWrath <= 40 && polWrath > 30)
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
                                            else if (polWrath <= 30 && polWrath > 20)
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
                                            else if (polWrath <= 20 && polWrath > 10)
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
                                            else if (polWrath <= 10 && polWrath > 0)
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
                                            else if (polWrath == 0)
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
                                    case Actor.Job.PLD:
                                        break;
                                    case Actor.Job.MNK:
                                        var greased = Cooldowns.GreasedLightningStacks;
                                        var greaseRemaining = Cooldowns.GreasedLightningTimeRemaining;
                                        var burstmnkcol = Color.Aqua;
                                        var burstmnkempty = Color.Black;

                                        if (greased > 0)
                                        {
                                            if (greaseRemaining > 0 && greaseRemaining <= 5)
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
                                    case Actor.Job.DRG:

                                        var burstdrgcol = Color.Red;
                                        var negdrgcol = Color.Black;
                                        var bloodremain = Cooldowns.BloodOfTheDragonTimeRemaining;
                                        var polBlood = (bloodremain - 0) * (50 - 0) / (30 - 0) + 0;

                                        if (bloodremain > 0)
                                        {
                                            if (polBlood <= 50 && polBlood > 40)
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
                                            else if (polBlood <= 40 && polBlood > 30)
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
                                            else if (polBlood <= 30 && polBlood > 20)
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
                                            else if (polBlood <= 20 && polBlood > 10)
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
                                            else if (polBlood <= 10 && polBlood > 0)
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
                                    case Actor.Job.BRD:
                                        //Bard Songs
                                        var burstcol = Color.Black;
                                        var negcol = Color.Black;

                                        if (Cooldowns.Song != Cooldowns.BardSongs.None)
                                        {
                                            var songremain = Cooldowns.SongTimeRemaining;
                                            var polSong = (songremain - 0) * (50 - 0) / (30 - 0) + 0;

                                            switch (Cooldowns.Song)
                                            {
                                                case Cooldowns.BardSongs.ArmysPaeon:
                                                    burstcol = Color.Orange;
                                                    negcol = Color.Black;
                                                    break;
                                                case Cooldowns.BardSongs.MagesBallad:
                                                    burstcol = Color.MediumSlateBlue;
                                                    negcol = Color.Black;
                                                    break;
                                                case Cooldowns.BardSongs.WanderersMinuet:
                                                    burstcol = Color.MediumSpringGreen;
                                                    negcol = Color.Black;
                                                    break;
                                            }

                                            if (polSong <= 50 && polSong > 40)
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
                                            else if (polSong <= 40 && polSong > 30)
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
                                            else if (polSong <= 30 && polSong > 20)
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
                                            else if (polSong <= 20 && polSong > 10)
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
                                            else if (polSong <= 10 && polSong > 0)
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
                                    case Actor.Job.WHM:
                                        break;
                                    case Actor.Job.BLM:
                                        break;
                                    case Actor.Job.SMN:
                                        var aetherflowsmn = Cooldowns.AetherflowCount;

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
                                    case Actor.Job.SCH:

                                        var aetherflowsch = Cooldowns.AetherflowCount;

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
                                    case Actor.Job.NIN:
                                        break;
                                    case Actor.Job.DRK:
                                        break;
                                    case Actor.Job.AST:
                                        var burstastcol = Color.Black;

                                        if (Cooldowns.CurrentCard != Cooldowns.CardTypes.None)
                                        {
                                            switch (Cooldowns.CurrentCard)
                                            {
                                                case Cooldowns.CardTypes.Arrow:
                                                    burstastcol = Color.Lime;
                                                    break;
                                                case Cooldowns.CardTypes.Balance:
                                                    burstastcol = Color.Crimson;
                                                    break;
                                                case Cooldowns.CardTypes.Bole:
                                                    burstastcol = Color.Orange;
                                                    break;
                                                case Cooldowns.CardTypes.Ewer:
                                                    burstastcol = Color.MediumBlue;
                                                    break;
                                                case Cooldowns.CardTypes.Spear:
                                                    burstastcol = Color.Turquoise;
                                                    break;
                                                case Cooldowns.CardTypes.Spire:
                                                    burstastcol = Color.SlateBlue;
                                                    break;
                                            }

                                            if (Cooldowns.CurrentCard != _currentCard)
                                            {
                                                if (Cooldowns.CurrentCard != Cooldowns.CardTypes.None)
                                                    GlobalRipple1(burstastcol, 80, _baseColor);

                                                _currentCard = Cooldowns.CurrentCard;
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
                                    case Actor.Job.MCH:
                                        break;
                                    case Actor.Job.SAM:
                                        break;
                                    case Actor.Job.RDM:
                                        var blackmana = Cooldowns.BlackMana;
                                        var whitemana = Cooldowns.WhiteMana;
                                        var polBlack = (blackmana - 0) * (40 - 0) / (100 - 0) + 0;
                                        var polWhite = (whitemana - 0) * (40 - 0) / (100 - 0) + 0;

                                        var blackburst = Color.Red;
                                        var whiteburst = Color.White;
                                        var negburst = Color.Black;

                                        GlobalApplyMapKeyLighting("NumDivide", Color.Black, false);
                                        GlobalApplyMapKeyLighting("Num8", Color.Black, false);
                                        GlobalApplyMapKeyLighting("Num5", Color.Black, false);
                                        GlobalApplyMapKeyLighting("Num2", Color.Black, false);

                                        if (polBlack <= 40 && polBlack > 30)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num9", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num3", blackburst, false);
                                        }
                                        else if (polBlack <= 30 && polBlack > 20)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                            GlobalApplyMapKeyLighting("Num9", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num3", blackburst, false);
                                        }
                                        else if (polBlack <= 20 && polBlack > 10)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num3", blackburst, false);
                                        }
                                        else if (polBlack <= 10 && polBlack > 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                                            GlobalApplyMapKeyLighting("Num6", negburst, false);
                                            GlobalApplyMapKeyLighting("Num3", blackburst, false);
                                        }
                                        else if (polBlack == 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                                            GlobalApplyMapKeyLighting("Num6", negburst, false);
                                            GlobalApplyMapKeyLighting("Num3", negburst, false);
                                        }


                                        if (polWhite <= 40 && polWhite > 30)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                                        }
                                        else if (polWhite <= 30 && polWhite > 20)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                            GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                                        }
                                        else if (polWhite <= 20 && polWhite > 10)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                                        }
                                        else if (polWhite <= 10 && polWhite > 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                                            GlobalApplyMapKeyLighting("Num4", negburst, false);
                                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                                        }
                                        else if (polWhite == 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                                            GlobalApplyMapKeyLighting("Num4", negburst, false);
                                            GlobalApplyMapKeyLighting("Num1", negburst, false);
                                        }

                                        break;
                                }
                            }
                            else
                            {
                                ToggleGlobalFlash3(false);
                                GlobalApplyMapKeyLighting("NumLock", _baseColor, false);
                                GlobalApplyMapKeyLighting("NumDivide", _baseColor, false);
                                GlobalApplyMapKeyLighting("NumMultiply", _baseColor, false);
                                GlobalApplyMapKeyLighting("NumSubtract", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num7", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num8", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num9", _baseColor, false);
                                GlobalApplyMapKeyLighting("NumPlus", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num4", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num5", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num6", _baseColor, false);
                                GlobalApplyMapKeyLighting("NumEnter", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num1", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num2", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num3", _baseColor, false);
                                GlobalApplyMapKeyLighting("Num0", _baseColor, false);
                                GlobalApplyMapKeyLighting("NumDecimal", _baseColor, false);
                            }

                            //Duty Finder Bell

                            FfxivDutyFinder.RefreshData();
                            //Debug.WriteLine(FFXIVInterfaces.FFXIVDutyFinder.isPopped() + "//" + FFXIVInterfaces.FFXIVDutyFinder.Countdown());

                            if (FfxivDutyFinder.IsPopped())
                            {
                                Debug.WriteLine("DF Pop");
                                if (!_dfpop)
                                {
                                    ToggleGlobalFlash4(true);
                                    GlobalFlash4(_baseColor,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), 500,
                                        DeviceEffects.GlobalKeys);

                                    GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));
                                    GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

                                    _dfpop = true;
                                }
                                else
                                {
                                    if (FfxivDutyFinder.Countdown() < 10 && !_dfcount)
                                    {
                                        ToggleGlobalFlash4(false);
                                        GlobalFlash4(_baseColor,
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), 200,
                                            DeviceEffects.GlobalKeys);

                                        GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));
                                        GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

                                        _dfcount = true;
                                    }
                                }
                            }
                            else
                            {
                                if (_dfpop)
                                {
                                    ToggleGlobalFlash4(false);
                                    _dfpop = false;
                                    _dfcount = false;
                                    SetKeysbase = false;
                                    SetMousebase = false;

                                    GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, _baseColor);
                                    GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, _baseColor, false);

                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", _baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", _baseColor, false);
                                }
                            }
                        }

                        if (CorsairSdkCalled == 1)
                        {
                            //CorsairUpdateLED();
                        }

                        GlobalKeyboardUpdate();
                        MemoryTasks.Cleanup();
                    }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, "Parse Error: " + ex.Message);
                WriteConsole(ConsoleTypes.Error, "Internal Error: " + ex.StackTrace);
            }
        }
    }
}