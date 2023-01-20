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
        RGBDeviceType deviceType {get; set;}
        bool Enabled { get; set; }
        int layerTypeindex { get; set; }
        Dictionary<int, LedId> deviceLeds { get; set; }

    }
}
