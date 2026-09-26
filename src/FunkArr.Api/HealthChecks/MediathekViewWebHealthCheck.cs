using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FunkArr.Api.HealthChecks;

public sealed class MediathekViewWebHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var (reachable, message) = await ProbeAsync(httpClientFactory, cancellationToken);

        return reachable
            ? HealthCheckResult.Healthy()
            : new HealthCheckResult(context.Registration.FailureStatus, message);
    }

    internal static async Task<(bool Reachable, string? Message)> ProbeAsync(
        IHttpClientFactory factory, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = factory.CreateClient();
            client.Timeout = _timeout;
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://mediathekviewweb.de/");
            using var response = await client.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, $"MediathekViewWeb returned HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, $"MediathekViewWeb unreachable: {ex.Message}");
        }
    }
}
