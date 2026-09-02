using System.Diagnostics;
using FunkArr.Api.Models;
using FunkArr.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FunkArr.Api;

public static class SetupApiEndpoints
{
    private const string _defaultApiKey = "funkarr-default-api-key";
    private static readonly TimeSpan _httpTimeout = TimeSpan.FromSeconds(3);

    public static WebApplication MapSetupApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/health");

        group.MapGet("/setup", async (
            IOptionsMonitor<FunkArrOptions> options,
            IHttpClientFactory httpClientFactory) =>
        {
            var opts = options.CurrentValue;

            var apiKeyCheck = CheckApiKey(opts);
            var mediathekTask = CheckMediathekViewWeb(httpClientFactory);
            var dataTask = CheckDirectory("dataDirectory", opts.DataPath);
            var downloadTask = CheckDirectory("downloadDirectory", opts.DownloadPath);
            var indexerTask = CheckSelfEndpoint(httpClientFactory, opts, "/index/api?t=caps&apikey=");
            var downloadApiTask = CheckSelfEndpoint(httpClientFactory, opts, "/download/api?mode=version&apikey=");
            var ffmpegTask = CheckFfmpeg();

            await Task.WhenAll(mediathekTask, dataTask, downloadTask, indexerTask, downloadApiTask, ffmpegTask);

            var checks = new Dictionary<string, CheckResult>
            {
                ["apiKey"] = apiKeyCheck,
                ["mediathekViewWeb"] = await mediathekTask,
                ["dataDirectory"] = await dataTask,
                ["downloadDirectory"] = await downloadTask,
                ["indexerApi"] = await indexerTask,
                ["downloadApi"] = await downloadApiTask,
                ["ffmpeg"] = await ffmpegTask,
            };

            var connectionInfo = new SetupConnectionInfo(
                IndexerApiPath: "/index/api",
                DownloadApiPath: "/download/api",
                DefaultPort: 5000);

            return Results.Ok(new SetupHealthCheck(checks, connectionInfo));
        });

        return app;
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

    internal static async Task<CheckResult> CheckDirectory(string name, string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);

            if (!Directory.Exists(fullPath))
            {
                return new CheckResult("fail", $"Directory does not exist: {fullPath}", Path: fullPath);
            }

            var testFile = Path.Combine(fullPath, $".funkarr-write-test-{Guid.NewGuid():N}");
            await File.WriteAllTextAsync(testFile, "test");
            File.Delete(testFile);

            return new CheckResult("ok", Path: fullPath);
        }
        catch (Exception ex)
        {
            return new CheckResult("fail", $"Directory not writable: {ex.Message}", Path: Path.GetFullPath(path));
        }
    }

    private static async Task<CheckResult> CheckSelfEndpoint(
        IHttpClientFactory factory, FunkArrOptions options, string pathWithParam)
    {
        try
        {
            using var client = factory.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5000");
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
