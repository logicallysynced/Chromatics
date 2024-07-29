using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromatics.Enums;
using RGB.NET.Core;

namespace Chromatics.Interfaces
{
    public interface IMappingLayer
    {
        int layerID { get; set; }
        int layerIndex { get; set; }
        int zindex { get; set; }
        LayerType rootLayerType { get; }
        public Guid deviceGuid { get; set; }
        public RGBDeviceType deviceType { get; set; }
        LayerModes layerModes { get; set; }
        bool Enabled { get; set; }
        bool allowBleed { get; set; }
        int layerTypeindex { get; set; }
        bool requestUpdate { get; set; }
        Dictionary<int, LedId> deviceLeds { get; set; }

    }
}
