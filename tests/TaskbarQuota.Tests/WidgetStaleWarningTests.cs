using System;
using TaskbarQuota.Controls;
using TaskbarQuota.Usage;
using TaskbarQuota.Usage.Providers;

namespace TaskbarQuota.Tests;

public class WidgetStaleWarningTests
{
    private static UsageResult Result()
        => UsageResult.Success(ProviderId.Claude, new ClaudeProvider(),
            new ProviderFetchResult(new UsageSnapshot(new RateWindow(40)), "oauth"));

    [Fact]
    public void Old_values_kept_after_failed_refreshes_show_the_warning()
        => Assert.True(WidgetSummary.ShowsStaleWarning(Result().AsFailureFallback(1, DateTimeOffset.Now, isStale: true)));

    [Fact]
    public void Recent_values_kept_after_a_failed_refresh_do_not_show_the_warning()
        => Assert.False(WidgetSummary.ShowsStaleWarning(Result().AsFailureFallback(1, DateTimeOffset.Now)));

    [Fact]
    public void Snapshot_restored_at_startup_does_not_show_the_warning()
        => Assert.False(WidgetSummary.ShowsStaleWarning(Result().AsStale()));

    [Fact]
    public void Live_values_do_not_show_the_warning()
        => Assert.False(WidgetSummary.ShowsStaleWarning(Result().AsLiveObservation(1, DateTimeOffset.Now)));
}
