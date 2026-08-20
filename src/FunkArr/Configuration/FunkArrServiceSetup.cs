using FunkArr.Health;
using FunkArr.Muxing;
using FunkArr.RuleSet;
using FunkArr.Search;
using FunkArr.Shared;
using Microsoft.Extensions.Options;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class FunkArrServiceSetup : IServiceSetupContainer
{
    internal static readonly string AppVersion =
        typeof(FunkArrServiceSetup).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FunkArrOptions>()
            .Bind(configuration.GetSection(FunkArrOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<FunkArrOptions>, FunkArrOptionsValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<MuxingService>();
        services.AddSingleton<ConfigFileWriter>();

        services.AddSingleton<QualityProbeService>();
        services.AddSingleton<GitHubReleaseClient>();

        services.AddHttpClient("GitHubRelease", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", $"FunkArr/{FunkArrServiceSetup.AppVersion}");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        });

        services.AddHttpClient("QualityProbe", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "FunkArr");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<MediathekClient>(client =>
        {
            client.BaseAddress = new Uri("https://mediathekviewweb.de/");
            client.DefaultRequestHeaders.Add("User-Agent", "FunkArr");
        });

        services.AddHttpClient<TvdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.thetvdb.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "FunkArr");
        });

        services.AddHealthChecks()
            .AddCheck<FfmpegHealthCheck>("ffmpeg");
    }
}
