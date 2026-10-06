using System;
using TaskbarQuota.Diagnostics;

namespace TaskbarQuota.Taskbar;

internal sealed class TrayIconCreator
{
    internal const int MaxAttempts = 40;

    private readonly Action _create;
    private readonly Func<bool> _remove;
    private readonly Action _scheduleRetry;

    public TrayIconCreator(Action create, Func<bool> remove, Action scheduleRetry)
    {
        _create = create;
        _remove = remove;
        _scheduleRetry = scheduleRetry;
    }

    public int FailedAttempts { get; private set; }

    public bool TryCreate()
    {
        try
        {
            _create();
            if (FailedAttempts > 0)
                Log.Information($"Tray icon created after {FailedAttempts} failed attempts");
            FailedAttempts = 0;
            return true;
        }
        catch (Exception ex)
        {
            FailedAttempts++;
            if (FailedAttempts == 1)
                Log.Warning(ex, "Tray icon could not be created yet; retrying");
            if (FailedAttempts >= MaxAttempts)
            {
                Log.Warning($"Tray icon creation gave up after {FailedAttempts} attempts");
                return false;
            }
            _scheduleRetry();
            return false;
        }
    }

    // Explorer's restart drops the icon, but the library still reports it as created and would skip Create.
    public bool RecreateAfterTaskbarRestart()
    {
        FailedAttempts = 0;
        try { _remove(); }
        catch (Exception ex) { Log.Warning(ex, "Tray icon removal before recreation failed"); }
        return TryCreate();
    }
}
