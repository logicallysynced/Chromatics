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
using System.Linq;
using static Chromatics.Helpers.MathHelper;

namespace Chromatics.Layers
{
    // Job Gauge C — tertiary resource for jobs that genuinely expose a third meaningful
    // gauge element in Dawntrail (e.g. BRD Radiant Finale codas, MNK Nadi, RDM Mana Stacks).
    // Jobs with only one or two meaningful elements return null here and the layer stays
    // unlit, matching how JobGaugeB already behaves for MNK/PLD/AST under Endwalker.
    public class JobGaugeCProcessor : LayerProcessor
    {
        private static JobGaugeCProcessor _instance;
        private static Dictionary<int, JobGaugeCDynamicModel> layerProcessorModel = new Dictionary<int, JobGaugeCDynamicModel>();
        private bool _disposed = false;

        private JobGaugeCProcessor() { }

        public static JobGaugeCProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new JobGaugeCProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JobGaugeCProcessor));

            JobGaugeCDynamicModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new JobGaugeCDynamicModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedSortedArray(layer);
            var countKeys = ledArray.Count();

            if (model.init && (layer.requestUpdate || !layer.Enabled))
            {
                DetachAndClearGroups(model._localgroups);

                if (!layer.Enabled)
                    return;
            }

            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors() && _memoryHandler.Reader.CanGetJobResources())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                var getJobResources = _memoryHandler.Reader.GetJobResources();
                if (getCurrentPlayer.Entity == null || getJobResources.JobResourcesContainer == null) return;

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
                        var currentVal_Interpolate = LinearInterpolation.Interpolate<double>(jobGauge.currentValue, jobGauge.minValue, jobGauge.maxValue, 0, countKeys + jobGauge.offset);

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
            var container = jobResources.JobResourcesContainer;

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
                    return null;

                case Actor.Job.BRD:
                    // Radiant Finale Coda — 3-bit bitfield (Ballad / Paeon / Minuet).
                    // Most significant active coda wins for the single "highlight" colour;
                    // fill width is the number of codas collected (0–3).
                    {
                        var coda = container.Bard.RadiantFinaleCoda;
                        var codaCount = PopCount(coda);

                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDNegative.Color);
                        if ((coda & 0b100) != 0)
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDRadiantFinaleMinuet.Color);
                        else if ((coda & 0b010) != 0)
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDRadiantFinalePaeon.Color);
                        else if ((coda & 0b001) != 0)
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDRadiantFinaleBallad.Color);
                        else
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobBRDNegative.Color);

                        jobGauge.minValue = 0;
                        jobGauge.maxValue = 3;
                        jobGauge.currentValue = codaCount;
                    }
                    break;

                case Actor.Job.MNK:
                    // Nadi — two mutually-compatible flags (Lunar / Solar). When both are
                    // present the gauge lights solid Gold; single-Nadi lights its own colour.
                    {
                        var nadi = container.Monk.Nadi;
                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNegative.Color);
                        jobGauge.minValue = 0;
                        jobGauge.maxValue = 2;
                        jobGauge.currentValue = 0;

                        var lunar = (nadi & NadiFlags.Lunar) != 0;
                        var solar = (nadi & NadiFlags.Solar) != 0;

                        if (lunar && solar)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNadiSolar.Color);
                            jobGauge.currentValue = 2;
                        }
                        else if (lunar)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNadiLunar.Color);
                            jobGauge.currentValue = 1;
                        }
                        else if (solar)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNadiSolar.Color);
                            jobGauge.currentValue = 1;
                        }
                        else
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobMNKNegative.Color);
                        }
                    }
                    break;

                case Actor.Job.DRG:
                    // Firstminds' Focus — 0–2 stacks built during Life of the Dragon,
                    // spent on Dragonfire Dive.
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRGFirstminds.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRGNegative.Color);
                    jobGauge.minValue = 0;
                    jobGauge.maxValue = 2;
                    jobGauge.currentValue = Math.Min((int)container.Dragoon.FirstmindsFocusCount, 2);
                    break;

                case Actor.Job.DRK:
                    // Living Shadow pet timer — 0 when inactive.
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKLivingShadow.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKNegative.Color);
                    jobGauge.minValue = 0;
                    jobGauge.maxValue = 20; // Living Shadow duration
                    jobGauge.currentValue = Math.Min((int)container.DarkKnight.ShadowTimer.TotalSeconds, jobGauge.maxValue);
                    jobGauge.offset = (int)0.5;

                    if (jobGauge.currentValue <= jobGauge.minValue)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobDRKNegative.Color);
                        jobGauge.currentValue = jobGauge.minValue;
                    }
                    break;

                case Actor.Job.GNB:
                    // Bloodfest cooldown tracker. Uses MaxTimerDuration as the reference max
                    // so the bar scales correctly as the ability's base cooldown changes.
                    {
                        var timer = (int)container.GunBreaker.Timer.TotalSeconds;
                        var max = Math.Max(1, (int)container.GunBreaker.MaxTimerDuration.TotalSeconds);

                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBBloodfestTimer.Color);
                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBNegative.Color);
                        jobGauge.minValue = 0;
                        jobGauge.maxValue = max;
                        jobGauge.currentValue = Math.Min(timer, max);
                        jobGauge.offset = (int)0.5;

                        if (jobGauge.currentValue <= jobGauge.minValue)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobGNBNegative.Color);
                            jobGauge.currentValue = jobGauge.minValue;
                        }
                    }
                    break;

                case Actor.Job.AST:
                    // Astral / Umbral Draw state — binary toggle showing the current side of the
                    // Draw cycle. Full-fill on the active colour; negative when no card is held.
                    {
                        var hasCard = container.Astrologian.CurrentArcana != AstrologianCard.None
                                      || container.Astrologian.DrawnCards.Count > 0;

                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTNegative.Color);
                        jobGauge.minValue = 0;
                        jobGauge.maxValue = 100;
                        jobGauge.currentValue = hasCard ? 100 : 0;

                        if (!hasCard)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTNegative.Color);
                        }
                        else if (container.Astrologian.DrawType == AstrologianDraw.Umbral)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTUmbralDraw.Color);
                        }
                        else
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobASTAstralDraw.Color);
                        }
                    }
                    break;

                case Actor.Job.RDM:
                    // Mana Stacks — 0–3 melee-combo finisher pips.
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMManaStacks.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMNegative.Color);
                    jobGauge.minValue = 0;
                    jobGauge.maxValue = 3;
                    jobGauge.currentValue = Math.Min(container.RedMage.ManaStacks, 3);

                    if (jobGauge.currentValue <= 0)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobRDMNegative.Color);
                    }
                    break;

                case Actor.Job.SAM:
                    // Kaeshi ready — binary indicator. Full colour when any Kaeshi follow-up
                    // is queued; negative otherwise.
                    {
                        var kaeshi = container.Samurai.Kaeshi;
                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMNegative.Color);
                        jobGauge.minValue = 0;
                        jobGauge.maxValue = 100;

                        if (kaeshi == KaeshiAction.None)
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMNegative.Color);
                            jobGauge.currentValue = 0;
                        }
                        else
                        {
                            jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobSAMKaeshiReady.Color);
                            jobGauge.currentValue = 100;
                        }
                    }
                    break;

                case Actor.Job.VPR:
                    // Reawakened duration — timer now reports correctly in Sharlayan 9; previously
                    // the offset was mapped but the read was missing, so values were always zero.
                    {
                        var timer = (int)container.Viper.Timer.TotalSeconds;
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobVPRReawakened.Color);
                        jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobVPRNegative.Color);
                        jobGauge.minValue = 0;
                        jobGauge.maxValue = 30; // Reawakened buff duration
                        jobGauge.currentValue = Math.Min(timer, jobGauge.maxValue);
                        jobGauge.offset = (int)0.5;

                        if (jobGauge.currentValue <= jobGauge.minValue)
                        {
                            // When no Reawakened timer is running, highlight if any Serpent Combo
                            // follow-up is queued — gives the layer something to show in melee phase.
                            if (container.Viper.SerpentCombo != SerpentCombo.None)
                            {
                                jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobVPRSerpentCombo.Color);
                                jobGauge.currentValue = jobGauge.maxValue;
                            }
                            else
                            {
                                jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobVPRNegative.Color);
                                jobGauge.currentValue = jobGauge.minValue;
                            }
                        }
                    }
                    break;

                case Actor.Job.PLD:
                    // Confiteor combo step indicator — 4-hit chain (Confiteor → Blade of Faith → Truth → Valor).
                    jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDConfiteorStep.Color);
                    jobGauge.emptyColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDNegative.Color);
                    jobGauge.minValue = 0;
                    jobGauge.maxValue = 3;
                    jobGauge.currentValue = Math.Min(container.Paladin.ConfiteorComboStep, 3);

                    if (jobGauge.currentValue <= 0)
                    {
                        jobGauge.fullColor = ColorHelper.ColorToRGBColor(_colorPalette.JobPLDNegative.Color);
                    }
                    break;

                case Actor.Job.Unknown:
                default:
                    return null;
            }

            if (jobGauge.currentValue > jobGauge.maxValue) jobGauge.currentValue = jobGauge.maxValue;
            if (jobGauge.currentValue < jobGauge.minValue) jobGauge.currentValue = jobGauge.minValue;

            return jobGauge;
        }

        private static int PopCount(byte b)
        {
            int count = 0;
            while (b != 0)
            {
                count += b & 1;
                b >>= 1;
            }
            return count;
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

        private class JobGaugeCDynamicModel
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
