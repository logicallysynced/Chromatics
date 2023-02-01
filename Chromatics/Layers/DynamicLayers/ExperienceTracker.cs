using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    public class ExperienceTrackerProcessor : LayerProcessor
    {
        private static Dictionary<int, ExpTrackerDynamicModel> layerProcessorModel = new Dictionary<int, ExpTrackerDynamicModel>();
        private static Dictionary<int, int> levelMap = new Dictionary<int, int>();
                
        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            ExpTrackerDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new ExpTrackerDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            //Experience Tracker Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();

            if (levelMap.Count <= 0)
            {
                GetLevelAPI();
            }

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

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                var currentLvl = getCurrentPlayer.Entity.Level;
                var currentExp = 4000000; //Insert current EXP value here.
                var minExp = 0;
                var maxExp = 0;
                

                if (levelMap.ContainsKey(currentLvl))
                {
                    maxExp = levelMap[currentLvl];
                }

                if (currentExp >= maxExp)
                {
                    currentExp = maxExp;
                }

                //Determine if level cap
                if (currentLvl >= 90)
                {
                    currentExp = 0;
                }

                //Debug.WriteLine($"Current Level: {currentLvl}. Current Exp: {currentExp}/{maxExp}");

                if (maxExp <= 0) return;

                var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.ExpFull.Color);
                var empty_col = ColorHelper.ColorToRGBColor(_colorPalette.ExpEmpty.Color); //Bleed layer
            

                if (model.highlight_brush == null || model.highlight_brush.Color != highlight_col) model.highlight_brush = new SolidColorBrush(highlight_col);



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
                    
                    var currentVal_Interpolate = LinearInterpolation.Interpolate(currentExp, minExp, maxExp, 0, countKeys);
                    if (currentVal_Interpolate < 0) currentVal_Interpolate = 0;
                    if (currentVal_Interpolate > countKeys) currentVal_Interpolate = countKeys;

                    if (currentVal_Interpolate != model._interpolateValue || layer.requestUpdate)
                    {
                                       
                        //Debug.WriteLine($"Interpolate HP Tracker: {currentHp_Interpolate}/{countKeys}.");

                        //Process Lighting
                        var ledGroups = new List<PublicListLedGroup>();
                                        
                        for (int i = 0; i < countKeys; i++)
                        {
                            var ledGroup = new PublicListLedGroup(surface, ledArray[i])
                            {
                                ZIndex = layer.zindex,
                            };

                            ledGroup.Detach();

                            if (i < currentVal_Interpolate)
                            {
                                ledGroup.Brush = model.highlight_brush;
                                
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
                    
                    var currentVal_Fader = ColorHelper.GetInterpolatedColor(currentExp, minExp, maxExp, model.empty_brush.Color, model.highlight_brush.Color);
                    if (currentVal_Fader != model._faderValue || layer.requestUpdate)
                    {
                        var ledGroup = new PublicListLedGroup(surface, ledArray)
                        {
                            ZIndex = layer.zindex,
                            Brush = new SolidColorBrush(currentVal_Fader)
                        };

                        ledGroup.Detach();
                        model._localgroups.Add(ledGroup);
                        model._faderValue = currentVal_Fader;
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
            layer.requestUpdate = false;
        }

        private void GetLevelAPI()
        {
            var _levelData = FileOperationsHelper.GetCsvData(@"https://raw.githubusercontent.com/viion/ffxiv-datamining/master/csv/ParamGrow.csv", @"ParamGrow.csv");

            var delimiters = new[] { ',' };
            using (var reader = new StreamReader(_levelData))
            {
                int lineptr = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    lineptr++;

                    if (lineptr < 3) continue;

                    if (line == null)
                    {
                        break;
                    }

                    var parts = line.Split(delimiters);

                    if (parts[0] == null && parts[0] == "") continue;
                    if (parts[1] == null && parts[1] == "") continue;

                    if (!int.TryParse(parts[0], out var id)) continue;
                    if (!int.TryParse(parts[1], out var exp)) continue;

                    if (!levelMap.ContainsKey(id))
                    {
                        levelMap.Add(id, exp);
                        Debug.WriteLine(id + @"//" + exp);
                    }
                }
            }
        }



        private class ExpTrackerDynamicModel
        {
            public List<PublicListLedGroup> _localgroups { get; set; } = new List<PublicListLedGroup>();
            public SolidColorBrush highlight_brush { get; set; }
            public SolidColorBrush empty_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; }
            public Color _faderValue { get; set; }
            public bool init { get; set; }


        }
    }
}
