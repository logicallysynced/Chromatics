using System;

namespace Chromatics.Models
{
    // Persisted (settings.json) record of a user-adopted Hue bulb. LightId is
    // the canonical identity (CLIP v2 Guid for the light resource). Label /
    // ProductId / ModelId are hints we re-verify on each provider start since
    // the user may rename bulbs in the Hue app between runs.
    //
    // Auto-adoption migration: on the first launch after upgrading to schema
    // v5+, if the user has existing Hue-bound layers in layers.chromatics4
    // but no entries in deviceHueAdoptedDevices, we populate this list with
    // every bulb the bridge currently exposes. Existing mappings keep working;
    // the user can then deselect bulbs from the new adoption dialog.
    public class HueAdoptedDevice
    {
        public Guid LightId { get; set; }
        public string Label { get; set; }
        public string ModelId { get; set; }
    }
}
