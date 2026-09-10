using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.Download;

public static class DownloadServiceExtensions
{
    public static IServiceCollection AddDownloadServices(this IServiceCollection services)
    {
        services.AddHttpClient<ISubtitlePreparer, SubtitlePreparer>();
        services.AddSingleton<IFfmpegRunner, FfmpegRunner>();
        services.AddTransient<IRemuxer, Remuxer>();
        return services;
    }
}
