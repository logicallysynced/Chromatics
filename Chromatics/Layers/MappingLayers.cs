using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public static class MappingLayers
    {
        private static int _layerAutoID = 0;

        private static int _version = 0;

        private static int _prev = 0;

        private static bool _preview;
        
        private static ConcurrentDictionary<int, Layer> _layers = new ConcurrentDictionary<int, Layer>();

        public static int AddLayer(int index, LayerType rootLayerType, RGBDeviceType devicetype, int layerTypeIndex, int zindex, bool enabled, Dictionary<int, LedId> deviceLeds, bool allowBleed, LayerModes layerModes)
        {
            _layerAutoID++;

            var id = _layerAutoID;

            var layer = new Layer(id, index, rootLayerType, devicetype, layerTypeIndex, zindex, enabled, deviceLeds, allowBleed, layerModes);
            _layers.GetOrAdd(id, layer);
            _version++;

            #if DEBUG
                Debug.WriteLine(@"New Layer: " + id + @". zindex: " + zindex + @". Type: " + rootLayerType + @". Device: " + devicetype + @". LayerType: " + layerTypeIndex);
            #endif

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

        public static bool LoadMappings()
        {
            if (FileOperationsHelper.CheckLayerMappingsExist())
            {
                _layers.Clear();
                _layers = FileOperationsHelper.LoadLayerMappings();

                if (_layers == null)
                {
                    return false;
                }

                _layerAutoID = _layers.LastOrDefault().Key;
                _version++;

                return true;
            }

            return false;
        }

        public static bool SaveMappings()
        {
            FileOperationsHelper.SaveLayerMappings(_layers);

            return true;
        }

        public static bool ImportMappings()
        {
            var layers = FileOperationsHelper.ImportLayerMappings();

            if (layers != null)
            {
                _layers.Clear();
                _layers = layers;
                _layerAutoID = _layers.LastOrDefault().Key;
                _version++;

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
        public int layerID { get; set; }
        public int layerIndex { get; set; }
        public LayerType rootLayerType { get; set; }
        public RGBDeviceType deviceType { get; set; }
        public bool Enabled { get; set; }
        public int zindex { get; set; }
        public bool allowBleed { get; set; }
        public int layerTypeindex { get; set; }
        public Dictionary<int, LedId> deviceLeds { get; set; }
        public LayerModes layerModes { get; set; }
        public bool requestUpdate { get; set; }

        public Layer(int _id, int _index, LayerType _rootLayerType, RGBDeviceType _devicetype, int _layerTypeIndex, int _zindex, bool _enabled, Dictionary<int, LedId> _deviceLeds, bool _allowBleed, LayerModes _mode)
        {
            layerID = _id;
            layerIndex = _index;
            rootLayerType = _rootLayerType;
            deviceType = _devicetype;
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
