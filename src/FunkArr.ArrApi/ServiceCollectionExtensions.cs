using FunkArr.ArrApi.Newznab.Services;
using FunkArr.ArrApi.Sabnzbd.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.ArrApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArrApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptionsWithValidateOnStart<ArrApiOptions, ArrApiOptionsValidator>()
            .Bind(configuration.GetSection(ArrApiOptions.SectionName));

        services.AddOutputCache(NewznabFeedCache.AddPolicy);
        services.AddScoped<NewznabSearchService>();
        services.AddScoped<SabnzbdQueueService>();
        services.AddScoped<SabnzbdDownloadService>();

        services.AddScoped<NewznabApiKeyFilter>();
        services.AddScoped<SabnzbdApiKeyFilter>();

        return services;
    }
}
