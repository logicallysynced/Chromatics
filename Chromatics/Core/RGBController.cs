using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Forms;
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
using RGB.NET.Devices.OpenRGB;
using RGB.NET.Devices.Razer;
using RGB.NET.Devices.SteelSeries;
using RGB.NET.Devices.Wooting;
using RGB.NET.Layout;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Groups;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using Sharlayan.Core.JobResources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace Chromatics.Core
{
    public delegate void WasPreviewed();

    public static class RGBController
    {
        private static RGBSurface surface = new RGBSurface();

        private static bool _loaded;

        private static bool _wasPreviewed;

        private static List<IRGBDeviceProvider> loadedDeviceProviders = new List<IRGBDeviceProvider>();

        private static Dictionary<Guid, IRGBDevice> _devices = new Dictionary<Guid, IRGBDevice>();

        private static Dictionary<int, ListLedGroup[]> _layergroups = new Dictionary<int, ListLedGroup[]>();

        private static List<Led> _layergroupledcollection = new List<Led>();

        private static PaletteColorModel _colorPalette = new PaletteColorModel();

        private static EffectTypesModel _effects = new EffectTypesModel();

        private static List<ListLedGroup> _runningEffects = new List<ListLedGroup>();

        private static bool _baseLayerEffectRunning;
                
        public static event WasPreviewed PreviewTriggered;

        private static RGBSurface.ExceptionEventHandler surfaceExceptionEventHandler;

        private static EventHandler<ExceptionEventArgs> deviceExceptionEventHandler;

        public static void Setup()
        {
            try
            {
                //Bind to console
                Logger.WriteConsole(Enums.LoggerTypes.Devices, @"Looking for RGB Devices..");

                var appSettings = AppSettings.GetSettings();

                //Setup Exception Events
                surfaceExceptionEventHandler = args_ => Logger.WriteConsole(Enums.LoggerTypes.Error, $"Device Error: {args_.Exception.Message}");
                deviceExceptionEventHandler = (sender, e) => Logger.WriteConsole(Enums.LoggerTypes.Error, $"Device Error: {e.Exception.Message}");

                surface.Exception += surfaceExceptionEventHandler;

            
                if (appSettings.deviceLogitechEnabled)
                {
                    LoadDeviceProvider(LogitechDeviceProvider.Instance);
                }
                    

                if (appSettings.deviceCorsairEnabled)
                {
                    var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    var natives = CorsairDeviceProvider.PossibleX64NativePaths;
                    natives.Add($"{enviroment}\\x64\\CUESDK.dll");

                    Debug.WriteLine($"{enviroment}\\x64\\CUESDK.dll");

                    LoadDeviceProvider(CorsairDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceCoolermasterEnabled)
                {
                    LoadDeviceProvider(CoolerMasterDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceNovationEnabled)
                {
                    LoadDeviceProvider(NovationDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceRazerEnabled)
                {
                    if (AppSettings.GetSettings().showEmulatorDevices)
                        RazerDeviceProvider.Instance.LoadEmulatorDevices = RazerEndpointType.All;

                    #if DEBUG
                        RazerDeviceProvider.Instance.LoadEmulatorDevices = RazerEndpointType.All;
                    #endif

                    LoadDeviceProvider(RazerDeviceProvider.Instance); 
                }
            
                if (appSettings.deviceAsusEnabled)
                {
                    LoadDeviceProvider(AsusDeviceProvider.Instance);
                }
                    
                
                if (appSettings.deviceMsiEnabled)
                {
                    LoadDeviceProvider(MsiDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceSteelseriesEnabled)
                {
                    LoadDeviceProvider(SteelSeriesDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceWootingEnabled)
                {
                    LoadDeviceProvider(WootingDeviceProvider.Instance);
                }

                if (appSettings.deviceOpenRGBEnabled)
                {
                    var openrgb = new OpenRGBServerDefinition
                    {
                        Port = 6742,
                        Ip = "127.0.0.1",
                        ClientName = "Chromatics"
                    };

                    OpenRGBDeviceProvider.Instance.AddDeviceDefinition(openrgb);
                    LoadDeviceProvider(OpenRGBDeviceProvider.Instance);



                }   

                if (appSettings.deviceHueEnabled)
                {
                    //LoadDeviceProvider(HueRGBDeviceProvider.Instance);
                }
                            
            
                if (appSettings.rgbRefreshRate <= 0) appSettings.rgbRefreshRate = 0.05;

                var TimerTrigger = new TimerUpdateTrigger();
                TimerTrigger.UpdateFrequency = appSettings.rgbRefreshRate;
                surface.RegisterUpdateTrigger(TimerTrigger);

                surface.AlignDevices();
                surface.Updating += Surface_Updating;

                #if DEBUG
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"{surface.Devices.Count} devices loaded.");
                #endif
                
                _loaded = true;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"RGBController Setup Error: {ex.Message}");
            }
        }

        private static void DevicesChanged(object sender, DevicesChangedEventArgs e)
        {
            var device = e.Device;
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

            //Generate GUID for Device
            int counter = 1;
            var guid = Helpers.DeviceHelper.GenerateDeviceGuid(device.DeviceInfo.DeviceName);

            while (_devices.ContainsKey(guid))
            {
                //Make GUID unique if multiple devices of same type detected

                var deviceName = device.DeviceInfo.DeviceName + counter;
                guid = Helpers.DeviceHelper.GenerateDeviceGuid(deviceName);
                counter++;
            }

            if (e.Action == DevicesChangedEventArgs.DevicesChangedAction.Added)
            {
                //Device Added

                //Handle cases where a device is loaded with 0 LEDs
                if (device.Count() <= 0 && device.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
                {
                    var path = $"{enviroment}/Layouts/Default/Keyboard/Artemis XL keyboard-ISO.xml";

                    if (File.Exists(path))
                    {
                        var layout = DeviceLayout.Load(path);
                        LayoutExtension.ApplyTo(layout, device, true);

                        #if DEBUG
                            Debug.WriteLine($"Loaded layout for {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}. New Leds: {device.Count()}");
                        #endif
                    }
                }
                else if (device.Count() <= 0 && device.DeviceInfo.DeviceType == RGBDeviceType.Headset)
                {
                    var path = $"{enviroment}/Layouts/Default/Keyboard/Artemis 4 LEDs headset.xml";

                    if (File.Exists(path))
                    {
                        var layout = DeviceLayout.Load(path);
                        LayoutExtension.ApplyTo(layout, device, true);

                        #if DEBUG
                            Debug.WriteLine($"Loaded layout for {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}. New Leds: {device.Count()}");
                        #endif
                    }
                }

                #if DEBUG
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Found {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName} (ID: {guid}).");
                #else
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Found {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName}.");
                #endif

                if (!_devices.ContainsKey(guid))
                {
                    _devices.Add(guid, device);
                    
                }

                Uc_Mappings.OnDeviceAdded(EventArgs.Empty);

            }
            else if (e.Action == DevicesChangedEventArgs.DevicesChangedAction.Removed)
            {
                //Device Removed

                #if DEBUG
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Lost {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName} (ID: {guid}).");
                #else
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Lost {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName}.");
                #endif

                if (_devices.ContainsKey(guid))
                {
                    _devices.Remove(guid);
                }

                Uc_Mappings.OnDeviceRemoved(EventArgs.Empty);
            }

            
            surface.AlignDevices();

            /*
            if (_loaded)
            {
                StopEffects();
                ResetLayerGroups();
                    
                if (!GameController.IsGameConnected())
                    RunStartupEffects();
            }
            */
        }

        public static void Unload()
        {
            if (!_loaded) return;

            try
            {
                foreach (var deviceProvider in loadedDeviceProviders)
                {
                    UnloadDeviceProvider(deviceProvider, false);
                }

                loadedDeviceProviders.Clear();

                //surface.Updating -= Surface_Updating;
                surface.Exception -= surfaceExceptionEventHandler;
                surface?.Dispose(); 
            } catch { }
        }

        public static void LoadDeviceProvider(IRGBDeviceProvider provider)
        {
            if (!loadedDeviceProviders.Contains(provider))
            {
                //Attach device provider
                foreach (var device in provider.Devices)
                {
                    Console.WriteLine(@"Device: " + device.DeviceInfo.DeviceName);
                    surface.Attach(device);
                }

                var showErrors = AppSettings.GetSettings().showDeviceErrors;

                #if DEBUG
                    showErrors = true;
                #endif

                if (showErrors)
                    provider.Exception += deviceExceptionEventHandler;

                provider.DevicesChanged += DevicesChanged;
                
                surface.Load(provider);
                loadedDeviceProviders.Add(provider);

                if (_loaded)
                {
                    StopEffects();
                    ResetLayerGroups();
                    
                    if (!GameController.IsGameConnected())
                        RunStartupEffects();
                }

            }
        }

        public static void UnloadDeviceProvider(IRGBDeviceProvider provider, bool removeFromList = true)
        {
            if (loadedDeviceProviders.Contains(provider))
            {
                foreach (var device in provider.Devices)
                {

                    //Check for device removal incase event handler isn't built into RGB.NET Provider
                    var _device = _devices.FirstOrDefault(kvp => kvp.Value == device);
                    if (_devices.ContainsKey(_device.Key))
                    {
                        #if DEBUG
                            Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Removed {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName} (ID: {_device.Key}).");
                        #else
                            Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Removed {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName}.");
                        #endif

                        _devices.Remove(_device.Key);
                    }

                    surface.Detach(device);
                }


                provider.Exception -= deviceExceptionEventHandler;
                provider.DevicesChanged -= DevicesChanged;

                if (removeFromList)
                    loadedDeviceProviders.Remove(provider);

                provider.Dispose();

                if (_loaded)
                {
                    StopEffects();
                    ResetLayerGroups();
                    
                    if (!GameController.IsGameConnected())
                        RunStartupEffects();
                }
            }
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
                var ledgroup = new ListLedGroup(surface);

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

        public static List<ListLedGroup> GetRunningEffects()
        {
            return _runningEffects;
        }

        public static RGBSurface GetLiveSurfaces()
        {
            return surface;
        }

        public static Dictionary<Guid, IRGBDevice> GetLiveDevices()
        {
            return _devices;
        }

        public static List<IRGBDeviceProvider> GetDeviceProviders()
        {
            return loadedDeviceProviders;
        }

        public static List<Led> GetLiveLayerGroupCollection()
        {
            return _layergroupledcollection;
        }

        public static Dictionary<int, ListLedGroup[]> GetLiveLayerGroups()
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

                        var layergroup = new ListLedGroup(surface)
                        {
                            ZIndex = mapping.zindex,
                        };


                        if (_layergroups.ContainsKey(mapping.layerID))
                        {
                            layergroup = _layergroups[mapping.layerID].FirstOrDefault();
                        }
                        else
                        {
                            var lg = new ListLedGroup[1];
                            lg[0] = layergroup;
                            _layergroups.Add(mapping.layerID, lg);
                        }


                        var drawing_col = (System.Drawing.Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(mapping.rootLayerType).Value;
                        var highlight_col = ColorHelper.ColorToRGBColor(drawing_col);

                        foreach (var device in devices)
                        {
                            if (!_devices.ContainsValue(device)) continue;

                            foreach (var led in device)
                            {
                                if (!mapping.deviceLeds.Any(v => v.Equals(led.Id)))
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
