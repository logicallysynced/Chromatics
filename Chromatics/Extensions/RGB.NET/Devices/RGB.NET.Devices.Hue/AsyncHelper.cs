using Chromatics.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue;

internal static class AsyncHelper
{
    private static readonly TaskFactory _taskFactory = new(CancellationToken.None,
        TaskCreationOptions.None,
        TaskContinuationOptions.None,
        TaskScheduler.Default);

    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        return _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        
    }

    public static void RunSync(Func<Task> func)
    {
        try
        {
            _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            Logger.WriteConsole(Enums.LoggerTypes.Error, ex.Message);
        }
    }
}