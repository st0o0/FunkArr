using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Core;
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
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton(sp =>
        {
            var dataPaths = new DataPaths(
                sp.GetRequiredService<IOptions<FunkArrOptions>>().Value,
                sp.GetRequiredService<IOptions<DownloadOptions>>().Value);
            dataPaths.EnsureDirectories();
            return dataPaths;
        });
        services.AddSingleton<IDataFiles, DataFiles>();

        services.AddOpenApi();

        services.AddOutputCache(options =>
        {
            options.AddPolicy("RuleSetList", builder =>
                builder.Expire(TimeSpan.FromSeconds(30)).Tag("rulesets"));
        });

        services.AddHealthChecks();
    }
}
