using Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol;

namespace Chromatics.Tests.Extensions.Nanoleaf;

// REST control-plane behaviour against the simulator: pairing window,
// streaming enter, scene restore, and the retry path under injected faults.
public class NanoleafRestClientTests
{
    [Fact]
    public async Task Pair_InPairingMode_ReturnsToken()
    {
        using var sim = new NanoleafSimulator();
        sim.PairingMode = true;

        var token = await NanoleafRestClient.PairAsync(sim.Host, sim.HttpPort);

        Assert.Equal(sim.Token, token);
    }

    [Fact]
    public async Task Pair_WhenWindowClosed_ReturnsNull()
    {
        using var sim = new NanoleafSimulator();
        sim.PairingMode = false;

        var token = await NanoleafRestClient.PairAsync(sim.Host, sim.HttpPort);

        Assert.Null(token);
    }

    [Fact]
    public async Task GetState_ReadsSimulatorState()
    {
        using var sim = new NanoleafSimulator();
        sim.SeedHexWall(6);
        sim.Brightness = 42;
        sim.SelectedEffect = "Forest";

        var rest = new NanoleafRestClient(sim.Host, sim.HttpPort, sim.Token);
        var state = await rest.GetStateAsync();

        Assert.NotNull(state);
        Assert.Equal(42, state!.Brightness);
        Assert.Equal("Forest", state.SelectedEffect);
        Assert.Equal(6, state.Panels.Count);
    }

    [Fact]
    public async Task EnableStreaming_ReturnsStreamEndpoint_AndFlipsMode()
    {
        using var sim = new NanoleafSimulator();

        var rest = new NanoleafRestClient(sim.Host, sim.HttpPort, sim.Token);
        var info = await rest.EnableStreamingAsync();

        Assert.NotNull(info);
        Assert.Equal(sim.StreamPort, info!.Port);
        Assert.True(await rest.IsStreamingAsync());
    }

    [Fact]
    public async Task SelectEffect_RestoresSceneByName_AndExitsStreaming()
    {
        using var sim = new NanoleafSimulator();
        var rest = new NanoleafRestClient(sim.Host, sim.HttpPort, sim.Token);

        await rest.EnableStreamingAsync();
        Assert.True(sim.Streaming);

        var ok = await rest.SelectEffectAsync("Northern Lights");

        Assert.True(ok);
        Assert.False(sim.Streaming);
        Assert.Equal("Northern Lights", sim.SelectedEffect);
    }

    [Fact]
    public async Task SetOn_RetriesThroughTransientFailures()
    {
        using var sim = new NanoleafSimulator { On = true };
        sim.FailNextWrites = 2; // first two PUTs 503, third succeeds

        var rest = new NanoleafRestClient(sim.Host, sim.HttpPort, sim.Token);
        var ok = await rest.SetOnAsync(false);

        Assert.True(ok);
        Assert.False(sim.On);
    }

    [Fact]
    public async Task SetOn_GivesUpAfterThreeFailures()
    {
        using var sim = new NanoleafSimulator { On = true };
        sim.FailNextWrites = 5; // exceeds the 3-attempt budget

        var rest = new NanoleafRestClient(sim.Host, sim.HttpPort, sim.Token);
        var ok = await rest.SetOnAsync(false);

        Assert.False(ok);
        Assert.True(sim.On); // never applied
    }

    // An offline controller is an expected state (dead pairing rows,
    // startup capture retries, watchdog mid-reboot), so every call must
    // fail soft without a single exception reaching the debugger - the TCP
    // pre-flight gate keeps HttpClient (which throws HttpRequestException
    // for unreachable hosts) out of the picture entirely.
    [Fact]
    public async Task RestClient_UnreachableController_FailsSoftWithoutThrowing()
    {
        int throwsInClient = 0;
        EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> recorder = (_, e) =>
        {
            if (Environment.StackTrace.Contains("Protocol.NanoleafRestClient"))
                Interlocked.Increment(ref throwsInClient);
        };

        int closedPort = GetClosedTcpPort();
        AppDomain.CurrentDomain.FirstChanceException += recorder;
        try
        {
            var rest = new NanoleafRestClient("127.0.0.1", closedPort, "TOKEN");

            Assert.Null(await NanoleafRestClient.PairAsync("127.0.0.1", closedPort));
            Assert.Null(await rest.GetStateAsync());
            Assert.Null(await rest.EnableStreamingAsync());
            Assert.False(await rest.IsStreamingAsync());
            Assert.False(await rest.SetOnAsync(true));
            Assert.Null(await rest.GetOnAsync());
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= recorder;
        }

        Assert.Equal(0, throwsInClient);
    }

    private static int GetClosedTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
