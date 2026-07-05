using System.Net;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    // Per-controller configuration hydrated from settings before
    // LoadDeviceProvider. Id is the canonical identity; Endpoint is the
    // last-known REST endpoint (host + control port, default 16021).
    // AuthToken authorises the REST calls.
    public class NanoleafClientDefinition
    {
        public NanoleafClientDefinition(string id, string label, IPEndPoint endpoint, string authToken, string model, string firmware, int panelCount)
        {
            Id = id;
            Label = label;
            Endpoint = endpoint;
            AuthToken = authToken;
            Model = model;
            Firmware = firmware;
            PanelCount = panelCount;
        }

        public string Id { get; }
        public string Label { get; set; }
        public IPEndPoint Endpoint { get; set; }
        public string AuthToken { get; set; }
        public string Model { get; set; }
        public string Firmware { get; set; }
        public int PanelCount { get; set; }

        // Persisted slot table (see NanoleafAdoptedDevice.PanelOrder).
        // Hydrated from settings; the provider re-resolves it against the
        // live layout on every load and persists any appended panels.
        public System.Collections.Generic.List<int> PanelOrder { get; set; } = new();
    }
}
