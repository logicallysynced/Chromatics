using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class NetworkFailureHelperTests
{
    [Fact]
    public void ConnectionRefused_IsUnreachable()
    {
        var ex = new SocketException((int)SocketError.ConnectionRefused);

        Assert.True(NetworkFailureHelper.IsUnreachable(ex));
    }

    [Fact]
    public void SocketFailureWrappedInIoException_IsUnreachable()
    {
        var ex = new IOException("write failed", new SocketException((int)SocketError.HostUnreachable));

        Assert.True(NetworkFailureHelper.IsUnreachable(ex));
    }

    [Fact]
    public void HttpAndTimeoutFailures_AreUnreachable()
    {
        Assert.True(NetworkFailureHelper.IsUnreachable(new HttpRequestException("no route")));
        Assert.True(NetworkFailureHelper.IsUnreachable(new TimeoutException()));
        Assert.True(NetworkFailureHelper.IsUnreachable(new TaskCanceledException()));
    }

    [Fact]
    public void SocketFailureInsideAggregate_IsUnreachable()
    {
        var ex = new AggregateException(
            new InvalidOperationException("unrelated"),
            new SocketException((int)SocketError.TimedOut));

        Assert.True(NetworkFailureHelper.IsUnreachable(ex));
    }

    [Fact]
    public void ApplicationBugs_AreNotUnreachable()
    {
        Assert.False(NetworkFailureHelper.IsUnreachable(new NullReferenceException()));
        Assert.False(NetworkFailureHelper.IsUnreachable(new InvalidOperationException()));
        Assert.False(NetworkFailureHelper.IsUnreachable(
            new InvalidOperationException("outer", new ArgumentOutOfRangeException())));
    }
}
