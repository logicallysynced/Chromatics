using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.Models;
using RGB.NET.Core;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.CoolerMaster;
using RGB.NET.Devices.Corsair;
using RGB.NET.Devices.Logitech;
using RGB.NET.Devices.Msi;
using RGB.NET.Devices.Novation;
using RGB.NET.Devices.Razer;
using RGB.NET.Devices.SteelSeries;
using RGB.NET.Devices.Wooting;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Groups;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Core
{
    public delegate void WasPreviewed();

    public static class RGBController
    {
        private static RGBSurface surface = new RGBSurface();

        private static bool _loaded;

        private static bool _wasPreviewed;

        private static List<IRGBDevice> _devices = new List<IRGBDevice>();

        private static Dictionary<int, PublicListLedGroup[]> _layergroups = new Dictionary<int, PublicListLedGroup[]>();

        private static List<Led> _layergroupledcollection = new List<Led>();

        private static PaletteColorModel _colorPalette = new PaletteColorModel();

        private static EffectTypesModel _effects = new EffectTypesModel();

        private static List<PublicListLedGroup> _runningEffects = new List<PublicListLedGroup>();

        private static bool _baseLayerEffectRunning;
                
        public static event WasPreviewed PreviewTriggered;


        public static void Setup()
        {
            //Bind to console
            Logger.WriteConsole(Enums.LoggerTypes.Devices, @"Looking for RGB Devices..");

            var appSettings = AppSettings.GetSettings();

            surface.Exception += args_ => throw args_.Exception;//Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Device Error: {args_.Exception.Message}");

            //Load devices

            if (appSettings.deviceLogitechEnabled)
                surface.Load(LogitechDeviceProvider.Instance, RGBDeviceType.All);

            if (appSettings.deviceCorsairEnabled)
                surface.Load(CorsairDeviceProvider.Instance, RGBDeviceType.All);
            
            if (appSettings.deviceCoolermasterEnabled)
                surface.Load(CoolerMasterDeviceProvider.Instance, RGBDeviceType.All);
            
            if (appSettings.deviceNovationEnabled)
                surface.Load(NovationDeviceProvider.Instance, RGBDeviceType.All);
            
            if (appSettings.deviceRazerEnabled)
                surface.Load(RazerDeviceProvider.Instance, RGBDeviceType.All);
            
            if (appSettings.deviceAsusEnabled)
                surface.Load(AsusDeviceProvider.Instance, RGBDeviceType.All);
            
            if (appSettings.deviceMsiEnabled)
                surface.Load(MsiDeviceProvider.Instance, RGBDeviceType.All);
            
            if (appSettings.deviceSteelseriesEnabled)
                surface.Load(SteelSeriesDeviceProvider.Instance, RGBDeviceType.All);
            
            if (appSettings.deviceWootingEnabled)
                surface.Load(WootingDeviceProvider.Instance, RGBDeviceType.All);

            if (appSettings.deviceHueEnabled)
                surface.Load(HueRGBDeviceProvider.Instance, RGBDeviceType.All);
                


            var deviceCount = 0;
            foreach (var surfaceDevice in surface.Devices)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Found {surfaceDevice.DeviceInfo.Manufacturer} {surfaceDevice.DeviceInfo.DeviceType}: {surfaceDevice.DeviceInfo.DeviceName}.");
                _devices.Add(surfaceDevice);
                deviceCount++;

            }
            
            if (appSettings.rgbRefreshRate <= 0) appSettings.rgbRefreshRate = 0.05;

            var TimerTrigger = new TimerUpdateTrigger();
            TimerTrigger.UpdateFrequency = appSettings.rgbRefreshRate;
            surface.RegisterUpdateTrigger(TimerTrigger);

            surface.AlignDevices();
            surface.Updating += Surface_Updating;

            //Startup Effects
            //RunStartupEffects();

                        
            Logger.WriteConsole(Enums.LoggerTypes.Devices, $"{deviceCount} devices loaded.");
            _loaded = true;
        }
                

        public static void Unload()
        {
            if (!_loaded) return;

            try
            {
                surface.Updating -= Surface_Updating;
                surface?.Dispose(); 
            } catch { }
        }

        public static bool IsLoaded()
        {
            return _loaded;
        }

        public static PaletteColorModel GetActivePalette()
        {
            return _colorPalette;
        }

        public static bool LoadColorPalette()
        {
            if (FileOperationsHelper.CheckColorMappingsExist())
            {
                _colorPalette = FileOperationsHelper.LoadColorMappings();

                return true;
            }

            return false;
        }

        public static bool SaveColorPalette()
        {
            FileOperationsHelper.SaveColorMappings(_colorPalette);
            
            foreach (var layer in MappingLayers.GetLayers())
            {
                layer.Value.requestUpdate = true;
            }

            return true;
        }

        public static bool ImportColorPalette()
        {
            var colorPalette = FileOperationsHelper.ImportColorMappings();

            if (colorPalette != null)
            {
                _colorPalette = colorPalette;

                SaveColorPalette();
                return true;
            }

            return false;
        }

        public static bool ExportColorPalette()
        {
            FileOperationsHelper.ExportColorMappings(_colorPalette);
            return true;
        }

        public static EffectTypesModel GetEffectsSettings()
        {
            return _effects;
        }

        public static bool LoadEffectsSettings()
        {
            if (FileOperationsHelper.CheckEffectSettingsExist())
            {
                _effects = FileOperationsHelper.LoadEffectSettings();

                return true;
            }

            return false;
        }

        public static bool SaveEffectsSettings()
        {
            FileOperationsHelper.SaveEffectSettings(_effects);
            return true;
        }

        public static void RunStartupEffects()
        {
            if (!_effects.effect_startupanimation) return;

            var devices = surface.GetDevices(RGBDeviceType.All);

            var move = new MoveGradientDecorator(surface)
            {
                IsEnabled = true,
                Speed = 100,
            };

            foreach (var device in devices)
            {
                var gradient = new RainbowGradient();
                var ledgroup = new PublicListLedGroup(surface);

                ledgroup.ZIndex = 1000;
                foreach (var led in device)
                {
                    ledgroup.AddLed(led);
                }

                gradient.AddDecorator(move);

                if (device.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
                {
                    ledgroup.Brush = new TextureBrush(new ConicalGradientTexture(new Size(100, 100), gradient));
                }
                else
                {
                    ledgroup.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), gradient));
                }
                    

                _runningEffects.Add(ledgroup);
            }
        }

        public static void StopEffects(bool gameFirstConnected = false)
        {
            foreach (var effects in _runningEffects)
            {
                foreach (var decorator in effects.Decorators)
                {
                    decorator.IsEnabled = false;
                }
                                        
                effects.RemoveAllDecorators();
                effects.Detach();
            }

            _runningEffects.Clear();

            #if DEBUG
                Debug.WriteLine($"Stopping all effects");
            #endif

        }

        public static bool IsBaseLayerEffectRunning()
        {
            return _baseLayerEffectRunning;
        }

        public static void SetBaseLayerEffect(bool toggle)
        {
            _baseLayerEffectRunning = toggle;
        }

        public static List<PublicListLedGroup> GetRunningEffects()
        {
            return _runningEffects;
        }

        public static RGBSurface GetLiveSurfaces()
        {
            return surface;
        }

        public static List<IRGBDevice> GetLiveDevices()
        {
            return _devices;
        }

        public static List<Led> GetLiveLayerGroupCollection()
        {
            return _layergroupledcollection;
        }

        public static Dictionary<int, PublicListLedGroup[]> GetLiveLayerGroups()
        {
            return _layergroups;
        }

        public static void RemoveLayerGroup(int targetId)
        {
            if (_layergroups.ContainsKey(targetId))
            {
                foreach (var layer in _layergroups[targetId])
                {
                    layer.RemoveAllDecorators();
                    layer.Detach();

                    if (_runningEffects.Contains(layer))
                        _runningEffects.Remove(layer);
                }
                
                _layergroups.Remove(targetId);
                               
                #if DEBUG
                    Debug.WriteLine(@"Remove Layer Requested: " + targetId);
                #endif
            }
        }

        public static void ResetLayerGroups()
        {
            foreach (var layergroup in _layergroups)
            {
                foreach (var layer in layergroup.Value)
                {
                    layer.RemoveAllDecorators();
                    layer.Detach();
                }
            }

            foreach (var mapping in MappingLayers.GetLayers())
            {
                mapping.Value.requestUpdate = true;
            }

            _runningEffects.Clear();
            _layergroups.Clear();
            _layergroupledcollection.Clear();

            #if DEBUG
                Debug.WriteLine(@"Reset Layer Groups");
            #endif
        }

        public static bool WasPreviewed()
        {
            return _wasPreviewed;
        }

        private static void Surface_Updating(UpdatingEventArgs args)
        {
            if (!_loaded) return;

            //If Previewing layers - update RGB on each refresh cycle
            if (MappingLayers.IsPreview())
            {
                if (!_wasPreviewed)
                {
                    //Release any running effects
                    StopEffects();
                    ResetLayerGroups();
                    _wasPreviewed = true;
                

                    var layers = MappingLayers.GetLayers().OrderBy(x => x.Value.zindex);
                                       

                    //Display mappings on devices
                    foreach (var layer in layers)
                    {
                        var mapping = layer.Value;
                        
                        if (mapping.rootLayerType == Enums.LayerType.EffectLayer) continue;

                        //Loop through all LED's and assign to device layer
                        var devices = surface.GetDevices(mapping.deviceType);

                        var layergroup = new PublicListLedGroup(surface)
                        {
                            ZIndex = mapping.zindex,
                        };

                        if (_layergroups.ContainsKey(mapping.layerID))
                        {
                            layergroup = _layergroups[mapping.layerID].FirstOrDefault();
                        }
                        else
                        {
                            var lg = new PublicListLedGroup[1];
                            lg[0] = layergroup;
                            _layergroups.Add(mapping.layerID, lg);
                        }


                        var drawing_col = (System.Drawing.Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(mapping.rootLayerType).Value;
                        var highlight_col = ColorHelper.ColorToRGBColor(drawing_col);

                        foreach (var device in devices)
                        {
                            if (!_devices.Contains(device)) continue;

                            foreach (var led in device)
                            {
                                if (!mapping.deviceLeds.Any(v => v.Value.Equals(led.Id)))
                                {

                                    layergroup.RemoveLed(led);
                                    _layergroupledcollection.Remove(led);
                                    continue;
                                }

                                if (!mapping.Enabled && mapping.rootLayerType == Enums.LayerType.BaseLayer)
                                {
                                    drawing_col = System.Drawing.Color.Black;
                                    highlight_col = ColorHelper.ColorToRGBColor(drawing_col);
                                }
                                else if (!mapping.Enabled)
                                {
                                    if (_layergroupledcollection.Contains(led))
                                    {
                                        layergroup.RemoveLed(led);
                                        _layergroupledcollection.Remove(led);
                                    }

                                    continue;
                                }

                                if (led.Color != highlight_col)
                                {
                                    layergroup.RemoveLed(led);
                                    led.Color = highlight_col;
                                }

                                if (!_layergroupledcollection.Contains(led))
                                {
                                    _layergroupledcollection.Add(led);
                                }

                                layergroup.AddLed(led);
                                layergroup.Detach();

                            }

                        }

                        //Apply lighting
                        var brush = new SolidColorBrush(highlight_col);

                        layergroup.Brush = brush;
                        layergroup.Attach(surface);
                    }
                }
            }
            else
            {
                if (_wasPreviewed)
                {            
                    ResetLayerGroups();
                    
                    if (!GameController.IsGameConnected())
                        RunStartupEffects();
                    
                    _wasPreviewed = false;
                    PreviewTriggered?.Invoke(); 
                }
            }
        }
    }
}
