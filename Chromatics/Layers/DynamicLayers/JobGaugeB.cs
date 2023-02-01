using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using Sharlayan.Core.Enums;
using Sharlayan.Core.JobResources.Enums;
using Sharlayan.Models.ReadResults;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    public class JobGaugeBProcessor : LayerProcessor
    {
        private static Dictionary<int, JobGaugeBDynamicModel> layerProcessorModel = new Dictionary<int, JobGaugeBDynamicModel>();
                
        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            JobGaugeBDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new JobGaugeBDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            //HP Tracker Layer Implementation
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

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors() && _memoryHandler.Reader.CanGetJobResources())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                var getJobResources = _memoryHandler.Reader.GetJobResources();
                if (getCurrentPlayer.Entity == null || getJobResources.JobResourcesContainer == null) return;
                                                                                

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

                var jobGauge = ReturnJobGauge(getCurrentPlayer, getJobResources, _colorPalette);
                if (jobGauge == null) return;

                if (model.highlight_brush == null || model.highlight_brush.Color != jobGauge.fullColor) model.highlight_brush = new SolidColorBrush(jobGauge.fullColor);

                if (layer.allowBleed)
                {
                    //Allow bleeding of other layers
                    model.empty_brush = new SolidColorBrush(Color.Transparent);
                }
                else
                {
                    model.empty_brush = new SolidColorBrush(jobGauge.emptyColor);
                }
            
                if (layer.layerModes == Enums.LayerModes.Interpolate)
                {
                    //Interpolate implementation
                    var currentVal_Interpolate = LinearInterpolation.Interpolate(jobGauge.currentValue, jobGauge.minValue, jobGauge.maxValue, 0, countKeys + jobGauge.offset);

                    //Debug.WriteLine($"Interpolate Value: {currentVal_Interpolate}/{countKeys}.");

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
                    
                }
                else if (layer.layerModes == Enums.LayerModes.Fade)
                {
                    //Fade implementation
                    
                    var currentVal_Fader = ColorHelper.GetInterpolatedColor(jobGauge.currentValue, jobGauge.minValue, jobGauge.maxValue, model.empty_brush.Color, model.highlight_brush.Color);
                    
                    var ledGroup = new PublicListLedGroup(surface, ledArray)
                    {
                        ZIndex = layer.zindex,
                        Brush = new SolidColorBrush(currentVal_Fader)
                    };

                    ledGroup.Detach();
                    model._localgroups.Add(ledGroup);
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

        private static JobGaugeResponse ReturnJobGauge(CurrentPlayerResult currentPlayer, JobResourceResult jobResources, PaletteColorModel _colorPalette)
        {
            var jobGauge = new JobGaugeResponse();

            switch (currentPlayer.Entity.Job)
            {
                case Actor.Job.CPT:
                case Actor.Job.BSM:
                case Actor.Job.ARM:
                case Actor.Job.GSM:
                case Actor.Job.LTW:
                case Actor.Job.WVR:
                case Actor.Job.ALC:
                case Actor.Job.CUL:
                case Actor.Job.MIN:
                    //Crafters

                    break;
                case Actor.Job.MNK:
                    break;
                case Actor.Job.WAR:
                
                    break;
                case Actor.Job.DRG:
                    break;
                case Actor.Job.BRD:
                    //Bard Soul Voice
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDSoulVoice.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDNegative.Color);
                    
                    jobGauge.minValue = 0;
                    jobGauge.maxValue = 100; //Bard Soul Voice Maximum
                    jobGauge.currentValue = jobResources.JobResourcesContainer.Bard.SoulVoice;
                    //jobGauge.offset = 1;

                    if (jobGauge.currentValue >= 80)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDSoulVoiceThreshold.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.WHM:
                    break;
                case Actor.Job.BLM:
                    break;
                case Actor.Job.SMN:
                    break;
                case Actor.Job.SCH:
                    break;
                case Actor.Job.NIN:
                    break;
                case Actor.Job.MCH:
                    break;
                case Actor.Job.DRK:
                    break;
                case Actor.Job.AST:
                    break;
                case Actor.Job.SAM:
                    break;
                case Actor.Job.RDM:
                    break;
                case Actor.Job.GNB:
                    break;
                case Actor.Job.DNC:
                    break;
                case Actor.Job.RPR:
                    break;
                case Actor.Job.SGE:
                    break;
                case Actor.Job.Unknown:
                default:
                    return null;
            }

            return jobGauge;
        }

        private class JobGaugeResponse
        {
            public int minValue { get; set; } = 0;
            public int maxValue { get; set; }
            public int currentValue { get; set; }
            public Color fullColor { get; set; }
            public Color emptyColor { get; set; }
            public bool direction { get; set; }
            public int offset { get; set; } = 0;
        }

        private class JobGaugeBDynamicModel
        {
            public List<PublicListLedGroup> _localgroups { get; set; } = new List<PublicListLedGroup>();
            public SolidColorBrush empty_brush { get; set; }
            public SolidColorBrush highlight_brush { get; set; }
            public LayerModes _currentMode { get; set; }
            public int _interpolateValue { get; set; } = -1;
            public Color _faderValue { get; set; }
            public Actor.Job _currentJob { get; set; }
            public bool init { get; set; }
        }
    }
}
