using Sharlayan.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.FFXIVInterfaces
{
    class FFXIVDutyFinder
    {
        public static DateTime lastUpdated = DateTime.MinValue;

        private static TimeSpan updateInterval = TimeSpan.FromSeconds(0.05);
        private static bool _siginit;
        private static bool _memoryready;
        private static List<Signature> sList;
        private static bool _isPopped;
        private static int _countdown;
        private static bool initialized;

        static object refreshLock = new object();

        public static void RefreshData()
        {
            lock (refreshLock)
            {
                if (!_memoryready)
                {
                    if (!Sharlayan.Scanner.Instance.Locations.ContainsKey("COOLDOWNS") || !_siginit)
                    {
                        sList = new List<Signature>();

                        sList.Add(new Signature
                        {
                            Key = "DUTYFINDER",
                            PointerPath = new List<long>()
                                {
                                    0x0178B018,
                                    0x7D0,
                                    0x2A0,
                                    0x7C0
                                }
                        });

                        Sharlayan.Scanner.Instance.LoadOffsets(sList);

                        Thread.Sleep(100);

                        if (Sharlayan.Scanner.Instance.Locations.ContainsKey("DUTYFINDER"))
                        {
                            Debug.WriteLine("Initializing DUTYFINDER done: " + Sharlayan.Scanner.Instance.Locations["DUTYFINDER"].GetAddress().ToInt64().ToString("X"));

                            _siginit = true;
                        }

                        if (_siginit)
                        {
                            _memoryready = true;
                        }
                    }
                }

                if (_memoryready)
                {
                    if (Sharlayan.Scanner.Instance.Locations.ContainsKey("DUTYFINDER"))
                    {
                        Signature address = Sharlayan.Scanner.Instance.Locations["DUTYFINDER"];

                        //PluginController.debug(" " + address.ToString("X8"));
                        var isPopped = Sharlayan.MemoryHandler.Instance.GetInt32(address.GetAddress(), 0);
                        _isPopped = isPopped == 0 ? false : true;

                        _countdown = Sharlayan.MemoryHandler.Instance.GetInt32(address.GetAddress(), 4);
                        initialized = true;
                        //Debug.WriteLine(isPopped + "/" + countdown);
                    }


                    lastUpdated = DateTime.Now;
                }
            }
        }

        private static object cacheLock = new object();
        public static void CheckCache()
        {
            lock (cacheLock)
            {
                if (lastUpdated + updateInterval <= DateTime.Now)
                {
                    RefreshData();
                }
            }
        }

        public static bool isPopped()
        {
            if (!initialized)
                return false;

            CheckCache();

            return _isPopped;
        }

        public static int Countdown()
        {
            if (!initialized)
                return 0;

            CheckCache();

            return _countdown;
        }
    }
}
