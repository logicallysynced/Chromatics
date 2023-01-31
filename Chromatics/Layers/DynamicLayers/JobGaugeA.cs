using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public class JobGaugeAProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {
            //Job Gauge A Dynamic Layer Implementation
            if (MappingLayers.IsPreview()) return;

            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetJobResources())
            {
                var getJobResources = _memoryHandler.Reader.GetJobResources();
                if (getJobResources.JobResourcesContainer == null) return;

                Debug.WriteLine($"Song Timer: {getJobResources.JobResourcesContainer.Bard.Timer}");
                Debug.WriteLine($"Stacks: {getJobResources.JobResourcesContainer.Bard.Repertoire}");
                Debug.WriteLine($"Voice: {getJobResources.JobResourcesContainer.Bard.SoulVoice}");
                Debug.WriteLine($"Song: {getJobResources.JobResourcesContainer.Bard.ActiveSong}");
            }
        }
    }
}
