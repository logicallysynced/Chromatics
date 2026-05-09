namespace Chromatics.Models
{
    // Persisted (settings.json) record of a user-adopted LIFX bulb. Identity
    // is the MAC; LastIp / Label / ProductId / ZoneCount are hints we re-
    // verify on each provider start (DHCP IP changes are common, users may
    // rename bulbs in the LIFX app).
    public class LifxAdoptedDevice
    {
        public string Mac { get; set; }
        public string Label { get; set; }
        public string LastIp { get; set; }
        public uint ProductId { get; set; }
        public ushort ZoneCount { get; set; }
    }
}
