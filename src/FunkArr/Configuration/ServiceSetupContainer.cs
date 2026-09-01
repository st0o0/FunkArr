using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class ServiceSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FunkArrOptions>()
            .Bind(configuration.GetSection(FunkArrOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<MatchHistoryOptions>()
            .Bind(configuration.GetSection(MatchHistoryOptions.SectionName))
            .ValidateOnStart();

        services.AddHealthChecks();

        services.AddHttpClient("MediathekViewWeb", client =>
        {
            client.BaseAddress = new Uri("https://mediathekviewweb.de/api/query");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
    }
}
