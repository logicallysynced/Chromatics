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
}
