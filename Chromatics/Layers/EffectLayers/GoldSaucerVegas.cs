using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using Sharlayan.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public class GoldSaucerVegasProcessor : LayerProcessor
    {
        private static Dictionary<int, GoldSaucerVegasEffectModel> layerProcessorModel = new Dictionary<int, GoldSaucerVegasEffectModel>();

        public override void Process(IMappingLayer layer)
        {
            //Gold Saucer Vegas Mode Effect Layer Implementation - NOTE: This effect operates on the base layer. Disabling base layer wont disable this effect however
            var effectSettings = RGBController.GetEffectsSettings();
            var _layergroups = RGBController.GetLiveLayerGroups();
            
            

            GoldSaucerVegasEffectModel model;
            ListLedGroup layergroup;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new GoldSaucerVegasEffectModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }
            

            if (model.isEnabled != effectSettings.effect_vegasmode && model.init)
            {
                if (!effectSettings.effect_vegasmode)
                {
                    if (model.gradientEffects != null)
                    {
                        model.gradientEffects.RemoveAllDecorators();
                        RGBController.SetBaseLayerEffect(false);
                        RGBController.ResetLayerGroups();

                        model.gradientEffects = null;
                    }
                    
                }
                else
                {
                    model.reEnabled = true;
                }

                model.isEnabled = effectSettings.effect_vegasmode;
            }

            if (!model.isEnabled)
            {
                if (!model.init)
                    model.init = true;

                return;
            }
            var _memoryHandler = GameController.GetGameData();
            

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActors())
            {
                var getCurrentPlayer = _memoryHandler.Reader.GetCurrentPlayer();
                if (getCurrentPlayer.Entity == null) return;

                //var currentZone = ZoneLookup.GetZoneInfo(getCurrentPlayer.Entity.MapTerritory).Name.English;
                var currentZone = GameHelper.GetZoneNameById(getCurrentPlayer.Entity.MapTerritory);

                var baseLayer = MappingLayers.GetLayers().Values.Where(x => x.rootLayerType == Enums.LayerType.BaseLayer && x.deviceType == layer.deviceType).FirstOrDefault();

                if (baseLayer.deviceType == RGBDeviceType.Keyboard)
                {
                    model.test = true;
                }

                var device = GetDevice(layer);
                if (device == null)
                {
                    return;
                }

                var ledArray = device.Where(led => baseLayer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

                if (_layergroups.ContainsKey(baseLayer.layerID))
                {
                    layergroup = _layergroups[baseLayer.layerID].FirstOrDefault();
                }
                else
                {
                    layergroup = new ListLedGroup(surface, ledArray)
                    {
                        ZIndex = baseLayer.zindex,
                    };

                    var lg = new ListLedGroup[] { layergroup };
                    _layergroups.Add(baseLayer.layerID, lg);

                    if (model.gradientEffects != null)
                    {
                        model.gradientEffects.RemoveAllDecorators();
                        model.gradientEffects = null;
                    }
                    //layergroup.Detach();
                }

                if (currentZone != model._currentZone || model.reEnabled || baseLayer.requestUpdate)
                {   
                    var runningEffects = RGBController.GetRunningEffects();
                    var gradient = new RainbowGradient();
                    var effect = new MoveGradientDecorator(surface)
                    {
                        IsEnabled = true,
                        Speed = 100,
                    };

                    if (currentZone == "The Gold Saucer")
                    {
                        if (runningEffects.Contains(layergroup))
                            runningEffects.Remove(layergroup);

                        layergroup.RemoveAllDecorators();

                        if (model.gradientEffects == null)
                        {
                            gradient.AddDecorator(effect);
                            RGBController.SetBaseLayerEffect(true);

                            layergroup.Brush = new TextureBrush(new ConicalGradientTexture(new Size(100, 100), gradient));

                            runningEffects.Add(layergroup);
                            model.gradientEffects = gradient;
                        }
                    }
                    else
                    {
                        if (model.gradientEffects != null)
                        {
                            if (runningEffects.Contains(layergroup))
                                runningEffects.Remove(layergroup);

                            layergroup.RemoveAllDecorators();

                            RGBController.SetBaseLayerEffect(false);
                            RGBController.ResetLayerGroups();
                            model.gradientEffects = null;
                        }

                    }

                    model.reEnabled = false;
                    model._currentZone = currentZone;
                }

                
            }
            
            model.init = true;
            layer.requestUpdate = false;
        }

        private class GoldSaucerVegasEffectModel
        {
            public bool init { get; set; }
            public bool isEnabled { get; set; }
            public bool reEnabled { get; set; }
            public string _currentZone { get; set; }
            public RainbowGradient gradientEffects { get; set; }
            public bool test { get; set; }
        }
    }
}
