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
        private Color _baseWeatherColor = Color.Black;
        private Color _highlightWeatherColor = Color.Black;

        private Cooldowns.CardTypes _currentCard;
        private string _currentStatus = "";
        private bool _dfcount;
        private bool _dfpop;
        private int _hp;
        private bool _targeted;
        private bool _castalert;
        private int _lastWeather = 0;
        private bool _weathertoggle;
        private bool _inCutscene;
        private bool _inVegas;
        private int _catchMenuchange;
        private int _countMainMenuHold;

        /* Parse FFXIV Function
         * Read the data from Sharlayan and call lighting functions according
         */

        private bool _lastcast;
        private bool _menuNotify;

        private ActorItem _playerInfo = new ActorItem();
        private ActorItem _menuInfo = new ActorItem();
        private ConcurrentDictionary<uint, ActorItem> _playerInfoX = new ConcurrentDictionary<uint, ActorItem>();

        private bool _playgroundonce;
        private bool _successcast;

        private FFXIVUnsafeMethods _unsafe = new FFXIVUnsafeMethods();
        private readonly object _CallLcdData = new object();
        private readonly object _CallFFXIVData = new object();

        public void FfxivGameStop()
        {
            if (Attatched == 0) return;

            //Console.WriteLine("Debug trip");

            HoldReader = true;
            _ffxiVcts.Cancel();
            _attachcts.Cancel();

            if (ArxSdkCalled == 1 && ArxState == 0)
                _arx.ArxSetIndex("info.html");

            MemoryTasks.Cleanup();

            //Watchdog.WatchdogStop();
            //_gameResetCatch.Enabled = false;
            WriteConsole(ConsoleTypes.Ffxiv, @"Game stopped");
            SetFormName(@"Chromatics " + _currentVersionX);


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
            GlobalStopParticleEffects();
            GlobalStopCycleEffects();
            GlobalApplyAllDeviceLighting(ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
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
                    WriteConsole(ConsoleTypes.Ffxiv, @"Attempting Attach..");
                    SetFormName(@"Chromatics " + _currentVersionX);

                    if (Init)
                    {
                        WriteConsole(ConsoleTypes.Ffxiv, @"Chromatics already attached.");
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

                    WriteConsole(ConsoleTypes.Ffxiv, @"DX9 Initiated");
                    WriteConsole(ConsoleTypes.Error,
                        "DX9 support has been phased out from Chromatics. Please use DX11 when using Chromatics.");
                }

                // DX11
                else if (processes11.Length > 0)
                {
                    WriteConsole(ConsoleTypes.Ffxiv, @"Attempting Attach..");
                    SetFormName(@"Chromatics " + _currentVersionX);
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

                    WriteConsole(ConsoleTypes.Ffxiv, @"DX11 Initiated");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "The remote server returned an error: (500) Internal Server Error." || ex.Message == "The remote server returned an error: (400) Page Not Found.")
                {
                    WriteConsole(ConsoleTypes.Error, @"The external API server experienced an error. Please use local cache or try again later.");
                    WriteConsole(ConsoleTypes.Error, @"Error: " + ex.Message);
                }
                else
                {
                    WriteConsole(ConsoleTypes.Error, @"Error: " + ex.Message);
                    WriteConsole(ConsoleTypes.Error, @"Internal Error: " + ex.StackTrace);
                }
            }

            return initiated;
        }

        /* Memory Loop */

        private async Task CallFfxivMemory(CancellationTokenSource ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && !_exit)
                {
                    if (_exit)
                    {
                        ct.Cancel();
                    }

                    ReadFfxivMemory();
                    await Task.Delay(300);
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

                    _playerInfoX = Reader.GetActors().CurrentPCs;
                    _menuInfo = ActorItem.CurrentUser; //Reader.GetPlayerInfo().PlayerEntity;
                    
                    if (_playerInfoX.Count == 0)
                    {
                        _catchMenuchange++;
                    }
                    else
                    {
                        _catchMenuchange = 0;

                    }

                    if (Attatched == 3)
                    {
                        //Game is Running
                        //Check if game has stopped by checking Character data for a null value.

                        _countMainMenuHold = 0;

                        //if ((_menuInfo == null || _menuInfo.Name == "") && _catchMenuchange > 5)
                        if (_catchMenuchange > 5)
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
                                GlobalStopParticleEffects();
                                GlobalStopCycleEffects();
                                GlobalApplyAllDeviceLighting(ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                                
                                //WriteConsole(ConsoleTypes.Ffxiv, @"Returning to Main Menu..");
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
                            {
                                lock (_CallFFXIVData)
                                {
                                    ProcessFfxivData();
                                }
                            }
                        }
                    }
                    else
                    {
                        //Game Not Running
                        if (Attatched == 1)
                        {
                            State = 6;
                            //GlobalUpdateState("wave", Color.Magenta, false, Color.MediumSeaGreen, true, 40);
                            //GlobalSetWave();
                            GlobalStopParticleEffects();
                            GlobalStopCycleEffects();
                            GlobalApplyAllDeviceLighting(ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                            
                            Attatched = 2;
                            _countMainMenuHold = 0;
                        }

                        //Game in Menu
                        else if (Attatched == 2)
                        {
                            if (_menuInfo != null && _menuInfo.Name != "" && _catchMenuchange <= 5)
                            //if (_catchMenuchange <= 5)
                            {
                                //Set Game Active
                                WriteConsole(ConsoleTypes.Ffxiv, @"Game Running (" + _menuInfo.Name + ")");
                                SetFormName(@"Chromatics " + _currentVersionX + @" (Running)");


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
                                _countMainMenuHold = 0;

                                FFXIVWeather.GetWeatherAPI();

                                if (ArxSdkCalled == 1)
                                {
                                    if (ArxState != 0) return;
                                    if (cb_arx_mode.SelectedIndex < 3)
                                    {
                                        ArxState = cb_arx_mode.SelectedIndex + 1;
                                        _arx.SetArxCurrentID(ArxState);

                                        switch (ArxState)
                                        {
                                            case 1:
                                                _arx.ArxSetIndex("playerhud.html");
                                                break;
                                            case 2:
                                                _arx.ArxSetIndex("partylist.html");
                                                break;
                                            case 3:
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
                                if (_countMainMenuHold == 5)
                                {
                                    GlobalApplyAllDeviceLighting(
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingMenuBase));
                                    GlobalParticleEffects(
                                        new Color[]
                                        {
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingMenuHighlight1),
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingMenuHighlight2),
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingMenuHighlight3),
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingMenuBase)
                                        }, null, 20);
                                }

                                _countMainMenuHold++;

                                if (!_menuNotify)
                                {
                                    WriteConsole(ConsoleTypes.Ffxiv, @"Main Menu is still active.");
                                    SetFormName(@"Chromatics " + _currentVersionX + @" (Paused)");
                                    
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
                WriteConsole(ConsoleTypes.Error, @"Init Error: " + ex.Message);
                WriteConsole(ConsoleTypes.Error, @"Internal Error: " + ex.StackTrace);
            }
        }

        private int xi = 0;
        private int xi_interval = 50;
        private int xi_bench = 50;
        private int xi_scan = 20;

        private void ProcessFfxivData()
        {
            MemoryTasks.Cleanup();

            //Check for crash
            GlobalCheckCrash();

            if (!ChromaticsSettings.ChromaticsSettingsMemoryCheck)
            {
                if (xi > xi_interval)
                {
                    //Console.WriteLine(@"Ping");
                    using (var proc = Process.GetCurrentProcess())
                    {

                        if (proc.PrivateMemorySize64 / 1024 > 153600)
                        {
                            WriteConsole(ConsoleTypes.Error,
                                "Chromatics exceeded maximum memory size. Pausing execution (Memory Size: " +
                                proc.PrivateMemorySize64 / 1024 + "MB).");
                            SetFormName(@"Chromatics " + _currentVersionX + @" (Paused)");
                            HoldReader = true;
                            xi_interval = xi_scan;
                        }
                        else
                        {
                            if (HoldReader && xi_interval == xi_scan)
                            {
                                WriteConsole(ConsoleTypes.Ffxiv,
                                    "Resuming Execution (Memory Size: " + proc.PrivateMemorySize64 / 1024 + "MB)..");
                                SetFormName(@"Chromatics " + _currentVersionX + @" (Running)");
                                HoldReader = false;
                                xi_interval = xi_bench;
                            }
                        }
                    }

                    xi = 0;
                }
                else
                {
                    xi++;
                }
            }
            else
            {
                if (HoldReader && xi_interval == xi_scan)
                {
                    WriteConsole(ConsoleTypes.Ffxiv,
                        "Resuming Execution..");
                    SetFormName(@"Chromatics " + _currentVersionX + @" (Running)");
                    HoldReader = false;
                    xi_interval = xi_bench;
                }
            }

            if (HoldReader) return;
            
            try
            {
                //Get Data
                var targetInfo = new ActorItem();
                var targetEmnityInfo = new List<EnmityItem>();
                var partyInfo = new ConcurrentDictionary<uint, PartyMember>();
                var partyListNew = new ConcurrentDictionary<uint, PartyMember>();
                var partyListOld = new ConcurrentDictionary<uint, PartyMember>();
                //var personalInfo = new PlayerEntity();


                Reader.GetActors();
                //_playerInfoX = Reader.GetActors()?.PCEntities;
                _playerInfo = ActorItem.CurrentUser;
                var _playerData = Reader.GetCurrentPlayer();


                try
                {
                    if (_playerInfo.Name != "" && _playerInfo.TargetType != Actor.TargetType.Unknown)
                    {
                        if (Reader.CanGetTargetInfo())
                        {
                            targetInfo = Reader.GetTargetInfo()?.TargetInfo?.CurrentTarget;
                        }

                        if (Reader.CanGetEnmityEntities())
                        {
                            targetEmnityInfo = Reader.GetTargetInfo()?.TargetInfo?.EnmityItems;
                        }

                        //Console.WriteLine(@"Name:" + targetInfo.Name);
                    }


                    partyInfo = Reader.GetPartyMembers()?.PartyMembers;

                    partyListNew = Reader.GetPartyMembers()?.NewPartyMembers;
                    partyListOld = Reader.GetPartyMembers()?.RemovedPartyMembers;

                    //personalInfo = Reader.GetPlayerInfo()?.PlayerEntity;

                    
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error, @"Parser B: " + ex.Message);
                }


                //Cutscenes
                if (ChromaticsSettings.ChromaticsSettingsCutsceneAnimation && !_inVegas)
                {
                    if (_playerInfo.IconID == 15)
                    {
                        if (!_inCutscene)
                        {
                            GlobalApplyAllDeviceLighting(ColorTranslator.FromHtml(ColorMappings.ColorMappingCutsceneBase));

                            GlobalParticleEffects(
                                new Color[]
                                {
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingCutsceneHighlight1),
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingCutsceneHighlight2),
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingCutsceneHighlight3),
                                    ColorTranslator.FromHtml(ColorMappings.ColorMappingCutsceneBase)
                                }, null,
                                20);

                            _inCutscene = true;
                        }

                        return;
                    }
                    else
                    {
                        if (_inCutscene)
                        {
                            _inCutscene = false;
                            SetKeysbase = false;
                        }

                        GlobalStopParticleEffects();
                    }
                }
                else
                {
                    if (!_inVegas)
                        GlobalStopParticleEffects();
                }

                //DF Bell
                FfxivDutyFinder.RefreshData();

                //Vegas Mode
                if (ChromaticsSettings.ChromaticsSettingsVegasMode)
                {
                    if (_playerInfo.MapTerritory == 144 && !FfxivDutyFinder.IsPopped())
                    {
                        if (!_inVegas)
                        {
                            //Console.WriteLine(@"Vegas Mode On");
                            ToggleGlobalFlash4(false);
                            GlobalApplyAllDeviceLighting(ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));

                            GlobalParticleEffects(
                                new Color[]
                                {
                                    Color.Red,
                                    Color.Orange,
                                    Color.Green,
                                    Color.DodgerBlue,
                                    Color.Magenta,
                                    Color.Purple
                                }, null,
                                50);

                            GlobalColorCycle();

                            _inVegas = true;
                        }

                        return;
                    }
                    else
                    {
                        if (_inVegas)
                        {
                            _inVegas = false;
                            SetKeysbase = false;
                            //Console.WriteLine(@"Vegas Mode Off");
                        }

                        GlobalStopParticleEffects();
                        GlobalStopCycleEffects();
                    }
                }
                else
                {
                    GlobalStopParticleEffects();
                    GlobalStopCycleEffects();
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
                                        //var pid = partyListNew[i];
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
                                            ptEmnityno = emnitytableX.FindIndex(a => a.Key == partyInfo[i].ID)
                                                .ToString();
                                        }
                                        else
                                        {
                                            ptEmnityno = "0";
                                        }

                                        switch (partyInfo[i].Job)
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

                                        datastring[i] = "1," + ptType + "," + partyInfo[i].Name + "," +
                                                        partyInfo[i].HPPercent.ToString("#0%") + "," +
                                                        partyInfo[i].HPCurrent + "," +
                                                        partyInfo[i].MPPercent.ToString("#0%") + "," +
                                                        partyInfo[i].MPCurrent + "," + ptTppercent + "," +
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
                                var weatherMapbaseKey = _mappingPalette.FirstOrDefault(x =>
                                    x.Value[0] == FFXIVWeather.WeatherIconName(_lastWeather) + @" (Base)").Key;
                                var weatherMaphighlightKey = _mappingPalette.FirstOrDefault(x =>
                                    x.Value[0] == FFXIVWeather.WeatherIconName(_lastWeather) + @" (Highlight)").Key;

                                if (string.IsNullOrWhiteSpace(weatherMapbaseKey) ||
                                    string.IsNullOrWhiteSpace(weatherMaphighlightKey))
                                {
                                    _baseWeatherColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor);
                                    _highlightWeatherColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingHighlightColor);

                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.ReactiveWeather,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor), false, "All");
                                }
                                else
                                {
                                    var weatherMapbase = (string)typeof(Datastore.FfxivColorMappings)
                                        .GetField(weatherMapbaseKey).GetValue(ColorMappings);
                                    var weatherMaphighlight = (string)typeof(Datastore.FfxivColorMappings)
                                        .GetField(weatherMaphighlightKey).GetValue(ColorMappings);

                                    _baseWeatherColor = ColorTranslator.FromHtml(weatherMapbase);
                                    _highlightWeatherColor = ColorTranslator.FromHtml(weatherMaphighlight);

                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.ReactiveWeather,
                                        ColorTranslator.FromHtml(weatherMapbase), false, "All");
                                }
                                

                                baseColor = _baseWeatherColor;
                                highlightColor = _highlightWeatherColor;
                                
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

                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.DefaultColor, _baseColor, "All");
                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, _baseColor, "All");
                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, _baseColor, "All");

                            GlobalApplyKeySingleLighting(DevModeTypes.HighlightColor, highlightColor);
                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.HighlightColor, highlightColor, "All");
                            GlobalUpdateBulbState(BulbModeTypes.HighlightColor, highlightColor, 100);

                            if (_LightbarMode == LightbarMode.DefaultColor)
                            {
                                foreach (var f in DeviceEffects.LightbarZones)
                                {
                                    GlobalApplyMapLightbarLighting(f, _baseColor, false, false);
                                }
                            }
                            else if (_LightbarMode == LightbarMode.HighlightColor)
                            {
                                foreach (var f in DeviceEffects.LightbarZones)
                                {
                                    GlobalApplyMapLightbarLighting(f, highlightColor, false, false);
                                }
                            }

                            SetKeysbase = true;
                        }

                        if (SetMousebase == false)
                        {
                            GlobalApplyMapMouseLighting(DevModeTypes.DefaultColor, baseColor, false);
                            GlobalApplyMapMouseLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapMouseLighting(DevModeTypes.Castbar, baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "LeftSide1", "RightSide1", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "LeftSide2", "RightSide2", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "LeftSide3", "RightSide3", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "LeftSide4", "RightSide4", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "LeftSide5", "RightSide5", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "LeftSide6", "RightSide6", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.DefaultColor, "LeftSide7", "RightSide7", baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide1", "RightSide1", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide2", "RightSide2", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide3", "RightSide3", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide4", "RightSide4", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", baseColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", baseColor, false);

                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "LeftSide1", "RightSide1", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "LeftSide2", "RightSide2", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "LeftSide3", "RightSide3", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "LeftSide4", "RightSide4", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "LeftSide5", "RightSide5", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "LeftSide6", "RightSide6", highlightColor, false);
                            GlobalApplyStripMouseLighting(DevModeTypes.HighlightColor, "LeftSide7", "RightSide7", highlightColor, false);

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
                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.DefaultColor, baseColor, false, "All");
                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, baseColor, false, "All");
                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, baseColor, false, "All");

                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.HighlightColor, highlightColor, false, "All");

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
                            GlobalApplyMapKeyLighting("LeftAlt", highlightColor, false);
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
                            GlobalApplyMapKeyLighting("LeftAlt", _baseColor, false);
                        }
                        
                        if (targetInfo == null)
                        {
                            GlobalApplyMapKeyLighting("PrintScreen",
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalUpdateBulbState(BulbModeTypes.EnmityTracker,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), 250);

                            GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));
                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), "All");
                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false, "All");
                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));

                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

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

                        //Check Base Weather
                        if (ChromaticsSettings.ChromaticsSettingsReactiveWeather)
                        {
                            if (_baseWeatherColor != _baseColor)
                            {
                                _baseColor = _baseWeatherColor;
                            }

                            if (_baseWeatherColor != baseColor)
                            {
                                baseColor = _baseWeatherColor;
                            }

                            if (_highlightWeatherColor != highlightColor)
                            {
                                highlightColor = _highlightWeatherColor;
                            }
                        }


                        //Debuff Status Effects

                        //if (PlayerInfo.IsClaimed)
                        //{
                        
                        
                        var statEffects = _playerInfo.StatusItems;
                        
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
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Petrification")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingPetrification);
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Old")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSlow);
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Slow")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingSlow), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSlow);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Stun")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingStun);
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Silence")
                                    {
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSilence);
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 250);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Poison")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingPoison), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingPoison);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Pollen")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingPollen), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingPollen);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Pox")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingPox), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingPox);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Paralysis")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingParalysis),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingParalysis);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Leaden")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingLeaden), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingLeaden);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Incapacitation")
                                    {
                                        GlobalRipple2(
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingIncapacitation),
                                            100);
                                        _baseColor =
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingIncapacitation);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Dropsy")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingDropsy), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingDropsy);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Amnesia")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingAmnesia),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingAmnesia);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Bleed")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingBleed), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBleed);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Misery")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingMisery), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingMisery);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Sleep")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingSleep), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingSleep);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Daze")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingDaze), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingDaze);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Heavy")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingHeavy), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingHeavy);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Infirmary")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingInfirmary),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingInfirmary);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Burns")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingBurns), 100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingBurns);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Deep Freeze")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingDeepFreeze),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingDeepFreeze);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else if (status.StatusName == "Damage Down")
                                    {
                                        GlobalRipple2(ColorTranslator.FromHtml(ColorMappings.ColorMappingDamageDown),
                                            100);
                                        _baseColor = ColorTranslator.FromHtml(ColorMappings.ColorMappingDamageDown);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 1000);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");
                                    }
                                    else
                                    {
                                        _baseColor = baseColor;
                                        //GlobalUpdateState("static", _baseColor, false);
                                        GlobalApplyAllKeyLighting(_baseColor);
                                        GlobalUpdateBulbState(BulbModeTypes.StatusEffects, _baseColor, 500);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.StatusEffects,
                                            _baseColor, false, "All");

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

                                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker,
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), "All");

                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false, "All");
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

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

                                GlobalApplyKeySingleLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), polTargetHpx2);

                                GlobalApplyMapMouseLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), polTargetHpx2);

                                //Lightbar

                                if (_LightbarMode == LightbarMode.TargetHp)
                                {
                                    var LBTargetHp_Collection = DeviceEffects.LightbarZones;
                                    var LBTargetHp_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, LBTargetHp_Collection.Length, 0);

                                    for (int i = 0; i < LBTargetHp_Collection.Length; i++)
                                    {
                                        GlobalApplyMapLightbarLighting(LBTargetHp_Collection[i], LBTargetHp_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, false);
                                    }
                                }

                                var KeyTargetHp_Collection = DeviceEffects.MacroTarget;
                                var KeyTargetHp_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, KeyTargetHp_Collection.Length, 0);

                                for (int i = 0; i < KeyTargetHp_Collection.Length; i++)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyMapKeyLighting(KeyTargetHp_Collection[i],
                                            KeyTargetHp_Interpolate > i
                                                ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed)
                                                : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                            false);
                                    }
                                    else
                                    {
                                        GlobalApplyMapKeyLighting(KeyTargetHp_Collection[i],
                                            KeyTargetHp_Interpolate > i
                                                ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle)
                                                : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty),
                                            false);
                                    }
                                }

                                //Mouse
                                var TargetHpMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                var TargetHpMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;
                                var TargetHpMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, TargetHpMouseStrip_CollectionA.Length, 0);

                                for (int i = 0; i < TargetHpMouseStrip_CollectionA.Length; i++)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, TargetHpMouseStrip_CollectionA[i], TargetHpMouseStrip_CollectionB[i], TargetHpMouseStrip_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    }
                                    else
                                    {
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, TargetHpMouseStrip_CollectionA[i], TargetHpMouseStrip_CollectionB[i], TargetHpMouseStrip_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    }
                                        
                                }

                                //Mousepad
                                var TargetHpMousePadCollection = 5;
                                var TargetHpMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, TargetHpMousePadCollection, 0);

                                for (int i = 0; i < TargetHpMousePadCollection; i++)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10 + i, 9 - i, 4 - i, TargetHpMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    }
                                    else
                                    {
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10 + i, 9 - i, 4 - i, TargetHpMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    }
                                }

                                //Keypad
                                var TargetHpKeypad_Collection = DeviceEffects.Keypadzones;
                                var TargetHpKeypad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, TargetHpKeypad_Collection.Length, 0);

                                for (int i = 0; i < TargetHpKeypad_Collection.Length; i++)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, TargetHpKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, TargetHpKeypad_Collection[i]);
                                    }
                                    else
                                    {
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, TargetHpKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, TargetHpKeypad_Collection[i]);
                                    }
                                }

                                //MultiKeyboard
                                var TargetHpMulti_Collection = DeviceEffects.Multikeyzones;
                                var TargetHpMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, TargetHpMulti_Collection.Length, 0);

                                for (int i = 0; i < TargetHpMulti_Collection.Length; i++)
                                {
                                    if (targetInfo.IsClaimed)
                                    {
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, TargetHpMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), TargetHpMulti_Collection[i]);
                                    }
                                    else
                                    {
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, TargetHpMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), TargetHpMulti_Collection[i]);
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

                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), "All");

                                    GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false, "All");
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting));

                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);

                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    
                                    GlobalApplyMapKeyLighting("Macro16",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapKeyLighting("Macro17",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
                                    GlobalApplyMapKeyLighting("Macro18",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetCasting), false);
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

                                    //WriteConsole(ConsoleTypes.Ffxiv, targetInfo.Name + @": " + targetInfo.ID);

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
                                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, colEm0, "All");
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm0, false);
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm0, false);
                                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, colEm0, false, "All");
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm0);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", colEm0, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", colEm0, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm0, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm0, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm0, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm0, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm0, false);

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.LightbarZones)
                                                {
                                                    GlobalApplyMapLightbarLighting(f, colEm0, false, false);

                                                }
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
                                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, colEm1, "All");
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm1, false);
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm1, false);
                                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, colEm1, false, "All");
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm1);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", colEm1, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", colEm1, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm1, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm1, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm1, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm1, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm1, false);

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.LightbarZones)
                                                {
                                                    GlobalApplyMapLightbarLighting(f, colEm1, false, false);

                                                }
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
                                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, colEm2, "All");
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm2, false);
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm2, false);
                                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, colEm2, false, "All");
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm2);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", colEm2, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", colEm2, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm2, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm2, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm2, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm2, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm2, false);

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.LightbarZones)
                                                {
                                                    GlobalApplyMapLightbarLighting(f, colEm2, false, false);

                                                }
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
                                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, colEm3, "All");
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm3, false);
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm3, false);
                                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, colEm3, false, "All");
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm3);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", colEm3, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", colEm3, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm3, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm3, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm3, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm3, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm3, false);

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.LightbarZones)
                                                {
                                                    GlobalApplyMapLightbarLighting(f, colEm3, false, false);

                                                }
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
                                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, colEm4, "All");
                                            GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm4, false);
                                            GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm4, false);
                                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, colEm4, false, "All");
                                            GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm4);

                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", colEm4, false);
                                            GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", colEm4, false);

                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm4, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm4, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm4, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm4, false);
                                            GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm4, false);

                                            if (_LightbarMode == LightbarMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.LightbarZones)
                                                {
                                                    GlobalApplyMapLightbarLighting(f, colEm4, false, false);

                                                }
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
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, colEm0, "All");
                                        GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, colEm0, false);
                                        GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, colEm0, false);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, colEm0, false, "All");
                                        GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, colEm0);

                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", colEm0, false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", colEm0, false);

                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, colEm0, false);
                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, colEm0, false);
                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, colEm0, false);
                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, colEm0, false);
                                        GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, colEm0, false);

                                        if (_LightbarMode == LightbarMode.EnmityTracker)
                                        {
                                            foreach (var f in DeviceEffects.LightbarZones)
                                            {
                                                GlobalApplyMapLightbarLighting(f, colEm0, false, false);

                                            }
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
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), "All");
                                GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false, "All");
                                GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity));

                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingNoEmnity), false);

                                if (_LightbarMode == LightbarMode.TargetHp || _LightbarMode == LightbarMode.EnmityTracker)
                                {
                                    foreach (var f in DeviceEffects.LightbarZones)
                                    {
                                        GlobalApplyMapLightbarLighting(f, _baseColor, false, false);

                                    }
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

                                GlobalApplyMapKeyLighting("Macro1", _baseColor, false);
                                GlobalApplyMapKeyLighting("Macro2", _baseColor, false);
                                GlobalApplyMapKeyLighting("Macro3", _baseColor, false);
                                GlobalApplyMapKeyLighting("Macro4", _baseColor, false);
                                GlobalApplyMapKeyLighting("Macro5", _baseColor, false);
                                GlobalUpdateBulbState(BulbModeTypes.EnmityTracker, baseColor, 1000);
                                GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, baseColor);
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, baseColor, "All");
                                GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, baseColor, false);
                                GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, baseColor, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, baseColor, false, "All");
                                GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, baseColor);

                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide1", "RightSide1", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide2", "RightSide2", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide3", "RightSide3", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide4", "RightSide4", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide5", "RightSide5", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide6", "RightSide6", baseColor, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.EnmityTracker, "LeftSide7", "RightSide7", baseColor, false);

                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 14, 5, 0, baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 13, 6, 1, baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 12, 7, 2, baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 11, 8, 3, baseColor, false);
                                GlobalApplyMapPadLighting(DevModeTypes.EnmityTracker, 10, 9, 4, baseColor, false);
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("Macro1", _baseColor, false);
                            GlobalApplyMapKeyLighting("Macro2", _baseColor, false);
                            GlobalApplyMapKeyLighting("Macro3", _baseColor, false);
                            GlobalApplyMapKeyLighting("Macro4", _baseColor, false);
                            GlobalApplyMapKeyLighting("Macro5", _baseColor, false);
                            GlobalUpdateBulbState(BulbModeTypes.TargetHp, baseColor, 1000);
                            GlobalApplyKeySingleLighting(DevModeTypes.TargetHp, baseColor);
                            GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, baseColor, "All");
                            GlobalApplyMapMouseLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapHeadsetLighting(DevModeTypes.TargetHp, baseColor, false);
                            GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, baseColor, false, "All");
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

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.Castbar, colCastempty, colCastcharge, castPercentage);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.Castbar, colCastempty, colCastcharge, false, castPercentage);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.TargetHp, colCastempty, colCastcharge, false, castPercentage);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TargetHp, colCastempty, colCastcharge, castPercentage);

                            
                            //Cast Charge Keys
                            var Castcharge_Collection = DeviceEffects.Functions;

                            var Castcharge_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Double(castPercentage, 0.0, 1.0,
                                    Castcharge_Collection.Length, 0);
                            
                            for (int i = 0; i < Castcharge_Collection.Length; i++)
                            {
                                if (Castcharge_Interpolate >= i && ChromaticsSettings.ChromaticsSettingsCastToggle)
                                {
                                    GlobalApplyMapKeyLighting(Castcharge_Collection[i], colCastcharge, false);

                                    if (i == Castcharge_Collection.Length - 1) _successcast = true;
                                }
                                else
                                {
                                    if (i == Castcharge_Collection.Length) _successcast = true;
                                }
                            }

                            //Mousepad
                            var CastMousePadCollection = 5;
                            var CastMousePad_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Double(castPercentage, 0.0, 1.0,
                                    CastMousePadCollection, 0);

                            for (int i = 0; i < CastMousePadCollection; i++)
                            {
                                if (Castcharge_Interpolate >= i && ChromaticsSettings.ChromaticsSettingsCastToggle)
                                {
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10 + i, 9 - i, 4 - i, colCastcharge, false);
                                }
                            }

                            //Keypad
                            var CastKeypad_Collection = DeviceEffects.Keypadzones;
                            var CastKeypad_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Double(castPercentage, 0.0, 1.0,
                                    CastKeypad_Collection.Length, 0);

                            for (int i = 0; i < CastKeypad_Collection.Length; i++)
                            {
                                if (Castcharge_Interpolate >= i && ChromaticsSettings.ChromaticsSettingsCastToggle)
                                {
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, CastKeypad_Collection[i]);
                                }
                            }

                            //MultiKeyboard
                            var CastMulti_Collection = DeviceEffects.Multikeyzones;
                            var CastMulti_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Double(castPercentage, 0.0, 1.0,
                                    CastMulti_Collection.Length, 0);

                            for (int i = 0; i < CastMulti_Collection.Length; i++)
                            {
                                if (Castcharge_Interpolate >= i && ChromaticsSettings.ChromaticsSettingsCastToggle)
                                {
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, CastMulti_Collection[i]);
                                }
                            }

                            //Mouse
                            var CastMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                            var CastMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;
                            var CastMouseStrip_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Double(castPercentage, 0.0, 1.0,
                                    CastMouseStrip_CollectionA.Length, 0);

                            for (int i = 0; i < CastMouseStrip_CollectionA.Length; i++)
                            {
                                if (Castcharge_Interpolate >= i && ChromaticsSettings.ChromaticsSettingsCastToggle)
                                {
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar,
                                        CastMouseStrip_CollectionA[i],
                                        CastMouseStrip_CollectionB[i], colCastcharge, false);
                                }
                            }

                            //Lightbar
                            if (_LightbarMode == LightbarMode.Castbar)
                            {
                                var CastLightbar_Collection = DeviceEffects.LightbarZones;
                                var CastLightbar_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Double(castPercentage, 0.0, 1.0,
                                        CastLightbar_Collection.Length, 0);

                                for (int i = 0; i < CastLightbar_Collection.Length; i++)
                                {
                                    if (Castcharge_Interpolate >= i && ChromaticsSettings.ChromaticsSettingsCastToggle)
                                    {
                                        GlobalApplyMapLightbarLighting(CastLightbar_Collection[i], colCastcharge, false,
                                            false);
                                    }
                                }
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

                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide1", "RightSide1",
                                        baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide2", "RightSide2",
                                        baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide3", "RightSide3",
                                        baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide4", "RightSide4",
                                        baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5",
                                        baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6",
                                        baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7",
                                        baseColor, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, baseColor, false);

                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, baseColor, false, "0");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, baseColor, false, "1");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, baseColor, false, "2");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, baseColor, false, "3");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, baseColor, false, "4");

                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, baseColor, "0");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, baseColor, "1");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, baseColor, "2");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, baseColor, "3");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, baseColor, "4");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, baseColor, "5");

                                    if (_LightbarMode == LightbarMode.Castbar)
                                    {
                                        foreach (var f in DeviceEffects.LightbarZones)
                                        {
                                            GlobalApplyMapLightbarLighting(f, _baseColor, false, false);
                                        }
                                    }
                                }

                                var cBulbRip1 = new Task(() =>
                                {
                                    GlobalUpdateBulbState(BulbModeTypes.Castbar, baseColor, 500);
                                    GlobalApplyKeySingleLighting(DevModeTypes.Castbar, baseColor);
                                    GlobalApplyMapMouseLighting(DevModeTypes.Castbar, baseColor, false);
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.Castbar, baseColor, false);
                                    //GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, baseColor, false, "All");
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.Castbar, baseColor);
                                });
                                MemoryTasks.Add(cBulbRip1);
                                MemoryTasks.Run(cBulbRip1);


                                if (_successcast && ChromaticsSettings.ChromaticsSettingsCastAnimate)
                                {
                                    GlobalRipple1(colCastcharge, 80, _baseColor);

                                    if (_KeypadZone1Mode == DevMultiModeTypes.Castbar || _KeysMultiKeyMode == DevMultiModeTypes.Castbar)
                                        GlobalMultiRipple1(colCastcharge, 200, _baseColor);
                                }

                                _lastcast = false;
                                _successcast = false;
                            }
                        }

                        //HP
                        if (maxHp != 0)
                        {
                            var polHp = (currentHp - 0) * (40 - 0) / (maxHp - 0) + 0;
                            var polHpz = (currentHp - 0) * (65535 - 0) / (maxHp - 0) + 0;
                            var polHpz2 = (currentHp - 0) * (1.0 - 0.0) / (maxHp - 0) + 0.0;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.HpTracker, polHp <= 10 ? colHpempty : colHpfull, (ushort)polHpz, 250);
                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpcritical : colHpfull, polHpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpempty : colHpfull, polHpz2);


                            //FKeys
                            if (_FKeyMode == FKeyMode.HpMpTp)
                            {
                                var HpFunction_Collection = DeviceEffects.Function1;
                                var HpFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp,
                                        HpFunction_Collection.Length, 0);

                                for (int i = 0; i < HpFunction_Collection.Length; i++)
                                {
                                    if (_playerInfo.IsCasting || !ChromaticsSettings.ChromaticsSettingsShowStats)
                                    {
                                        break;
                                    }

                                    GlobalApplyMapKeyLighting(HpFunction_Collection[i],
                                        HpFunction_Interpolate > i ? colHpfull : colHpempty, false);
                                }
                            }

                            if (_FKeyMode == FKeyMode.HpTracker)
                            {
                                var HpFunction_Collection = DeviceEffects.Functions;
                                var HpFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp,
                                        HpFunction_Collection.Length, 0);

                                for (int i = 0; i < HpFunction_Collection.Length; i++)
                                {
                                    if (_playerInfo.IsCasting || !ChromaticsSettings.ChromaticsSettingsShowStats)
                                    {
                                        break;
                                    }

                                    GlobalApplyMapKeyLighting(HpFunction_Collection[i],
                                        HpFunction_Interpolate > i ? colHpfull : colHpempty, false);
                                }
                            }

                            //Mousepad
                            var HpMousePadCollection = 5;
                            var HpMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpMousePadCollection, 0);

                            for (int i = 0; i < HpMousePadCollection; i++)
                            {
                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 10+i, 9-i, 4-i, HpMousePad_Interpolate > i ? colHpfull : colHpempty, false);
                            }

                            //Keypad
                            var HpKeypad_Collection = DeviceEffects.Keypadzones;
                            var HpKeypad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpKeypad_Collection.Length, 0);

                            for (int i = 0; i < HpKeypad_Collection.Length; i++)
                            {
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, HpKeypad_Interpolate > i ? colHpfull : colHpempty, false, HpKeypad_Collection[i]);
                            }

                            //MultiKeyboard
                            var HpMulti_Collection = DeviceEffects.Multikeyzones;
                            var HpMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpMulti_Collection.Length, 0);

                            for (int i = 0; i < HpMulti_Collection.Length; i++)
                            {
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, HpMulti_Interpolate > i ? colHpfull : colHpempty, HpMulti_Collection[i]);
                            }

                            //Mouse
                            var HpMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                            var HpMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;
                            var HpMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpMouseStrip_CollectionA.Length, 0);

                            for (int i = 0; i < HpMouseStrip_CollectionA.Length; i++)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, HpMouseStrip_CollectionA[i], HpMouseStrip_CollectionB[i], HpMouseStrip_Interpolate > i ? colHpfull : colHpempty, false);
                            }

                            //Lightbar
                            var HpLightbar_Collection = DeviceEffects.LightbarZones;
                            var HpLightbar_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpLightbar_Collection.Length, 0);

                            for (int i = 0; i < HpLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(HpLightbar_Collection[i], HpLightbar_Interpolate > i ? colHpfull : colHpempty, false, false);
                            }
                            
                        }

                        //MP
                        if (maxMp != 0)
                        {
                            var polMp = (currentMp - 0) * (40 - 0) / (maxMp - 0) + 0;
                            var polMpz = (currentMp - 0) * (65535 - 0) / (maxMp - 0) + 0;
                            var polMpz2 = (currentMp - 0) * (1.0 - 0.0) / (maxMp - 0) + 0.0;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.MpTracker, colMpfull, (ushort)polMpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, polMpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, false, polMpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, false, polMpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, polMpz2);

                            //FKeys
                            if (_FKeyMode == FKeyMode.HpMpTp)
                            {
                                var MpFunction_Collection = DeviceEffects.Function2;
                                var MpFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp,
                                        MpFunction_Collection.Length, 0);

                                for (int i = 0; i < MpFunction_Collection.Length; i++)
                                {
                                    if (_playerInfo.IsCasting || !ChromaticsSettings.ChromaticsSettingsShowStats)
                                    {
                                        break;
                                    }

                                    GlobalApplyMapKeyLighting(MpFunction_Collection[i],
                                        MpFunction_Interpolate > i ? colMpfull : colMpempty, false);
                                }
                            }

                            if (_FKeyMode == FKeyMode.MpTracker)
                            {
                                var MpFunction_Collection = DeviceEffects.Functions;
                                var MpFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp,
                                        MpFunction_Collection.Length, 0);

                                for (int i = 0; i < MpFunction_Collection.Length; i++)
                                {
                                    if (_playerInfo.IsCasting || !ChromaticsSettings.ChromaticsSettingsShowStats)
                                    {
                                        break;
                                    }

                                    GlobalApplyMapKeyLighting(MpFunction_Collection[i],
                                        MpFunction_Interpolate > i ? colMpfull : colMpempty, false);
                                }
                            }

                            //Mousepad
                            var MpMousePadCollection = 5;
                            var MpMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp, MpMousePadCollection, 0);

                            for (int i = 0; i < MpMousePadCollection; i++)
                            {
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 10 + i, 9 - i, 4 - i, MpMousePad_Interpolate > i ? colMpfull : colMpempty, false);
                            }

                            //Keypad
                            var MpKeypad_Collection = DeviceEffects.Keypadzones;
                            var MpKeypad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp, MpKeypad_Collection.Length, 0);

                            for (int i = 0; i < MpKeypad_Collection.Length; i++)
                            {
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, MpKeypad_Interpolate > i ? colMpfull : colMpempty, false, MpKeypad_Collection[i]);
                            }

                            //MultiKeyboard
                            var MpMulti_Collection = DeviceEffects.Multikeyzones;
                            var MpMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp, MpMulti_Collection.Length, 0);

                            for (int i = 0; i < MpMulti_Collection.Length; i++)
                            {
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, MpMulti_Interpolate > i ? colMpfull : colMpempty, MpMulti_Collection[i]);
                            }

                            //Mouse
                            var MpMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                            var MpMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;
                            var MpMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp, MpMouseStrip_CollectionA.Length, 0);

                            for (int i = 0; i < MpMouseStrip_CollectionA.Length; i++)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, MpMouseStrip_CollectionA[i], MpMouseStrip_CollectionB[i], MpMouseStrip_Interpolate > i ? colMpfull : colMpempty, false);
                            }

                            //Lightbar
                            var MpLightbar_Collection = DeviceEffects.LightbarZones;
                            var MpLightbar_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp, MpLightbar_Collection.Length, 0);

                            for (int i = 0; i < MpLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(MpLightbar_Collection[i], MpLightbar_Interpolate > i ? colMpfull : colMpempty, false, false);
                            }
                        }

                        //TP
                        if (maxTp != 0)
                        {
                            var polTp = (currentTp - 0) * (40 - 0) / (maxTp - 0) + 0;
                            var polTpz = (currentTp - 0) * (65535 - 0) / (maxTp - 0) + 0;
                            var polTpz2 = (currentTp - 0) * (1.0 - 0.0) / (maxTp - 0) + 0.0;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.TpTracker, colTpfull, (ushort)polTpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.TpTracker, colTpempty, colTpfull, polTpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.TpTracker, colTpempty, colTpfull, false, polTpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.TpTracker, colTpempty, colTpfull, false, polTpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TpTracker, colTpempty, colTpfull, polTpz2);

                            //FKeys
                            if (_FKeyMode == FKeyMode.HpMpTp)
                            {
                                var TpFunction_Collection = DeviceEffects.Function3;
                                var TpFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentTp, 0, maxTp,
                                        TpFunction_Collection.Length, 0);

                                for (int i = 0; i < TpFunction_Collection.Length; i++)
                                {
                                    if (_playerInfo.IsCasting || !ChromaticsSettings.ChromaticsSettingsShowStats)
                                    {
                                        break;
                                    }

                                    GlobalApplyMapKeyLighting(TpFunction_Collection[i],
                                        TpFunction_Interpolate > i ? colTpfull : colTpempty, false);
                                }
                            }

                            if (_FKeyMode == FKeyMode.TpTracker)
                            {
                                var TpFunction_Collection = DeviceEffects.Functions;
                                var TpFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentTp, 0, maxTp,
                                        TpFunction_Collection.Length, 0);

                                for (int i = 0; i < TpFunction_Collection.Length; i++)
                                {
                                    if (_playerInfo.IsCasting || !ChromaticsSettings.ChromaticsSettingsShowStats)
                                    {
                                        break;
                                    }

                                    GlobalApplyMapKeyLighting(TpFunction_Collection[i],
                                        TpFunction_Interpolate > i ? colTpfull : colTpempty, false);
                                }
                            }

                            //Mousepad
                            var TpMousePadCollection = 5;
                            var TpMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentTp, 0, maxTp, TpMousePadCollection, 0);

                            for (int i = 0; i < TpMousePadCollection; i++)
                            {
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 10 + i, 9 - i, 4 - i, TpMousePad_Interpolate > i ? colTpfull : colTpempty, false);
                            }

                            //Keypad
                            var TpKeypad_Collection = DeviceEffects.Keypadzones;
                            var TpKeypad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentTp, 0, maxTp, TpKeypad_Collection.Length, 0);

                            for (int i = 0; i < TpKeypad_Collection.Length; i++)
                            {
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, TpKeypad_Interpolate > i ? colTpfull : colTpempty, false, TpKeypad_Collection[i]);
                            }

                            //MultiKeyboard
                            var TpMulti_Collection = DeviceEffects.Multikeyzones;
                            var TpMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentTp, 0, maxTp, TpMulti_Collection.Length, 0);

                            for (int i = 0; i < TpMulti_Collection.Length; i++)
                            {
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, TpMulti_Interpolate > i ? colTpfull : colTpempty, TpMulti_Collection[i]);
                            }

                            //Mouse
                            var TpMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                            var TpMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;
                            var TpMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentTp, 0, maxTp, TpMouseStrip_CollectionA.Length, 0);

                            for (int i = 0; i < TpMouseStrip_CollectionA.Length; i++)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, TpMouseStrip_CollectionA[i], TpMouseStrip_CollectionB[i], TpMouseStrip_Interpolate > i ? colTpfull : colTpempty, false);
                            }

                            //Lightbar
                            var TpLightbar_Collection = DeviceEffects.LightbarZones;
                            var TpLightbar_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentTp, 0, maxTp, TpLightbar_Collection.Length, 0);

                            for (int i = 0; i < TpLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(TpLightbar_Collection[i], TpLightbar_Interpolate > i ? colTpfull : colTpempty, false, false);
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

                                    foreach (var hotbar in hotbars.ActionContainers)
                                    {
                                        if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_1 ||
                                            hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_2 ||
                                            hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_3 ||
                                            hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_4 ||
                                            hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_5 ||
                                            hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_6 ||
                                            hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_7 ||
                                            hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_8 ||
                                            hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_PETBAR) continue;

                                        foreach (var action in hotbar.ActionItems)
                                        {
                                            if (!action.IsKeyBindAssigned || string.IsNullOrEmpty(action.Name) ||
                                                string.IsNullOrEmpty(action.KeyBinds) ||
                                                string.IsNullOrEmpty(action.ActionKey)) continue;

                                            //Console.WriteLine(action.Name);
                                            
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

                                            if (ChromaticsSettings.ChromaticsSettingsQwertyMode == KeyRegion.AZERTY)
                                            {
                                                if (FfxivHotbar.KeybindtranslationAZERTY.ContainsKey(action.ActionKey))
                                                {
                                                    var keyid = FfxivHotbar.KeybindtranslationAZERTY[action.ActionKey];

                                                    if (_modsactive == 0)

                                                        if (action.Category == 49 || action.Category == 51)
                                                        {
                                                            if (!action.IsAvailable || !action.InRange || _playerInfo.IsCasting || action.CoolDownPercent > 0)
                                                            {
                                                                GlobalApplyMapKeyLighting(keyid,
                                                                    ColorTranslator.FromHtml(ColorMappings
                                                                        .ColorMappingHotbarNotAvailable), false, true);

                                                                continue;
                                                            }

                                                            switch (action.Name)
                                                            {
                                                                case "Map":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindMap), false, true);
                                                                    break;
                                                                case "Aether Currents":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindAetherCurrents), false, true);
                                                                    break;
                                                                case "Signs":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindSigns), false, true);
                                                                    break;
                                                                case "Waymarks":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindWaymarks), false, true);
                                                                    break;
                                                                case "Record Ready Check":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindRecordReadyCheck), false, true);
                                                                    break;
                                                                case "Ready Check":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindReadyCheck), false, true);
                                                                    break;
                                                                case "Countdown":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindCountdown), false, true);
                                                                    break;
                                                                case "Emotes":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindEmotes), false, true);
                                                                    break;
                                                                case "Linkshells":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindLinkshells), false, true);
                                                                    break;
                                                                case "Cross-world Linkshell":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindCrossWorldLS), false, true);
                                                                    break;
                                                                case "Contacts":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindContacts), false, true);
                                                                    break;
                                                                case "Sprint":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindSprint), false, true);
                                                                    break;
                                                                case "Teleport":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindTeleport), false, true);
                                                                    break;
                                                                case "Return":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindReturn), false, true);
                                                                    break;
                                                                case "Limit Break":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindLimitBreak), false, true);
                                                                    break;
                                                                case "Duty Action":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindDutyAction), false, true);
                                                                    break;
                                                                case "Repair":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindRepair), false, true);
                                                                    break;
                                                                case "Dig":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindDig), false, true);
                                                                    break;
                                                                case "Inventory":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindInventory), false, true);
                                                                    break;
                                                            }

                                                            continue;
                                                        }

                                                        if (action.IsAvailable || !_playerInfo.IsCasting)
                                                        {
                                                            if (action.InRange)
                                                            {
                                                                if (action.IsProcOrCombo)
                                                                {
                                                                    //Action Proc'd
                                                                    if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.PETBAR)
                                                                    {
                                                                        GlobalApplyMapKeyLighting(keyid,
                                                                            ColorTranslator.FromHtml(ColorMappings
                                                                                .ColorMappingPetProc), false, true);
                                                                    }
                                                                    else
                                                                    {
                                                                        GlobalApplyMapKeyLighting(keyid,
                                                                            ColorTranslator.FromHtml(ColorMappings
                                                                                .ColorMappingHotbarProc), false, true);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.PETBAR)
                                                                    {
                                                                        if (action.CoolDownPercent > 0)
                                                                            GlobalApplyMapKeyLighting(keyid,
                                                                                ColorTranslator.FromHtml(ColorMappings
                                                                                    .ColorMappingPetCd), false,
                                                                                true);
                                                                        else
                                                                            GlobalApplyMapKeyLighting(keyid,
                                                                                ColorTranslator.FromHtml(ColorMappings
                                                                                    .ColorMappingPetReady), false,
                                                                                true);
                                                                    }
                                                                    else
                                                                    {
                                                                        if (action.CoolDownPercent > 0)
                                                                            GlobalApplyMapKeyLighting(keyid,
                                                                                ColorTranslator.FromHtml(ColorMappings
                                                                                    .ColorMappingHotbarCd), false,
                                                                                true);
                                                                        else
                                                                            GlobalApplyMapKeyLighting(keyid,
                                                                                ColorTranslator.FromHtml(ColorMappings
                                                                                    .ColorMappingHotbarReady), false,
                                                                                true);
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.PETBAR)
                                                                {
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingPetOutRange), false, true);
                                                                }
                                                                else
                                                                {
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingHotbarOutRange), false, true);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.PETBAR)
                                                            {
                                                                GlobalApplyMapKeyLighting(keyid,
                                                                    ColorTranslator.FromHtml(ColorMappings
                                                                        .ColorMappingPetNotAvailable), false, true);
                                                            }
                                                            else
                                                            {
                                                                GlobalApplyMapKeyLighting(keyid,
                                                                    ColorTranslator.FromHtml(ColorMappings
                                                                        .ColorMappingHotbarNotAvailable), false, true);
                                                            }
                                                        }
                                                }
                                            }
                                            else
                                            {
                                                if (FfxivHotbar.Keybindtranslation.ContainsKey(action.ActionKey))
                                                {
                                                    var keyid = FfxivHotbar.Keybindtranslation[action.ActionKey];
                                                    
                                                    if (_modsactive == 0)
                                                       
                                                        if (action.Category == 49 || action.Category == 51)
                                                        {
                                                            if (!action.IsAvailable || !action.InRange || _playerInfo.IsCasting || action.CoolDownPercent > 0)
                                                            {
                                                                GlobalApplyMapKeyLighting(keyid,
                                                                    ColorTranslator.FromHtml(ColorMappings
                                                                        .ColorMappingHotbarNotAvailable), false, true);

                                                                continue;
                                                            }

                                                            switch (action.Name)
                                                            {
                                                                case "Map":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindMap), false, true);
                                                                    break;
                                                                case "Aether Currents":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindAetherCurrents), false, true);
                                                                    break;
                                                                case "Signs":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindSigns), false, true);
                                                                    break;
                                                                case "Waymarks":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindWaymarks), false, true);
                                                                    break;
                                                                case "Record Ready Check":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindRecordReadyCheck), false, true);
                                                                    break;
                                                                case "Ready Check":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindReadyCheck), false, true);
                                                                    break;
                                                                case "Countdown":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindCountdown), false, true);
                                                                    break;
                                                                case "Emotes":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindEmotes), false, true);
                                                                    break;
                                                                case "Linkshells":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindLinkshells), false, true);
                                                                    break;
                                                                case "Cross-world Linkshell":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindCrossWorldLS), false, true);
                                                                    break;
                                                                case "Contacts":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindContacts), false, true);
                                                                    break;
                                                                case "Sprint":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindSprint), false, true);
                                                                    break;
                                                                case "Teleport":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindTeleport), false, true);
                                                                    break;
                                                                case "Return":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindReturn), false, true);
                                                                    break;
                                                                case "Limit Break":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindLimitBreak), false, true);
                                                                    break;
                                                                case "Duty Action":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindDutyAction), false, true);
                                                                    break;
                                                                case "Repair":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindRepair), false, true);
                                                                    break;
                                                                case "Dig":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindDig), false, true);
                                                                    break;
                                                                case "Inventory":
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingKeybindInventory), false, true);
                                                                    break;
                                                            }

                                                            continue;
                                                        }

                                                        if (action.IsAvailable || !_playerInfo.IsCasting)
                                                        {
                                                            if (action.InRange)
                                                            {
                                                                if (action.IsProcOrCombo)
                                                                {
                                                                    if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.PETBAR)
                                                                    {
                                                                        //Action Proc'd
                                                                        GlobalApplyMapKeyLighting(keyid,
                                                                            ColorTranslator.FromHtml(ColorMappings
                                                                                .ColorMappingPetProc), false, true);
                                                                    }
                                                                    else
                                                                    {
                                                                        //Action Proc'd
                                                                        GlobalApplyMapKeyLighting(keyid,
                                                                            ColorTranslator.FromHtml(ColorMappings
                                                                                .ColorMappingHotbarProc), false, true);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.PETBAR)
                                                                    {
                                                                        if (action.CoolDownPercent > 0)
                                                                            GlobalApplyMapKeyLighting(keyid,
                                                                                ColorTranslator.FromHtml(ColorMappings
                                                                                    .ColorMappingPetCd), false,
                                                                                true);
                                                                        else
                                                                            GlobalApplyMapKeyLighting(keyid,
                                                                                ColorTranslator.FromHtml(ColorMappings
                                                                                    .ColorMappingPetReady), false,
                                                                                true);
                                                                    }
                                                                    else
                                                                    {
                                                                        if (action.CoolDownPercent > 0)
                                                                            GlobalApplyMapKeyLighting(keyid,
                                                                                ColorTranslator.FromHtml(ColorMappings
                                                                                    .ColorMappingHotbarCd), false,
                                                                                true);
                                                                        else
                                                                            GlobalApplyMapKeyLighting(keyid,
                                                                                ColorTranslator.FromHtml(ColorMappings
                                                                                    .ColorMappingHotbarReady), false,
                                                                                true);
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.PETBAR)
                                                                {
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingPetOutRange), false, true);
                                                                }
                                                                else
                                                                {
                                                                    GlobalApplyMapKeyLighting(keyid,
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingHotbarOutRange), false, true);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.PETBAR)
                                                            {
                                                                GlobalApplyMapKeyLighting(keyid,
                                                                    ColorTranslator.FromHtml(ColorMappings
                                                                        .ColorMappingPetNotAvailable), false, true);
                                                            }
                                                            else
                                                            {
                                                                GlobalApplyMapKeyLighting(keyid,
                                                                    ColorTranslator.FromHtml(ColorMappings
                                                                        .ColorMappingHotbarNotAvailable), false, true);
                                                            }
                                                        }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (FfxivHotbar.Keybindwhitelist.Count > 0)
                                        FfxivHotbar.Keybindwhitelist.Clear();

                                    foreach (var key in FfxivHotbar.Keybindtranslation)
                                    {
                                        GlobalApplyMapKeyLighting(key.Value, _baseColor, false);
                                    }

                                    //SetKeysbase = false;
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

                                var _role = _playerData.CurrentPlayer.WVR_CurrentEXP;
                                var _currentlvl = 0;

                                var expcolempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingExpEmpty);
                                var expcolfull = ColorTranslator.FromHtml(ColorMappings.ColorMappingExpFull);


                                switch (_playerInfo.Job)
                                {
                                    case Actor.Job.Unknown:
                                        _role = _playerData.CurrentPlayer.WVR_CurrentEXP;
                                        _currentlvl = 0;
                                        break;
                                    case Actor.Job.GLD:
                                        _role = _playerData.CurrentPlayer.GLD_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.GLD;
                                        break;
                                    case Actor.Job.PGL:
                                        _role = _playerData.CurrentPlayer.PGL_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.PGL;
                                        break;
                                    case Actor.Job.MRD:
                                        _role = _playerData.CurrentPlayer.MRD_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.MRD;
                                        break;
                                    case Actor.Job.LNC:
                                        _role = _playerData.CurrentPlayer.LNC_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.LNC;
                                        break;
                                    case Actor.Job.ARC:
                                        _role = _playerData.CurrentPlayer.ARC_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ARC;
                                        break;
                                    case Actor.Job.CNJ:
                                        _role = _playerData.CurrentPlayer.CNJ_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.CNJ;
                                        break;
                                    case Actor.Job.THM:
                                        _role = _playerData.CurrentPlayer.THM_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.THM;
                                        break;
                                    case Actor.Job.CPT:
                                        _role = _playerData.CurrentPlayer.CPT_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.CPT;
                                        break;
                                    case Actor.Job.BSM:
                                        _role = _playerData.CurrentPlayer.BSM_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.BSM;
                                        break;
                                    case Actor.Job.ARM:
                                        _role = _playerData.CurrentPlayer.ARM_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ARM;
                                        break;
                                    case Actor.Job.GSM:
                                        _role = _playerData.CurrentPlayer.GSM_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.GSM;
                                        break;
                                    case Actor.Job.LTW:
                                        _role = _playerData.CurrentPlayer.LTW_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.LTW;
                                        break;
                                    case Actor.Job.WVR:
                                        _role = _playerData.CurrentPlayer.WVR_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.WVR;
                                        break;
                                    case Actor.Job.ALC:
                                        _role = _playerData.CurrentPlayer.ALC_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ALC;
                                        break;
                                    case Actor.Job.CUL:
                                        _role = _playerData.CurrentPlayer.CUL_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.CUL;
                                        break;
                                    case Actor.Job.MIN:
                                        _role = _playerData.CurrentPlayer.MIN_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.MIN;
                                        break;
                                    case Actor.Job.BTN:
                                        _role = _playerData.CurrentPlayer.BTN_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.BTN;
                                        break;
                                    case Actor.Job.FSH:
                                        _role = _playerData.CurrentPlayer.FSH_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.FSH;
                                        break;
                                    case Actor.Job.PLD:
                                        _role = _playerData.CurrentPlayer.GLD_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.GLD;
                                        break;
                                    case Actor.Job.MNK:
                                        _role = _playerData.CurrentPlayer.PGL_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.PGL;
                                        break;
                                    case Actor.Job.WAR:
                                        _role = _playerData.CurrentPlayer.MRD_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.MRD;
                                        break;
                                    case Actor.Job.DRG:
                                        _role = _playerData.CurrentPlayer.LNC_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.LNC;
                                        break;
                                    case Actor.Job.BRD:
                                        _role = _playerData.CurrentPlayer.ARC_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ARC;
                                        break;
                                    case Actor.Job.WHM:
                                        _role = _playerData.CurrentPlayer.CNJ_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.CNJ;
                                        break;
                                    case Actor.Job.BLM:
                                        _role = _playerData.CurrentPlayer.THM_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.THM;
                                        break;
                                    case Actor.Job.ACN:
                                        _role = _playerData.CurrentPlayer.ACN_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ACN;
                                        break;
                                    case Actor.Job.SMN:
                                        _role = _playerData.CurrentPlayer.ACN_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ACN;
                                        break;
                                    case Actor.Job.SCH:
                                        _role = _playerData.CurrentPlayer.ACN_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ACN;
                                        break;
                                    case Actor.Job.ROG:
                                        _role = _playerData.CurrentPlayer.ROG_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ROG;
                                        break;
                                    case Actor.Job.NIN:
                                        _role = _playerData.CurrentPlayer.ROG_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.ROG;
                                        break;
                                    case Actor.Job.MCH:
                                        _role = _playerData.CurrentPlayer.MCH_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.MCH;
                                        break;
                                    case Actor.Job.DRK:
                                        _role = _playerData.CurrentPlayer.DRK_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.DRK;
                                        break;
                                    case Actor.Job.AST:
                                        _role = _playerData.CurrentPlayer.AST_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.AST;
                                        break;
                                    case Actor.Job.SAM:
                                        _role = _playerData.CurrentPlayer.SAM_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.SAM;
                                        break;
                                    case Actor.Job.RDM:
                                        _role = _playerData.CurrentPlayer.RDM_CurrentEXP;
                                        _currentlvl = _playerData.CurrentPlayer.RDM;
                                        break;
                                    default:
                                        _role = _playerData.CurrentPlayer.WVR_CurrentEXP;
                                        _currentlvl = 0;
                                        break;
                                }


                                if (_currentlvl == 70)
                                {
                                    foreach (var f in DeviceEffects.LightbarZones)
                                    {
                                        GlobalApplyMapLightbarLighting(f, expcolfull, false, false);
                                    }
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
                            ImplementJobGauges(statEffects, _baseColor);


                            //Duty Finder Bell
                            
                            if (FfxivDutyFinder.IsPopped())
                            {
                                //Debug.WriteLine("DF Pop");
                                if (!_dfpop)
                                {
                                    _dfpop = true;

                                    ToggleGlobalFlash4(true);

                                    GlobalFlash4(_baseColor,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), 500,
                                        DeviceEffects.GlobalKeys);

                                    GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.DutyFinder,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), "All");
                                    GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.DutyFinder,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.DutyFinder,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false,
                                        "All");
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.DutyFinder,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));

                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide1", "RightSide1",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide2", "RightSide2",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide3", "RightSide3",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide4", "RightSide4",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide5", "RightSide5",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide6", "RightSide6",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide7", "RightSide7",
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 14, 5, 0,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 13, 6, 1,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 12, 7, 2,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 11, 8, 3,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 10, 9, 4,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

                                    if (_LightbarMode == LightbarMode.DutyFinder)
                                    {
                                        foreach (var f in DeviceEffects.LightbarZones)
                                        {
                                            GlobalApplyMapLightbarLighting(f,
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false,
                                                false);
                                        }
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

                                    GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, _baseColor);
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.DutyFinder, baseColor, "All");
                                    GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, baseColor, false);
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.DutyFinder, baseColor, false);
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.DutyFinder, baseColor, false, "All");
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.DutyFinder, baseColor);

                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide1", "RightSide1", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide2", "RightSide2", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide3", "RightSide3", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide4", "RightSide4", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide5", "RightSide5", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide6", "RightSide6", baseColor, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide7", "RightSide7", baseColor, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 14, 5, 0, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 13, 6, 1, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 12, 7, 2, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 11, 8, 3, baseColor, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.DutyFinder, 10, 9, 4, baseColor, false);

                                    if (_LightbarMode == LightbarMode.DutyFinder)
                                    {
                                        foreach (var f in DeviceEffects.LightbarZones)
                                        {
                                            GlobalApplyMapLightbarLighting(f, _baseColor, false, false);
                                        }
                                    }
                                }
                            }


                        }
                        
                        GlobalKeyboardUpdate();
                        MemoryTasks.Cleanup();
                    }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, @"Parse Error: " + ex.Message);
                WriteConsole(ConsoleTypes.Error, @"Internal Error: " + ex.StackTrace);
            }
        }
    }
}
 