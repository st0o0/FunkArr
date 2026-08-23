using System.Diagnostics;
using System.Text.RegularExpressions;
using FunkArr.Api.Models;
using FunkArr.Configuration;
using FunkArr.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    [ProducesResponseType<StatusResponse>(200)]
    public async Task<ActionResult<StatusResponse>> GetStatus()
    {
        var opts = options.Value;
        var download = downloadOptions.Value;

        var ffmpegTask = CheckFfmpeg();
        var pathsTask = Task.Run(() => (DownloadOk: TestWriteAccess(download.DownloadPath), TempOk: TestWriteAccess(download.TempPath)));
        var mediathekTask = CheckMediathek();

        await Task.WhenAll(ffmpegTask, pathsTask, mediathekTask);

        var ffmpeg = await ffmpegTask;
        var paths = await pathsTask;
        var mediathek = await mediathekTask;

        var configured = ffmpeg.Found
                         && paths is { DownloadOk: true, TempOk: true }
                         && mediathek
                         && !string.IsNullOrEmpty(opts.ApiKey);

        return Ok(new StatusResponse(
            configured,
            opts.ApiKey ?? string.Empty,
            new FfmpegStatus(ffmpeg.Found, ffmpeg.Version),
            new PathsStatus(paths.DownloadOk, paths.TempOk),
            new MediathekStatus(mediathek),
            new RulesetsStatus(0)));
    }

    [HttpPost("test-prowlarr")]
    [ProducesResponseType<TestConnectionResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestProwlarr([FromBody] TestConnectionRequest body)
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

    [HttpPost("test-arr")]
    [ProducesResponseType<TestConnectionResponse>(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestArr([FromBody] TestArrRequest body)
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

    [HttpPost("test-paths")]
    [ProducesResponseType<TestPathsResponse>(200)]
    [ProducesResponseType(400)]
    public ActionResult<TestPathsResponse> TestPaths([FromBody] TestPathsRequest body)
    {
        var downloadOk = TestWriteAccess(body.DownloadPath);
        var tempOk = TestWriteAccess(body.TempPath);

        return Ok(new TestPathsResponse(downloadOk, tempOk));
    }

    [HttpPost("test-ffmpeg")]
    [ProducesResponseType<FfmpegResponse>(200)]
    public async Task<ActionResult<FfmpegResponse>> TestFfmpeg()
    {
        var result = await CheckFfmpeg();
        return Ok(new FfmpegResponse(result.Found, result.Version));
    }

    [HttpPost("validate")]
    [ProducesResponseType<ValidationResult>(200)]
    public async Task<ActionResult<ValidationResult>> Validate(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ValidationRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ValidationRequest(null, null, null);
        var result = await validationService.ValidateAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("test-mediathek")]
    [ProducesResponseType<MediathekResponse>(200)]
    public async Task<ActionResult<MediathekResponse>> TestMediathek()
    {
        var reachable = await CheckMediathek();
        return Ok(new MediathekResponse(reachable));
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
