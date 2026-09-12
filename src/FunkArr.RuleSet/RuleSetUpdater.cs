using System.IO.Compression;
using System.Text.Json;
using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using Microsoft.Extensions.Options;

namespace FunkArr.RuleSet;

public sealed class RuleSetUpdater : ReceiveActor, IWithTimers
{
    public sealed record CheckForUpdates;

    private static readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataFiles _dataFiles;
    private readonly DataPaths _dataPaths;
    private readonly IOptionsMonitor<RuleSetUpdaterOptions> _optionsMonitor;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public ITimerScheduler Timers { get; set; } = null!;

    public RuleSetUpdater(
        IHttpClientFactory httpClientFactory,
        IDataFiles dataFiles,
        DataPaths dataPaths,
        IOptionsMonitor<RuleSetUpdaterOptions> optionsMonitor)
    {
        _httpClientFactory = httpClientFactory;
        _dataFiles = dataFiles;
        _dataPaths = dataPaths;
        _optionsMonitor = optionsMonitor;

        ReceiveAsync<CheckForUpdates>(_ => HandleCheckForUpdates());
    }

    protected override void PreStart()
    {
        if (!_optionsMonitor.CurrentValue.RefreshEnabled)
        {
            _log.Info("Community ruleset refresh is disabled");
            return;
        }

        Self.Tell(new CheckForUpdates());
    }

    private async Task HandleCheckForUpdates()
    {
        try
        {
            await DoCheckForUpdates();
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to check for community ruleset updates");
        }

        ScheduleNext();
    }

    private void ScheduleNext()
        => Timers.StartSingleTimer("refresh", new CheckForUpdates(), _refreshInterval);

    private async Task DoCheckForUpdates()
    {
        var opts = _optionsMonitor.CurrentValue;
        var rulesetsDir = _dataPaths.CommunityRuleSets;
        var versionFile = _dataPaths.RuleSetVersion;

        var localVersion = _dataFiles.Exists(versionFile)
            ? _dataFiles.ReadText(versionFile).Trim()
            : null;

        var release = await FindRelease();
        if (release is null)
        {
            return;
        }

        if (localVersion is not null && localVersion == release.Value.Version)
        {
            _log.Debug("Community rulesets already at version {Version}", localVersion);
            return;
        }

        if (release.Value.AssetUrl is null)
        {
            _log.Warning("Release {Tag} has no rulesets.zip asset", release.Value.Tag);
            return;
        }

        _log.Info("Updating community rulesets from {OldVersion} to {NewVersion}",
            localVersion ?? "(none)", release.Value.Version);

        using var downloadClient = _httpClientFactory.CreateClient("GitHub");
        var zipBytes = await downloadClient.GetByteArrayAsync(release.Value.AssetUrl);

        var tempDir = Path.Join(_dataPaths.Temp, $"rulesets-{Guid.NewGuid():N}");

        try
        {
            _dataFiles.CreateDirectory(tempDir);

            using var stream = new MemoryStream(zipBytes);
            await using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            await archive.ExtractToDirectoryAsync(tempDir);

            var extractedRulesets = Path.Join(tempDir, "rulesets");
            var sourceDir = Directory.Exists(extractedRulesets) ? extractedRulesets : tempDir;
            _dataFiles.ReplaceDirectory(sourceDir, rulesetsDir);
            _dataFiles.WriteText(versionFile, release.Value.Version);

            _log.Info("Community rulesets updated to version {Version}", release.Value.Version);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to extract community rulesets");
        }
        finally
        {
            _dataFiles.Remove(tempDir);
        }
    }

    private async Task<ReleaseInfo?> FindRelease()
    {
        var opts = _optionsMonitor.CurrentValue;
        var url = $"repos/{opts.Repository}/releases";

        using var client = _httpClientFactory.CreateClient("GitHub");
        using var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _log.Warning("GitHub API returned {StatusCode} for {Url}", (int)response.StatusCode, url);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var releases = JsonSerializer.Deserialize<JsonElement[]>(json);
        if (releases is null)
        {
            return null;
        }

        foreach (var rel in releases)
        {
            if (!rel.TryGetProperty("tag_name", out var tagEl))
            {
                continue;
            }

            var tag = tagEl.GetString();
            if (tag is null || !tag.StartsWith("rulesets-v", StringComparison.Ordinal))
            {
                continue;
            }

            var version = tag["rulesets-v".Length..];

            if (opts.Version != "latest" && version != opts.Version)
            {
                continue;
            }

            string? assetUrl = null;
            if (rel.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var nameEl) &&
                        nameEl.GetString() == "rulesets.zip" &&
                        asset.TryGetProperty("browser_download_url", out var urlEl))
                    {
                        assetUrl = urlEl.GetString();
                        break;
                    }
                }
            }

            return new ReleaseInfo(tag, version, assetUrl);
        }

        if (opts.Version != "latest")
        {
            _log.Warning("Pinned version {Version} not found in releases", opts.Version);
        }

        return null;
    }

    private readonly record struct ReleaseInfo(string Tag, string Version, string? AssetUrl);
}
