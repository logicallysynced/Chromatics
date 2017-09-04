using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromatics.Datastore;
using Chromatics.DeviceInterfaces;
using Chromatics.LCDInterfaces;
using Gma.System.MouseKeyHook;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;

namespace Chromatics
{
    public partial class Chromatics : Form, ILogWrite
    {
        //Setup Threading/Tasks
        private CancellationTokenSource _memoryTask = new CancellationTokenSource();

        private ILogitechArx _arx;
        private ILogitechLcd _lcd;
        private Task _call;
        public Task MemoryTask;
        private bool _allowClose;
        private bool _allowVisible = true;

        public bool ArxSdk;
        public int ArxSdkCalled;
        public int ArxState;
        public bool ArxToggle = true;

        public bool LcdSdk;
        public int LcdSdkCalled;

        private CancellationTokenSource _attachcts = new CancellationTokenSource();

        //Setup Variables
        public int Attatched;

        public ChromaticsSettings ChromaticsSettings = new ChromaticsSettings();

        public FfxivColorMappings ColorMappings = new FfxivColorMappings();

        private bool _coolermasterDeviceKeyboard = true;
        private bool _coolermasterDeviceMouse = true;
        public bool CoolermasterSdk = false;
        public int CoolermasterSdkCalled = 0;

        private bool _corsairDeviceHeadset = true;
        private bool _corsairDeviceKeyboard = true;
        private bool _corsairDeviceKeypad = true;
        private bool _corsairDeviceMouse = true;
        private bool _corsairDeviceMousepad = true;

        public bool CorsairRescan = false;
        public bool CorsairSdk = false;
        public int CorsairSdkCalled = 0;
        private readonly string _currentVersionX = "2.2.9";
        public bool DeviceGridStartup = false;

        public bool EffectRunning = false;
        private CancellationTokenSource _ffxiVcts = new CancellationTokenSource();

        private bool _gameNotify;
        //private Timer _gameResetCatch;
        public bool HoldReader = false;

        private string _hueDefault = "";
        public bool HueRescan = false;
        public bool HueSdk = false;
        public int HueSdkCalled = 0;
        public bool Init = false;
        public bool IsDx11 = false;
        private bool _keyAlt;

        private bool _keyCtrl;
        private bool _keyShift;
        private string _lgsInstall = "";
        public bool LifxRescan = false;
        public bool LifxSdk = false;
        public int LifxSdkCalled = 0;

        private bool _logitechDeviceKeyboard = true;
        public bool LogitechRescan = false;
        public bool LogitechSdk = false;
        public int LogitechSdkCalled = 0;
        private IKeyboardMouseEvents _mGlobalHook;
        public bool MappingGridStartup = false;
        public int MouseToggle = 0;
        public int PaletteMappingCurrentSelect = 0;
        public bool LCCStatus = false;

        public List<string> Plugs = new List<string>();
        private bool _razerDeviceHeadset = true;

        private bool _razerDeviceKeyboard = true;
        private bool _razerDeviceKeypad = true;
        private bool _razerDeviceMouse = true;
        private bool _razerDeviceMousepad = true;
        private bool _razerDeviceChromaLink = true;
        public bool RazerRescan = false;
        public bool RazerSdk = false;
        public int RazerSdkCalled = 0;

        private readonly RegistryKey _rkApp =
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private bool _roccatDeviceKeyboard = true;
        private bool _roccatDeviceMouse = true;
        public bool RoccatSdk = false;
        public int RoccatSdkCalled = 0;
        public bool SetKeysbase;
        public bool SetMousebase;
        public bool SetPadbase;
        public bool SetHeadsetbase;
        public bool SetKeypadbase;
        public bool SetCLbase;
        public bool Startup;
        public int State = 0;

        private bool _deviceKeyboard = true;
        private bool _deviceMouse = true;
        private bool _deviceMousepad = true;
        private bool _deviceHeadset = true;
        private bool _deviceKeypad = true;
        private bool _deviceCL = true;
        private bool _KeysSingleKeyModeEnabled;
        private DevModeTypes _KeysSingleKeyMode = DevModeTypes.Disabled;

        public DevModeTypes _MouseZone1Mode = DevModeTypes.DefaultColor;
        public DevModeTypes _MouseZone2Mode = DevModeTypes.EnmityTracker;
        public DevModeTypes _MouseZone3Mode = DevModeTypes.DefaultColor;
        public DevModeTypes _MouseStrip1Mode = DevModeTypes.HpTracker;
        public DevModeTypes _MouseStrip2Mode = DevModeTypes.MpTracker;

        public DevModeTypes _PadZone1Mode = DevModeTypes.HpTracker;
        public DevModeTypes _PadZone2Mode = DevModeTypes.TpTracker;
        public DevModeTypes _PadZone3Mode = DevModeTypes.MpTracker;

        public DevModeTypes _HeadsetZone1Mode = DevModeTypes.DefaultColor;
        public DevModeTypes _KeypadZone1Mode = DevModeTypes.DefaultColor;

        public DevModeTypes _CLZone1Mode = DevModeTypes.DefaultColor;
        public DevModeTypes _CLZone2Mode = DevModeTypes.DefaultColor;
        public DevModeTypes _CLZone3Mode = DevModeTypes.DefaultColor;
        public DevModeTypes _CLZone4Mode = DevModeTypes.DefaultColor;
        public DevModeTypes _CLZone5Mode = DevModeTypes.DefaultColor;

        //Main Thread
        public Chromatics()
        {
            InitializeComponent();
        }

        public void WriteConsole(ConsoleTypes type, string line)
        {
            try
            {
                //Debug.WriteLine(line);

                if (InvokeRequired)
                {
                    if (type == ConsoleTypes.System)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Black; });
                    else if (type == ConsoleTypes.Ffxiv)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DarkCyan; });
                    else if (type == ConsoleTypes.Razer)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.LimeGreen; });
                    else if (type == ConsoleTypes.Corsair)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.MediumVioletRed; });
                    else if (type == ConsoleTypes.Logitech)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DodgerBlue; });
                    else if (type == ConsoleTypes.Lifx)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.BlueViolet; });
                    else if (type == ConsoleTypes.Hue)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Orange; });
                    else if (type == ConsoleTypes.Arx)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Aqua; });
                    else if (type == ConsoleTypes.Steel)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.HotPink; });
                    else if (type == ConsoleTypes.Coolermaster)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DarkBlue; });
                    else if (type == ConsoleTypes.Roccat)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.RosyBrown; });
                    else if (type == ConsoleTypes.Error)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Red; });
                    else rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Black; });

                    rtb_debug.Invoke((Action) delegate { rtb_debug.AppendText(line + Environment.NewLine); });
                }
                else
                {
                    if (type == ConsoleTypes.System) rtb_debug.SelectionColor = Color.Black;
                    else if (type == ConsoleTypes.Ffxiv) rtb_debug.SelectionColor = Color.DarkCyan;
                    else if (type == ConsoleTypes.Razer) rtb_debug.SelectionColor = Color.LimeGreen;
                    else if (type == ConsoleTypes.Corsair) rtb_debug.SelectionColor = Color.MediumVioletRed;
                    else if (type == ConsoleTypes.Logitech) rtb_debug.SelectionColor = Color.DodgerBlue;
                    else if (type == ConsoleTypes.Lifx) rtb_debug.SelectionColor = Color.BlueViolet;
                    else if (type == ConsoleTypes.Hue) rtb_debug.SelectionColor = Color.Orange;
                    else if (type == ConsoleTypes.Arx) rtb_debug.SelectionColor = Color.Aqua;
                    else if (type == ConsoleTypes.Steel) rtb_debug.SelectionColor = Color.HotPink;
                    else if (type == ConsoleTypes.Coolermaster) rtb_debug.SelectionColor = Color.DarkBlue;
                    else if (type == ConsoleTypes.Roccat) rtb_debug.SelectionColor = Color.RosyBrown;
                    else if (type == ConsoleTypes.Error) rtb_debug.SelectionColor = Color.Red;
                    else rtb_debug.SelectionColor = Color.Black;

                    rtb_debug.AppendText(line + Environment.NewLine);
                    rtb_debug.SelectionStart = rtb_debug.Text.Length;
                    rtb_debug.ScrollToCaret();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MainThread();
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void MainThread()
        {
            //Setup References
            //Watchdog.WatchdogStartup();
            Text = @"Chromatics " + _currentVersionX + @" Beta";

            //Setup Event Listeners
            FormClosing += OnFormClosing;
            Resize += ChromaticsForm_Resize;
            Application.ApplicationExit += OnApplicationExit;
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            mi_effectsenable.Click += enableeffects_Click;
            mi_arxenable.Click += enablearx_Click;
            mi_showwindow.Click += showwindow_Click;
            mi_winstart.Click += mi_winstart_Click;
            mi_updatecheck.Click += mi_updatecheck_Click;

            _mGlobalHook = Hook.GlobalEvents();
            _mGlobalHook.KeyDown += Kh_KeyDown;
            _mGlobalHook.KeyUp += Kh_KeyUp;

            /*
            _gameResetCatch = new Timer();
            _gameResetCatch.Elapsed += (source, e) => { FfxivGameStop(); };
            _gameResetCatch.Interval = 12000;
            _gameResetCatch.AutoReset = false;
            _gameResetCatch.Enabled = false;
            */

            try
            {
                var lgsApp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Logitech\\Logitech Gaming Software", false);
                if (lgsApp != null) _lgsInstall = lgsApp.GetValue("InstallDir").ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            //Bind
            WriteConsole(ConsoleTypes.System, "Starting Chromatics Version " + _currentVersionX + " (Beta)");


            //Load Functions
            LoadDevices();
            LoadChromaticsSettings();
            LoadColorMappings();


            //Check Administrator permissions
            if (!IsAdministrator())
            {
                WriteConsole(ConsoleTypes.Error,
                    "Chromatics is not running as Administrator. Please restart with administrative privileges.");

                if (chk_lccauto.Checked)
                    tooltip_main.SetToolTip(gB_lcc,
                        "Logitech Conflict Mode requires Chromatics to be run with Administrative privileges. Please restart with administrative privileges.");

                gB_lcc.Enabled = false;
            }

            //Check Updater
            try
            {
                string enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                if (!File.Exists(enviroment + @"/updater.exe"))
                {
                    if (File.Exists(enviroment + @"/_updater.exe"))
                    {
                        FileSystem.RenameFile(enviroment + @"/_updater.exe", "updater.exe");
                        WriteConsole(ConsoleTypes.System, "Updated Chromatics Updater to latest version.");
                    }
                }
                else
                {
                    if (File.Exists(enviroment + @"/_updater.exe"))
                    {
                        File.Delete(enviroment + @"/updater.exe");
                        FileSystem.RenameFile(enviroment + @"/_updater.exe", "updater.exe");
                        WriteConsole(ConsoleTypes.System, "Updated Chromatics Updater to latest version.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            //Setup GUI
            InitDeviceDataGrid();
            InitColorMappingGrid();
            InitSettingsGui();
            SetupTooltips();
            CenterPictureBox(pB_logo1, pB_logo1.Image);
            notify_master.ContextMenuStrip = contextMenuStrip1;
            //mapping_colorEditorManager.Color = Color.White;

            if (ChromaticsSettings.ChromaticsSettingsDesktopNotifications)
            {
                notify_master.BalloonTipText = @"Chromatics will automatically attach to Final Fantasy XIV";
                notify_master.ShowBalloonTip(2000);
            }

            new Task(() => { CheckUpdates(0); }).Start();

            //Setup Device Interfaces
            InitializeSdk();
            InitDevicesGui();

            if (LogitechSdkCalled == 1)
            {
                if (gB_lcc.Enabled)
                {
                    //Check Logitech Enviroment
                    try
                    {
                        if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                        {
                            if (chk_lccauto.Checked)
                            {
                                ToggleLccMode(true);

                                chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                                chk_lccenable.Checked = true;
                                chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                            }
                            else
                            {
                                WriteConsole(ConsoleTypes.Error,
                                    "Logitech: Chromatics has detected that the LGS internal SDK library is causing a conflict between FFXIV and Chromatics. Please make sure to enable 'Logitech Conflict Mode' under the settings tab and check that 'LED Illumination' is disabled for 'ffxiv_dx11' within LGS.");
                            }
                        }
                        else
                        {
                            WriteConsole(ConsoleTypes.Logitech, "Logitech Conflict Mode is already enabled.");

                            chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                            chk_lccenable.Checked = true;
                            chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);

                        if (chk_lccauto.Checked)
                        {
                            WriteConsole(ConsoleTypes.Error,
                                "Logitech Conflict Mode failed to automatically start. Error: " + ex.Message);

                            chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                            chk_lccenable.Checked = false;
                            chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                        }
                    }
                }
                else
                {
                    if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                        WriteConsole(ConsoleTypes.Error,
                            "Logitech: Chromatics has detected that the LGS internal SDK library is causing a conflict between FFXIV and Chromatics. Please make sure to enable 'Logitech Conflict Mode' under the settings tab and check that 'LED Illumination' is disabled for 'ffxiv_dx11' within LGS.");
                }
            }
            else
            {
                tooltip_main.SetToolTip(gB_lcc,
                    "Logitech SDK not loaded. Please open LGS Software and restart Chromatics as Administrator.");
                gB_lcc.Enabled = false;
            }


            //Setup LCD Interfaces

            if (ChromaticsSettings.ChromaticsSettingsLcdEnabled)
            {
                _lcd = LogitechLcdInterface.InitializeLcdSdk();

                if (_lcd != null)
                {
                    LcdSdk = true;
                    LcdSdkCalled = 1;
                    WriteConsole(ConsoleTypes.Logitech, "LCD SDK Loaded");
                }
            }


            if (chk_arxtoggle.Checked)
            {
                _arx = LogitechArxInterface.InitializeArxSdk();

                if (_arx != null)
                {
                    ArxSdk = true;
                    ArxState = 0;
                    ArxSdkCalled = 1;
                    WriteConsole(ConsoleTypes.Arx, "ARX SDK Loaded");

                    //Load Plugins
                    LoadArxPlugins();
                }
            }

            
            //Finish GUI Setup
            InitSettingsArxGui();
            Startup = true;

            //Ambience
            /*
            if (IsAdministrator())
                AmbienceInterface.StartAmbience();
            */

            //Split off MemoryReader to a Task 
            MemoryTask = new Task(() =>
            {
                _call = CallFfxivAttach(_attachcts.Token);
            }, _memoryTask.Token);

            MemoryTasks.Add(MemoryTask);
            MemoryTasks.Run(MemoryTask);
        }

        private void LoadArxPlugins()
        {
            if (ArxSdk && ArxSdkCalled == 1)
            {
                //Load Plugins
                if (Plugs.Count > 0)
                {
                    foreach (var plugin in Plugs)
                        if (cb_arx_mode.Items.Contains(plugin))
                            cb_arx_mode.Items.Remove(plugin);

                    Plugs.Clear();
                }

                Plugs = _arx.LoadPlugins();
                if (Plugs.Count > 0)
                    foreach (var plug in Plugs)
                    {
                        cb_arx_mode.Items.Add(plug);
                        WriteConsole(ConsoleTypes.Arx, plug + " Plugin Loaded.");
                    }
                else
                    WriteConsole(ConsoleTypes.Arx, "No Plugins Found.");
            }
        }

        private async Task CallFfxivAttach(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(2500, ct);
                AttachFfxiv();
            }
        }

        private void AttachFfxiv()
        {
            //Debug.WriteLine("Debug trip B");

            if (InitiateMemory())
            {
                //Console.WriteLine("Attached");
                //rtb_debug.Invoke((Action)delegate { rtb_debug.AppendText("Attached" + Environment.NewLine); });
                WriteConsole(ConsoleTypes.Ffxiv, "Attached to FFXIV");
                _gameNotify = false;
                notify_master.Text = @"Attached to FFXIV";

                if (ChromaticsSettings.ChromaticsSettingsDesktopNotifications)
                {
                    notify_master.BalloonTipText = @"Attached to FFXIV";
                    notify_master.ShowBalloonTip(1500);
                }

                if (ArxSdkCalled == 1 && ArxState == 0)
                    _arx.ArxUpdateInfo("Attached to FFXIV");

                if (LcdSdkCalled == 1)
                {
                    _lcd.StatusLCDInfo(@"Attached to FFXIV");
                }

                Attatched = 1;
                
                _attachcts.Cancel();
                
                MemoryTasks.Remove(MemoryTask);
                MemoryTask = null;
                _memoryTask = new CancellationTokenSource();
                _ffxiVcts = new CancellationTokenSource();

                //_call = CallFfxivMemory(_ffxiVcts.Token);

                MemoryTask = new Task(() =>
                {
                    _call = CallFfxivMemory(_ffxiVcts.Token);
                }, _memoryTask.Token, TaskCreationOptions.LongRunning);

                MemoryTasks.Add(MemoryTask);
                MemoryTasks.Run(MemoryTask);
                
                
            }
            else
            {
                if (!_gameNotify)
                {
                    WriteConsole(ConsoleTypes.System, "Waiting for Game Launch..");
                    notify_master.Text = @"Waiting for Game Launch..";

                    if (LcdSdkCalled == 1)
                    {
                        _lcd.StatusLCDInfo(@"Waiting for Game Launch..");
                    }

                    if (ArxSdkCalled == 1 && ArxState == 0)
                        _arx.ArxUpdateInfo("Waiting for Game Launch..");

                    _gameNotify = true;
                }
            }
        }

        public void RestartServices()
        {
            if (InvokeRequired)
            {
                BlinkDelegate del = RestartServices;
                Invoke(del);
            }
            else
            {
                //Watchdog.WatchdogGo();
                _call = CallFfxivMemory(_ffxiVcts.Token);
            }
        }

        private void CheckUpdates(int notify)
        {
            try
            {
                var currentVersion = _currentVersionX.Replace(".", string.Empty); //UPDATE ME
                string newVersion;
                var webRequest = WebRequest.Create(@"https://chromaticsffxiv.com/chromatics2/update/version.txt");

                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    var newVersionA = reader.ReadToEnd();
                    newVersion = newVersionA.Replace(".", string.Empty);
                }

                var cV = int.Parse(currentVersion);
                var nV = int.Parse(newVersion);

                if (nV > cV)
                {
                    WriteConsole(ConsoleTypes.System, "There is an updated version of Chromatics available.");
                    var result = MessageBox.Show(@"There is an updated version of Chromatics. Update it now?",
                        @"New Version", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        var updatedFile = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = updatedFile + @"\updater.exe",
                            Arguments = "\"" + updatedFile + "\""
                        };

                        if (LogitechSdkCalled == 1)
                            ToggleLccMode(false, true);

                        Process.Start(startInfo);
                        Environment.Exit(1);
                    }
                }
                else
                {
                    WriteConsole(ConsoleTypes.System, "No new updates currently available.");

                    if (notify != 1) return;
                    if (ChromaticsSettings.ChromaticsSettingsDesktopNotifications)
                    {
                        notify_master.BalloonTipText = @"No new updates currently available.";
                        notify_master.ShowBalloonTip(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, @"Unable to check for updates (Error: " + ex.Message + @").");
                if (notify == 1)
                {
                    if (ChromaticsSettings.ChromaticsSettingsDesktopNotifications)
                    {
                        notify_master.BalloonTipText = @"Unable to check for updates (Error: " + ex.Message + @").";
                        notify_master.ShowBalloonTip(2000);
                    }
                }
            }
        }

        private delegate void BlinkDelegate();

        
    }

    public static class ExceptionExtensions
    {
        public static Exception GetOriginalException(this Exception ex)
        {
            if (ex.InnerException == null) return ex;

            return ex.InnerException.GetOriginalException();
        }
    }

    /// <summary>
    ///     Interface used for writing to the Console or debug output in application.
    ///     Additionally accesses GUI functions for LIFX methods.
    /// </summary>
    internal interface ILogWrite
    {
        /// <summary>
        ///     Writes to the console a line of the given type.
        /// </summary>
        void WriteConsole(ConsoleTypes type, string line);

        /// <summary>
        ///     Access the Save Devices method for LIFX functions.
        /// </summary>
        void SaveDevices();

        /// <summary>
        ///     Refresh the Device tab for LIFX functions.
        /// </summary>
        void ResetDeviceDataGrid();

        void FfxivGameStop();
    }
}