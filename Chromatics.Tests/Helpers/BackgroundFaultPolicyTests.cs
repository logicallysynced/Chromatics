using System;
using System.IO;
using System.Net.Sockets;
using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class BackgroundFaultPolicyTests
{
    // The shape CHROMATICS-1W arrived in: Yeelight's update queue surfaces a
    // failed write through the provider, which wraps it twice before the
    // unobserved-task handler sees it. Only the innermost frame knows it was a
    // dropped connection.
    private static Exception WrappedDroppedConnection(SocketError code) =>
        new AggregateException(
            new InvalidOperationException("DeviceProviderException stand-in",
                new IOException("Unable to write data to the transport connection.",
                    new SocketException((int)code))));

    [Theory]
    [InlineData(SocketError.ConnectionAborted)]
    [InlineData(SocketError.ConnectionReset)]
    [InlineData(SocketError.ConnectionRefused)]
    [InlineData(SocketError.TimedOut)]
    [InlineData(SocketError.HostUnreachable)]
    public void ADroppedConnectionIsBenignHoweverDeeplyWrapped(SocketError code)
    {
        Assert.True(BackgroundFaultPolicy.IsBenign(WrappedDroppedConnection(code)));
    }

    [Fact]
    public void ABareSocketFailureIsStillBenign()
    {
        Assert.True(BackgroundFaultPolicy.IsBenign(new SocketException((int)SocketError.ConnectionReset)));
    }

    [Fact]
    public void ASocketFailureWeDoNotExcuseStaysFatal()
    {
        // Not a teardown or unreachable condition, so it should still reach the
        // crash handler rather than being quietly swallowed.
        Assert.False(BackgroundFaultPolicy.IsBenign(
            WrappedDroppedConnection(SocketError.AddressAlreadyInUse)));
    }

    [Fact]
    public void ApplicationBugsStayFatal()
    {
        Assert.False(BackgroundFaultPolicy.IsBenign(new NullReferenceException()));
        Assert.False(BackgroundFaultPolicy.IsBenign(new InvalidOperationException("something real broke")));
        Assert.False(BackgroundFaultPolicy.IsBenign(
            new AggregateException(new ArgumentOutOfRangeException())));
        Assert.False(BackgroundFaultPolicy.IsBenign(null));
    }

    [Fact]
    public void OneRealFaultAlongsideADroppedConnectionStillReports()
    {
        var mixed = new AggregateException(
            new SocketException((int)SocketError.ConnectionReset),
            new NullReferenceException());

        Assert.False(BackgroundFaultPolicy.IsBenign(mixed));
    }

    [Fact]
    public void ShutdownAndCancellationAreBenign()
    {
        Assert.True(BackgroundFaultPolicy.IsBenign(new ObjectDisposedException("surface")));
        Assert.True(BackgroundFaultPolicy.IsBenign(new OperationCanceledException()));
        Assert.True(BackgroundFaultPolicy.IsBenign(
            new InvalidOperationException("Application is already shutting down.")));
    }

    [Fact]
    public void ACircularChainTerminates()
    {
        // Depth cap guards the recursion; a self-referencing chain must return
        // rather than run the stack out.
        var outer = new InvalidOperationException("outer");
        var inner = new InvalidOperationException("inner", outer);

        Assert.False(BackgroundFaultPolicy.IsBenign(
            new InvalidOperationException("top", inner)));
    }
}
