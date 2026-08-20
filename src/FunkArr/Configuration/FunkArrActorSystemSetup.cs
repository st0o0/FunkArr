using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
using Akka.Logger.Serilog;
using Akka.Pattern;
using Akka.Persistence.Sql.Hosting;
using FunkArr.DownloadClient;
using FunkArr.RuleSet;
using FunkArr.Search;
using LinqToDB;
using Microsoft.Extensions.Options;
using Servus.Akka;
using Servus.Akka.Startup;

namespace FunkArr.Configuration;

public sealed class FunkArrActorSystemSetup : ActorSystemSetupContainer
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);
    private const double RandomFactor = 0.2;

    protected override string GetActorSystemName() => "funkarr";

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<FunkArrOptions>>().Value;
        var usePostgres = options.Postgres.IsConfigured;

        var connectionString = usePostgres
            ? options.Postgres.BuildConnectionString()
            : $"Data Source={Path.GetFullPath(options.PersistencePath)}";

        var providerName = usePostgres ? ProviderName.PostgreSQL : ProviderName.SQLiteMS;

        builder
            .ConfigureLoggers(loggers =>
            {
                loggers.ClearLoggers();
                loggers.AddLoggerFactory();
                loggers.AddSerilogLogging();
            })
            .WithSqlPersistence(connectionString, providerName, autoInitialize: true)
            .WithActors((system, registry, resolver) =>
            {
                RegisterWithBackoff<DownloadQueueActor>(system, registry, resolver, "download-queue");
            })
            .WithResolvableActors(r =>
            {
                r.Register<RuleSetRegistryActor>("ruleset-registry");
                r.Register<MatchLedgerActor>("match-ledger");
                r.Register<SearchActor>("search");
            });
    }

    private static void RegisterWithBackoff<TActor>(
        ActorSystem system, IActorRegistry registry,
        IDependencyResolver resolver, string name)
        where TActor : ActorBase
    {
        var childProps = resolver.Props<TActor>();
        var supervisorProps = BackoffSupervisor.Props(
            Backoff.OnFailure(childProps, name, MinBackoff, MaxBackoff, RandomFactor, maxNrOfRetries: -1));
        var supervisor = system.ActorOf(supervisorProps, $"{name}-supervisor");
        registry.Register<TActor>(supervisor);
    }
}
