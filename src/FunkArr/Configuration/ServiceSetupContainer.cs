using System.IO.Abstractions;
using System.Text.Json;
using FunkArr.Api;
using FunkArr.Api.HealthChecks;
using FunkArr.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
            .AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName));

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton(sp =>
        {
            var dataPaths = new DataPaths(
                sp.GetRequiredService<IOptions<FunkArrOptions>>(),
                sp.GetRequiredService<IOptions<DownloadOptions>>());
            dataPaths.EnsureDirectories();
            return dataPaths;
        });
        services.AddSingleton<IDataFiles, DataFiles>();
        services.AddSingleton<RuleSet.RuleSetStore>();

        services.AddOpenApi();

        services.AddOutputCache(options =>
        {
            options.AddPolicy("RuleSetList", builder =>
                builder.Expire(TimeSpan.FromSeconds(30)).Tag("rulesets"));
            options.AddPolicy("SystemVersion", builder =>
                builder.Expire(TimeSpan.FromSeconds(60)));
        });

        services.AddHttpClient<ArrApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddControllers()
            .AddApplicationPart(typeof(FunkArr.ArrApi.AssemblyMarker).Assembly);

        services.AddHealthChecks()
            .AddCheck<FfmpegHealthCheck>("ffmpeg", failureStatus: HealthStatus.Unhealthy)
            .AddCheck<MediathekViewWebHealthCheck>("mediathekviewweb", failureStatus: HealthStatus.Degraded)
            .Add(new HealthCheckRegistration(
                "data-directory",
                sp => new DirectoryHealthCheck(
                    sp.GetRequiredService<DataPaths>(),
                    sp.GetRequiredService<IDataFiles>(),
                    p => p.DataRoot),
                failureStatus: HealthStatus.Unhealthy,
                tags: null))
            .Add(new HealthCheckRegistration(
                "complete-directory",
                sp => new DirectoryHealthCheck(
                    sp.GetRequiredService<DataPaths>(),
                    sp.GetRequiredService<IDataFiles>(),
                    p => p.Complete),
                failureStatus: HealthStatus.Unhealthy,
                tags: null))
            .Add(new HealthCheckRegistration(
                "incomplete-directory",
                sp => new DirectoryHealthCheck(
                    sp.GetRequiredService<DataPaths>(),
                    sp.GetRequiredService<IDataFiles>(),
                    p => p.Incomplete),
                failureStatus: HealthStatus.Unhealthy,
                tags: null));
    }
}
