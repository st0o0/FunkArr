using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Logger.Serilog;
using Akka.Persistence.Sql.Hosting;
using FunkArr.DownloadClient;
using FunkArr.DownloadClient.Pipeline;
using FunkArr.DownloadClient.Queue;
using FunkArr.DownloadClient.Tracker;
using FunkArr.RuleSet;
using FunkArr.Search;
using FunkArr.Search.Resolvers;
using FunkArr.Shared;
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
            .WithClustering(new ClusterOptions
            {
                SeedNodes = ["akka.tcp://funkarr@0.0.0.0:2552"],
            })
            .WithShardRegion<DownloadRequestActor>(
                typeName: "download-request-tracker",
                entityPropsFactory: (_, _, resolver) => _ => resolver.Props<DownloadRequestActor>(),
                messageExtractor: new ShardedMessageExtractor(10),
                shardOptions: new ShardOptions())
            .WithShardRegion<DownloadActor>(
                typeName: "download-coordinator",
                entityPropsFactory: (_, _, resolver) => _ => resolver.Props<DownloadActor>(),
                messageExtractor: new ShardedMessageExtractor(10),
                shardOptions: new ShardOptions())
            .WithShardRegion<TextSearchActor>(
                typeName: "text-search",
                entityPropsFactory: (_, _, resolver) => _ => resolver.Props<TextSearchActor>(),
                messageExtractor: new ShardedMessageExtractor(20),
                shardOptions: new ShardOptions())
            .WithShardRegion<TvSearchActor>(
                typeName: "tv-search",
                entityPropsFactory: (_, _, resolver) => _ => resolver.Props<TvSearchActor>(),
                messageExtractor: new ShardedMessageExtractor(20),
                shardOptions: new ShardOptions())
            .WithShardRegion<MovieSearchActor>(
                typeName: "movie-search",
                entityPropsFactory: (_, _, resolver) => _ => resolver.Props<MovieSearchActor>(),
                messageExtractor: new ShardedMessageExtractor(20),
                shardOptions: new ShardOptions())
            .WithResolvableActors(r =>
            {
                r.Register<RuleSetActor>("ruleset-registry");
                r.Register<MediathekGatewayActor>("mediathek-gateway");
                r.Register<BrowseActor>("browse-coordinator");
                r.Register<SeriesResolver>("series-resolver");
                r.Register<MovieResolver>("movie-resolver");
                r.Register<QueueActor>("queue-coordinator");
            });
    }
}
