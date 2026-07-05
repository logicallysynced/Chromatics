using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.Protocol
{
    // No-throw TCP reachability check. SocketAsyncEventArgs reports failure
    // through SocketError instead of throwing, so "not there" - the expected
    // outcome for offline controllers and wrong IPs - stays exception-free.
    // Shared by the manual-IP probe and the REST client's pre-flight gate;
    // without the gate, HttpClient reports an unreachable host by throwing
    // HttpRequestException, which spams the debugger during pairing polls
    // and capture retries.
    public static class NanoleafTcpCheck
    {
        public static async Task<bool> CanConnectAsync(string host, int port, TimeSpan timeout, CancellationToken ct = default)
        {
            if (!IPAddress.TryParse(host, out var addr)) return false;
            if (ct.IsCancellationRequested) return false;

            try
            {
                using var sock = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                using var connectArgs = new SocketAsyncEventArgs { RemoteEndPoint = new IPEndPoint(addr, port) };
                var completion = new TaskCompletionSource<SocketError>(TaskCreationOptions.RunContinuationsAsynchronously);
                connectArgs.Completed += (_, e) => completion.TrySetResult(e.SocketError);

                if (!sock.ConnectAsync(connectArgs))
                    completion.TrySetResult(connectArgs.SocketError);

                var winner = await Task.WhenAny(completion.Task, Task.Delay(timeout)).ConfigureAwait(false);
                if (winner != completion.Task)
                {
                    // Timed out. Abort the attempt, then wait for the aborted
                    // completion so the args aren't disposed mid-operation.
                    Socket.CancelConnectAsync(connectArgs);
                    await Task.WhenAny(completion.Task, Task.Delay(1000)).ConfigureAwait(false);
                    return false;
                }

                return completion.Task.Result == SocketError.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}
