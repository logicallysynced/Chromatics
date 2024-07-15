using FFXIVWeather.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Models
{
    public class WeatherData
    {
        public List<WeatherRateIndex> WeatherRateIndices { get; set; }
        public List<TerriType> TerriTypes { get; set; }
        public List<Weather> WeatherKinds { get; set; }
    }
}
