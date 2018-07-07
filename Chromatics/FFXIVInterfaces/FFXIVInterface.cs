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

                                ////GlobalApplyMapKeypadLightingBrightness(DevModeTypes.TargetHp, targetInfo.IsClaimed ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
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

                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);

                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                    GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);

                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "3");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "2");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "1");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "0");

                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "3");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "2");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "1");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed), "0");
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

                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 14, 5, 0, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 13, 6, 1, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 12, 7, 2, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 11, 8, 3, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10, 9, 4, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false);

                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "4");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "3");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "2");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "1");
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, "0");

                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "5");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "4");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "3");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "2");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "1");
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), "0");
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
                            //GlobalApplyMapKeypadLightingBrightness(DevModeTypes.TargetHp, colCastcharge, false, castPercentage);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TargetHp, colCastempty, colCastcharge, castPercentage);

                            if (polCast <= 1 && ChromaticsSettings.ChromaticsSettingsCastToggle)
                            {
                                GlobalApplyMapKeyLighting("F1", colCastcharge, false);

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "2");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "2");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "2");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "2");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide4", "RightSide4", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "3");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "3");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide4", "RightSide4", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "3");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "3");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide4", "RightSide4", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide3", "RightSide3", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "4");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "4");

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

                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide4", "RightSide4", colCastcharge, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide3", "RightSide3", colCastcharge, false);

                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "4");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "4");
                                

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

                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide4", "RightSide4", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide3", "RightSide3", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide2", "RightSide2", colCastcharge, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, colCastcharge, false);

                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "2");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "3");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "4");

                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "2");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "3");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "4");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "5");

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

                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide7", "RightSide7", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide6", "RightSide6", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide5", "RightSide5", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide4", "RightSide4", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide3", "RightSide3", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide2", "RightSide2", colCastcharge, false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.Castbar, "LeftSide1", "RightSide1", colCastcharge, false);

                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 14, 5, 0, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 13, 6, 1, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 12, 7, 2, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 11, 8, 3, colCastcharge, false);
                                    GlobalApplyMapPadLighting(DevModeTypes.Castbar, 10, 9, 4, colCastcharge, false);

                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "0");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "1");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "2");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "3");
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.Castbar, colCastcharge, false, "4");

                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "0");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "1");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "2");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "3");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "4");
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.Castbar, colCastcharge, "5");
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
                            var polHpx = (currentHp - 0) * (70 - 0) / (maxHp - 0) + 0;
                            var polHpz = (currentHp - 0) * (65535 - 0) / (maxHp - 0) + 0;
                            var polHpz2 = (currentHp - 0) * (1.0 - 0.0) / (maxHp - 0) + 0.0;

                            //Console.WriteLine(currentHp + @"/" + maxHp);

                            //Debug.WriteLine(polHpz2);

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.HpTracker,
                                polHp <= 10 ? colHpempty : colHpfull,
                                (ushort)polHpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpcritical : colHpfull, polHpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);
                            //GlobalApplyMapKeypadLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= 10 ? colHpempty : colHpfull, polHpz2);

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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpempty, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpcritical, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpempty, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpcritical, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpcritical, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpcritical, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpcritical, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpcritical, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, colHpcritical, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpcritical, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpcritical, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpcritical, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpcritical, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpcritical, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, colHpcritical, "0");
                            }

                            //Mouse
                            if (polHpx <= 70 && polHpx > 60)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide7", "RightSide7", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide6", "RightSide6", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide5", "RightSide5", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide4", "RightSide4", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide3", "RightSide3", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide2", "RightSide2", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide1", "RightSide1", colHpfull, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide7", "RightSide7", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide6", "RightSide6", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide5", "RightSide5", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide4", "RightSide4", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide3", "RightSide3", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide2", "RightSide2", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide1", "RightSide1", colHpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide7", "RightSide7", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide6", "RightSide6", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide5", "RightSide5", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide4", "RightSide4", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide3", "RightSide3", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide2", "RightSide2", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide1", "RightSide1", colHpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide7", "RightSide7", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide6", "RightSide6", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide5", "RightSide5", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide4", "RightSide4", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide3", "RightSide3", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide2", "RightSide2", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide1", "RightSide1", colHpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide7", "RightSide7", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide6", "RightSide6", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide5", "RightSide5", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide4", "RightSide4", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide3", "RightSide3", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide2", "RightSide2", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide1", "RightSide1", colHpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide7", "RightSide7", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide6", "RightSide6", colHpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide5", "RightSide5", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide4", "RightSide4", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide3", "RightSide3", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide2", "RightSide2", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide1", "RightSide1", colHpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide7", "RightSide7", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide6", "RightSide6", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide5", "RightSide5", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide4", "RightSide4", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide3", "RightSide3", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide2", "RightSide2", colHpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide1", "RightSide1", colHpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide7", "RightSide7", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide6", "RightSide6", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide5", "RightSide5", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide4", "RightSide4", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide3", "RightSide3", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide2", "RightSide2", colHpcritical, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, "LeftSide1", "RightSide1", colHpcritical, false);

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

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, polMpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, false, polMpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, false, polMpz2);
                            //GlobalApplyMapKeypadLightingBrightness(DevModeTypes.MpTracker, colMpfull, false, polMpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, polMpz2);

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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "0");
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
                                GlobalApplyMapPadLighting(DevModeTypes.MpTracker, 10, 9, 4, colMpfull, false);

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.MpTracker, colMpempty, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.MpTracker, colMpempty, "0");
                            }

                            //Mouse
                            if (polMpx <= 70 && polMpx > 60)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide7", "RightSide7", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide6", "RightSide6", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide5", "RightSide5", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide4", "RightSide4", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide3", "RightSide3", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide2", "RightSide2", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide1", "RightSide1", colMpfull, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide7", "RightSide7", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide6", "RightSide6", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide5", "RightSide5", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide4", "RightSide4", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide3", "RightSide3", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide2", "RightSide2", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide1", "RightSide1", colMpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide7", "RightSide7", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide6", "RightSide6", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide5", "RightSide5", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide4", "RightSide4", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide3", "RightSide3", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide2", "RightSide2", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide1", "RightSide1", colMpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide7", "RightSide7", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide6", "RightSide6", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide5", "RightSide5", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide4", "RightSide4", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide3", "RightSide3", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide2", "RightSide2", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide1", "RightSide1", colMpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide7", "RightSide7", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide6", "RightSide6", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide5", "RightSide5", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide4", "RightSide4", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide3", "RightSide3", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide2", "RightSide2", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide1", "RightSide1", colMpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide7", "RightSide7", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide6", "RightSide6", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide5", "RightSide5", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide4", "RightSide4", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide3", "RightSide3", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide2", "RightSide2", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide1", "RightSide1", colMpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide7", "RightSide7", colMpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide6", "RightSide6", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide5", "RightSide5", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide4", "RightSide4", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide3", "RightSide3", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide2", "RightSide2", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide1", "RightSide1", colMpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide7", "RightSide7", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide6", "RightSide6", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide5", "RightSide5", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide4", "RightSide4", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide3", "RightSide3", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide2", "RightSide2", colMpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.MpTracker, "LeftSide1", "RightSide1", colMpempty, false);

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

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.TpTracker, colTpempty, colTpfull, polTpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.TpTracker, colTpempty, colTpfull, false, polTpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.TpTracker, colTpempty, colTpfull, false, polTpz2);
                            //GlobalApplyMapKeypadLightingBrightness(DevModeTypes.TpTracker, colTpfull, false, polTpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TpTracker, colTpempty, colTpfull, polTpz2);

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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "5");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "0");
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
                                GlobalApplyMapPadLighting(DevModeTypes.TpTracker, 10, 9, 4, colTpfull, false);

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpfull, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpfull, "0");
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

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "4");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "3");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "2");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "1");
                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.TpTracker, colTpempty, false, "0");

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "5");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "4");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "3");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "2");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "1");
                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.TpTracker, colTpempty, "0");
                            }

                            //Mouse
                            if (polTpx <= 70 && polTpx > 60)
                            {
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide7", "RightSide7", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide6", "RightSide6", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide5", "RightSide5", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide4", "RightSide4", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide3", "RightSide3", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide2", "RightSide2", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide1", "RightSide1", colTpfull, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide7", "RightSide7", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide6", "RightSide6", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide5", "RightSide5", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide4", "RightSide4", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide3", "RightSide3", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide2", "RightSide2", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide1", "RightSide1", colTpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide7", "RightSide7", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide6", "RightSide6", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide5", "RightSide5", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide4", "RightSide4", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide3", "RightSide3", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide2", "RightSide2", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide1", "RightSide1", colTpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide7", "RightSide7", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide6", "RightSide6", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide5", "RightSide5", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide4", "RightSide4", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide3", "RightSide3", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide2", "RightSide2", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide1", "RightSide1", colTpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide7", "RightSide7", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide6", "RightSide6", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide5", "RightSide5", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide4", "RightSide4", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide3", "RightSide3", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide2", "RightSide2", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide1", "RightSide1", colTpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide7", "RightSide7", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide6", "RightSide6", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide5", "RightSide5", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide4", "RightSide4", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide3", "RightSide3", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide2", "RightSide2", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide1", "RightSide1", colTpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide7", "RightSide7", colTpfull, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide6", "RightSide6", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide5", "RightSide5", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide4", "RightSide4", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide3", "RightSide3", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide2", "RightSide2", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide1", "RightSide1", colTpempty, false);

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
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide7", "RightSide7", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide6", "RightSide6", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide5", "RightSide5", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide4", "RightSide4", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide3", "RightSide3", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide2", "RightSide2", colTpempty, false);
                                GlobalApplyStripMouseLighting(DevModeTypes.TpTracker, "LeftSide1", "RightSide1", colTpempty, false);

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
                            ImplementJobGauges(statEffects, _baseColor);


                            //Duty Finder Bell
                            
                            if (FfxivDutyFinder.IsPopped())
                            {
                                //Debug.WriteLine("DF Pop");
                                if (!_dfpop)
                                {
                                    ToggleGlobalFlash4(true);
                                    GlobalFlash4(_baseColor,
                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), 500,
                                        DeviceEffects.GlobalKeys);

                                    GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));
                                    GlobalApplyKeyMultiLighting(DevMultiModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), "All");
                                    GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapHeadsetLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyMapKeypadLighting(DevMultiModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, "All");
                                    GlobalApplyMapChromaLinkLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));

                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                    GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

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
                                        GlobalFlash4(_baseColor,
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), 200,
                                            DeviceEffects.GlobalKeys);

                                        GlobalApplyKeySingleLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), "All");
                                        GlobalApplyMapMouseLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapHeadsetLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false, "All");
                                        GlobalApplyMapChromaLinkLighting(DevModeTypes.DutyFinder, ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell));

                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide1", "RightSide1", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide2", "RightSide2", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide3", "RightSide3", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide4", "RightSide4", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide5", "RightSide5", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide6", "RightSide6", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);
                                        GlobalApplyStripMouseLighting(DevModeTypes.DutyFinder, "LeftSide7", "RightSide7", ColorTranslator.FromHtml(ColorMappings.ColorMappingDutyFinderBell), false);

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
 