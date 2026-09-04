using Microsoft.Extensions.DependencyInjection;

namespace FunkArr.Download;

public static class DownloadServiceExtensions
{
    public static IServiceCollection AddDownloadServices(this IServiceCollection services) =>
        services
            .AddSingleton<IFfmpegRunner, FfmpegRunner>();
}
