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
using Chromatics.Controllers;


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
        private int _lastWeather = 0;
        private bool _weathertoggle;

        /* Parse FFXIV Function
         * Read the data from Sharlayan and call lighting functions according
         */

        private bool _lastcast;
        private bool _menuNotify;

        //private static readonly object _ReadFFXIVMemory = new object();
        
        private ActorEntity _playerInfo = new ActorEntity();
        private ActorEntity _menuInfo = new ActorEntity();
        private ConcurrentDictionary<uint, ActorEntity> _playerInfoX = new ConcurrentDictionary<uint, ActorEntity>();

        private bool _playgroundonce;
        private bool _successcast;

        private FFXIVUnsafeMethods _unsafe = new FFXIVUnsafeMethods();
        private readonly object _CallLcdData = new object();

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
            SetPadbase = false;
            SetHeadsetbase = false;
            SetKeypadbase = false;
            SetCLbase = false;

            if (LcdSdkCalled == 1)
            {
                _lcd.SetStartupFlag = false;
            }

            MemoryHandler.Instance.UnsetProcess();
            _call = null;

            //HoldReader = false;

            //GlobalUpdateState("static", Color.DeepSkyBlue, false);
            GlobalApplyAllKeyLighting(ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
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

                    switch (ChromaticsSettings.ChromaticsSettingsLanguage)
                    {
                        case 0:
                            //English
                            gameLanguage = "English";
                            break;
                        case 1:
                            //Chinese
                            gameLanguage = "Chinese";
                            break;
                        case 2:
                            //Japanese
                            gameLanguage = "Japanese";
                            break;
                        case 3:
                            //French
                            gameLanguage = "French";
                            break;
                        case 4:
                            //German
                            gameLanguage = "German";
                            break;
                        case 5:
                            //Korean
                            gameLanguage = "Korean";
                            break;
                    }

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
                    if (LcdSdkCalled == 1)
                    {
                        _lcd.StatusLCDInfo(@"Attempting Attach..");
                    }

                    if (Init)
                        return true;


                    Init = false;
                    // supported: English, Chinese, Japanese, French, German, Korean
                    var gameLanguage = "English";

                    switch (ChromaticsSettings.ChromaticsSettingsLanguage)
                    {
                        case 0:
                            //English
                            gameLanguage = "English";
                            break;
                        case 1:
                            //Chinese
                            gameLanguage = "Chinese";
                            break;
                        case 2:
                            //Japanese
                            gameLanguage = "Japanese";
                            break;
                        case 3:
                            //French
                            gameLanguage = "French";
                            break;
                        case 4:
                            //German
                            gameLanguage = "German";
                            break;
                        case 5:
                            //Korean
                            gameLanguage = "Korean";
                            break;
                    }

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
                    _menuInfo = ActorEntity.CurrentUser; //Reader.GetPlayerInfo().PlayerEntity;


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
                                SetPadbase = false;
                                SetHeadsetbase = false;
                                SetKeypadbase = false;
                                SetCLbase = false;

                                if (LcdSdkCalled == 1)
                                {
                                    _lcd.SetStartupFlag = false;
                                }

                                //GlobalUpdateState("static", Color.DeepSkyBlue, false);
                                GlobalApplyAllKeyLighting(ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));

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
                            //GlobalUpdateState("wave", Color.Magenta, false, Color.MediumSeaGreen, true, 40);
                            GlobalSetWave();

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
                                SetPadbase = false;
                                SetHeadsetbase = false;
                                SetKeypadbase = false;
                                SetCLbase = false;
                                //GlobalUpdateState("static", Color.Red, false);
                                //GlobalUpdateBulbState(100, System.Drawing.Color.Red, 100);
                                //Watchdog.WatchdogGo();
                                Attatched = 3;
                                HoldReader = false;

                                FFXIVWeather.GetWeatherAPI();

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

                                    if (LcdSdkCalled == 1)
                                    {
                                        _lcd.StatusLCDInfo(@"Main Menu is still active.");
                                    }

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
                //var personalInfo = new PlayerEntity();

                //_playerInfoX = Reader.GetActors()?.PCEntities;
                _playerInfo = ActorEntity.CurrentUser;
                var _playerData = Reader.GetPlayerInfo().PlayerEntity;
                
                try
                {
                    if (_playerInfo.Name != "" && _playerInfo.TargetType != Actor.TargetType.Unknown)
                    {
                        if (Reader.CanGetTargetInfo())
                        {
                            targetInfo = Reader.GetTargetInfo()?.TargetEntity?.CurrentTarget;
                        }

                        if (Reader.CanGetEnmityEntities())
                        {
                            targetEmnityInfo = Reader.GetTargetInfo()?.TargetEntity?.EnmityEntries;
                        }

                        //Console.WriteLine(@"Name:" + targetInfo.Name);
                    }


                    partyInfo = Reader.GetPartyMembers()?.PartyEntities;

                    partyListNew = Reader.GetPartyMembers()?.NewParty;
                    partyListOld = Reader.GetPartyMembers()?.RemovedParty;

                    //personalInfo = Reader.GetPlayerInfo()?.PlayerEntity;

                    
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

                        //Console.WriteLine(_playerInfo.Name);
                        //Console.WriteLine(_playerInfo.IsCasting);

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

                        //Console.WriteLine(ChromaticsSettings.ChromaticsSettingsReactiveWeather);

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

                        if (LcdSdkCalled == 1)
                        {
                            //_lcd.DrawLCDInfo(_playerInfo, targetInfo);

                            lock (_CallLcdData)
                            {
                                _unsafe.CallLcdData(_lcd, _playerInfo, targetInfo);
                            }
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
                                //Console.WriteLine(partyInfo.Count);
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
                                        //Console.WriteLine(i + @": " + datastring[i]);
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

                        //Reactive Weather

                        if (ChromaticsSettings.ChromaticsSettingsReactiveWeather)
                        {
                            if (!_weathertoggle)
                            {
                                SetKeysbase = false;
                                SetMousebase = false;
                                SetPadbase = false;
                                SetHeadsetbase = false;
                                SetKeypadbase = false;
                                SetCLbase = false;

                                _weathertoggle = true;
                            }

                            FFXIVWeather.RefreshData();

                            var currentWeather = FFXIVWeather.WeatherIconID();

                            if (_lastWeather != currentWeather)
                            {
                                _lastWeather = currentWeather;
                                SetKeysbase = false;
                                SetMousebase = false;
                                SetPadbase = false;
                                SetHeadsetbase = false;
                                SetKeypadbase = false;
                                SetCLbase = false;
                            }

                            if (_lastWeather > 0)
                            {
                                var weatherMapbaseKey = _mappingPalette.First(x =>
                                    x.Value[0] == FFXIVWeather.WeatherIconName(_lastWeather) + @" (Base)").Key;
                                var weatherMaphighlightKey = _mappingPalette.First(x =>
                                    x.Value[0] == FFXIVWeather.WeatherIconName(_lastWeather) + @" (Highlight)").Key;

                                var weatherMapbase = (string) typeof(Datastore.FfxivColorMappings)
                                    .GetField(weatherMapbaseKey).GetValue(ColorMappings);
                                var weatherMaphighlight = (string) typeof(Datastore.FfxivColorMappings)
                                    .GetField(weatherMaphighlightKey).GetValue(ColorMappings);

                                baseColor = ColorTranslator.FromHtml(weatherMapbase);
                                highlightColor = ColorTranslator.FromHtml(weatherMaphighlight);


                            }
                        }
                        else
                        {
                            if (_weathertoggle)
                            {
                                SetKeysbase = false;
                                SetMousebase = false;
                                SetPadbase = false;
                                SetHeadsetbase = false;
                                SetKeypadbase = false;
                                SetCLbase = false;
                            }

                            _weathertoggle = false;
                        }

                        //Console.WriteLine(baseColor.Name);

                        //Set Base Keyboard lighting. 
                        //Other LED's are built above this base layer.

                        if (SetKeysbase == false)
                        {
                            _baseColor = baseColor;

                            //GlobalUpdateState("static", baseColor, false);
                            GlobalApplyAllKeyLighting(_baseColor);

                            GlobalUpdateBulbState(BulbModeTypes.DefaultColor, _baseColor, 500);
                            GlobalUpdateBulbState(BulbModeTypes.TargetHp, _baseColor, 500);
                            GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 500);
                            GlobalUpdateBulbState(BulbModeTypes.Castbar, _baseColor, 500);

                            GlobalApplyKeySingleLighting(DevModeTypes.DefaultColor, _baseColor);
                            GlobalApplyKeySingleLighting(DevModeTypes.TargetHp, _baseColor);
                            GlobalApplyKeySingleLighting(DevModeTypes.Castbar, _baseColor);

                            GlobalApplyKeySingleLighting(DevModeTypes.HighlightColor, highlightColor);
                            GlobalUpdateBulbState(BulbModeTypes.HighlightColor, highlightColor, 100);

                            if (_LightbarMode == LightbarMode.DefaultColor)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", _baseColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", _baseColor, false, false);
                            }
                            else if (_LightbarMode == LightbarMode.HighlightColor)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", highlightColor, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", highlightColor, false, false);
                            }

                            SetKeysbase = true;
                        }

                        if (SetMousebase == false)
                        {
                            GlobalApplyMapMouseLighting(DevModeTypes.DefaultColor, baseColor, false);
                            GlobalApplyMapMouseLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapMouseLighting(DevModeTypes.Castbar, baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip1", "Strip8", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip2", "Strip9", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip3", "Strip10", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip4", "Strip11", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip5", "Strip12", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip6", "Strip13", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "Strip7", "Strip14", baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip1", "Strip8", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip2", "Strip9", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip3", "Strip10", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip4", "Strip11", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip5", "Strip12", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip6", "Strip13", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "Strip7", "Strip14", baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip1", "Strip8", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip2", "Strip9", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip3", "Strip10", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "Strip1", "Strip8", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "Strip2", "Strip9", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "Strip3", "Strip10", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "Strip4", "Strip11", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "Strip5", "Strip12", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "Strip6", "Strip13", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "Strip7", "Strip14", highlightColor, false);

                            GlobalApplyMapMouseLighting(DevModeTypes.HighlightColor, highlightColor, false);

                            SetMousebase = true;
                        }

                        if (SetPadbase == false)
                        {
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 14, 5, 0, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 13, 6, 1, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 12, 7, 2, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 11, 8, 3, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.DefaultColor, 10, 9, 4, baseColor, false);

                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, baseColor, false);

                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, baseColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, baseColor, false);

                            GlobalApplyMapPadLighting(DevModeTypes.HighlightColor, 14, 5, 0, highlightColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.HighlightColor, 13, 6, 1, highlightColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.HighlightColor, 12, 7, 2, highlightColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.HighlightColor, 11, 8, 3, highlightColor, false);
                            GlobalApplyMapPadLighting(DevModeTypes.HighlightColor, 10, 9, 4, highlightColor, false);

                            SetPadbase = true;
                        }

                        if (SetHeadsetbase == false)
                        {
                            GlobalApplyMapHeadsetLighting(DevModeTypes.DefaultColor, baseColor, false);
                            GlobalApplyMapHeadsetLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapHeadsetLighting(DevModeTypes.Castbar, baseColor, false);

                            GlobalApplyMapHeadsetLighting(DevModeTypes.HighlightColor, highlightColor, false);

                            SetHeadsetbase = true;
                        }

                        if (SetKeypadbase == false)
                        {
                            GlobalApplyMapKeypadLighting(DevModeTypes.DefaultColor, baseColor, false);
                            GlobalApplyMapKeypadLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapKeypadLighting(DevModeTypes.Castbar, baseColor, false);

                            GlobalApplyMapKeypadLighting(DevModeTypes.HighlightColor, highlightColor, false);

                            SetKeypadbase = true;
                        }

                        if (SetCLbase == false)
                        {
                            GlobalApplyMapChromaLinkLighting(DevModeTypes.DefaultColor, baseColor);
                            GlobalApplyMapChromaLinkLighting(DevModeTypes.TargetHp, baseColor);
                            GlobalApplyMapChromaLinkLighting(DevModeTypes.Castbar, baseColor);

                            GlobalApplyMapChromaLinkLighting(DevModeTypes.HighlightColor, highlightColor);

                            SetCLbase = true;
                        }

                        //Highlight critical FFXIV keybinds

                        if (ChromaticsSettings.ChromaticsSettingsKeyHighlights)
                        {
                            GlobalApplyMapKeyLighting("W", highlightColor, false);
                            GlobalApplyMapKeyLighting("A", highlightColor, false);
                            GlobalApplyMapKeyLighting("S", highlightColor, false);
                            GlobalApplyMapKeyLighting("D", highlightColor, false);
                            GlobalApplyMapKeyLighting("LeftShift", highlightColor, false);
                            GlobalApplyMapKeyLighting("LeftControl", highlightColor, false);
                            GlobalApplyMapKeyLighting("Space", highlightColor, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("W", baseColor, false);
                            GlobalApplyMapKeyLighting("A", baseColor, false);
                            GlobalApplyMapKeyLighting("S", baseColor, false);
                            GlobalApplyMapKeyLighting("D", baseColor, false);
                            GlobalApplyMapKeyLighting("LeftShift", baseColor, false);
                            GlobalApplyMapKeyLighting("LeftControl", baseColor, false);
                            GlobalApplyMapKeyLighting("Space", baseColor, false);
                        }
                        
                        if (targetInfo == null)
                        {
                            GlobalApplyMapKeyLighting("PrintScreen",
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), 250);

                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));
                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));

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
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                    }
                                    else if (status.StatusName == "Old")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSlow);
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
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
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                    }
                                    else if (status.StatusName == "Silence")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSilence);
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
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
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
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
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));

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
                                //var polTargetHp = Helpers.LinIntDouble(0, maxThp, currentThp, 0, 5);
                                var polTargetHp = (currentThp - 0) * (5 - 0) / (maxThp - 0) + 0;
                                var polTargetHpx = (currentThp - 0) * (65535 - 0) / (maxThp - 0) + 0;
                                var polTargetHpx2 = (currentThp - 0) * (1.0 - 0.0) / (maxThp - 0) + 0.0;

                                var polTargetHpLB = (currentThp - 0) * (19 - 0) / (maxThp - 0) + 0;

                                GlobalUpdateBulbStateBrightness(BulbModeTypes.TargetHp,
                                    targetInfo.IsClaimed
                                        ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                        : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle),
                                    (ushort)polTargetHpx, 250);

                                GlobalApplyKeySingleLightingBrightness(DevModeTypes.TargetHp, targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), polTargetHpx2);

                                GlobalApplyMapMouseLightingBrightness(DevModeTypes.TargetHp, targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.TargetHp, targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                GlobalApplyMapKeypadLightingBrightness(DevModeTypes.TargetHp, targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TargetHp, targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), polTargetHpx2);

                                //Lightbar

                                if (_LightbarMode == LightbarMode.TargetHp)
                                {
                                    switch (polTargetHpLB)
                                    {
                                        case 19:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 18:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 17:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 16:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 15:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 14:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 13:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 12:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 11:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 10:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 9:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 8:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 7:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 6:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 5:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 4:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 3:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 2:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        case 1:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed),
                                                false, false);
                                            break;
                                        default:
                                            GlobalApplyMapLightbarLighting("Lightbar19",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1",
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                                false, false);
                                            //
                                            break;
                                    }
                                }


                                //Other
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
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting));

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
                                            200, new[] { "PrintScreen", "Scroll", "Pause" });

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
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm0, false);
                                            GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, colEm0, false);
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm0);

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

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", colEm0, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", colEm0, false, false);
                                            }

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
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm1, false);
                                            GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, colEm1, false);
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm1);

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

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", colEm1, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", colEm1, false, false);
                                            }

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
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm2, false);
                                            GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, colEm2, false);
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm2);

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

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", colEm2, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", colEm2, false, false);
                                            }

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
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm3, false);
                                            GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, colEm3, false);
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm3);

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

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", colEm3, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", colEm3, false, false);
                                            }

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
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm4, false);
                                            GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, colEm4, false);
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm4);

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

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", colEm4, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", colEm4, false, false);
                                            }

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
                                        GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm0, false);
                                        GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, colEm0, false);
                                        GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm0);

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

                                        if (_LightbarMode == LightbarMode.EnmityTracker)
                                        {
                                            GlobalApplyMapLightbarLighting("Lightbar19", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2", colEm0, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1", colEm0, false, false);
                                        }

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
                                GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));

                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                if (_LightbarMode == LightbarMode.TargetHp || _LightbarMode == LightbarMode.EnmityTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", baseColor, false, false);
                                }

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

                                GlobalApplyMapKeyLighting("Macro1", baseColor, false);
                                GlobalApplyMapKeyLighting("Macro2", baseColor, false);
                                GlobalApplyMapKeyLighting("Macro3", baseColor, false);
                                GlobalApplyMapKeyLighting("Macro4", baseColor, false);
                                GlobalApplyMapKeyLighting("Macro5", baseColor, false);
                                GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, baseColor, 1000);
                                GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, baseColor);
                                GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, baseColor, false);
                                GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, baseColor, false);
                                GlobalApplyMapKeypadLighting(DevModeTypes.EnmityTracker, baseColor, false);
                                GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, baseColor);

                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip1", "Strip8", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip2", "Strip9", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip3", "Strip10", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip4", "Strip11", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip5", "Strip12", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip6", "Strip13", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "Strip7", "Strip14", baseColor, false);

                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, baseColor, false);
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("Macro1", baseColor, false);
                            GlobalApplyMapKeyLighting("Macro2", baseColor, false);
                            GlobalApplyMapKeyLighting("Macro3", baseColor, false);
                            GlobalApplyMapKeyLighting("Macro4", baseColor, false);
                            GlobalApplyMapKeyLighting("Macro5", baseColor, false);
                            GlobalUpdateBulbState(BulbModeTypes.TargetHp, baseColor, 1000);
                            GlobalApplyKeySingleLighting(DevModeTypes.TargetHp, baseColor);
                            GlobalApplyMapMouseLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapHeadsetLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapKeypadLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapChromaLinkLighting(DevModeTypes.TargetHp, baseColor);

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

                        //Console.WriteLine(_playerInfo.IsCasting);

                        if (_playerInfo.IsCasting)
                        {
                            //Console.WriteLine(CastPercentage);
                            _lastcast = true;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.Castbar, colCastcharge, (ushort)polCastZ,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.Castbar, colCastcharge, castPercentage);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.Castbar, colCastcharge, false, castPercentage);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.TargetHp, colCastcharge, false, castPercentage);
                            GlobalApplyMapKeypadLightingBrightness(DevModeTypes.TargetHp, colCastcharge, false, castPercentage);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TargetHp, colCastcharge, castPercentage);

                            if (polCast <= 1 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar5", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar7", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar9", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar11", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar13", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar15", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                if (_LightbarMode == LightbarMode.Castbar)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar17", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                }

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

                                    if (_LightbarMode == LightbarMode.Castbar)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", colCastcharge, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", colCastcharge, false, false);
                                    }

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
                                    GlobalApplyMapKeyLighting("F1", baseColor, false);
                                    GlobalApplyMapKeyLighting("F2", baseColor, false);
                                    GlobalApplyMapKeyLighting("F3", baseColor, false);
                                    GlobalApplyMapKeyLighting("F4", baseColor, false);
                                    GlobalApplyMapKeyLighting("F5", baseColor, false);
                                    GlobalApplyMapKeyLighting("F6", baseColor, false);
                                    GlobalApplyMapKeyLighting("F7", baseColor, false);
                                    GlobalApplyMapKeyLighting("F8", baseColor, false);
                                    GlobalApplyMapKeyLighting("F9", baseColor, false);
                                    GlobalApplyMapKeyLighting("F10", baseColor, false);
                                    GlobalApplyMapKeyLighting("F11", baseColor, false);
                                    GlobalApplyMapKeyLighting("F12", baseColor, false);

                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip1", "Strip8", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip2", "Strip9", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip3", "Strip10", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip4", "Strip11", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip5", "Strip12", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip6", "Strip13", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "Strip7", "Strip14", baseColor, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, baseColor, false);

                                    if (_LightbarMode == LightbarMode.Castbar)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", baseColor, false, false);
                                    }
                                }

                                var cBulbRip1 = new Task(() =>
                                {
                                    GlobalUpdateBulbState(BulbModeTypes.Castbar, baseColor, 500);
                                    GlobalApplyKeySingleLighting(DevModeTypes.Castbar, baseColor);
                                    GlobalApplyMapMouseLighting(DevModeTypes.Castbar, baseColor, false);
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.Castbar, baseColor, false);
                                    GlobalApplyMapKeypadLighting(DevModeTypes.Castbar, baseColor, false);
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.Castbar, baseColor);
                                });
                                MemoryTasks.Add(cBulbRip1);
                                MemoryTasks.Run(cBulbRip1);


                                if (_successcast && ChromaticsSettings.ChromaticsSettingsCastAnimate)
                                    GlobalRipple1(colCastcharge, 80, baseColor);

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
                                (ushort)polHpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.HpTracker, polHp <= 10 ? colHpempty : colHpfull, polHpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.HpTracker, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.HpTracker, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapKeypadLightingBrightness(DevModeTypes.HpTracker, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.HpTracker, polHp <= 10 ? colHpempty : colHpfull, polHpz2);

                            if (polHp <= 40 && polHp > 30)
                            {
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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

                                if (_LightbarMode == LightbarMode.HpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colHpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.HpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colHpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.HpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colHpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.HpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colHpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.HpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colHpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.HpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colHpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colHpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.HpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colHpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colHpcritical, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.HpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colHpcritical, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colHpcritical, false, false);
                                }
                            }
                        }

                        //MP
                        if (maxMp != 0)
                        {
                            var polMp = (currentMp - 0) * (40 - 0) / (maxMp - 0) + 0;
                            var polMpx = (currentMp - 0) * (70 - 0) / (maxMp - 0) + 0;
                            var polMpz = (currentMp - 0) * (65535 - 0) / (maxMp - 0) + 0;
                            var polMpz2 = (currentMp - 0) * (1.0 - 0.0) / (maxMp - 0) + 0.0;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.MpTracker, colMpfull, (ushort)polMpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.MpTracker, colMpfull, polMpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.MpTracker, colMpfull, false, polMpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.MpTracker, colMpfull, false, polMpz2);
                            GlobalApplyMapKeypadLightingBrightness(DevModeTypes.MpTracker, colMpfull, false, polMpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.MpTracker, colMpfull, polMpz2);

                            if (polMp <= 40 && polMp > 30)
                            {
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                            if (polMpx <= 70 && polMpx > 60)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip7", "Strip14", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip6", "Strip13", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip5", "Strip12", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip4", "Strip11", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip3", "Strip10", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip2", "Strip9", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "Strip1", "Strip8", colMpfull, false);

                                if (_LightbarMode == LightbarMode.MpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colMpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.MpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colMpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.MpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colMpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.MpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colMpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.MpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colMpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.MpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colMpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.MpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colMpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colMpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.MpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colMpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colMpempty, false, false);
                                }
                            }
                        }

                        //TP
                        if (maxTp != 0)
                        {
                            var polTp = (currentTp - 0) * (40 - 0) / (maxTp - 0) + 0;
                            var polTpx = (currentTp - 0) * (70 - 0) / (maxTp - 0) + 0;
                            var polTpz = (currentTp - 0) * (65535 - 0) / (maxTp - 0) + 0;
                            var polTpz2 = (currentTp - 0) * (1.0 - 0.0) / (maxTp - 0) + 0.0;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.TpTracker, colTpfull, (ushort)polTpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.TpTracker, colTpfull, polTpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.TpTracker, colTpfull, false, polTpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.TpTracker, colTpfull, false, polTpz2);
                            GlobalApplyMapKeypadLightingBrightness(DevModeTypes.TpTracker, colTpfull, false, polTpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TpTracker, colTpfull, polTpz2);

                            if (polTp <= 40 && polTp > 30)
                            {
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                                if (!_playerInfo.IsCasting && ChromaticsSettings.ChromaticsSettingsShowStats)
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
                            if (polTpx <= 70 && polTpx > 60)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip7", "Strip14", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip6", "Strip13", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip5", "Strip12", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip4", "Strip11", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip3", "Strip10", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip2", "Strip9", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "Strip1", "Strip8", colTpfull, false);

                                if (_LightbarMode == LightbarMode.TpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colTpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.TpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colTpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.TpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colTpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.TpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colTpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.TpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colTpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.TpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colTpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.TpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colTpfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colTpfull, false, false);
                                }
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

                                if (_LightbarMode == LightbarMode.TpTracker)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", colTpempty, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", colTpempty, false, false);
                                }
                            }
                            
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

                        //DX11 Effects

                        if (IsDx11)
                        {
                            Cooldowns.RefreshData();

                            //Hotbars        

                            if (Reader.CanGetActions())
                            {
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

                                            //Console.WriteLine(@"key: " + action.ActionKey);

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
                                    GlobalApplyMapKeyLighting("OemTilde", baseColor, false);
                                    GlobalApplyMapKeyLighting("D1", baseColor, false);
                                    GlobalApplyMapKeyLighting("D2", baseColor, false);
                                    GlobalApplyMapKeyLighting("D3", baseColor, false);
                                    GlobalApplyMapKeyLighting("D4", baseColor, false);
                                    GlobalApplyMapKeyLighting("D5", baseColor, false);
                                    GlobalApplyMapKeyLighting("D6", baseColor, false);
                                    GlobalApplyMapKeyLighting("D7", baseColor, false);
                                    GlobalApplyMapKeyLighting("D8", baseColor, false);
                                    GlobalApplyMapKeyLighting("D9", baseColor, false);
                                    GlobalApplyMapKeyLighting("D0", baseColor, false);
                                    GlobalApplyMapKeyLighting("OemMinus", baseColor, false);
                                    GlobalApplyMapKeyLighting("OemEquals", baseColor, false);
                                }
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

                            //Experience Bar

                            
                            if (_LightbarMode == LightbarMode.CurrentExp)
                            {

                                var _role = _playerData.WVR_CurrentEXP;
                                var _currentlvl = 0;

                                var expcolempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingExpEmpty);
                                var expcolfull = ColorTranslator.FromHtml(ColorMappings.ColorMappingExpFull);


                                switch (_playerInfo.Job)
                                {
                                    case Actor.Job.Unknown:
                                        _role = _playerData.WVR_CurrentEXP;
                                        _currentlvl = 0;
                                        break;
                                    case Actor.Job.GLD:
                                        _role = _playerData.GLD_CurrentEXP;
                                        _currentlvl = _playerData.GLD;
                                        break;
                                    case Actor.Job.PGL:
                                        _role = _playerData.PGL_CurrentEXP;
                                        _currentlvl = _playerData.PGL;
                                        break;
                                    case Actor.Job.MRD:
                                        _role = _playerData.MRD_CurrentEXP;
                                        _currentlvl = _playerData.MRD;
                                        break;
                                    case Actor.Job.LNC:
                                        _role = _playerData.LNC_CurrentEXP;
                                        _currentlvl = _playerData.LNC;
                                        break;
                                    case Actor.Job.ARC:
                                        _role = _playerData.ARC_CurrentEXP;
                                        _currentlvl = _playerData.ARC;
                                        break;
                                    case Actor.Job.CNJ:
                                        _role = _playerData.CNJ_CurrentEXP;
                                        _currentlvl = _playerData.CNJ;
                                        break;
                                    case Actor.Job.THM:
                                        _role = _playerData.THM_CurrentEXP;
                                        _currentlvl = _playerData.THM;
                                        break;
                                    case Actor.Job.CPT:
                                        _role = _playerData.CPT_CurrentEXP;
                                        _currentlvl = _playerData.CPT;
                                        break;
                                    case Actor.Job.BSM:
                                        _role = _playerData.BSM_CurrentEXP;
                                        _currentlvl = _playerData.BSM;
                                        break;
                                    case Actor.Job.ARM:
                                        _role = _playerData.ARM_CurrentEXP;
                                        _currentlvl = _playerData.ARM;
                                        break;
                                    case Actor.Job.GSM:
                                        _role = _playerData.GSM_CurrentEXP;
                                        _currentlvl = _playerData.GSM;
                                        break;
                                    case Actor.Job.LTW:
                                        _role = _playerData.LTW_CurrentEXP;
                                        _currentlvl = _playerData.LTW;
                                        break;
                                    case Actor.Job.WVR:
                                        _role = _playerData.WVR_CurrentEXP;
                                        _currentlvl = _playerData.WVR;
                                        break;
                                    case Actor.Job.ALC:
                                        _role = _playerData.ALC_CurrentEXP;
                                        _currentlvl = _playerData.ALC;
                                        break;
                                    case Actor.Job.CUL:
                                        _role = _playerData.CUL_CurrentEXP;
                                        _currentlvl = _playerData.CUL;
                                        break;
                                    case Actor.Job.MIN:
                                        _role = _playerData.MIN_CurrentEXP;
                                        _currentlvl = _playerData.MIN;
                                        break;
                                    case Actor.Job.BTN:
                                        _role = _playerData.BTN_CurrentEXP;
                                        _currentlvl = _playerData.BTN;
                                        break;
                                    case Actor.Job.FSH:
                                        _role = _playerData.FSH_CurrentEXP;
                                        _currentlvl = _playerData.FSH;
                                        break;
                                    case Actor.Job.PLD:
                                        _role = _playerData.GLD_CurrentEXP;
                                        _currentlvl = _playerData.GLD;
                                        break;
                                    case Actor.Job.MNK:
                                        _role = _playerData.PGL_CurrentEXP;
                                        _currentlvl = _playerData.PGL;
                                        break;
                                    case Actor.Job.WAR:
                                        _role = _playerData.MRD_CurrentEXP;
                                        _currentlvl = _playerData.MRD;
                                        break;
                                    case Actor.Job.DRG:
                                        _role = _playerData.LNC_CurrentEXP;
                                        _currentlvl = _playerData.LNC;
                                        break;
                                    case Actor.Job.BRD:
                                        _role = _playerData.ARC_CurrentEXP;
                                        _currentlvl = _playerData.ARC;
                                        break;
                                    case Actor.Job.WHM:
                                        _role = _playerData.CNJ_CurrentEXP;
                                        _currentlvl = _playerData.CNJ;
                                        break;
                                    case Actor.Job.BLM:
                                        _role = _playerData.THM_CurrentEXP;
                                        _currentlvl = _playerData.THM;
                                        break;
                                    case Actor.Job.ACN:
                                        _role = _playerData.ACN_CurrentEXP;
                                        _currentlvl = _playerData.ACN;
                                        break;
                                    case Actor.Job.SMN:
                                        _role = _playerData.ACN_CurrentEXP;
                                        _currentlvl = _playerData.ACN;
                                        break;
                                    case Actor.Job.SCH:
                                        _role = _playerData.ACN_CurrentEXP;
                                        _currentlvl = _playerData.ACN;
                                        break;
                                    case Actor.Job.ROG:
                                        _role = _playerData.ROG_CurrentEXP;
                                        _currentlvl = _playerData.ROG;
                                        break;
                                    case Actor.Job.NIN:
                                        _role = _playerData.ROG_CurrentEXP;
                                        _currentlvl = _playerData.ROG;
                                        break;
                                    case Actor.Job.MCH:
                                        _role = _playerData.MCH_CurrentEXP;
                                        _currentlvl = _playerData.MCH;
                                        break;
                                    case Actor.Job.DRK:
                                        _role = _playerData.DRK_CurrentEXP;
                                        _currentlvl = _playerData.DRK;
                                        break;
                                    case Actor.Job.AST:
                                        _role = _playerData.AST_CurrentEXP;
                                        _currentlvl = _playerData.AST;
                                        break;
                                    case Actor.Job.SAM:
                                        _role = _playerData.SAM_CurrentEXP;
                                        _currentlvl = _playerData.SAM;
                                        break;
                                    case Actor.Job.RDM:
                                        _role = _playerData.RDM_CurrentEXP;
                                        _currentlvl = _playerData.RDM;
                                        break;
                                    default:
                                        _role = _playerData.WVR_CurrentEXP;
                                        _currentlvl = 0;
                                        break;
                                }


                                if (_currentlvl == 70)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", expcolfull, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", expcolfull, false, false);
                                }
                                else
                                {
                                    var lvltranslator = (_role - 0) * (19 - 1) / (Helpers.ExperienceTable[_currentlvl] - 0) + 0;
                                        
                                    for (int i = 1; i < 19; i++)
                                    {
                                        if (lvltranslator >= i)
                                        {
                                            GlobalApplyMapLightbarLighting("Lightbar" + i, expcolfull, false,
                                                false);
                                        }
                                        else
                                        {
                                            GlobalApplyMapLightbarLighting("Lightbar" + i, expcolempty, false,
                                                false);
                                        }
                                    }
                                }
                            }
                            



                            //Job Gauges

                            if (ChromaticsSettings.ChromaticsSettingsJobGaugeToggle)
                            {
                                switch (_playerInfo.Job)
                                {
                                    case Actor.Job.WAR:
                                        var burstwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARWrathBurst);
                                        var maxwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARWrathMax);
                                        var negwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARNegative);
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                                }
                                            }
                                            else if (polWrath <= 10 && polWrath > 0)
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstwarcol, false, false);
                                                }
                                            }
                                            else if (polWrath == 0)
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negwarcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                ToggleGlobalFlash3(false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negwarcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negwarcol, false, false);
                                                }
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

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negwarcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negwarcol, false, false);
                                            }
                                        }

                                        break;
                                    case Actor.Job.PLD:
                                        //Paladin
                                        var negpldcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDNegative);
                                        var shieldcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDShieldOath);
                                        var swordcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDSwordOath);

                                        var oathgauge = Cooldowns.OathGauge;
                                        var oathPol = (oathgauge - 0) * (50 - 0) / (100 - 0) + 0;

                                        if (statEffects.Find(i => i.StatusName == "Shield Oath") != null)
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", shieldcol, false);
                                            GlobalApplyMapKeyLighting("NumAdd", shieldcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", shieldcol, false);

                                            if (oathPol <= 50 && oathPol > 40)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", shieldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", shieldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num2", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num3", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                                }
                                            }
                                            else if (oathPol <= 40 && oathPol > 30)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num2", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num3", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                                }
                                            }
                                            else if (oathPol <= 30 && oathPol > 20)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num2", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num3", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                                }
                                            }
                                            else if (oathPol <= 20 && oathPol > 10)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num2", shieldcol, false);
                                                GlobalApplyMapKeyLighting("Num3", shieldcol, false);

                                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                                }
                                            }
                                            else if (oathPol <= 10 && oathPol > 0)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num0", shieldcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", shieldcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", shieldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", shieldcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num0", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", negpldcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negpldcol, false, false);
                                                }
                                            }
                                        }
                                        else if (statEffects.Find(i => i.StatusName == "Sword Oath") != null)
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", swordcol, false);
                                            GlobalApplyMapKeyLighting("NumAdd", swordcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", swordcol, false);

                                            if (oathPol <= 50 && oathPol > 40)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", swordcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", swordcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num7", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num8", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num9", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num4", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num5", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num6", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num3", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                                }
                                            }
                                            else if (oathPol <= 40 && oathPol > 30)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num8", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num9", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num4", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num5", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num6", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num3", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                                }
                                            }
                                            else if (oathPol <= 30 && oathPol > 20)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num5", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num6", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num3", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                                }
                                            }
                                            else if (oathPol <= 20 && oathPol > 10)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                                GlobalApplyMapKeyLighting("Num3", swordcol, false);

                                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                                }
                                            }
                                            else if (oathPol <= 10 && oathPol > 0)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num0", swordcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", swordcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", swordcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", swordcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                                GlobalApplyMapKeyLighting("Num0", negpldcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", negpldcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negpldcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negpldcol, false, false);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);

                                            GlobalApplyMapKeyLighting("NumSubtract", negpldcol, false);
                                            GlobalApplyMapKeyLighting("NumAdd", negpldcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", negpldcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negpldcol, false);
                                            
                                            GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                            GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                            GlobalApplyMapKeyLighting("Num6", negpldcol, false);
                                            
                                            GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                            GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                            GlobalApplyMapKeyLighting("Num3", negpldcol, false);

                                            GlobalApplyMapKeyLighting("Num0", negpldcol, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", negpldcol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negpldcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negpldcol, false, false);
                                            }
                                        }

                                        break;
                                    case Actor.Job.MNK:
                                        var greased = Cooldowns.GreasedLightningStacks;
                                        var greaseRemaining = Cooldowns.GreasedLightningTimeRemaining;
                                        var burstmnkcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMNKGreased);
                                        var burstmnkempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMNKNegative);

                                        if (greased > 0)
                                        {
                                            if (greaseRemaining > 0 && greaseRemaining <= 5)
                                            {
                                                ToggleGlobalFlash3(true);
                                                GlobalFlash3(burstmnkcol, 150);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstmnkempty, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstmnkempty, false, false);
                                                }
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

                                                        if (_LightbarMode == LightbarMode.JobGauge)
                                                        {
                                                            GlobalApplyMapLightbarLighting("Lightbar19", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar18", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar17", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar16", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar15", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar14", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar13", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar12", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar11", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar10", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar9", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar8", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar7", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar6", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar5", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar4", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar3", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar2", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar1", burstmnkcol, false, false);
                                                        }
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

                                                        if (_LightbarMode == LightbarMode.JobGauge)
                                                        {
                                                            GlobalApplyMapLightbarLighting("Lightbar19", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar18", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar17", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar16", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar15", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar14", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar13", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar12", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar11", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar10", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar9", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar8", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar7", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar6", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar5", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar4", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar3", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar2", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar1", burstmnkcol, false, false);
                                                        }
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

                                                        if (_LightbarMode == LightbarMode.JobGauge)
                                                        {
                                                            GlobalApplyMapLightbarLighting("Lightbar19", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar18", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar17", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar16", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar15", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar14", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar13", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar12", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar11", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar10", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar9", burstmnkempty, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar8", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar7", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar6", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar5", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar4", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar3", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar2", burstmnkcol, false, false);
                                                            GlobalApplyMapLightbarLighting("Lightbar1", burstmnkcol, false, false);
                                                        }
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

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", burstmnkempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", burstmnkempty, false, false);
                                            }
                                        }

                                        break;
                                    case Actor.Job.DRG:

                                        var burstdrgcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGBloodDragon);
                                        var negdrgcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGNegative);
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstdrgcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstdrgcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstdrgcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstdrgcol, false, false);
                                                }
                                            }
                                            else if (polBlood <= 10 && polBlood > 0)
                                            {
                                                //Flash
                                                ToggleGlobalFlash3(true);
                                                GlobalFlash3(burstdrgcol, 150);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negdrgcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negdrgcol, false, false);
                                                }
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

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negdrgcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negdrgcol, false, false);
                                            }
                                        }
                                        break;
                                    case Actor.Job.BRD:
                                        //Bard Songs
                                        var burstcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);
                                        var negcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);
                                        var repcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDRepertoire);

                                        GlobalApplyMapKeyLighting("Num0", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                        GlobalApplyMapKeyLighting("NumDecimal", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);

                                        //Console.WriteLine(@"Song: " + Cooldowns.Song.ToString());

                                        if (Cooldowns.Song != Cooldowns.BardSongs.None)
                                        {
                                            var songremain = Cooldowns.SongTimeRemaining;
                                            var polSong = (songremain - 0) * (50 - 0) / (30 - 0) + 0;
                                            
                                            switch (Cooldowns.Song)
                                            {
                                                case Cooldowns.BardSongs.ArmysPaeon:
                                                    burstcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDArmys);
                                                    negcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);

                                                    GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                    GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                    GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                    break;
                                                case Cooldowns.BardSongs.MagesBallad:
                                                    burstcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDBallad);
                                                    negcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);

                                                    GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                    GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                    GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                    break;
                                                case Cooldowns.BardSongs.WanderersMinuet:
                                                    burstcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDMinuet);
                                                    negcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);

                                                    switch (Cooldowns.RepertoireStacks)
                                                    {
                                                        case 1:
                                                            GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                            GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                            GlobalApplyMapKeyLighting("NumEnter", repcol, false);
                                                            break;
                                                        case 2:
                                                            GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                            GlobalApplyMapKeyLighting("NumAdd", repcol, false);
                                                            GlobalApplyMapKeyLighting("NumEnter", repcol, false);
                                                            break;
                                                        case 3:
                                                            GlobalApplyMapKeyLighting("NumSubtract", repcol, false);
                                                            GlobalApplyMapKeyLighting("NumAdd", repcol, false);
                                                            GlobalApplyMapKeyLighting("NumEnter", repcol, false);
                                                            break;
                                                        default:
                                                            GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                            GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                            GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                                            break;
                                                    }

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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstcol, false, false);
                                                }
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

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", burstcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", burstcol, false, false);
                                                }
                                            }
                                            else if (polSong <= 10 && polSong > 0)
                                            {
                                                //Flash
                                                ToggleGlobalFlash3(true);
                                                GlobalFlash3(burstcol, 150);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negcol, false, false);
                                                }
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

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negcol, false, false);
                                            }
                                        }
                                        break;
                                    case Actor.Job.WHM:
                                        //White Mage
                                        var negwhmcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMNegative);
                                        var flowercol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMFlowerPetal);
                                        var cureproc = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMFreecure);

                                        var petalcount = Cooldowns.FlowerPetals;

                                        if (statEffects.Find(i => i.StatusName == "Freecure") != null)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", cureproc, false);
                                            GlobalApplyMapKeyLighting("NumDivide", cureproc, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", cureproc, false);
                                            GlobalApplyMapKeyLighting("NumSubtract", cureproc, false);
                                            GlobalApplyMapKeyLighting("NumAdd", cureproc, false);
                                            GlobalApplyMapKeyLighting("NumEnter", cureproc, false);
                                            GlobalApplyMapKeyLighting("Num0", cureproc, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", cureproc, false);
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negwhmcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negwhmcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negwhmcol, false);
                                            GlobalApplyMapKeyLighting("NumSubtract", negwhmcol, false);
                                            GlobalApplyMapKeyLighting("NumAdd", negwhmcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", negwhmcol, false);
                                            GlobalApplyMapKeyLighting("Num0", negwhmcol, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", negwhmcol, false);
                                        }

                                        switch (petalcount)
                                        {
                                            case 3:
                                                GlobalApplyMapKeyLighting("Num9", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num6", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num3", flowercol, false);

                                                GlobalApplyMapKeyLighting("Num8", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num5", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num2", flowercol, false);

                                                GlobalApplyMapKeyLighting("Num7", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num4", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num1", flowercol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", flowercol, false, false);
                                                }
                                                break;
                                            case 2:
                                                GlobalApplyMapKeyLighting("Num9", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negwhmcol, false);

                                                GlobalApplyMapKeyLighting("Num8", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num5", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num2", flowercol, false);

                                                GlobalApplyMapKeyLighting("Num7", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num4", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num1", flowercol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", flowercol, false, false);
                                                }
                                                break;
                                            case 1:
                                                GlobalApplyMapKeyLighting("Num9", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negwhmcol, false);

                                                GlobalApplyMapKeyLighting("Num8", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negwhmcol, false);

                                                GlobalApplyMapKeyLighting("Num7", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num4", flowercol, false);
                                                GlobalApplyMapKeyLighting("Num1", flowercol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", flowercol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", flowercol, false, false);
                                                }
                                                break;
                                            default:
                                                GlobalApplyMapKeyLighting("Num9", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negwhmcol, false);

                                                GlobalApplyMapKeyLighting("Num8", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negwhmcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num4", negwhmcol, false);
                                                GlobalApplyMapKeyLighting("Num1", negwhmcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negwhmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negwhmcol, false, false);
                                                }
                                                break;
                                        }


                                        break;
                                    case Actor.Job.BLM:
                                        //Black Mage

                                        var negblmcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMNegative);
                                        var firecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMAstralFire);
                                        var icecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMUmbralIce);
                                        var heartcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMUmbralHeart);
                                        var enochcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMEnochian);

                                        var firestacks = Cooldowns.AstralFire;
                                        var icestacks = Cooldowns.UmbralIce;
                                        var heartstacks = Cooldowns.UmbralHearts;

                                        switch (heartstacks)
                                        {
                                            case 1:
                                                GlobalApplyMapKeyLighting("NumSubtract", negblmcol, false);
                                                GlobalApplyMapKeyLighting("NumAdd", negblmcol, false);
                                                GlobalApplyMapKeyLighting("NumEnter", heartcol, false);
                                                break;
                                            case 2:
                                                GlobalApplyMapKeyLighting("NumSubtract", negblmcol, false);
                                                GlobalApplyMapKeyLighting("NumAdd", heartcol, false);
                                                GlobalApplyMapKeyLighting("NumEnter", heartcol, false);
                                                break;
                                            case 3:
                                                GlobalApplyMapKeyLighting("NumSubtract", heartcol, false);
                                                GlobalApplyMapKeyLighting("NumAdd", heartcol, false);
                                                GlobalApplyMapKeyLighting("NumEnter", heartcol, false);
                                                break;
                                            default:
                                                GlobalApplyMapKeyLighting("NumSubtract", negblmcol, false);
                                                GlobalApplyMapKeyLighting("NumAdd", negblmcol, false);
                                                GlobalApplyMapKeyLighting("NumEnter", negblmcol, false);
                                                break;
                                        }

                                        if (firestacks > 0)
                                        {
                                            switch (firestacks)
                                            {
                                                case 1:
                                                    GlobalApplyMapKeyLighting("NumLock", firecol, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", negblmcol, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                                    break;
                                                case 2:
                                                    GlobalApplyMapKeyLighting("NumLock", firecol, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", firecol, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                                    break;
                                                case 3:
                                                    GlobalApplyMapKeyLighting("NumLock", firecol, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", firecol, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", firecol, false);
                                                    break;
                                            }
                                        }
                                        else if (icestacks > 0)
                                        {
                                            switch (icestacks)
                                            {
                                                case 1:
                                                    GlobalApplyMapKeyLighting("NumLock", icecol, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", negblmcol, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                                    break;
                                                case 2:
                                                    GlobalApplyMapKeyLighting("NumLock", icecol, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", icecol, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                                    break;
                                                case 3:
                                                    GlobalApplyMapKeyLighting("NumLock", icecol, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", icecol, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", icecol, false);
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negblmcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negblmcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                        }

                                        if (Cooldowns.EnochianActive)
                                        {
                                            var enochtime = (Cooldowns.EnochianTimeRemaining - 0) * (40 - 0) / (30 - 0) + 0;

                                            if (enochtime <= 40 && enochtime > 30)
                                            {
                                                GlobalApplyMapKeyLighting("Num7", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num8", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num9", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num4", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num5", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num6", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num1", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num2", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num3", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num0", enochcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", enochcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", enochcol, false, false);
                                                }
                                            }
                                            else if (enochtime <= 30 && enochtime > 20)
                                            {
                                                GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num4", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num5", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num6", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num1", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num2", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num3", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num0", enochcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", enochcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", enochcol, false, false);
                                                }
                                            }
                                            else if (enochtime <= 20 && enochtime > 10)
                                            {
                                                GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num4", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num1", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num2", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num3", enochcol, false);
                                                GlobalApplyMapKeyLighting("Num0", enochcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", enochcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", enochcol, false, false);
                                                }
                                            }
                                            else if (enochtime <= 10 && enochtime > 0)
                                            {
                                                GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num4", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num1", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num0", enochcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", enochcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", enochcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", enochcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num4", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num1", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negblmcol, false);
                                                GlobalApplyMapKeyLighting("Num0", negblmcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", negblmcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negblmcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negblmcol, false, false);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num4", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num5", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num6", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num1", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num2", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num3", negblmcol, false);
                                            GlobalApplyMapKeyLighting("Num0", negblmcol, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", negblmcol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negblmcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negblmcol, false, false);
                                            }
                                        }
                                        
                                        break;
                                    case Actor.Job.SMN:
                                        var aetherflowsmn = Cooldowns.AetherflowCount;

                                        var burstsmncol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSMNAetherflow);
                                        var burstsmnempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSMNNegative);

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

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", burstsmncol, false, false);
                                                    }
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

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", burstsmncol, false, false);
                                                    }
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

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", burstsmnempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", burstsmncol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", burstsmncol, false, false);
                                                    }
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

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", burstsmnempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", burstsmnempty, false, false);
                                            }
                                        }

                                        break;
                                    case Actor.Job.SCH:

                                        var aetherflowsch = Cooldowns.AetherflowCount;

                                        var burstschcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSCHAetherflow);
                                        var burstschempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSCHNegative);

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

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", burstschcol, false, false);
                                                    }
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

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", burstschcol, false, false);
                                                    }
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

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", burstschempty, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", burstschcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", burstschcol, false, false);
                                                    }
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

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", burstschempty, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", burstschempty, false, false);
                                            }
                                        }

                                        break;
                                    case Actor.Job.NIN:
                                        //Ninja
                                        var negnincol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobNINNegative);
                                        var hutoncol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobNINHuton);

                                        var hutonremain = Cooldowns.HutonTimeRemaining;
                                        var polHuton = (hutonremain - 0) * (50 - 0) / (70 - 0) + 0;

                                        if (polHuton <= 50 && polHuton > 40)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", hutoncol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", hutoncol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", hutoncol, false);

                                            GlobalApplyMapKeyLighting("Num7", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num8", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num9", hutoncol, false);

                                            GlobalApplyMapKeyLighting("Num4", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num5", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num6", hutoncol, false);

                                            GlobalApplyMapKeyLighting("Num1", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num2", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num3", hutoncol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", hutoncol, false, false);
                                            }
                                        }
                                        else if (polHuton <= 40 && polHuton > 30)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negnincol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negnincol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negnincol, false);

                                            GlobalApplyMapKeyLighting("Num7", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num8", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num9", hutoncol, false);

                                            GlobalApplyMapKeyLighting("Num4", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num5", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num6", hutoncol, false);

                                            GlobalApplyMapKeyLighting("Num1", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num2", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num3", hutoncol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", hutoncol, false, false);
                                            }
                                        }
                                        else if (polHuton <= 30 && polHuton > 20)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negnincol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negnincol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negnincol, false);

                                            GlobalApplyMapKeyLighting("Num7", negnincol, false);
                                            GlobalApplyMapKeyLighting("Num8", negnincol, false);
                                            GlobalApplyMapKeyLighting("Num9", negnincol, false);

                                            GlobalApplyMapKeyLighting("Num4", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num5", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num6", hutoncol, false);

                                            GlobalApplyMapKeyLighting("Num1", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num2", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num3", hutoncol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", hutoncol, false, false);
                                            }
                                        }
                                        else if (polHuton <= 20 && polHuton > 10)
                                        {
                                            ToggleGlobalFlash3(false);

                                            GlobalApplyMapKeyLighting("NumLock", negnincol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negnincol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negnincol, false);

                                            GlobalApplyMapKeyLighting("Num7", negnincol, false);
                                            GlobalApplyMapKeyLighting("Num8", negnincol, false);
                                            GlobalApplyMapKeyLighting("Num9", negnincol, false);

                                            GlobalApplyMapKeyLighting("Num4", negnincol, false);
                                            GlobalApplyMapKeyLighting("Num5", negnincol, false);
                                            GlobalApplyMapKeyLighting("Num6", negnincol, false);

                                            GlobalApplyMapKeyLighting("Num1", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num2", hutoncol, false);
                                            GlobalApplyMapKeyLighting("Num3", hutoncol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", hutoncol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", hutoncol, false, false);
                                            }
                                        }
                                        else if (polHuton <= 10 && polHuton > 0)
                                        {
                                            //Flash
                                            ToggleGlobalFlash3(true);
                                            GlobalFlash3(hutoncol, 150);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negnincol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negnincol, false, false);
                                            }
                                        }
                                        else
                                        {
                                            ToggleGlobalFlash3(false);
                                        }

                                        break;
                                    case Actor.Job.DRK:
                                        var negdrkcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRKNegative);
                                        var bloodcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRKBloodGauge);
                                        var gritcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRKGrit);
                                        var darksidecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRKDarkside);

                                        var bloodgauge = Cooldowns.BloodGauge;
                                        var bloodPol = (bloodgauge - 0) * (50 - 0) / (100 - 0) + 0;

                                        if (statEffects.Find(i => i.StatusName == "Darkside") != null)
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", darksidecol, false);
                                            GlobalApplyMapKeyLighting("NumAdd", darksidecol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", darksidecol, false);
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("NumAdd", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", negdrkcol, false);
                                        }


                                        if (bloodPol <= 50 && bloodPol > 40)
                                        {
                                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", gritcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", gritcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num7", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num8", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num9", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num4", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num5", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num6", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num1", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num2", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num3", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", gritcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", bloodcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", bloodcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num7", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num8", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num9", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num4", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num5", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num6", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", bloodcol, false, false);
                                                }
                                            }
                                        }
                                        else if (bloodPol <= 40 && bloodPol > 30)
                                        {
                                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num7", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num8", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num9", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num4", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num5", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num6", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num1", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num2", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num3", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", gritcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num7", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num8", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num9", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num4", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num5", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num6", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", bloodcol, false, false);
                                                }
                                            }
                                        }
                                        if (bloodPol <= 30 && bloodPol > 20)
                                        {
                                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num4", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num5", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num6", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num1", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num2", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num3", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", gritcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num4", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num5", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num6", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", bloodcol, false, false);
                                                }
                                            }
                                        }
                                        else if (bloodPol <= 20 && bloodPol > 10)
                                        {
                                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num1", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num2", gritcol, false);
                                                GlobalApplyMapKeyLighting("Num3", gritcol, false);

                                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", gritcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", gritcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", bloodcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", bloodcol, false, false);
                                                }
                                            }
                                        }
                                        else if (bloodPol <= 10 && bloodPol > 0)
                                        {
                                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num1", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negdrkcol, false, false);
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num1", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num2", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negdrkcol, false);

                                                GlobalApplyMapKeyLighting("Num0", negdrkcol, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", negdrkcol, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", negdrkcol, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", negdrkcol, false, false);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                            GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                            GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                            GlobalApplyMapKeyLighting("Num1", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("Num2", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("Num3", negdrkcol, false);

                                            GlobalApplyMapKeyLighting("Num0", negdrkcol, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", negdrkcol, false);
                                        }
                                        break;
                                    case Actor.Job.AST:
                                        var burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTNegative);

                                        if (Cooldowns.CurrentCard != Cooldowns.CardTypes.None)
                                        {
                                            switch (Cooldowns.CurrentCard)
                                            {
                                                case Cooldowns.CardTypes.Arrow:
                                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTArrow);
                                                    break;
                                                case Cooldowns.CardTypes.Balance:
                                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTBalance);
                                                    break;
                                                case Cooldowns.CardTypes.Bole:
                                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTBole);
                                                    break;
                                                case Cooldowns.CardTypes.Ewer:
                                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTEwer);
                                                    break;
                                                case Cooldowns.CardTypes.Spear:
                                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTSpear);
                                                    break;
                                                case Cooldowns.CardTypes.Spire:
                                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTSpire);
                                                    break;
                                            }

                                            if (Cooldowns.CurrentCard != _currentCard)
                                            {
                                                if (Cooldowns.CurrentCard != Cooldowns.CardTypes.None)
                                                    GlobalRipple1(burstastcol, 80, baseColor);

                                                _currentCard = Cooldowns.CurrentCard;
                                            }

                                            GlobalApplyMapKeyLighting("NumLock", burstastcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", burstastcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", burstastcol, false);
                                            GlobalApplyMapKeyLighting("NumSubtract", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num7", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num8", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num9", burstastcol, false);
                                            GlobalApplyMapKeyLighting("NumAdd", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num4", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstastcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num1", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num0", burstastcol, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", burstastcol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", burstastcol, false, false);
                                            }
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
                                            GlobalApplyMapKeyLighting("NumAdd", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num4", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num5", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num6", burstastcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num1", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num2", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num3", burstastcol, false);
                                            GlobalApplyMapKeyLighting("Num0", burstastcol, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", burstastcol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", burstastcol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", burstastcol, false, false);
                                            }
                                        }
                                        break;
                                    case Actor.Job.MCH:
                                        //Machinist
                                        var ammo = Cooldowns.AmmoCount;

                                        var ammoburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMCHAmmo);
                                        var negmchburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMCHNegative);

                                        var heatnormal = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMCHHeatGauge);
                                        var heatover = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMCHOverheat);

                                        switch (ammo)
                                        {
                                            case 1:
                                                GlobalApplyMapKeyLighting("NumSubtract", negmchburst, false);
                                                GlobalApplyMapKeyLighting("NumAdd", negmchburst, false);
                                                GlobalApplyMapKeyLighting("NumEnter", ammoburst, false);
                                                break;
                                            case 2:
                                                GlobalApplyMapKeyLighting("NumSubtract", negmchburst, false);
                                                GlobalApplyMapKeyLighting("NumAdd", ammoburst, false);
                                                GlobalApplyMapKeyLighting("NumEnter", ammoburst, false);
                                                break;
                                            case 3:
                                                GlobalApplyMapKeyLighting("NumSubtract", ammoburst, false);
                                                GlobalApplyMapKeyLighting("NumAdd", ammoburst, false);
                                                GlobalApplyMapKeyLighting("NumEnter", ammoburst, false);
                                                break;
                                            default:
                                                GlobalApplyMapKeyLighting("NumSubtract", negmchburst, false);
                                                GlobalApplyMapKeyLighting("NumAdd", negmchburst, false);
                                                GlobalApplyMapKeyLighting("NumEnter", negmchburst, false);
                                                break;
                                        }

                                        if (Cooldowns.GaussBarrelEnabled)
                                        {
                                            var mchoverheat = Cooldowns.OverHeatTime;
                                            var mchgb = Cooldowns.HeatGauge;

                                            if (mchoverheat > 0)
                                            {
                                                //Overheating
                                                GlobalApplyMapKeyLighting("NumLock", heatover, false);
                                                GlobalApplyMapKeyLighting("NumDivide", heatover, false);
                                                GlobalApplyMapKeyLighting("NumMultiply", heatover, false);
                                                GlobalApplyMapKeyLighting("Num7", heatover, false);
                                                GlobalApplyMapKeyLighting("Num8", heatover, false);
                                                GlobalApplyMapKeyLighting("Num9", heatover, false);
                                                GlobalApplyMapKeyLighting("Num4", heatover, false);
                                                GlobalApplyMapKeyLighting("Num5", heatover, false);
                                                GlobalApplyMapKeyLighting("Num6", heatover, false);
                                                GlobalApplyMapKeyLighting("Num1", heatover, false);
                                                GlobalApplyMapKeyLighting("Num2", heatover, false);
                                                GlobalApplyMapKeyLighting("Num3", heatover, false);
                                                GlobalApplyMapKeyLighting("Num0", heatover, false);
                                                GlobalApplyMapKeyLighting("NumDecimal", heatover, false);

                                                if (_LightbarMode == LightbarMode.JobGauge)
                                                {
                                                    GlobalApplyMapLightbarLighting("Lightbar19", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar18", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar17", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar16", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar15", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar14", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar13", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar12", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar11", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar10", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar9", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar8", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar7", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar6", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar5", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar4", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar3", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar2", heatover, false, false);
                                                    GlobalApplyMapLightbarLighting("Lightbar1", heatover, false, false);
                                                }
                                            }
                                            else
                                            {
                                                //Normal
                                                var polGB = (mchgb - 0) * (50 - 0) / (100 - 0) + 0;
                                                if (polGB <= 50 && polGB > 40)
                                                {
                                                    GlobalApplyMapKeyLighting("NumLock", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num7", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num8", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num9", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num4", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num5", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num6", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num1", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num2", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num3", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                                    }
                                                }
                                                else if (polGB <= 40 && polGB > 30)
                                                {
                                                    GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num7", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num8", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num9", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num4", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num5", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num6", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num1", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num2", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num3", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                                    }
                                                }
                                                else if (polGB <= 30 && polGB > 20)
                                                {
                                                    GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num4", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num5", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num6", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num1", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num2", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num3", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                                    }
                                                }
                                                else if (polGB <= 20 && polGB > 10)
                                                {
                                                    GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num4", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num5", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num6", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num1", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num2", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num3", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", heatnormal, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                                    }
                                                }
                                                else if (polGB <= 10 && polGB > 0)
                                                {
                                                    GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num4", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num5", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num6", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num1", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num2", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num3", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", heatnormal, false, false);
                                                    }
                                                }
                                                else if (polGB == 0)
                                                {
                                                    GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num4", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num5", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num6", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num1", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num2", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num3", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("Num0", negmchburst, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negmchburst, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", negmchburst, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", negmchburst, false, false);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num4", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num5", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num6", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num1", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num2", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num3", negmchburst, false);
                                            GlobalApplyMapKeyLighting("Num0", negmchburst, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", negmchburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negmchburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negmchburst, false, false);
                                            }
                                        }

                                        break;
                                    case Actor.Job.SAM:
                                        //Samurai
                                        var negsamcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMNegative);
                                        var setsucol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMSetsu); //Top
                                        var getsucol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMGetsu); //Left
                                        var kacol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMKa); //Right
                                        var kenkicol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMKenki);

                                        var sen = Cooldowns.SenGauge;
                                        var kenkicharge = Cooldowns.KenkiCharge;
                                        var PolKenki = (kenkicharge - 0) * (40 - 0) / (100 - 0) + 0;
                                        
                                        switch (sen)
                                        {
                                            case 1:
                                                GlobalApplyMapKeyLighting("Num8", setsucol, false);
                                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negsamcol, false);
                                                break;
                                            case 2:
                                                GlobalApplyMapKeyLighting("Num8", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num1", getsucol, false);
                                                GlobalApplyMapKeyLighting("Num3", negsamcol, false);
                                                break;
                                            case 3:
                                                GlobalApplyMapKeyLighting("Num8", setsucol, false);
                                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num3", getsucol, false);
                                                break;
                                            case 4:
                                                GlobalApplyMapKeyLighting("Num8", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num3", kacol, false);
                                                break;
                                            case 5:
                                                GlobalApplyMapKeyLighting("Num8", setsucol, false);
                                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num3", kacol, false);
                                                break;
                                            case 6:
                                                GlobalApplyMapKeyLighting("Num8", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num1", getsucol, false);
                                                GlobalApplyMapKeyLighting("Num3", kacol, false);
                                                break;
                                            case 7:
                                                GlobalApplyMapKeyLighting("Num8", setsucol, false);
                                                GlobalApplyMapKeyLighting("Num1", getsucol, false);
                                                GlobalApplyMapKeyLighting("Num3", kacol, false);
                                                break;
                                            default:
                                                GlobalApplyMapKeyLighting("Num8", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                                GlobalApplyMapKeyLighting("Num3", negsamcol, false);
                                                break;
                                        }
                                        
                                        if (PolKenki <= 40 && PolKenki > 30)
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", kenkicol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", kenkicol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", kenkicol, false);
                                            GlobalApplyMapKeyLighting("NumLock", kenkicol, false);
                                        }
                                        else if (PolKenki <= 30 && PolKenki > 20)
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", kenkicol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", kenkicol, false);
                                            GlobalApplyMapKeyLighting("NumLock", kenkicol, false);
                                        }
                                        else if (PolKenki <= 20 && PolKenki > 10)
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", kenkicol, false);
                                            GlobalApplyMapKeyLighting("NumLock", kenkicol, false);
                                        }
                                        else if (PolKenki <= 10 && PolKenki > 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumLock", kenkicol, false);
                                        }
                                        else if (PolKenki == 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumSubtract", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", negsamcol, false);
                                            GlobalApplyMapKeyLighting("NumLock", negsamcol, false);
                                        }

                                        GlobalApplyMapKeyLighting("Num2", negsamcol, false);
                                        GlobalApplyMapKeyLighting("Num4", negsamcol, false);
                                        GlobalApplyMapKeyLighting("Num5", negsamcol, false);
                                        GlobalApplyMapKeyLighting("Num6", negsamcol, false);
                                        GlobalApplyMapKeyLighting("Num7", negsamcol, false);
                                        GlobalApplyMapKeyLighting("Num9", negsamcol, false);
                                        GlobalApplyMapKeyLighting("NumAdd", negsamcol, false);
                                        GlobalApplyMapKeyLighting("NumEnter", negsamcol, false);
                                        GlobalApplyMapKeyLighting("Num0", negsamcol, false);
                                        GlobalApplyMapKeyLighting("NumDecimal", negsamcol, false);

                                        break;
                                    case Actor.Job.RDM:
                                        var blackmana = Cooldowns.BlackMana;
                                        var whitemana = Cooldowns.WhiteMana;
                                        var polBlack = (blackmana - 0) * (40 - 0) / (100 - 0) + 0;
                                        var polWhite = (whitemana - 0) * (40 - 0) / (100 - 0) + 0;

                                        //Console.WriteLine(@"RDM: " + Cooldowns.BlackMana.ToString());

                                        var blackburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMBlackMana);
                                        var whiteburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMWhiteMana);
                                        var negburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMNegative);

                                        GlobalApplyMapKeyLighting("NumDivide", Color.Black, false);
                                        GlobalApplyMapKeyLighting("Num8", Color.Black, false);
                                        GlobalApplyMapKeyLighting("Num5", Color.Black, false);
                                        GlobalApplyMapKeyLighting("Num2", Color.Black, false);

                                        //Black
                                        if (polBlack <= 40 && polBlack > 30)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num9", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num3", blackburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", blackburst, false, false);
                                            }
                                        }
                                        else if (polBlack <= 30 && polBlack > 20)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                            GlobalApplyMapKeyLighting("Num9", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num3", blackburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", blackburst, false, false);
                                            }
                                        }
                                        else if (polBlack <= 20 && polBlack > 10)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                                            GlobalApplyMapKeyLighting("Num3", blackburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", blackburst, false, false);
                                            }
                                        }
                                        else if (polBlack <= 10 && polBlack > 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                                            GlobalApplyMapKeyLighting("Num6", negburst, false);
                                            GlobalApplyMapKeyLighting("Num3", blackburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", blackburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", blackburst, false, false);
                                            }
                                        }
                                        else if (polBlack == 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                                            GlobalApplyMapKeyLighting("Num6", negburst, false);
                                            GlobalApplyMapKeyLighting("Num3", negburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negburst, false, false);
                                            }
                                        }


                                        //White
                                        if (polWhite <= 40 && polWhite > 30)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar9", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", whiteburst, false, false);
                                            }
                                        }
                                        else if (polWhite <= 30 && polWhite > 20)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                            GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar9", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negburst, false, false);
                                            }
                                        }
                                        else if (polWhite <= 20 && polWhite > 10)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar9", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negburst, false, false);
                                            }
                                        }
                                        else if (polWhite <= 10 && polWhite > 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                                            GlobalApplyMapKeyLighting("Num4", negburst, false);
                                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar9", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", whiteburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negburst, false, false);
                                            }
                                        }
                                        else if (polWhite == 0)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                                            GlobalApplyMapKeyLighting("Num4", negburst, false);
                                            GlobalApplyMapKeyLighting("Num1", negburst, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar9", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negburst, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negburst, false, false);
                                            }
                                        }

                                        break;
                                    case Actor.Job.ALC:
                                    case Actor.Job.ARM:
                                    case Actor.Job.BSM:
                                    case Actor.Job.CPT:
                                    case Actor.Job.CUL:
                                    case Actor.Job.GSM:
                                    case Actor.Job.LTW:
                                    case Actor.Job.WVR:
                                        //Crafter
                                        var negcraftercol = Color.Black;
                                        var innerquietcol = Color.BlueViolet;
                                        var collectablecol = Color.Gold;
                                        var craftercol = Color.DodgerBlue;

                                        if (statEffects.Find(i => i.StatusName == "Collectable Synthesis") != null)
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", collectablecol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", collectablecol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", collectablecol, false);
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("NumLock", craftercol, false);
                                            GlobalApplyMapKeyLighting("NumDivide", craftercol, false);
                                            GlobalApplyMapKeyLighting("NumMultiply", craftercol, false);
                                        }

                                        if (statEffects.Find(i => i.StatusName == "Inner Quiet") != null)
                                        {
                                            var IQStacks = statEffects.Find(i => i.StatusName == "Inner Quiet").Stacks;
                                            switch (IQStacks)
                                            {
                                                case 1:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 2:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 3:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 4:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 5:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 6:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", innerquietcol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 7:
                                                    GlobalApplyMapKeyLighting("Num7", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 8:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 9:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", innerquietcol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 10:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 11:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", innerquietcol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                case 12:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num2", innerquietcol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", innerquietcol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", innerquietcol, false, false);
                                                    }
                                                    break;
                                                default:
                                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                                    if (_LightbarMode == LightbarMode.JobGauge)
                                                    {
                                                        GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar6", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar5", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar4", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar3", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar2", negcraftercol, false, false);
                                                        GlobalApplyMapLightbarLighting("Lightbar1", negcraftercol, false, false);
                                                    }

                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                            GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                            GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                            GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                            GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                            GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                            GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                            GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                            GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                            GlobalApplyMapKeyLighting("Num0", innerquietcol, false);
                                            GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                            if (_LightbarMode == LightbarMode.JobGauge)
                                            {
                                                GlobalApplyMapLightbarLighting("Lightbar19", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar18", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar17", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar16", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar15", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar14", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar13", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar12", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar11", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar10", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar9", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar8", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar7", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar6", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar5", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar4", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar3", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar2", negcraftercol, false, false);
                                                GlobalApplyMapLightbarLighting("Lightbar1", negcraftercol, false, false);
                                            }
                                        }


                                        GlobalApplyMapKeyLighting("NumEnter", negcraftercol, false);
                                        GlobalApplyMapKeyLighting("NumAdd", negcraftercol, false);
                                        GlobalApplyMapKeyLighting("NumSubtract", negcraftercol, false);

                                        break;

                                    case Actor.Job.FSH:
                                    case Actor.Job.BTN:
                                    case Actor.Job.MIN:
                                        //Gatherer
                                        GlobalApplyMapKeyLighting("NumLock", baseColor, false);
                                        GlobalApplyMapKeyLighting("NumDivide", baseColor, false);
                                        GlobalApplyMapKeyLighting("NumMultiply", baseColor, false);
                                        GlobalApplyMapKeyLighting("NumSubtract", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num7", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num8", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num9", baseColor, false);
                                        GlobalApplyMapKeyLighting("NumAdd", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num4", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num5", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num6", baseColor, false);
                                        GlobalApplyMapKeyLighting("NumEnter", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num1", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num2", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num3", baseColor, false);
                                        GlobalApplyMapKeyLighting("Num0", baseColor, false);
                                        GlobalApplyMapKeyLighting("NumDecimal", baseColor, false);

                                        if (_LightbarMode == LightbarMode.JobGauge)
                                        {
                                            GlobalApplyMapLightbarLighting("Lightbar19", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2", baseColor, false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1", baseColor, false, false);
                                        }

                                        break;

                                }
                            }
                            else
                            {
                                ToggleGlobalFlash3(false);
                                GlobalApplyMapKeyLighting("NumLock", baseColor, false);
                                GlobalApplyMapKeyLighting("NumDivide", baseColor, false);
                                GlobalApplyMapKeyLighting("NumMultiply", baseColor, false);
                                GlobalApplyMapKeyLighting("NumSubtract", baseColor, false);
                                GlobalApplyMapKeyLighting("Num7", baseColor, false);
                                GlobalApplyMapKeyLighting("Num8", baseColor, false);
                                GlobalApplyMapKeyLighting("Num9", baseColor, false);
                                GlobalApplyMapKeyLighting("NumAdd", baseColor, false);
                                GlobalApplyMapKeyLighting("Num4", baseColor, false);
                                GlobalApplyMapKeyLighting("Num5", baseColor, false);
                                GlobalApplyMapKeyLighting("Num6", baseColor, false);
                                GlobalApplyMapKeyLighting("NumEnter", baseColor, false);
                                GlobalApplyMapKeyLighting("Num1", baseColor, false);
                                GlobalApplyMapKeyLighting("Num2", baseColor, false);
                                GlobalApplyMapKeyLighting("Num3", baseColor, false);
                                GlobalApplyMapKeyLighting("Num0", baseColor, false);
                                GlobalApplyMapKeyLighting("NumDecimal", baseColor, false);

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    GlobalApplyMapLightbarLighting("Lightbar19", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar18", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar17", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar16", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar15", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar14", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar13", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar12", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar11", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar10", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar9", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar8", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar7", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar6", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar5", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar4", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar3", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar2", baseColor, false, false);
                                    GlobalApplyMapLightbarLighting("Lightbar1", baseColor, false, false);
                                }
                            }

                            //Duty Finder Bell

                            FfxivDutyFinder.RefreshData();
                            //Debug.WriteLine(FFXIVInterfaces.FFXIVDutyFinder.isPopped() + "//" + FFXIVInterfaces.FFXIVDutyFinder.Countdown());

                            if (FfxivDutyFinder.IsPopped())
                            {
                                //Debug.WriteLine("DF Pop");
                                if (!_dfpop)
                                {
                                    ToggleGlobalFlash4(true);
                                    GlobalFlash4(baseColor,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), 500,
                                        DeviceEffects.GlobalKeys);

                                    GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));
                                    GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapKeypadLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));

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

                                    if (_LightbarMode == LightbarMode.DutyFinder)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                    }

                                    _dfpop = true;
                                }
                                else
                                {
                                    if (FfxivDutyFinder.Countdown() < 10 && !_dfcount)
                                    {
                                        ToggleGlobalFlash4(false);
                                        GlobalFlash4(baseColor,
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), 200,
                                            DeviceEffects.GlobalKeys);

                                        GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));
                                        GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapHeadsetLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapKeypadLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapChromaLinkLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));

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

                                        if (_LightbarMode == LightbarMode.DutyFinder)
                                        {
                                            GlobalApplyMapLightbarLighting("Lightbar19", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar18", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar17", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar16", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar15", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar14", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar13", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar12", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar11", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar10", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar9", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar8", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar7", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar6", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar5", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar4", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar3", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar2", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                            GlobalApplyMapLightbarLighting("Lightbar1", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, false);
                                        }

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
                                    SetPadbase = false;
                                    SetHeadsetbase = false;
                                    SetKeypadbase = false;
                                    SetCLbase = false;

                                    GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, baseColor);
                                    GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, baseColor, false);
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.DutyFinder, baseColor, false);
                                    GlobalApplyMapKeypadLighting(DevModeTypes.DutyFinder, baseColor, false);
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.DutyFinder, baseColor);

                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip1", "Strip8", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip2", "Strip9", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip3", "Strip10", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip4", "Strip11", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip5", "Strip12", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip6", "Strip13", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "Strip7", "Strip14", baseColor, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 14, 5, 0, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 13, 6, 1, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 12, 7, 2, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 11, 8, 3, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 10, 9, 4, baseColor, false);

                                    if (_LightbarMode == LightbarMode.DutyFinder)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", baseColor, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", baseColor, false, false);
                                    }
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
 