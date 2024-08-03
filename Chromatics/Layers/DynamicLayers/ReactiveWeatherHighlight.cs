using Chromatics.Core;
using Chromatics.Extensions;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Extensions.Sharlayan;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures.Gradients;
using Sharlayan.Models.ReadResults;
using Sharlayan.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Chromatics.Layers.DynamicLayers
{
    public class ReactiveWeatherHighlightProcessor : LayerProcessor
    {
        private static Dictionary<int, ReactiveWeatherHighlightDynamicLayer> layerProcessorModel = new Dictionary<int, ReactiveWeatherHighlightDynamicLayer>();

        int _previousArrayIndex = 0;
        int _previousOffset = 0;
        bool dutyComplete = false;

        public override void Process(IMappingLayer layer)
        {
            ReactiveWeatherHighlightDynamicLayer model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new ReactiveWeatherHighlightDynamicLayer();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            //Reactive Weather Dynamic Layer Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var weather_color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherUnknownHighlight.Color);
            var weather_brush = new SolidColorBrush(weather_color);
            var _layergroups = RGBController.GetLiveLayerGroups();
            var reactiveWeatherEffects = RGBController.GetEffectsSettings().effect_reactiveweather;
            var raidEffects = RGBController.GetEffectsSettings().effect_raideffects;


            var weatherService = FFXIVWeatherExtensions.GetWeatherService();
            if (weatherService == null) return;

            //loop through all LED's and assign to device layer  (Order of LEDs is not important for a highlight layer)
            var surface = RGBController.GetLiveSurfaces();
            var device = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == layer.deviceGuid).Value;

            ListLedGroup layergroup;
            var ledArray = device.Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;
            }
            else
            {
                layergroup = new ListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new ListLedGroup[] { layergroup };
                _layergroups.Add(layer.layerID, lg);

                layergroup.Brush = weather_brush;
                layergroup.Detach();
            }

            if (layer.requestUpdate)
            {
                foreach (var led in layergroup)
                {
                    layergroup.RemoveLed(led);
                }

                layergroup.AddLeds(ledArray);
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

                    //var currentZone = ZoneLookup.GetZoneInfo(getCurrentPlayer.Entity.MapTerritory).Name.English;
                    var currentZone = GameHelper.GetZoneNameById(getCurrentPlayer.Entity.MapTerritory);

                    DutyFinderBellExtension.CheckCache();


                    if (DutyFinderBellExtension.InInstance())
                    {
                        ChatLogResult readResult = _memoryHandler.Reader.GetChatLog(_previousArrayIndex, _previousOffset);

                        var chatLogEntries = readResult.ChatLogItems;

                        if (readResult.PreviousArrayIndex != _previousArrayIndex)
                        {
                            if (chatLogEntries.Count > 0)
                            {
                                if (chatLogEntries.First().Code == "0840" && chatLogEntries.First().Message.Contains("completion time"))
                                {
                                    
                                    dutyComplete = true;
                                }

                                if (chatLogEntries.First().Code == "0839" && chatLogEntries.First().Message.Contains("has begun."))
                                {
                                    
                                    dutyComplete = false;
                                }

                                if (chatLogEntries.First().Message.Contains("Allagan tomestones of") && Regex.IsMatch(chatLogEntries.First().Message, @"You obtain \d+ Allagan tomestones of \w+\."))
                                {
                                    
                                    dutyComplete = true;
                                }

                            }

                            _previousArrayIndex = readResult.PreviousArrayIndex;
                            _previousOffset = readResult.PreviousOffset;
                        }
                    }
                    else
                    {
                        dutyComplete = false;
                    }

                    if (currentZone != "???" && currentZone != "")
                    {
                        var currentWeatherZone = WeatherExtension.WeatherId();
                        var currentWeather = WeatherHelper.GetWeatherNameById(currentWeatherZone);

                        if (currentWeather == null)
                        {
                            currentWeather = weatherService.GetCurrentWeather(currentZone).Item1.ToString();
                        }

                        if ((model._currentWeather != currentWeather || model._currentZone != currentZone || model._reactiveWeatherEffects != reactiveWeatherEffects || model._raidEffects != raidEffects  || layer.requestUpdate || model._inInstance != DutyFinderBellExtension.InInstance() || model._dutyComplete != dutyComplete) && currentWeather != "CutScene")
                        {
                            //layergroup.Brush = weather_brush;
                            SetReactiveWeather(layergroup, currentZone, currentWeather, weather_brush, _colorPalette, DutyFinderBellExtension.InInstance(), dutyComplete);

                            model._currentWeather = currentWeather;
                            model._currentZone = currentZone;
                            model._inInstance = DutyFinderBellExtension.InInstance();
                            model._dutyComplete = dutyComplete;
                        }
                    }

                }
                
            }
            

            //Apply lighting
            if (model._reactiveWeatherEffects != reactiveWeatherEffects)
            {
                model._reactiveWeatherEffects = reactiveWeatherEffects;
            }

            if (model._raidEffects != raidEffects)
            {
                model._raidEffects = raidEffects;
            }

            layergroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;

        }

        private static void SetReactiveWeather(ListLedGroup layer, string zone, string weather, SolidColorBrush weather_brush, PaletteColorModel _colorPalette, bool inInstance, bool dutyComplete)
        {
            var color = GetWeatherColor(weather, _colorPalette);
            var reactiveWeatherEffects = RGBController.GetEffectsSettings();
            var effectSettings = RGBController.GetEffectsSettings();

            //Filter for zone specific special weather
            if (inInstance && !dutyComplete)
            {
                switch (zone)
                {
                    case "Everkeep":
                        //Raid Zone Effect
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectEverkeepKeyHighlight.Color);
                        }
                        break;
                    case "Interphos":

                        //Raid Zone Effect
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectInterphosKeyHighlight.Color);
                        }
                        break;
                    case "Scratching Ring":
                        //Raid Zone Effect
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM1KeyHighlight.Color);
                        }
                        break;
                    case "Lovely Lovering":
                        //Raid Zone Effect
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM2KeyHighlight.Color);
                        }
                        break;
                    case "Blasting Ring":
                        //Raid Zone Effect
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM3KeyHighlight.Color);
                        }
                        break;
                    case "The Thundering":
                        //Raid Zone Effect
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM4KeyHighlight.Color);
                        }
                        break;
                }
            }

            switch (zone)
            {
                case "Mare Lamentorum":
                    if (reactiveWeatherEffects.effect_reactiveweather && (reactiveWeatherEffects.weather_marelametorum_animation || reactiveWeatherEffects.weather_marelametorum_umbralwind_animation))
                        color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherMoonDustBase.Color);
                    else
                        color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherMoonDustHighlight.Color);
                    break;
                case "Ultima Thule":
                    if (weather == "Fair Skies")
                    {
                        if (reactiveWeatherEffects.effect_reactiveweather && reactiveWeatherEffects.weather_ultimathule_animation)
                            color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherUltimaThuleAnimationHighlight.Color);
                        else
                            color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherUltimaThuleAnimationHighlight.Color);
                    }
                    else if (weather == "Astromagnetic Storm")
                    {
                        if (reactiveWeatherEffects.effect_reactiveweather && reactiveWeatherEffects.weather_astromagneticstorm_animation)
                            color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherAstromagneticStormHighlight.Color);
                        else
                            color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherAstromagneticStormHighlight.Color);
                    }
                    else if (weather == "Umbral Wind")
                    {
                        if (reactiveWeatherEffects.effect_reactiveweather && reactiveWeatherEffects.weather_ultimathule_umbralwind_animation)
                            color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherUltimaThuleAnimationHighlight.Color);
                        else
                            color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherUltimaThuleAnimationHighlight.Color);
                    }
                    break;
            }

            //Filter for special weather
            switch(weather)
            {
                default:
                    //Apply Standard Lookup Weather
                    weather_brush.Color = color;
                    layer.Brush = weather_brush;
                    break;
            }

            
        }

        private class ReactiveWeatherHighlightDynamicLayer
        {
            public string _currentWeather { get; set; }
            public string _currentZone { get; set; }
            public bool _raidEffects { get; set; }
            public bool _reactiveWeatherEffects { get; set; }
            public bool _inInstance { get; set; }
            public bool _dutyComplete { get; set; }


        }

        public static RGB.NET.Core.Color GetWeatherColor(string weatherType, PaletteColorModel colorPalette)
        {
            var paletteType = typeof(PaletteColorModel);
            var fieldName = @"Weather" + weatherType.Replace(" ", "") + @"Highlight";
            var fieldInfo = paletteType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                #if DEBUG
                    Debug.WriteLine($"Unknown Weather Color Model: {fieldName}");
                #endif

                return ColorHelper.ColorToRGBColor(colorPalette.WeatherUnknownHighlight.Color);
            }

            var colorMapping = (ColorMapping)fieldInfo.GetValue(colorPalette);
            return ColorHelper.ColorToRGBColor(colorMapping.Color);
        }
    }
}
