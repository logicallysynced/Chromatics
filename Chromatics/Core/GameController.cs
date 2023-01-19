using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharlayan;
using Sharlayan.Enums;
using Sharlayan.Models;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.Interfaces;
using Chromatics.Enums;

namespace Chromatics.Core
{
    public static class GameController
    {
        private static CustomComparers.LayerComparer comparer = new();
        private static bool gameConnected;

        public static void Setup()
        {
            comparer = new CustomComparers.LayerComparer();
        }

        private static void ConnectFFXIVClient()
        {
            /*
            var processes = Process.GetProcessesByName("ffxiv_dx11");
            if (processes.length > 0)
            {
                // supported: Global, Chinese, Korean
                GameRegion gameRegion = GameRegion.Global;
                GameLanguage gameLanguage = GameLanguage.English;
	            
                // whether to always hit API on start to get the latest sigs based on patchVersion, or use the local json cache (if the file doesn't exist, API will be hit)
	            bool useLocalCache = true;

	            // patchVersion of game, or latest
	            string patchVersion = "latest";
                Process process = processes[0];
                ProcessModel processModel = new ProcessModel {
                    Process = process
                }
                
                SharlayanConfiguration configuration = new SharlayanConfiguration {
                    ProcessModel = processModel,
                    GameLanguage = gameLanguage,
                    GameRegion = gameRegion,
                    PatchVersion = patchVersion,
                    UseLocalCache = Settings.Default.UseLocalMemoryJSONDataCache
                };
                
                MemoryHandler memoryHandler = SharlayanMemoryManager.Instance.AddHandler(configuration);
                gameConnected = true;
            }
            */
        }

        private static void GameMemoryReadLoop()
        {
            if (!gameConnected) return;

            var _layers = MappingLayers.GetLayers();

            foreach (IMappingLayer layer in _layers.Values.OrderBy(x => x.zindex, comparer))
            {
                // Perform processing on the layer
                if (!layer.Enabled) continue;

                switch (layer.rootLayerType)
                {
                    case LayerType.BaseLayer:

                        var baseLayerProcessors = BaseLayerProcessorFactory.GetProcessors();
                        baseLayerProcessors[(BaseLayerType)layer.layerTypeindex].Process(layer);
                        break;

                    case LayerType.DynamicLayer:
                        
                        var dynamicLayerProcessors = DynamicLayerProcessorFactory.GetProcessors();
                        dynamicLayerProcessors[(DynamicLayerType)layer.layerTypeindex].Process(layer);
                        break;

                    case LayerType.EffectLayer:

                        var effectLayerProcessors = EffectLayerProcessorFactory.GetProcessors();
                        effectLayerProcessors[(EffectLayerType)layer.layerTypeindex].Process(layer);
                        break;
                }
            }
        }

    }
}
