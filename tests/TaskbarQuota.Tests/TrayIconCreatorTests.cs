using System;
using TaskbarQuota.Taskbar;

namespace TaskbarQuota.Tests;

public class TrayIconCreatorTests
{
    private sealed class FakeTray
    {
        public bool IsCreated;
        public int FailuresLeft;
        public int CreateCalls;
        public int RemoveCalls;
        public int RetriesScheduled;

        // Mirrors H.NotifyIcon: Create returns early once created, and TryRemove clears the flag.
        public void Create()
        {
            CreateCalls++;
            if (IsCreated)
                return;
            if (FailuresLeft > 0)
            {
                FailuresLeft--;
                throw new InvalidOperationException("TryCreate failed.");
            }
            IsCreated = true;
        }

        public bool TryRemove()
        {
            RemoveCalls++;
            IsCreated = false;
            return true;
        }

        public TrayIconCreator Creator() => new(Create, TryRemove, () => RetriesScheduled++);
    }

    [Fact]
    public void Failure_at_login_schedules_retries_until_the_tray_is_ready()
    {
        var tray = new FakeTray { FailuresLeft = 2 };
        var creator = tray.Creator();

        Assert.False(creator.TryCreate());
        Assert.False(creator.TryCreate());
        Assert.True(creator.TryCreate());

        Assert.True(tray.IsCreated);
        Assert.Equal(2, tray.RetriesScheduled);
        Assert.Equal(0, creator.FailedAttempts);
    }

    [Fact]
    public void Retries_stop_after_the_limit()
    {
        var tray = new FakeTray { FailuresLeft = int.MaxValue };
        var creator = tray.Creator();

        for (var i = 0; i < TrayIconCreator.MaxAttempts; i++)
            creator.TryCreate();

        Assert.Equal(TrayIconCreator.MaxAttempts - 1, tray.RetriesScheduled);
        Assert.False(tray.IsCreated);
    }

    [Fact]
    public void Taskbar_restart_after_a_successful_create_adds_the_icon_again()
    {
        var tray = new FakeTray();
        var creator = tray.Creator();
        creator.TryCreate();
        var callsBefore = tray.CreateCalls;

        Assert.True(creator.RecreateAfterTaskbarRestart());

        Assert.Equal(1, tray.RemoveCalls);
        Assert.Equal(callsBefore + 1, tray.CreateCalls);
        Assert.True(tray.IsCreated);
    }

    [Fact]
    public void Taskbar_restart_resets_the_retry_count()
    {
        var tray = new FakeTray { FailuresLeft = int.MaxValue };
        var creator = tray.Creator();
        for (var i = 0; i < TrayIconCreator.MaxAttempts; i++)
            creator.TryCreate();
        tray.FailuresLeft = 1;

        Assert.False(creator.RecreateAfterTaskbarRestart());

        Assert.Equal(1, creator.FailedAttempts);
        Assert.Equal(TrayIconCreator.MaxAttempts, tray.RetriesScheduled);
    }
}
