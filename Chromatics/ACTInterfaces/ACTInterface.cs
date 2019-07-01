using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Chromatics.Datastore;
using GalaSoft.MvvmLight.Ioc;
using Newtonsoft.Json;

namespace Chromatics.ACTInterfaces
{
    public class ACTInterface
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();

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
                    var ret = new ACTDataTemplate();
                    ret.Version = 0;
                    return ret;
                }

                return actData.IsConnected ? actData : new ACTDataTemplate();
            }
            catch (Exception e)
            {
                Write.WriteConsole(ConsoleTypes.Error ,@"Error: " + e.Message);
                return new ACTDataTemplate();
            }

            
        }
    }

    public static class EncounterTimers
    {
        public static List<EncounterData> Encounters = new List<EncounterData>
        {
            {new EncounterData {InstanceName = "The Jade Stoa (Extreme)", BossName = "Byakko" ,EnrageTimer = 683} },
            {new EncounterData {InstanceName = "The Minstrel's Ballad: Tsukuyomi's Pain", BossName = "Tsukuyomi" ,EnrageTimer = 852 } },
            {new EncounterData {InstanceName = "The Minstrel's Ballad: Shinryu's Domain", BossName = "Shinryu" ,EnrageTimer = 1115} }, //
            {new EncounterData {InstanceName = "Emanation (Extreme)", BossName = "Lakshmi" ,EnrageTimer = 789 } },
            {new EncounterData {InstanceName = "The Pool of Tribute (Extreme)", BossName = "Susano" ,EnrageTimer = 679 } },
            {new EncounterData {InstanceName = "Hells' Kier (Extreme)", BossName = "Suzaku" ,EnrageTimer = 675 } },
            {new EncounterData {InstanceName = "Thok ast Thok (Extreme)", BossName = "Ravana" ,EnrageTimer = 475 } },
            {new EncounterData {InstanceName = "Containment Bay (P1T6)", BossName = "Sophia" ,EnrageTimer = 910 } },
            {new EncounterData {InstanceName = "Containment Bay (S1T7)", BossName = "Sephirot" ,EnrageTimer = 1027 } }, //
            {new EncounterData {InstanceName = "Alexander - The Fist Of The Father (Savage)", BossName = "Oppressor" ,EnrageTimer = 510 } },
            {new EncounterData {InstanceName = "Alexander - The Cuff Of The Father (Savage)", BossName = "None" ,EnrageTimer = 630 } },
            {new EncounterData {InstanceName = "Alexander - The Arm Of The Father (Savage)", BossName = "Living Liquid" ,EnrageTimer = 765 } },
            {new EncounterData {InstanceName = "Alexander - The Burden Of The Father (Savage)", BossName = "None" ,EnrageTimer = 790 } },
            {new EncounterData {InstanceName = "Alexander - The Fist Of The Son (Savage)", BossName = "Ratfinx Twinkledinks" ,EnrageTimer = 549 } },
            {new EncounterData {InstanceName = "Alexander - The Cuff Of The Son (Savage)", BossName = "None" ,EnrageTimer = 630 } },
            {new EncounterData {InstanceName = "Alexander - The Arm Of The Son (Savage)", BossName = "Quickthinx Allthoughts" ,EnrageTimer = 750 } },
            {new EncounterData {InstanceName = "Alexander - The Burden Of The Son (Savage)", BossName = "Brute Justice" ,EnrageTimer = 793 } },
            {new EncounterData {InstanceName = "Alexander - The Eyes Of The Creator (Savage)", BossName = "Refurbisher 0" ,EnrageTimer = 548 } },
            {new EncounterData {InstanceName = "Alexander - The Breath Of The Creator (Savage)", BossName = "Lamebrix Strikebocks" ,EnrageTimer = 510 } },
            {new EncounterData {InstanceName = "Alexander - The Heart Of The Creator (Savage)", BossName = "Cruise Chaser" ,EnrageTimer = 670 } },
            {new EncounterData {InstanceName = "Alexander - The Soul Of The Creator (Savage)", BossName = "Alexander Prime" ,EnrageTimer = 730 } },
            {new EncounterData {InstanceName = "Deltascape V1.0 (Savage)", BossName = "Alte Roite" ,EnrageTimer = 579 } },
            {new EncounterData {InstanceName = "Deltascape V2.0 (Savage)", BossName = "Catastrophe" ,EnrageTimer = 630 } },
            {new EncounterData {InstanceName = "Deltascape V3.0 (Savage)", BossName = "Halicarnassus" ,EnrageTimer = 690 } },
            {new EncounterData {InstanceName = "Deltascape V4.0 (Savage)", BossName = "Exdeath" ,EnrageTimer = 290 } }, //507
            {new EncounterData {InstanceName = "Deltascape V4.0 (Savage)", BossName = "Neo Exdeath" ,EnrageTimer = 766 } },
            {new EncounterData {InstanceName = "Sigmascape V1.0 (Savage)", BossName = "Phantom Train" ,EnrageTimer = 579 } },
            {new EncounterData {InstanceName = "Sigmascape V2.0 (Savage)", BossName = "Chadarnook" ,EnrageTimer = 641 } },
            {new EncounterData {InstanceName = "Sigmascape V3.0 (Savage)", BossName = "Guardian" ,EnrageTimer = 726 } },
            {new EncounterData {InstanceName = "Sigmascape V4.0 (Savage)", BossName = "Kefka" ,EnrageTimer = 408 } },
            {new EncounterData {InstanceName = "Sigmascape V4.0 (Savage)", BossName = "Kefka" ,EnrageTimer = 640 } },
            {new EncounterData {InstanceName = "Alphascape V1.0 (Savage)", BossName = "Chaos" ,EnrageTimer = 579 } },
            {new EncounterData {InstanceName = "Alphascape V2.0 (Savage)", BossName = "Midgarsormr" ,EnrageTimer = 715 } },
            {new EncounterData {InstanceName = "Alphascape V3.0 (Savage)", BossName = "Omega" ,EnrageTimer = 781 } },
            {new EncounterData {InstanceName = "Alphascape V4.0 (Savage)", BossName = "Omega-M" ,EnrageTimer = 480 } },
            {new EncounterData {InstanceName = "Alphascape V4.0 (Savage)", BossName = "Omega" ,EnrageTimer = 640 } }, //
            {new EncounterData {InstanceName = "The Unending Coil of Bahamut (Ultimate)", BossName = "None" ,EnrageTimer = 1413 } }, //
            {new EncounterData {InstanceName = "The Weapon's Refrain (Ultimate)", BossName = "None" ,EnrageTimer = 1600 } }, //

            {new EncounterData {InstanceName = "The Crown Of The Immaculate (Extreme)", BossName = "Innocence" ,EnrageTimer = 690 } }, //
            {new EncounterData {InstanceName = "The Dancing Plague (Extreme)", BossName = "Titania" ,EnrageTimer = 650 } } //
        };
    }

    public class EncounterData
    {
        public string InstanceName { get; set; }
        public string BossName { get; set; }
        public int EnrageTimer { get; set; }
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
        public List<string> Enemies { get; set; }

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
            Enemies = new List<string>();
        }
    }

}
