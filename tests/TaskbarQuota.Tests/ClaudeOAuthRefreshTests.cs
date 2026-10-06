using TaskbarQuota.Services;

namespace TaskbarQuota.Tests;

public class ClaudeOAuthRefreshTests
{
    [Fact]
    public void Refresh_does_not_ask_for_the_scope_the_token_endpoint_rejects()
    {
        var body = ClaudeOAuth.BuildRefreshBody("refresh-token");

        Assert.Equal("refresh_token", body["grant_type"]);
        Assert.Equal("refresh-token", body["refresh_token"]);
        Assert.DoesNotContain("org:create_api_key", body["scope"]);
        Assert.Contains("user:inference", body["scope"]);
    }
}
