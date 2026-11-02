using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Sabnzbd;
using FunkArr.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArrApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ArrApiOptions>()
            .Bind(configuration.GetSection(ArrApiOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<SearchResultCache>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ArrApiOptions>>().Value;
            return new SearchResultCache(
                TimeSpan.FromSeconds(options.SearchCacheTtlSeconds),
                TimeProvider.System);
        });

        services.AddOutputCache(NewznabFeedCache.AddPolicy);
        services.AddSingleton<IFeedCacheInvalidator, OutputCacheFeedInvalidator>();
        services.AddScoped<NewznabSearchService>();
        services.AddScoped<SabnzbdQueueService>();
        services.AddScoped<SabnzbdDownloadService>();

        services.AddScoped<NewznabApiKeyFilter>();
        services.AddScoped<SabnzbdApiKeyFilter>();

        return services;
    }
}
