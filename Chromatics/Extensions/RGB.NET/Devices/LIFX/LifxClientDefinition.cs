using System.Net;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX
{
    // Per-bulb configuration captured at adoption time. Persisted via
    // SettingsModel.deviceLifxAdoptedDevices so we can reconnect on the next
    // launch without forcing the user to re-discover.
    //
    // Mac is the canonical identity (constant across IP changes — DHCP renewals
    // are common on consumer networks). LastIp is a hint we try first; if it
    // doesn't respond we fall back to a fresh discovery sweep on startup.
    public class LifxClientDefinition
    {
        public LifxClientDefinition(string mac, string label, IPEndPoint endpoint, uint productId, ushort zoneCount)
        {
            Mac = mac;
            Label = label;
            Endpoint = endpoint;
            ProductId = productId;
            ZoneCount = zoneCount;
        }

        public string Mac { get; }
        public string Label { get; set; }
        public IPEndPoint Endpoint { get; set; }
        public uint ProductId { get; }
        public ushort ZoneCount { get; set; }
    }
}
