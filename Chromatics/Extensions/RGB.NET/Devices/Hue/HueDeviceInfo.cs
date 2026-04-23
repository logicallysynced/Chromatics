using System;
using System.Collections.Generic;
using System.Linq;
using HueApi.Entertainment.Models;
using HueApi.Models;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue;

public class HueDeviceInfo : IRGBDeviceInfo
{
    public HueDeviceInfo(Light light, string modelId)
    {
        // IdV1 is null on CLIP v2 resources without a v1 counterpart.
        LightId = light.IdV1 ?? light.Id.ToString();

        DeviceType = RGBDeviceType.LedController;
        // Metadata is nullable in HueApi 3.x; fall back to the resource id.
        DeviceName = light.Metadata?.Name ?? light.Id.ToString();
        Manufacturer = "Philips";
        // Hardware model id (e.g. "LCA001") joined from the Device endpoint by
        // the provider. Falls back to light.Type ("light") if the device fetch
        // failed — HueDevice's switch will then hit its default branch but the
        // light still works.
        Model = !string.IsNullOrEmpty(modelId) ? modelId : light.Type;
    }

    public string LightId { get; }

    public RGBDeviceType DeviceType { get; }
    public string DeviceName { get; }
    public string Manufacturer { get; }
    public string Model { get; }
    public object LayoutMetadata { get; set; }
}