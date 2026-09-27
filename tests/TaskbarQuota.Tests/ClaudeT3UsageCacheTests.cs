using System;
using System.Text.Json;
using TaskbarQuota.Usage;
using TaskbarQuota.Usage.Providers;

namespace TaskbarQuota.Tests;

public class ClaudeT3UsageCacheTests
{
    private static readonly DateTimeOffset CheckedAt = DateTimeOffset.Parse("2026-09-27T03:15:41.667Z");

    private const string Cache = """
        {
          "usageLimits": {
            "checkedAt": "2026-09-27T03:15:41.667Z",
            "windows": [
              { "id": "five_hour", "kind": "session", "windowDurationMins": 300, "usedPercent": 70, "resetsAt": "2026-09-27T04:59:59.524Z" },
              { "id": "seven_day", "kind": "weekly", "windowDurationMins": 10080, "usedPercent": 36, "resetsAt": "2026-09-27T17:59:59.524Z" },
              { "id": "seven_day_fable", "kind": "weekly", "windowDurationMins": 10080, "usedPercent": 3, "resetsAt": "2026-09-27T17:59:59.525Z" }
            ]
          }
        }
        """;

    private static ProviderFetchResult? Build(string json, DateTimeOffset now)
    {
        using var doc = JsonDocument.Parse(json);
        return ClaudeProvider.BuildResultFromT3Cache(doc.RootElement, now, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Fresh_cache_maps_session_weekly_and_fable()
    {
        var result = Build(Cache, CheckedAt.AddMinutes(2));

        Assert.NotNull(result);
        var usage = result!.Usage;
        Assert.Equal(70, usage.Primary.UsedPercent);
        Assert.Equal(300, usage.Primary.WindowMinutes);
        Assert.Equal(DateTimeOffset.Parse("2026-09-27T04:59:59.524Z"), usage.Primary.ResetAt);
        Assert.True(usage.HasPrimaryWindow);
        Assert.Equal(36, usage.Secondary!.UsedPercent);
        var fable = Assert.Single(usage.ExtraRateWindows);
        Assert.Equal("claude-fable", fable.Id);
        Assert.Equal(3, fable.Window.UsedPercent);
        Assert.Equal("t3code", result.SourceLabel);
    }

    [Fact]
    public void Cache_older_than_the_limit_is_ignored()
        => Assert.Null(Build(Cache, CheckedAt.AddMinutes(6)));

    [Fact]
    public void Cache_without_usage_limits_is_ignored()
        => Assert.Null(Build("""{ "status": "ready" }""", CheckedAt));
}
