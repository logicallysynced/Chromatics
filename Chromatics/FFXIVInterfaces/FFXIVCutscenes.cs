using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sharlayan;
using Sharlayan.Models;

namespace Chromatics.FFXIVInterfaces
{
    internal class FfxivCutscenes
    {
        public static DateTime LastUpdated = DateTime.MinValue;

        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);
        private static bool _siginit;
        private static bool _memoryready;
        private static List<Signature> _sList;
        private static bool _inCutscene;
        private static bool _initialized;

        private static readonly object RefreshLock = new object();

        private static readonly object CacheLock = new object();

        public static void RefreshData()
        {
            lock (RefreshLock)
            {
                if (!_memoryready)
                    if (!Scanner.Instance.Locations.ContainsKey("CUTSCENES") || !_siginit)
                    {

                        
                        _sList = new List<Signature>
                        {
                            new Signature
                            {
                                Key = "CUTSCENES",
                                PointerPath = new List<long>
                                {
                                    //0x018ECB30,
                                    //0x24
                                    //0x18EDB40
                                    0x18AFC58
                                }
                            }
                        };


                        Scanner.Instance.LoadOffsets(_sList);

                        Thread.Sleep(100);

                        if (Scanner.Instance.Locations.ContainsKey("CUTSCENES"))
                        {
                            Debug.WriteLine("Initializing CUTSCENES done: " +
                                            Scanner.Instance.Locations["CUTSCENES"].GetAddress().ToInt64()
                                                .ToString("X"));

                            _siginit = true;
                        }

                        if (_siginit)
                            _memoryready = true;
                    }

                if (_memoryready)
                {
                    if (Scanner.Instance.Locations.ContainsKey("CUTSCENES"))
                    {
                        var address = Scanner.Instance.Locations["CUTSCENES"];

                        //PluginController.debug(" " + address.ToString("X8"));
                        var CutsceneState = MemoryHandler.Instance.GetByte(address.GetAddress(), 0x0);
                        //var instanceLock = MemoryHandler.Instance.GetByte(address.GetAddress(), 7);
                        //_isPopped = isPopped == 2;

                        _inCutscene = CutsceneState == 1;

                        //_countdown = MemoryHandler.Instance.GetInt32(address.GetAddress(), 4);
                        _initialized = true;
                        //Console.WriteLine(CutsceneState);
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

        public static bool InCutscene()
        {
            if (!_initialized)
                return false;

            CheckCache();

            return _inCutscene;
        }
    }
}