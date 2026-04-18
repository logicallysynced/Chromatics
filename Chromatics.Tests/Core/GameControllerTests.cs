using Chromatics.Core;

namespace Chromatics.Tests.Core;

public class GameControllerTests
{
    [Fact]
    public void Exit_WithoutSetup_DoesNotThrow()
    {
        // Regression: `Exit()` -> `StopGameLoop()` used to NRE at the
        // `_configuration.ProcessModel.Process?.Dispose()` line whenever the app
        // was closed before the FFXIV connection had populated `_configuration`.
        // Shutdown must be safe in every lifecycle state.
        var ex = Record.Exception(() => GameController.Exit());
        Assert.Null(ex);
    }

    [Fact]
    public void IsGameConnected_BeforeSetup_ReturnsFalse()
    {
        Assert.False(GameController.IsGameConnected());
    }

    [Fact]
    public void GetGameData_BeforeSetup_ReturnsNull()
    {
        Assert.Null(GameController.GetGameData());
    }

    [Fact]
    public void GetGameProcess_BeforeSetup_ReturnsNull()
    {
        Assert.Null(GameController.GetGameProcess());
    }
}
