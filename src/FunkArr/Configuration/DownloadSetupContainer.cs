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
        services.AddSingleton<IValidateOptions<RoutingOptions>, RoutingOptionsValidator>();

        services
            .AddOptions<DownloadOptions>()
            .Bind(configuration.GetSection(DownloadOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<RoutingOptions>()
            .Bind(configuration.GetSection(RoutingOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IRouteResolver, RouteResolver>();

        services.AddDownloadServices(configuration);
        services.AddArrApiServices(configuration);
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.MapDownloadsApi();
    }
}
