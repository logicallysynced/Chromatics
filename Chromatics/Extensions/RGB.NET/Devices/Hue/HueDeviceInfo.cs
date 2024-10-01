using System;
using System.Collections.Generic;
using System.Linq;
using HueApi.Entertainment.Models;
using HueApi.Models;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue;

public class HueDeviceInfo : IRGBDeviceInfo
{
    public HueDeviceInfo(Light light)
    {
        LightId = light.IdV1;

        DeviceType = RGBDeviceType.LedController;
        DeviceName = light.Metadata.Name;
        Manufacturer = "Philips";
        Model = light.Type;

    }

    public string LightId { get; }

    public RGBDeviceType DeviceType { get; }
    public string DeviceName { get; }
    public string Manufacturer { get; }
    public string Model { get; }
    public object LayoutMetadata { get; set; }
}