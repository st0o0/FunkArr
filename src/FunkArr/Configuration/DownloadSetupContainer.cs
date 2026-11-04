using FunkArr.Api;
using FunkArr.Api.HealthChecks;
using FunkArr.Core;
using FunkArr.Core.Configuration;
using FunkArr.Download;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class DownloadSetupContainer : ISetup<IServiceCollection>, ISetup<WebApplication>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services
            .AddOptionsWithValidateOnStart<DownloadOptions, DownloadOptionsValidator>()
            .Bind(context.Configuration.GetSection(DownloadOptions.SectionName));

        services.AddSingleton<IRouteResolver, RouteResolver>();
        services.AddDownloadServices();

        services.AddHealthChecks()
            .AddCheck<FfmpegHealthCheck>("ffmpeg", failureStatus: HealthStatus.Unhealthy);
    }

    public void Setup(WebApplication app, SetupContext _) => app.MapDownloadsApi();
}
