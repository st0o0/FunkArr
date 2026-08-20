using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.RuleSet;

public sealed class GitHubReleaseClient
{
    private const string TagPrefix = "community-rulesets-v";
    private const string AssetName = "community-rulesets.zip";
    private const string VersionFileName = "version.txt";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FunkArrOptions _options;
    private readonly ILogger<GitHubReleaseClient> _logger;

    public GitHubReleaseClient(
        IHttpClientFactory httpClientFactory,
        IOptions<FunkArrOptions> options,
        ILogger<GitHubReleaseClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> RefreshAsync(string communityPath, CancellationToken ct = default)
    {
        var release = await FindReleaseAsync(ct);
        if (release is null)
            return false;

        var remoteVersion = release.TagName[TagPrefix.Length..];
        var localVersion = ReadLocalVersion(communityPath);

        if (localVersion is not null && localVersion == remoteVersion)
        {
            _logger.LogDebug("Community rulesets already at version {Version}, skipping download", remoteVersion);
            return false;
        }

        var assetUrl = release.Assets
            .FirstOrDefault(a => a.Name == AssetName)
            ?.BrowserDownloadUrl;

        if (assetUrl is null)
        {
            _logger.LogWarning("Release {Tag} has no {Asset} asset", release.TagName, AssetName);
            return false;
        }

        var zipBytes = await DownloadAssetAsync(assetUrl, ct);
        if (zipBytes is null)
            return false;

        ExtractAtomically(communityPath, zipBytes, remoteVersion);

        _logger.LogInformation("Updated community rulesets to version {Version}", remoteVersion);
        return true;
    }

    private async Task<GitHubRelease?> FindReleaseAsync(CancellationToken ct)
    {
        try
        {
            using var client = CreateClient();
            var url = $"https://api.github.com/repos/{_options.RuleSetRepository}/releases";
            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var releases = await JsonSerializer.DeserializeAsync<GitHubRelease[]>(
                await response.Content.ReadAsStreamAsync(ct), JsonOptions, ct);

            if (releases is null || releases.Length == 0)
            {
                _logger.LogWarning("No releases found in {Repository}", _options.RuleSetRepository);
                return null;
            }

            var isLatest = string.Equals(_options.RuleSetVersion, "latest", StringComparison.OrdinalIgnoreCase);

            if (isLatest)
            {
                return releases.FirstOrDefault(r => r.TagName.StartsWith(TagPrefix, StringComparison.Ordinal));
            }

            var targetTag = $"{TagPrefix}{_options.RuleSetVersion}";
            var match = releases.FirstOrDefault(r =>
                string.Equals(r.TagName, targetTag, StringComparison.Ordinal));

            if (match is null)
                _logger.LogWarning("Pinned version {Version} not found in {Repository}", _options.RuleSetVersion, _options.RuleSetRepository);

            return match;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query GitHub Releases API for {Repository}", _options.RuleSetRepository);
            return null;
        }
    }

    private async Task<byte[]?> DownloadAssetAsync(string url, CancellationToken ct)
    {
        try
        {
            using var client = CreateClient();
            return await client.GetByteArrayAsync(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download ruleset ZIP from {Url}", url);
            return null;
        }
    }

    private void ExtractAtomically(string communityPath, byte[] zipBytes, string version)
    {
        var tempPath = communityPath + "-temp-" + Guid.NewGuid().ToString("N")[..8];
        var oldPath = communityPath + "-old-" + Guid.NewGuid().ToString("N")[..8];

        try
        {
            Directory.CreateDirectory(tempPath);

            using (var stream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempPath);
            }

            if (Directory.Exists(communityPath))
                Directory.Move(communityPath, oldPath);

            Directory.Move(tempPath, communityPath);

            File.WriteAllText(Path.Combine(communityPath, VersionFileName), version);

            if (Directory.Exists(oldPath))
                Directory.Delete(oldPath, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract community rulesets atomically");

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, recursive: true);

            if (!Directory.Exists(communityPath) && Directory.Exists(oldPath))
                Directory.Move(oldPath, communityPath);
        }
    }

    private static string? ReadLocalVersion(string communityPath)
    {
        var versionFile = Path.Combine(communityPath, VersionFileName);
        if (!File.Exists(versionFile))
            return null;

        return File.ReadAllText(versionFile).Trim();
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("GitHubRelease");
        return client;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("assets")]
        public GitHubAsset[] Assets { get; init; } = [];
    }

    private sealed record GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; init; } = string.Empty;
    }
}
