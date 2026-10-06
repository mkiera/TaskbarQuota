using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using TaskbarQuota.Diagnostics;

namespace TaskbarQuota;

internal static class DispatcherQueueExtensions
{
    // An exception escaping a DispatcherQueue callback fails fast without reaching Application.UnhandledException.
    public static bool TryEnqueueSafe(
        this DispatcherQueue dispatcher,
        DispatcherQueueHandler callback,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => dispatcher.TryEnqueue(Guard(callback, file, line));

    public static bool TryEnqueueSafe(
        this DispatcherQueue dispatcher,
        DispatcherQueuePriority priority,
        DispatcherQueueHandler callback,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        => dispatcher.TryEnqueue(priority, Guard(callback, file, line));

    internal static DispatcherQueueHandler Guard(
        DispatcherQueueHandler callback,
        string file,
        int line,
        Action<Exception, string>? report = null)
        => () =>
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                var message = $"UI callback queued at {System.IO.Path.GetFileName(file)}:{line} failed";
                if (report is null)
                    Log.Error(ex, message);
                else
                    report(ex, message);
            }
        };
}
