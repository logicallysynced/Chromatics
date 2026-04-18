using Chromatics.Core;

namespace Chromatics.Tests.Core;

public class KeyControllerTests
{
    [Fact]
    public void Stop_WithoutPriorSetup_DoesNotThrow()
    {
        // Regression: Stop() used to NRE when the hook had never been initialized.
        // Calling before Setup must be a safe no-op so shutdown paths can run
        // unconditionally during application exit.
        var ex = Record.Exception(() => KeyController.Stop());
        Assert.Null(ex);
    }

    [Fact]
    public void Stop_CalledTwice_DoesNotThrow()
    {
        KeyController.Stop();
        var ex = Record.Exception(() => KeyController.Stop());
        Assert.Null(ex);
    }

    [Fact]
    public void GetKeyController_BeforeSetup_ReturnsNull()
    {
        KeyController.Stop();
        Assert.Null(KeyController.GetKeyController());
    }

    [Fact]
    public void ModifierFlags_DefaultToFalse()
    {
        Assert.False(KeyController.IsCtrlPressed());
        Assert.False(KeyController.IsShiftPressed());
        Assert.False(KeyController.IsAltPressed());
    }
}
