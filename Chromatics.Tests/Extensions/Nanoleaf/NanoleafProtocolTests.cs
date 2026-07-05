using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol;
using System.Buffers.Binary;

namespace Chromatics.Tests.Extensions.Nanoleaf;

public class NanoleafProtocolTests
{
    // ── Streaming frame encoding (extControl v2) ────────────────────────

    [Fact]
    public void EncodeFrame_ProducesV2Layout()
    {
        var panels = new List<(int, byte, byte, byte)>
        {
            (100, 10, 20, 30),
            (101, 40, 50, 60),
        };

        var frame = NanoleafStreamProtocol.EncodeFrame(panels);

        // 2-byte count + 8 bytes per panel.
        Assert.Equal(2 + 2 * 8, frame.Length);
        Assert.Equal(2, BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(0, 2)));

        // First panel: id(2) r g b w(=0) transition(2).
        Assert.Equal(100, BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(2, 2)));
        Assert.Equal(10, frame[4]);
        Assert.Equal(20, frame[5]);
        Assert.Equal(30, frame[6]);
        Assert.Equal(0, frame[7]); // white channel always 0
        Assert.Equal(NanoleafStreamProtocol.DefaultTransition, BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(8, 2)));

        // Second panel id at the right offset.
        Assert.Equal(101, BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(10, 2)));
    }

    [Fact]
    public void EncodeFrame_EmptyPanelList_IsJustTheCount()
    {
        var frame = NanoleafStreamProtocol.EncodeFrame(new List<(int, byte, byte, byte)>());
        Assert.Equal(2, frame.Length);
        Assert.Equal(0, BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(0, 2)));
    }

    // ── State parsing (layout normalisation) ────────────────────────────

    [Fact]
    public void ParseState_ReadsPowerBrightnessSceneAndPanels()
    {
        const string json = """
        {
          "name": "Living Room",
          "model": "NL42",
          "firmwareVersion": "7.1.2",
          "serialNo": "ABC123",
          "state": { "on": { "value": true }, "brightness": { "value": 73 } },
          "effects": { "select": "Northern Lights" },
          "panelLayout": {
            "globalOrientation": { "value": 0 },
            "layout": { "positionData": [
              { "panelId": 105, "x": 100, "y": 50, "o": 0, "shapeType": 7 },
              { "panelId": 101, "x": 0, "y": 0, "o": 0, "shapeType": 7 }
            ] }
          }
        }
        """;

        var state = NanoleafRestClient.ParseState(json);

        Assert.True(state.On);
        Assert.Equal(73, state.Brightness);
        Assert.Equal("Northern Lights", state.SelectedEffect);
        Assert.Equal("NL42", state.Model);
        Assert.Equal(2, state.Panels.Count);
        // Panels are read in document order; the provider sorts by panelId.
        Assert.Contains(state.Panels, p => p.PanelId == 105 && p.X == 100 && p.Y == 50);
        Assert.Contains(state.Panels, p => p.PanelId == 101 && p.X == 0 && p.Y == 0);
    }

    [Fact]
    public void ParseState_MissingFields_DefaultsGracefully()
    {
        var state = NanoleafRestClient.ParseState("{ \"name\": \"Bare\" }");
        Assert.False(state.On);
        Assert.Equal(0, state.Brightness);
        Assert.Equal("", state.SelectedEffect);
        Assert.Empty(state.Panels);
    }

    // ── mDNS query bytes ────────────────────────────────────────────────

    [Fact]
    public void BuildPtrQuery_EncodesServiceNameAsDnsLabels()
    {
        var q = NanoleafDiscovery.BuildPtrQuery("_nanoleafapi._tcp.local");

        // 12-byte header, qdcount == 1.
        Assert.Equal(1, BinaryPrimitives.ReadUInt16BigEndian(q.AsSpan(4, 2)));
        // First label length prefix is the length of "_nanoleafapi" (12).
        Assert.Equal(12, q[12]);
        // Trailing QTYPE PTR (12) and QCLASS IN (1).
        Assert.Equal(12, q[^3]);
        Assert.Equal(1, q[^1]);
    }
}
