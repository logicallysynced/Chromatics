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
        public static LogitechArx InitializeArxSDK()
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
        public const int LOGI_ARX_ORIENTATION_PORTRAIT = 0x01;

        public const int LOGI_ARX_ORIENTATION_LANDSCAPE = 0x10;
        public const int LOGI_ARX_EVENT_FOCUS_ACTIVE = 0x01;
        public const int LOGI_ARX_EVENT_FOCUS_INACTIVE = 0x02;
        public const int LOGI_ARX_EVENT_TAP_ON_TAG = 0x04;
        public const int LOGI_ARX_EVENT_MOBILEDEVICE_ARRIVAL = 0x08;
        public const int LOGI_ARX_EVENT_MOBILEDEVICE_REMOVAL = 0x10;
        public const int LOGI_ARX_DEVICETYPE_IPHONE = 0x01;
        public const int LOGI_ARX_DEVICETYPE_IPAD = 0x02;
        public const int LOGI_ARX_DEVICETYPE_ANDROID_SMALL = 0x03;
        public const int LOGI_ARX_DEVICETYPE_ANDROID_NORMAL = 0x04;
        public const int LOGI_ARX_DEVICETYPE_ANDROID_LARGE = 0x05;
        public const int LOGI_ARX_DEVICETYPE_ANDROID_XLARGE = 0x06;
        public const int LOGI_ARX_DEVICETYPE_ANDROID_OTHER = 0x07;

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

        void ArxUpdateFfxivStats(double hp_percent, double mp_percent, double tp_percent, int hp_current,
            int mp_current,
            int tp_current, uint zone, string job, string hudmode, double target_percent, int target_hpcurrent,
            int target_hpmax, string target_name, int target_engaged);

        void ArxUpdateFfxivPlugin(double hp_percent, double mp_percent, double tp_percent, int hp_current,
            int mp_current,
            int tp_current, uint zone, string job, string hudmode, double target_percent, int target_hpcurrent,
            int target_hpmax, string target_name, int target_engaged, int hp_max, int mp_max, double playerposX,
            double playerposY, double playerposZ, Actor.ActionStatus actionstatus, double castperc, float castprogress,
            float casttime, bool castingtoggle, float hitboxrad, bool playerclaimed, Actor.Job playerjob, uint mapid,
            uint mapindex, uint mapterritory, string playername, Actor.TargetType targettype);

        void ArxUpdateFfxivParty(string p1data, string p2data, string p3data, string p4data, string p5data,
            string p6data, string p7data, string p8data, string p9data);

        void ShutdownArx();
        void ArxSetIndex(string file);
        void ArxUpdateTheme(string theme);
        void ArxUpdateInfo(string info);
        void ArxSendACTInfo(string IP, int port);
    }

    public class LogitechArx : ILogitechArx
    {
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
                    Console.WriteLine("ARX EX: " + retCode);
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
                            var pluginimgPNG =
                                Directory.GetFiles(resourcedir + @"\img\", "*.png", SearchOption.AllDirectories);
                            var pluginimgJPG =
                                Directory.GetFiles(resourcedir + @"\img\", "*.jpg", SearchOption.AllDirectories);
                            var pluginimgGIF =
                                Directory.GetFiles(resourcedir + @"\img\", "*.gif", SearchOption.AllDirectories);

                            if (plugincss.Length > 0)
                                foreach (var _css in plugincss)
                                {
                                    var reduced = _css.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = _css.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "text/css");
                                }

                            if (pluginjs.Length > 0)
                                foreach (var _js in pluginjs)
                                {
                                    var reduced = _js.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = _js.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "text/javascript");
                                }

                            if (pluginimgPNG.Length > 0)
                                foreach (var _png in pluginimgPNG)
                                {
                                    var reduced = _png.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = _png.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "image/png");
                                }

                            if (pluginimgJPG.Length > 0)
                                foreach (var _jpg in pluginimgJPG)
                                {
                                    var reduced = _jpg.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = _jpg.Replace(@"\", "/");
                                    LogitechArxWrapper.LogiArxAddFileAs(output, reduced, "image/jpeg");
                                }

                            if (pluginimgGIF.Length > 0)
                                foreach (var _gif in pluginimgGIF)
                                {
                                    var reduced = _gif.Replace(@"\", "/").Replace(plugdir, "");
                                    var output = _gif.Replace(@"\", "/");
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

        public void ArxUpdateFfxivStats(double hp_percent, double mp_percent, double tp_percent, int hp_current,
            int mp_current,
            int tp_current, uint zone, string job, string hudmode, double target_percent, int target_hpcurrent,
            int target_hpmax, string target_name, int target_engaged)
        {
            LogitechArxWrapper.LogiArxSetTagContentById("hp_percent", hp_percent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("mp_percent", mp_percent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("tp_percent", tp_percent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("hp_current", hp_current.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mp_current", mp_current.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("tp_current", tp_current.ToString());

            LogitechArxWrapper.LogiArxSetTagContentById("hud_type", job);
            LogitechArxWrapper.LogiArxSetTagContentById("current_location",
                _ffxivMapIds.ContainsKey(zone) ? _ffxivMapIds[zone] : "");

            LogitechArxWrapper.LogiArxSetTagContentById("hud_mode", hudmode);
            LogitechArxWrapper.LogiArxSetTagContentById("target_hppercent", target_percent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("target_hpcurrent", target_hpcurrent.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("target_hpmax", target_hpmax.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("target_name", target_name);
            LogitechArxWrapper.LogiArxSetTagContentById("target_engaged", target_engaged.ToString());
        }

        public void ArxUpdateFfxivPlugin(double hp_percent, double mp_percent, double tp_percent, int hp_current,
            int mp_current,
            int tp_current, uint zone, string job, string hudmode, double target_percent, int target_hpcurrent,
            int target_hpmax, string target_name, int target_engaged, int hp_max, int mp_max, double playerposX,
            double playerposY, double playerposZ, Actor.ActionStatus actionstatus, double castperc, float castprogress,
            float casttime, bool castingtoggle, float hitboxrad, bool playerclaimed, Actor.Job playerjob, uint mapid,
            uint mapindex, uint mapterritory, string playername, Actor.TargetType targettype)
        {
            LogitechArxWrapper.LogiArxSetTagContentById("hp_percent", hp_percent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("mp_percent", mp_percent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("tp_percent", tp_percent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("hp_current", hp_current.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mp_current", mp_current.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("tp_current", tp_current.ToString());

            LogitechArxWrapper.LogiArxSetTagContentById("hud_type", job);
            LogitechArxWrapper.LogiArxSetTagContentById("current_location",
                _ffxivMapIds.ContainsKey(zone) ? _ffxivMapIds[zone] : "");

            LogitechArxWrapper.LogiArxSetTagContentById("hud_mode", hudmode);
            LogitechArxWrapper.LogiArxSetTagContentById("target_hppercent", target_percent.ToString("#0%"));
            LogitechArxWrapper.LogiArxSetTagContentById("target_hpcurrent", target_hpcurrent.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("target_hpmax", target_hpmax.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("target_name", target_name);
            LogitechArxWrapper.LogiArxSetTagContentById("target_engaged", target_engaged.ToString());

            LogitechArxWrapper.LogiArxSetTagContentById("hp_max", hp_max.ToString());
            LogitechArxWrapper.LogiArxSetTagContentById("mp_max", mp_max.ToString());
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

        public void ArxUpdateFfxivParty(string p1data, string p2data, string p3data, string p4data, string p5data,
            string p6data, string p7data, string p8data, string p9data)
        {
            LogitechArxWrapper.LogiArxSetTagContentById("p1_data", p1data);
            LogitechArxWrapper.LogiArxSetTagContentById("p2_data", p2data);
            LogitechArxWrapper.LogiArxSetTagContentById("p3_data", p3data);
            LogitechArxWrapper.LogiArxSetTagContentById("p4_data", p4data);
            LogitechArxWrapper.LogiArxSetTagContentById("p5_data", p5data);
            LogitechArxWrapper.LogiArxSetTagContentById("p6_data", p6data);
            LogitechArxWrapper.LogiArxSetTagContentById("p7_data", p7data);
            LogitechArxWrapper.LogiArxSetTagContentById("p8_data", p8data);
            LogitechArxWrapper.LogiArxSetTagContentById("p9_data", p9data);
        }

        public void ArxSendACTInfo(string IP, int port)
        {
            LogitechArxWrapper.LogiArxSetTagContentById("actipaddress", IP);
            LogitechArxWrapper.LogiArxSetTagContentById("actport", port.ToString());
        }

        public void ArxUpdateTheme(string theme)
        {
            LogitechArxWrapper.LogiArxSetTagContentById("theme", theme);
        }

        public void ArxUpdateInfo(string info)
        {
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
                case LogitechArxWrapper.LOGI_ARX_EVENT_MOBILEDEVICE_ARRIVAL:
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

                    var core_images = Directory.GetFiles(webdir + @"\img", "*.png", SearchOption.AllDirectories);
                    foreach (var coreimage in core_images)
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
                case LogitechArxWrapper.LOGI_ARX_EVENT_MOBILEDEVICE_REMOVAL:
                    //Device disconnected
                    break;
                case LogitechArxWrapper.LOGI_ARX_EVENT_TAP_ON_TAG:
                    if (eventArg == "myBtn")
                    {
                        //Do something on this input
                    }
                    break;
            }
        }
    }
}