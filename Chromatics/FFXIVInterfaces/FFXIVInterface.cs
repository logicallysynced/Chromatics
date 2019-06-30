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
using System.IO;
using System.Reflection;
using Chromatics.ACTInterfaces;
using System.Timers;
using System.Net.Http;
using EasyHttp.Http;


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
        private bool _dfpopOnce;
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
                
                
                FfxivThread = new Thread(new ThreadStart(CallThreadFFXIVAttach));
                FfxivThread.Start();
                FfxivThread.Join();
                ct.Cancel();
                
                
                /*
                while (!ct.IsCancellationRequested && !_exit)
                {
                    if (_exit)
                    {
                        ct.Cancel();
                    }

                    ReadFfxivMemory();
                    await Task.Delay(ChromaticsSettings.ChromaticsSettingsPollingInterval);
                    
                }
                */
                
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CallThreadFFXIVAttach()
        {
            while (true)
            {
                Thread.Sleep(ChromaticsSettings.ChromaticsSettingsPollingInterval);

                if (_exit)
                {
                    break;
                }

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
                                ChatInit = false;

                                if (LcdSdkCalled == 1)
                                {
                                    _lcd.SetStartupFlag = false;
                                }

                                //GlobalUpdateState("static", Color.DeepSkyBlue, false);
                                GlobalStopParticleEffects();
                                GlobalStopCycleEffects();
                                GlobalApplyAllDeviceLighting(Color.BlueViolet);
                                
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
                            GlobalApplyAllDeviceLighting(Color.BlueViolet);
                            
                            Attatched = 2;
                            _countMainMenuHold = 0;
                        }

                        //Game in Menu
                        else if (Attatched == 2)
                        {
                            if (_menuInfo != null && _menuInfo.Name != "" && !_menuInfo.Name.StartsWith("Typeid:") && _catchMenuchange <= 5)
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
                                ChatInit = false;
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
                                    GlobalApplyAllDeviceLighting(Color.BlueViolet);
                                    GlobalParticleEffects(
                                        new Color[]
                                        {
                                            Color.BlueViolet, Color.MediumBlue, Color.Salmon, Color.Purple
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
                        var hpPerc = _playerInfo.HPPercent;
                        var mpPerc = _playerInfo.MPPercent;
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

                                    _arx.ArxUpdateFfxivStats(hpPerc, mpPerc, 0, currentHp, currentMp, 0,
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
                                        

                                        datastring[i] = "1," + ptType + "," + partyInfo[i].Name + "," +
                                                        partyInfo[i].HPPercent.ToString("#0%") + "," +
                                                        partyInfo[i].HPCurrent + "," +
                                                        partyInfo[i].MPPercent.ToString("#0%") + "," +
                                                        partyInfo[i].MPCurrent + "," + 0 + "," +
                                                        0 +
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

                                _arx.ArxUpdateFfxivPlugin(hpPerc, mpPerc, 0, currentHp, currentMp, 0,
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

                            if (_FKeyMode == FKeyMode.DefaultColor)
                            {
                                foreach (var f in DeviceEffects.Functions)
                                {
                                    GlobalApplyMapKeyLighting(f, _baseColor, false, false);
                                }
                            }
                            else if (_FKeyMode == FKeyMode.HighlightColor)
                            {
                                foreach (var f in DeviceEffects.Functions)
                                {
                                    GlobalApplyMapKeyLighting(f, highlightColor, false, false);
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
                            switch (ChromaticsSettings.ChromaticsSettingsQwertyMode)
                            {
                                case KeyRegion.QWERTY:
                                    GlobalApplyMapKeyLighting("W", highlightColor, false);
                                    GlobalApplyMapKeyLighting("A", highlightColor, false);
                                    GlobalApplyMapKeyLighting("S", highlightColor, false);
                                    GlobalApplyMapKeyLighting("D", highlightColor, false);
                                    GlobalApplyMapKeyLighting("Z", _baseColor, false);
                                    GlobalApplyMapKeyLighting("Q", _baseColor, false);
                                    GlobalApplyMapKeyLighting("E", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F", _baseColor, false);
                                    break;
                                case KeyRegion.AZERTY:
                                    GlobalApplyMapKeyLighting("W", highlightColor, false);
                                    GlobalApplyMapKeyLighting("A", highlightColor, false);
                                    GlobalApplyMapKeyLighting("S", highlightColor, false);
                                    GlobalApplyMapKeyLighting("D", highlightColor, false);
                                    GlobalApplyMapKeyLighting("Z", _baseColor, false);
                                    GlobalApplyMapKeyLighting("Q", _baseColor, false);
                                    GlobalApplyMapKeyLighting("E", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F", _baseColor, false);
                                    break;
                                case KeyRegion.QWERTZ:
                                    GlobalApplyMapKeyLighting("W", highlightColor, false);
                                    GlobalApplyMapKeyLighting("A", highlightColor, false);
                                    GlobalApplyMapKeyLighting("S", highlightColor, false);
                                    GlobalApplyMapKeyLighting("D", highlightColor, false);
                                    GlobalApplyMapKeyLighting("Z", _baseColor, false);
                                    GlobalApplyMapKeyLighting("E", _baseColor, false);
                                    GlobalApplyMapKeyLighting("F", _baseColor, false);
                                    GlobalApplyMapKeyLighting("Q", _baseColor, false);
                                    break;
                                case KeyRegion.ESDF:
                                    GlobalApplyMapKeyLighting("E", highlightColor, false);
                                    GlobalApplyMapKeyLighting("S", highlightColor, false);
                                    GlobalApplyMapKeyLighting("D", highlightColor, false);
                                    GlobalApplyMapKeyLighting("F", highlightColor, false);
                                    GlobalApplyMapKeyLighting("W", _baseColor, false);
                                    GlobalApplyMapKeyLighting("A", _baseColor, false);
                                    GlobalApplyMapKeyLighting("Z", _baseColor, false);
                                    GlobalApplyMapKeyLighting("Q", _baseColor, false);
                                    break;
                            }

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
                            GlobalApplyMapKeyLighting("Z", _baseColor, false);
                            GlobalApplyMapKeyLighting("E", _baseColor, false);
                            GlobalApplyMapKeyLighting("F", _baseColor, false);
                            GlobalApplyMapKeyLighting("Q", _baseColor, false);
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

                        if (ChromaticsSettings.ChromaticsSettingsStatusEffectToggle)
                        {
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
                        }


                        //Target
                        if (targetInfo != null)
                        {
                            if (targetInfo.Type == Actor.Type.PC || targetInfo.Type == Actor.Type.NPC)
                            {
                                //Friendly HP
                                var currentThp = targetInfo.HPCurrent;
                                var maxThp = targetInfo.HPMax;
                                var polTargetHpx = (currentThp - 0) * ((long)65535 - 0) / (maxThp - 0) + 0;
                                var polTargetHpx2 = (currentThp - 0) * (1.0 - 0.0) / (maxThp - 0) + 0.0;

                                GlobalUpdateBulbStateBrightness(BulbModeTypes.TargetHp,
                                    targetInfo.IsClaimed
                                        ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly)
                                        : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle),
                                    (ushort)polTargetHpx, 250);

                                GlobalApplyKeySingleLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), polTargetHpx2);

                                GlobalApplyMapMouseLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), false, polTargetHpx2);

                                GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.TargetHp, ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), targetInfo.IsClaimed
                                    ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly)
                                    : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle), polTargetHpx2);

                                //Lightbar
                                if (_LightbarMode == LightbarMode.TargetHp)
                                {
                                    var LBTargetHp_Collection = DeviceEffects.LightbarZones;
                                    var LBTargetHp_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, LBTargetHp_Collection.Length, 0);

                                    for (int i = 0; i < LBTargetHp_Collection.Length; i++)
                                    {
                                        GlobalApplyMapLightbarLighting(LBTargetHp_Collection[i], LBTargetHp_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, false);
                                    }
                                }

                                //Function Keys
                                if (_FKeyMode == FKeyMode.TargetHp)
                                {
                                    var FKTargetHp_Collection = DeviceEffects.Functions;
                                    var FKTargetHp_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, FKTargetHp_Collection.Length, 0);

                                    for (int i = 0; i < FKTargetHp_Collection.Length; i++)
                                    {
                                        GlobalApplyMapKeyLighting(FKTargetHp_Collection[i], FKTargetHp_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, false);
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
                                                ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly)
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
                                        GlobalApplyStripMouseLighting(DevModeTypes.TargetHp, TargetHpMouseStrip_CollectionA[i], TargetHpMouseStrip_CollectionB[i], TargetHpMouseStrip_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
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
                                        GlobalApplyMapPadLighting(DevModeTypes.TargetHp, 10 + i, 9 - i, 4 - i, TargetHpMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false);
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
                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.TargetHp, TargetHpKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, TargetHpKeypad_Collection[i]);
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
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, TargetHpMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpFriendly) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), TargetHpMulti_Collection[i]);
                                    }
                                    else
                                    {
                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.TargetHp, TargetHpMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpIdle) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), TargetHpMulti_Collection[i]);
                                    }
                                }
                            }
                            else if (targetInfo.Type == Actor.Type.Monster)
                            {
                                if (!_targeted)
                                    _targeted = true;


                                //Target HP
                                var currentThp = targetInfo.HPCurrent;
                                var maxThp = targetInfo.HPMax;
                                var polTargetHpx = (currentThp - 0) * ((long)65535 - 0) / (maxThp - 0) + 0;
                                var polTargetHpx2 = (currentThp - 0) * (1.0 - 0.0) / (maxThp - 0) + 0.0;

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

                                //Function Keys
                                if (_FKeyMode == FKeyMode.TargetHp)
                                {
                                    var FKTargetHp_Collection = DeviceEffects.Functions;
                                    var FKTargetHp_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentThp, 0, maxThp, FKTargetHp_Collection.Length, 0);

                                    for (int i = 0; i < FKTargetHp_Collection.Length; i++)
                                    {
                                        GlobalApplyMapKeyLighting(FKTargetHp_Collection[i], FKTargetHp_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpClaimed) : ColorTranslator.FromHtml(ColorMappings.ColorMappingTargetHpEmpty), false, false);
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
                                                    GlobalApplyMapLightbarLighting(f, colEm0, false);

                                                }
                                            }

                                            //Function Keys
                                            if (_FKeyMode == FKeyMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.Functions)
                                                {
                                                    GlobalApplyMapKeyLighting(f, colEm0, false);
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

                                            //Function Keys
                                            if (_FKeyMode == FKeyMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.Functions)
                                                {
                                                    GlobalApplyMapKeyLighting(f, colEm1, false);
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

                                            //Function Keys
                                            if (_FKeyMode == FKeyMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.Functions)
                                                {
                                                    GlobalApplyMapKeyLighting(f, colEm2, false);
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

                                            //Function Keys
                                            if (_FKeyMode == FKeyMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.Functions)
                                                {
                                                    GlobalApplyMapKeyLighting(f, colEm3, false);
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

                                            //Function Keys
                                            if (_FKeyMode == FKeyMode.EnmityTracker)
                                            {
                                                foreach (var f in DeviceEffects.Functions)
                                                {
                                                    GlobalApplyMapKeyLighting(f, colEm4, false);
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

                                        //Function Keys
                                        if (_FKeyMode == FKeyMode.EnmityTracker)
                                        {
                                            foreach (var f in DeviceEffects.Functions)
                                            {
                                                GlobalApplyMapKeyLighting(f, colEm0, false);
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

                                //Function Keys
                                if (_FKeyMode == FKeyMode.TargetHp || _FKeyMode == FKeyMode.EnmityTracker)
                                {
                                    foreach (var f in DeviceEffects.Functions)
                                    {
                                        GlobalApplyMapKeyLighting(f, _baseColor, false);
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

                            if (_LightbarMode == LightbarMode.TargetHp || _LightbarMode == LightbarMode.EnmityTracker)
                            {
                                foreach (var f in DeviceEffects.LightbarZones)
                                {
                                    GlobalApplyMapLightbarLighting(f, _baseColor, false, false);

                                }
                            }

                            //Function Keys
                            if (_FKeyMode == FKeyMode.TargetHp || _FKeyMode == FKeyMode.EnmityTracker)
                            {
                                foreach (var f in DeviceEffects.Functions)
                                {
                                    GlobalApplyMapKeyLighting(f, _baseColor, false);
                                }
                            }

                            if (_targeted)
                                _targeted = false;

                            ToggleGlobalFlash2(false);
                            _castalert = false;
                        }
                        

                        //Castbar
                        var castPercentage = _playerInfo.CastingPercentage;
                        var polCastZ = (castPercentage - 0) * ((long)65535 - 0) / (1.0 - 0.0) + 0;
                        
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
                            var polHpz = (currentHp - 0) * ((long)65535 - 0) / (maxHp - 0) + 0; //65535

                            var polHpz2 = (currentHp - 0) * (1.0 - 0.0) / (maxHp - 0) + 0.0;
                            var criticalThresh = ChromaticsSettings.ChromaticsSettingsCriticalHP / 100;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.HpTracker, polHp <= criticalThresh ? colHpempty : colHpfull, (ushort)polHpz, 250);
                            

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= criticalThresh ? colHpcritical : colHpfull, polHpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= criticalThresh ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= criticalThresh ? colHpempty : colHpfull, false, polHpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.HpTracker, colHpempty, polHp <= criticalThresh ? colHpempty : colHpfull, polHpz2);


                            //FKeys
                            if (_FKeyMode == FKeyMode.HpMp)
                            {
                                var HpFunction_Collection = DeviceEffects.Function1;
                                var HpFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp,
                                        HpFunction_Collection.Length, 0);

                                for (int i = 0; i < HpFunction_Collection.Length; i++)
                                {
                                    if (_playerInfo.IsCasting)
                                    {
                                        break;
                                    }

                                    var col = colHpfull;

                                    if (polHpz2 < criticalThresh)
                                    {
                                        col = colHpcritical;
                                    }

                                    GlobalApplyMapKeyLighting(HpFunction_Collection[i],
                                        HpFunction_Interpolate > i ? col : colHpempty, false);
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
                                    if (_playerInfo.IsCasting)
                                    {
                                        break;
                                    }

                                    var col = colHpfull;

                                    if (polHpz2 < criticalThresh)
                                    {
                                        col = colHpcritical;
                                    }

                                    GlobalApplyMapKeyLighting(HpFunction_Collection[i],
                                        HpFunction_Interpolate > i ? col : colHpempty, false);
                                }
                            }

                            //Mousepad
                            var HpMousePadCollection = 5;
                            var HpMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpMousePadCollection, 0);

                            for (int i = 0; i < HpMousePadCollection; i++)
                            {
                                var col = colHpfull;

                                if (polHpz2 < criticalThresh)
                                {
                                    col = colHpcritical;
                                }

                                GlobalApplyMapPadLighting(DevModeTypes.HpTracker, 10+i, 9-i, 4-i, HpMousePad_Interpolate > i ? col : colHpempty, false);
                            }

                            //Keypad
                            var HpKeypad_Collection = DeviceEffects.Keypadzones;
                            var HpKeypad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpKeypad_Collection.Length, 0);

                            for (int i = 0; i < HpKeypad_Collection.Length; i++)
                            {
                                var col = colHpfull;

                                if (polHpz2 < criticalThresh)
                                {
                                    col = colHpcritical;
                                }

                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.HpTracker, HpKeypad_Interpolate > i ? col : colHpempty, false, HpKeypad_Collection[i]);
                            }

                            //MultiKeyboard
                            var HpMulti_Collection = DeviceEffects.Multikeyzones;
                            var HpMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpMulti_Collection.Length, 0);

                            for (int i = 0; i < HpMulti_Collection.Length; i++)
                            {
                                var col = colHpfull;

                                if (polHpz2 < criticalThresh)
                                {
                                    col = colHpcritical;
                                }

                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.HpTracker, HpMulti_Interpolate > i ? col : colHpempty, HpMulti_Collection[i]);
                            }

                            //Mouse
                            var HpMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                            var HpMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;
                            var HpMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp, HpMouseStrip_CollectionA.Length, 0);

                            for (int i = 0; i < HpMouseStrip_CollectionA.Length; i++)
                            {
                                var col = colHpfull;

                                if (polHpz2 < criticalThresh)
                                {
                                    col = colHpcritical;
                                }

                                GlobalApplyStripMouseLighting(DevModeTypes.HpTracker, HpMouseStrip_CollectionA[i], HpMouseStrip_CollectionB[i], HpMouseStrip_Interpolate > i ? col : colHpempty, false);
                            }

                            //Lightbar
                            if (_LightbarMode == LightbarMode.HpTracker)
                            {
                                var HpLightbar_Collection = DeviceEffects.LightbarZones;
                                var HpLightbar_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentHp, 0, maxHp,
                                        HpLightbar_Collection.Length, 0);

                                for (int i = 0; i < HpLightbar_Collection.Length; i++)
                                {
                                    var col = colHpfull;

                                    if (polHpz2 < criticalThresh)
                                    {
                                        col = colHpcritical;
                                    }

                                    GlobalApplyMapLightbarLighting(HpLightbar_Collection[i],
                                        HpLightbar_Interpolate > i ? col : colHpempty, false, false);
                                }
                            }

                        }

                        //MP
                        if (maxMp != 0)
                        {
                            var polMp = (currentMp - 0) * (40 - 0) / (maxMp - 0) + 0;
                            var polMpz = (currentMp - 0) * ((long)65535 - 0) / (maxMp - 0) + 0;
                            var polMpz2 = (currentMp - 0) * (1.0 - 0.0) / (maxMp - 0) + 0.0;

                            GlobalUpdateBulbStateBrightness(BulbModeTypes.MpTracker, colMpfull, (ushort)polMpz,
                                250);

                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, polMpz2);
                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, false, polMpz2);
                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, false, polMpz2);
                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.MpTracker, colMpempty, colMpfull, polMpz2);

                            //FKeys
                            if (_FKeyMode == FKeyMode.HpMp)
                            {
                                var MpFunction_Collection = DeviceEffects.Function2;
                                var MpFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp,
                                        MpFunction_Collection.Length, 0);

                                for (int i = 0; i < MpFunction_Collection.Length; i++)
                                {
                                    if (_playerInfo.IsCasting)
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
                                    if (_playerInfo.IsCasting)
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
                            if (_LightbarMode == LightbarMode.MpTracker)
                            {
                                var MpLightbar_Collection = DeviceEffects.LightbarZones;
                                var MpLightbar_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(currentMp, 0, maxMp,
                                        MpLightbar_Collection.Length, 0);

                                for (int i = 0; i < MpLightbar_Collection.Length; i++)
                                {
                                    GlobalApplyMapLightbarLighting(MpLightbar_Collection[i],
                                        MpLightbar_Interpolate > i ? colMpfull : colMpempty, false, false);
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
                                            else if (ChromaticsSettings.ChromaticsSettingsQwertyMode == KeyRegion.ESDF)
                                            {
                                                if (FfxivHotbar.KeybindtranslationESDF.ContainsKey(action.ActionKey))
                                                {
                                                    var keyid = FfxivHotbar.KeybindtranslationESDF[action.ActionKey];

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

                                    if (GetACTJob() != "Gladiator")
                                        SwitchACTJob("Gladiator");
                                    break;
                                case Actor.Job.PGL:
                                    _role = _playerData.CurrentPlayer.PGL_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.PGL;
                                    if (GetACTJob() != "Pugilist")
                                        SwitchACTJob("Pugilist");
                                    break;
                                case Actor.Job.MRD:
                                    _role = _playerData.CurrentPlayer.MRD_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.MRD;
                                    if (GetACTJob() != "Marauder")
                                        SwitchACTJob("Marauder");
                                    break;
                                case Actor.Job.LNC:
                                    _role = _playerData.CurrentPlayer.LNC_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.LNC;
                                    if (GetACTJob() != "Lancer")
                                        SwitchACTJob("Lancer");
                                    break;
                                case Actor.Job.ARC:
                                    _role = _playerData.CurrentPlayer.ARC_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.ARC;
                                    if (GetACTJob() != "Archer")
                                        SwitchACTJob("Archer");
                                    break;
                                case Actor.Job.CNJ:
                                    _role = _playerData.CurrentPlayer.CNJ_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.CNJ;
                                    if (GetACTJob() != "Conjurer")
                                        SwitchACTJob("Conjurer");
                                    break;
                                case Actor.Job.THM:
                                    _role = _playerData.CurrentPlayer.THM_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.THM;
                                    if (GetACTJob() != "Thaumaturge")
                                        SwitchACTJob("Thaumaturge");
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
                                    if (GetACTJob() != "Paladin")
                                        SwitchACTJob("Paladin");
                                    break;
                                case Actor.Job.MNK:
                                    _role = _playerData.CurrentPlayer.PGL_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.PGL;
                                    if (GetACTJob() != "Monk")
                                        SwitchACTJob("Monk");
                                    break;
                                case Actor.Job.WAR:
                                    _role = _playerData.CurrentPlayer.MRD_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.MRD;
                                    if (GetACTJob() != "Warrior")
                                        SwitchACTJob("Warrior");
                                    break;
                                case Actor.Job.DRG:
                                    _role = _playerData.CurrentPlayer.LNC_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.LNC;
                                    if (GetACTJob() != "Dragoon")
                                        SwitchACTJob("Dragoon");
                                    break;
                                case Actor.Job.BRD:
                                    _role = _playerData.CurrentPlayer.ARC_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.ARC;
                                    if (GetACTJob() != "Bard")
                                        SwitchACTJob("Bard");
                                    break;
                                case Actor.Job.WHM:
                                    _role = _playerData.CurrentPlayer.CNJ_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.CNJ;
                                    if (GetACTJob() != "White Mage")
                                        SwitchACTJob("White Mage");
                                    break;
                                case Actor.Job.BLM:
                                    _role = _playerData.CurrentPlayer.THM_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.THM;
                                    if (GetACTJob() != "Black Mage")
                                        SwitchACTJob("Black Mage");
                                    break;
                                case Actor.Job.ACN:
                                    _role = _playerData.CurrentPlayer.ACN_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.ACN;
                                    if (GetACTJob() != "Arcanist")
                                        SwitchACTJob("Arcanist");
                                    break;
                                case Actor.Job.SMN:
                                    _role = _playerData.CurrentPlayer.ACN_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.ACN;
                                    if (GetACTJob() != "Summoner")
                                        SwitchACTJob("Summoner");
                                    break;
                                case Actor.Job.SCH:
                                    _role = _playerData.CurrentPlayer.ACN_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.ACN;
                                    if (GetACTJob() != "Scholar")
                                        SwitchACTJob("Scholar");
                                    break;
                                case Actor.Job.ROG:
                                    _role = _playerData.CurrentPlayer.ROG_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.ROG;
                                    if (GetACTJob() != "Rouge")
                                        SwitchACTJob("Rouge");
                                    break;
                                case Actor.Job.NIN:
                                    _role = _playerData.CurrentPlayer.ROG_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.ROG;
                                    if (GetACTJob() != "Ninja")
                                        SwitchACTJob("Ninja");
                                    break;
                                case Actor.Job.MCH:
                                    _role = _playerData.CurrentPlayer.MCH_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.MCH;
                                    if (GetACTJob() != "Machinist")
                                        SwitchACTJob("Machinist");
                                    break;
                                case Actor.Job.DRK:
                                    _role = _playerData.CurrentPlayer.DRK_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.DRK;
                                    if (GetACTJob() != "Dark Knight")
                                        SwitchACTJob("Dark Knight");
                                    break;
                                case Actor.Job.AST:
                                    _role = _playerData.CurrentPlayer.AST_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.AST;
                                    if (GetACTJob() != "Astrologian")
                                        SwitchACTJob("Astrologian");
                                    break;
                                case Actor.Job.SAM:
                                    _role = _playerData.CurrentPlayer.SAM_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.SAM;
                                    if (GetACTJob() != "Samurai")
                                        SwitchACTJob("Samurai");
                                    break;
                                case Actor.Job.RDM:
                                    _role = _playerData.CurrentPlayer.RDM_CurrentEXP;
                                    _currentlvl = _playerData.CurrentPlayer.RDM;
                                    if (GetACTJob() != "Red Mage")
                                        SwitchACTJob("Red Mage");
                                    break;
                                default:
                                    _role = _playerData.CurrentPlayer.WVR_CurrentEXP;
                                    _currentlvl = 0;
                                    break;
                            }


                            if (_currentlvl == 70)
                            {
                                if (_LightbarMode == LightbarMode.CurrentExp)
                                {
                                    foreach (var f in DeviceEffects.LightbarZones)
                                    {
                                        GlobalApplyMapLightbarLighting(f, expcolfull, false, false);
                                    }
                                }

                                if (_FKeyMode == FKeyMode.CurrentExp)
                                {
                                    foreach (var f in DeviceEffects.Functions)
                                    {
                                        GlobalApplyMapKeyLighting(f, expcolfull, false, false);
                                    }
                                }
                            }
                            else
                            {
                                //var lvltranslator = (_role - 0) * (19 - 1) / (Helpers.ExperienceTable[_currentlvl] - 0) + 0;

                                if (_LightbarMode == LightbarMode.CurrentExp)
                                {
                                    var LBExp_Collection = DeviceEffects.LightbarZones;
                                    var LBExp_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(_role, 0, Helpers.ExperienceTable[_currentlvl], LBExp_Collection.Length, 0);

                                    for (var i = 0; i < LBExp_Collection.Length; i++)
                                    {
                                        GlobalApplyMapLightbarLighting(LBExp_Collection[i], LBExp_Interpolate > i ? expcolfull : expcolempty, false, false);
                                    }
                                }

                                if (_FKeyMode == FKeyMode.CurrentExp)
                                {
                                    var FKExp_Collection = DeviceEffects.Functions;
                                    var FKExp_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(_role, 0, Helpers.ExperienceTable[_currentlvl], FKExp_Collection.Length, 0);

                                    for (var i2 = 0; i2 < FKExp_Collection.Length; i2++)
                                    {
                                        GlobalApplyMapKeyLighting(FKExp_Collection[i2], FKExp_Interpolate > i2 ? expcolfull : expcolempty, false, false);
                                    }
                                }
                            }
                            
                            
                            //Job Gauges
                            ImplementJobGauges(statEffects, _baseColor);


                            //Pull Countdown
                            if (!ChatInit)
                            {
                                ChatpreviousArrayIndex = 0;
                                ChatpreviousOffset = 0;
                                ChatInit = true;
                            }

                            if (Reader.CanGetChatLog())
                            {
                                var ChatReadResult = Reader.GetChatLog(ChatpreviousArrayIndex, ChatpreviousOffset);
                                ChatpreviousArrayIndex = ChatReadResult.PreviousArrayIndex;
                                ChatpreviousOffset = ChatReadResult.PreviousOffset;

                                if (pullCountdownRun)
                                {
                                    if (pullCountInterval == pullCountMax)
                                    {
                                        _rzFl1Cts.Cancel();
                                        _corsairF1Cts.Cancel();

                                        GlobalFlash1(
                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingPullCountdownEngage),
                                            200, DeviceEffects.GlobalKeys3);

                                        pullCountdownRun = false;
                                    }

                                    if (_FKeyMode == FKeyMode.PullCountdown)
                                    {
                                        var FKPullCount_Collection = DeviceEffects.Functions;
                                        var FKPullCount_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(pullCountInterval, 0, pullCountMax, 0, FKPullCount_Collection.Length);

                                        if (pullCountMax - pullCountInterval > FKPullCount_Collection.Length)
                                        {
                                            FKPullCount_Interpolate = FKPullCount_Collection.Length;
                                        }

                                        for (var i2 = 0; i2 < FKPullCount_Collection.Length; i2++)
                                        {
                                            GlobalApplyMapKeyLighting(FKPullCount_Collection[i2], FKPullCount_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings.ColorMappingPullCountdownTick) : ColorTranslator.FromHtml(ColorMappings.ColorMappingPullCountdownEmpty), false, false);
                                        }
                                    }

                                    if (_LightbarMode == LightbarMode.PullCountdown)
                                    {
                                        var LBPullCount_Collection = DeviceEffects.LightbarZones;
                                        var LBPullCount_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Int(pullCountInterval, 0, pullCountMax, 0, LBPullCount_Collection.Length);

                                        if (pullCountMax - pullCountInterval > LBPullCount_Collection.Length)
                                        {
                                            LBPullCount_Interpolate = LBPullCount_Collection.Length;
                                        }

                                        for (var i2 = 0; i2 < LBPullCount_Collection.Length; i2++)
                                        {
                                            GlobalApplyMapLightbarLighting(LBPullCount_Collection[i2], LBPullCount_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings.ColorMappingPullCountdownTick) : ColorTranslator.FromHtml(ColorMappings.ColorMappingPullCountdownEmpty), false, false);
                                        }
                                    }
                                }
                                else
                                {
                                    if (_FKeyMode == FKeyMode.PullCountdown)
                                    {
                                        var FKPullCount_Collection = DeviceEffects.Functions;
                                        
                                        for (var i2 = 0; i2 < FKPullCount_Collection.Length; i2++)
                                        {
                                            if (_playerInfo.IsCasting)
                                            {
                                                break;
                                            }

                                            GlobalApplyMapKeyLighting(FKPullCount_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingPullCountdownEmpty), false, false);
                                        }
                                    }

                                    if (_LightbarMode == LightbarMode.PullCountdown)
                                    {
                                        var LBPullCount_Collection = DeviceEffects.LightbarZones;

                                        for (var i2 = 0; i2 < LBPullCount_Collection.Length; i2++)
                                        {
                                            GlobalApplyMapLightbarLighting(LBPullCount_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingPullCountdownEmpty), false, false);
                                        }
                                    }

                                    if (ChatReadResult.ChatLogItems.Count > 0)
                                    {
                                        var chatItem =
                                            ChatReadResult.ChatLogItems
                                                .LastOrDefault(); //Engage! //戦闘開始！ //À l'attaque ! //Start!
                                        if (chatItem.Code == "00B9")
                                        {
                                            if (chatItem.Line.StartsWith("Battle commencing in"))
                                            {
                                                var pullcount = chatItem.Line.Substring(21).Split(' ')[0];
                                                int.TryParse(pullcount, out int x);
                                                pullCountInterval = 0;
                                                pullCountMax = x;
                                                
                                                var pullTimer = new System.Timers.Timer();
                                                pullTimer.Elapsed += new ElapsedEventHandler((source, e) =>
                                                    {
                                                        pullCountInterval++;

                                                        if (pullCountInterval == pullCountMax)
                                                        {
                                                            pullTimer.Stop();
                                                        }
                                                    });
                                                pullTimer.Interval = 960;
                                                pullTimer.Start();

                                                pullCountdownRun = true;

                                            }
                                            else if (chatItem.Line.StartsWith("戦闘開始まで"))
                                            {
                                                var pullcount = chatItem.Line.Substring(7).Split('秒')[0];
                                                int.TryParse(pullcount, out int x);
                                                pullCountInterval = 0;
                                                pullCountMax = x;

                                                var pullTimer = new System.Timers.Timer();
                                                pullTimer.Elapsed += new ElapsedEventHandler((source, e) =>
                                                {
                                                    pullCountInterval++;

                                                    if (pullCountInterval == pullCountMax)
                                                    {
                                                        pullTimer.Stop();
                                                    }
                                                });
                                                pullTimer.Interval = 960;
                                                pullTimer.Start();

                                                pullCountdownRun = true;
                                            }
                                            else if (chatItem.Line.StartsWith("Début du combat dans"))
                                            {
                                                var pullcount = chatItem.Line.Substring(21).Split(' ')[0];
                                                int.TryParse(pullcount, out int x);
                                                pullCountInterval = 0;
                                                pullCountMax = x;

                                                var pullTimer = new System.Timers.Timer();
                                                pullTimer.Elapsed += new ElapsedEventHandler((source, e) =>
                                                {
                                                    pullCountInterval++;

                                                    if (pullCountInterval == pullCountMax)
                                                    {
                                                        pullTimer.Stop();
                                                    }
                                                });
                                                pullTimer.Interval = 960;
                                                pullTimer.Start();

                                                pullCountdownRun = true;
                                            }
                                            else if (chatItem.Line.StartsWith("Noch"))
                                            {
                                                var pullcount = chatItem.Line.Substring(5).Split(' ')[0];
                                                int.TryParse(pullcount, out int x);
                                                pullCountInterval = 0;
                                                pullCountMax = x;

                                                var pullTimer = new System.Timers.Timer();
                                                pullTimer.Elapsed += new ElapsedEventHandler((source, e) =>
                                                {
                                                    pullCountInterval++;

                                                    if (pullCountInterval == pullCountMax)
                                                    {
                                                        pullTimer.Stop();
                                                    }
                                                });
                                                pullTimer.Interval = 960;
                                                pullTimer.Start();

                                                pullCountdownRun = true;
                                            }
                                        }
                                    }
                                }
                            }

                            //ACT
                            if (!blockACTVersion)
                            {
                                if (Process.GetProcessesByName("Advanced Combat Tracker").Length > 0)
                                {
                                    var _ACTData = ACTInterface.FetchActData();

                                    if (_ACTData.Version != ACTVersionMatch)
                                    {
                                        blockACTVersion = true;
                                        WriteConsole(ConsoleTypes.Error,
                                            "You are running an outdated verion of the Chromatics ACT Plugin. Please update the plugin and restart Chromatics to use ACT features.");
                                    }
                                    else
                                    {
                                        //Process ACT Data
                                        
                                        //Custom Triggers
                                        if (ChromaticsSettings.ChromaticsSettingsACTFlashCustomTrigger)
                                        {
                                            if (_ACTData.CustomTriggerActive)
                                            {
                                                if (!threshTrigger)
                                                {
                                                    _rzFl1Cts.Cancel();
                                                    _corsairF1Cts.Cancel();
                                                    GlobalFlash1(
                                                        ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTCustomTriggerBell), 200,
                                                        DeviceEffects.GlobalKeys3);

                                                    threshTrigger = true;
                                                }
                                            }
                                            else
                                            {
                                                threshTrigger = false;
                                            }
                                        }

                                        if (_ACTMode == ACTMode.CustomTrigger)
                                        {
                                            if (_ACTData.CustomTriggerActive)
                                            {
                                                GlobalApplyKeySingleLighting(DevModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerBell));
                                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerBell), "All");
                                                GlobalApplyMapMouseLighting(DevModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerBell), false);
                                                GlobalApplyMapHeadsetLighting(DevModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerBell), false);
                                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerBell), false, "All");
                                                GlobalApplyMapChromaLinkLighting(DevModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerBell));

                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTTrigger_Collection = DeviceEffects.Functions;
                                                    for (var i2 = 0; i2 < FKACTTrigger_Collection.Length; i2++)
                                                    {
                                                        GlobalApplyMapKeyLighting(FKACTTrigger_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                    }
                                                }

                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTTrigger_Collection = DeviceEffects.LightbarZones;
                                                    for (var i2 = 0; i2 < LBACTTrigger_Collection.Length; i2++)
                                                    {
                                                        GlobalApplyMapLightbarLighting(LBACTTrigger_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerIdle));
                                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerIdle), "All");
                                                GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerIdle), false);
                                                GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerIdle), false);
                                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerIdle), false, "All");
                                                GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerIdle));

                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTTrigger_Collection = DeviceEffects.Functions;
                                                    for (var i2 = 0; i2 < FKACTTrigger_Collection.Length; i2++)
                                                    {
                                                        if (_playerInfo.IsCasting)
                                                        {
                                                            break;
                                                        }

                                                        GlobalApplyMapKeyLighting(FKACTTrigger_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerIdle), false, false);
                                                    }
                                                }

                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTTrigger_Collection = DeviceEffects.LightbarZones;
                                                    for (var i2 = 0; i2 < LBACTTrigger_Collection.Length; i2++)
                                                    {
                                                        GlobalApplyMapLightbarLighting(LBACTTrigger_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTCustomTriggerIdle), false, false);
                                                    }
                                                }
                                            }
                                        }

                                        //Timers
                                        if (ChromaticsSettings.ChromaticsSettingsACTFlashTimer)
                                        {
                                            if (_ACTData.CustomTriggerActive)
                                            {
                                                if (!threshTimer)
                                                {
                                                    _rzFl1Cts.Cancel();
                                                    _corsairF1Cts.Cancel();
                                                    GlobalFlash1(
                                                        ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTTimerFlash), 200,
                                                        DeviceEffects.GlobalKeys3);

                                                    threshTimer = true;
                                                }
                                            }
                                            else
                                            {
                                                threshTimer = false;
                                            }
                                        }

                                        if (_ACTMode == ACTMode.Timer)
                                        {
                                            if (_ACTData.CustomTriggerActive)
                                            {
                                                GlobalApplyKeySingleLighting(DevModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerBuild));
                                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerBuild), "All");
                                                GlobalApplyMapMouseLighting(DevModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerBuild), false);
                                                GlobalApplyMapHeadsetLighting(DevModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerBuild), false);
                                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerBuild), false, "All");
                                                GlobalApplyMapChromaLinkLighting(DevModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerBuild));

                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTTimer_Collection = DeviceEffects.Functions;
                                                    for (var i2 = 0; i2 < FKACTTimer_Collection.Length; i2++)
                                                    {
                                                        GlobalApplyMapKeyLighting(FKACTTimer_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerBuild), false, false);
                                                    }
                                                }

                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTTimer_Collection = DeviceEffects.LightbarZones;
                                                    for (var i2 = 0; i2 < LBACTTimer_Collection.Length; i2++)
                                                    {
                                                        GlobalApplyMapLightbarLighting(LBACTTimer_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerBuild), false, false);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                GlobalApplyKeySingleLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerIdle));
                                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerIdle), "All");
                                                GlobalApplyMapMouseLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerIdle), false);
                                                GlobalApplyMapHeadsetLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerIdle), false);
                                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerIdle), false, "All");
                                                GlobalApplyMapChromaLinkLighting(DevModeTypes.EnmityTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerIdle));

                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTTimer_Collection = DeviceEffects.Functions;
                                                    for (var i2 = 0; i2 < FKACTTimer_Collection.Length; i2++)
                                                    {
                                                        if (_playerInfo.IsCasting)
                                                        {
                                                            break;
                                                        }

                                                        GlobalApplyMapKeyLighting(FKACTTimer_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerIdle), false, false);
                                                    }
                                                }

                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTTimer_Collection = DeviceEffects.LightbarZones;
                                                    for (var i2 = 0; i2 < LBACTTimer_Collection.Length; i2++)
                                                    {
                                                        GlobalApplyMapLightbarLighting(LBACTTimer_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTTimerIdle), false, false);
                                                    }
                                                }
                                            }
                                        }

                                        if (_ACTData.IsConnected)
                                        {
                                            var jobkey = _actJobs.FirstOrDefault(x => x.Value == GetACTJob()).Key;

                                            //Enrage Timers
                                            EncounterData encounter = null;
                                            foreach (var zone in EncounterTimers.Encounters)
                                            {
                                                if (zone.InstanceName == _ACTData.CurrentEncounterName)
                                                {
                                                    encounter = zone;
                                                    break;
                                                }
                                            }

                                            if (encounter != null)
                                            {
                                                if ((encounter.BossName == "None") || _ACTData.Enemies.Contains(encounter.BossName))
                                                {
                                                    if (_FKeyMode == FKeyMode.ACTEnrage)
                                                    {
                                                        var FKACTEnrage_Collection = DeviceEffects.Functions;
                                                        var FKACTEnrage_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.CurrentEncounterTime, 0, encounter.EnrageTimer, 0, FKACTEnrage_Collection.Length);
                                                        var enrageTimerCol = ColorTranslator.FromHtml(ColorMappings.ColorMappingACTEnrageCountdown);

                                                        if (FKACTEnrage_Interpolate >= (encounter.EnrageTimer - 60))
                                                        {
                                                            enrageTimerCol = ColorTranslator.FromHtml(ColorMappings.ColorMappingACTEnrageWarning);
                                                        }

                                                        for (var i2 = 0; i2 < FKACTEnrage_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTEnrage_Collection[i2], FKACTEnrage_Interpolate > i2 ? enrageTimerCol : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTEnrageEmpty), false, false);
                                                        }
                                                    }

                                                    if (_LightbarMode == LightbarMode.ACTTracker)
                                                    {
                                                        var LBACTEnrage_Collection = DeviceEffects.LightbarZones;
                                                        var LBACTEnrage_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDPS, 0, ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0], 0, LBACTEnrage_Collection.Length);
                                                        var enrageTimerCol = ColorTranslator.FromHtml(ColorMappings.ColorMappingACTEnrageCountdown);

                                                        if (LBACTEnrage_Interpolate >= (encounter.EnrageTimer - 60))
                                                        {
                                                            enrageTimerCol = ColorTranslator.FromHtml(ColorMappings.ColorMappingACTEnrageWarning);
                                                        }

                                                        for (var i2 = 0; i2 < LBACTEnrage_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTEnrage_Collection[i2], LBACTEnrage_Interpolate > i2 ? enrageTimerCol : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTEnrageEmpty), false, false);
                                                        }
                                                    }
                                                }
                                            }
                                            

                                            //DPS Tracker
                                            if (_ACTMode == ACTMode.DPS)
                                            {
                                                if ((long)_ACTData.PlayerCurrentDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0])
                                                {
                                                    if (!threshFlash)
                                                    {
                                                        if (ChromaticsSettings.ChromaticsSettingsACTFlash)
                                                        {
                                                            _rzFl1Cts.Cancel();
                                                            _corsairF1Cts.Cancel();
                                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdFlash), 200, DeviceEffects.GlobalKeys3);
                                                        }

                                                        threshFlash = true;
                                                    }

                                                    GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                        (ushort)(long)65535,
                                                        250);

                                                    GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                    GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                }
                                                else
                                                {
                                                    threshFlash = false;

                                                    var polACTDPSz =
                                                        ((long)_ACTData.PlayerCurrentDPS - 0) * ((long)65535 - 0) / (ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0] - 0) + 0;
                                                    var polACTDPSz2 = (_ACTData.PlayerCurrentDPS - 0) * (1.0 - 0.0) / (ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0] - 0) + 0.0;
                                                    
                                                    if (polACTDPSz >= 0 && polACTDPSz <= 65535)
                                                    {
                                                        GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild),
                                                            (ushort) polACTDPSz,
                                                            250);
                                                    }

                                                    if (polACTDPSz2 >= 0.0 && polACTDPSz2 <= 1.0)
                                                    {
                                                        GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTDPSz2);
                                                        GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTDPSz2);
                                                        GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTDPSz2);
                                                        GlobalApplyMapChromaLinkLightingBrightness(
                                                            DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTDPSz2);
                                                    }
                                                }

                                                //Functions
                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTDPS_Collection = DeviceEffects.Functions;
                                                    
                                                    if ((long)_ACTData.PlayerCurrentDPS >= ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < FKACTDPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTDPS_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var FKACTDPS_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDPS, 0, ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0], FKACTDPS_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < FKACTDPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTDPS_Collection[i2], FKACTDPS_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Lightbar
                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTDPS_Collection = DeviceEffects.LightbarZones;

                                                    if ((long)_ACTData.PlayerCurrentDPS >= ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < LBACTDPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTDPS_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var LBACTDPS_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDPS, 0, ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0], LBACTDPS_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < LBACTDPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTDPS_Collection[i2], LBACTDPS_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Mousepad
                                                var ACTDPSMousePadCollection = 5;

                                                if ((long) _ACTData.PlayerCurrentDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDPSMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDPSMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDPS, 0, ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0], ACTDPSMousePadCollection, 0);

                                                    for (int i = 0; i < ACTDPSMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ACTDPSMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                                    }
                                                }
                                                
                                                //Keypad
                                                var ACTDPSKeypad_Collection = DeviceEffects.Keypadzones;

                                                if ((long) _ACTData.PlayerCurrentDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDPSKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                            false, ACTDPSKeypad_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDPSKeypad_Interpolate =
                                                        Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDPS, 0, ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0],
                                                            ACTDPSKeypad_Collection.Length, 0);

                                                    for (int i = 0; i < ACTDPSKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ACTDPSKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                            false, ACTDPSKeypad_Collection[i]);
                                                    }
                                                }

                                                //MultiKeyboard
                                                var ACTDPSMulti_Collection = DeviceEffects.Multikeyzones;

                                                if ((long) _ACTData.PlayerCurrentDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDPSMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), ACTDPSMulti_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDPSMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDPS, 0, ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0], ACTDPSMulti_Collection.Length, 0);

                                                    for (int i = 0; i < ACTDPSMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ACTDPSMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTDPSMulti_Collection[i]);
                                                    }
                                                }
                                                
                                                //Mouse
                                                var ACTDPSMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                                var ACTDPSMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                                if ((long) _ACTData.PlayerCurrentDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDPSMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTDPSMouseStrip_CollectionA[i], ACTDPSMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDPSMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDPS, 0, ChromaticsSettings.ChromaticsSettingsACTDPS[jobkey][0], ACTDPSMouseStrip_CollectionA.Length, 0);

                                                    for (int i = 0; i < ACTDPSMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTDPSMouseStrip_CollectionA[i], ACTDPSMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }

                                                    
                                            }

                                            //HPS Tracker
                                            if (_ACTMode == ACTMode.HPS)
                                            {
                                                

                                                if ((long)_ACTData.PlayerCurrentHPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0])
                                                {
                                                    if (!threshFlash)
                                                    {
                                                        if (ChromaticsSettings.ChromaticsSettingsACTFlash)
                                                        {
                                                            _rzFl1Cts.Cancel();
                                                            _corsairF1Cts.Cancel();
                                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdFlash), 200, DeviceEffects.GlobalKeys3);
                                                        }

                                                        threshFlash = true;
                                                    }

                                                    GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                        (ushort)(long)65535,
                                                        250);

                                                    GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                    GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                }
                                                else
                                                {
                                                    threshFlash = false;

                                                    var polACTHPSz =
                                                        ((long)_ACTData.PlayerCurrentHPS - 0) * ((long)65535 - 0) / (ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0] - 0) + 0;
                                                    var polACTHPSz2 = ((long)_ACTData.PlayerCurrentHPS - 0) * (1.0 - 0.0) / (ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0] - 0) + 0.0;

                                                    if (polACTHPSz >= 0 && polACTHPSz <= 65535)
                                                    {
                                                        GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild),
                                                            (ushort) polACTHPSz,
                                                            250);
                                                    }

                                                    if (polACTHPSz2 >= 0.0 && polACTHPSz2 <= 1.0)
                                                    {
                                                        GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTHPSz2);
                                                        GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTHPSz2);
                                                        GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTHPSz2);
                                                        GlobalApplyMapChromaLinkLightingBrightness(
                                                            DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTHPSz2);
                                                    }
                                                }

                                                //Functions
                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTHPS_Collection = DeviceEffects.Functions;

                                                    if ((long)_ACTData.PlayerCurrentHPS >= ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < FKACTHPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTHPS_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var FKACTHPS_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentHPS, 0, ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0], FKACTHPS_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < FKACTHPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTHPS_Collection[i2], FKACTHPS_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Lightbar
                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTHPS_Collection = DeviceEffects.LightbarZones;

                                                    if ((long)_ACTData.PlayerCurrentHPS >= ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < LBACTHPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTHPS_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var LBACTHPS_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentHPS, 0, ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0], LBACTHPS_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < LBACTHPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTHPS_Collection[i2], LBACTHPS_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Mousepad
                                                var ACTHPSMousePadCollection = 5;

                                                if ((long)_ACTData.PlayerCurrentHPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTHPSMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTHPSMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentHPS, 0, ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0], ACTHPSMousePadCollection, 0);

                                                    for (int i = 0; i < ACTHPSMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ACTHPSMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                                    }
                                                }

                                                //Keypad
                                                var ACTHPSKeypad_Collection = DeviceEffects.Keypadzones;

                                                if ((long)_ACTData.PlayerCurrentHPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTHPSKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                            false, ACTHPSKeypad_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTHPSKeypad_Interpolate =
                                                        Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentHPS, 0, ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0],
                                                            ACTHPSKeypad_Collection.Length, 0);

                                                    for (int i = 0; i < ACTHPSKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ACTHPSKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                            false, ACTHPSKeypad_Collection[i]);
                                                    }
                                                }

                                                //MultiKeyboard
                                                var ACTHPSMulti_Collection = DeviceEffects.Multikeyzones;

                                                if ((long)_ACTData.PlayerCurrentHPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTHPSMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), ACTHPSMulti_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTHPSMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentHPS, 0, ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0], ACTHPSMulti_Collection.Length, 0);

                                                    for (int i = 0; i < ACTHPSMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ACTHPSMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTHPSMulti_Collection[i]);
                                                    }
                                                }

                                                //Mouse
                                                var ACTHPSMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                                var ACTHPSMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                                if ((long)_ACTData.PlayerCurrentHPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTHPSMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTHPSMouseStrip_CollectionA[i], ACTHPSMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTHPSMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentHPS, 0, ChromaticsSettings.ChromaticsSettingsACTHPS[jobkey][0], ACTHPSMouseStrip_CollectionA.Length, 0);

                                                    for (int i = 0; i < ACTHPSMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTHPSMouseStrip_CollectionA[i], ACTHPSMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                            }

                                            //GroupDPS Tracker
                                            if (_ACTMode == ACTMode.GroupDPS)
                                            {
                                                

                                                if ((long)_ACTData.PlayerCurrentGroupDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0])
                                                {

                                                    if (!threshFlash)
                                                    {
                                                        if (ChromaticsSettings.ChromaticsSettingsACTFlash)
                                                        {
                                                            _rzFl1Cts.Cancel();
                                                            _corsairF1Cts.Cancel();
                                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdFlash), 200, DeviceEffects.GlobalKeys3);
                                                        }

                                                        threshFlash = true;
                                                    }

                                                    GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                        (ushort)(long)65535,
                                                        250);

                                                    GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                    GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                }
                                                else
                                                {
                                                    threshFlash = false;

                                                    var polACTGroupDPSz =
                                                        ((long)_ACTData.PlayerCurrentGroupDPS - 0) * ((long)65535 - 0) / (ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0] - 0) + 0;
                                                    var polACTGroupDPSz2 = ((long)_ACTData.PlayerCurrentGroupDPS - 0) * (1.0 - 0.0) / (ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0] - 0) + 0.0;

                                                    if (polACTGroupDPSz >= 0 && polACTGroupDPSz <= 65535)
                                                    {
                                                        GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild),
                                                            (ushort) polACTGroupDPSz,
                                                            250);
                                                    }

                                                    if (polACTGroupDPSz2 >= 0.0 && polACTGroupDPSz2 <= 1.0)
                                                    {
                                                        GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTGroupDPSz2);
                                                        GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false,
                                                            polACTGroupDPSz2);
                                                        GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false,
                                                            polACTGroupDPSz2);
                                                        GlobalApplyMapChromaLinkLightingBrightness(
                                                            DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTGroupDPSz2);
                                                    }
                                                }

                                                //Functions
                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTGroupDPS_Collection = DeviceEffects.Functions;

                                                    if ((long)_ACTData.PlayerCurrentGroupDPS >= ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < FKACTGroupDPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTGroupDPS_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var FKACTGroupDPS_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentGroupDPS, 0, ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0], FKACTGroupDPS_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < FKACTGroupDPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTGroupDPS_Collection[i2], FKACTGroupDPS_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Lightbar
                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTGroupDPS_Collection = DeviceEffects.LightbarZones;

                                                    if ((long)_ACTData.PlayerCurrentGroupDPS >= ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < LBACTGroupDPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTGroupDPS_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var LBACTGroupDPS_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentGroupDPS, 0, ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0], LBACTGroupDPS_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < LBACTGroupDPS_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTGroupDPS_Collection[i2], LBACTGroupDPS_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Mousepad
                                                var ACTGroupDPSMousePadCollection = 5;

                                                if ((long)_ACTData.PlayerCurrentGroupDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTGroupDPSMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTGroupDPSMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentGroupDPS, 0, ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0], ACTGroupDPSMousePadCollection, 0);

                                                    for (int i = 0; i < ACTGroupDPSMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ACTGroupDPSMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                                    }
                                                }

                                                //Keypad
                                                var ACTGroupDPSKeypad_Collection = DeviceEffects.Keypadzones;

                                                if ((long)_ACTData.PlayerCurrentGroupDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTGroupDPSKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                            false, ACTGroupDPSKeypad_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTGroupDPSKeypad_Interpolate =
                                                        Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentGroupDPS, 0, ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0],
                                                            ACTGroupDPSKeypad_Collection.Length, 0);

                                                    for (int i = 0; i < ACTGroupDPSKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ACTGroupDPSKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                            false, ACTGroupDPSKeypad_Collection[i]);
                                                    }
                                                }

                                                //MultiKeyboard
                                                var ACTGroupDPSMulti_Collection = DeviceEffects.Multikeyzones;

                                                if ((long)_ACTData.PlayerCurrentGroupDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTGroupDPSMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), ACTGroupDPSMulti_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTGroupDPSMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentGroupDPS, 0, ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0], ACTGroupDPSMulti_Collection.Length, 0);

                                                    for (int i = 0; i < ACTGroupDPSMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ACTGroupDPSMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTGroupDPSMulti_Collection[i]);
                                                    }
                                                }

                                                //Mouse
                                                var ACTGroupDPSMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                                var ACTGroupDPSMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                                if ((long)_ACTData.PlayerCurrentGroupDPS >=
                                                    ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTGroupDPSMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTGroupDPSMouseStrip_CollectionA[i], ACTGroupDPSMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTGroupDPSMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentGroupDPS, 0, ChromaticsSettings.ChromaticsSettingsACTGroupDPS[jobkey][0], ACTGroupDPSMouseStrip_CollectionA.Length, 0);

                                                    for (int i = 0; i < ACTGroupDPSMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTGroupDPSMouseStrip_CollectionA[i], ACTGroupDPSMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                            }

                                            //Crit Tracker
                                            if (_ACTMode == ACTMode.CritPrc)
                                            {
                                                

                                                if ((long)_ACTData.PlayerCurrentCrit >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0])
                                                {

                                                    if (!threshFlash)
                                                    {
                                                        if (ChromaticsSettings.ChromaticsSettingsACTFlash)
                                                        {
                                                            _rzFl1Cts.Cancel();
                                                            _corsairF1Cts.Cancel();
                                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdFlash), 200, DeviceEffects.GlobalKeys3);
                                                        }

                                                        threshFlash = true;
                                                    }

                                                    GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                        (ushort)(long)65535,
                                                        250);

                                                    GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                    GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                }
                                                else
                                                {
                                                    threshFlash = false;

                                                    var polACTCritz =
                                                        ((long)_ACTData.PlayerCurrentCrit - 0) * ((long)65535 - 0) / (ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0] - 0) + 0;
                                                    var polACTCritz2 = ((long)_ACTData.PlayerCurrentCrit - 0) * (1.0 - 0.0) / (ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0] - 0) + 0.0;

                                                    if (polACTCritz >= 0 && polACTCritz <= 65535)
                                                    {
                                                        GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild),
                                                            (ushort) polACTCritz,
                                                            250);
                                                    }

                                                    if (polACTCritz2 >= 0.0 && polACTCritz2 <= 1.0)
                                                    {
                                                        GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTCritz2);
                                                        GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTCritz2);
                                                        GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTCritz2);
                                                        GlobalApplyMapChromaLinkLightingBrightness(
                                                            DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTCritz2);
                                                    }
                                                }

                                                //Functions
                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTCrit_Collection = DeviceEffects.Functions;

                                                    if ((long)_ACTData.PlayerCurrentCrit >= ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < FKACTCrit_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTCrit_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var FKACTCrit_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCrit, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0], FKACTCrit_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < FKACTCrit_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTCrit_Collection[i2], FKACTCrit_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Lightbar
                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTCrit_Collection = DeviceEffects.LightbarZones;

                                                    if ((long)_ACTData.PlayerCurrentCrit >= ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < LBACTCrit_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTCrit_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var LBACTCrit_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCrit, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0], LBACTCrit_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < LBACTCrit_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTCrit_Collection[i2], LBACTCrit_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Mousepad
                                                var ACTCritMousePadCollection = 5;

                                                if ((long)_ACTData.PlayerCurrentCrit >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTCritMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTCritMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCrit, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0], ACTCritMousePadCollection, 0);

                                                    for (int i = 0; i < ACTCritMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ACTCritMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                                    }
                                                }

                                                //Keypad
                                                var ACTCritKeypad_Collection = DeviceEffects.Keypadzones;

                                                if ((long)_ACTData.PlayerCurrentCrit >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTCritKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                            false, ACTCritKeypad_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTCritKeypad_Interpolate =
                                                        Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCrit, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0],
                                                            ACTCritKeypad_Collection.Length, 0);

                                                    for (int i = 0; i < ACTCritKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ACTCritKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                            false, ACTCritKeypad_Collection[i]);
                                                    }
                                                }

                                                //MultiKeyboard
                                                var ACTCritMulti_Collection = DeviceEffects.Multikeyzones;

                                                if ((long)_ACTData.PlayerCurrentCrit >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTCritMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), ACTCritMulti_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTCritMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCrit, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0], ACTCritMulti_Collection.Length, 0);

                                                    for (int i = 0; i < ACTCritMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ACTCritMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTCritMulti_Collection[i]);
                                                    }
                                                }

                                                //Mouse
                                                var ACTCritMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                                var ACTCritMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                                if ((long)_ACTData.PlayerCurrentCrit >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTCritMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTCritMouseStrip_CollectionA[i], ACTCritMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTCritMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCrit, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCrit[jobkey][0], ACTCritMouseStrip_CollectionA.Length, 0);

                                                    for (int i = 0; i < ACTCritMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTCritMouseStrip_CollectionA[i], ACTCritMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                            }

                                            //DH Tracker
                                            if (_ACTMode == ACTMode.DHPrc)
                                            {
                                                

                                                if ((long)_ACTData.PlayerCurrentDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0])
                                                {
                                                    if (!threshFlash)
                                                    {
                                                        if (ChromaticsSettings.ChromaticsSettingsACTFlash)
                                                        {
                                                            _rzFl1Cts.Cancel();
                                                            _corsairF1Cts.Cancel();
                                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdFlash), 200, DeviceEffects.GlobalKeys3);
                                                        }

                                                        threshFlash = true;
                                                    }

                                                    GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                        (ushort)(long)65535,
                                                        250);

                                                    GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                    GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                }
                                                else
                                                {
                                                    threshFlash = false;

                                                    var polACTDHz =
                                                        ((long)_ACTData.PlayerCurrentDH - 0) * ((long)65535 - 0) / (ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0] - 0) + 0;
                                                    var polACTDHz2 = ((long)_ACTData.PlayerCurrentDH - 0) * (1.0 - 0.0) / (ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0] - 0) + 0.0;

                                                    if (polACTDHz >= 0 && polACTDHz <= 65535)
                                                    {
                                                        GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdSuccess),
                                                            (ushort) polACTDHz,
                                                            250);
                                                    }

                                                    if (polACTDHz2 >= 0.0 && polACTDHz2 <= 1.0)
                                                    {
                                                        GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTDHz2);
                                                        GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTDHz2);
                                                        GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTDHz2);
                                                        GlobalApplyMapChromaLinkLightingBrightness(
                                                            DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTDHz2);
                                                    }
                                                }

                                                //Functions
                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTDH_Collection = DeviceEffects.Functions;

                                                    if ((long)_ACTData.PlayerCurrentDH >= ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < FKACTDH_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTDH_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var FKACTDH_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0], FKACTDH_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < FKACTDH_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTDH_Collection[i2], FKACTDH_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Lightbar
                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTDH_Collection = DeviceEffects.LightbarZones;

                                                    if ((long)_ACTData.PlayerCurrentDH >= ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < LBACTDH_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTDH_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var LBACTDH_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0], LBACTDH_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < LBACTDH_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTDH_Collection[i2], LBACTDH_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Mousepad
                                                var ACTDHMousePadCollection = 5;

                                                if ((long)_ACTData.PlayerCurrentDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDHMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDHMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0], ACTDHMousePadCollection, 0);

                                                    for (int i = 0; i < ACTDHMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ACTDHMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                                    }
                                                }

                                                //Keypad
                                                var ACTDHKeypad_Collection = DeviceEffects.Keypadzones;

                                                if ((long)_ACTData.PlayerCurrentDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDHKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                            false, ACTDHKeypad_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDHKeypad_Interpolate =
                                                        Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0],
                                                            ACTDHKeypad_Collection.Length, 0);

                                                    for (int i = 0; i < ACTDHKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ACTDHKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                            false, ACTDHKeypad_Collection[i]);
                                                    }
                                                }

                                                //MultiKeyboard
                                                var ACTDHMulti_Collection = DeviceEffects.Multikeyzones;

                                                if ((long)_ACTData.PlayerCurrentDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDHMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), ACTDHMulti_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDHMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0], ACTDHMulti_Collection.Length, 0);

                                                    for (int i = 0; i < ACTDHMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ACTDHMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTDHMulti_Collection[i]);
                                                    }
                                                }

                                                //Mouse
                                                var ACTDHMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                                var ACTDHMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                                if ((long)_ACTData.PlayerCurrentDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDHMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTDHMouseStrip_CollectionA[i], ACTDHMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDHMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetDH[jobkey][0], ACTDHMouseStrip_CollectionA.Length, 0);

                                                    for (int i = 0; i < ACTDHMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTDHMouseStrip_CollectionA[i], ACTDHMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                            }

                                            //CritDH Tracker
                                            if (_ACTMode == ACTMode.CritDHPrc)
                                            {
                                                

                                                if ((long)_ACTData.PlayerCurrentCritDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0])
                                                {
                                                    if (!threshFlash)
                                                    {
                                                        if (ChromaticsSettings.ChromaticsSettingsACTFlash)
                                                        {
                                                            _rzFl1Cts.Cancel();
                                                            _corsairF1Cts.Cancel();
                                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdFlash), 200, DeviceEffects.GlobalKeys3);
                                                        }

                                                        threshFlash = true;
                                                    }

                                                    GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdBuild),
                                                        (ushort)(long)65535,
                                                        250);

                                                    GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdBuild), 1.0);
                                                    GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdBuild), false, 1.0);
                                                    GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdBuild), false, 1.0);
                                                    GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdBuild), 1.0);
                                                }
                                                else
                                                {
                                                    threshFlash = false;

                                                    var polACTCritDHz =
                                                        ((long)_ACTData.PlayerCurrentCritDH - 0) * ((long)65535 - 0) / (ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0] - 0) + 0;
                                                    var polACTCritDHz2 = ((long)_ACTData.PlayerCurrentCritDH - 0) * (1.0 - 0.0) / (ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0] - 0) + 0.0;

                                                    if (polACTCritDHz >= 0 && polACTCritDHz <= 65535)
                                                    {
                                                        GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdSuccess),
                                                            (ushort) polACTCritDHz,
                                                            250);
                                                    }

                                                    if (polACTCritDHz2 >= 0.0 && polACTCritDHz2 <= 1.0)
                                                    {
                                                        GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdSuccess), polACTCritDHz2);
                                                        GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdSuccess), false,
                                                            polACTCritDHz2);
                                                        GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdSuccess), false,
                                                            polACTCritDHz2);
                                                        GlobalApplyMapChromaLinkLightingBrightness(
                                                            DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdSuccess), polACTCritDHz2);
                                                    }
                                                }

                                                //Functions
                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTCritDH_Collection = DeviceEffects.Functions;

                                                    if ((long)_ACTData.PlayerCurrentCritDH >= ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < FKACTCritDH_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTCritDH_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var FKACTCritDH_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCritDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0], FKACTCritDH_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < FKACTCritDH_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTCritDH_Collection[i2], FKACTCritDH_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Lightbar
                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTCritDH_Collection = DeviceEffects.LightbarZones;

                                                    if ((long)_ACTData.PlayerCurrentCritDH >= ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < LBACTCritDH_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTCritDH_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var LBACTCritDH_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCritDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0], LBACTCritDH_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < LBACTCritDH_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTCritDH_Collection[i2], LBACTCritDH_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Mousepad
                                                var ACTCritDHMousePadCollection = 5;

                                                if ((long)_ACTData.PlayerCurrentCritDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTCritDHMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTCritDHMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCritDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0], ACTCritDHMousePadCollection, 0);

                                                    for (int i = 0; i < ACTCritDHMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ACTCritDHMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                                    }
                                                }

                                                //Keypad
                                                var ACTCritDHKeypad_Collection = DeviceEffects.Keypadzones;

                                                if ((long)_ACTData.PlayerCurrentCritDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTCritDHKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                            false, ACTCritDHKeypad_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTCritDHKeypad_Interpolate =
                                                        Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCritDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0],
                                                            ACTCritDHKeypad_Collection.Length, 0);

                                                    for (int i = 0; i < ACTCritDHKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ACTCritDHKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                            false, ACTCritDHKeypad_Collection[i]);
                                                    }
                                                }

                                                //MultiKeyboard
                                                var ACTCritDHMulti_Collection = DeviceEffects.Multikeyzones;

                                                if ((long)_ACTData.PlayerCurrentCritDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTCritDHMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), ACTCritDHMulti_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTCritDHMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCritDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0], ACTCritDHMulti_Collection.Length, 0);

                                                    for (int i = 0; i < ACTCritDHMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ACTCritDHMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTCritDHMulti_Collection[i]);
                                                    }
                                                }

                                                //Mouse
                                                var ACTCritDHMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                                var ACTCritDHMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                                if ((long)_ACTData.PlayerCurrentCritDH >=
                                                    ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTCritDHMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTCritDHMouseStrip_CollectionA[i], ACTCritDHMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTCritDHMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentCritDH, 0, ChromaticsSettings.ChromaticsSettingsACTTargetCritDH[jobkey][0], ACTCritDHMouseStrip_CollectionA.Length, 0);

                                                    for (int i = 0; i < ACTCritDHMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTCritDHMouseStrip_CollectionA[i], ACTCritDHMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                            }

                                            //Overheal Tracker
                                            if (_ACTMode == ACTMode.OverhealPrc)
                                            {
                                                

                                                if ((long)_ACTData.PlayerCurrentOverheal >=
                                                    ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0])
                                                {
                                                    if (!threshFlash)
                                                    {
                                                        if (ChromaticsSettings.ChromaticsSettingsACTFlash)
                                                        {
                                                            _rzFl1Cts.Cancel();
                                                            _corsairF1Cts.Cancel();
                                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdFlash), 200, DeviceEffects.GlobalKeys3);
                                                        }

                                                        threshFlash = true;
                                                    }

                                                    GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                        (ushort)(long)65535,
                                                        250);

                                                    GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                    GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                }
                                                else
                                                {
                                                    threshFlash = false;

                                                    var polACTOverhealz =
                                                        ((long)_ACTData.PlayerCurrentOverheal - 0) * ((long)65535 - 0) / (ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0] - 0) + 0;
                                                    var polACTOverhealz2 = ((long)_ACTData.PlayerCurrentOverheal - 0) * (1.0 - 0.0) / (ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0] - 0) + 0.0;

                                                    if (polACTOverhealz >= 0 && polACTOverhealz <= 65535)
                                                    {
                                                        GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild),
                                                            (ushort) polACTOverhealz,
                                                            250);
                                                    }

                                                    if (polACTOverhealz2 >= 0.0 && polACTOverhealz2 <= 1.0)
                                                    {
                                                        GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTOverhealz2);
                                                        GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false,
                                                            polACTOverhealz2);
                                                        GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false,
                                                            polACTOverhealz2);
                                                        GlobalApplyMapChromaLinkLightingBrightness(
                                                            DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTOverhealz2);
                                                    }
                                                }

                                                //Functions
                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTOverheal_Collection = DeviceEffects.Functions;

                                                    if ((long)_ACTData.PlayerCurrentOverheal >= ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < FKACTOverheal_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTOverheal_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var FKACTOverheal_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentOverheal, 0, ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0], FKACTOverheal_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < FKACTOverheal_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTOverheal_Collection[i2], FKACTOverheal_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Lightbar
                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTOverheal_Collection = DeviceEffects.LightbarZones;

                                                    if ((long)_ACTData.PlayerCurrentOverheal >= ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < LBACTOverheal_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTOverheal_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var LBACTOverheal_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentOverheal, 0, ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0], LBACTOverheal_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < LBACTOverheal_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTOverheal_Collection[i2], LBACTOverheal_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Mousepad
                                                var ACTOverhealMousePadCollection = 5;

                                                if ((long)_ACTData.PlayerCurrentOverheal >=
                                                    ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTOverhealMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTOverhealMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentOverheal, 0, ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0], ACTOverhealMousePadCollection, 0);

                                                    for (int i = 0; i < ACTOverhealMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ACTOverhealMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                                    }
                                                }

                                                //Keypad
                                                var ACTOverhealKeypad_Collection = DeviceEffects.Keypadzones;

                                                if ((long)_ACTData.PlayerCurrentOverheal >=
                                                    ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTOverhealKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                            false, ACTOverhealKeypad_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTOverhealKeypad_Interpolate =
                                                        Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentOverheal, 0, ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0],
                                                            ACTOverhealKeypad_Collection.Length, 0);

                                                    for (int i = 0; i < ACTOverhealKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ACTOverhealKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                            false, ACTOverhealKeypad_Collection[i]);
                                                    }
                                                }

                                                //MultiKeyboard
                                                var ACTOverhealMulti_Collection = DeviceEffects.Multikeyzones;

                                                if ((long)_ACTData.PlayerCurrentOverheal >=
                                                    ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTOverhealMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), ACTOverhealMulti_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTOverhealMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentOverheal, 0, ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0], ACTOverhealMulti_Collection.Length, 0);

                                                    for (int i = 0; i < ACTOverhealMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ACTOverhealMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTOverhealMulti_Collection[i]);
                                                    }
                                                }

                                                //Mouse
                                                var ACTOverhealMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                                var ACTOverhealMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                                if ((long)_ACTData.PlayerCurrentOverheal >=
                                                    ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTOverhealMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTOverhealMouseStrip_CollectionA[i], ACTOverhealMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTOverhealMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentOverheal, 0, ChromaticsSettings.ChromaticsSettingsACTOverheal[jobkey][0], ACTOverhealMouseStrip_CollectionA.Length, 0);

                                                    for (int i = 0; i < ACTOverhealMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTOverhealMouseStrip_CollectionA[i], ACTOverhealMouseStrip_CollectionB[i],  ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                            }

                                            //Damage Tracker
                                            if (_ACTMode == ACTMode.DamagePrc)
                                            {
                                                if ((long)_ACTData.PlayerCurrentDamage >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0])
                                                {
                                                    if (!threshFlash)
                                                    {
                                                        if (ChromaticsSettings.ChromaticsSettingsACTFlash)
                                                        {
                                                            _rzFl1Cts.Cancel();
                                                            _corsairF1Cts.Cancel();
                                                            GlobalFlash1(ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdFlash), 200, DeviceEffects.GlobalKeys3);
                                                        }

                                                        threshFlash = true;
                                                    }

                                                    GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                        (ushort)(long)65535,
                                                        250);

                                                    GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                    GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 1.0);
                                                    GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                        ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 1.0);
                                                }
                                                else
                                                {
                                                    threshFlash = false;

                                                    var polACTDamagez =
                                                        ((long)_ACTData.PlayerCurrentDamage - 0) * ((long)65535 - 0) / (ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0] - 0) + 0;
                                                    var polACTDamagez2 = ((long)_ACTData.PlayerCurrentDamage - 0) * (1.0 - 0.0) / (ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0] - 0) + 0.0;

                                                    if (polACTDamagez >= 0 && polACTDamagez <= 65535)
                                                    {
                                                        GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild),
                                                            (ushort) polACTDamagez,
                                                            250);
                                                    }

                                                    if (polACTDamagez2 >= 0.0 && polACTDamagez2 <= 1.0)
                                                    {
                                                        GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTDamagez2);
                                                        GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTDamagez2);
                                                        GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), false, polACTDamagez2);
                                                        GlobalApplyMapChromaLinkLightingBrightness(
                                                            DevModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdEmpty),
                                                            ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild), polACTDamagez2);
                                                    }
                                                }

                                                //Functions
                                                if (_FKeyMode == FKeyMode.ACTTracker)
                                                {
                                                    var FKACTDamage_Collection = DeviceEffects.Functions;

                                                    if ((long)_ACTData.PlayerCurrentDamage >= ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < FKACTDamage_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTDamage_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var FKACTDamage_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDamage, 0, ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0], FKACTDamage_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < FKACTDamage_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapKeyLighting(FKACTDamage_Collection[i2], FKACTDamage_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Lightbar
                                                if (_LightbarMode == LightbarMode.ACTTracker)
                                                {
                                                    var LBACTDamage_Collection = DeviceEffects.LightbarZones;

                                                    if ((long)_ACTData.PlayerCurrentDamage >= ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0])
                                                    {
                                                        for (var i2 = 0; i2 < LBACTDamage_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTDamage_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, false);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var LBACTDamage_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDamage, 0, ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0], LBACTDamage_Collection.Length, 0);

                                                        for (var i2 = 0; i2 < LBACTDamage_Collection.Length; i2++)
                                                        {
                                                            GlobalApplyMapLightbarLighting(LBACTDamage_Collection[i2], LBACTDamage_Interpolate > i2 ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                        }
                                                    }
                                                }

                                                //Mousepad
                                                var ACTDamageMousePadCollection = 5;

                                                if ((long)_ACTData.PlayerCurrentDamage >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDamageMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDamageMousePad_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDamage, 0, ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0], ACTDamageMousePadCollection, 0);

                                                    for (int i = 0; i < ACTDamageMousePadCollection; i++)
                                                    {
                                                        GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ACTDamageMousePad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                                    }
                                                }

                                                //Keypad
                                                var ACTDamageKeypad_Collection = DeviceEffects.Keypadzones;

                                                if ((long)_ACTData.PlayerCurrentDamage >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDamageKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess),
                                                            false, ACTDamageKeypad_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDamageKeypad_Interpolate =
                                                        Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDamage, 0, ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0],
                                                            ACTDamageKeypad_Collection.Length, 0);

                                                    for (int i = 0; i < ACTDamageKeypad_Collection.Length; i++)
                                                    {
                                                        GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,
                                                            ACTDamageKeypad_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                                .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                            false, ACTDamageKeypad_Collection[i]);
                                                    }
                                                }

                                                //MultiKeyboard
                                                var ACTDamageMulti_Collection = DeviceEffects.Multikeyzones;

                                                if ((long)_ACTData.PlayerCurrentDamage >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDamageMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), ACTDamageMulti_Collection[i]);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDamageMulti_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDamage, 0, ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0], ACTDamageMulti_Collection.Length, 0);

                                                    for (int i = 0; i < ACTDamageMulti_Collection.Length; i++)
                                                    {
                                                        GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ACTDamageMulti_Interpolate > i ? ColorTranslator.FromHtml(ColorMappings
                                                            .ColorMappingACTThresholdBuild) : ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTDamageMulti_Collection[i]);
                                                    }
                                                }

                                                //Mouse
                                                var ACTDamageMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                                var ACTDamageMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                                if ((long)_ACTData.PlayerCurrentDamage >=
                                                    ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0])
                                                {
                                                    for (int i = 0; i < ACTDamageMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTDamageMouseStrip_CollectionA[i], ACTDamageMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                                else
                                                {
                                                    var ACTDamageMouseStrip_Interpolate = Helpers.FFXIVInterpolation.Interpolate_Long((long)_ACTData.PlayerCurrentDamage, 0, ChromaticsSettings.ChromaticsSettingsACTDamage[jobkey][0], ACTDamageMouseStrip_CollectionA.Length, 0);

                                                    for (int i = 0; i < ACTDamageMouseStrip_CollectionA.Length; i++)
                                                    {
                                                        GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTDamageMouseStrip_CollectionA[i], ACTDamageMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false);
                                                    }
                                                }
                                            }

                                        }
                                        else
                                        {
                                            _rzFl1Cts.Cancel();
                                            _corsairF1Cts.Cancel();
                                            threshFlash = false;

                                            GlobalUpdateBulbStateBrightness(BulbModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty),
                                                (ushort)(long)65535,
                                                250);

                                            GlobalApplyKeySingleLightingBrightness(DevModeTypes.ACTTracker,
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 0.0);
                                            GlobalApplyMapMouseLightingBrightness(DevModeTypes.ACTTracker,
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 0.0);
                                            GlobalApplyMapHeadsetLightingBrightness(DevModeTypes.ACTTracker,
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), false, 0.0);
                                            GlobalApplyMapChromaLinkLightingBrightness(DevModeTypes.ACTTracker,
                                                ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdSuccess), 0.0);

                                            //Functions
                                            if (_FKeyMode == FKeyMode.ACTTracker)
                                            {
                                                var FKACTDPS_Collection = DeviceEffects.Functions;

                                                for (var i2 = 0; i2 < FKACTDPS_Collection.Length; i2++)
                                                {
                                                    if (_playerInfo.IsCasting)
                                                    {
                                                        break;
                                                    }

                                                    GlobalApplyMapKeyLighting(FKACTDPS_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                }
                                            }

                                            if (_FKeyMode == FKeyMode.ACTEnrage)
                                            {
                                                var FKACTEnrage_Collection = DeviceEffects.Functions;

                                                for (var i2 = 0; i2 < FKACTEnrage_Collection.Length; i2++)
                                                {
                                                    if (_playerInfo.IsCasting)
                                                    {
                                                        break;
                                                    }

                                                    GlobalApplyMapKeyLighting(FKACTEnrage_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTEnrageEmpty), false, false);
                                                }
                                            }

                                            //Lightbar
                                            if (_LightbarMode == LightbarMode.ACTTracker)
                                            {
                                                var LBACTDPS_Collection = DeviceEffects.LightbarZones;

                                                for (var i2 = 0; i2 < LBACTDPS_Collection.Length; i2++)
                                                {
                                                    GlobalApplyMapLightbarLighting(LBACTDPS_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, false);
                                                }
                                            }

                                            if (_LightbarMode == LightbarMode.ACTEnrage)
                                            {
                                                var LBACTEnrage_Collection = DeviceEffects.LightbarZones;

                                                for (var i2 = 0; i2 < LBACTEnrage_Collection.Length; i2++)
                                                {
                                                    GlobalApplyMapLightbarLighting(LBACTEnrage_Collection[i2], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTEnrageEmpty), false, false);
                                                }
                                            }

                                            //Mousepad
                                            var ACTDPSMousePadCollection = 5;

                                            for (int i = 0; i < ACTDPSMousePadCollection; i++)
                                            {
                                                GlobalApplyMapPadLighting(DevModeTypes.ACTTracker, 10 + i, 9 - i, 4 - i, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                            }

                                            //Keypad
                                            var ACTDPSKeypad_Collection = DeviceEffects.Keypadzones;

                                            for (int i = 0; i < ACTDPSKeypad_Collection.Length; i++)
                                            {
                                                GlobalApplyMapKeypadLighting(DevMultiModeTypes.ACTTracker,ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false, ACTDPSKeypad_Collection[i]);
                                            }

                                            //MultiKeyboard
                                            var ACTDPSMulti_Collection = DeviceEffects.Multikeyzones;

                                            for (int i = 0; i < ACTDPSMulti_Collection.Length; i++)
                                            {
                                                GlobalApplyKeyMultiLighting(DevMultiModeTypes.ACTTracker, ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), ACTDPSMulti_Collection[i]);
                                            }

                                            //Mouse
                                            var ACTDPSMouseStrip_CollectionA = DeviceEffects.MouseStripsLeft;
                                            var ACTDPSMouseStrip_CollectionB = DeviceEffects.MouseStripsRight;

                                            for (int i = 0; i < ACTDPSMouseStrip_CollectionA.Length; i++)
                                            {
                                                GlobalApplyStripMouseLighting(DevModeTypes.ACTTracker, ACTDPSMouseStrip_CollectionA[i], ACTDPSMouseStrip_CollectionB[i], ColorTranslator.FromHtml(ColorMappings.ColorMappingACTThresholdEmpty), false);
                                            }
                                        }
                                    }
                                }
                            }

                            //Duty Finder Bell
                            if (FfxivDutyFinder.IsPopped())
                            {
                                if (!_dfpopOnce)
                                {
                                    if (ChromaticsSettings.ChromaticsSettingsCastEnabled && ChromaticsSettings.ChromaticsSettingsCastDFBell)
                                    {
                                        SharpcastController.CastMedia("dfpop_notify.mp3");
                                    }

                                    if (ChromaticsSettings.ChromaticsSettingsIFTTTEnable)
                                    {
                                        IFTTTController.FireIFTTTEvent(@"Chromatics_DFBell", ChromaticsSettings.ChromaticsSettingsIFTTTURL);
                                    }

                                    _dfpopOnce = true;
                                }

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
                                    _dfpopOnce = false;

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
 