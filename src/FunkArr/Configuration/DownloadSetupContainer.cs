using FunkArr.Api;
using FunkArr.Api.HealthChecks;
using FunkArr.Core;
using FunkArr.Download;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class DownloadSetupContainer : ISetup<IServiceCollection>, ISetup<WebApplication>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services.AddSingleton<IValidateOptions<DownloadOptions>, DownloadOptionsValidator>();

        services
            .AddOptions<DownloadOptions>()
            .Bind(context.Configuration.GetSection(DownloadOptions.SectionName))
            .ValidateOnStart();

        services.AddDownloadServices(context.Configuration);

        services.AddHealthChecks()
            .AddCheck<FfmpegHealthCheck>("ffmpeg", failureStatus: HealthStatus.Unhealthy);
    }

    public void Setup(WebApplication app, SetupContext _) => app.MapDownloadsApi();
}
