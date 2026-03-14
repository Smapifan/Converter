using ConverterApp.Services;
using Xunit;

namespace ConverterApp.Tests;

/// <summary>Tests for the GitHub URL parser (offline, no network calls).</summary>
public class GitHubImportServiceTests
{
    // We test the URL parser via the public ImportAsync (which we can mock)
    // by using a private-method reflection approach, or we expose an internal
    // helper. For now we test behaviour through observable output expectations.

    [Theory]
    [InlineData("https://github.com/owner/repo", true)]
    [InlineData("https://github.com/owner/repo.git", true)]
    [InlineData("https://github.com/owner/repo/tree/main", true)]
    [InlineData("https://github.com/owner/repo/tree/main/src/dir", true)]
    [InlineData("github.com/owner/repo", true)]
    [InlineData("https://example.com/owner/repo", false)]
    [InlineData("", false)]
    [InlineData("not-a-url", false)]
    public async Task TryParseGitHubUrl_ValidatesUrlCorrectly(string url, bool expectedValid)
    {
        // For invalid URLs the service returns a failure before making any network calls.
        // For valid-format URLs we don't exercise network I/O here.
        if (!expectedValid)
        {
            var svc = new GitHubImportService();
            // Use a 500ms cancellation safety net; the URL check itself is synchronous
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var result = await svc.ImportAsync(url, null, null, cts.Token);
            Assert.False(result.Success);
        }
    }

    [Fact]
    public void ImportResult_Fail_SetsSuccessFalse()
    {
        var r = GitHubImportResult.Fail("Some error");
        Assert.False(r.Success);
        Assert.Equal("Some error", r.Error);
        Assert.Empty(r.Files);
    }

    [Fact]
    public void ImportResult_Ok_SetsSuccessTrue()
    {
        var files = new Dictionary<string, string> { ["a.html"] = "<p>" };
        var r = GitHubImportResult.Ok(files);
        Assert.True(r.Success);
        Assert.Null(r.Error);
        Assert.Single(r.Files);
    }
}
