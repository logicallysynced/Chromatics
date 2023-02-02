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

            PublicListLedGroup layergroup;
            var ledArray = devices.SelectMany(d => d).Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;
            }
            else
            {
                layergroup = new PublicListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new PublicListLedGroup[] { layergroup };
                _layergroups.Add(layer.layerID, lg);
                layergroup.Detach();
            }



            if (!layer.Enabled || !effectSettings.effect_cutscenes)
            {
                layergroup.RemoveAllDecorators();

                if (runningEffects.Contains(layergroup))
                    runningEffects.Remove(layergroup);

                if (!layer.Enabled)
                {
                    layergroup.Brush = new SolidColorBrush(Color.Transparent);
                    //layergroup.Detach();
                }

                model._inCutscene = false;
                model.wasDisabled = true;
                return;
            }

            var baseColor = ColorHelper.ColorToRGBColor(_colorPalette.MenuBase.Color);
            var highlightColors = new Color[] {
                ColorHelper.ColorToRGBColor(_colorPalette.MenuHighlight1.Color),
                ColorHelper.ColorToRGBColor(_colorPalette.MenuHighlight2.Color),
                ColorHelper.ColorToRGBColor(_colorPalette.MenuHighlight3.Color)
            };

            var animation = new StarfieldDecorator(layergroup, (layergroup.PublicGroupLeds.Count / 4), 10, 500, highlightColors, surface, false);


            //Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                //Debug.WriteLine($"InCutscene: {getCurrentPlayer.Entity.InCutscene}");

                if (model._inCutscene != getCurrentPlayer.Entity.InCutscene || model.wasDisabled)
                {
                    if (getCurrentPlayer.Entity.InCutscene)
                    {
                        if (runningEffects.Contains(layergroup))
                                runningEffects.Remove(layergroup);


                        //layergroup.RemoveAllDecorators();

                        layergroup.Brush = new SolidColorBrush(baseColor);
                        layergroup.AddDecorator(animation);                    
                        

                        runningEffects.Add(layergroup);

                        if (layer.deviceType == RGBDeviceType.Keyboard)
                            Debug.WriteLine($"Apply Effect: {layergroup.PublicGroupLeds.Count}. zindex: {layergroup.ZIndex}");
                    }
                    else
                    {
                        if (!model.wasDisabled)
                        {
                            if (layer.deviceType == RGBDeviceType.Keyboard)
                                Debug.WriteLine($"Stop Effect");
                        
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

            if (layergroup.Decorators.Count == 0)
            {
                layergroup.Attach(surface);
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
