using System.Net;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf
{
    // Per-controller configuration hydrated from settings before
    // LoadDeviceProvider. Id is the canonical identity; Endpoint is the
    // last-known REST endpoint (host + control port, default 16021), re-
    // resolved via mDNS when it stops responding. AuthToken authorises the
    // REST calls; StreamPort is the UDP port the controller returns when we
    // enter external-control streaming mode.
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

        // Populated after the streaming-mode handshake. Null until the queue
        // enters extControl; the UDP frames target this endpoint.
        public IPEndPoint StreamEndpoint { get; set; }
    }
}
