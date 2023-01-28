using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using Sharlayan.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Chromatics.Extensions.FFXIVWeatherExtensions;

namespace Chromatics.Layers.DynamicLayers
{
    public class ReactiveWeatherHighlightProcessor : LayerProcessor
    {
        private FFXIVWeatherServiceManual weatherService;
        private string _currentWeather = @"";

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;
            
            //Reactive Weather Dynamic Layer Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var weather_color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherUnknownHighlight.Color);
            var weather_brush = new SolidColorBrush(weather_color);
            var _layergroups = RGBController.GetLiveLayerGroups();


            if (FileOperationsHelper.CheckWeatherDataLoaded() && weatherService == null)
            {
                weatherService = new FFXIVWeatherServiceManual();
            }

            //loop through all LED's and assign to device layer (Order of LEDs is not important for a base layer)
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);

            PublicListLedGroup layergroup;
            var ledArray = devices.SelectMany(d => d).Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;
            }
            else
            {
                layergroup = new PublicListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new PublicListLedGroup[] { layergroup };
                _layergroups.Add(layer.layerID, lg);

                layergroup.Brush = weather_brush;
                layergroup.Detach();
            }

            if (!layer.Enabled)
            {
                layergroup.Detach();
                return;
            }
            else
            {
                //Process data from FFXIV
                
                var _memoryHandler = GameController.GetGameData();

                if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
                {
                    var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                    if (getCurrentPlayer.Entity == null) return;

                    var currentZone = ZoneLookup.GetZoneInfo(getCurrentPlayer.Entity.MapTerritory).Name.English;

                    if (currentZone != "???" && currentZone != "")
                    {
                        var currentWeather = weatherService.GetCurrentWeather(currentZone).Item1.ToString();

                        if (_currentWeather != currentWeather || layer.requestUpdate)
                        {
                            //layergroup.Brush = weather_brush;
                            SetReactiveWeather(layergroup, currentZone, currentWeather, weather_brush, _colorPalette);

                            _currentWeather = currentWeather;
                        }
                    }
                }
                
            }
            

            //Apply lighting
            layergroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;

        }

        private static void SetReactiveWeather(PublicListLedGroup layer, string zone, string weather, SolidColorBrush weather_brush, PaletteColorModel _colorPalette)
        {
            var color = GetWeatherColor(weather, _colorPalette);

            weather_brush.Color = color;
            layer.Brush = weather_brush;
        }

        public static Color GetWeatherColor(string weatherType, PaletteColorModel colorPalette)
        {
            var paletteType = typeof(PaletteColorModel);
            var fieldName = @"Weather" + weatherType.Replace(" ", "") + @"Highlight";
            var fieldInfo = paletteType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                Debug.WriteLine($"Unknown Weather Color Model: {fieldName}");
                return ColorHelper.ColorToRGBColor(colorPalette.WeatherUnknownHighlight.Color);
            }

            var colorMapping = (ColorMapping)fieldInfo.GetValue(colorPalette);
            return ColorHelper.ColorToRGBColor(colorMapping.Color);
        }
    }
}
