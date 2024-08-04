using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using HidSharp;
using OpenRGB.NET;
using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using System.Drawing;
using Chromatics.Forms;
using System.Runtime.Serialization;
using Org.BouncyCastle.Utilities.Collections;

namespace Chromatics.Layers
{
    public static class MappingLayers
    {
        private static int _layerAutoID = 0;

        private static int _version = 0;

        private static System.Timers.Timer _timer;

        private static int _prev = 0;

        private static bool _preview;
        
        private static ConcurrentDictionary<int, Layer> _layers = new ConcurrentDictionary<int, Layer>();


        public static int AddLayer(int index, LayerType rootLayerType, Guid deviceGuid, RGBDeviceType deviceType, int layerTypeIndex, int zindex, bool enabled, Dictionary<int, LedId> deviceLeds, bool allowBleed, LayerModes layerModes)
        {
            _layerAutoID++;
            var id = _layerAutoID;
            var layer = new Layer(AppSettings.currentMappingLayerVersion, id, index, rootLayerType, deviceGuid, deviceType, layerTypeIndex, zindex, enabled, deviceLeds, allowBleed, layerModes);
            _layers.GetOrAdd(id, layer);
            _version++;


            return id;
        }

        public static void UpdateLayer(Layer layer)
        {
            if (_layers.ContainsKey(layer.layerID))
            {
                var kvp = _layers.FirstOrDefault(x => x.Value.layerID == layer.layerID);
                _layers.TryUpdate(kvp.Key, layer, kvp.Value);
                _version++;
            }
        }

        public static void RemoveLayer(int id)
        {
            if (_layers.ContainsKey(id))
            {
                var kvp = _layers.GetValueOrDefault(id);
                var result = _layers.TryRemove(new KeyValuePair<int, Layer>(id, kvp));
                _version++;
            }
        }

        public static ConcurrentDictionary<int, Layer> GetLayers()
        {
            return _layers;
        }

        public static Layer GetLayer(int index)
        {
            if (_layers.Any(x => x.Value.layerID == index))
            {
                var kvp = _layers.FirstOrDefault(x => x.Value.layerID == index);
                return kvp.Value;
            }

            return null;
        }

        public static int CountLayers()
        {
            return _layers.Count;
        }

        public static int GetNextLayerID()
        {
            return _layerAutoID + 1;
        }

        public static bool LoadMappings(bool over = false)
        {
            if (FileOperationsHelper.CheckLayerMappingsExist())
            {
                var _Templayers = FileOperationsHelper.LoadLayerMappings();

                if (_Templayers == null)
                {
                    return false;
                }

                var flag = false;
                var empty = false;

                foreach (var mapping in _Templayers)
                {
                    if (mapping.Value.layerVersion != AppSettings.currentMappingLayerVersion || mapping.Value.layerVersion == null || mapping.Value.deviceGuid == Guid.Empty)
                    {
                        flag = true;

                        if (mapping.Value.deviceGuid == Guid.Empty)
                        {
                            empty = true;
                        }
                    }
                }

                if (flag || over)
                {
                    Debug.WriteLine("Flagged for upgrade");

                    if (!empty)
                    {
                        FileOperationsHelper.CreateLayersBackup();
                    }

                    QueueImportMappings(_Templayers, empty);
                }
                else
                {
                    _layers.Clear();
                    _layers = _Templayers;
                }

                _layerAutoID = _layers.LastOrDefault().Key;
                _version++;

                return true;
            }

            return false;
        }

        private static void QueueImportMappings(ConcurrentDictionary<int, Layer> importedLayer, bool empty = false)
        {
            // Start a timer to check periodically if RGBController is loaded
            _timer = new System.Timers.Timer(1000); // Check every second
            _timer.Elapsed += (sender, e) => CheckRGBControllerLoaded(importedLayer, empty);
            _timer.Start();
        }

        private static void CheckRGBControllerLoaded(object state, bool empty)
        {
            if (RGBController.IsLoaded())
            {
                _timer.Dispose();
                ImportMappings(state as ConcurrentDictionary<int, Layer>, empty);

                if (Uc_Mappings.Instance.InvokeRequired)
                {
                    Uc_Mappings.Instance.Invoke(new MethodInvoker(() => Uc_Mappings.Instance.ChangeDeviceType()));
                }
                else
                {
                    Uc_Mappings.Instance.ChangeDeviceType();
                }
            }
        }

        public static bool SaveMappings()
        {
            FileOperationsHelper.SaveLayerMappings(_layers);

            /*
            foreach (var layer in _layers)
            {
                Debug.WriteLine($"Saving layer: {layer.Key}, Version: {layer.Value.layerVersion}, Guid: {layer.Value.deviceGuid}");
            }
            */

            return true;
        }

        public static bool ImportMappings(ConcurrentDictionary<int, Layer> importedLayer = null, bool empty = false)
        {
            var layers = importedLayer ?? FileOperationsHelper.ImportLayerMappings();

            if (layers != null)
            {


                // Log layers and their versions
#if DEBUG
                Debug.WriteLine($"Total layers loaded: {layers.Count}");

                foreach (var layer in layers)
                {
                    Debug.WriteLine($"Layer ID: {layer.Key}, Version: {layer.Value.layerVersion}");
                }
#endif
                var migratedLayers = new Dictionary<int, Layer>();
                var layersToMigrate = layers.Where(l => l.Value.layerVersion == "1").GroupBy(l => l.Value.deviceType);

                if (importedLayer != null && !empty)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.System, $"Detected version 1 layers to migrate. Count: {layersToMigrate.Count()}.");
                }

                var currentDevices = RGBController.GetLiveDevices();

                foreach (var group in layersToMigrate)
                {
                    var deviceType = group.Key;
                    var deviceLayers = group.ToList();

                    Debug.WriteLine($"Device Type: {deviceType}, Layers Count: {deviceLayers.Count}");

                    var availableDevices = currentDevices.Where(d => d.Value.DeviceInfo.DeviceType == deviceType).ToList();
                    if (availableDevices.Count == 0)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.System, $"No available devices of type {deviceType} to migrate layers.");
                        continue;
                    }

                    var selectedDeviceGuid = availableDevices.First().Key; // Automatically select the first available device

                    foreach (var layer in deviceLayers)
                    {
                        layer.Value.deviceGuid = selectedDeviceGuid;
                        layer.Value.layerVersion = AppSettings.currentMappingLayerVersion;
                        migratedLayers[layer.Key] = layer.Value;
                    }
                }

                // Process layers with blank deviceGuid
                var layersWithBlankGuid = layers.Where(l => l.Value.deviceGuid == Guid.Empty).GroupBy(l => l.Value.deviceType);

                foreach (var group in layersWithBlankGuid)
                {
                    var deviceType = group.Key;
                    var deviceLayers = group.ToList();

                    Debug.WriteLine($"Device Type: {deviceType}, Layers Count: {deviceLayers.Count} with blank GUID");

                    var availableDevices = currentDevices.Where(d => d.Value.DeviceInfo.DeviceType == deviceType).ToList();
                    if (availableDevices.Count == 0)
                    {
                        //Logger.WriteConsole(Enums.LoggerTypes.System, $"No available devices of type {deviceType} to assign layers with blank GUID.");
                        continue;
                    }

                    foreach (var device in availableDevices)
                    {
                        foreach (var layer in deviceLayers)
                        {
                            var newLayer = new Layer(
                                layer.Value.layerVersion,
                                _layerAutoID++,
                                layer.Value.layerIndex,
                                layer.Value.rootLayerType,
                                device.Key, // Assign to current device
                                layer.Value.deviceType,
                                layer.Value.layerTypeindex,
                                layer.Value.zindex,
                                layer.Value.Enabled,
                                new Dictionary<int, LedId>(layer.Value.deviceLeds),
                                layer.Value.allowBleed,
                                layer.Value.layerModes
                            );
                            migratedLayers[newLayer.layerID] = newLayer;
                        }
                    }
                }

                // Clear out version 1 layers and add non-migrated layers (layerVersion != "1")
                _layers.Clear();
                foreach (var layer in layers.Where(l => l.Value.layerVersion != "1" && l.Value.deviceGuid != Guid.Empty))
                {
                    _layers[layer.Key] = layer.Value;
                }

                // Add migrated layers
                foreach (var layer in migratedLayers)
                {
                    _layers[layer.Key] = layer.Value;
                }

                _layerAutoID = _layers.LastOrDefault().Key;
                _version++;

                SaveMappings();

                return true;
            }

            return false;
        }


        public static bool ExportMappings()
        {
            FileOperationsHelper.ExportLayerMappings(_layers);
            return true;
        }

        public static bool IsPreview()
        {
            return _preview;
        }

        public static void SetPreview(bool value)
        {
            foreach (var layer in _layers)
            {
                layer.Value.requestUpdate = true;
            }
            
            _preview = value;
        }

        public static bool HasChanged()
        {
            if (_version != _prev)
            {
                _prev = _version;
                return true;
            }

            return false;
        }

        public static int ChangeVersion()
        {
            return _version;
        }
    }

    public class Layer : IMappingLayer
    {
        public string layerVersion { get; set; }
        public int layerID { get; set; }
        public int layerIndex { get; set; }
        public LayerType rootLayerType { get; set; }
        public Guid deviceGuid { get; set; }
        public RGBDeviceType deviceType { get; set; }
        public bool Enabled { get; set; }
        public int zindex { get; set; }
        public bool allowBleed { get; set; }
        public int layerTypeindex { get; set; }
        public Dictionary<int, LedId> deviceLeds { get; set; }
        public LayerModes layerModes { get; set; }
        public bool requestUpdate { get; set; }

        public Layer(string _layerVersion, int _id, int _index, LayerType _rootLayerType, Guid _deviceGuid, RGBDeviceType _deviceType, int _layerTypeIndex, int _zindex, bool _enabled, Dictionary<int, LedId> _deviceLeds, bool _allowBleed, LayerModes _mode)
        {
            layerVersion = _layerVersion;
            layerID = _id;
            layerIndex = _index;
            rootLayerType = _rootLayerType;
            deviceGuid = _deviceGuid;
            deviceType = _deviceType;
            layerTypeindex = _layerTypeIndex;
            zindex = _zindex;
            Enabled = _enabled;
            deviceLeds = _deviceLeds;
            allowBleed = _allowBleed;
            requestUpdate = false;
            layerModes = _mode;
        }
    }

}
