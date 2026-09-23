using FunkArr.Api;
using FunkArr.ArrApi;
using FunkArr.Core;
using FunkArr.Download;
using Microsoft.Extensions.Options;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class DownloadSetupContainer : ApplicationSetupContainer<WebApplication>, IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<DownloadOptions>, DownloadOptionsValidator>();

        services
            .AddOptions<DownloadOptions>()
            .Bind(configuration.GetSection(DownloadOptions.SectionName))
            .ValidateOnStart();

        services.AddDownloadServices();
        services.AddArrApiServices(configuration);
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.MapDownloadsApi();
    }
}
