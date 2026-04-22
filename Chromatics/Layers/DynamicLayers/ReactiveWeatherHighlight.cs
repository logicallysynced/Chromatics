using Chromatics.Core;
using Chromatics.Extensions;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.RGB.NET.Decorators;
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

namespace Chromatics.Layers.DynamicLayers
{
    public class ReactiveWeatherHighlightProcessor : LayerProcessor
    {
        private static ReactiveWeatherHighlightProcessor _instance;
        private static Dictionary<int, ReactiveWeatherHighlightDynamicLayer> layerProcessorModel = new Dictionary<int, ReactiveWeatherHighlightDynamicLayer>();

        internal static bool dutyComplete = false;
        internal static bool raidEffectsRunning = false;
        // BGM the active raid effect was built for. Mirrors the base-layer
        // pattern in ReactiveWeatherProcessor so highlight raid cases can
        // also opt into phase-based switching.
        internal static uint currentRaidBgmId = 0;
        // Victory Fanfare BGM ID — same constant as the base layer.
        internal const uint VictoryBgmId = 18;

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
            var reactiveWeatherEffects = RGBController.GetEffectsSettings().effect_reactiveweather;
            var raidEffects = RGBController.GetEffectsSettings().effect_raideffects;

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

                        // Single-per-tick snapshot of game state (InInstance + current weather + name + BGM).
                        var gameState = _memoryHandler.Reader.GetGameState();
                        bool inInstance = gameState.InInstance;
                        uint currentBgmId = gameState.CurrentBgmId;

                        // Victory Fanfare detection — replaces the prior chat-scan
                        // path. See ReactiveWeatherProcessor for the same pattern.
                        if (currentBgmId == VictoryBgmId && raidEffectsRunning)
                        {
                            dutyComplete = true;
                            raidEffectsRunning = false;
                            currentRaidBgmId = 0;
                        }

                        // Out-of-instance reset so the next duty starts clean.
                        if (!inInstance)
                        {
                            raidEffectsRunning = false;
                            currentRaidBgmId = 0;
                            dutyComplete = false;
                        }

                        if (currentZone != "???" && currentZone != "")
                        {
                            var currentWeather = gameState.CurrentWeatherName;
                            if (!string.IsNullOrEmpty(currentWeather) && currentWeather != "CutScene" &&
                                (model._currentWeather != currentWeather || model._currentZone != currentZone || model._reactiveWeatherEffects != reactiveWeatherEffects || model._raidEffects != raidEffects || layer.requestUpdate || model._inInstance != inInstance || model._dutyComplete != dutyComplete || model._currentBgmId != currentBgmId))
                            {
                                SetReactiveWeather(layergroup, currentZone, currentWeather, weather_brush, _colorPalette, inInstance, currentBgmId);

                                model._currentWeather = currentWeather;
                                model._currentZone = currentZone;
                                model._inInstance = inInstance;
                                model._dutyComplete = dutyComplete;
                                model._currentBgmId = currentBgmId;
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

            if (model._raidEffects != raidEffects)
            {
                model._raidEffects = raidEffects;
            }

            layergroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;
        }

        private static void SetReactiveWeather(ListLedGroup layer, string zone, string weather, SolidColorBrush weather_brush, PaletteColorModel _colorPalette, bool inInstance, uint currentBgmId)
        {
            var color = GetWeatherColor(weather, _colorPalette);
            var reactiveWeatherEffects = RGBController.GetEffectsSettings();
            var effectSettings = RGBController.GetEffectsSettings();

            // Filter for zone specific special weather
            if (inInstance && !dutyComplete)
            {
                switch (zone)
                {
                    case "Summit of Everkeep":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectEverkeepKeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "Interphos":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectInterphosKeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "Scratching Ring":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM1KeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "Lovely Lovering":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM2KeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "Blasting Ring":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM3KeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "The Thundering":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM4KeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "Sphere of Naught":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM4KeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "Groovy Ring":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM5KeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "Rebel Ring":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM6KeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    case "Demolition Site":
                        if (effectSettings.effect_raideffects)
                        {
                            color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7KeyHighlight.Color);
                            raidEffectsRunning = true;
                        }
                        break;
                    // Demo of BGM-based phase switching for highlight raid effects.
                    // Mirrors the base-layer demo at "Hunter's Ring" / "Hunting Ground".
                    // Picks a different highlight colour per phase based on the in-game
                    // BGM id, and updates currentRaidBgmId so the next tick's snapshot
                    // comparison treats the new phase as the established state.
                    case "Hunter's Ring":
                    case "Hunting Ground":
                        if (effectSettings.effect_raideffects)
                        {
                            switch (currentBgmId)
                            {
                                // Phase 2 highlight
                                case 999u: // TODO: replace with phase-2 BGM ID
                                    color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Highlight2.Color);
                                    break;
                                // Phase 3 / enrage highlight
                                case 998u: // TODO: replace with phase-3 BGM ID
                                    color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7Highlight3.Color);
                                    break;
                                // Phase 1 / default highlight
                                default:
                                    color = ColorHelper.ColorToRGBColor(_colorPalette.RaidEffectM7KeyHighlight.Color);
                                    break;
                            }
                            raidEffectsRunning = true;
                            currentRaidBgmId = currentBgmId;
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

            // Apply Standard Lookup Weather
            weather_brush.Color = color;
            layer.Brush = weather_brush;
        }

        private class ReactiveWeatherHighlightDynamicLayer
        {
            public string _currentWeather { get; set; }
            public string _currentZone { get; set; }
            public bool _raidEffects { get; set; }
            public bool _reactiveWeatherEffects { get; set; }
            public bool _inInstance { get; set; }
            public bool _dutyComplete { get; set; }
            public uint _currentBgmId { get; set; }
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
