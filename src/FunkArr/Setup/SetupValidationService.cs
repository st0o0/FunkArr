using FunkArr.Configuration;
using FunkArr.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FunkArr.Setup;

public sealed class SetupValidationService(
    IOptions<FunkArrOptions> options,
    IOptions<DownloadOptions> downloadOptions,
    FfmpegHealthCheck ffmpegHealthCheck,
    IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan ExternalCallTimeout = TimeSpan.FromSeconds(10);

    public async Task<ValidationResult> ValidateAsync(
        ValidationRequest request, CancellationToken cancellationToken)
    {
        var checkTasks = new List<Task<CheckResult>>
        {
            RunSafely(() => CheckApiKeyAsync(), "self", "api-key"),
            RunSafely(() => CheckFfmpegAsync(cancellationToken), "self", "ffmpeg"),
            RunSafely(() => CheckPathAsync("download-path", downloadOptions.Value.DownloadPath), "self", "download-path"),
            RunSafely(() => CheckPathAsync("temp-path", downloadOptions.Value.TempPath), "self", "temp-path"),
        };

        if (request.Prowlarr is not null && !string.IsNullOrWhiteSpace(request.Prowlarr.Url))
        {
            checkTasks.AddRange(BuildProwlarrChecks(request.Prowlarr, request.SelfUrl, cancellationToken));
        }

        foreach (var instance in request.ArrInstances ?? [])
        {
            checkTasks.AddRange(BuildArrInstanceChecks(instance, request.SelfUrl, cancellationToken));
        }

        var results = await Task.WhenAll(checkTasks);

        return ValidationResult.From(results);
    }

    private static Task<CheckResult> RunSafely(
        Func<Task<CheckResult>> check, string category, string name) =>
        RunSafelyCore(check, category, name);

    private static async Task<CheckResult> RunSafelyCore(
        Func<Task<CheckResult>> check, string category, string name)
    {
        try
        {
            return await check();
        }
        catch (Exception ex)
        {
            return new CheckResult(
                category, name, CheckStatus.Fail,
                $"Check failed unexpectedly: {ex.Message}",
                "Check the FunkArr logs for details and retry.");
        }
    }

    private Task<CheckResult> CheckApiKeyAsync()
    {
        var result = string.IsNullOrWhiteSpace(options.Value.ApiKey)
            ? new CheckResult(
                "self", "api-key", CheckStatus.Fail,
                "FunkArr's API key is not configured.",
                "Set the FunkArr__ApiKey environment variable (or ApiKey in appsettings) to a non-empty value.")
            : new CheckResult(
                "self", "api-key", CheckStatus.Pass,
                "API key is configured.",
                null);

        return Task.FromResult(result);
    }

    private async Task<CheckResult> CheckFfmpegAsync(CancellationToken cancellationToken)
    {
        var health = await ffmpegHealthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);
        var status = health.Status switch
        {
            HealthStatus.Healthy => CheckStatus.Pass,
            HealthStatus.Degraded => CheckStatus.Warning,
            _ => CheckStatus.Fail,
        };

        var fixGuidance = status == CheckStatus.Pass
            ? null
            : "Install FFmpeg and ensure it is on PATH, or verify the container image includes it.";

        return new CheckResult("self", "ffmpeg", status, health.Description ?? "FFmpeg check completed.", fixGuidance);
    }

    private static Task<CheckResult> CheckPathAsync(string name, string? path)
    {
        var result = TryWriteAccess(path)
            ? new CheckResult(
                "self", name, CheckStatus.Pass,
                $"Path '{path}' exists and is writable.",
                null)
            : new CheckResult(
                "self", name, CheckStatus.Fail,
                $"Path '{path}' could not be created or written to.",
                "Ensure the configured path exists and the FunkArr process has read/write permissions (check the volume mount if running in Docker).");

        return Task.FromResult(result);
    }

    private static bool TryWriteAccess(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

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

    private IEnumerable<Task<CheckResult>> BuildProwlarrChecks(
        ArrConnection prowlarr, string? selfUrl, CancellationToken cancellationToken)
    {
        var connectivityTask = RunSafely(
            () => CheckHttpConnectivityAsync(
                "prowlarr", "prowlarr-connectivity",
                $"{prowlarr.Url.TrimEnd('/')}/api/v1/health", prowlarr.ApiKey,
                "Verify the Prowlarr URL and API key.", cancellationToken),
            "prowlarr", "prowlarr-connectivity");

        yield return connectivityTask;
        yield return RunSafely(() => CheckRegistrationAsync(
            connectivityTask,
            () => CreateClient(prowlarr.ApiKey),
            client => ArrRegistrationChecker.CheckProwlarrRegisteredAsync(client, prowlarr.Url, selfUrl, cancellationToken),
            "prowlarr", "prowlarr-registered", "Prowlarr"),
            "prowlarr", "prowlarr-registered");
    }

    private IEnumerable<Task<CheckResult>> BuildArrInstanceChecks(
        ArrInstanceConnection instance, string? selfUrl, CancellationToken cancellationToken)
    {
        var category = instance.Type.ToString().ToLowerInvariant();
        var connectivityName = $"{instance.Name}-connectivity";
        var registeredName = $"{instance.Name}-registered";

        var connectivityTask = RunSafely(
            () => CheckHttpConnectivityAsync(
                category, connectivityName,
                $"{instance.Url.TrimEnd('/')}/api/v3/system/status", instance.ApiKey,
                $"Verify the {instance.Name} URL and API key.", cancellationToken),
            category, connectivityName);

        yield return connectivityTask;
        yield return RunSafely(() => CheckRegistrationAsync(
            connectivityTask,
            () => CreateClient(instance.ApiKey),
            client => ArrRegistrationChecker.CheckArrDownloadClientRegisteredAsync(client, instance, selfUrl, cancellationToken),
            category, registeredName, instance.Name),
            category, registeredName);
    }

    private async Task<CheckResult> CheckHttpConnectivityAsync(
        string category, string name, string url, string apiKey, string fixGuidance, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateClient(apiKey);
            using var response = await client.GetAsync(url, cancellationToken);

            return response.IsSuccessStatusCode
                ? new CheckResult(category, name, CheckStatus.Pass, "Reachable.", null)
                : new CheckResult(
                    category, name, CheckStatus.Fail,
                    $"Received HTTP {(int)response.StatusCode}.", fixGuidance);
        }
        catch (Exception ex)
        {
            return new CheckResult(category, name, CheckStatus.Fail, $"Could not connect: {ex.Message}", fixGuidance);
        }
    }

    private static async Task<CheckResult> CheckRegistrationAsync(
        Task<CheckResult> connectivityTask,
        Func<HttpClient> createClient,
        Func<HttpClient, Task<CheckResult>> registrationCheck,
        string category,
        string name,
        string appLabel)
    {
        var connectivity = await connectivityTask;
        if (connectivity.Status != CheckStatus.Pass)
        {
            return new CheckResult(
                category, name, CheckStatus.Fail,
                $"Skipped: could not connect to {appLabel}.",
                null);
        }

        using var client = createClient();
        return await registrationCheck(client);
    }

    private HttpClient CreateClient(string apiKey)
    {
        var client = httpClientFactory.CreateClient(nameof(SetupValidationService));
        client.Timeout = ExternalCallTimeout;
        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }
}
