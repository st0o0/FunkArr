using System.Net.Mime;
using FunkArr.Api;
using FunkArr.Core;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class MediathekSetupContainer : ApplicationSetupContainer<WebApplication>, IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(HttpClientNames.MediathekViewWeb, client =>
        {
            client.BaseAddress = new Uri("https://mediathekviewweb.de/api/query");
            client.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
        });
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.MapMediathekApi();
    }
}
