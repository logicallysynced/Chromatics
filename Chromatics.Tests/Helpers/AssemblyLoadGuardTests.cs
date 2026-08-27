using System;
using System.IO;
using System.Reflection;
using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class AssemblyLoadGuardTests
{
    private static Exception AppControlBlock()
        => new FileLoadException("An Application Control policy has blocked this file. (0x800711C7)");

    [Fact]
    public void DirectLoadFailure_IsReported()
    {
        Assert.True(AssemblyLoadGuard.TryReportLoadFailure("Razer", AppControlBlock()));
        Assert.True(AssemblyLoadGuard.TryReportLoadFailure("Razer", new FileNotFoundException("missing")));
        Assert.True(AssemblyLoadGuard.TryReportLoadFailure("Razer", new BadImageFormatException("bad")));
        Assert.True(AssemblyLoadGuard.TryReportLoadFailure("Razer", new TypeLoadException("type")));
    }

    [Fact]
    public void LoadFailureWrappedByAnEventHandler_IsReported()
    {
        // How it arrives from a routed-event handler (CHROMATICS-1N).
        var ex = new TargetInvocationException(AppControlBlock());

        Assert.True(AssemblyLoadGuard.TryReportLoadFailure("Logitech", ex));
    }

    [Fact]
    public void LoadFailureInsideAggregate_IsReported()
    {
        // How it arrives from an unobserved task (CHROMATICS-1G / 1H).
        var ex = new AggregateException(new InvalidOperationException("unrelated"), AppControlBlock());

        Assert.True(AssemblyLoadGuard.TryReportLoadFailure("OpenRGB", ex));
    }

    [Fact]
    public void TypeInitializerCarryingALoadFailure_IsReported()
    {
        var ex = new TypeInitializationException("SomeType", AppControlBlock());

        Assert.True(AssemblyLoadGuard.TryReportLoadFailure("Corsair", ex));
    }

    [Fact]
    public void UnrelatedFailures_AreNotReportedAsLoadFailures()
    {
        Assert.False(AssemblyLoadGuard.TryReportLoadFailure("Hue", new InvalidOperationException()));
        Assert.False(AssemblyLoadGuard.TryReportLoadFailure("Hue", new TimeoutException()));
        Assert.False(AssemblyLoadGuard.TryReportLoadFailure("Hue",
            new InvalidOperationException("outer", new ArgumentOutOfRangeException())));
    }

    [Fact]
    public void TryRun_ReturnsFalseWhenTheActionFaults()
    {
        Assert.True(AssemblyLoadGuard.TryRun("Razer", () => { }));
        Assert.False(AssemblyLoadGuard.TryRun("Razer", () => throw AppControlBlock()));
        Assert.False(AssemblyLoadGuard.TryRun("Razer", () => throw new InvalidOperationException()));
    }
}
