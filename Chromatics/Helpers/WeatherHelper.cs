using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    public class WeatherHelper
    {
        private static Dictionary<int, string> _weatherCache;
        private static readonly object CacheLock = new object();
        private const string fileName = "weatherKinds.json"; // Set your path here

        public static string GetWeatherNameById(int id)
        {
            // Load and cache the weather data if not already done
            if (_weatherCache == null)
            {
                lock (CacheLock)
                {
                    if (_weatherCache == null) // Double-checked locking
                    {
                        LoadWeatherData();
                    }
                }
            }

            // Return the English name for the given weather ID
            return _weatherCache.TryGetValue(id, out var name) ? name : null;
        }

        private static void LoadWeatherData()
        {
            // Load the JSON file and parse it into a dictionary
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = Path.Combine(enviroment, fileName);
            var weatherDataJson = File.ReadAllText(path);
            var weatherList = JsonConvert.DeserializeObject<List<WeatherData>>(weatherDataJson);
            _weatherCache = new Dictionary<int, string>();

            

            foreach (var weather in weatherList)
            {
                _weatherCache[weather.Id] = weather.name_en;
                //Debug.WriteLine($"Weather ID: {weather.Id}, Name: {weather.name_en}");
            }
        }

        private class WeatherData
        {
            public int Id { get; set; }
            public string name_en { get; set; }
            // Additional fields can be added if needed
        }
    }
}
