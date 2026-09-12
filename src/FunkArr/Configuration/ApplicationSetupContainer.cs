using FunkArr.Api;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class ApplicationSetupContainer : ApplicationSetupContainer<WebApplication>
{
    protected override void SetupApplication(WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "FunkArr API";
            options.ForceThemeMode = ThemeMode.Dark;
            options.TagSorter = TagSorter.Alpha;
            options.DefaultOpenAllTags = true;
            options.HideModels = true;
            options.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        app.UseStaticFiles();
        app.UseOutputCache();

        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = 200,
                [HealthStatus.Degraded] = 200,
                [HealthStatus.Unhealthy] = 503,
            },
        });
        app.MapGet("/alive", () => Results.Ok("Alive"))
            .WithTags("Health")
            .WithSummary("Liveness probe");

        app.MapSystemApi();

        app.MapFallbackToFile("index.html");
    }
}
