using RGB.NET.Core;
using System.Collections.Generic;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight
{
    // Maps SSDP `model` strings reported by Yeelight bulbs to a friendly
    // device-type classification for the Mapping tab. Lets a Lightstrip Plus
    // show up as `LedStripe`, a Bedside Lamp as `Lamp`, a regular bulb as
    // `LightBulb`, etc., rather than every device falling into a single
    // generic bucket.
    //
    // Source for model strings: https://www.yeelight.com/en_US/developer
    // (the Inter-Operation Spec) plus the LAN-spec community wiki. New
    // models periodically ship with new strings — when an unknown one is
    // encountered we fall back to LightBulb, which is the most common form.
    //
    // **Single-LED constraint.** The Yeelight LAN protocol's set_rgb /
    // bg_set_rgb commands paint an entire device one colour at a time —
    // there's no per-zone API for light strips, gradient bulbs, or matrix
    // devices. Models that have a "background light" element (Bedside Lamp
    // 2, some ceiling lights) are the only exception and get two LEDs via
    // HasBackgroundLight. All other multi-zone devices expose one LED.
    public static class YeelightModelCatalog
    {
        public sealed class ProductInfo
        {
            public string Model { get; }
            public string DisplayName { get; }
            public RGBDeviceType DeviceType { get; }

            public ProductInfo(string model, string displayName, RGBDeviceType deviceType)
            {
                Model = model;
                DisplayName = displayName;
                DeviceType = deviceType;
            }
        }

        // Entries keyed by lowercased model string. The Yeelight firmware
        // is consistent on case but we lowercase the input before lookup
        // to be defensive.
        private static readonly Dictionary<string, ProductInfo> _byModel = new()
        {
            // RGB bulbs
            ["color"]    = new("color",    "Color Bulb",           RGBDeviceType.LedController),
            ["color1"]   = new("color1",   "Color Bulb 1",         RGBDeviceType.LedController),
            ["color2"]   = new("color2",   "Color Bulb 2",         RGBDeviceType.LedController),
            ["color3"]   = new("color3",   "Color Bulb 3",         RGBDeviceType.LedController),
            ["color4"]   = new("color4",   "Color Bulb 4",         RGBDeviceType.LedController),
            ["color5"]   = new("color5",   "Color Bulb 5",         RGBDeviceType.LedController),
            ["color6"]   = new("color6",   "Smart LED Bulb 1S",    RGBDeviceType.LedController),
            ["colorc"]   = new("colorc",   "Color Bulb Pro",       RGBDeviceType.LedController),
            ["colora"]   = new("colora",   "Smart LED Bulb W3",    RGBDeviceType.LedController),
            ["colorb"]   = new("colorb",   "Smart LED Bulb",       RGBDeviceType.LedController),

            // White-only / colour-temperature bulbs
            ["mono"]     = new("mono",     "White Bulb",           RGBDeviceType.LedController),
            ["mono1"]    = new("mono1",    "White Bulb 1",         RGBDeviceType.LedController),
            ["mono4"]    = new("mono4",    "White Bulb 4",         RGBDeviceType.LedController),
            ["mono5"]    = new("mono5",    "White Bulb 5",         RGBDeviceType.LedController),
            ["mono6"]    = new("mono6",    "White Bulb 6",         RGBDeviceType.LedController),
            ["ct_bulb"]  = new("ct_bulb",  "Tunable White Bulb",   RGBDeviceType.LedController),
            ["ct2"]      = new("ct2",      "Tunable White Bulb 2", RGBDeviceType.LedController),

            // Light strips
            ["stripe"]   = new("stripe",   "Light Strip",          RGBDeviceType.LedStripe),
            ["strip1"]   = new("strip1",   "Light Strip 1",        RGBDeviceType.LedStripe),
            ["strip2"]   = new("strip2",   "Light Strip 2",        RGBDeviceType.LedStripe),
            ["strip4"]   = new("strip4",   "Light Strip 4 (Pro)",  RGBDeviceType.LedStripe),
            ["strip6"]   = new("strip6",   "Light Strip 6",        RGBDeviceType.LedStripe),
            ["strip8"]   = new("strip8",   "Light Strip 8",        RGBDeviceType.LedStripe),

            // Bedside / ambient lamps
            ["bslamp"]   = new("bslamp",   "Bedside Lamp",         RGBDeviceType.LedController),
            ["bslamp1"]  = new("bslamp1",  "Bedside Lamp 1",       RGBDeviceType.LedController),
            ["bslamp2"]  = new("bslamp2",  "Bedside Lamp 2",       RGBDeviceType.LedController),
            ["bslamp3"]  = new("bslamp3",  "Bedside Lamp 3",       RGBDeviceType.LedController),
            ["lamp"]     = new("lamp",     "Lamp",                 RGBDeviceType.LedController),
            ["lamp1"]    = new("lamp1",    "Lamp 1",               RGBDeviceType.LedController),
            ["lamp15"]   = new("lamp15",   "Smart Lamp",           RGBDeviceType.LedController),

            // Ceiling lights — single-colour from the LAN protocol's perspective
            ["ceiling"]   = new("ceiling",   "Ceiling Light",      RGBDeviceType.LedController),
            ["ceiling1"]  = new("ceiling1",  "Ceiling Light 1",    RGBDeviceType.LedController),
            ["ceiling2"]  = new("ceiling2",  "Ceiling Light 2",    RGBDeviceType.LedController),
            ["ceiling3"]  = new("ceiling3",  "Ceiling Light 3",    RGBDeviceType.LedController),
            ["ceiling4"]  = new("ceiling4",  "Ceiling Light 4",    RGBDeviceType.LedController),
            ["ceiling10"] = new("ceiling10", "Ceiling Light 10",   RGBDeviceType.LedController),
            ["ceiling11"] = new("ceiling11", "Ceiling Light 11",   RGBDeviceType.LedController),
            ["ceiling13"] = new("ceiling13", "Ceiling Light 13",   RGBDeviceType.LedController),
            ["ceiling15"] = new("ceiling15", "Ceiling Light 15",   RGBDeviceType.LedController),
            ["ceiling18"] = new("ceiling18", "Ceiling Light 18",   RGBDeviceType.LedController),
            ["ceiling19"] = new("ceiling19", "Ceiling Light 19",   RGBDeviceType.LedController),
            ["ceiling20"] = new("ceiling20", "Ceiling Light 20",   RGBDeviceType.LedController),
            ["ceiling22"] = new("ceiling22", "Ceiling Light 22",   RGBDeviceType.LedController),

            // Monitor / desk bars
            ["mlight"]   = new("mlight",   "Monitor Light Bar",    RGBDeviceType.LedStripe),
            ["mlight2"]  = new("mlight2",  "Monitor Light Bar Pro", RGBDeviceType.LedStripe),

            // Cube / matrix
            ["cube"]     = new("cube",     "Cube Light",           RGBDeviceType.LedMatrix),
            ["cube1"]    = new("cube1",    "Cube Light 1",         RGBDeviceType.LedMatrix),
        };

        // Look up a model. Unknown models fall back to a generic LightBulb
        // entry — Yeelight ships new bulbs faster than we can catalog them
        // and the LAN protocol behaviour is consistent across the line.
        public static ProductInfo GetOrDefault(string model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return new ProductInfo("", "Yeelight bulb", RGBDeviceType.LedController);

            string key = model.Trim().ToLowerInvariant();
            if (_byModel.TryGetValue(key, out var info)) return info;
            return new ProductInfo(model, $"Yeelight {model}", RGBDeviceType.LedController);
        }
    }
}
