using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.HealthChecks;
using FunkArr.Api.Models;
using FunkArr.Api.Validation;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FunkArr.Api;

public static class SystemApiEndpoints
{
    private const string _defaultApiKey = "funkarr-default-api-key";
    private static readonly TimeSpan _httpTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan _askTimeout = TimeSpan.FromSeconds(10);

    public static WebApplication MapSystemApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/system")
            .WithTags("System")
            .AddEndpointFilter<ValidationEndpointFilter>()
            .AddEndpointFilter<EndpointExceptionFilter>();

        group.MapGet("/setup", async (
            IOptionsMonitor<FunkArrOptions> options,
            DataPaths dataPaths,
            IDataFiles dataFiles,
            IHttpClientFactory httpClientFactory,
            HttpContext ctx) =>
        {
            var opts = options.CurrentValue;
            var selfBaseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";

            var apiKeyCheck = CheckApiKey(opts);
            var mediathekTask = CheckMediathekViewWebAsync(httpClientFactory);
            var dataCheck = CheckDirectory(dataPaths.DataRoot, dataFiles);
            var completeCheck = CheckDirectory(dataPaths.Complete, dataFiles);
            var incompleteCheck = CheckDirectory(dataPaths.Incomplete, dataFiles);
            var indexerTask = CheckSelfEndpoint(httpClientFactory, selfBaseUrl, opts, "/index/api?t=caps&apikey=");
            var downloadApiTask = CheckSelfEndpoint(httpClientFactory, selfBaseUrl, opts, "/download/api?mode=version&apikey=");
            var ffmpegTask = CheckFfmpegAsync();

            await Task.WhenAll(mediathekTask, indexerTask, downloadApiTask, ffmpegTask);

            var checks = new Dictionary<string, CheckResult>
            {
                ["apiKey"] = apiKeyCheck,
                ["mediathekViewWeb"] = await mediathekTask,
                ["dataDirectory"] = dataCheck,
                ["completeDirectory"] = completeCheck,
                ["incompleteDirectory"] = incompleteCheck,
                ["indexerApi"] = await indexerTask,
                ["downloadApi"] = await downloadApiTask,
                ["ffmpeg"] = await ffmpegTask,
            };

            var port = ctx.Request.Host.Port ?? (ctx.Request.Scheme == "https" ? 443 : 80);
            var connectionInfo = new SetupConnectionInfo(
                IndexerApiPath: "/index/api",
                DownloadApiPath: "/download/api",
                DefaultPort: port);

            return Results.Ok(new SetupHealthCheck(checks, connectionInfo));
        })
        .WithSummary("Run setup checks");

        group.MapGet("/version", (DataPaths dataPaths, IDataFiles dataFiles) =>
        {
            var appVersion = FunkArr.Core.VersionInfo.Version;
            var communityVersion = dataFiles.Exists(dataPaths.RuleSetVersion)
                ? dataFiles.ReadText(dataPaths.RuleSetVersion).Trim()
                : null;
            return Results.Ok(new VersionResponse(appVersion, communityVersion));
        })
        .WithSummary("Get application and ruleset version")
        .Produces<VersionResponse>();

        group.MapGet("/storage", (DataPaths dataPaths) =>
        {
            var complete = GetStorageDirectory(dataPaths.Complete);
            var incomplete = GetStorageDirectory(dataPaths.Incomplete);
            return Results.Ok(new StorageStatusResponse(complete, incomplete));
        })
        .WithSummary("Get storage status")
        .Produces<StorageStatusResponse>();

        group.MapGet("/cache", async (IActorRegistry registry) =>
        {
            var resolver = await registry.GetAsync<IEnrichmentManager>();
            var result = await resolver.Ask<CacheStatsResult>(new QueryCacheStats(), _askTimeout);
            return Results.Ok(new CacheStatsResponse(
                result.TvdbEntries,
                result.TmdbEntries,
                result.OldestEntry?.ToString("o")));
        })
        .WithSummary("Get metadata cache stats")
        .Produces<CacheStatsResponse>()
        .ProducesProblem(504);

        group.MapGet("/logs", (RingBufferSink sink) =>
        {
            return Results.Ok(sink.GetEntries());
        })
        .WithSummary("Get recent log entries")
        .Produces<LogEntry[]>();

        group.MapGet("/logs/stream", async (HttpContext ctx, RingBufferSink sink) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var ct = ctx.RequestAborted;
            await foreach (var entry in sink.Reader.ReadAllAsync(ct))
            {
                var json = System.Text.Json.JsonSerializer.Serialize(entry,
                    new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                await ctx.Response.WriteAsync($"data: {json}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        })
        .WithSummary("Stream log entries via SSE")
        .ExcludeFromDescription();

        group.MapGet("/routes", (IOptionsMonitor<RoutingOptions> routes) =>
        {
            var opts = routes.CurrentValue;
            var definitions = opts.Definitions.Select(d => new RouteDefinitionResponse(d.Name, d.Proxy)).ToArray();
            var channelRoutes = opts.ChannelRoutes.Select(c => new ChannelRouteResponse(c.Pattern, c.Route)).ToArray();
            return Results.Ok(new RoutesResponse(definitions, channelRoutes, opts.Default));
        })
        .WithSummary("Get network route configuration")
        .Produces<RoutesResponse>();

        return app;
    }

    internal static StorageDirectory GetStorageDirectory(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var driveInfo = new DriveInfo(Path.GetPathRoot(fullPath)!);
            return new StorageDirectory(fullPath, driveInfo.AvailableFreeSpace, driveInfo.TotalSize);
        }
        catch
        {
            return new StorageDirectory(path, 0, 0);
        }
    }

    internal static CheckResult CheckApiKey(FunkArrOptions options)
    {
        var key = options.ApiKey;
        var isDefault = string.Equals(key, _defaultApiKey, StringComparison.Ordinal);
        var masked = key.Length > 3
            ? new string('*', key.Length - 3) + key[^3..]
            : key;

        return isDefault
            ? new CheckResult(CheckStatus.Warn, "API key is still the default — change it for security", Value: key, Masked: masked)
            : new CheckResult(CheckStatus.Ok, Value: key, Masked: masked);
    }

    private static async Task<CheckResult> CheckMediathekViewWebAsync(IHttpClientFactory factory)
    {
        var (reachable, message) = await MediathekViewWebHealthCheck.ProbeAsync(factory);
        return reachable ? CheckResult.Ok() : CheckResult.Fail(message!);
    }

    internal static CheckResult CheckDirectory(string path, IDataFiles dataFiles)
    {
        var (writable, fullPath) = DirectoryHealthCheck.Probe(path, dataFiles);
        return writable
            ? new CheckResult(CheckStatus.Ok, Path: fullPath)
            : new CheckResult(CheckStatus.Fail, $"Directory not writable or does not exist: {fullPath}", Path: fullPath);
    }

    private static async Task<CheckResult> CheckSelfEndpoint(
        IHttpClientFactory factory, string selfBaseUrl, FunkArrOptions options, string pathWithParam)
    {
        try
        {
            using var client = factory.CreateClient();
            client.BaseAddress = new Uri(selfBaseUrl);
            client.Timeout = _httpTimeout;
            var url = $"{pathWithParam}{Uri.EscapeDataString(options.ApiKey)}";
            using var response = await client.GetAsync(url);

            return response.IsSuccessStatusCode
                ? CheckResult.Ok()
                : CheckResult.Fail($"Returned HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return CheckResult.Fail($"Self-test failed: {ex.Message}");
        }
    }

    internal static async Task<CheckResult> CheckFfmpegAsync()
    {
        var (available, version) = await FfmpegHealthCheck.ProbeAsync();
        return available
            ? new CheckResult(CheckStatus.Ok, Version: version)
            : CheckResult.Warn("FFmpeg not found on PATH — needed for downloads");
    }
}
