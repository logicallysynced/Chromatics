using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.Sharlayan;
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
    public class HPTrackerProcessor : LayerProcessor
    {
        private static Dictionary<int, HPTrackerDynamicModel> layerProcessorModel = new Dictionary<int, HPTrackerDynamicModel>();
                
        public override void Process(IMappingLayer layer)
        {

            HPTrackerDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new HPTrackerDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            //HP Tracker Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            //loop through all LED's and assign to device layer (must maintain order of LEDs)
            
            
            var _layergroups = RGBController.GetLiveLayerGroups();
            var device = GetDevice(layer);
            if (device == null)
            {
                return;
            }

            var ledArray = (from led in device.Select((led, index) => new { Index = index, Led = led }) join id in layer.deviceLeds.Values.Select((id, index) => new { Index = index, Id = id }) on led.Led.Id equals id.Id orderby id.Index select led.Led).ToArray();
            
            var countKeys = ledArray.Count();

            //Check if layer has been updated or if layer is disabled or if currently in Preview mode    
            if (model.init && (layer.requestUpdate || !layer.Enabled))
            {
                foreach (var layergroup in model._localgroups)
                {
                    if (layergroup != null)
                        layergroup.Detach();

                }

                model._localgroups.Clear();

                if (!layer.Enabled)
                    return;
            }
            
            

            //Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                var currentVal = getCurrentPlayer.Entity.HPCurrent;
                var minVal = 0;
                var maxVal = getCurrentPlayer.Entity.HPMax;
                var valPercentage = MathHelper.CalculatePercentage(currentVal, maxVal);
                               

                var full_col = ColorHelper.ColorToRGBColor(_colorPalette.HpFull.Color);
                var critical_col = ColorHelper.ColorToRGBColor(_colorPalette.HpCritical.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.HpEmpty.Color); //Bleed layer
            
                if (maxVal <= 0) maxVal = currentVal + 1;

                if (model.critical_brush == null || model.critical_brush.Color != critical_col) model.critical_brush = new SolidColorBrush(critical_col);
                if (model.full_brush == null || model.full_brush.Color != full_col) model.full_brush = new SolidColorBrush(full_col);

                var criticalHpPercentage = AppSettings.GetSettings().criticalHpPercentage;
                if (criticalHpPercentage < 0) criticalHpPercentage = 0;
                if (criticalHpPercentage > maxVal) criticalHpPercentage = maxVal;


                if (valPercentage < criticalHpPercentage)
                {
                    model.full_brush = model.critical_brush;
                }
                else
                {
                    model.full_brush.Color = full_col;
                }

                if (layer.allowBleed)
                {
                    //Allow bleeding of other layers
                    model.empty_brush = new SolidColorBrush(Color.Transparent);
                }
                else
                {
                    model.empty_brush = new SolidColorBrush(empty_col);
                }
                                

                //Check if layer mode has changed
                if (model._currentMode != layer.layerModes)
                {
                    foreach (var layergroup in model._localgroups)
                    {
                        if (layergroup != null)
                            layergroup.Detach();
                    }

                    model._localgroups.Clear();
                    model._currentMode = layer.layerModes;
                }
            
                if (layer.layerModes == Enums.LayerModes.Interpolate)
                {
                    //Interpolate implementation
                    
                    var currentVal_Interpolate = LinearInterpolation.Interpolate(currentVal, minVal, maxVal, 0, countKeys);
                    if (currentVal_Interpolate < 0) currentVal_Interpolate = 0;
                    if (currentVal_Interpolate > countKeys) currentVal_Interpolate = countKeys;

                    if (currentVal_Interpolate != model._interpolateValue || layer.requestUpdate)
                    {
                        //Process Lighting
                        var ledGroups = new List<ListLedGroup>();
                                        
                        for (int i = 0; i < countKeys; i++)
                        {
                            var ledGroup = new ListLedGroup(surface, ledArray[i])
                            {
                                ZIndex = layer.zindex,
                            };

                            ledGroup.Detach();

                            if (i < currentVal_Interpolate)
                            {
                                ledGroup.Brush = model.full_brush;
                                
                            }
                            else
                            {
                                ledGroup.Brush = model.empty_brush;
                            }
                            
                            ledGroups.Add(ledGroup);
                            
                        }

                        foreach (var layergroup in model._localgroups)
                        {
                            layergroup.Detach();
                        }

                        model._localgroups = ledGroups;
                        model._interpolateValue = currentVal_Interpolate;
                    }
                    
                }
                else if (layer.layerModes == Enums.LayerModes.Fade)
                {
                    //Fade implementation
                    
                    var currentVal_Fader = ColorHelper.GetInterpolatedColor(currentVal, minVal, maxVal, model.empty_brush.Color, model.full_brush.Color);
                    if (currentVal_Fader != model._faderValue || layer.requestUpdate)
                    {
                        if (valPercentage < criticalHpPercentage)
                        {
                            model.full_brush.Color = full_col;
                            model.empty_brush.Color = critical_col;
                        }
                        else
                        {
                            model.empty_brush.Color = empty_col;
                        }

                        var ledGroup = new ListLedGroup(surface, ledArray)
                        {
                            ZIndex = layer.zindex,
                            Brush = new SolidColorBrush(currentVal_Fader)
                        };

                        ledGroup.Detach();

                        if (!model._localgroups.Contains(ledGroup))
                            model._localgroups.Add(ledGroup);

                        model._faderValue = currentVal_Fader;
                    }
                }

                //Send layers to _layergroups Dictionary to be tracked outside this method
                
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

            //Apply lighting
            foreach (var layergroup in model._localgroups)
            {
                layergroup.Attach(surface);
            }
            
            model.init = true;
            layer.requestUpdate = false;

        }

        private class HPTrackerDynamicModel
        {
            public List<ListLedGroup> _localgroups { get; set; } = new List<ListLedGroup>();
            public SolidColorBrush critical_brush { get; set; }
            public SolidColorBrush empty_brush { get; set; }
            public SolidColorBrush full_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; }
            public Color _faderValue { get; set; }
            public bool init { get; set; }


        }
    }
}
