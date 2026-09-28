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

namespace FunkArr.IntegrationTests;

public sealed class FunkArrFixture : IAsyncLifetime
{
    private WebApplication _app = null!;
    private ActorSystem _actorSystem = null!;
    private Dictionary<Type, TestProbe> _probes = null!;
    private string _tempDir = null!;

    public HttpClient Client { get; private set; } = null!;

    public TestProbe GetProbe<TKey>() => _probes[typeof(TKey)];

    public async ValueTask InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-int-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _actorSystem = ActorSystem.Create("test");
        var registry = ActorRegistry.For(_actorSystem);
        _probes = RegisterProbes(_actorSystem, registry);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        var services = builder.Services;

        services.AddSingleton<IActorRegistry>(registry);

        var funkArrOptions = new FunkArrOptions { ApiKey = "test-key", DataPath = _tempDir };
        var downloadOptions = new DownloadOptions { Path = Path.Combine(_tempDir, "downloads") };
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
        services.AddSingleton(new RingBufferSink());
        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient();
        services.AddHttpClient<ArrSetupClient>();
        services.AddOutputCache();
        services.AddHealthChecks();

        services.AddArrApiServices(builder.Configuration);

        services.AddControllers()
            .AddApplicationPart(typeof(ArrApi.AssemblyMarker).Assembly);

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        _app = builder.Build();

        _app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = 200,
                [HealthStatus.Degraded] = 200,
                [HealthStatus.Unhealthy] = 503
            }
        });

        _app.MapGet("/alive", () => Microsoft.AspNetCore.Http.Results.Ok("Alive"));
        _app.MapSystemApi();
        _app.MapDownloadsApi();
        _app.MapRuleSetApi();
        _app.MapMediathekApi();
        _app.MapSetupArrApi();
        _app.MapControllers();

        await _app.StartAsync();

        Client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _actorSystem.Terminate();

        try
        {
            Directory.Delete(_tempDir, true);
        }
        catch
        {
            // noop
        }
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
}
