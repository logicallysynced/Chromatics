using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using Sharlayan.Core.Enums;
using Sharlayan.Models.ReadResults;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    public class ExperienceTrackerProcessor : LayerProcessor
    {
        private static Dictionary<int, ExpTrackerDynamicModel> layerProcessorModel = new Dictionary<int, ExpTrackerDynamicModel>();
        private static Dictionary<int, int> levelMap = new Dictionary<int, int>();
                
        public override void Process(IMappingLayer layer)
        {

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
                var currentExp = GetJobCurrentExperience(getCurrentPlayer);
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

        private int GetJobCurrentExperience(CurrentPlayerResult currentPlayer)
        {
            switch (currentPlayer.Entity.Job)
            {
                case Actor.Job.Unknown:
                    return 0;
                case Actor.Job.GLD:
                    return currentPlayer.PlayerInfo.GLD_CurrentEXP;
                case Actor.Job.PGL:
                    return currentPlayer.PlayerInfo.PGL_CurrentEXP;
                case Actor.Job.MRD:
                    return currentPlayer.PlayerInfo.MRD_CurrentEXP;
                case Actor.Job.LNC:
                    return currentPlayer.PlayerInfo.LNC_CurrentEXP;
                case Actor.Job.ARC:
                    return currentPlayer.PlayerInfo.ARC_CurrentEXP;
                case Actor.Job.CNJ:
                    return currentPlayer.PlayerInfo.CNJ_CurrentEXP;
                case Actor.Job.THM:
                    return currentPlayer.PlayerInfo.THM_CurrentEXP;
                case Actor.Job.CPT:
                    return currentPlayer.PlayerInfo.CPT_CurrentEXP;
                case Actor.Job.BSM:
                    return currentPlayer.PlayerInfo.BSM_CurrentEXP;
                case Actor.Job.ARM:
                    return currentPlayer.PlayerInfo.ARM_CurrentEXP;
                case Actor.Job.GSM:
                    return currentPlayer.PlayerInfo.GSM_CurrentEXP;
                case Actor.Job.LTW:
                    return currentPlayer.PlayerInfo.LTW_CurrentEXP;
                case Actor.Job.WVR:
                    return currentPlayer.PlayerInfo.WVR_CurrentEXP;
                case Actor.Job.ALC:
                    return currentPlayer.PlayerInfo.ALC_CurrentEXP;
                case Actor.Job.CUL:
                    return currentPlayer.PlayerInfo.CUL_CurrentEXP;
                case Actor.Job.MIN:
                    return currentPlayer.PlayerInfo.MIN_CurrentEXP;
                case Actor.Job.BTN:
                    return currentPlayer.PlayerInfo.BTN_CurrentEXP;
                case Actor.Job.FSH:
                    return currentPlayer.PlayerInfo.FSH_CurrentEXP;
                case Actor.Job.PLD:
                    return currentPlayer.PlayerInfo.GLD_CurrentEXP;
                case Actor.Job.MNK:
                    return currentPlayer.PlayerInfo.PGL_CurrentEXP;
                case Actor.Job.WAR:
                    return currentPlayer.PlayerInfo.MRD_CurrentEXP;
                case Actor.Job.DRG:
                    return currentPlayer.PlayerInfo.LNC_CurrentEXP;
                case Actor.Job.BRD:
                    return currentPlayer.PlayerInfo.ARC_CurrentEXP;
                case Actor.Job.WHM:
                    return currentPlayer.PlayerInfo.CNJ_CurrentEXP;
                case Actor.Job.BLM:
                    return currentPlayer.PlayerInfo.THM_CurrentEXP;
                case Actor.Job.ACN:
                    return currentPlayer.PlayerInfo.ACN_CurrentEXP;
                case Actor.Job.SMN:
                    return currentPlayer.PlayerInfo.ACN_CurrentEXP;
                case Actor.Job.SCH:
                    return currentPlayer.PlayerInfo.ACN_CurrentEXP;
                case Actor.Job.ROG:
                    return currentPlayer.PlayerInfo.ROG_CurrentEXP;
                case Actor.Job.NIN:
                    return currentPlayer.PlayerInfo.ROG_CurrentEXP;
                case Actor.Job.MCH:
                    return currentPlayer.PlayerInfo.MCH_CurrentEXP;
                case Actor.Job.DRK:
                    return currentPlayer.PlayerInfo.DRK_CurrentEXP;
                case Actor.Job.AST:
                    return currentPlayer.PlayerInfo.AST_CurrentEXP;
                case Actor.Job.SAM:
                    return currentPlayer.PlayerInfo.SAM_CurrentEXP;
                case Actor.Job.RDM:
                    return currentPlayer.PlayerInfo.RDM_CurrentEXP;
                case Actor.Job.BLU:
                    return currentPlayer.PlayerInfo.BLU_CurrentEXP;
                case Actor.Job.GNB:
                    return currentPlayer.PlayerInfo.GNB_CurrentEXP;
                case Actor.Job.DNC:
                    return currentPlayer.PlayerInfo.DNC_CurrentEXP;
                case Actor.Job.RPR:
                    return currentPlayer.PlayerInfo.RPR_CurrentEXP;
                case Actor.Job.SGE:
                    return currentPlayer.PlayerInfo.SGE_CurrentEXP;
                default:
                    return 0;
            }
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
                    }
                }
            }
        }



        private class ExpTrackerDynamicModel
        {
            public List<ListLedGroup> _localgroups { get; set; } = new List<ListLedGroup>();
            public SolidColorBrush highlight_brush { get; set; }
            public SolidColorBrush empty_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; }
            public Color _faderValue { get; set; }
            public bool init { get; set; }


        }
    }
}
