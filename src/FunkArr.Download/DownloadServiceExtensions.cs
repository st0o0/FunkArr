using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.Download;

public static class DownloadServiceExtensions
{
    public static IServiceCollection AddDownloadServices(this IServiceCollection services)
    {
        services.AddHttpClient<ISubtitlePreparer, SubtitlePreparer>()
            .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IFfmpegRunner, FfmpegRunner>();
        services.AddTransient<IRemuxer, Remuxer>();
        return services;
    }
}
