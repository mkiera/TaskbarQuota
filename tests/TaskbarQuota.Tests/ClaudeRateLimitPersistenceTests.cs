using System;
using System.IO;
using TaskbarQuota.Usage;
using TaskbarQuota.Usage.Providers;

namespace TaskbarQuota.Tests;

[Collection(WidgetRowSettingsCollection.Name)]
public class ClaudeRateLimitPersistenceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("claude-rate-limit-").FullName;
    private readonly IDisposable _storage;

    public ClaudeRateLimitPersistenceTests()
    {
        _storage = AppStorage.OverrideAppDataDirectoryForTesting(_directory);
        ClaudeProvider.ForgetInMemoryRateLimitForTesting();
    }

    public void Dispose()
    {
        ClaudeProvider.ForgetInMemoryRateLimitForTesting();
        _storage.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Rate_limit_survives_a_restart()
    {
        ClaudeProvider.RecordOAuthRateLimitUntil(DateTimeOffset.Now.AddHours(1));

        ClaudeProvider.ForgetInMemoryRateLimitForTesting();

        Assert.True(ClaudeProvider.IsOAuthRateLimited());
    }

    [Fact]
    public void Expired_rate_limit_is_cleared_after_a_restart()
    {
        ClaudeProvider.RecordOAuthRateLimitUntil(DateTimeOffset.Now.AddSeconds(-1));

        ClaudeProvider.ForgetInMemoryRateLimitForTesting();

        Assert.False(ClaudeProvider.IsOAuthRateLimited());
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void New_login_clears_a_saved_rate_limit_before_it_is_loaded()
    {
        ClaudeProvider.RecordOAuthRateLimitUntil(DateTimeOffset.Now.AddHours(1));
        ClaudeProvider.ForgetInMemoryRateLimitForTesting();

        ClaudeProvider.ClearRateLimitAfterLogin();
        ClaudeProvider.ForgetInMemoryRateLimitForTesting();

        Assert.False(ClaudeProvider.IsOAuthRateLimited());
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void No_saved_rate_limit_means_not_limited()
        => Assert.False(ClaudeProvider.IsOAuthRateLimited());

    [Fact]
    public void Recent_result_is_reused_only_for_the_same_token()
    {
        var now = DateTimeOffset.Now;
        var result = new ProviderFetchResult(new UsageSnapshot(new RateWindow(40)), "oauth");
        ClaudeProvider.RememberOAuthResult("token-a", result, now);

        Assert.Same(result, ClaudeProvider.RecentOAuthResult("token-a", now.AddSeconds(10)));
        Assert.Null(ClaudeProvider.RecentOAuthResult("token-b", now.AddSeconds(10)));
        Assert.Null(ClaudeProvider.RecentOAuthResult("token-a", now.AddSeconds(31)));
    }
}
