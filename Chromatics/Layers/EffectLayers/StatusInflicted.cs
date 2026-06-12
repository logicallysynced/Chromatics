using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Chromatics.Layers
{
    public class StatusInflictedProcessor : LayerProcessor
    {
        private static StatusInflictedProcessor _instance;
        private static Dictionary<int, StatusInflictedEffectModel> layerProcessorModel = new Dictionary<int, StatusInflictedEffectModel>();
        private bool _disposed = false;

        private const long CooldownMs = 5000;

        // Status names resolved against the StatusEffects palette category.
        // The palette doubles as the detrimental whitelist: a status only
        // fires the pulse when its English name matches an entry here, which
        // filters out beneficial and neutral statuses without a Lumina sheet
        // lookup. Keyed lowercase; values are PaletteColorModel fields read
        // from the ACTIVE palette at trigger time so user edits apply.
        private static readonly Dictionary<string, FieldInfo> _statusPaletteFields = BuildStatusPaletteFields();

        private static Dictionary<string, FieldInfo> BuildStatusPaletteFields()
        {
            var map = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
            var defaults = new PaletteColorModel();
            foreach (var field in typeof(PaletteColorModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType != typeof(ColorMapping)) continue;
                var mapping = (ColorMapping)field.GetValue(defaults);
                if (mapping.Type != Enums.Palette.PaletteTypes.StatusEffects) continue;
                map[mapping.Name] = field;
            }
            return map;
        }

        private StatusInflictedProcessor() { }

        public static StatusInflictedProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StatusInflictedProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            var effectSettings = RGBController.GetEffectsSettings();

            StatusInflictedEffectModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new StatusInflictedEffectModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();

            // Own group pinned just under the cutscene tier.
            ListLedGroup layergroup = model.layergroup;
            bool registered = layergroup != null
                && _layergroups.TryGetValue(layer.layerID, out var registeredGroups)
                && Array.IndexOf(registeredGroups, layergroup) >= 0;
            if (!registered)
            {
                if (model.activeBrush != null)
                {
                    model.activeBrush.RemoveAllDecorators();
                    model.activeBrush = null;
                    model.activeRipple = null;
                }
                layergroup?.Detach();
                layergroup = new ListLedGroup(surface, GetLedArray(layer))
                {
                    ZIndex = EffectZIndex.StatusInflicted,
                    Brush = new SolidColorBrush(Color.Transparent),
                };
                layergroup.Detach();
                model.layergroup = layergroup;
                RGBController.RegisterLiveLayerGroup(layer.layerID, layergroup);
            }

            if (!layer.Enabled || !effectSettings.effect_statusinflicted || !MappingLayers.IsDeviceEffectsEnabled(layer.deviceGuid))
            {
                if (model.activeBrush != null)
                {
                    model.activeBrush.RemoveAllDecorators();
                    layergroup.Brush = new SolidColorBrush(Color.Transparent);
                    model.activeBrush = null;
                    model.activeRipple = null;
                }
                model.knownStatusIds.Clear();
                model.statusBaselineSet = false;
                return;
            }

            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity != null)
                {
                    var statuses = getCurrentPlayer.Entity.StatusItems;

                    // Latest received wins: scan in list order and keep the
                    // last new detrimental match, so a burst of simultaneous
                    // debuffs plays a single pulse in the newest status's
                    // colour.
                    FieldInfo triggeredField = null;
                    var currentIds = new HashSet<short>();

                    foreach (var status in statuses)
                    {
                        if (status == null || status.StatusID <= 0) continue;
                        currentIds.Add(status.StatusID);

                        if (!model.statusBaselineSet) continue;
                        if (model.knownStatusIds.Contains(status.StatusID)) continue;

                        var name = !string.IsNullOrEmpty(status.StatusNameEnglish) ? status.StatusNameEnglish : status.StatusName;
                        if (string.IsNullOrEmpty(name)) continue;

                        if (_statusPaletteFields.TryGetValue(name, out var field))
                            triggeredField = field;
                    }

                    // The first populated tick seeds the baseline without
                    // firing, so statuses already on the player at launch
                    // (food, login buff residue, sustained DoTs) don't pulse.
                    model.knownStatusIds = currentIds;
                    model.statusBaselineSet = true;

                    long now = Environment.TickCount64;
                    if (triggeredField != null && now - model.lastTriggerMs >= CooldownMs)
                    {
                        var mapping = (ColorMapping)triggeredField.GetValue(_colorPalette);
                        var ringColor = ColorHelper.ColorToRGBColor(mapping.Color);

                        model.activeBrush?.RemoveAllDecorators();

                        var brush = new SolidColorBrush(Color.Transparent);
                        var ripple = new RippleBrushDecorator(layergroup, surface, null, 12.0, 2.5, ringColor)
                        {
                            IsEnabled = true,
                        };
                        brush.AddDecorator(ripple);

                        layergroup.Brush = brush;
                        model.activeBrush = brush;
                        model.activeRipple = ripple;
                        model.lastTriggerMs = now;
                    }
                }
            }

            if (model.activeRipple != null && model.activeRipple.IsFinished)
            {
                model.activeBrush?.RemoveAllDecorators();
                layergroup.Brush = new SolidColorBrush(Color.Transparent);
                model.activeBrush = null;
                model.activeRipple = null;
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
                    foreach (var model in layerProcessorModel.Values)
                    {
                        model.activeBrush?.RemoveAllDecorators();
                    }
                    layerProcessorModel.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }

        private class StatusInflictedEffectModel
        {
            public HashSet<short> knownStatusIds { get; set; } = new HashSet<short>();
            public bool statusBaselineSet { get; set; }
            public long lastTriggerMs { get; set; }
            public SolidColorBrush activeBrush { get; set; }
            public RippleBrushDecorator activeRipple { get; set; }
            public ListLedGroup layergroup { get; set; }
            public bool init { get; set; }
        }
    }
}
