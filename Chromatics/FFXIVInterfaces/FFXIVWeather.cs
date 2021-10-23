using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Sharlayan;
using Sharlayan.Models;

namespace Chromatics.FFXIVInterfaces
{
    internal class FFXIVWeather
    {
        public static DateTime LastUpdated = DateTime.MinValue;

        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);
        private static bool _siginit;
        private static bool _memoryready;
        private static List<Signature> _sList;

        private static int _weatherIconID;
        private static bool _initialized;

        private static readonly object RefreshLock = new object();

        private static readonly object CacheLock = new object();
        private static Dictionary<int, string> WeatherMap = new Dictionary<int, string>();
        private static MemoryHandler _memoryHandler;

        public static void GetWeatherAPI()
        {
            var _WeatherData = Controllers.Helpers.GetCsvData(@"https://raw.githubusercontent.com/viion/ffxiv-datamining/master/csv/Weather.csv", @"Weather.csv");

            var delimiters = new[] { ',' };
            using (var reader = new StreamReader(_WeatherData))
            {
                int lineptr = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    lineptr++;

                    if (lineptr < 4) continue;

                    if (line == null)
                    {
                        break;
                    }

                    var parts = line.Split(delimiters);

                    if (parts[1] == null && parts[1] == "") continue;
                    if (parts[2] == null && parts[2] == "") continue;

                    if (!int.TryParse(parts[1], out var id)) continue;
                    if (!WeatherMap.ContainsKey(id))
                    {
                        WeatherMap.Add(id, parts[2]?.Replace("\"", ""));
                        //Console.WriteLine(id + @"//" + parts[2]);
                    }
                }
            }
        }

        

        public static void RefreshData(MemoryHandler memoryHandler)
        {
            lock (RefreshLock)
            {
                _memoryHandler = memoryHandler;

                if (!_memoryready)
                    if (!_memoryHandler.Scanner.Locations.ContainsKey("WEATHER") || !_siginit)
                    {
                        _sList = new List<Signature>
                        {
                            new Signature
                            {
                                Key = "WEATHER",
                                Value = "49896b**488bf1488b0d",
                                ASMSignature = true,
                                PointerPath = new List<long>
                                {
                                    0x0,
                                    0x0,
                                    0x38,
                                    0x18,
                                    0x190,
                                    0x20,
                                    0x0
                                }
                            }
                        };

                        /*
                        _sList.Add(new Signature
                        {
                            Key = "WEATHER",
                            PointerPath = new List<long>
                            {
                                0x018FE408,
                                0x38,
                                0x18,
                                0x190,
                                0x20,
                                0x0
                            }
                        });
                        */

                        _memoryHandler.Scanner.LoadOffsets(_sList.ToArray());

                        Thread.Sleep(100);

                        if (_memoryHandler.Scanner.Locations.ContainsKey("WEATHER"))
                        {
                            Debug.WriteLine("Initializing WEATHER done: " +
                                            _memoryHandler.Scanner.Locations["WEATHER"].GetAddress().ToInt64()
                                                .ToString("X"));

                            _siginit = true;
                        }

                        if (_siginit)
                            _memoryready = true;
                    }

                if (_memoryready)
                {
                    if (_memoryHandler.Scanner.Locations.ContainsKey("WEATHER"))
                    {
                        var address = _memoryHandler.Scanner.Locations["WEATHER"];

                        //PluginController.debug(" " + address.ToString("X8"));
                        _weatherIconID = _memoryHandler.GetInt32(address.GetAddress(), 0);

                        
                        _initialized = true;
                        //Debug.WriteLine(_weatherIconID);
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

        public static int WeatherIconID()
        {
            if (!_initialized)
                return 0;

            CheckCache();

            return _weatherIconID;
        }

        public static string WeatherIconName(int id)
        {
            if (!_initialized)
                return "Unknown";

            CheckCache();
            if (WeatherMap.ContainsKey(id))
            {
                return WeatherMap[id];
            }

            return "Unknown";
        }

    }
}