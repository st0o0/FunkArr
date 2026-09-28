using System.Text.Json.Serialization;

namespace FunkArr.RuleSet;

internal sealed record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string? TagName,
    [property: JsonPropertyName("assets")] GitHubReleaseAsset[]? Assets);

internal sealed record GitHubReleaseAsset(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("browser_download_url")] string? BrowserDownloadUrl);
