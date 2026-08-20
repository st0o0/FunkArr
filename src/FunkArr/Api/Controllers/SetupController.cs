using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Asp.Versioning;
using FunkArr.Api.Models;
using FunkArr.Configuration;
using FunkArr.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Tags("Setup & Config")]
public sealed class SetupController(
    IOptions<FunkArrOptions> options,
    IOptions<DownloadOptions> downloadOptions,
    IOptions<RuleSetOptions> ruleSetOptions,
    IOptions<QualityOptions> qualityOptions,
    IOptions<SearchOptions> searchOptions,
    IHttpClientFactory httpClientFactory,
    ConfigFileWriter configFileWriter,
    SetupValidationService validationService) : ControllerBase
{
    [HttpGet("api/v{version:apiVersion}/setup/status")]
    [ProducesResponseType<StatusResponse>(200)]
    public async Task<ActionResult<StatusResponse>> GetStatus()
    {
        var opts = options.Value;
        var download = downloadOptions.Value;

        var ffmpegTask = CheckFfmpeg();
        var pathsTask = Task.Run(() => (DownloadOk: TestWriteAccess(download.DownloadPath), TempOk: TestWriteAccess(download.TempPath)));
        var mediathekTask = CheckMediathek();
        var prowlarrTask = CheckProwlarr(opts.Prowlarr);
        var arrTask = CheckArrInstances(opts.ArrInstances);

        await Task.WhenAll(ffmpegTask, pathsTask, mediathekTask, prowlarrTask, arrTask);

        var ffmpeg = await ffmpegTask;
        var paths = await pathsTask;
        var mediathek = await mediathekTask;
        var prowlarr = await prowlarrTask;
        var arrInstances = await arrTask;

        var configured = ffmpeg.Found
                         && paths is { DownloadOk: true, TempOk: true }
                         && mediathek
                         && !string.IsNullOrEmpty(opts.ApiKey);

        return Ok(new StatusResponse(
            configured,
            new FfmpegStatus(ffmpeg.Found, ffmpeg.Version),
            new PathsStatus(paths.DownloadOk, paths.TempOk),
            new MediathekStatus(mediathek),
            new RulesetsStatus(0),
            new ProwlarrStatus(prowlarr),
            arrInstances.Select(a => new ArrInstanceStatus(a.Name, a.Connected)).ToList()));
    }

    [HttpPost("api/v{version:apiVersion}/setup/test-prowlarr")]
    [ProducesResponseType<TestConnectionResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestProwlarr([FromBody] Models.TestConnectionRequest body)
    {
        if (string.IsNullOrEmpty(body.Url) || string.IsNullOrEmpty(body.ApiKey))
        {
            return BadRequest(new TestConnectionResponse(false, Error: "url and apiKey are required"));
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"{body.Url.TrimEnd('/')}/api/v1/health";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", body.ApiKey);
            var response = await client.SendAsync(request);

            return Ok(new TestConnectionResponse(response.IsSuccessStatusCode, (int)response.StatusCode));
        }
        catch (Exception ex)
        {
            return Ok(new TestConnectionResponse(false, Error: ex.Message));
        }
    }

    [HttpPost("api/v{version:apiVersion}/setup/test-arr")]
    [ProducesResponseType<TestConnectionResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestArr([FromBody] Models.TestArrRequest body)
    {
        if (string.IsNullOrEmpty(body.Url) || string.IsNullOrEmpty(body.ApiKey))
        {
            return BadRequest(new TestConnectionResponse(false, Error: "url, apiKey, and type are required"));
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"{body.Url.TrimEnd('/')}/api/v3/system/status";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", body.ApiKey);
            var response = await client.SendAsync(request);

            return Ok(new TestConnectionResponse(response.IsSuccessStatusCode, (int)response.StatusCode));
        }
        catch (Exception ex)
        {
            return Ok(new TestConnectionResponse(false, Error: ex.Message));
        }
    }

    [HttpPost("api/v{version:apiVersion}/setup/test-paths")]
    [ProducesResponseType<TestPathsResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestPaths([FromBody] Models.TestPathsRequest body)
    {
        if (body is null)
        {
            return BadRequest(new ErrorResponse("downloadPath and tempPath are required"));
        }

        var downloadOk = TestWriteAccess(body.DownloadPath);
        var tempOk = TestWriteAccess(body.TempPath);

        return Ok(new TestPathsResponse(downloadOk, tempOk));
    }

    [HttpPost("api/v{version:apiVersion}/setup/test-ffmpeg")]
    [ProducesResponseType<FfmpegResponse>(200)]
    public async Task<ActionResult<FfmpegResponse>> TestFfmpeg()
    {
        var result = await CheckFfmpeg();
        return Ok(new FfmpegResponse(result.Found, result.Version));
    }

    [HttpPost("api/v{version:apiVersion}/setup/validate")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Validate(CancellationToken cancellationToken)
    {
        ValidationRequest request;
        if (Request.ContentLength is null or 0)
        {
            request = new ValidationRequest(null, null, null);
        }
        else
        {
            request = await Request.ReadFromJsonAsync<ValidationRequest>(SetupValidationJsonOptions.Default, cancellationToken)
                      ?? new ValidationRequest(null, null, null);
        }

        var result = await validationService.ValidateAsync(request, cancellationToken);
        return new JsonResult(result, SetupValidationJsonOptions.Default);
    }

    [HttpPost("api/v{version:apiVersion}/setup/test-mediathek")]
    [ProducesResponseType<MediathekResponse>(200)]
    public async Task<ActionResult<MediathekResponse>> TestMediathek()
    {
        var reachable = await CheckMediathek();
        return Ok(new MediathekResponse(reachable));
    }

    [HttpGet("api/v{version:apiVersion}/config")]
    [ProducesResponseType<ConfigResponse>(200)]
    public ActionResult<ConfigResponse> GetConfig()
    {
        var opts = options.Value;
        var download = downloadOptions.Value;
        var ruleSet = ruleSetOptions.Value;
        var quality = qualityOptions.Value;
        var search = searchOptions.Value;

        var maskedArrInstances = opts.ArrInstances.Select(a => new ArrInstanceConfig(
            a.Name, a.Type.ToString(), a.Url, MaskApiKey(a.ApiKey))).ToList();

        var maskedProwlarr = opts.Prowlarr is not null
            ? new ProwlarrConfig(opts.Prowlarr.Url, MaskApiKey(opts.Prowlarr.ApiKey))
            : null;

        return Ok(new ConfigResponse(
            opts.ApiKey,
            download.DownloadPath,
            download.TempPath,
            opts.PersistencePath,
            download.ConcurrentDownloads,
            download.PathMapping,
            opts.LogFormat,
            ruleSet.Repository,
            ruleSet.Version,
            ruleSet.Path,
            ruleSet.RefreshIntervalMinutes,
            opts.MatchLedgerCapacity,
            quality.Probing,
            quality.CacheTtlMinutes,
            quality.CacheCapacity,
            search.QualityProbeLimit,
            maskedProwlarr,
            maskedArrInstances));
    }

    [HttpPut("api/v{version:apiVersion}/config")]
    [ProducesResponseType<SuccessResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> PutConfig()
    {
        JsonObject? body;
        try
        {
            body = await Request.ReadFromJsonAsync<JsonObject>();
        }
        catch (JsonException ex)
        {
            return BadRequest(new ErrorResponse($"Invalid JSON: {ex.Message}"));
        }

        if (body is null)
        {
            return BadRequest(new ErrorResponse("Request body is required"));
        }

        var wrapper = new JsonObject
        {
            [FunkArrOptions.SectionName] = body,
        };

        configFileWriter.Write(wrapper);

        return Ok(new SuccessResponse(true));
    }

    private async Task<FfmpegResult> CheckFfmpeg()
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
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(path);
            var testFile = Path.Combine(path, $".funkarr-write-test-{Guid.NewGuid():N}");
            System.IO.File.WriteAllText(testFile, "test");
            System.IO.File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckMediathek()
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

    private async Task<bool> CheckProwlarr(ArrConnection? prowlarr)
    {
        if (prowlarr is null || string.IsNullOrEmpty(prowlarr.Url))
        {
            return false;
        }

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

    private async Task<List<ArrInstanceResult>> CheckArrInstances(List<ArrInstanceConnection> instances)
    {
        var results = new List<ArrInstanceResult>();

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
            }

            results.Add(new ArrInstanceResult(instance.Name, connected));
        }

        return results;
    }

    private static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return string.Empty;
        }

        if (apiKey.Length <= 4)
        {
            return new string('●', apiKey.Length);
        }

        var masked = new string('●', apiKey.Length - 4);
        return masked + apiKey[^4..];
    }

    private sealed record FfmpegResult(bool Found, string? Version);

    private sealed record ArrInstanceResult(string Name, bool Connected);
}
