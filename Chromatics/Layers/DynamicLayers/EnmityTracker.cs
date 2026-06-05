using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    public class EnmityTrackerProcessor : LayerProcessor
    {
        private static EnmityTrackerProcessor _instance;
        private static Dictionary<int, EnmityDynamicModel> layerProcessorModel = new Dictionary<int, EnmityDynamicModel>();
        private bool _disposed = false;

        // Private constructor to prevent direct instantiation
        private EnmityTrackerProcessor() { }

        // Singleton instance access
        public static EnmityTrackerProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EnmityTrackerProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            EnmityDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new EnmityDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            // Enmity Tracker Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedSortedArray(layer);
            var countKeys = ledArray.Count();

            // Check if layer has been updated or if layer is disabled or if currently in Preview mode    
            if (model.init && (layer.requestUpdate || !layer.Enabled))
            {
                foreach (var layergroup in model._localgroups)
                {
                    layergroup?.Detach();
                }

                model._localgroups.Clear();

                if (!layer.Enabled)
                    return;
            }

            // Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetTargetInfo() && _memoryHandler.Reader.CanGetActors())
            {
                var enmity_top_col = ColorHelper.ColorToRGBColor(_colorPalette.EmnityRed.Color);
                var enmity_high_col = ColorHelper.ColorToRGBColor(_colorPalette.EmnityOrange.Color);
                var enmity_med_col = ColorHelper.ColorToRGBColor(_colorPalette.EmnityYellow.Color);
                var enmity_low_col = ColorHelper.ColorToRGBColor(_colorPalette.EmnityGreen.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.NoEmnity.Color); // Bleed layer

                if (model.enmity_brush == null || model.enmity_brush.Color != enmity_low_col)
                    model.enmity_brush = new SolidColorBrush(enmity_low_col);

                if (model.empty_brush == null || model.empty_brush.Color != empty_col)
                    model.empty_brush = new SolidColorBrush(empty_col);

                if (layer.allowBleed)
                {
                    // Allow bleeding of other layers
                    model.empty_brush.Color = Color.Transparent;
                }
                else
                {
                    model.empty_brush.Color = empty_col;
                }

                var getTargetInfo = _memoryHandler.Reader.GetTargetInfo();
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();

                // Resolve enmityPosition with a default of 0 (no aggro) for
                // every "we don't have the data" case: not in a fight, target
                // is friendly, our actor isn't on the hate table yet, etc.
                // The processor used to `return` on each of those branches -
                // which on the re-enable tick left _localgroups empty (the
                // top cleanup had just cleared it), nothing got attached, and
                // the base layer painted through every selected key. From the
                // user side that looked like "Bleed disabled is not
                // re-applying" because the negative colour they expected on
                // empty cells was wherever the base layer's colour was. The
                // fall-through here keeps the layer painting an empty bar
                // whenever the player has no measurable threat instead of
                // going dark.
                uint enmityPosition = 0;
                uint targetId = 0;

                if (getTargetInfo.TargetInfo != null && getCurrentPlayer.Entity != null)
                {
                    if (getTargetInfo.TargetInfo.CurrentTarget != null)
                        targetId = getTargetInfo.TargetInfo.CurrentTarget.ID;

                    if (targetId != 0)
                    {
                        // Cross-check two sources and take the larger value.
                        // TargetInfo.EnmityItems is the target's hate table -
                        // each entry's ID is the hater's actor ID and the
                        // Enmity field is that hater's percentage on this
                        // target. PlayerInfo.EnmityItems is the player's own
                        // AGGROMAP - each entry's ID is a mob the player has
                        // aggro on, and Enmity is the player's enmity on
                        // that mob. The player-centric list updates the
                        // moment a target swap happens; the target-centric
                        // list can lag by 1 - 2 seconds while FFXIV
                        // re-populates Hate._hateInfo for the new current
                        // target. Without the cross-check, retargeting an
                        // already-engaged enemy flashes the empty colour
                        // (or, with bleed on, the base layer) until the
                        // hate table catches up. Taking max() keeps the
                        // value monotonic across both lists - whichever
                        // updated first wins for that tick, and the second
                        // source catches up within a tick or two without
                        // ever pulling the visual down.
                        var playerId = getCurrentPlayer.Entity.ID;
                        if (getTargetInfo.TargetInfo.EnmityItems != null)
                        {
                            var targetEntry = getTargetInfo.TargetInfo.EnmityItems
                                .FirstOrDefault(item => item.ID == playerId);
                            if (targetEntry != null && targetEntry.Enmity > enmityPosition)
                                enmityPosition = targetEntry.Enmity;
                        }
                        if (getCurrentPlayer.PlayerInfo?.EnmityItems != null)
                        {
                            var playerEntry = getCurrentPlayer.PlayerInfo.EnmityItems
                                .FirstOrDefault(item => item.ID == targetId);
                            if (playerEntry != null && playerEntry.Enmity > enmityPosition)
                                enmityPosition = playerEntry.Enmity;
                        }
                    }
                }

                if (targetId != model._targetId)
                {
                    DetachAndClearGroups(model._localgroups);
                    model._targetReset = true;
                    model._targetId = targetId;
                }

                var currentVal = (int)enmityPosition;
                var minVal = 0;
                var maxVal = 100;

                if (enmityPosition != model._enmityPosition || model._targetReset || layer.requestUpdate)
                {
                    if (enmityPosition == 100)
                        model.enmity_brush.Color = enmity_top_col;
                    else if (enmityPosition >= 80)
                        model.enmity_brush.Color = enmity_high_col;
                    else if (enmityPosition >= 50)
                        model.enmity_brush.Color = enmity_med_col;
                    else
                        model.enmity_brush.Color = enmity_low_col;
                }

                if (model._currentMode != layer.layerModes)
                {
                    DetachAndClearGroups(model._localgroups);
                    model._currentMode = layer.layerModes;
                }

                if (layer.layerModes == Enums.LayerModes.Interpolate)
                {
                    // Bar fill - cells 0..currentVal_Interpolate light up in
                    // the current tier colour; cells past that point fall to
                    // empty_brush (NoEmnity opaque when bleed off, transparent
                    // when bleed on so the base shows through). Tier colour
                    // changes with enmity rising through Minimal / Low / High
                    // / Top.
                    var currentVal_Interpolate = LinearInterpolation.Interpolate(currentVal, minVal, maxVal, 0, countKeys);
                    currentVal_Interpolate = MathHelper.Clamp(currentVal_Interpolate, 0, countKeys);

                    if (currentVal_Interpolate != model._interpolateValue || model._targetReset || layer.requestUpdate)
                    {
                        var ledGroups = new List<ListLedGroup>();
                        for (int i = 0; i < countKeys; i++)
                        {
                            var ledGroup = new ListLedGroup(surface, ledArray[i])
                            {
                                ZIndex = layer.zindex,
                            };
                            ledGroup.Detach();
                            ledGroup.Brush = i < currentVal_Interpolate
                                ? model.enmity_brush
                                : model.empty_brush;
                            ledGroups.Add(ledGroup);
                        }
                        DetachAndClearGroups(model._localgroups);
                        model._localgroups = ledGroups;
                        model._interpolateValue = currentVal_Interpolate;
                    }
                }
                else if (layer.layerModes == Enums.LayerModes.Fade)
                {
                    // Whole-row tier. All selected keys light up with the
                    // current enmity tier colour (Minimal / Low / High / Top)
                    // rather than blending 0 - 100 through the gradient.
                    // Matches the status-indicator shape FFXIV's threat HUD
                    // uses; Interpolate above is the bar-fill alternative
                    // for users who want a magnitude readout.
                    var tier_col = enmityPosition == 0 ? model.empty_brush.Color : model.enmity_brush.Color;
                    if (tier_col != model._faderValue || model._targetReset || layer.requestUpdate)
                    {
                        var ledGroup = new ListLedGroup(surface, ledArray)
                        {
                            ZIndex = layer.zindex,
                            Brush = new SolidColorBrush(tier_col)
                        };
                        ledGroup.Detach();
                        DetachAndClearGroups(model._localgroups);
                        model._localgroups.Add(ledGroup);
                        model._faderValue = tier_col;
                    }
                }

                model._enmityPosition = enmityPosition;

                // Send layers to _layergroups Dictionary to be tracked outside this method
                var lg = model._localgroups.ToArray();

                if (_layergroups.ContainsKey(layer.layerID))
                {
                    _layergroups[layer.layerID] = lg;
                }
                else
                {
                    _layergroups.Add(layer.layerID, lg);
                }
            }

            // Apply lighting
            foreach (var layergroup in model._localgroups)
            {
                layergroup.Attach(surface);
            }

            model.init = true;
            model._targetReset = false;
            layer.requestUpdate = false;
        }

        private void DetachAndClearGroups(List<ListLedGroup> groups)
        {
            foreach (var group in groups)
            {
                group?.Detach();
            }
            groups.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    var _layergroups = RGBController.GetLiveLayerGroups();
                    if (_layergroups != null)
                    {
                        foreach (var layerGroupArray in _layergroups.Values)
                        {
                            foreach (var layerGroup in layerGroupArray)
                            {
                                layerGroup?.Detach();
                            }
                        }
                        _layergroups.Clear();
                    }

                    layerProcessorModel.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }

        private class EnmityDynamicModel
        {
            public List<ListLedGroup> _localgroups { get; set; } = new List<ListLedGroup>();
            public SolidColorBrush empty_brush { get; set; }
            public SolidColorBrush enmity_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; }
            public Color _faderValue { get; set; }
            public uint _enmityPosition { get; set; }
            public uint _targetId { get; set; }
            public bool _targetReset { get; set; }
            public bool init { get; set; }
        }
    }
}
