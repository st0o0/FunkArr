using System.Net;
using FunkArr.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.Download;

public static class DownloadServiceExtensions
{
    public static IServiceCollection AddDownloadServices(this IServiceCollection services, IConfiguration configuration)
    {
        var routingOptions = new RoutingOptions();
        configuration.GetSection(RoutingOptions.SectionName).Bind(routingOptions);

        foreach (var definition in routingOptions.Definitions)
        {
            var builder = services.AddHttpClient($"route:{definition.Name}");

            if (!string.IsNullOrWhiteSpace(definition.Proxy))
            {
                var proxyUrl = definition.Proxy;
                builder.ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler { Proxy = new WebProxy(proxyUrl), UseProxy = true });
            }

            builder.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            });
        }

        services.AddTransient<ISubtitlePreparer, SubtitlePreparer>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IFfmpegRunner, FfmpegRunner>();
        services.AddTransient<IRemuxer, Remuxer>();
        return services;
    }
}
