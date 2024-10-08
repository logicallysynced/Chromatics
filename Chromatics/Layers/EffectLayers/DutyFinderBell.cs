﻿using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Chromatics.Extensions.Sharlayan;

namespace Chromatics.Layers
{
    public class DutyFinderBellProcessor : LayerProcessor
    {
        private static DutyFinderBellProcessor _instance;
        private static Dictionary<int, DutyFinderBellEffectModel> layerProcessorModel = new Dictionary<int, DutyFinderBellEffectModel>();
        private bool _disposed = false;

        // Private constructor to prevent direct instantiation
        private DutyFinderBellProcessor() { }

        // Singleton instance access
        public static DutyFinderBellProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DutyFinderBellProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if layer/effect is disabled
            var effectSettings = RGBController.GetEffectsSettings();

            DutyFinderBellEffectModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new DutyFinderBellEffectModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            //Duty Finder Bell Effect Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();

            ListLedGroup layergroup;
            var ledArray = GetLedArray(layer);

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;
            }
            else
            {
                layergroup = new ListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new ListLedGroup[] { layergroup };
                _layergroups.Add(layer.layerID, lg);
                layergroup.Detach();
            }

            if (!layer.Enabled || !effectSettings.effect_dfbell)
            {
                if (model.activeBrush != null && model.activeBrush.Decorators.Count > 0)
                {
                    model.activeBrush.RemoveAllDecorators();
                    layergroup.Brush = new SolidColorBrush(Color.Transparent);
                    layergroup.ZIndex = 2000;
                    model.wasPopped = false;
                    model.activeBrush = null;
                }

                model.wasPopped = false;
                model.wasDisabled = true;
                return;
            }

            var highlight_col = ColorHelper.ColorToRGBColor(_colorPalette.DutyFinderBell.Color);
            var highlight_brush = new SolidColorBrush(highlight_col);

            var flash = new ShotFlashDecorator(surface)
            {
                IsEnabled = true,
                Order = 100,
                Attack = 0.2f,
                Release = 0.2f,
                Sustain = 0.3f,
                Repetitions = 0
            };

            //Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null)
            {
                DutyFinderBellExtension.CheckCache();

                if (DutyFinderBellExtension.IsPopped() != model.wasPopped && !model.wasDisabled)
                {
                    if (DutyFinderBellExtension.IsPopped())
                    {
                        highlight_brush.AddDecorator(flash);

                        layergroup.Brush = highlight_brush;
                        model.activeBrush = highlight_brush;
                        model.wasPopped = true;
                    }
                    else
                    {
                        if (model.activeBrush != null && model.activeBrush.Decorators.Count > 0)
                        {
                            model.activeBrush.RemoveAllDecorators();
                            model.wasPopped = false;
                        }
                    }

                    model.wasPopped = DutyFinderBellExtension.IsPopped();
                }

                model.wasDisabled = false;
            }

            //Apply lighting
            if (model.activeBrush != null && model.activeBrush.Decorators.Count == 0)
            {
                layergroup.Brush = new SolidColorBrush(Color.Transparent);
                model.activeBrush = null;
            }

            if (layergroup.Decorators.Count == 0)
            {
                layergroup.Attach(surface);
            }

            model.init = true;
            layer.requestUpdate = false;
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
                        if (model.activeBrush != null)
                        {
                            model.activeBrush.RemoveAllDecorators();
                        }
                    }
                    layerProcessorModel.Clear();
                }

                _disposed = true;
            }

            // Call base class implementation if there's one
            base.Dispose(disposing);
            _instance = null;
        }

        private class DutyFinderBellEffectModel
        {
            public bool wasPopped { get; set; }
            public bool wasDisabled { get; set; }
            public SolidColorBrush activeBrush { get; set; }
            public bool init { get; set; }
        }
    }
}
