using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Chromatics.Layers.DynamicLayers
{
    public class ReactiveWeatherHighlightProcessor : LayerProcessor
    {
        private static ReactiveWeatherHighlightProcessor _instance;
        private static Dictionary<int, ReactiveWeatherHighlightDynamicLayer> layerProcessorModel = new Dictionary<int, ReactiveWeatherHighlightDynamicLayer>();

        private bool _disposed = false;
        private SolidColorBrush weather_brush;

        // Private constructor to prevent direct instantiation
        private ReactiveWeatherHighlightProcessor() { }

        // Singleton instance access
        public static ReactiveWeatherHighlightProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ReactiveWeatherHighlightProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

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

            // Reactive Weather Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var weather_color = ColorHelper.ColorToRGBColor(_colorPalette.WeatherUnknownHighlight.Color);

            weather_brush = new SolidColorBrush(weather_color);

            var _layergroups = RGBController.GetLiveLayerGroups();
            // Reactive-weather highlight is the animated overlay variant —
            // gated on both the global flag and the per-device EffectLayer
            // toggle so unticking effects on the Mappings tab silences it.
            var reactiveWeatherEffects = RGBController.GetEffectsSettings().effect_reactiveweather
                                         && MappingLayers.IsDeviceEffectsEnabled(layer.deviceGuid);

            ListLedGroup layergroup;
            var ledArray = GetLedArray(layer);

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;

                if (layer.requestUpdate)
                {
                    layergroup.RemoveLeds(layergroup);
                    layergroup.AddLeds(ledArray);
                }
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

            if (!layer.Enabled)
            {
                layergroup.Detach();
                return;
            }
            else
            {
                // Process data from FFXIV
                var _memoryHandler = GameController.GetGameData();

                if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
                {
                    var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                    if (getCurrentPlayer.Entity != null)
                    {
                        var currentZone = GameHelper.GetZoneNameById(getCurrentPlayer.Entity.MapTerritory);

                        // Single-per-tick snapshot of game state.
                        var gameState = _memoryHandler.Reader.GetGameState();
                        bool inInstance = gameState.InInstance;

                        if (currentZone != "???" && currentZone != "")
                        {
                            var currentWeather = gameState.CurrentWeatherName;
                            if (!string.IsNullOrEmpty(currentWeather) && currentWeather != "CutScene" &&
                                (model._currentWeather != currentWeather || model._currentZone != currentZone || model._reactiveWeatherEffects != reactiveWeatherEffects || layer.requestUpdate || model._inInstance != inInstance))
                            {
                                SetReactiveWeather(layergroup, currentZone, currentWeather, weather_brush, _colorPalette);

                                model._currentWeather = currentWeather;
                                model._currentZone = currentZone;
                                model._inInstance = inInstance;
                            }
                        }
                    }
                }
            }

            // Apply lighting
            if (model._reactiveWeatherEffects != reactiveWeatherEffects)
            {
                model._reactiveWeatherEffects = reactiveWeatherEffects;
            }

            layergroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;
        }

        // Raid highlight handling lives in RaidEffectHighlightProcessor now;
        // this method only resolves the weather highlight colour. The raid
        // overlay (run separately by GameProcessLayers) sits at a higher
        // ZIndex and visually overrides whatever colour we set here.
        private static void SetReactiveWeather(ListLedGroup layer, string zone, string weather, SolidColorBrush weather_brush, PaletteColorModel _colorPalette)
        {
            var color = GetWeatherColor(weather, _colorPalette);
            var reactiveWeatherEffects = RGBController.GetEffectsSettings();

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
                    else if (weather == "Astromagnetic Storms")
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

            // Apply Standard Lookup Weather
            weather_brush.Color = color;
            layer.Brush = weather_brush;
        }

        private class ReactiveWeatherHighlightDynamicLayer
        {
            public string _currentWeather { get; set; }
            public string _currentZone { get; set; }
            public bool _reactiveWeatherEffects { get; set; }
            public bool _inInstance { get; set; }
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

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    weather_brush = null;
                    layerProcessorModel.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }
    }
}
