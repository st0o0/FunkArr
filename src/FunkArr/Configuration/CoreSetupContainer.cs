using System.IO.Abstractions;
using System.Text.Json;
using FunkArr.Api.HealthChecks;
using FunkArr.Core;
using FunkArr.Core.Configuration;
using FunkArr.Core.Data;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Terp.Hosting;

namespace FunkArr.Configuration;

public sealed class CoreSetupContainer : ISetup<IServiceCollection>
{
    public void Setup(IServiceCollection services, SetupContext context)
    {
        services
            .AddOptionsWithValidateOnStart<FunkArrOptions, FunkArrOptionsValidator>()
            .Bind(context.Configuration.GetSection(FunkArrOptions.SectionName));

        services
            .AddOptionsWithValidateOnStart<PostgresOptions, PostgresOptionsValidator>()
            .Bind(context.Configuration.GetSection(PostgresOptions.SectionName));

        services
            .AddOptionsWithValidateOnStart<RoutingOptions, RoutingOptionsValidator>()
            .Bind(context.Configuration.GetSection(RoutingOptions.SectionName));

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

        services.AddRequestTimeouts();

        services.AddHttpLogging(options =>
        {
            options.LoggingFields =
                HttpLoggingFields.RequestMethod |
                HttpLoggingFields.RequestPath |
                HttpLoggingFields.RequestQuery |
                HttpLoggingFields.ResponseStatusCode |
                HttpLoggingFields.Duration |
                HttpLoggingFields.RequestBody |
                HttpLoggingFields.ResponseBody;
            options.CombineLogs = true;
        });

        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 2048;
            options.TrackStatistics = true;
        });

        services.AddProblemDetails();
        services.AddOpenApi();

        services.AddHealthChecks()
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
