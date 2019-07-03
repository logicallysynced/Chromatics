// reference:Newtonsoft.Json.dll

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace ChromaticsACT
{
    public partial class ChromaticsACTPlugin : UserControl, IActPluginV1
    {
        private EncounterData ActiveEnconter;
        private Label lblStatus;
        private bool inCombat;
        private string enviroment;
        private ACTDataTemplate dataStruct;
        private List<string> customTriggers = new List<string>();

        public ChromaticsACTPlugin()
        {
            InitializeComponent();
        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            lblStatus = pluginStatusText;


            var pluginData = ActGlobals.oFormActMain.PluginGetSelfData(this);
            enviroment = Path.GetDirectoryName(pluginData.pluginFile.ToString()) + @"\";
            Debug.WriteLine(@"Startup: " + enviroment);

            if (!LoadAssemblies()) return;

            dataStruct = new ACTDataTemplate();

            pluginScreenSpace.Controls.Add(this);
            this.Dock = DockStyle.Fill;
            pluginScreenSpace.Text = "Chromatics";

            //pluginScreenSpace.Dispose();

            //Events
            ActGlobals.oFormActMain.BeforeLogLineRead += OnBeforeLogLineRead;
            ActGlobals.oFormActMain.OnCombatStart += oFormActMain_OnCombatStart;
            ActGlobals.oFormActMain.OnCombatEnd += oFormActMain_OnCombatEnd;
            ActGlobals.oFormSpellTimers.OnSpellTimerExpire += oFormSpellTimers_OnSpellTimerExpire;
            ActGlobals.oFormSpellTimers.OnSpellTimerWarning += oFormSpellTimers_OnSpellTimerWarning;
            ActGlobals.oFormSpellTimers.OnSpellTimerRemoved += oFormSpellTimers_OnSpellTimerRemoved;
            
            //Custom Triggers
            var cTriggers = ActGlobals.oFormActMain.ActiveCustomTriggers;

            //Fetch custom triggers from ACT and store in array
            foreach (var cTrigger in cTriggers)
            {
                var preparse = cTrigger.Key;
                var preparsearray = preparse.Split(new string[] { "|" }, StringSplitOptions.None);
                customTriggers.Add(preparsearray[1]);
            }


            //Timers

            lblStatus.Text = "Plugin Started";
        }

        public void DeInitPlugin()
        {
            // Unsubscribe from any events you listen to when exiting!
            ActGlobals.oFormActMain.BeforeLogLineRead -= OnBeforeLogLineRead;
            ActGlobals.oFormActMain.OnCombatStart -= oFormActMain_OnCombatStart;
            ActGlobals.oFormActMain.OnCombatEnd -= oFormActMain_OnCombatEnd;
            ActGlobals.oFormSpellTimers.OnSpellTimerExpire -= oFormSpellTimers_OnSpellTimerExpire;
            ActGlobals.oFormSpellTimers.OnSpellTimerWarning -= oFormSpellTimers_OnSpellTimerWarning;
            ActGlobals.oFormSpellTimers.OnSpellTimerRemoved -= oFormSpellTimers_OnSpellTimerRemoved;

            lblStatus.Text = "Plugin Exited";
        }

        void oFormActMain_OnCombatStart(bool isImport, CombatToggleEventArgs actionInfo)
        {
            inCombat = true;
            ActiveEnconter = actionInfo.encounter;

            if (dataStruct == null) return;
            dataStruct.IsConnected = true;

            Debug.WriteLine(@"Start");
        }

        void oFormActMain_OnCombatEnd(bool isImport, CombatToggleEventArgs actionInfo)
        {
            inCombat = false;
            ActiveEnconter = null;

            if (dataStruct == null) return;
            dataStruct.IsConnected = false;

            WriteJSON(dataStruct);

            Debug.WriteLine(@"End");
        }

        void oFormSpellTimers_OnSpellTimerExpire(TimerFrame actionInfo)
        {
            if (dataStruct == null) return;
            dataStruct.TimerActive = false;

            WriteJSON(dataStruct);
        }

        void oFormSpellTimers_OnSpellTimerWarning(TimerFrame actionInfo)
        {
            if (dataStruct == null) return;
            dataStruct.TimerActive = true;

            WriteJSON(dataStruct);
        }

        void oFormSpellTimers_OnSpellTimerRemoved(TimerFrame actionInfo)
        {
            if (dataStruct == null) return;
            dataStruct.TimerActive = false;

            WriteJSON(dataStruct);
        }

        void OnBeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            var log = logInfo.logLine;

            //Fetch Data
            if (inCombat)
            {
                
                var actData = ActiveEnconter.GetCombatant("YOU");

                if (actData == null) return;
                
                //Encounter Data

                if (double.TryParse(actData.GetColumnByName("EncDPS").Replace(@",", @"").Replace(@"%", @""),
                    out var encDps))
                {
                    if (encDps >= 0)
                        dataStruct.PlayerCurrentDPS = encDps;
                }

                if (double.TryParse(actData.GetColumnByName("EncHPS").Replace(@",", @"").Replace(@"%", @""),
                    out var encHps))
                {
                    if (encDps >= 0)
                        dataStruct.PlayerCurrentHPS = encHps;
                }

                if (actData.GetColumnByName("Job") == "Whm" || actData.GetColumnByName("Job") == "Sch" ||
                    actData.GetColumnByName("Job") == "Ast")
                {
                    if (double.TryParse(actData.GetColumnByName("CritHeal%").Replace(@",", @"").Replace(@"%", @""),
                        out var encCrit))
                    {
                        if (encDps >= 0)
                            dataStruct.PlayerCurrentCrit = encCrit;
                    }
                }
                else
                {
                    if (double.TryParse(actData.GetColumnByName("CritDam%").Replace(@",", @"").Replace(@"%", @""),
                        out var encCrit))
                    {
                        if (encDps >= 0)
                            dataStruct.PlayerCurrentCrit = encCrit;
                    }
                }

                if (double.TryParse(actData.GetColumnByName("DirectHitPct").Replace(@",", @"").Replace(@"%", @""),
                    out var encDH))
                {
                    if (encDps >= 0)
                        dataStruct.PlayerCurrentDH = encDH;
                }

                if (double.TryParse(actData.GetColumnByName("CritDirectHitPct").Replace(@",", @"").Replace(@"%", @""),
                    out var encCritDH))
                {
                    if (encDps >= 0)
                        dataStruct.PlayerCurrentCritDH = encCritDH;
                }

                if (double.TryParse(actData.GetColumnByName("OverHealPct").Replace(@",", @"").Replace(@"%", @""),
                    out var encOverheal))
                {
                    if (encDps >= 0)
                        dataStruct.PlayerCurrentOverheal = encOverheal;
                }

                if (int.TryParse(actData.GetColumnByName("Damage%").Replace(@",", @"").Replace(@"%", @""),
                    out var entDmg))
                {
                    if (encDps >= 0)
                        dataStruct.PlayerCurrentDamage = entDmg;
                }

                
                //Encounter Information
                var encTime = ActiveEnconter.Duration.TotalSeconds;
                dataStruct.CurrentEncounterTime = encTime;
                dataStruct.CurrentEncounterName = ActiveEnconter.ZoneName;
                
                //Enemies
                dataStruct.Enemies.Clear();
                
                foreach (var combatant in ActiveEnconter.Items)
                {
                    if (combatant.Value.GetColumnByName("Job") != "") continue;
                    dataStruct.Enemies.Add(combatant.Value.Name);
                }
             }

            //Custom Triggers
            if (log.Length > 23)
            {
                var loglineA = log.Substring(23);
                dataStruct.CustomTriggerActive = customTriggers.Any(str => loglineA.Contains(str));

            }

            //Write Data
            WriteJSON(dataStruct);
        }

        private void WriteJSON(ACTDataTemplate data)
        {
            try
            {
                var path = enviroment + @"actdata.chromatics";

                using (var file = File.CreateText(path))
                {
                    var serializer = new Newtonsoft.Json.JsonSerializer();
                    serializer.Serialize(file, dataStruct);
                }
            }
            catch (Exception e)
            {
                //throw;
                Console.WriteLine(e.Message);
            }
        }

        private bool LoadAssemblies()
        {
            if (!File.Exists(enviroment + @"ICSharpCode.SharpZipLib.dll"))
            {
                lblStatus.Text = "Error Loading Plugin. Required Library not found. Please reinstall.";
                return false;
            }
            Assembly.LoadFrom(enviroment + @"ICSharpCode.SharpZipLib.dll");

            if (!File.Exists(enviroment + @"GammaJul.LgLcd.dll"))
            {
                lblStatus.Text = "Error Loading Plugin. Required Library not found. Please reinstall.";
                return false;
            }
            Assembly.LoadFrom(enviroment + @"GammaJul.LgLcd.dll");

            if (!File.Exists(enviroment + @"Newtonsoft.Json.dll"))
            {
                lblStatus.Text = "Error Loading Plugin. Required Library not found. Please reinstall.";
                return false;
            }
            Assembly.LoadFrom(enviroment + @"Newtonsoft.Json.dll");

            return true;
        }
    }
}
