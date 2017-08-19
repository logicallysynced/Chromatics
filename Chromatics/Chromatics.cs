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
using Chromatics.LCDInterfaces;
using Gma.System.MouseKeyHook;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Timer = System.Timers.Timer;

namespace Chromatics
{
    public partial class Chromatics : Form, ILogWrite
    {
        //Setup Threading/Tasks
        private readonly CancellationTokenSource MemoryTask = new CancellationTokenSource();

        private ILogitechArx _arx;
        private Task _call;
        private Task _MemoryTask;
        public int _PaletteMappingCurrentSelect = 1;
        private bool allowClose;
        private bool allowVisible = true;

        public bool ArxSDK;
        public int ArxSDKCalled;
        public int ArxState;
        public bool ArxToggle = true;

        private CancellationTokenSource Attachcts = new CancellationTokenSource();

        //Setup Variables
        public int attatched;

        public ChromaticsSettings ChromaticsSettings = new ChromaticsSettings();

        public FFXIVColorMappings ColorMappings = new FFXIVColorMappings();

        private bool CoolermasterDeviceKeyboard = true;
        private bool CoolermasterDeviceMouse = true;
        public bool CoolermasterSDK = false;
        public int CoolermasterSDKCalled = 0;

        private bool CorsairDeviceHeadset = true;
        private bool CorsairDeviceKeyboard = true;
        private bool CorsairDeviceKeypad = true;
        private bool CorsairDeviceMouse = true;
        private bool CorsairDeviceMousepad = true;

        public bool CorsairRescan = false;
        public bool CorsairSDK = false;
        public int CorsairSDKCalled = 0;
        private readonly string currentVersionX = "2.2.7";
        public bool DeviceGridStartup = false;

        public bool effectRunning = false;
        private CancellationTokenSource FFXIVcts = new CancellationTokenSource();

        private bool GameNotify;
        private Timer GameResetCatch;
        public bool HoldReader = false;

        private string HUEDefault = "";
        public bool HueRescan = false;
        public bool HueSDK = false;
        public int HueSDKCalled = 0;
        public bool init = false;
        public bool isDX11 = false;
        private bool KeyAlt;

        private bool KeyCtrl;
        private bool KeyShift;
        private string LgsInstall = "";
        public bool LifxRescan = false;
        public bool LifxSDK = false;
        public int LifxSDKCalled = 0;

        private bool LogitechDeviceKeyboard = true;
        public bool LogitechRescan = false;
        public bool LogitechSDK = false;
        public int LogitechSDKCalled = 0;
        private IKeyboardMouseEvents m_GlobalHook;
        public bool MappingGridStartup = false;
        public int MouseToggle = 0;
        public int PaletteMappingCurrentSelect = 0;

        public List<string> plugs = new List<string>();
        private bool RazerDeviceHeadset = true;

        private bool RazerDeviceKeyboard = true;
        private bool RazerDeviceKeypad = true;
        private bool RazerDeviceMouse = true;
        private bool RazerDeviceMousepad = true;
        public bool RazerRescan = false;
        public bool RazerSDK = false;
        public int RazerSDKCalled = 0;

        private readonly RegistryKey rkApp =
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private bool RoccatDeviceKeyboard = true;
        private bool RoccatDeviceMouse = true;
        public bool RoccatSDK = false;
        public int RoccatSDKCalled = 0;
        public bool Setbase;
        public bool startup;
        public int state = 0;

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
                    if (type == ConsoleTypes.SYSTEM)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Black; });
                    else if (type == ConsoleTypes.FFXIV)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DarkCyan; });
                    else if (type == ConsoleTypes.RAZER)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.LimeGreen; });
                    else if (type == ConsoleTypes.CORSAIR)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.MediumVioletRed; });
                    else if (type == ConsoleTypes.LOGITECH)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DodgerBlue; });
                    else if (type == ConsoleTypes.LIFX)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.BlueViolet; });
                    else if (type == ConsoleTypes.HUE)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Orange; });
                    else if (type == ConsoleTypes.ARX)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Aqua; });
                    else if (type == ConsoleTypes.STEEL)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.HotPink; });
                    else if (type == ConsoleTypes.COOLERMASTER)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.DarkBlue; });
                    else if (type == ConsoleTypes.ROCCAT)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.RosyBrown; });
                    else if (type == ConsoleTypes.ERROR)
                        rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Red; });
                    else rtb_debug.Invoke((Action) delegate { rtb_debug.SelectionColor = Color.Black; });

                    rtb_debug.Invoke((Action) delegate { rtb_debug.AppendText(line + Environment.NewLine); });
                }
                else
                {
                    if (type == ConsoleTypes.SYSTEM) rtb_debug.SelectionColor = Color.Black;
                    else if (type == ConsoleTypes.FFXIV) rtb_debug.SelectionColor = Color.DarkCyan;
                    else if (type == ConsoleTypes.RAZER) rtb_debug.SelectionColor = Color.LimeGreen;
                    else if (type == ConsoleTypes.CORSAIR) rtb_debug.SelectionColor = Color.MediumVioletRed;
                    else if (type == ConsoleTypes.LOGITECH) rtb_debug.SelectionColor = Color.DodgerBlue;
                    else if (type == ConsoleTypes.LIFX) rtb_debug.SelectionColor = Color.BlueViolet;
                    else if (type == ConsoleTypes.HUE) rtb_debug.SelectionColor = Color.Orange;
                    else if (type == ConsoleTypes.ARX) rtb_debug.SelectionColor = Color.Aqua;
                    else if (type == ConsoleTypes.STEEL) rtb_debug.SelectionColor = Color.HotPink;
                    else if (type == ConsoleTypes.COOLERMASTER) rtb_debug.SelectionColor = Color.DarkBlue;
                    else if (type == ConsoleTypes.ROCCAT) rtb_debug.SelectionColor = Color.RosyBrown;
                    else if (type == ConsoleTypes.ERROR) rtb_debug.SelectionColor = Color.Red;
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
            Watchdog.WatchdogStartup();
            Text = "Chromatics " + currentVersionX + " Beta";

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

            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += Kh_KeyDown;
            m_GlobalHook.KeyUp += Kh_KeyUp;

            GameResetCatch = new Timer();
            GameResetCatch.Elapsed += (source, e) => { FFXIVGameStop(); };
            GameResetCatch.Interval = 12000;
            GameResetCatch.AutoReset = false;
            GameResetCatch.Enabled = false;

            try
            {
                var LgsApp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Logitech\\Logitech Gaming Software", false);
                LgsInstall = LgsApp.GetValue("InstallDir").ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            //Bind
            WriteConsole(ConsoleTypes.SYSTEM, "Starting Chromatics Version " + currentVersionX + " (Beta)");


            //Load Functions
            LoadDevices();
            LoadChromaticsSettings();
            LoadColorMappings();


            //Check Administrator permissions
            if (!IsAdministrator())
            {
                WriteConsole(ConsoleTypes.ERROR,
                    "Chromatics is not running as Administrator. Please restart with administrative privileges.");

                if (chk_lccauto.Checked)
                    tooltip_main.SetToolTip(gB_lcc,
                        "Logitech Conflict Mode requires Chromatics to be run with Administrative privileges. Please restart with administrative privileges.");

                gB_lcc.Enabled = false;
            }

            //Check Updater
            try
            {
                var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                if (!File.Exists(enviroment + @"/updater.exe"))
                {
                    if (File.Exists(enviroment + @"/_updater.exe"))
                    {
                        FileSystem.RenameFile(enviroment + @"/_updater.exe", "updater.exe");
                        WriteConsole(ConsoleTypes.SYSTEM, "Updated Chromatics Updater to latest version.");
                    }
                }
                else
                {
                    if (File.Exists(enviroment + @"/_updater.exe"))
                    {
                        File.Delete(enviroment + @"/updater.exe");
                        FileSystem.RenameFile(enviroment + @"/_updater.exe", "updater.exe");
                        WriteConsole(ConsoleTypes.SYSTEM, "Updated Chromatics Updater to latest version.");
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
            InitSettingsGUI();
            SetupTooltips();
            CenterPictureBox(pB_logo1, pB_logo1.Image);
            notify_master.ContextMenuStrip = contextMenuStrip1;
            //mapping_colorEditorManager.Color = Color.White;

            notify_master.BalloonTipText = @"Chromatics will automatically attach to Final Fantasy XIV";
            notify_master.ShowBalloonTip(2000);

            new Task(() => { CheckUpdates(0); }).Start();

            //Setup Device Interfaces
            InitializeSDK();

            if (LogitechSDKCalled == 1)
            {
                if (gB_lcc.Enabled)
                {
                    //Check Logitech Enviroment
                    try
                    {
                        if (File.Exists(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                        {
                            if (chk_lccauto.Checked)
                            {
                                ToggleLCCMode(true);

                                chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                                chk_lccenable.Checked = true;
                                chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                            }
                            else
                            {
                                WriteConsole(ConsoleTypes.ERROR,
                                    "Logitech: Chromatics has detected that the LGS internal SDK library is causing a conflict between FFXIV and Chromatics. Please make sure to enable 'Logitech Conflict Mode' under the settings tab and check that 'LED Illumination' is disabled for 'ffxiv_dx11' within LGS.");
                            }
                        }
                        else
                        {
                            WriteConsole(ConsoleTypes.LOGITECH, "Logitech Conflict Mode is already enabled.");

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
                            WriteConsole(ConsoleTypes.ERROR,
                                "Logitech Conflict Mode failed to automatically start. Error: " + ex.Message);

                            chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                            chk_lccenable.Checked = false;
                            chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                        }
                    }
                }
                else
                {
                    if (File.Exists(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                        WriteConsole(ConsoleTypes.ERROR,
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

            if (chk_arxtoggle.Checked)
            {
                _arx = LogitechArxInterface.InitializeArxSDK();

                if (_arx != null)
                {
                    ArxSDK = true;
                    ArxState = 0;
                    ArxSDKCalled = 1;
                    WriteConsole(ConsoleTypes.ARX, "ARX SDK Loaded");

                    //Load Plugins
                    LoadArxPlugins();
                }
            }

            //Finish GUI Setup
            InitSettingsArxGUI();
            startup = true;

            //Split off MemoryReader to a Task 
            _MemoryTask = new Task(() =>
            {
                var _call = CallFFXIVAttach(Attachcts.Token);
            }, MemoryTask.Token);

            MemoryTasks.Add(_MemoryTask);
            MemoryTasks.Run(_MemoryTask);
        }

        private void LoadArxPlugins()
        {
            if (ArxSDK && ArxSDKCalled == 1)
            {
                //Load Plugins
                if (plugs.Count > 0)
                {
                    foreach (var plugin in plugs)
                        if (cb_arx_mode.Items.Contains(plugin))
                            cb_arx_mode.Items.Remove(plugin);

                    plugs.Clear();
                }

                plugs = _arx.LoadPlugins();
                if (plugs.Count > 0)
                    foreach (var plug in plugs)
                    {
                        cb_arx_mode.Items.Add(plug);
                        WriteConsole(ConsoleTypes.ARX, plug + " Plugin Loaded.");
                    }
                else
                    WriteConsole(ConsoleTypes.ARX, "No Plugins Found.");
            }
        }

        private async Task CallFFXIVAttach(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                //Console.Write("Debug E");
                await Task.Delay(2500);
                AttachFFXIV();
            }
        }

        private void AttachFFXIV()
        {
            if (InitiateMemory())
            {
                //Console.WriteLine("Attached");
                //rtb_debug.Invoke((Action)delegate { rtb_debug.AppendText("Attached" + Environment.NewLine); });
                WriteConsole(ConsoleTypes.FFXIV, "Attached to FFXIV");
                GameNotify = false;
                notify_master.Text = @"Attached to FFXIV";
                notify_master.BalloonTipText = @"Attached to FFXIV";
                notify_master.ShowBalloonTip(1500);

                if (ArxSDKCalled == 1 && ArxState == 0)
                    _arx.ArxUpdateInfo("Attached to FFXIV");

                attatched = 1;

                FFXIVcts = new CancellationTokenSource();

                _call = CallFFXIVMemory(FFXIVcts.Token); //put into local variable so it doesn't complain, lol
                Attachcts.Cancel();
            }
            else
            {
                if (!GameNotify)
                {
                    WriteConsole(ConsoleTypes.SYSTEM, "Waiting for Game Launch..");
                    notify_master.Text = @"Waiting for Game Launch..";
                    if (ArxSDKCalled == 1 && ArxState == 0)
                        _arx.ArxUpdateInfo("Waiting for Game Launch..");

                    GameNotify = true;
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
                Watchdog.WatchdogGo();
                var _call = CallFFXIVMemory(FFXIVcts.Token);
            }
        }

        private void CheckUpdates(int notify)
        {
            try
            {
                var currentVersion = currentVersionX.Replace(".", string.Empty); //UPDATE ME
                var newVersion = currentVersion;
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
                    var result = MessageBox.Show(@"There is an updated version of Chromatics. Update it now?",
                        @"New Version", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        var updatedFile = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                        var startInfo = new ProcessStartInfo();
                        startInfo.FileName = updatedFile + @"\updater.exe";
                        startInfo.Arguments = "\"" + updatedFile + "\"";

                        if (LogitechSDKCalled == 1)
                            ToggleLCCMode(false, true);

                        Process.Start(startInfo);
                        Environment.Exit(1);
                    }
                }
                else
                {
                    if (notify == 1)
                    {
                        notify_master.BalloonTipText = @"No new updates currently available.";
                        notify_master.ShowBalloonTip(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Unable to check for updates (Error: " + ex.Message + @").");
                if (notify == 1)
                {
                    notify_master.BalloonTipText = @"Unable to check for updates (Error: " + ex.Message + @").";
                    notify_master.ShowBalloonTip(2000);
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

        void FFXIVGameStop();
    }
}