using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using Sharlayan.Core.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromatics.Extensions.Sharlayan;
using System.Security.Cryptography;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures.Gradients;
using RGB.NET.Presets.Textures;

namespace Chromatics.Layers
{
    public class CutsceneAnimationProcessor : LayerProcessor
    {
        private static Dictionary<int, CutsceneAnimationEffectModel> layerProcessorModel = new Dictionary<int, CutsceneAnimationEffectModel>();

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode or if layer/effect is disabled
            var effectSettings = RGBController.GetEffectsSettings();
            var runningEffects = RGBController.GetRunningEffects();

            if (MappingLayers.IsPreview()) return;
            
            CutsceneAnimationEffectModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new CutsceneAnimationEffectModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }

            
            //Cutscene Effect Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();

            //loop through all LED's and assign to device layer (Order of LEDs is not important for a base layer)
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);

            ListLedGroup layergroup;
            var ledArray = devices.SelectMany(d => d).Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

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



            if (!layer.Enabled || !effectSettings.effect_cutscenes)
            {
                layergroup.RemoveAllDecorators();

                if (runningEffects.Contains(layergroup))
                    runningEffects.Remove(layergroup);

                layergroup.Brush = new SolidColorBrush(Color.Transparent);
                layergroup.Detach();

                model._inCutscene = false;
                model.wasDisabled = true;
                return;
            }


            var baseColor = ColorHelper.ColorToRGBColor(_colorPalette.CutsceneBase.Color);
            var highlightColors = new Color[] {
                ColorHelper.ColorToRGBColor(_colorPalette.CutsceneHighlight1.Color),
                ColorHelper.ColorToRGBColor(_colorPalette.CutsceneHighlight2.Color),
                ColorHelper.ColorToRGBColor(_colorPalette.CutsceneHighlight3.Color)
            };

            var animationGradient = new LinearGradient(new GradientStop((float)0, baseColor), 
                new GradientStop((float)0.20, highlightColors[0]), 
                new GradientStop((float)0.35, baseColor),
                new GradientStop((float)0.50, highlightColors[1]),
                new GradientStop((float)0.65, baseColor),
                new GradientStop((float)0.80, highlightColors[2]),
                new GradientStop((float)1.00, baseColor));

            var gradientMove = new MoveGradientDecorator(surface, 80, true);
            var animation = new StarfieldDecorator(layergroup, (layergroup.Count() / 4), 10, 500, highlightColors, surface, false, baseColor);

            //Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;


                if (model._inCutscene != getCurrentPlayer.Entity.InCutscene || model.wasDisabled || layer.requestUpdate)
                {
                    if (getCurrentPlayer.Entity.InCutscene)
                    {
                        if (runningEffects.Contains(layergroup))
                        {
                            runningEffects.Remove(layergroup);
                        }

                        layergroup.RemoveAllDecorators();
                        animationGradient.WrapGradient = true;
                        animationGradient.AddDecorator(gradientMove);
                                                
                        layergroup.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), animationGradient)); //new SolidColorBrush(baseColor);

                        //layergroup.AddDecorator(animation);

                        runningEffects.Add(layergroup);

                    }
                    else
                    {
                        if (!model.wasDisabled && layergroup != null)
                        {
                            layergroup.RemoveAllDecorators();
                            layergroup.Brush = new SolidColorBrush(Color.Transparent);
                            //layergroup.Detach();

                            if (runningEffects.Contains(layergroup))
                                runningEffects.Remove(layergroup);
                        }
                        
                    }

                    model._inCutscene = getCurrentPlayer.Entity.InCutscene;
                }
                
                model.wasDisabled = false;
                
            }

            model.init = true;
            layer.requestUpdate = false;
        }

        private class CutsceneAnimationEffectModel
        {   
            public bool _inCutscene { get; set; }
            public bool wasDisabled { get; set; }
            public SolidColorBrush activeBrush { get; set;}
            public bool init { get; set; }
        }
    }
}
