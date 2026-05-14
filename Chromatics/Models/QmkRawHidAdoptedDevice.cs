namespace Chromatics.Models
{
    // Persisted (settings.json) record of a user-adopted QMK keyboard.
    //
    // Identity is the triple (VendorId, ProductId, Manufacturer + Product),
    // not the OS HID DevicePath — DevicePath changes when the user moves the
    // keyboard between USB ports, but VID/PID + the firmware-reported product
    // string are stable across plugs. We re-resolve a concrete HidDevice on
    // each provider start via QmkRawHidDiscovery, then match candidates to
    // adopted entries on this triple.
    //
    // Layout (the VIA keymap source URL) is captured at adoption time so a
    // subsequent run knows which keymap JSON to fetch + cache without
    // re-running detection. Empty string means "no semantic layout known —
    // present LEDs as Custom1..N and let the user position them via the
    // Mapping tab."
    public class QmkRawHidAdoptedDevice
    {
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public string Manufacturer { get; set; }
        public string Product { get; set; }
        public int LedCount { get; set; }
        public string Protocol { get; set; }    // "ViaOnly" | "OpenRgbQmk"
        public string ViaKeymapKey { get; set; } // via-keyboards repo key (e.g. "novelkeys/nk65"), empty if unmapped
    }
}
