using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using Cyotek.Windows.Forms;
using FFXIVWeather;
using RGB.NET.Core;
using Sharlayan.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static Chromatics.Extensions.FFXIVWeatherExtensions;
using static MetroFramework.Drawing.MetroPaint;

namespace Chromatics.Layers
{
    public class ReactiveWeatherProcessor : LayerProcessor
    {
        private FFXIVWeatherServiceManual weatherService;
        private string _currentWeather = @"";
        private string _currentZone = @"";
        private bool _reactiveWeatherEffects;

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview())
            {
                //StopEffects(layergroup);
                return;
            }
            
            //Reactive Weather Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var reactiveWeatherEffects = RGBController.GetEffectsSettings().effect_reactiveweather;
            var weather_color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherUnknownBase.Color);
            var weather_brush = new SolidColorBrush(weather_color);
            var _layergroups = RGBController.GetLiveLayerGroups();
            var effectApplied = false;

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

            if (_reactiveWeatherEffects != reactiveWeatherEffects)
            {
                if (!reactiveWeatherEffects)
                {
                    StopEffects(layergroup);
                }
            }
            

            if (!layer.Enabled)
            {
                StopEffects(layergroup);
                weather_brush.Color = ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
                layergroup.Brush = weather_brush;
                //layergroup.Detach();
                //return;
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
                    //Debug.WriteLine($"Zone: {currentZone}");

                    if (currentZone != "???" && currentZone != "")
                    {
                        var currentWeather = weatherService.GetCurrentWeather(currentZone).Item1.ToString();

                        if (_currentWeather != currentWeather || _currentZone != currentZone || _reactiveWeatherEffects != reactiveWeatherEffects || layer.requestUpdate)
                        {
                            //layergroup.Brush = weather_brush;
                            effectApplied = SetReactiveWeather(layergroup, currentZone, currentWeather, weather_brush, _colorPalette, surface, ledArray);

                            Debug.WriteLine($"Zone Lookup: {currentZone}. Weather: {currentWeather}");

                            _currentWeather = currentWeather;
                            _currentZone = currentZone;
                        }
                    }
                }
                
            }
            

            //Apply lighting
            if (_reactiveWeatherEffects != reactiveWeatherEffects)
            {
                _reactiveWeatherEffects = reactiveWeatherEffects;
            }

            if (!effectApplied && layergroup.Decorators.Count <= 0)
            {
                layergroup.Attach(surface);
            }
                
            
            _init = true;
            layer.requestUpdate = false;

        }

        private static void SetEffect(ILedGroupDecorator effect, PublicListLedGroup layer)
        {
            var runningEffects = RGBController.GetRunningEffects();

            if (runningEffects.Contains(layer))
                runningEffects.Remove(layer);

            layer.RemoveAllDecorators();
            layer.AddDecorator(effect);

            runningEffects.Add(layer);
        }

        private static void StopEffects(PublicListLedGroup layer)
        {
            var runningEffects = RGBController.GetRunningEffects();

            if (runningEffects.Contains(layer))
                runningEffects.Remove(layer);

            layer.RemoveAllDecorators();
        }

        private static bool SetReactiveWeather(PublicListLedGroup layer, string zone, string weather, SolidColorBrush weather_brush, PaletteColorModel _colorPalette, RGBSurface surface, Led[] ledArray)
        {
            var reactiveWeatherEffects = RGBController.GetEffectsSettings().effect_reactiveweather;
            var color = GetWeatherColor(weather, _colorPalette);
            

            
            //Filter for zone specific special weather
            switch(zone)
            {
                case "Mare Lamentorum":
                    if (weather == "Fair Skies" || weather == "Moon Dust" || weather == "Umbral Wind")
                    {
                        if (reactiveWeatherEffects)
                        {
                            var baseCol = ColorHelper.ColorToRGBColor(_colorPalette.WeatherMoonDustAnimationBase.Color);
                            var animationCol = new Color[] { ColorHelper.ColorToRGBColor(_colorPalette.WeatherMoonDustAnimationHighlight.Color) };
                            var starfield = new StarfieldDecorator(layer, (layer.PublicGroupLeds.Count / 4), 20, 900, animationCol, surface, false, baseCol);
                            
                            layer.Brush = new SolidColorBrush(baseCol);
                            SetEffect(starfield, layer);

                            return true;
                        }
                        else
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherMoonDustBase.Color);
                        }
                    }
                    /*
                    else if (weather == "Umbral Wind")
                    {
                        if (reactiveWeatherEffects)
                        {
                            //return;
                        }
                    }
                    */
                    break;
            }

            //Filter for special weather
            switch(weather)
            {
                default:
                    //Apply Standard Lookup Weather
                    StopEffects(layer);
                    weather_brush.Color = color;
                    layer.Brush = weather_brush;
                    break;
            }

            return false;
        }

        public static Color GetWeatherColor(string weatherType, PaletteColorModel colorPalette)
        {
            var paletteType = typeof(PaletteColorModel);
            var fieldName = @"Weather" + weatherType.Replace(" ", "") + @"Base";
            var fieldInfo = paletteType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                Debug.WriteLine($"Unknown Weather Color Model: {fieldName}");
                return ColorHelper.ColorToRGBColor(colorPalette.WeatherUnknownBase.Color);
            }

            var colorMapping = (ColorMapping)fieldInfo.GetValue(colorPalette);

            return ColorHelper.ColorToRGBColor(colorMapping.Color);
        }
    }
}
