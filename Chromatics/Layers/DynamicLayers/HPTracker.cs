using Chromatics.Core;
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
    public class HPTrackerProcessor : LayerProcessor
    {
        private HashSet<PublicListLedGroup> _layergroups = new HashSet<PublicListLedGroup>();
        private SolidColorBrush hp_critical_brush;
        private SolidColorBrush hp_empty_brush;
        private SolidColorBrush hp_full_brush;
        private int _groupCount = -1;

        public override void Process(IMappingLayer layer)
        {
            //HP Tracker Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            //loop through all LED's and assign to device layer
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);
            var ledArray = devices.SelectMany(d => d).Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();
                        
            //Process data from FFXIV
            var hp_full_col = ColorHelper.ColorToRGBColor(_colorPalette.HpFull.Color);
            var hp_critical_col = ColorHelper.ColorToRGBColor(_colorPalette.HpCritical.Color);
            var hp_empty_col = ColorHelper.ColorToRGBColor(_colorPalette.HpEmpty.Color); //Bleed layer
            var hp_threshold = 200;

            if (hp_critical_brush == null) hp_critical_brush = new SolidColorBrush(hp_critical_col);
            if (hp_full_brush == null) hp_full_brush = new SolidColorBrush(hp_full_col);
            if (hp_empty_brush == null) hp_empty_brush = new SolidColorBrush(hp_empty_col);

            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();

                var currentHp = getCurrentPlayer.Entity.HPCurrent;
                var maxHp = getCurrentPlayer.Entity.HPMax;
            
                if (layer.layerModes == Enums.LayerModes.Interpolate)
                {
                    //Interpolate implementation

                    var countKeys = ledArray.Count();
                    
                    //Check if layer has been updated or if layer is disabled or if currently in Preview mode
                    if (_groupCount != countKeys || !layer.Enabled || MappingLayers.IsPreview())
                    {
                        foreach (var layergroup in _layergroups)
                        {
                            layergroup.Detach();
                        }

                        _layergroups.Clear();
                        _groupCount = countKeys;

                        Debug.WriteLine(@"Clearing");

                        if (!layer.Enabled || MappingLayers.IsPreview())
                            return;
                    }

                    var currentHp_Interpolate = LinearInterpolation.Interpolate(currentHp, 0, maxHp, 0, countKeys);
                    if (currentHp_Interpolate < 0) currentHp_Interpolate = 0;
                    if (currentHp_Interpolate > countKeys) currentHp_Interpolate = countKeys;

                    //Debug.WriteLine($"Interpolate HP Tracker: {currentHp_Interpolate}/{countKeys}.");

                    //Process Lighting
                    if (layer.allowBleed)
                    {
                        //Allow bleeding of other layers
                    }
                    else
                    {
                        
                        var ledGroups = new HashSet<PublicListLedGroup>();


                        
                        for (int i = 0; i < countKeys; i++)
                        {
                            var ledGroup = new PublicListLedGroup(surface, ledArray[i])
                            {
                                ZIndex = layer.zindex,
                            };

                            ledGroup.Detach();

                            if (i <= currentHp_Interpolate)
                            {
                                if (currentHp < hp_threshold)
                                {
                                    ledGroup.Brush = hp_critical_brush;
                                }
                                else
                                {
                                    ledGroup.Brush = hp_full_brush;
                                }

                                var currentBrush = ledGroup.Brush as SolidColorBrush;
                                
                            }
                            else
                            {
                                ledGroup.Brush = hp_empty_brush;

                                var currentBrush = ledGroup.Brush as SolidColorBrush;
                            }
                            
                            ledGroups.Add(ledGroup);
                            Debug.WriteLine($"Key {i}: {ledArray[i].Color}");
                        }
                        
                        foreach (var layergroup in _layergroups)
                        {
                            layergroup.Detach();
                        }

                                                                       
                        _layergroups = ledGroups;

                    }
                }
                else if (layer.layerModes == Enums.LayerModes.Fade)
                {
                    //Fade implementation
                }

            }

            //Apply lighting
            
            var x = 1;
            foreach (var layergroup in _layergroups)
            {
                var col = layergroup.Brush as SolidColorBrush;
                //Debug.WriteLine($"Layer Group {x}: {col.Color}");
                x++;
                layergroup.Attach(surface);
            }
            
            _init = true;
            layer.requestUpdate = false;
        }
    }
}
