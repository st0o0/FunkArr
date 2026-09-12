using FunkArr.Api;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class MediathekSetupContainer : ApplicationSetupContainer<WebApplication>, IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("MediathekViewWeb", client =>
        {
            client.BaseAddress = new Uri("https://mediathekviewweb.de/api/query");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
    }

    protected override void SetupApplication(WebApplication app)
    {
        app.MapMediathekApi();
    }
}
