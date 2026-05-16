using System.Collections.Generic;
using System.Net;

namespace Chromatics.Extensions.RGB.NET.Devices.Yeelight
{
    // Runtime descriptor for one adopted Yeelight bulb. Captures the
    // identifying metadata from SSDP discovery plus the resolved control
    // endpoint, so the provider and update queue don't have to re-discover
    // on every Load.
    public sealed class YeelightClientDefinition
    {
        public YeelightClientDefinition(string id, string label, IPEndPoint endpoint, string model, string firmwareVersion = null, IReadOnlyList<string> support = null)
        {
            Id = id;
            Label = label;
            Endpoint = endpoint;
            Model = model;
            FirmwareVersion = firmwareVersion;
            Support = support ?? System.Array.Empty<string>();
        }

        // Stable hex id from the bulb (e.g. "0x000000000d2a4b1c"). Survives
        // IP changes and is the only identity that can be persisted across
        // restarts.
        public string Id { get; }

        // User-visible label. SSDP `name` field if the user set one,
        // otherwise "Yeelight {model}" or the bulb id.
        public string Label { get; }

        // Last-known control endpoint (ip:55443). Updated by discovery
        // sweeps so the provider can stomach DHCP renewals.
        public IPEndPoint Endpoint { get; set; }

        // SSDP `model` field — "color", "stripe", "color1", "ceiling4", etc.
        // Used for the Mapping-tab label and to gate Music Mode (some older
        // firmware models don't support it).
        public string Model { get; }

        public string FirmwareVersion { get; }

        // Space-separated capability list from SSDP `support` (e.g.
        // "set_default set_rgb set_bright set_power set_music ..."). Used
        // to gate optional protocol calls like Music Mode and color
        // temperature.
        public IReadOnlyList<string> Support { get; }

        public bool SupportsMusicMode => HasSupport("set_music");

        // True when the bulb exposes a secondary "background" light element
        // (Bedside Lamp 2 main + bg, some ceiling lights). When present we
        // expose two LEDs and route bg_set_* commands to the second one.
        public bool HasBackgroundLight => HasSupport("bg_set_rgb");

        // True when the bulb supports tunable-white via colour temperature.
        // Used so monochrome / tunable-white bulbs (mono*, ct_bulb) get a
        // sensible representation rather than just refusing colour writes.
        public bool SupportsColorTemperature => HasSupport("set_ct_abx");

        private bool HasSupport(string method)
        {
            foreach (var s in Support)
                if (string.Equals(s, method, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
