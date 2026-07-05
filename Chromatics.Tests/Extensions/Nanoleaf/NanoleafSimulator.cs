using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Chromatics.Tests.Extensions.Nanoleaf;

// A fake Nanoleaf controller for tests: an HTTP listener implementing the
// OpenAPI endpoints the provider uses, plus a UDP listener capturing
// streamed extControl frames for byte-level assertions. Binds ephemeral
// localhost ports; dispose stops both listeners.
public sealed class NanoleafSimulator : IDisposable
{
    private readonly HttpListener _http = new();
    private readonly UdpClient _udp;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _httpLoop;
    private readonly Task _udpLoop;

    public int HttpPort { get; }
    public int StreamPort { get; }
    public string Host => "127.0.0.1";
    public string Token { get; }

    // Mutable device state the tests drive and assert against.
    public bool PairingMode { get; set; }
    public bool On { get; set; } = true;
    public int Brightness { get; set; } = 60;
    public string SelectedEffect { get; set; } = "Northern Lights";
    public bool Streaming { get; private set; }
    public string Model { get; set; } = "NL42";     // Shapes
    public string Firmware { get; set; } = "7.1.2";
    public string SerialNo { get; set; } = "SIMSERIAL01";
    public List<(int panelId, int x, int y, int shape)> Panels { get; } = new();

    // Fault injection: number of next REST writes to fail before succeeding.
    public int FailNextWrites { get; set; }

    // Captured UDP frames (raw bytes), newest last.
    public ConcurrentQueue<byte[]> Frames { get; } = new();

    public NanoleafSimulator(string token = "SIMTOKEN")
    {
        Token = token;

        HttpPort = FreeTcpPort();
        _http.Prefixes.Add($"http://127.0.0.1:{HttpPort}/");
        _http.Start();

        _udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        StreamPort = ((IPEndPoint)_udp.Client.LocalEndPoint!).Port;

        _httpLoop = Task.Run(HttpLoopAsync);
        _udpLoop = Task.Run(UdpLoopAsync);
    }

    public void SeedHexWall(int count)
    {
        Panels.Clear();
        // Rough hex packing: alternate rows, deterministic ids.
        for (int i = 0; i < count; i++)
        {
            int row = i / 4;
            int col = i % 4;
            Panels.Add((100 + i, col * 100 + (row % 2) * 50, row * 86, 7 /*hexagon*/));
        }
    }

    private async Task HttpLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _http.GetContextAsync(); }
            catch { break; }

            try { HandleRequest(ctx); }
            catch { /* keep the listener alive */ }
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var path = req.Url!.AbsolutePath;
        string body = "";
        using (var r = new StreamReader(req.InputStream, Encoding.UTF8)) body = r.ReadToEnd();

        // Pairing: POST /api/v1/new
        if (path.EndsWith("/api/v1/new") && req.HttpMethod == "POST")
        {
            if (PairingMode)
                WriteJson(ctx, 200, new JObject { ["auth_token"] = Token });
            else
                ctx.Response.StatusCode = 403;
            ctx.Response.Close();
            return;
        }

        // Everything else is token-scoped: /api/v1/{token}/...
        string tokenPrefix = $"/api/v1/{Token}";
        if (!path.StartsWith(tokenPrefix))
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.Close();
            return;
        }
        string rest = path.Substring(tokenPrefix.Length);

        if (FailNextWrites > 0 && req.HttpMethod == "PUT")
        {
            FailNextWrites--;
            ctx.Response.StatusCode = 503;
            ctx.Response.Close();
            return;
        }

        // GET all-state
        if (rest is "" or "/" && req.HttpMethod == "GET")
        {
            WriteJson(ctx, 200, BuildStateJson());
            ctx.Response.Close();
            return;
        }

        // GET /effects/select
        if (rest == "/effects/select" && req.HttpMethod == "GET")
        {
            WriteRaw(ctx, 200, "\"" + (Streaming ? "*ExtControl*" : SelectedEffect) + "\"");
            ctx.Response.Close();
            return;
        }

        // PUT /effects  (streaming enter OR scene select)
        if (rest == "/effects" && req.HttpMethod == "PUT")
        {
            var o = JObject.Parse(body);
            if (o["write"]?["animType"]?.ToString() == "extControl")
            {
                Streaming = true;
                WriteJson(ctx, 200, new JObject
                {
                    ["streamControlIpAddr"] = Host,
                    ["streamControlPort"] = StreamPort,
                });
            }
            else if (o["select"] != null)
            {
                SelectedEffect = o["select"]!.ToString();
                Streaming = false;
                ctx.Response.StatusCode = 204;
            }
            else ctx.Response.StatusCode = 204;
            ctx.Response.Close();
            return;
        }

        // PUT /state (on / brightness)
        if (rest == "/state" && req.HttpMethod == "PUT")
        {
            var o = JObject.Parse(body);
            if (o["on"]?["value"] != null) On = o["on"]!["value"]!.Value<bool>();
            if (o["brightness"]?["value"] != null) Brightness = o["brightness"]!["value"]!.Value<int>();
            ctx.Response.StatusCode = 204;
            ctx.Response.Close();
            return;
        }

        ctx.Response.StatusCode = 404;
        ctx.Response.Close();
    }

    private JObject BuildStateJson()
    {
        var positions = new JArray();
        foreach (var (panelId, x, y, shape) in Panels)
        {
            positions.Add(new JObject
            {
                ["panelId"] = panelId, ["x"] = x, ["y"] = y, ["o"] = 0, ["shapeType"] = shape,
            });
        }

        return new JObject
        {
            ["name"] = "Simulator",
            ["model"] = Model,
            ["firmwareVersion"] = Firmware,
            ["serialNo"] = SerialNo,
            ["state"] = new JObject
            {
                ["on"] = new JObject { ["value"] = On },
                ["brightness"] = new JObject { ["value"] = Brightness },
            },
            ["effects"] = new JObject { ["select"] = Streaming ? "*ExtControl*" : SelectedEffect },
            ["panelLayout"] = new JObject
            {
                ["globalOrientation"] = new JObject { ["value"] = 0 },
                ["layout"] = new JObject { ["positionData"] = positions },
            },
        };
    }

    private async Task UdpLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            UdpReceiveResult res;
            try { res = await _udp.ReceiveAsync(_cts.Token); }
            catch { break; }
            Frames.Enqueue(res.Buffer);
        }
    }

    private static void WriteJson(HttpListenerContext ctx, int status, JObject body)
        => WriteRaw(ctx, status, body.ToString());

    private static void WriteRaw(HttpListenerContext ctx, int status, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    private static int FreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _http.Stop(); } catch { }
        try { _udp.Dispose(); } catch { }
        try { Task.WaitAll(new[] { _httpLoop, _udpLoop }, 500); } catch { }
        _cts.Dispose();
    }
}
