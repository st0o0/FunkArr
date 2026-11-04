using FunkArr.Download.Ffmpeg;
using FunkArr.Download.Subtitles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace FunkArr.Download;

public static class DownloadServiceExtensions
{
    public static IServiceCollection AddDownloadServices(this IServiceCollection services)
    {
        services.AddSingleton<IHttpMessageHandlerBuilderFilter, RouteProxyFilter>();
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<ISubtitleDownloader, SubtitleDownloader>();
        services.AddSingleton<IFfmpegProcess, FfmpegProcess>();
        services.AddTransient<IRemuxer, Remuxer>();
        return services;
    }
}
