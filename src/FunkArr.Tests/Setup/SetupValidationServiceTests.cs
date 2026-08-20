using System.Net;
using FunkArr.Configuration;
using FunkArr.Health;
using FunkArr.Setup;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Setup;

public sealed class SetupValidationServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "funkarr-setup-test-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -- Self-checks ----------------------------------------------------

    [Fact]
    public async Task ValidateAsync_ApiKeyConfigured_ReportsPass()
    {
        var sut = CreateService(apiKey: "some-key");

        var result = await sut.ValidateAsync(new ValidationRequest(null, null, null), CancellationToken.None);

        var check = GetCheck(result, "api-key");
        Assert.Equal(CheckStatus.Pass, check.Status);
        Assert.Null(check.FixGuidance);
    }

    [Fact]
    public async Task ValidateAsync_ApiKeyMissing_ReportsFail()
    {
        var sut = CreateService(apiKey: "");

        var result = await sut.ValidateAsync(new ValidationRequest(null, null, null), CancellationToken.None);

        var check = GetCheck(result, "api-key");
        Assert.Equal(CheckStatus.Fail, check.Status);
        Assert.NotNull(check.FixGuidance);
    }

    [Fact]
    public async Task ValidateAsync_Ffmpeg_MatchesDirectHealthCheckMapping()
    {
        var sut = CreateService();
        var directHealth = await new FfmpegHealthCheck().CheckHealthAsync(new());

        var result = await sut.ValidateAsync(new ValidationRequest(null, null, null), CancellationToken.None);

        var check = GetCheck(result, "ffmpeg");
        var expectedStatus = directHealth.Status switch
        {
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => CheckStatus.Pass,
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => CheckStatus.Warning,
            _ => CheckStatus.Fail,
        };
        Assert.Equal(expectedStatus, check.Status);
    }

    [Fact]
    public async Task ValidateAsync_DownloadPathWritable_ReportsPass()
    {
        var sut = CreateService(downloadPath: Path.Combine(_tempDir, "downloads"));

        var result = await sut.ValidateAsync(new ValidationRequest(null, null, null), CancellationToken.None);

        Assert.Equal(CheckStatus.Pass, GetCheck(result, "download-path").Status);
    }

    [Fact]
    public async Task ValidateAsync_DownloadPathBlockedByFile_ReportsFail()
    {
        Directory.CreateDirectory(_tempDir);
        var blockerFile = Path.Combine(_tempDir, "blocker");
        File.WriteAllText(blockerFile, "x");

        var sut = CreateService(downloadPath: blockerFile);

        var result = await sut.ValidateAsync(new ValidationRequest(null, null, null), CancellationToken.None);

        var check = GetCheck(result, "download-path");
        Assert.Equal(CheckStatus.Fail, check.Status);
        Assert.NotNull(check.FixGuidance);
    }

    [Fact]
    public async Task ValidateAsync_TempPathIndependentOfDownloadPath()
    {
        Directory.CreateDirectory(_tempDir);
        var blockerFile = Path.Combine(_tempDir, "blocker");
        File.WriteAllText(blockerFile, "x");

        var sut = CreateService(
            downloadPath: Path.Combine(_tempDir, "downloads"),
            tempPath: blockerFile);

        var result = await sut.ValidateAsync(new ValidationRequest(null, null, null), CancellationToken.None);

        Assert.Equal(CheckStatus.Pass, GetCheck(result, "download-path").Status);
        Assert.Equal(CheckStatus.Fail, GetCheck(result, "temp-path").Status);
    }

    // -- Orchestration ----------------------------------------------------

    [Fact]
    public async Task ValidateAsync_NoArrSections_OnlyReturnsSelfChecks()
    {
        var sut = CreateService(downloadPath: Path.Combine(_tempDir, "d"), tempPath: Path.Combine(_tempDir, "t"));

        var result = await sut.ValidateAsync(new ValidationRequest(null, null, null), CancellationToken.None);

        Assert.Equal(4, result.Checks.Count);
        Assert.All(result.Checks, c => Assert.Equal("self", c.Category));
    }

    [Fact]
    public async Task ValidateAsync_ProwlarrUnreachable_ConnectivityFailsAndRegistrationSkipped()
    {
        var sut = CreateService(
            downloadPath: Path.Combine(_tempDir, "d"),
            tempPath: Path.Combine(_tempDir, "t"),
            responder: _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var request = new ValidationRequest(new ArrConnection { Url = "http://prowlarr:9696", ApiKey = "key" }, null, null);
        var result = await sut.ValidateAsync(request, CancellationToken.None);

        Assert.Equal(CheckStatus.Fail, GetCheck(result, "prowlarr-connectivity").Status);
        var registered = GetCheck(result, "prowlarr-registered");
        Assert.Equal(CheckStatus.Fail, registered.Status);
        Assert.Contains("skipped", registered.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_ProwlarrReachableAndRegistered_ReportsPass()
    {
        var indexerJson = """
        [ { "name": "FunkArr", "fields": [ { "name": "baseUrl", "value": "http://funkarr:9797" } ] } ]
        """;

        var sut = CreateService(
            downloadPath: Path.Combine(_tempDir, "d"),
            tempPath: Path.Combine(_tempDir, "t"),
            responder: req => req.RequestUri!.AbsolutePath.Contains("health")
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : FakeHttpMessageHandler.JsonResponse(indexerJson));

        var request = new ValidationRequest(
            new ArrConnection { Url = "http://prowlarr:9696", ApiKey = "key" }, null, "http://funkarr:9797");
        var result = await sut.ValidateAsync(request, CancellationToken.None);

        Assert.Equal(CheckStatus.Pass, GetCheck(result, "prowlarr-connectivity").Status);
        Assert.Equal(CheckStatus.Pass, GetCheck(result, "prowlarr-registered").Status);
    }

    [Fact]
    public async Task ValidateAsync_MultipleArrInstances_FailuresIsolated()
    {
        var sut = CreateService(
            downloadPath: Path.Combine(_tempDir, "d"),
            tempPath: Path.Combine(_tempDir, "t"),
            responder: req => req.RequestUri!.Host == "sonarr"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var request = new ValidationRequest(
            null,
            [
                new ArrInstanceConnection { Name = "Sonarr", Type = ArrType.Sonarr, Url = "http://sonarr:8989", ApiKey = "key" },
                new ArrInstanceConnection { Name = "Radarr", Type = ArrType.Radarr, Url = "http://radarr:7878", ApiKey = "key" },
            ],
            null);

        var result = await sut.ValidateAsync(request, CancellationToken.None);

        Assert.Equal(CheckStatus.Pass, GetCheck(result, "Sonarr-connectivity").Status);
        Assert.Equal(CheckStatus.Fail, GetCheck(result, "Radarr-connectivity").Status);
    }

    [Fact]
    public async Task ValidateAsync_MalformedRegistrationResponse_StillReturnsFullResultWithFailedCheck()
    {
        var sut = CreateService(
            downloadPath: Path.Combine(_tempDir, "d"),
            tempPath: Path.Combine(_tempDir, "t"),
            responder: req => req.RequestUri!.AbsolutePath.Contains("health")
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-json") });

        var request = new ValidationRequest(
            new ArrConnection { Url = "http://prowlarr:9696", ApiKey = "key" }, null, null);
        var result = await sut.ValidateAsync(request, CancellationToken.None);

        Assert.Equal(CheckStatus.Fail, GetCheck(result, "prowlarr-registered").Status);
        Assert.Equal(6, result.Checks.Count);
    }

    // -- OverallStatus derivation -----------------------------------------

    [Fact]
    public void ValidationResult_AllPass_OverallIsPass()
    {
        var result = ValidationResult.From([
            new CheckResult("self", "a", CheckStatus.Pass, "ok", null),
            new CheckResult("self", "b", CheckStatus.Pass, "ok", null),
        ]);

        Assert.Equal(CheckStatus.Pass, result.OverallStatus);
    }

    [Fact]
    public void ValidationResult_AnyWarningNoFail_OverallIsWarning()
    {
        var result = ValidationResult.From([
            new CheckResult("self", "a", CheckStatus.Pass, "ok", null),
            new CheckResult("self", "b", CheckStatus.Warning, "meh", "fix"),
        ]);

        Assert.Equal(CheckStatus.Warning, result.OverallStatus);
    }

    [Fact]
    public void ValidationResult_AnyFail_OverallIsFail()
    {
        var result = ValidationResult.From([
            new CheckResult("self", "a", CheckStatus.Warning, "meh", "fix"),
            new CheckResult("self", "b", CheckStatus.Fail, "broken", "fix"),
        ]);

        Assert.Equal(CheckStatus.Fail, result.OverallStatus);
    }

    // -- Helpers ------------------------------------------------------------

    private static CheckResult GetCheck(ValidationResult result, string name) =>
        result.Checks.Single(c => c.Name == name);

    private SetupValidationService CreateService(
        string apiKey = "test-key",
        string? downloadPath = null,
        string? tempPath = null,
        Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        var funkArrOptions = Options.Create(new FunkArrOptions { ApiKey = apiKey });
        var downloadOptions = Options.Create(new DownloadOptions
        {
            DownloadPath = downloadPath ?? Path.Combine(_tempDir, "downloads"),
            TempPath = tempPath ?? Path.Combine(_tempDir, "temp"),
        });

        var handler = new FakeHttpMessageHandler(
            responder ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var services = new ServiceCollection();
        services.AddHttpClient(nameof(SetupValidationService))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        return new SetupValidationService(funkArrOptions, downloadOptions, new FfmpegHealthCheck(), factory);
    }
}
