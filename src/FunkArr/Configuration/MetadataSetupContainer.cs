using FunkArr.Core;
using FunkArr.Enrichment;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class MetadataSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<TvdbOptions>()
            .Bind(configuration.GetSection("FunkArr:Tvdb"));

        services
            .AddOptions<TmdbOptions>()
            .Bind(configuration.GetSection("FunkArr:Tmdb"));

        services.AddMemoryCache();

        services.AddHttpClient<TvdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api4.thetvdb.com/v4/");
        })
        .AddStandardResilienceHandler();

        services.AddHttpClient<TmdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<EpisodeEnricher>();
        services.AddSingleton<MovieEnricher>();
    }
}
