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
        private static int _countdown = 0;
        private static bool _initialized;

        private static readonly object RefreshLock = new object();
        private static readonly object CacheLock = new object();
        private static MemoryHandler _memoryHandler;

        public static void RefreshData(MemoryHandler memoryHandler)
        {
            lock (RefreshLock)
            {
                _memoryHandler = memoryHandler;

                if (!_memoryready)
                    if (!_memoryHandler.Scanner.Locations.ContainsKey("DUTYFINDER") || !_siginit)
                    {

                        _sList = new List<Signature>
                        {
                            new Signature
                            {
                                Key = "DUTYFINDER",
                                Value = "440fb643**488d51**488d0d",
                                ASMSignature = true,
                                PointerPath = new List<long>
                                {
                                    0x0,
                                    0xF4
                                }
                            }
                        };

                        /*
                        _sList = new List<Signature>
                        {
                            new Signature
                            {
                                Key = "DUTYFINDER",
                                PointerPath = new List<long>
                                {
                                    //0x018ECB30,
                                    //0x24
                                    //0x18EDB40
                                    0x193A8F5
                                }
                            }
                        };
                        */


                        _memoryHandler.Scanner.LoadOffsets(_sList.ToArray());

                        Thread.Sleep(100);

                        if (_memoryHandler.Scanner.Locations.ContainsKey("DUTYFINDER"))
                        {
                            Debug.WriteLine("Initializing DUTYFINDER done: " +
                                            _memoryHandler.Scanner.Locations["DUTYFINDER"].GetAddress().ToInt64()
                                                .ToString("X"));

                            _siginit = true;
                        }

                        if (_siginit)
                            _memoryready = true;
                    }

                if (_memoryready)
                {
                    if (_memoryHandler.Scanner.Locations.ContainsKey("DUTYFINDER"))
                    {
                        var address = _memoryHandler.Scanner.Locations["DUTYFINDER"];

                        //PluginController.debug(" " + address.ToString("X8"));
                        var contentFinderState = _memoryHandler.GetByte(address.GetAddress(), 0x71);
                        //var instanceLock = MemoryHandler.Instance.GetByte(address.GetAddress(), 7);
                        //_isPopped = isPopped == 2;

                        _isPopped = contentFinderState == 2;

                        //_countdown = MemoryHandler.Instance.GetInt32(address.GetAddress(), 4);
                        _initialized = true;
                        //Console.WriteLine(contentFinderState);
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
                    RefreshData(_memoryHandler);
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