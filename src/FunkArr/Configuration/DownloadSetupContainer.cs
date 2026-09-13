using FunkArr.Api;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Sabnzbd;
using FunkArr.Core;
using FunkArr.Download;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class DownloadSetupContainer : ApplicationSetupContainer<WebApplication>, IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DownloadOptions>()
            .Bind(configuration.GetSection(DownloadOptions.SectionName))
            .ValidateOnStart();

        services.AddDownloadServices();
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.MapDownloadsApi();
        app.MapNewznabApi();
        app.MapSabnzbdApi();
    }
}
