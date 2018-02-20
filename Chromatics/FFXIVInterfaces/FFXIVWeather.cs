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

        

        public static void RefreshData()
        {
            lock (RefreshLock)
            {
                if (!_memoryready)
                    if (!Scanner.Instance.Locations.ContainsKey("WEATHER") || !_siginit)
                    {
                        _sList = new List<Signature>();

                        _sList.Add(new Signature
                        {
                            Key = "WEATHER",
                            PointerPath = new List<long>
                            {
                                0x018AE778,
                                0x38,
                                0x10,
                                0x190,
                                0x20,
                                0x0
                            }
                        });

                        Scanner.Instance.LoadOffsets(_sList);

                        Thread.Sleep(100);

                        if (Scanner.Instance.Locations.ContainsKey("WEATHER"))
                        {
                            Debug.WriteLine("Initializing WEATHER done: " +
                                            Scanner.Instance.Locations["WEATHER"].GetAddress().ToInt64()
                                                .ToString("X"));

                            _siginit = true;
                        }

                        if (_siginit)
                            _memoryready = true;
                    }

                if (_memoryready)
                {
                    if (Scanner.Instance.Locations.ContainsKey("WEATHER"))
                    {
                        var address = Scanner.Instance.Locations["WEATHER"];

                        //PluginController.debug(" " + address.ToString("X8"));
                        _weatherIconID = MemoryHandler.Instance.GetInt32(address.GetAddress(), 0);

                        
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