using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace FunkArr.Configuration;

public static class SetupEndpoints
{
    public static void MapSetupEndpoints(this WebApplication app)
    {
        var setupGroup = app.MapGroup("/api/setup")
            .AddEndpointFilter<SetupApiKeyFilter>();

        setupGroup.MapGet("/status", HandleStatus);
        setupGroup.MapPost("/test-prowlarr", HandleTestProwlarr);
        setupGroup.MapPost("/test-arr", HandleTestArr);
        setupGroup.MapPost("/test-paths", (Delegate)HandleTestPaths);
        setupGroup.MapPost("/test-ffmpeg", HandleTestFfmpeg);
        setupGroup.MapPost("/test-mediathek", HandleTestMediathek);

        var configGroup = app.MapGroup("/api/config")
            .AddEndpointFilter<SetupApiKeyFilter>();

        configGroup.MapGet("/", HandleGetConfig);
        configGroup.MapPut("/", HandlePutConfig);
    }

    private static async Task<IResult> HandleStatus(
        IOptions<FunkArrOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        var opts = options.Value;

        var ffmpegTask = CheckFfmpeg();
        var pathsTask = Task.Run(() => (DownloadOk: TestWriteAccess(opts.DownloadPath), TempOk: TestWriteAccess(opts.TempPath)));
        var mediathekTask = CheckMediathek(httpClientFactory);
        var prowlarrTask = CheckProwlarr(opts.Prowlarr, httpClientFactory);
        var arrTask = CheckArrInstances(opts.ArrInstances, httpClientFactory);

        await Task.WhenAll(ffmpegTask, pathsTask, mediathekTask, prowlarrTask, arrTask);

        var ffmpeg = await ffmpegTask;
        var paths = await pathsTask;
        var mediathek = await mediathekTask;
        var prowlarr = await prowlarrTask;
        var arrInstances = await arrTask;

        var configured = ffmpeg.Found
                         && paths.DownloadOk
                         && paths.TempOk
                         && mediathek
                         && !string.IsNullOrEmpty(opts.ApiKey);

        return Results.Json(new
        {
            configured,
            ffmpeg = new { found = ffmpeg.Found, version = ffmpeg.Version },
            paths = new { downloadOk = paths.DownloadOk, tempOk = paths.TempOk },
            mediathek = new { reachable = mediathek },
            rulesets = new { topicCount = 0 },
            prowlarr = new { connected = prowlarr },
            arrInstances,
        });
    }

    private static async Task<IResult> HandleTestProwlarr(
        HttpContext context,
        IHttpClientFactory httpClientFactory)
    {
        var body = await context.Request.ReadFromJsonAsync<TestConnectionRequest>();
        if (body is null || string.IsNullOrEmpty(body.Url) || string.IsNullOrEmpty(body.ApiKey))
        {
            return Results.Json(new { success = false, error = "url and apiKey are required" }, statusCode: 400);
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"{body.Url.TrimEnd('/')}/api/v1/health";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", body.ApiKey);
            var response = await client.SendAsync(request);

            return Results.Json(new { success = response.IsSuccessStatusCode, statusCode = (int)response.StatusCode });
        }
        catch (Exception ex)
        {
            return Results.Json(new { success = false, error = ex.Message });
        }
    }

    private static async Task<IResult> HandleTestArr(
        HttpContext context,
        IHttpClientFactory httpClientFactory)
    {
        var body = await context.Request.ReadFromJsonAsync<TestArrRequest>();
        if (body is null || string.IsNullOrEmpty(body.Url) || string.IsNullOrEmpty(body.ApiKey))
        {
            return Results.Json(new { success = false, error = "url, apiKey, and type are required" }, statusCode: 400);
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"{body.Url.TrimEnd('/')}/api/v3/system/status";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", body.ApiKey);
            var response = await client.SendAsync(request);

            return Results.Json(new { success = response.IsSuccessStatusCode, statusCode = (int)response.StatusCode });
        }
        catch (Exception ex)
        {
            return Results.Json(new { success = false, error = ex.Message });
        }
    }

    private static async Task<IResult> HandleTestPaths(HttpContext context)
    {
        var body = await context.Request.ReadFromJsonAsync<TestPathsRequest>();
        if (body is null)
        {
            return Results.Json(new { success = false, error = "downloadPath and tempPath are required" }, statusCode: 400);
        }

        var downloadOk = TestWriteAccess(body.DownloadPath);
        var tempOk = TestWriteAccess(body.TempPath);

        return Results.Json(new { downloadOk, tempOk });
    }

    private static async Task<IResult> HandleTestFfmpeg()
    {
        var result = await CheckFfmpeg();
        return Results.Json(new { found = result.Found, version = result.Version });
    }

    private static async Task<IResult> HandleTestMediathek(IHttpClientFactory httpClientFactory)
    {
        var reachable = await CheckMediathek(httpClientFactory);
        return Results.Json(new { reachable });
    }

    private static async Task<IResult> HandleGetConfig(IOptions<FunkArrOptions> options)
    {
        var opts = options.Value;

        var maskedArrInstances = opts.ArrInstances.Select(a => new
        {
            a.Name,
            a.Type,
            a.Url,
            apiKey = MaskApiKey(a.ApiKey),
        }).ToList();

        var maskedProwlarr = opts.Prowlarr is not null
            ? new { opts.Prowlarr.Url, apiKey = MaskApiKey(opts.Prowlarr.ApiKey) }
            : null;

        return Results.Json(new
        {
            opts.ApiKey,
            opts.DownloadPath,
            opts.TempPath,
            opts.PersistencePath,
            opts.ConcurrentDownloads,
            opts.PathMapping,
            opts.LogFormat,
            opts.RuleSetSourceUrl,
            opts.RuleSetRepository,
            opts.RuleSetVersion,
            opts.RuleSetRefreshMode,
            opts.RuleSetPath,
            opts.RuleSetRefreshIntervalMinutes,
            opts.MatchLedgerCapacity,
            opts.QualityProbing,
            opts.QualityCacheTtlMinutes,
            opts.QualityCacheCapacity,
            opts.QualityProbeLimit,
            prowlarr = maskedProwlarr,
            arrInstances = maskedArrInstances,
        });
    }

    private static async Task<IResult> HandlePutConfig(
        HttpContext context,
        ConfigFileWriter configFileWriter)
    {
        JsonObject? body;
        try
        {
            body = await context.Request.ReadFromJsonAsync<JsonObject>();
        }
        catch (JsonException ex)
        {
            return Results.Json(new { error = $"Invalid JSON: {ex.Message}" }, statusCode: 400);
        }

        if (body is null)
        {
            return Results.Json(new { error = "Request body is required" }, statusCode: 400);
        }

        var wrapper = new JsonObject
        {
            [FunkArrOptions.SectionName] = body,
        };

        configFileWriter.Write(wrapper);

        return Results.Json(new { success = true });
    }

    private static async Task<FfmpegResult> CheckFfmpeg()
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

            if (output is not null)
            {
                var match = Regex.Match(output, @"ffmpeg version (\S+)");
                var version = match.Success ? match.Groups[1].Value : "unknown";
                return new FfmpegResult(true, version);
            }

            return new FfmpegResult(false, null);
        }
        catch
        {
            return new FfmpegResult(false, null);
        }
    }

    private static bool TestWriteAccess(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            Directory.CreateDirectory(path);
            var testFile = Path.Combine(path, $".funkarr-write-test-{Guid.NewGuid():N}");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> CheckMediathek(IHttpClientFactory httpClientFactory)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://mediathekviewweb.de/");
            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> CheckProwlarr(ArrConnection? prowlarr, IHttpClientFactory httpClientFactory)
    {
        if (prowlarr is null || string.IsNullOrEmpty(prowlarr.Url))
            return false;

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"{prowlarr.Url.TrimEnd('/')}/api/v1/health";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", prowlarr.ApiKey);
            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<List<object>> CheckArrInstances(
        List<ArrInstanceConnection> instances,
        IHttpClientFactory httpClientFactory)
    {
        var results = new List<object>();

        foreach (var instance in instances)
        {
            var connected = false;
            try
            {
                using var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var url = $"{instance.Url.TrimEnd('/')}/api/v3/system/status";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Api-Key", instance.ApiKey);
                var response = await client.SendAsync(request);
                connected = response.IsSuccessStatusCode;
            }
            catch
            {
                // connection failed
            }

            results.Add(new { name = instance.Name, connected });
        }

        return results;
    }

    private static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return string.Empty;

        if (apiKey.Length <= 4)
            return new string('●', apiKey.Length);

        var masked = new string('●', apiKey.Length - 4);
        return masked + apiKey[^4..];
    }

    private sealed record FfmpegResult(bool Found, string? Version);

    private sealed record TestConnectionRequest(string? Url, string? ApiKey);

    private sealed record TestArrRequest(string? Url, string? ApiKey, string? Type);

    private sealed record TestPathsRequest(string? DownloadPath, string? TempPath);
}

public sealed class SetupApiKeyFilter(IOptions<FunkArrOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var apiKey = context.HttpContext.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || apiKey != options.Value.ApiKey)
        {
            return Results.Json(new { error = "Incorrect user credentials" }, statusCode: 401);
        }

        return await next(context);
    }
}
