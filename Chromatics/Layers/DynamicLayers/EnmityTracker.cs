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
        private List<PublicListLedGroup> _localgroups = new List<PublicListLedGroup>();
        private SolidColorBrush empty_brush;
        private SolidColorBrush enmity_brush;
        private LayerModes _currentMode;
        private int _enmityPosition;
        private uint _targetId;
        private bool _targetReset;

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            //Enmity Tracker Dynamic Layer Implementation
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

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetTargetInfo() && _memoryHandler.Reader.CanGetActors())
            {
                var enmity_top_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity4.Color);
                var enmity_high_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity3.Color);
                var enmity_med_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity2.Color);
                var enmity_low_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity1.Color);
                var enmity_minimal_col = ColorHelper.ColorToRGBColor(_colorPalette.Emnity0.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.NoEmnity.Color); //Bleed layer

                if (enmity_brush == null || enmity_brush.Color != enmity_minimal_col) enmity_brush = new SolidColorBrush(enmity_minimal_col);
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
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();

                if (getTargetInfo.TargetInfo == null || getCurrentPlayer.Entity == null) return;

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
            
                    
                            if (enmityPosition != _enmityPosition || _targetReset)
                            {
                                if (enmityPosition < 0)
                                {
                                    //Engaged & No Aggro
                                    enmity_brush = empty_brush;
                                }
                                else if (enmityPosition == 0)
                                {
                                    //Full Aggro
                                    enmity_brush.Color = enmity_top_col;
                                }
                                else if (enmityPosition == 1)
                                {
                                    //High Aggro
                                    enmity_brush.Color = enmity_high_col;
                                }
                                else if (enmityPosition > 1 && _enmityPosition <= 4)
                                {
                                    //Moderate Aggro
                                    enmity_brush.Color = enmity_med_col;
                                }
                                else if (enmityPosition > 4) //&& _enmityPosition <= 8)
                                {
                                    //Low Aggro
                                    enmity_brush.Color = enmity_low_col;
                                }
                                else
                                {
                                    //Not Engaged & No Aggro
                                    enmity_brush = empty_brush;
                                }


                                var ledGroup = new PublicListLedGroup(surface, ledArray)
                                {
                                    ZIndex = layer.zindex,
                                    Brush = enmity_brush
                                };

                                ledGroup.Detach();
                                _localgroups.Add(ledGroup);
                                _enmityPosition = enmityPosition;
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
