using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Core;
using FunkArr.Download;
using FunkArr.MetadataResolver;
using Microsoft.Extensions.Options;
using Servus.Core.Application.Startup;

namespace FunkArr.Configuration;

public sealed class ServiceSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<FunkArrOptions>()
            .Bind(configuration.GetSection(FunkArrOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<RuleSetUpdaterOptions>()
            .Bind(configuration.GetSection(RuleSetUpdaterOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<ScoringOptions>()
            .Bind(configuration.GetSection(ScoringOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<MatchHistoryOptions>()
            .Bind(configuration.GetSection(MatchHistoryOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<DownloadOptions>()
            .Bind(configuration.GetSection(DownloadOptions.SectionName))
            .ValidateOnStart();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton(sp => new DataPaths(
            sp.GetRequiredService<IOptions<FunkArrOptions>>().Value,
            sp.GetRequiredService<IOptions<DownloadOptions>>().Value));
        services.AddSingleton<IDataFiles, DataFiles>();

        services.AddOpenApi();

        services.AddHealthChecks();

        services.AddDownloadServices();

        services
            .AddOptions<TvdbOptions>()
            .Bind(configuration.GetSection("Tvdb"));

        services
            .AddOptions<MetadataResolverOptions>()
            .Bind(configuration.GetSection("MetadataResolver"));

        services
            .AddOptions<TmdbOptions>()
            .Bind(configuration.GetSection("Tmdb"));

        services.AddHttpClient("Tvdb");
        services.AddSingleton<TvdbClient>();

        services.AddHttpClient("Tmdb");
        services.AddSingleton<TmdbClient>();

        services.AddHttpClient("MediathekViewWeb", client =>
        {
            client.BaseAddress = new Uri("https://mediathekviewweb.de/api/query");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        var version = typeof(ServiceSetupContainer).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        services.AddHttpClient("GitHub", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("User-Agent", $"FunkArr/{version}");
        });
    }
}
