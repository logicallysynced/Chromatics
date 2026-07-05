using System.Collections.Generic;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol
{
    // Plain data shapes for the OpenAPI JSON we parse. Kept deliberately
    // minimal - only the fields the provider reads. Newtonsoft populates
    // these directly; unknown members are ignored, so firmware additions
    // don't break parsing.

    public sealed class NanoleafPanelPosition
    {
        public int PanelId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int O { get; set; } // orientation degrees
        public int ShapeType { get; set; }
    }

    // The subset of GET / (all-state) we use plus a normalised panel list.
    public sealed class NanoleafState
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public string FirmwareVersion { get; set; }
        public string SerialNo { get; set; }
        public bool On { get; set; }
        public int Brightness { get; set; }        // 0-100
        public string SelectedEffect { get; set; } // active scene name, "" if none
        public int GlobalOrientation { get; set; }
        public List<NanoleafPanelPosition> Panels { get; set; } = new();
    }

    // Restorable snapshot captured before entering streaming mode. Scene
    // name is the restore key; power + brightness cover the "solid colour,
    // no scene" case.
    public sealed class NanoleafOriginalState
    {
        public bool On { get; set; }
        public int Brightness { get; set; }
        public string SelectedEffect { get; set; }
    }

    // Result of the streaming-mode handshake: the UDP host + port the
    // controller listens on for extControl v2 frames.
    public sealed class NanoleafStreamInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int ProtocolVersion { get; set; }
    }
}
