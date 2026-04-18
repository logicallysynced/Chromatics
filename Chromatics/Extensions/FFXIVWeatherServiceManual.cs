using FFXIVWeather;
using FFXIVWeather.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Chromatics.Extensions
{
    public class FFXIVWeatherServiceManual : FFXIVWeatherService
    {
        private const double Seconds = 1;
        private const double Minutes = 60 * Seconds;
        private const double WeatherPeriod = 23 * Minutes + 20 * Seconds;

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

        private readonly Weather[] weatherKinds;
        private readonly WeatherRateIndex[] weatherRateIndices;
        private readonly TerriType[] terriTypes;

        public FFXIVWeatherServiceManual()
        {
            this.weatherKinds = LoadFile<Weather[]>("weatherKinds.json");
            this.weatherRateIndices = LoadFile<WeatherRateIndex[]>("weatherRateIndices.json");
            this.terriTypes = LoadFile<TerriType[]>("terriTypes.json");
        }

        public new IList<(Weather, DateTime)> GetForecast(string placeName, uint count = 1, double secondIncrement = WeatherPeriod, double initialOffset = 0 * Minutes, LangKind lang = LangKind.En)
            => GetForecast(GetTerritory(placeName, lang), count, secondIncrement, initialOffset);

        public new IList<(Weather, DateTime)> GetForecast(int terriTypeId, uint count = 1, double secondIncrement = WeatherPeriod, double initialOffset = 0 * Minutes)
            => GetForecast(GetTerritory(terriTypeId), count, secondIncrement, initialOffset);

        public new IList<(Weather, DateTime)> GetForecast(TerriType terriType, uint count = 1, double secondIncrement = WeatherPeriod, double initialOffset = 0 * Minutes)
        {
            if (count == 0) return new (Weather, DateTime)[0];

            var weatherRateIndex = GetTerriTypeWeatherRateIndex(terriType);

            var forecast = new List<(Weather, DateTime)> { GetCurrentWeather(terriType, initialOffset) };

            for (var i = 1; i < count; i++)
            {
                var time = forecast[0].Item2.AddSeconds(i * secondIncrement);
                var weatherTarget = CalculateTarget(time);
                var weather = GetWeather(weatherRateIndex, weatherTarget);
                forecast.Add((weather, time));
            }

            return forecast;
        }

        public new (Weather, DateTime) GetCurrentWeather(string placeName, double initialOffset = 0 * Minutes, LangKind lang = LangKind.En)
            => GetCurrentWeather(GetTerritory(placeName, lang), initialOffset);

        public new (Weather, DateTime) GetCurrentWeather(int terriTypeId, double initialOffset = 0 * Minutes)
            => GetCurrentWeather(GetTerritory(terriTypeId), initialOffset);

        public new (Weather, DateTime) GetCurrentWeather(TerriType terriType, double initialOffset = 0 * Minutes)
        {
            var rootTime = GetCurrentWeatherRootTime(initialOffset);
            var target = CalculateTarget(rootTime);

            var weatherRateIndex = GetTerriTypeWeatherRateIndex(terriType);
            var weather = GetWeather(weatherRateIndex, target);

            return (weather, rootTime);
        }

        private Weather GetWeather(WeatherRateIndex weatherRateIndex, int target)
        {
            var weatherId = weatherRateIndex.Rates.First(w => target < w.Rate).Id;
            var weather = this.weatherKinds[weatherId - 1];
            return weather;
        }

        private WeatherRateIndex GetTerriTypeWeatherRateIndex(TerriType terriType)
        {
            var terriTypeWeatherRateId = terriType.WeatherRate;
            var weatherRateIndex = this.weatherRateIndices[terriTypeWeatherRateId];
            return weatherRateIndex;
        }

        private TerriType GetTerritory(string placeName, LangKind lang)
        {
            var ciPlaceName = placeName.ToLowerInvariant();
            var terriType = this.terriTypes.FirstOrDefault(tt => tt.GetName(lang).ToLowerInvariant() == ciPlaceName);
            if (terriType == null) throw new ArgumentException("Specified place does not exist.", nameof(placeName));
            return terriType;
        }

        private TerriType GetTerritory(int terriTypeId)
        {
            var terriType = this.terriTypes.FirstOrDefault(tt => tt.Id == terriTypeId);
            if (terriType == null) throw new ArgumentException("Specified territory type does not exist.", nameof(terriTypeId));
            return terriType;
        }

        private static DateTime GetCurrentWeatherRootTime(double initialOffset)
        {
            var now = DateTime.UtcNow;
            var adjustedNow = now.AddMilliseconds(-now.Millisecond).AddSeconds(initialOffset);
            var rootTime = adjustedNow;
            var seconds = (long)(rootTime - UnixEpoch).TotalSeconds % WeatherPeriod;
            rootTime = rootTime.AddSeconds(-seconds);
            return rootTime;
        }

        /// <summary>
        ///     Calculate the value used for the <see cref="WeatherRateIndex"/> at a specific <see cref="DateTime" />.
        ///     This method is lifted straight from SaintCoinach.
        /// </summary>
        private static int CalculateTarget(DateTime time)
        {
            var unix = (int)(time - UnixEpoch).TotalSeconds;
            var bell = unix / 175;
            var increment = ((uint)(bell + 8 - (bell % 8))) % 24;
            var totalDays = (uint)(unix / 4200);
            var calcBase = (totalDays * 0x64) + increment;
            var step1 = (calcBase << 0xB) ^ calcBase;
            var step2 = (step1 >> 8) ^ step1;
            return (int)(step2 % 0x64);
        }

        private static T LoadFile<T>(string name)
        {
            var file = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"/{name}";
            using var streamReader = new StreamReader(file);
            return JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd());
        }
    }
}
