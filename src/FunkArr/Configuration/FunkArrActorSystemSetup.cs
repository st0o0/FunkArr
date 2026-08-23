using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Logger.Serilog;
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
            .WithActorSystemLivenessCheck()
            .ConfigureLoggers(loggers =>
            {
                loggers.ClearLoggers();
                loggers.AddLoggerFactory();
                loggers.AddSerilogLogging();
            })
            .WithSqlPersistence(
                connectionString, providerName, autoInitialize: true,
                journalBuilder: journal => journal.WithHealthCheck(),
                snapshotBuilder: snapshot => snapshot.WithHealthCheck())
            .WithClustering()
            .WithShardRegion<DownloadRequestTracker>(
                typeName: "download-request-tracker",
                entityPropsFactory: (_, _, resolver) => _ => resolver.Props<DownloadRequestTracker>(),
                messageExtractor: new DownloadRequestTrackerMessageExtractor(),
                shardOptions: new ShardOptions())
            .WithShardRegion<DownloadCoordinator>(
                typeName: "download-coordinator",
                entityPropsFactory: (_, _, resolver) => _ => resolver.Props<DownloadCoordinator>(),
                messageExtractor: new DownloadCoordinatorMessageExtractor(),
                shardOptions: new ShardOptions())
            .WithResolvableActors(r =>
            {
                r.Register<RuleSetCoordinator>("ruleset-registry");
                r.Register<SearchCoordinator>("search");
                r.Register<QueueCoordinator>("queue-coordinator");
            });
    }
}
