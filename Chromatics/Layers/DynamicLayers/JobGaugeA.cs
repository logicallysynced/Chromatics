using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using Sharlayan.Core.Enums;
using Sharlayan.Core.JobResources;
using Sharlayan.Core.JobResources.Enums;
using Sharlayan.Models.ReadResults;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    public class JobGaugeAProcessor : LayerProcessor
    {
        private static Dictionary<int, JobGaugeADynamicModel> layerProcessorModel = new Dictionary<int, JobGaugeADynamicModel>();

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            JobGaugeADynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new JobGaugeADynamicModel();
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

                if (jobGauge == null)
                {
                    if (layer.allowBleed)
                    {
                        //Allow bleeding of other layers
                        model.empty_brush = new SolidColorBrush(Color.Transparent);
                    }
                    else
                    {
                        model.empty_brush = new SolidColorBrush(ColorHelper.ColorToRGBColor(System.Drawing.Color.Black));
                    }

                    model.highlight_brush = model.empty_brush;
                }
                else
                {
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
                        var currentVal_Interpolate = LinearInterpolation.Interpolate<double>(jobGauge.currentValue, jobGauge.minValue, jobGauge.maxValue, 0, countKeys + jobGauge.offset);

                        //Process Lighting
                        var ledGroups = new List<ListLedGroup>();

                        for (int i = 0; i < countKeys; i++)
                        {
                            var ledGroup = new ListLedGroup(surface, ledArray[i])
                            {
                                ZIndex = layer.zindex,
                            };

                            ledGroup.Detach();

                            if (i <= currentVal_Interpolate)
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

                        if (currentVal_Fader != model._faderValue)
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

                    return null;
                case Actor.Job.MNK:
                    //Monk Chakras
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKChakra.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Monk.Chakra;
                    jobGauge.maxValue = 5; //Monk Max Chakras
                    jobGauge.minValue = 0;


                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.WAR:
                    //Warrior Beast Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWARBeastGauge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWARNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Warrior.BeastGauge;
                    jobGauge.maxValue = 100; //Warrior Max Beast Gauge
                    jobGauge.minValue = 0;

                    if (jobGauge.currentValue == jobGauge.maxValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWARBeastGaugeMax.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWARNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.DRG:
                    //Dragoon Blood Dragon Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRGBloodDragon.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRGNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Dragoon.Timer.TotalSeconds;
                    jobGauge.maxValue = 30; //Dragoon Max Dragon Gauge
                    jobGauge.minValue = 0;
                    jobGauge.offset = (int)0.5;

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRGNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.BRD:
                    //Bard Songs
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDNegative.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDNegative.Color);

                    switch (jobResources.JobResourcesContainer.Bard.ActiveSong)
                    {
                        case SongFlags.WanderersMinuet:
                            if (jobResources.JobResourcesContainer.Bard.Repertoire >= 3)
                            {
                                jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDRepertoire.Color);
                            }
                            else
                            {
                                jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDMinuet.Color);
                            }
                            break;
                        case SongFlags.MagesBallad:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDBallad.Color);
                            break;
                        case SongFlags.ArmysPaeon:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDArmys.Color);
                            break;
                        default:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDNegative.Color);
                            break;
                    }

                    var bardMaxTimer = 45; //Bard Max Timer

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Bard.Timer.TotalSeconds;
                    jobGauge.maxValue = bardMaxTimer;
                    jobGauge.minValue = 0;
                    jobGauge.offset = (int)0.5;

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.PLD:
                    //Paladin Oath Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDOathGauge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Paladin.OathGauge;
                    jobGauge.maxValue = 100; //Paldain Max Oath Gauge
                    jobGauge.minValue = 0;

                    if (currentPlayer.Entity.StatusItems.Find(i => i.StatusName == "Iron Will") != null)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDIronWill.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.WHM:
                    //White Mage Flower Charge Timer
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWHMFlowerCharge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWHMNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.WhiteMage.Timer.TotalSeconds;
                    jobGauge.maxValue = 20; //White Mage Max Flower Charge
                    jobGauge.minValue = 0;
                    jobGauge.offset = (int)0.5;


                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWHMNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.BLM:
                    //Black Mage Astral Timers

                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMAstralFire.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.BlackMage.AstralTimer;
                    jobGauge.maxValue = 15 * 4; //Black Mage Astral/Umbral Timer
                    jobGauge.minValue = 0;
                    jobGauge.offset = (int)0.5;

                    if (jobResources.JobResourcesContainer.BlackMage.AstralStacks > 0)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMAstralFire.Color);
                    }
                    else if (jobResources.JobResourcesContainer.BlackMage.UmbralStacks > 0)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMUmbralIce.Color);
                    }
                    else
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMNegative.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.SMN:
                    //Summoner Attunement Timer

                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNNegative.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Summoner.AttunementTimer.TotalSeconds;
                    jobGauge.maxValue = 30; //Summoner Attunement Timer Max
                    jobGauge.minValue = 0;
                    jobGauge.offset = (int)0.5;

                    if (jobResources.JobResourcesContainer.Summoner.Aether.HasFlag(AetherFlags.GarudaAttuned))
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNGaruda.Color);
                    }
                    else if (jobResources.JobResourcesContainer.Summoner.Aether.HasFlag(AetherFlags.IfritAttuned))
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNIfrit.Color);
                    }
                    else if (jobResources.JobResourcesContainer.Summoner.Aether.HasFlag(AetherFlags.TitanAttuned))
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNTitan.Color);
                    }
                    else
                    {
                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNNegative.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.SCH:
                    //Scholar Faerie Gauge

                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSCHFaerieGauge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSCHNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Scholar.FaerieGauge;
                    jobGauge.maxValue = 100; //Scholar Faerie Gauge Max
                    jobGauge.minValue = 0;

                    if (currentPlayer.Entity.StatusItems.Find(i => i.StatusName == "Summon Seraph") != null)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSCHSeraph.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSCHNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.NIN:
                    //Ninja Huton Timer

                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobNINHuton.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobNINNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Ninja.Timer.TotalSeconds;
                    jobGauge.maxValue = 60; //Ninja Huton Timer Max
                    jobGauge.minValue = 0;
                    jobGauge.offset = (int)0.5;


                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobNINNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.MCH:
                    //Machinist Heat Gauge

                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMCHHeatGauge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMCHNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Machinist.Heat;
                    jobGauge.maxValue = 100; //Machinist Heat Gauge Max
                    jobGauge.minValue = 0;

                    if (jobResources.JobResourcesContainer.Machinist.OverheatTimer.TotalSeconds > 0)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMCHOverheat.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMCHNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.DRK:
                    //Dark Knight Blood Gauge

                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKBloodGauge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.DarkKnight.BlackBlood;
                    jobGauge.maxValue = 100; //Dark Knight Blood Gauge Max
                    jobGauge.minValue = 0;

                    if (currentPlayer.Entity.StatusItems.Find(i => i.StatusName == "Grit") != null)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKGrit.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.AST:
                    //Astrologian Card Drawn
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTNegative.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTNegative.Color);

                    jobGauge.currentValue = 100;
                    jobGauge.maxValue = 100;
                    jobGauge.minValue = 0;

                    switch (jobResources.JobResourcesContainer.Astrologian.Arcana)
                    {
                        case AstrologianCard.None:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTNegative.Color);
                            break;
                        case AstrologianCard.Balance:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTBalance.Color);
                            break;
                        case AstrologianCard.Bole:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTBole.Color);
                            break;
                        case AstrologianCard.Arrow:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTArrow.Color);
                            break;
                        case AstrologianCard.Spear:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTSpear.Color);
                            break;
                        case AstrologianCard.Ewer:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTEwer.Color);
                            break;
                        case AstrologianCard.Spire:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTSpire.Color);
                            break;
                        case AstrologianCard.Lord:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTLord.Color);
                            break;
                        case AstrologianCard.Lady:
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTLady.Color);
                            break;
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.SAM:
                    //Samurai Kenki Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMKenki.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Samurai.Kenki;
                    jobGauge.maxValue = 100; //Samurai Kenki Gauge Max
                    jobGauge.minValue = 0;

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.RDM:
                    //Red Mage White Mana
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMWhiteMana.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.RedMage.WhiteMana;
                    jobGauge.maxValue = 100; //Red Mage White Mana Max
                    jobGauge.minValue = 0;

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.GNB:
                    //Gunbreaker Royal Guard
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBNegative.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBNegative.Color);

                    jobGauge.currentValue = 100;
                    jobGauge.maxValue = 100;
                    jobGauge.minValue = 0;

                    if (currentPlayer.Entity.StatusItems.Find(i => i.StatusName == "Royal Guard") != null)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBRoyalGuard.Color);
                    }

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.DNC:
                    //Dancer Fourfold Feathers
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDNCFeathers.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDNCNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Dancer.FourFoldFeathers;
                    jobGauge.maxValue = 4; //Dancer Fourfold Feathers Max
                    jobGauge.minValue = 0;

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDNCNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.RPR:
                    //Reaper Soul Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRPRSouls.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRPRNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Reaper.Soul;
                    jobGauge.maxValue = 100; //Reaper Soul Gauge Max
                    jobGauge.minValue = 0;

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRPRNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.SGE:
                    //Sage Addersgall Recharge Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSGEAddersgallRecharge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSGENegative.Color);

                    if (jobResources.JobResourcesContainer.Sage.Addersgall == 3)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSGEAddersgallStacks.Color);
                        jobGauge.currentValue = 100;
                        jobGauge.maxValue = 100;
                        jobGauge.minValue = 0;
                    }
                    else
                    {
                        jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Sage.AddersgallTimer.TotalSeconds;
                        jobGauge.maxValue = 20; //Sage Addersgall Gauge Max
                        jobGauge.minValue = 0;
                    }



                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSGENegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
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

        private class JobGaugeADynamicModel
        {
            public List<ListLedGroup> _localgroups { get; set; } = new List<ListLedGroup>();
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
