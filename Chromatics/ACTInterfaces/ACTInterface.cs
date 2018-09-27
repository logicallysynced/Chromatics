using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Chromatics.Datastore;
using Newtonsoft.Json;

namespace Chromatics.ACTInterfaces
{
    public class ACTInterface
    {
        public static ACTDataTemplate FetchActData()
        {
            return ReadActDataFile();
        }

        private static ACTDataTemplate ReadActDataFile()
        {
            var actData = new ACTDataTemplate();

            try
            {
                var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                var path = enviroment + @"\actdata.chromatics";
                
                if (File.Exists(path))
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var r = new StreamReader(fs))
                    {
                        var json = r.ReadToEnd();
                        actData = JsonConvert.DeserializeObject<ACTDataTemplate>(json);
                    }
                }
                else
                {
                    Console.WriteLine(@"ACT File not found: " + path);
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(@"Error: " + e.Message);
            }

            return actData.IsConnected ? actData : new ACTDataTemplate();
        }
    }


    public class ACTDataTemplate
    {
        public bool IsConnected { get; set; }
        public int Version { get; set; }

        public double PlayerCurrentDPS { get; set; }
        public double PlayerCurrentHPS { get; set; }
        public double PlayerCurrentGroupDPS { get; set; }
        public double PlayerCurrentCrit { get; set; }
        public double PlayerCurrentDH { get; set; }
        public double PlayerCurrentCritDH { get; set; }
        public double PlayerCurrentOverheal { get; set; }
        public int PlayerCurrentDamage { get; set; }
        public double CurrentEncounterTime { get; set; }
        public string CurrentEncounterName { get; set; }
        public bool CustomTriggerActive { get; set; }
        public bool TimerActive { get; set; }

        public ACTDataTemplate()
        {
            IsConnected = false;
            Version = 1;
            PlayerCurrentDPS = 0;
            PlayerCurrentHPS = 0;
            PlayerCurrentGroupDPS = 0;
            PlayerCurrentCrit = 0;
            PlayerCurrentDH = 0;
            PlayerCurrentCritDH = 0;
            PlayerCurrentOverheal = 0;
            PlayerCurrentDamage = 0;
            CurrentEncounterTime = 0;
            CurrentEncounterName = "";
            CustomTriggerActive = false;
            TimerActive = false;
        }
    }

}
