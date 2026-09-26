using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunkArr.Configuration;

internal static class ExternalApiMetrics
{
    internal static readonly Meter Meter = new("FunkArr.ExternalApi");

    internal static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "funkarr.external_api.requests_total", description: "External API requests by API and status code class");

    internal static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "funkarr.external_api.duration_seconds", "s", "External API request duration");
}

internal sealed class ExternalApiMetricsHandler(string apiName) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            var statusClass = ((int)response.StatusCode / 100) + "xx";
            ExternalApiMetrics.Requests.Add(1,
                new KeyValuePair<string, object?>("api", apiName),
                new KeyValuePair<string, object?>("status_code", statusClass));
            ExternalApiMetrics.Duration.Record(sw.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("api", apiName));

            return response;
        }
        catch (Exception) when (sw.IsRunning)
        {
            sw.Stop();
            ExternalApiMetrics.Requests.Add(1,
                new KeyValuePair<string, object?>("api", apiName),
                new KeyValuePair<string, object?>("status_code", "error"));
            ExternalApiMetrics.Duration.Record(sw.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("api", apiName));
            throw;
        }
    }
}
