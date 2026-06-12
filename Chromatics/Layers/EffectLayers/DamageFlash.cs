using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using Sharlayan.Core.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using Color = RGB.NET.Core.Color;
using System.Linq;

namespace Chromatics.Layers
{
    public class DamageFlashProcessor : LayerProcessor
    {
        private static DamageFlashProcessor _instance;
        private static Dictionary<int, DamageFlashEffectModel> layerProcessorModel = new Dictionary<int, DamageFlashEffectModel>();
        private bool _disposed = false;

        // Private constructor to prevent direct instantiation
        private DamageFlashProcessor() { }

        // Singleton instance access
        public static DamageFlashProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DamageFlashProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            // Do not apply if layer/effect is disabled
            var effectSettings = RGBController.GetEffectsSettings();

            DamageFlashEffectModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new DamageFlashEffectModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            if (!layer.Enabled || !effectSettings.effect_damageflash || !MappingLayers.IsDeviceEffectsEnabled(layer.deviceGuid))
            {
                model.wasDisabled = true;
                return;
            }

            // Damage Flash Effect Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();

            // Own group at the top of the effect z-order: the flash is the
            // "you're dying" signal, so it composites over the bell, the
            // cutscene gradient, and every raid overlay. Idle brush stays
            // Color.Transparent, which Color+ short-circuits to a no-op, so
            // the pin has no visible effect outside a flash event. Rebuilt
            // whenever the generic cleanup in GameController evicted it from
            // the live registry so LED reassignments are picked up.
            ListLedGroup layergroup = model.layergroup;
            bool registered = layergroup != null
                && _layergroups.TryGetValue(layer.layerID, out var registeredGroups)
                && Array.IndexOf(registeredGroups, layergroup) >= 0;
            if (!registered)
            {
                layergroup?.RemoveAllDecorators();
                layergroup?.Detach();
                layergroup = new ListLedGroup(surface, GetLedArray(layer))
                {
                    ZIndex = EffectZIndex.DamageFlash,
                };
                layergroup.Detach();
                model.layergroup = layergroup;
                RGBController.RegisterLiveLayerGroup(layer.layerID, layergroup);
            }

            var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.DamageFlashAnimation.Color);

            // Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                if (getCurrentPlayer.Entity.HPCurrent != model.currentHp)
                {
                    if (getCurrentPlayer.Entity.HPCurrent < model.currentHp && getCurrentPlayer.Entity.Job == model.currentJob && !model.wasDisabled)
                    {
                        var damageDelta = model.currentHp - getCurrentPlayer.Entity.HPCurrent;
                        var hpMax = Math.Max(1, getCurrentPlayer.Entity.HPMax);
                        var damageRatio = (float)damageDelta / hpMax;

                        // Filter likely auto-attacks: small frequent ticks under
                        // ~1.5% HPMax. Sharlayan's GetCurrentPlayer doesn't expose
                        // the action source so this is a heuristic — tank auto-
                        // attack ticks land around 0.5-1.5%, while real ability
                        // damage (busters, AoEs, DoT spikes) is well above.
                        const float autoAttackThreshold = 0.015f;
                        if (damageRatio >= autoAttackThreshold)
                        {
                            // Opacity 1.0 so peak-envelope renders at full alpha. RGB.NET's
                            // Led.Color setter alpha-blends via Color+, so a 0.5 default would
                            // only produce a ~50% tint over a bright raid decorator write —
                            // visually swamped. ShotFlashDecorator modulates A through its
                            // ADSR envelope, ramping in/out smoothly.
                            var highlight_brush = new SolidColorBrush(highlight_col)
                            {
                                Opacity = 1.0f
                            };

                            var flash = new ShotFlashDecorator(surface)
                            {
                                IsEnabled = true,
                                Order = 100,
                                Attack = 0.15f,
                                Release = 0.15f,
                                Sustain = 0.1f,
                                Repetitions = 1
                            };

                            // Scale flash opacity depending on how much damage taken. More damage = brighter flash
                            if (effectSettings.effect_damageflash_scaledamage)
                            {
                                if (damageRatio > 1) damageRatio = 1;
                                if (damageRatio < 0) damageRatio = 0;

                                var minOpacity = effectSettings.effect_damageflash_min_flash;
                                if (minOpacity > 1) minOpacity = 1;
                                if (minOpacity < 0) minOpacity = 0;

                                var opacity = Math.Max((float)minOpacity, (float)damageRatio);
                                highlight_brush.Opacity = opacity;
                            }

                            highlight_brush.AddDecorator(flash);
                            layergroup.Brush = highlight_brush;
                            model.activeBrush = highlight_brush;
                        }
                    }

                    model.currentHp = getCurrentPlayer.Entity.HPCurrent;
                    model.currentJob = getCurrentPlayer.Entity.Job;
                    model.wasDisabled = false;
                }
            }

            // Apply lighting
            if (model.activeBrush != null && model.activeBrush.Decorators.Count == 0)
            {
                layergroup.Brush = new SolidColorBrush(Color.Transparent);
                model.activeBrush = null;
            }

            if (layergroup.Decorators.Count == 0)
            {
                layergroup.Attach(surface);
            }

            model.init = true;
            layer.requestUpdate = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    foreach (var model in layerProcessorModel.Values)
                    {
                        model.activeBrush = null;
                    }
                    layerProcessorModel.Clear();
                    var _layergroups = RGBController.GetLiveLayerGroups();
                    foreach (var layergroup in _layergroups.Values.SelectMany(lg => lg))
                    {
                        layergroup?.Detach();
                    }
                    _layergroups.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }

        private class DamageFlashEffectModel
        {
            public int currentHp { get; set; }
            public Actor.Job currentJob { get; set; }
            public bool wasDisabled { get; set; }
            public SolidColorBrush activeBrush { get; set; }
            public ListLedGroup layergroup { get; set; }
            public bool init { get; set; }
        }
    }
}
