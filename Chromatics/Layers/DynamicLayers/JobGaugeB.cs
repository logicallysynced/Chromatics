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
        private static JobGaugeBProcessor _instance;
        private static Dictionary<int, JobGaugeBDynamicModel> layerProcessorModel = new Dictionary<int, JobGaugeBDynamicModel>();
        private bool _disposed = false;

        // Private constructor to prevent direct instantiation
        private JobGaugeBProcessor() { }

        // Singleton instance access
        public static JobGaugeBProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new JobGaugeBProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JobGaugeBProcessor));

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

            // HP Tracker Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedSortedArray(layer);
            var countKeys = ledArray.Count();

            // Check if layer has been updated or if layer is disabled or if currently in Preview mode    
            if (model.init && (layer.requestUpdate || !layer.Enabled))
            {
                DetachAndClearGroups(model._localgroups);

                if (!layer.Enabled)
                    return;
            }

            // Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors() && _memoryHandler.Reader.CanGetJobResources())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                var getJobResources = _memoryHandler.Reader.GetJobResources();
                if (getCurrentPlayer.Entity == null || getJobResources.JobResourcesContainer == null) return;

                // Check if layer mode has changed
                if (model._currentMode != layer.layerModes)
                {
                    DetachAndClearGroups(model._localgroups);
                    model._currentMode = layer.layerModes;
                }

                var jobGauge = ReturnJobGauge(getCurrentPlayer, getJobResources, _colorPalette);

                if (jobGauge == null)
                {
                    model.highlight_brush = model.empty_brush = layer.allowBleed
                        ? new SolidColorBrush(Color.Transparent)
                        : new SolidColorBrush(ColorHelper.ColorToRGBColor(System.Drawing.Color.Black));
                }
                else
                {
                    model.highlight_brush ??= new SolidColorBrush(jobGauge.fullColor);
                    model.highlight_brush.Color = jobGauge.fullColor;

                    model.empty_brush = layer.allowBleed
                        ? new SolidColorBrush(Color.Transparent)
                        : new SolidColorBrush(jobGauge.emptyColor);

                    if (layer.layerModes == Enums.LayerModes.Interpolate)
                    {
                        // Interpolate implementation
                        var currentVal_Interpolate = LinearInterpolation.Interpolate<double>(jobGauge.currentValue, jobGauge.minValue, jobGauge.maxValue, 0, countKeys + jobGauge.offset);

                        // Process Lighting
                        var ledGroups = new List<ListLedGroup>();

                        for (int i = 0; i < countKeys; i++)
                        {
                            var ledGroup = new ListLedGroup(surface, ledArray[i])
                            {
                                ZIndex = layer.zindex,
                            };

                            ledGroup.Detach();

                            ledGroup.Brush = i <= currentVal_Interpolate ? model.highlight_brush : model.empty_brush;
                            ledGroups.Add(ledGroup);
                        }

                        DetachAndClearGroups(model._localgroups);
                        model._localgroups = ledGroups;
                    }
                    else if (layer.layerModes == Enums.LayerModes.Fade)
                    {
                        // Fade implementation
                        var currentVal_Fader = ColorHelper.GetInterpolatedColor(jobGauge.currentValue, jobGauge.minValue, jobGauge.maxValue, model.empty_brush.Color, model.highlight_brush.Color);

                        if (currentVal_Fader != model._faderValue)
                        {
                            var ledGroup = new ListLedGroup(surface, ledArray)
                            {
                                ZIndex = layer.zindex,
                                Brush = new SolidColorBrush(currentVal_Fader)
                            };

                            ledGroup.Detach();

                            DetachAndClearGroups(model._localgroups);
                            model._localgroups.Add(ledGroup);
                            model._faderValue = currentVal_Fader;
                        }
                    }
                }

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
                    // Beast Chakra aggregate — sum of OpoOpo/Raptor/Coeurl stacks (each 0–2)
                    // feeds the Masterful Blitz finishers, so a 0–6 bar is the most legible
                    // single-value representation. Per-type colours surface on Gauge C.
                    {
                        var monk = jobResources.JobResourcesContainer.Monk;
                        var stacks = Math.Min(monk.OpoOpoStacks + monk.RaptorStacks + monk.CoeurlStacks, 6);

                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKBeastChakra.Color);
                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNegative.Color);
                        jobGauge.minValue = 0;
                        jobGauge.maxValue = 6;
                        jobGauge.currentValue = stacks;

                        if (stacks <= 0)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNegative.Color);
                        }
                    }
                    break;
                case Actor.Job.WAR:
                    //Warrior Defiance
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWARNonDefiance.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWARNegative.Color);

                    if (currentPlayer.Entity.StatusItems.Find(i => i.StatusName == "Defiance") != null)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWARDefiance.Color);
                    }
                    else
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWARNonDefiance.Color);
                    }

                    jobGauge.minValue = 0;
                    jobGauge.maxValue = 100;
                    jobGauge.currentValue = 100;

                    break;
                case Actor.Job.DRG:
                    //Dragoon Gaze
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRGDragonGaze.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRGNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Dragoon.DragonGaze;
                    jobGauge.maxValue = 2; //Dragoon Max Gaze
                    jobGauge.minValue = 0;
                                        
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRGNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
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
                case Actor.Job.PLD:
                    // Confiteor combo window — drives the Blade of Faith/Truth/Valor follow-ups.
                    {
                        var pld = jobResources.JobResourcesContainer.Paladin;
                        var window = (int)pld.ConfiteorComboTimer.TotalSeconds;
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDConfiteorTimer.Color);
                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDNegative.Color);
                        jobGauge.minValue = 0;
                        jobGauge.maxValue = 30; //Confiteor combo window (Blade chain)
                        jobGauge.currentValue = Math.Min(window, jobGauge.maxValue);
                        jobGauge.offset = (int)0.5;

                        if (jobGauge.currentValue <= jobGauge.minValue)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDNegative.Color);
                            jobGauge.currentValue = jobGauge.minValue;
                        }
                    }
                    break;
                case Actor.Job.WHM:
                    //White Mage Flower Count
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWHMFlowerPetal.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWHMNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.WhiteMage.Lily;
                    jobGauge.maxValue = 3; //White Mage Max Flowers
                    jobGauge.minValue = 0;

                    if (jobResources.JobResourcesContainer.WhiteMage.BloodLily == 3)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWHMBloodLily.Color);
                    }

                    
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobWHMNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.BLM:
                    // Astral Soul stacks (0–6) — Dawntrail's Flare Star resource. Replaces the
                    // former Enochian countdown on Gauge B: the unified stance Timer is already
                    // on Gauge A, so Gauge B is freed up for the new soul-stack display.

                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMAstralSoul.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMNegative.Color);

                    jobGauge.currentValue = Math.Min(jobResources.JobResourcesContainer.BlackMage.AstralSoulStacks, 6);
                    jobGauge.maxValue = 6;
                    jobGauge.minValue = 0;

                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBLMNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.SMN:
                    //Summoner Summon Timer
                    
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNCarbuncleTimer.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Summoner.SummonTimer.TotalSeconds;
                    jobGauge.maxValue = 15; //Summoner Summon Timer Max
                    jobGauge.minValue = 0;
                    jobGauge.offset = (int)1.5;

                    if ((int)jobResources.JobResourcesContainer.Summoner.AttunementTimer.TotalSeconds > 0)
                    {
                        jobGauge.currentValue = 0;
                    }

                    if (currentPlayer.Entity.Level < 58)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNCarbuncleTimer.Color);
                    }
                    else if (currentPlayer.Entity.Level < 70)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNDreadwyrmTimer.Color);
                    }
                    else if (currentPlayer.Entity.Level <= 90)
                    {
                        if (jobResources.JobResourcesContainer.Summoner.Aether.HasFlag(AetherFlags.PhoenixReady))
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNPhoenixTimer.Color);
                        }
                        else
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSMNBahamutTimer.Color);
                        }
                        
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
                    // Aetherflow stacks — was previously reading FaerieGauge (bug: the 0–100 fairy
                    // resource can't fit into a 0–3 aetherflow pip display). Sharlayan 9 exposes
                    // the correct property directly.

                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSCHAetherflow.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSCHNegative.Color);

                    jobGauge.currentValue = Math.Min(jobResources.JobResourcesContainer.Scholar.Aetherflow, 3);
                    jobGauge.maxValue = 3; //Scholar Aetherflow Max
                    jobGauge.minValue = 0;


                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSCHNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.NIN:
                    //Ninja Ninki Gauge
                    
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobNINNinkiGauge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobNINNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Ninja.NinkiGauge;
                    jobGauge.maxValue = 100; //Ninja Ninki Gauge Max
                    jobGauge.minValue = 0;

                    
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobNINNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.MCH:
                    //Machinist Battery Gauge
                    
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMCHBatteryGauge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMCHNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Machinist.Battery;
                    jobGauge.maxValue = 100; //Machinist Battery Gauge Max
                    jobGauge.minValue = 0;

                    
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMCHNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.DRK:
                    //Dark Knight Darkside Timer
                    
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKDarkside.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.DarkKnight.Timer.TotalSeconds;
                    jobGauge.maxValue = 60; //DRK Darkside Timer Max
                    jobGauge.minValue = 0;
                    jobGauge.offset = (int)0.5;

                    
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }

                    break;
                case Actor.Job.AST:
                    // Top of the Drawn Cards pile — Dawntrail's Seals-free redesign holds up to
                    // three drawn cards; showing the next one up on Gauge B surfaces the player's
                    // immediate next play while Gauge A keeps the currently-active arcana.
                    {
                        var ast = jobResources.JobResourcesContainer.Astrologian;
                        var nextCard = ast.DrawnCards != null && ast.DrawnCards.Count > 0
                            ? ast.DrawnCards[0]
                            : AstrologianCard.None;

                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTNegative.Color);
                        jobGauge.minValue = 0;
                        jobGauge.maxValue = 100;
                        jobGauge.currentValue = nextCard == AstrologianCard.None ? 0 : 100;

                        switch (nextCard)
                        {
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
                            default:
                                jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTNegative.Color);
                                break;
                        }
                    }
                    break;
                case Actor.Job.SAM:
                    //Samurai Meditation Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMMeditation.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Samurai.Meditation;
                    jobGauge.maxValue = 3; //Samurai Meditation Stacks Max
                    jobGauge.minValue = 0;
                                                            
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.RDM:
                    //Red Mage Black Mana
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMBlackMana.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.RedMage.BlackMana;
                    jobGauge.maxValue = 100; //Red Mage Black Mana Max
                    jobGauge.minValue = 0;
                                                            
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.GNB:
                    //Gunbreaker Ammo
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBCartridge.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.GunBreaker.Cartridge;
                    jobGauge.maxValue = 3; //Gunbreaker Ammo Max
                    jobGauge.minValue = 0;
                                                            
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.DNC:
                    //Dancer Espirit
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDNCEspirit.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDNCNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Dancer.Esprit;
                    jobGauge.maxValue = 100; //Dancer Espirit Max
                    jobGauge.minValue = 0;
                                                            
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDNCNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.RPR:
                    //Reaper Shroud Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRPRShrouds.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRPRNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Reaper.Shroud;
                    jobGauge.maxValue = 100; //Reaper Shroud Gauge Max
                    jobGauge.minValue = 0;
                                                            
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRPRNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.SGE:
                    //Sage Addersting Gauge
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSGEAdderstingStacks.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSGENegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Sage.Addersting;
                    jobGauge.maxValue = 3; //Sage Addersting Max
                    jobGauge.minValue = 0;
                                                            
                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSGENegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;
                case Actor.Job.VPR:
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobVPRSerpentOffering.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobVPRNegative.Color);

                    jobGauge.currentValue = (int)jobResources.JobResourcesContainer.Viper.SerpentOffering;
                    jobGauge.maxValue = 100;
                    jobGauge.minValue = 0;

                    if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobVPRNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }


                    break;
                case Actor.Job.PCT:
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPCTPalette.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPCTNegative.Color);

                    var pctCanvas = (CanvasFlags)jobResources.JobResourcesContainer.Pictomancer.CanvasFlags;

                    jobGauge.currentValue = 0;
                    jobGauge.maxValue = 100;
                    jobGauge.minValue = 0;

                    if (pctCanvas == CanvasFlags.Landscape) {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPCTLandscape.Color);
                        jobGauge.maxValue = jobGauge.maxValue;

                    } else if (pctCanvas == CanvasFlags.Maw) {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPCTMaw.Color);
                        jobGauge.maxValue = jobGauge.maxValue;

                    } else if (pctCanvas == CanvasFlags.Weapon) {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPCTWeapon.Color);
                        jobGauge.maxValue = jobGauge.maxValue;

                    } else if (pctCanvas == CanvasFlags.Claw) {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPCTClaw.Color);
                        jobGauge.maxValue = jobGauge.maxValue;

                    } else if (pctCanvas == CanvasFlags.Wing) {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPCTWing.Color);
                        jobGauge.maxValue = jobGauge.maxValue;

                    } else {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPCTNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }


                    break;
                case Actor.Job.Unknown:
                default:
                    return null;
            }

            return jobGauge;
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
                    // Dispose managed resources
                    foreach (var model in layerProcessorModel.Values)
                    {
                        DetachAndClearGroups(model._localgroups);
                    }
                    layerProcessorModel.Clear();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
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
