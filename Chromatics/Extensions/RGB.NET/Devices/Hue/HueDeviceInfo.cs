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
        // IdV1 is null on CLIP v2 resources without a v1 counterpart.
        LightId = light.IdV1 ?? light.Id.ToString();

        DeviceType = RGBDeviceType.LedController;
        // Metadata is nullable in HueApi 3.x; fall back to the resource id.
        DeviceName = light.Metadata?.Name ?? light.Id.ToString();
        Manufacturer = "Philips";
        // CLIP v2 splits Device from Light; the hardware model id lives on
        // Device.ProductData.ModelId (fetched from a separate endpoint), not
        // on Light. light.Type ("light") is the only thing available here, so
        // HueDevice's per-model switch will always hit its default branch.
        // Resolving the real model id would require fetching the device list
        // and joining via service references — out of scope for now.
        Model = light.Type;
    }

    public string LightId { get; }

    public RGBDeviceType DeviceType { get; }
    public string DeviceName { get; }
    public string Manufacturer { get; }
    public string Model { get; }
    public object LayoutMetadata { get; set; }
}