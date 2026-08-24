using System.Diagnostics;
using System.Text.RegularExpressions;
using FunkArr.Configuration;
using FunkArr.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Contracts = FunkArr.Api.Contracts;

namespace FunkArr.Api.Controllers;

[ApiController]
[Route("api/v1/setup")]
[Tags("Setup")]
public sealed class SetupController(
    IOptions<FunkArrOptions> options,
    IOptions<DownloadOptions> downloadOptions,
    IHttpClientFactory httpClientFactory,
    SetupValidationService validationService) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<Contracts.StatusResponse>(200)]
    public async Task<ActionResult<Contracts.StatusResponse>> GetStatus()
    {
        var opts = options.Value;
        var download = downloadOptions.Value;

        var ffmpegTask = CheckFfmpeg();
        var pathsTask = Task.Run(() => (DownloadOk: TestWriteAccess(download.Path), TempOk: TestWriteAccess(download.TempPath)));
        var mediathekTask = CheckMediathek();

        await Task.WhenAll(ffmpegTask, pathsTask, mediathekTask);

        var ffmpeg = await ffmpegTask;
        var paths = await pathsTask;
        var mediathek = await mediathekTask;

        var configured = ffmpeg.Found
                         && paths is { DownloadOk: true, TempOk: true }
                         && mediathek
                         && !string.IsNullOrEmpty(opts.ApiKey);

        return Ok(new Contracts.StatusResponse(
            opts.ApiKey ?? string.Empty,
            configured,
            new Contracts.FfmpegStatus(ffmpeg.Found, ffmpeg.Version),
            new Contracts.MediathekStatus(mediathek),
            new Contracts.PathsStatus(paths.DownloadOk, paths.TempOk),
            new Contracts.RulesetsStatus(0)));
    }

    [HttpPost("test-prowlarr")]
    [ProducesResponseType<Contracts.TestConnectionResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestProwlarr([FromBody] Contracts.TestConnectionRequest body)
    {
        if (string.IsNullOrEmpty(body.Url) || string.IsNullOrEmpty(body.ApiKey))
        {
            return BadRequest(new Contracts.TestConnectionResponse(error: "url and apiKey are required", statusCode: 0, success: false));
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"{body.Url.TrimEnd('/')}/api/v1/health";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", body.ApiKey);
            var response = await client.SendAsync(request);

            return Ok(new Contracts.TestConnectionResponse(error: null, statusCode: (int)response.StatusCode, success: response.IsSuccessStatusCode));
        }
        catch (Exception ex)
        {
            return Ok(new Contracts.TestConnectionResponse(error: ex.Message, statusCode: 0, success: false));
        }
    }

    [HttpPost("test-arr")]
    [ProducesResponseType<Contracts.TestConnectionResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestArr([FromBody] Contracts.TestArrRequest body)
    {
        if (string.IsNullOrEmpty(body.Url) || string.IsNullOrEmpty(body.ApiKey))
        {
            return BadRequest(new Contracts.TestConnectionResponse(error: "url, apiKey, and type are required", statusCode: 0, success: false));
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var url = $"{body.Url.TrimEnd('/')}/api/v3/system/status";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", body.ApiKey);
            var response = await client.SendAsync(request);

            return Ok(new Contracts.TestConnectionResponse(error: null, statusCode: (int)response.StatusCode, success: response.IsSuccessStatusCode));
        }
        catch (Exception ex)
        {
            return Ok(new Contracts.TestConnectionResponse(error: ex.Message, statusCode: 0, success: false));
        }
    }

    [HttpPost("test-paths")]
    [ProducesResponseType<Contracts.TestPathsResponse>(200)]
    [ProducesResponseType(400)]
    public ActionResult<Contracts.TestPathsResponse> TestPaths([FromBody] Contracts.TestPathsRequest body)
    {
        var downloadOk = TestWriteAccess(body.DownloadPath);
        var tempOk = TestWriteAccess(body.TempPath);

        return Ok(new Contracts.TestPathsResponse(downloadOk, tempOk));
    }

    [HttpPost("test-ffmpeg")]
    [ProducesResponseType<Contracts.FfmpegResponse>(200)]
    public async Task<ActionResult<Contracts.FfmpegResponse>> TestFfmpeg()
    {
        var result = await CheckFfmpeg();
        return Ok(new Contracts.FfmpegResponse(result.Found, result.Version));
    }

    [HttpPost("validate")]
    [ProducesResponseType<Contracts.ValidationResult>(200)]
    public async Task<ActionResult<Contracts.ValidationResult>> Validate(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ValidationRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ValidationRequest(null, null, null);
        var result = await validationService.ValidateAsync(request, cancellationToken);

        var checks = result.Checks.Select(c => new Contracts.CheckResult(
            c.Category, c.FixGuidance, c.Message, c.Name,
            Enum.Parse<Contracts.CheckResultStatus>(c.Status.ToString(), ignoreCase: true))).ToList();

        var overallStatus = Enum.Parse<Contracts.ValidationResultOverallStatus>(
            result.OverallStatus.ToString(), ignoreCase: true);

        return Ok(new Contracts.ValidationResult(checks, overallStatus));
    }

    [HttpPost("test-mediathek")]
    [ProducesResponseType<Contracts.MediathekResponse>(200)]
    public async Task<ActionResult<Contracts.MediathekResponse>> TestMediathek()
    {
        var reachable = await CheckMediathek();
        return Ok(new Contracts.MediathekResponse(reachable));
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

    private sealed record FfmpegResult(bool Found, string? Version);
}
