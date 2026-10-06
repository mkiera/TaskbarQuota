using System;

namespace TaskbarQuota.Tests;

public class DispatcherQueueExtensionsTests
{
    [Fact]
    public void Guarded_callback_reports_the_exception_with_its_queue_site()
    {
        Exception? reported = null;
        string? message = null;
        var thrown = new InvalidOperationException("boom");
        var guarded = DispatcherQueueExtensions.Guard(() => throw thrown, @"C:\src\Taskbar\TaskBarWidget.cs", 1842,
            (ex, text) => { reported = ex; message = text; });

        guarded();

        Assert.Same(thrown, reported);
        Assert.Contains("TaskBarWidget.cs:1842", message);
    }

    [Fact]
    public void Guarded_callback_still_runs_the_callback()
    {
        var ran = false;
        var reported = false;
        var guarded = DispatcherQueueExtensions.Guard(() => ran = true, "Widget.cs", 12, (_, _) => reported = true);

        guarded();

        Assert.True(ran);
        Assert.False(reported);
    }
}
