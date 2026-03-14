using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace ConverterApp.Services;

/// <summary>
/// Downloads source files from a public GitHub repository without requiring authentication.
/// Uses the GitHub REST API (unauthenticated, 60 req/h rate limit).
/// </summary>
public class GitHubImportService
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "ConverterApp/1.0" } },
    };

    // ──────────────────────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches source files from a GitHub repo URL and returns their contents keyed by path.
    /// Supports:
    ///   https://github.com/owner/repo
    ///   https://github.com/owner/repo/tree/branch
    ///   https://github.com/owner/repo/tree/branch/path/to/dir
    /// </summary>
    public async Task<GitHubImportResult> ImportAsync(
        string githubUrl,
        string[]? extensionFilter = null,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        if (!TryParseGitHubUrl(githubUrl, out var owner, out var repo, out var branch, out var path))
            return GitHubImportResult.Fail("Invalid GitHub URL. Expected: https://github.com/owner/repo");

        progress?.Report($"Connecting to {owner}/{repo}…");

        try
        {
            // Resolve default branch if not specified
            if (string.IsNullOrEmpty(branch))
                branch = await GetDefaultBranchAsync(owner, repo, ct) ?? "main";

            progress?.Report($"Fetching file tree from branch '{branch}'…");

            var files = await FetchFilesAsync(owner, repo, branch, path, extensionFilter, progress, ct);
            return GitHubImportResult.Ok(files);
        }
        catch (HttpRequestException hEx)
        {
            return GitHubImportResult.Fail($"Network error: {hEx.Message}");
        }
        catch (OperationCanceledException)
        {
            return GitHubImportResult.Fail("Import was cancelled.");
        }
        catch (Exception ex)
        {
            return GitHubImportResult.Fail($"Unexpected error: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Implementation
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<Dictionary<string, string>> FetchFilesAsync(
        string owner, string repo, string branch, string? dirPath,
        string[]? extensionFilter, IProgress<string>? progress, CancellationToken ct)
    {
        // Use the Trees API to get a flat list of blobs
        var treeUrl = $"https://api.github.com/repos/{owner}/{repo}/git/trees/{branch}?recursive=1";
        var treeJson = await _http.GetStringAsync(treeUrl, ct);

        using var doc = JsonDocument.Parse(treeJson);
        var blobs = doc.RootElement
            .GetProperty("tree")
            .EnumerateArray()
            .Where(e => e.GetProperty("type").GetString() == "blob")
            .Select(e => e.GetProperty("path").GetString()!)
            .Where(p => dirPath == null || p.StartsWith(dirPath, StringComparison.OrdinalIgnoreCase))
            .Where(p => extensionFilter == null || extensionFilter.Any(ext =>
                p.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var i = 0;
        foreach (var filePath in blobs)
        {
            i++;
            progress?.Report($"Downloading {i}/{blobs.Count}: {filePath}");
            var rawUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{filePath}";
            var content = await _http.GetStringAsync(rawUrl, ct);
            result[filePath] = content;
        }

        return result;
    }

    private async Task<string?> GetDefaultBranchAsync(string owner, string repo, CancellationToken ct)
    {
        var repoUrl = $"https://api.github.com/repos/{owner}/{repo}";
        var json = await _http.GetStringAsync(repoUrl, ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("default_branch", out var b) ? b.GetString() : null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  URL parser
    // ──────────────────────────────────────────────────────────────────────────

    private static bool TryParseGitHubUrl(
        string url,
        out string owner, out string repo, out string? branch, out string? path)
    {
        owner = repo = string.Empty;
        branch = path = null;

        if (string.IsNullOrWhiteSpace(url)) return false;

        url = url.Trim().TrimEnd('/');

        // Remove protocol + host
        const string prefix = "https://github.com/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            // also accept github.com/owner/repo
            if (!url.StartsWith("github.com/", StringComparison.OrdinalIgnoreCase))
                return false;
            url = "https://" + url;
        }

        var rest = url[prefix.Length..];
        var segments = rest.Split('/');

        if (segments.Length < 2) return false;
        owner = segments[0];
        repo  = segments[1].Replace(".git", "");

        if (segments.Length >= 4 && segments[2] == "tree")
        {
            branch = segments[3];
            if (segments.Length >= 5)
                path = string.Join("/", segments[4..]);
        }

        return !string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo);
    }
}

public record GitHubImportResult(
    bool Success,
    string? Error,
    Dictionary<string, string> Files)
{
    public static GitHubImportResult Ok(Dictionary<string, string> files) =>
        new(true, null, files);

    public static GitHubImportResult Fail(string error) =>
        new(false, error, new());
}
