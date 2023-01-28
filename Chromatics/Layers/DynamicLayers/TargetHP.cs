using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    public class TargetHPProcessor : LayerProcessor
    {
        private List<PublicListLedGroup> _localgroups = new List<PublicListLedGroup>();
        private SolidColorBrush empty_brush;
        private SolidColorBrush full_brush;
        private LayerModes _currentMode;
        private int _interpolateValue;
        private Color _faderValue;
        private uint _targetId;
        private bool _targetReset;

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            //Target HP Tracker Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            //loop through all LED's and assign to device layer (must maintain order of LEDs)
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);
            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = (from led in devices.SelectMany(d => d).Select((led, index) => new { Index = index, Led = led }) join id in layer.deviceLeds.Values.Select((id, index) => new { Index = index, Id = id }) on led.Led.Id equals id.Id orderby id.Index select led.Led).ToArray();
            
            var countKeys = ledArray.Count();

            //Check if layer has been updated or if layer is disabled or if currently in Preview mode    
            if (_init && (layer.requestUpdate || !layer.Enabled))
            {
                foreach (var layergroup in _localgroups)
                {
                    if (layergroup != null)
                        layergroup.Detach();
                }

                _localgroups.Clear();

                if (!layer.Enabled)
                    return;
            }
            
            //Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetTargetInfo())
            {
                var full_col = ColorHelper.ColorToRGBColor(_colorPalette.TargetHpClaimed.Color);
                var friendly_col = ColorHelper.ColorToRGBColor(_colorPalette.TargetHpFriendly.Color);
                var idle_col = ColorHelper.ColorToRGBColor(_colorPalette.TargetHpIdle.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.TargetHpEmpty.Color); //Bleed layer

                if (full_brush == null || full_brush.Color != full_col) full_brush = new SolidColorBrush(full_col);
                if (empty_brush == null || empty_brush.Color != empty_col) empty_brush = new SolidColorBrush(empty_col);

                if (layer.allowBleed)
                {
                    //Allow bleeding of other layers
                    empty_brush.Color = Color.Transparent;
                }
                else
                {
                    empty_brush.Color = empty_col;
                }

                var getTargetInfo = _memoryHandler.Reader.GetTargetInfo();
                if (getTargetInfo.TargetInfo == null) return;

                uint targetId = 0;

                if (getTargetInfo.TargetInfo.CurrentTarget != null)
                {
                    targetId = getTargetInfo.TargetInfo.CurrentTarget.ID;
                                        
                }

                if (targetId != _targetId)
                {
                    foreach (var layergroup in _localgroups)
                    {
                        if (layergroup != null)
                            layergroup.Detach();
                    }

                    _localgroups.Clear();
                    _targetReset = true;
                    _targetId = targetId;
                    Debug.WriteLine(@"Target Reset");
                }


                //No target found
                if (targetId == 0 || !_init)
                {
                    var ledGroup = new PublicListLedGroup(surface, ledArray)
                    {
                        ZIndex = layer.zindex,
                        Brush = empty_brush
                    };

                    ledGroup.Detach();
                    _localgroups.Add(ledGroup);

                    //Debug.WriteLine($"Target ID: 0");
                }
                else
                {
                    if (getTargetInfo.TargetInfo.CurrentTarget != null)
                    {
                        //Debug.WriteLine($"Target ID: {targetId}");

                        var currentVal = getTargetInfo.TargetInfo.CurrentTarget.HPCurrent;
                        var minVal = 0;
                        var maxVal = getTargetInfo.TargetInfo.CurrentTarget.HPMax;
                        var valPercentage = MathHelper.CalculatePercentage(currentVal, maxVal);
                    
                        if (maxVal <= 0) maxVal = currentVal + 1;
                        
                        if (getTargetInfo.TargetInfo.CurrentTarget.IsClaimed && getTargetInfo.TargetInfo.CurrentTarget.IsAggressive)
                        {
                            full_brush.Color = full_col;
                        }
                        else if (getTargetInfo.TargetInfo.CurrentTarget.IsClaimed && !getTargetInfo.TargetInfo.CurrentTarget.IsAggressive)
                        {
                            full_brush.Color = friendly_col;
                        }
                        else
                        {
                            full_brush.Color = idle_col;
                        }

                        Debug.WriteLine($"Target ID: {targetId}. Claimed: {getTargetInfo.TargetInfo.CurrentTarget.IsAgroed}. IsAggressive: {getTargetInfo.TargetInfo.CurrentTarget.IsAggressive}");


                        //Check if layer mode has changed
                        if (_currentMode != layer.layerModes)
                        {
                            foreach (var layergroup in _localgroups)
                            {
                                if (layergroup != null)
                                    layergroup.Detach();
                            }

                            _localgroups.Clear();
                            _currentMode = layer.layerModes;
                        }
            
                        if (layer.layerModes == Enums.LayerModes.Interpolate)
                        {
                            //Interpolate implementation
                    
                            var currentVal_Interpolate = LinearInterpolation.Interpolate(currentVal, minVal, maxVal, 0, countKeys);
                            if (currentVal_Interpolate < 0) currentVal_Interpolate = 0;
                            if (currentVal_Interpolate > countKeys) currentVal_Interpolate = countKeys;

                            if (currentVal_Interpolate != _interpolateValue || _targetReset)
                            {
                                       
                                Debug.WriteLine($"Interpolate Target HP Tracker: {currentVal_Interpolate}/{countKeys}.");

                                //Process Lighting
                                var ledGroups = new List<PublicListLedGroup>();
                                        
                                for (int i = 0; i < countKeys; i++)
                                {
                                    var ledGroup = new PublicListLedGroup(surface, ledArray[i])
                                    {
                                        ZIndex = layer.zindex,
                                    };

                                    ledGroup.Detach();

                                    if (i <= currentVal_Interpolate)
                                    {
                                        ledGroup.Brush = full_brush;
                                
                                    }
                                    else
                                    {
                                        ledGroup.Brush = empty_brush;
                                    }
                            
                                    ledGroups.Add(ledGroup);
                            
                                }

                                foreach (var layergroup in _localgroups)
                                {
                                    layergroup.Detach();
                                }

                                _localgroups = ledGroups;
                                _interpolateValue = currentVal_Interpolate;
                            }
                    
                        }
                        else if (layer.layerModes == Enums.LayerModes.Fade)
                        {
                            //Fade implementation
                    
                            var currentVal_Fader = ColorHelper.GetInterpolatedColor(currentVal, minVal, maxVal, empty_brush.Color, full_brush.Color);
                            if (currentVal_Fader != _faderValue || _targetReset)
                            {

                                var ledGroup = new PublicListLedGroup(surface, ledArray)
                                {
                                    ZIndex = layer.zindex,
                                    Brush = new SolidColorBrush(currentVal_Fader)
                                };

                                ledGroup.Detach();
                                _localgroups.Add(ledGroup);
                                _faderValue = currentVal_Fader;
                            }
                        }
                    }
                }


                //Send layers to _layergroups Dictionary to be tracked outside this method
                foreach (var group in _localgroups)
                {
                    var lg = _localgroups.ToArray();

                    if (_layergroups.ContainsKey(layer.layerID))
                    {
                        _layergroups[layer.layerID] = lg;
                    }
                    else
                    {
                        _layergroups.Add(layer.layerID, lg);
                    }
                            
                }
            }

            //Apply lighting
            foreach (var layergroup in _localgroups)
            {
                layergroup.Attach(surface);
            }
            
            _init = true;
            _targetReset = false;
            layer.requestUpdate = false;
        }
    }
}
