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
    public class EnmityTrackerProcessor : LayerProcessor
    {
        private static Dictionary<int, EnmityDynamicModel> layerProcessorModel = new Dictionary<int, EnmityDynamicModel>();
                

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

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

            //Enmity Tracker Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            //loop through all LED's and assign to device layer (must maintain order of LEDs)
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);
            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = (from led in devices.SelectMany(d => d).Select((led, index) => new { Index = index, Led = led }) join id in layer.deviceLeds.Values.Select((id, index) => new { Index = index, Id = id }) on led.Led.Id equals id.Id orderby id.Index select led.Led).ToArray();
            
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

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetTargetInfo() && _memoryHandler.Reader.CanGetActors())
            {
                var enmity_top_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity4.Color);
                var enmity_high_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity3.Color);
                var enmity_med_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity2.Color);
                var enmity_low_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity1.Color);
                var enmity_minimal_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity0.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.NoEmnity.Color); //Bleed layer

                if (model.enmity_brush == null || model.enmity_brush.Color != enmity_minimal_col) model.enmity_brush = new SolidColorBrush(enmity_minimal_col);
                if (model.empty_brush == null || model.empty_brush.Color != empty_col) model.empty_brush = new SolidColorBrush(empty_col);

                if (layer.allowBleed)
                {
                    //Allow bleeding of other layers
                    model.empty_brush.Color = Color.Transparent;
                }
                else
                {
                    model.empty_brush.Color = empty_col;
                }

                var getTargetInfo = _memoryHandler.Reader.GetTargetInfo();
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();

                if (getTargetInfo.TargetInfo == null || getCurrentPlayer.Entity == null) return;

                uint targetId = 0;

                if (getTargetInfo.TargetInfo.CurrentTarget != null)
                {
                    targetId = getTargetInfo.TargetInfo.CurrentTarget.ID;
                                        
                }
                
                if (targetId != model._targetId)
                {
                    foreach (var layergroup in model._localgroups)
                    {
                        if (layergroup != null)
                            layergroup.Detach();
                    }

                    model._localgroups.Clear();
                    model._targetReset = true;
                    model._targetId = targetId;
                    Debug.WriteLine(@"Target Reset");
                }


                //No target found
                if (targetId == 0 || !model.init)
                {
                    var ledGroup = new PublicListLedGroup(surface, ledArray)
                    {
                        ZIndex = layer.zindex,
                        Brush = model.empty_brush
                    };

                    ledGroup.Detach();
                    model._localgroups.Add(ledGroup);

                    //Debug.WriteLine($"Target ID: 0");
                }
                else
                {
                    if (getTargetInfo.TargetInfo.CurrentTarget != null)
                    {
                        //Debug.WriteLine($"Target ID: {targetId}");
                        if (getTargetInfo.TargetInfo.EnmityItems != null)
                        {
                            var enmityList = getTargetInfo.TargetInfo.EnmityItems;
                            var enmityTable = enmityList.Select(t => new KeyValuePair<uint, uint>(t.ID, t.Enmity)).OrderBy(kvp => kvp.Value).ToList();
                            var enmityPosition = enmityTable.FindIndex(a => a.Key == getCurrentPlayer.Entity.ID);
                                                
                            

                            Debug.WriteLine($"Target ID: {targetId}. Enmity: {enmityPosition}. Count: {enmityList.Count}");

                            var i = 1;
                            foreach (var ent in enmityTable)
                            {
                                Debug.WriteLine($"{i}: {ent.Key} // {ent.Value}");
                                i++;
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
            
                    
                            if (enmityPosition != model._enmityPosition || model._targetReset)
                            {
                                if (enmityPosition < 0)
                                {
                                    //Engaged & No Aggro
                                    model.enmity_brush = model.empty_brush;
                                }
                                else if (enmityPosition == 0)
                                {
                                    //Full Aggro
                                    model.enmity_brush.Color = enmity_top_col;
                                }
                                else if (enmityPosition == 1)
                                {
                                    //High Aggro
                                    model.enmity_brush.Color = enmity_high_col;
                                }
                                else if (enmityPosition > 1 && model._enmityPosition <= 4)
                                {
                                    //Moderate Aggro
                                    model.enmity_brush.Color = enmity_med_col;
                                }
                                else if (enmityPosition > 4) //&& _enmityPosition <= 8)
                                {
                                    //Low Aggro
                                    model.enmity_brush.Color = enmity_low_col;
                                }
                                else
                                {
                                    //Not Engaged & No Aggro
                                    model.enmity_brush = model.empty_brush;
                                }


                                var ledGroup = new PublicListLedGroup(surface, ledArray)
                                {
                                    ZIndex = layer.zindex,
                                    Brush = model.enmity_brush
                                };

                                ledGroup.Detach();
                                model._localgroups.Add(ledGroup);
                                model._enmityPosition = enmityPosition;
                            }
                        }
                    }
                }


                //Send layers to _layergroups Dictionary to be tracked outside this method
                foreach (var group in model._localgroups)
                {
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
            }

            //Apply lighting
            foreach (var layergroup in model._localgroups)
            {
                layergroup.Attach(surface);
            }
            
            model.init = true;
            model._targetReset = false;
            layer.requestUpdate = false;
        }

        private class EnmityDynamicModel
        {
            public List<PublicListLedGroup> _localgroups { get; set; } = new List<PublicListLedGroup>();
            public SolidColorBrush empty_brush { get; set; }
            public SolidColorBrush enmity_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _enmityPosition { get; set; }
            public uint _targetId { get; set; }
            public bool _targetReset { get; set; }
            public bool init { get; set; }

        }
    }
}
