using Chromatics.Helpers;

namespace Chromatics.Extensions
{
    public static class FFXIVWeatherExtensions
    {
        private static FFXIVWeatherServiceManual weatherService;

        public static FFXIVWeatherServiceManual GetWeatherService()
        {
            if (weatherService == null && FileOperationsHelper.CheckWeatherDataLoaded())
            {
                weatherService = new FFXIVWeatherServiceManual();
            }

            return weatherService;
        }
    }
}
