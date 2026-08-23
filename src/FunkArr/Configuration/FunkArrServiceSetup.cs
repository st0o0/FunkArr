using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.DownloadClient;
using FunkArr.Health;
using FunkArr.Muxing;
using FunkArr.RuleSet;
using FunkArr.Search;
using FunkArr.Setup;
using FunkArr.Shared;
using FunkArr.Subtitle;
using Microsoft.Extensions.Options;
using Prometheus;
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

        services
            .AddOptions<DownloadOptions>()
            .Bind(configuration.GetSection(DownloadOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DownloadOptions>, DownloadOptionsValidator>();

        services
            .AddOptions<RuleSetOptions>()
            .Bind(configuration.GetSection(RuleSetOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RuleSetOptions>, RuleSetOptionsValidator>();

        services
            .AddOptions<QualityOptions>()
            .Bind(configuration.GetSection(QualityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<QualityOptions>, QualityOptionsValidator>();

        services
            .AddOptions<SearchOptions>()
            .Bind(configuration.GetSection(SearchOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SearchOptions>, SearchOptionsValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<MuxingService>();
        services.AddSingleton<Mp4DownloadService>();
        services.AddSingleton<HlsDownloadService>();
        services.AddSingleton<SubtitleAcquisitionService>();
        services.AddSingleton<SubtitleNormalizerService>();
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

        services.AddHttpClient<TmdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.DefaultRequestHeaders.Add("User-Agent", "FunkArr");
        });

        services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        services.AddOpenApi();

        Metrics.ConfigureMeterAdapter(options =>
        {
            options.InstrumentFilterPredicate = instrument => instrument.Meter.Name == "FunkArr";
        });

        services.AddHealthChecks()
            .AddCheck<FfmpegHealthCheck>("ffmpeg");

        services.AddSingleton<FfmpegHealthCheck>();
        services.AddSingleton<SetupValidationService>();
        services.AddHttpClient(nameof(SetupValidationService));
    }
}
