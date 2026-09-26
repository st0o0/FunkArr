using System.IO.Abstractions;
using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Api;
using FunkArr.ArrApi;
using FunkArr.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.Api.Tests.Integration;

public sealed class FunkArrTestServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ActorSystem _actorSystem;
    private readonly Dictionary<Type, TestProbe> _probes;
    private readonly string _tempDir;

    public HttpClient Client { get; }

    private FunkArrTestServer(WebApplication app, ActorSystem actorSystem,
        Dictionary<Type, TestProbe> probes, HttpClient client, string tempDir)
    {
        _app = app;
        _actorSystem = actorSystem;
        _probes = probes;
        _tempDir = tempDir;
        Client = client;
    }

    public TestProbe GetProbe<TKey>() => _probes[typeof(TKey)];

    public static async Task<FunkArrTestServer> CreateAsync()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var actorSystem = ActorSystem.Create("test");
        var registry = ActorRegistry.For(actorSystem);
        var probes = RegisterProbes(actorSystem, registry);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        var services = builder.Services;

        services.AddSingleton<IActorRegistry>(registry);

        var funkArrOptions = new FunkArrOptions { ApiKey = "test-key", DataPath = tempDir };
        var downloadOptions = new DownloadOptions { Path = Path.Combine(tempDir, "downloads") };
        services.Configure<FunkArrOptions>(o =>
        {
            o.ApiKey = funkArrOptions.ApiKey;
            o.DataPath = funkArrOptions.DataPath;
        });
        services.Configure<DownloadOptions>(o => o.Path = downloadOptions.Path);
        services.Configure<RoutingOptions>(_ => { });
        services.Configure<ArrApiOptions>(_ => { });
        services.Configure<ScoringOptions>(_ => { });
        services.Configure<ScoringHistoryOptions>(_ => { });
        services.Configure<RuleSetUpdaterOptions>(_ => { });

        var dataPaths = new DataPaths(Options.Create(funkArrOptions), Options.Create(downloadOptions));
        dataPaths.EnsureDirectories();
        services.AddSingleton(dataPaths);

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IDataFiles, DataFiles>();
        services.AddSingleton<RuleSet.RuleSetStore>();
        services.AddSingleton<IRuleSetValidator, RuleSet.RuleSetValidator>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddHttpClient();
        services.AddHttpClient<ArrApiClient>();
        services.AddOutputCache();
        services.AddHealthChecks();

        services.AddArrApiServices(builder.Configuration);

        services.AddControllers()
            .AddApplicationPart(typeof(FunkArr.ArrApi.AssemblyMarker).Assembly);

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        var app = builder.Build();

        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = 200,
                [HealthStatus.Degraded] = 200,
                [HealthStatus.Unhealthy] = 503,
            },
        });

        app.MapGet("/alive", () => Microsoft.AspNetCore.Http.Results.Ok("Alive"));
        app.MapSystemApi();
        app.MapDownloadsApi();
        app.MapRuleSetApi();
        app.MapMediathekApi();
        app.MapSetupArrApi();
        app.MapControllers();

        await app.StartAsync();

        var client = app.GetTestClient();

        return new FunkArrTestServer(app, actorSystem, probes, client, tempDir);
    }

    private static Dictionary<Type, TestProbe> RegisterProbes(ActorSystem system, ActorRegistry registry)
    {
        var probes = new Dictionary<Type, TestProbe>();
        var assertions = new XunitAssertions();

        Register<IDownloadManager>();
        Register<IDownloadHistoryManager>();
        Register<ISearchManager>();
        Register<IMediathekManager>();
        Register<IScoringManager>();
        Register<IRuleSetResolver>();
        Register<IRuleSetManager>();
        Register<IRuleSetRegion>();
        Register<IRuleSetUpdater>();
        Register<IHistoryRegion>();
        Register<IStatsCollector>();
        Register<IEnrichmentManager>();
        Register<IDownloadScheduler>();

        return probes;

        void Register<TKey>() where TKey : notnull
        {
            var probe = new TestProbe(system, assertions);
            registry.Register<TKey>(probe);
            probes[typeof(TKey)] = probe;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _actorSystem.Terminate();

        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
