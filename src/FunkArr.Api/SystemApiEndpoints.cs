using System.Diagnostics;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.MetadataResolver;
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
        var group = app.MapGroup("/api/system").WithTags("System");

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
            var mediathekTask = CheckMediathekViewWeb(httpClientFactory);
            var dataCheck = CheckDirectory(dataPaths.DataRoot, dataFiles);
            var completeCheck = CheckDirectory(dataPaths.Complete, dataFiles);
            var incompleteCheck = CheckDirectory(dataPaths.Incomplete, dataFiles);
            var indexerTask = CheckSelfEndpoint(httpClientFactory, selfBaseUrl, opts, "/index/api?t=caps&apikey=");
            var downloadApiTask = CheckSelfEndpoint(httpClientFactory, selfBaseUrl, opts, "/download/api?mode=version&apikey=");
            var ffmpegTask = CheckFfmpeg();

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
            var resolver = registry.Get<IMetadataResolver>();
            try
            {
                var result = await resolver.Ask<CacheStatsResult>(new QueryCacheStats(), _askTimeout);
                return Results.Ok(new CacheStatsResponse(
                    result.TvdbEntries,
                    result.TmdbEntries,
                    result.OldestEntry?.ToString("o")));
            }
            catch (Exception)
            {
                return Results.Problem(statusCode: 504, title: "Gateway Timeout");
            }
        })
        .WithSummary("Get metadata cache stats")
        .Produces<CacheStatsResponse>()
        .ProducesProblem(504);

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
            ? new CheckResult("warn", "API key is still the default — change it for security", Value: key, Masked: masked)
            : new CheckResult("ok", Value: key, Masked: masked);
    }

    private static async Task<CheckResult> CheckMediathekViewWeb(IHttpClientFactory factory)
    {
        try
        {
            using var client = factory.CreateClient();
            client.Timeout = _httpTimeout;
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://mediathekviewweb.de/");
            using var response = await client.SendAsync(request);

            return response.IsSuccessStatusCode
                ? CheckResult.Ok()
                : CheckResult.Fail($"MediathekViewWeb returned HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return CheckResult.Fail($"MediathekViewWeb unreachable: {ex.Message}");
        }
    }

    internal static CheckResult CheckDirectory(string path, IDataFiles dataFiles)
    {
        var fullPath = Path.GetFullPath(path);
        return dataFiles.CanWrite(fullPath)
            ? new CheckResult("ok", Path: fullPath)
            : new CheckResult("fail", $"Directory not writable or does not exist: {fullPath}", Path: fullPath);
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

    internal static async Task<CheckResult> CheckFfmpeg()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();

            var output = await process.StandardOutput.ReadLineAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && output is not null)
            {
                var version = output.Contains("version")
                    ? output.Split(' ').SkipWhile(s => s != "version").Skip(1).FirstOrDefault() ?? "unknown"
                    : "unknown";
                return new CheckResult("ok", Version: version);
            }

            return CheckResult.Warn("FFmpeg not working correctly — needed for downloads");
        }
        catch
        {
            return CheckResult.Warn("FFmpeg not found on PATH — needed for downloads");
        }
    }
}
