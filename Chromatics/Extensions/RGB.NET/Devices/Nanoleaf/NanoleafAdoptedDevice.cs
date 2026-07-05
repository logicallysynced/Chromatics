namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    // Persisted (settings.chromatics4) record of a user-paired Nanoleaf
    // controller. Identity is the controller Id reported by the OpenAPI
    // (stable across IP changes); LastIp / Label / Model / PanelCount are
    // hints re-verified on each provider start. AuthToken is the per-
    // controller credential returned by pairing - a rejected token means
    // the controller was factory reset and needs re-pairing.
    public class NanoleafAdoptedDevice
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string LastIp { get; set; }
        public int Port { get; set; } = 16021;
        public string AuthToken { get; set; }
        public string Model { get; set; }
        public string Firmware { get; set; }
        public int PanelCount { get; set; }

        // Slot table: PanelOrder[n] is the panelId assigned to LedId.Custom(n+1).
        // Grows append-only; a removed panel keeps its slot as a tombstone so
        // the panels after it never shift LedIds (layer assignments reference
        // LedIds) and a re-attached panel lands back on its old slot. Empty on
        // settings saved by older builds - the provider initialises it from
        // the live layout on first load.
        public System.Collections.Generic.List<int> PanelOrder { get; set; } = new();
    }
}
