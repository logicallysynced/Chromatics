using Microsoft.VisualBasic;
using Sharlayan;
using Sharlayan.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.Sharlayan
{
    public class WeatherExtension
    {
        public static DateTime LastUpdated = DateTime.MinValue;

        private static readonly string memoryName = @"WEATHERZONE";
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);
        private static bool _siginit;
        private static bool _memoryready;
        private static byte _weatherId;
        private static bool _initialized;
        private static List<Signature> _sList;
        private static readonly object RefreshLock = new object();
        private static readonly object CacheLock = new object();
        private static MemoryHandler _memoryHandler;

        public static void RefreshData(MemoryHandler memoryHandler)
        {
            lock (RefreshLock)
            {
                _memoryHandler = memoryHandler;

                if (!_memoryready)
                {
                    if (!_memoryHandler.Scanner.Locations.ContainsKey(memoryName) || !_siginit)
                    {

                        _sList = new List<Signature>
                        {
                            new Signature //Not used
                            {
                                Key = memoryName,
                                Value = "E8????????0FB6C883E907",
                                ASMSignature = true,
                                PointerPath = new List<long>
                                {   
                                    3
                                }
                            }
                        };

                        _memoryHandler.Scanner.LoadOffsets(_sList.ToArray());

                        Thread.Sleep(100);

                        if (_memoryHandler.Scanner.Locations.ContainsKey(memoryName))
                        {
                            #if DEBUG
                                Debug.WriteLine($"Found {memoryName}. Location: {_memoryHandler.Scanner.Locations[memoryName].GetAddress().ToInt64():X}");
                            #endif

                            _siginit = true;
                        }

                        if (_siginit)
                            _memoryready = true;
                    }
                }
                else
                {
                    if (_memoryHandler.Scanner.Locations.ContainsKey(memoryName))
                    {
                        var address = _memoryHandler.Scanner.Locations[memoryName]; //Not used
                        var currentWeather = _memoryHandler.GetByte(address.GetAddress(), 0x28); //Not used

                        nint baseAddress = _memoryHandler.Configuration.ProcessModel.Process.MainModule.BaseAddress;
                        var digitalWeather = _memoryHandler.GetByte(baseAddress + 0x2550CA4);

                        _weatherId = digitalWeather;

                        _initialized = true;
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

        public static byte WeatherId()
        {
            if (!_initialized)
                return 0;

            CheckCache();

            return _weatherId;
        }
    }
}
