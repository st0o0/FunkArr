using FunkArr.Api;
using FunkArr.Enrichment;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class ApplicationSetupContainer : ISetup<WebApplication>
{
    public void Setup(WebApplication app, SetupContext _)
    {
        Telemetry.SetCache(app.Services.GetRequiredService<IMemoryCache>());
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "FunkArr API";
            options.ForceThemeMode = ThemeMode.Dark;
            options.TagSorter = TagSorter.Alpha;
            options.DefaultOpenAllTags = true;
            options.HideModels = true;
            options.DefaultHttpClient =
                new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        app.UseForwardedHeaders();
        app.UseStatusCodePages();
        app.UseHttpLogging();
        app.UseRequestTimeouts();
        app.UseStaticFiles();
        app.UseOutputCache();

        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = 200,
                [HealthStatus.Degraded] = 200,
                [HealthStatus.Unhealthy] = 503
            }
        }).WithRequestTimeout(TimeSpan.FromSeconds(5));
        app.MapGet("/alive", () => Results.Ok("Alive"))
            .WithTags("Health")
            .WithSummary("Liveness probe")
            .WithRequestTimeout(TimeSpan.FromSeconds(5));

        app.MapSystemApi();
        app.MapFeedApi();
        app.MapSetupApi();
        app.MapControllers();

        app.MapPrometheusScrapingEndpoint("/metrics");

        app.Map($"/api/{{**rest}}", (string rest) => Results.NotFound(rest)).ExcludeFromDescription();
        app.MapFallbackToFile("index.html");
    }
}
