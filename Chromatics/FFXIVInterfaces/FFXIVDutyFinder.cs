using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sharlayan;
using Sharlayan.Models;

namespace Chromatics.FFXIVInterfaces
{
    internal class FfxivDutyFinder
    {
        public static DateTime LastUpdated = DateTime.MinValue;

        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);
        private static bool _siginit;
        private static bool _memoryready;
        private static List<Signature> _sList;
        private static bool _isPopped;
        private static int _countdown;
        private static bool _initialized;

        private static readonly object RefreshLock = new object();

        private static readonly object CacheLock = new object();

        public static void RefreshData()
        {
            lock (RefreshLock)
            {
                if (!_memoryready)
                    if (!Scanner.Instance.Locations.ContainsKey("COOLDOWNS") || !_siginit)
                    {
                        _sList = new List<Signature>();

                        _sList.Add(new Signature
                        {
                            Key = "DUTYFINDER",
                            PointerPath = new List<long>
                            {
                                0x0178C018,
                                0x7D0,
                                0x2A0,
                                0x7C0
                            }
                        });

                        Scanner.Instance.LoadOffsets(_sList);

                        Thread.Sleep(100);

                        if (Scanner.Instance.Locations.ContainsKey("DUTYFINDER"))
                        {
                            Debug.WriteLine("Initializing DUTYFINDER done: " +
                                            Scanner.Instance.Locations["DUTYFINDER"].GetAddress().ToInt64()
                                                .ToString("X"));

                            _siginit = true;
                        }

                        if (_siginit)
                            _memoryready = true;
                    }

                if (_memoryready)
                {
                    if (Scanner.Instance.Locations.ContainsKey("DUTYFINDER"))
                    {
                        var address = Scanner.Instance.Locations["DUTYFINDER"];

                        //PluginController.debug(" " + address.ToString("X8"));
                        var isPopped = MemoryHandler.Instance.GetInt32(address.GetAddress(), 0);
                        _isPopped = isPopped == 0 ? false : true;

                        _countdown = MemoryHandler.Instance.GetInt32(address.GetAddress(), 4);
                        _initialized = true;
                        //Debug.WriteLine(isPopped + "/" + _countdown);
                    }


                    LastUpdated = DateTime.Now;
                }
            }
        }

        public static void CheckCache()
        {
            lock (CacheLock)
            {
                if (LastUpdated + UpdateInterval <= DateTime.Now)
                    RefreshData();
            }
        }

        public static bool IsPopped()
        {
            if (!_initialized)
                return false;

            CheckCache();

            return _isPopped;
        }

        public static int Countdown()
        {
            if (!_initialized)
                return 0;

            CheckCache();

            return _countdown;
        }
    }
}