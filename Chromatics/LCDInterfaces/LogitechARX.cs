using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Chromatics.Properties;
using Sharlayan.Core.Enums;

namespace Chromatics.LCDInterfaces
{
    public class LogitechArxInterface
    {
        public static LogitechArx InitializeArxSdk()
        {
            LogitechArx arx = null;
            if (Process.GetProcessesByName("LCore").Length > 0)
            {
                arx = new LogitechArx();
                var result = arx.InitializeArx();
                if (!result)
                    return null;
            }
            return arx;
        }
    }

    public class LogitechArxWrapper
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogiArxCb(int eventType, int eventValue, [MarshalAs(UnmanagedType.LPWStr)] string eventArg,
            IntPtr context);

        //LED SDK
        public const int LogiArxOrientationPortrait = 0x01;

        public const int LogiArxOrientationLandscape = 0x10;
        public const int LogiArxEventFocusActive = 0x01;
        public const int LogiArxEventFocusInactive = 0x02;
        public const int LogiArxEventTapOnTag = 0x04;
        public const int LogiArxEventMobiledeviceArrival = 0x08;
        public const int LogiArxEventMobiledeviceRemoval = 0x10;
        public const int LogiArxDevicetypeIphone = 0x01;
        public const int LogiArxDevicetypeIpad = 0x02;
        public const int LogiArxDevicetypeAndroidSmall = 0x03;
        public const int LogiArxDevicetypeAndroidNormal = 0x04;
        public const int LogiArxDevicetypeAndroidLarge = 0x05;
        public const int LogiArxDevicetypeAndroidXlarge = 0x06;
        public const int LogiArxDevicetypeAndroidOther = 0x07;

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxInit(string identifier, string friendlyName, ref LogiArxCbContext callback);

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxInitWithIcon(string identifier, string friendlyName,
            ref LogiArxCbContext callback, byte[] iconBitmap);

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxAddFileAs(string filePath, string fileName, string mimeType = "");

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxAddContentAs(byte[] content, int size, string fileName, string mimeType = "");

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxAddUTF8StringAs(string stringContent, string fileName, string mimeType = "");

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxAddImageFromBitmap(byte[] bitmap, int width, int height, string fileName);

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxSetIndex(string fileName);

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxSetTagPropertyById(string tagId, string prop, string newValue);

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxSetTagsPropertyByClass(string tagsClass, string prop, string newValue);

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxSetTagContentById(string tagId, string newContent);

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiArxSetTagsContentByClass(string tagsClass, string newContent);

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int LogiArxGetLastError();

        [DllImport("LogitechGArxControlEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void LogiArxShutdown();

        public struct LogiArxCbContext
        {
            public LogiArxCb ArxCallBack;
            public IntPtr ArxContext;
        }
    }

    public interface ILogitechArx
    {
        bool InitializeArx();
        List<string> LoadPlugins();

        void ArxUpdateFfxivStats(double hpPercent, double mpPercent, double tpPercent, int hpCurrent,
            int mpCurrent,
            int tpCurrent, uint zone, string job, string hudmode, double targetPercent, int targetHpcurrent,
            int targetHpmax, string targetName, int targetEngaged);

        void ArxUpdateFfxivPlugin(double hpPercent, double mpPercent, double tpPercent, int hpCurrent,
            int mpCurrent,
            int tpCurrent, uint zone, string job, string hudmode, double targetPercent, int targetHpcurrent,
            int targetHpmax, string targetName, int targetEngaged, int hpMax, int mpMax, double playerposX,
            double playerposY, double playerposZ, Actor.ActionStatus actionstatus, double castperc, float castprogress,
            float casttime, bool castingtoggle, float hitboxrad, bool playerclaimed, Actor.Job playerjob, uint mapid,
            uint mapindex, uint mapterritory, string playername, Actor.TargetType targettype);

        void ArxUpdateFfxivParty(string p1Data, string p2Data, string p3Data, string p4Data, string p5Data,
            string p6Data, string p7Data, string p8Data, string p9Data);

        void ShutdownArx();
        void ArxSetIndex(string file);
        void ArxUpdateTheme(string theme);
        void ArxUpdateInfo(string info);
        void ArxSendActInfo(string ip, int port);

        void SetArxCurrentID(int id);
    }

    public class LogitechArx : ILogitechArx
    {
        private static int _ArxID;

        private readonly Dictionary<uint, string> _ffxivMapIds = new Dictionary<uint, string>
        {
            //Keys
            {2, "New Gridania"},
            {3, "Old Gridania"},
            {4, "Central Shroud"},
            {5, "East Shroud"},
            {6, "South Shroud"},
            {7, "North Shroud"},
            {11, "Limsa Lominsa Upper Decks"},
            {12, "Limsa Lominsa Lower Decks"},
            {13, "Ul'dah - Steps of Nald"},
            {14, "Ul'dah - Steps of Thal"},
            {15, "Middle La Noscea"},
            {16, "Lower La Noscea"},
            {17, "Eastern La Noscea"},
            {18, "Western La Noscea"},
            {19, "Upper La Noscea"},
            {20, "Western Thanalan"},
            {21, "Central Thanalan"},
            {22, "Eastern Thanalan"},
            {23, "Southern Thanalan"},
            {24, "Northern Thanalan"},
            {30, "Outer La Noscea"},
            {51, "Wolves' Den Peer"}, //Wolves' Den Peer
            {53, "Coerthas Central Highlands"},
            {72, "Mist"},
            {82, "The Lavender Beds"},
            {83, "The Goblet"},
            {109, "Mist"}, //Small House
            {110, "Mist"}, //Medium House
            {113, "Mist"}, //Large House
            {116, "The Lavender Beds"}, //Small House
            {196, "The Gold Saucer"}, //Gold Saucer
            {211, "Coerthas Western Highlands"},
            {212, "The Dravanian Forelands"},
            {213, "The Dravanian Hinterlands"},
            {214, "The Churning Mists"},
            {215, "The Sea of Clouds"},
            {216, "Azys Lla"},
            {218, "Ishgard - Foundation"}, //Foundation
            {219, "Ishgard - The Pillars"}, //Pillars
            {257, "Idyllshire"},
            {320, "Mist"}, //Appartment Lobby
            {323, "Mist"} //Appartment Room
        };

        private LogitechArxWrapper.LogiArxCb _hookProcDelegate;

        public void SetArxCurrentID(int id)
        {
            _ArxID = id;
            //Console.WriteLine(@"ARX ID: " + _ArxID);
        }

        public bool InitializeArx()
        {
            var result = true;
            try
            {
                //var write = SimpleIoc.Default.GetInstance<ILogWrite>();
                _hookProcDelegate = SdkCallback;
                var rm = Resources.ResourceManager;
                var chromaticsicon = IconToBytes((Icon) rm.GetObject("Chromatics_icon_144x144"));


                //write.WriteConsole(ConsoleTypes.ARX, "Attempting to load ARX SDK..");
                LogitechArxWrapper.LogiArxCbContext contextCallback;
                contextCallback.ArxCallBack = _hookProcDelegate;
                contextCallback.ArxContext = IntPtr.Zero;
                var retVal = LogitechArxWrapper.LogiArxInit("chromatics", "Chromatics - Final Fantasy XIV",
                    ref contextCallback);
                if (!retVal)
                {
                    var retCode = LogitechArxWrapper.LogiArxGetLastError();
                    Console.WriteLine(@"ARX EX: " + retCode);
                    //write.WriteConsole(ConsoleTypes.ARX, "Loading ARX SDK Failed:" + retCode);
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public void ShutdownArx()
        {
            try
            {
                LogitechArxWrapper.LogiArxShutdown();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<string> LoadPlugins()
        {
            var pluginsdata = new List<string>();
            var plugindir = "plugins";
            var pluginSearch = Directory.GetFiles(plugindir + @"\", "*.manifest", SearchOption.AllDirectories);

            if (pluginSearch.Length > 0)
                foreach (var plugin in pluginSearch)
                    using (var sr = new StreamReader(plugin))
                    {
                        try
                        {
                            var _plugname = sr.ReadToEnd();
                            var plugname = _plugname.Replace(" ", "_");
                            var plugdir = plugin.Replace(@"\", "/").Replace(@"plugin.manifest", "");
                            var resourcedir = plugin.Replace(plugindir + "/", "").Replace(@"\plugin.manifest", "");

                            sr.Close();
                            Console.WriteLine(plugname + @" Plugin Found. Attempting to Load..");

                            //Load Data

                            var plugincss =
                                Directory.GetFiles(resourcedir + @"\css\", "*.css", SearchOption.AllDirectories);
                            var pluginjs =
                                Directory.GetFiles(resourcedir + @"\js\", "*.js", SearchOption.AllDirectories);
                            var pluginimgPng =
                                Directory.GetFiles(resourcedir + @"\img\", "*.png", SearchOption.AllDirectories);
                            var pluginimgJpg =
                                Directory.GetFiles(resourcedir + @"\img\", "*.jpg", SearchOption.AllDirectories);
                            var pluginimgGif =
                                Directory.GetFiles(resourcedir + @"\img\", "*.gif", SearchOption.AllDirectories);

                            if (plugincss.Length > 0)
                                foreach (var css in plugincss)
                                {
                                    var reduced = css.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = css.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "text/css");
                                }

                            if (pluginjs.Length > 0)
                                foreach (var js in pluginjs)
                                {
                                    var reduced = js.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = js.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "text/javascript");
                                }

                            if (pluginimgPng.Length > 0)
                                foreach (var png in pluginimgPng)
                                {
                                    var reduced = png.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = png.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "image/png");
                                }

                            if (pluginimgJpg.Length > 0)
                                foreach (var jpg in pluginimgJpg)
                                {
                                    var reduced = jpg.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = jpg.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "image/jpeg");
                                }

                            if (pluginimgGif.Length > 0)
                                foreach (var gif in pluginimgGif)
                                {
                                    var reduced = gif.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = gif.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "image/gif");
                                }

                            var _final = resourcedir + @"\index.html";
                            var final = _final.Replace(@"\", "/");
                            LogitechArxWrapper.LogiArxAddFileAs("core/scripts/ffxivsetup.js", "ffxivsetup.js",
                                "text/javascript");
                            LogitechArxWrapper.LogiArxAddFileAs("core/scripts/ffxivexternal.js", "ffxivexternal.js",
                                "text/javascript");
                            LogitechArxWrapper.LogiArxAddFileAs(final, plugname + ".html", "text/html");
                            Console.WriteLine(@"Added file " + final + @" AS " + plugname + @".html");

                            pluginsdata.Add(plugname);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(@"EX: " + ex.Message);
                        }
                    }

            return pluginsdata;
        }

        public void ArxUpdateFfxivStats(double hpPercent, double mpPercent, double tpPercent, int hpCurrent,
            int mpCurrent,
            int tpCurrent, uint zone, string job, string hudmode, double targetPercent, int targetHpcurrent,
            int targetHpmax, string targetName, int targetEngaged)
        {

            if (_ArxID != 1) return;

            LogitechArxWrapper.LogiArxSetTagContentById("hp_percent", hpPercent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("mp_percent", mpPercent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("tp_percent", tpPercent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("hp_current", hpCurrent.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mp_current", mpCurrent.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("tp_current", tpCurrent.ToString());

            LogitechArxWrapper.LogiArxSetTagContentById("hud_type", job);
            LogitechArxWrapper.LogiArxSetTagContentById("current_location",
                _ffxivMapIds.ContainsKey(zone) ? _ffxivMapIds[zone] : "");

            LogitechArxWrapper.LogiArxSetTagContentById("hud_mode", hudmode);
            LogitechArxWrapper.LogiArxSetTagContentById("target_hppercent", targetPercent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("target_hpcurrent", targetHpcurrent.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("target_hpmax", targetHpmax.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("target_name", targetName);
            LogitechArxWrapper.LogiArxSetTagContentById("target_engaged", targetEngaged.ToString());
        }

        public void ArxUpdateFfxivPlugin(double hpPercent, double mpPercent, double tpPercent, int hpCurrent,
            int mpCurrent,
            int tpCurrent, uint zone, string job, string hudmode, double targetPercent, int targetHpcurrent,
            int targetHpmax, string targetName, int targetEngaged, int hpMax, int mpMax, double playerposX,
            double playerposY, double playerposZ, Actor.ActionStatus actionstatus, double castperc, float castprogress,
            float casttime, bool castingtoggle, float hitboxrad, bool playerclaimed, Actor.Job playerjob, uint mapid,
            uint mapindex, uint mapterritory, string playername, Actor.TargetType targettype)
        {
            if (_ArxID != 100) return;

            LogitechArxWrapper.LogiArxSetTagContentById("hp_percent", hpPercent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("mp_percent", mpPercent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("tp_percent", tpPercent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("hp_current", hpCurrent.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mp_current", mpCurrent.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("tp_current", tpCurrent.ToString());

            LogitechArxWrapper.LogiArxSetTagContentById("hud_type", job);
            LogitechArxWrapper.LogiArxSetTagContentById("current_location",
                _ffxivMapIds.ContainsKey(zone) ? _ffxivMapIds[zone] : "");

            LogitechArxWrapper.LogiArxSetTagContentById("hud_mode", hudmode);
            LogitechArxWrapper.LogiArxSetTagContentById("target_hppercent", targetPercent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("target_hpcurrent", targetHpcurrent.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("target_hpmax", targetHpmax.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("target_name", targetName);
            LogitechArxWrapper.LogiArxSetTagContentById("target_engaged", targetEngaged.ToString());

            LogitechArxWrapper.LogiArxSetTagContentById("hp_max", hpMax.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mp_max", mpMax.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("playerposX", playerposX.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("playerposY", playerposY.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("playerposZ", playerposZ.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("actionstatus", actionstatus.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("castperc", castperc.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("castprogress", castprogress.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("casttime", casttime.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("castingtoggle", castingtoggle.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("hitboxrad", hitboxrad.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("playerclaimed", playerclaimed.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("playerjob", playerjob.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mapid", mapid.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mapindex", mapindex.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mapterritory", mapterritory.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("playername", playername);
            LogitechArxWrapper.LogiArxSetTagContentById("targettype", targettype.ToString());
        }

        public void ArxUpdateFfxivParty(string p1Data, string p2Data, string p3Data, string p4Data, string p5Data,
            string p6Data, string p7Data, string p8Data, string p9Data)
        {
            if (_ArxID != 2) return;

            LogitechArxWrapper.LogiArxSetTagContentById("p1_data", p1Data);
            LogitechArxWrapper.LogiArxSetTagContentById("p2_data", p2Data);
            LogitechArxWrapper.LogiArxSetTagContentById("p3_data", p3Data);
            LogitechArxWrapper.LogiArxSetTagContentById("p4_data", p4Data);
            LogitechArxWrapper.LogiArxSetTagContentById("p5_data", p5Data);
            LogitechArxWrapper.LogiArxSetTagContentById("p6_data", p6Data);
            LogitechArxWrapper.LogiArxSetTagContentById("p7_data", p7Data);
            LogitechArxWrapper.LogiArxSetTagContentById("p8_data", p8Data);
            LogitechArxWrapper.LogiArxSetTagContentById("p9_data", p9Data);
        }

        public void ArxSendActInfo(string ip, int port)
        {
            if (_ArxID != 3) return;

            LogitechArxWrapper.LogiArxSetTagContentById("actipaddress", ip);
            LogitechArxWrapper.LogiArxSetTagContentById("actport", port.ToString());
        }

        public void ArxUpdateTheme(string theme)
        {
            LogitechArxWrapper.LogiArxSetTagContentById("theme", theme);
        }

        public void ArxUpdateInfo(string info)
        {
            if (_ArxID != 0) return;

            LogitechArxWrapper.LogiArxSetTagContentById("chromatics_info", info);
        }

        public void ArxSetIndex(string file)
        {
            LogitechArxWrapper.LogiArxSetIndex(file);
            Console.WriteLine(@"Switching to " + file);
        }

        private static byte[] IconToBytes(Icon icon)
        {
            using (var ms = new MemoryStream())
            {
                icon.Save(ms);
                //Console.WriteLine("BYTES: " + ms.Length);
                return ms.ToArray();
            }
        }

        private static Icon BytesToIcon(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                return new Icon(ms);
            }
        }

        private static void SdkCallback(int eventType, int eventValue, string eventArg, IntPtr context)
        {
            switch (eventType)
            {
                case LogitechArxWrapper.LogiArxEventMobiledeviceArrival:
                    //Send your files here
                    //public static extern bool LogiArxAddFileAs(string filePath, string fileName, string mimeType = "");

                    Console.WriteLine(@"ARX Client Found");

                    var webdir = "core";

                    //Load JavaScript

                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/libs/jquery/jquery/dist/jquery.js",
                        "libs/jquery/jquery/dist/jquery.js", "text/javascript");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/libs/jquery/tether/dist/js/tether.min.js",
                        "libs/jquery/tether/dist/js/tether.min.js", "text/javascript");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/libs/jquery/bootstrap/dist/js/bootstrap.js",
                        "libs/jquery/bootstrap/dist/js/bootstrap.js", "text/javascript");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/libs/jquery/underscore/underscore-min.js",
                        "libs/jquery/underscore/underscore-min.js", "text/javascript");
                    LogitechArxWrapper.LogiArxAddFileAs(
                        webdir + "/libs/jquery/jQuery-Storage-API/jquery.storageapi.min.js",
                        "libs/jquery/jQuery-Storage-API/jquery.storageapi.min.js", "text/javascript");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/libs/jquery/PACE/pace.min.js",
                        "libs/jquery/PACE/pace.min.js", "text/javascript");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/scripts/ui-form.js", "scripts/ui-form.js",
                        "text/javascript");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/scripts/ffxivdata.js", "scripts/ffxivdata.js",
                        "text/javascript");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/scripts/chromatics.js", "scripts/chromatics.js",
                        "text/javascript");


                    //Load CSS
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/assets/animate.css/animate.min.css",
                        "assets/animate.css/animate.min.css", "text/css");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/assets/glyphicons/glyphicons.css",
                        "assets/glyphicons/glyphicons.css", "text/css");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/assets/font-awesome/css/font-awesome.min.css",
                        "assets/font-awesome/css/font-awesome.min.css", "text/css");
                    LogitechArxWrapper.LogiArxAddFileAs(
                        webdir + "/assets/material-design-icons/material-design-icons.css",
                        "assets/material-design-icons/material-design-icons.css", "text/css");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/assets/bootstrap/dist/css/bootstrap.min.css",
                        "assets/bootstrap/dist/css/bootstrap.min.css", "text/css");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/assets/styles/app.css", "assets/styles/app.css",
                        "text/css");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/assets/styles/font.css", "assets/styles/font.css",
                        "text/css");

                    //Load Images

                    var coreImages = Directory.GetFiles(webdir + @"\img", "*.png", SearchOption.AllDirectories);
                    foreach (var coreimage in coreImages)
                    {
                        var reduced = coreimage.Replace(@"\", "/").Replace(webdir + "/", "");
                        var output = coreimage.Replace(@"\", "/");
                        LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "image/png");
                    }


                    //Load HTML
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/info.html", "info.html", "text/html");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/playerhud.html", "playerhud.html", "text/html");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/partylist.html", "partylist.html", "text/html");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/mapdata.html", "mapdata.html", "text/html");
                    LogitechArxWrapper.LogiArxAddFileAs(webdir + "/act.html", "act.html", "text/html");

                    //Init HTML
                    LogitechArxWrapper.LogiArxSetIndex("info.html");
                    Console.WriteLine(@"Switching to info.html");

                    break;
                case LogitechArxWrapper.LogiArxEventMobiledeviceRemoval:
                    //Device disconnected
                    break;
                case LogitechArxWrapper.LogiArxEventTapOnTag:
                    if (eventArg == "myBtn")
                    {
                        //Do something on this input
                    }
                    break;
            }
        }
    }
}