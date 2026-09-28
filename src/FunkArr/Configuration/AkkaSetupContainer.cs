using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Sql.Hosting;
using Akka.Remote.Hosting;
using FunkArr.Core;
using FunkArr.Download;
using FunkArr.Enrichment;
using FunkArr.History;
using FunkArr.RuleSet;
using FunkArr.Scoring;
using FunkArr.Search;
using LinqToDB;
using Microsoft.Extensions.Options;
using Servus.Akka.Startup;

namespace FunkArr.Configuration;

public sealed class AkkaSetupContainer : ActorSystemSetupContainer
{
    protected override string GetActorSystemName() => "funkarr";

    protected override void BuildSystem(AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
    {
        var postgresOptions = serviceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value;

        string connectionString;
        string providerName;

        if (!string.IsNullOrEmpty(postgresOptions.Host))
        {
            connectionString = postgresOptions.ToConnectionString();
            providerName = ProviderName.PostgreSQL;
        }
        else
        {
            var dataPaths = serviceProvider.GetRequiredService<DataPaths>();
            var dataFiles = serviceProvider.GetRequiredService<IDataFiles>();
            dataFiles.CreateDirectory(Path.GetDirectoryName(dataPaths.Database)!);
            connectionString = $"Data Source={dataPaths.Database}";
            providerName = ProviderName.SQLiteMS;
        }

        builder
            .ConfigureLoggers(loggers =>
            {
                loggers.ClearLoggers();
                loggers.AddLoggerFactory();
            })
            .WithSqlPersistence(connectionString, providerName, autoInitialize: true,
                journalBuilder: journal => journal.WithHealthCheck(),
                snapshotBuilder: snapshot => snapshot.WithHealthCheck())
            .WithActorSystemLivenessCheck()
            .WithAkkaClusterReadinessCheck()
            .WithRemoting(new RemoteOptions
            {
                HostName = "localhost",
                Port = 2552
            })
            .WithClustering(new ClusterOptions
            {
                SeedNodes = ["akka.tcp://funkarr@localhost:2552"]
            });

        RegisterSearchActors(builder);
        RegisterDownloadActors(builder);
        RegisterScoringActors(builder);
        RegisterRuleSetActors(builder);
        RegisterEnrichmentActors(builder);
    }

    private static void RegisterSearchActors(AkkaConfigurationBuilder builder)
    {
        builder
            .WithSingleton<IMediathekManager>("mediathek-view-web-manager",
                (_, _, resolver) => resolver.Props<MediathekViewWebManager>())
            .WithSingleton<ISearchManager>("search-manager",
                (_, _, resolver) => resolver.Props<SearchManager>())
            .WithShardRegion<ITvSearchRegion>("tv-search",
                (_, _, resolver) => nttId => resolver.Props<TvSearchWorker>(nttId),
                new ShardMessageExtractor(),
                new ShardOptions { PassivateIdleEntityAfter = TimeSpan.FromSeconds(30), ShouldPassivateIdleEntities = true })
            .WithShardRegion<IMovieSearchRegion>("movie-search",
                (_, _, resolver) => nttId => resolver.Props<MovieSearchWorker>(nttId),
                new ShardMessageExtractor(),
                new ShardOptions
                { PassivateIdleEntityAfter = TimeSpan.FromSeconds(30), ShouldPassivateIdleEntities = true });
    }

    private static void RegisterDownloadActors(AkkaConfigurationBuilder builder)
    {
        builder
            .WithSingleton<IDownloadManager>("download-manager",
                (_, _, resolver) => resolver.Props<DownloadManager>())
            .WithSingleton<IDownloadScheduler>("download-scheduler",
                (_, _, resolver) => resolver.Props<DownloadScheduler>())
            .WithSingleton<IDownloadHistoryManager>("download-history",
                (_, _, resolver) => resolver.Props<DownloadHistoryManager>())
            .WithShardRegion<IDownloadRegion>("download-worker",
                (_, _, resolver) => nttId => resolver.Props<DownloadWorker>(nttId),
                new ShardMessageExtractor(),
                new ShardOptions { PassivateIdleEntityAfter = TimeSpan.FromMinutes(5), ShouldPassivateIdleEntities = true });
    }

    private static void RegisterScoringActors(AkkaConfigurationBuilder builder)
    {
        builder
            .WithSingleton<IScoringManager>("scoring-manager",
                (_, _, resolver) => resolver.Props<ScoringManager>())
            .WithSingleton<IStatsCollector>("stats-collector",
                (_, _, resolver) => resolver.Props<StatsCollector>())
            .WithShardRegion<IHistoryRegion>("history",
                (_, _, resolver) => nttId => resolver.Props<HistoryWorker>(nttId),
                new ShardMessageExtractor(),
                new ShardOptions { PassivateIdleEntityAfter = TimeSpan.FromMinutes(5), ShouldPassivateIdleEntities = true });
    }

    private static void RegisterRuleSetActors(AkkaConfigurationBuilder builder)
    {
        builder
            .WithSingleton<IRuleSetResolver>("ruleset-resolver",
                (_, _, resolver) => resolver.Props<RuleSetResolver>())
            .WithSingleton<IRuleSetManager>("ruleset-manager",
                (_, _, resolver) => resolver.Props<RuleSetManager>())
            .WithSingleton<IRuleSetUpdater>("ruleset-updater",
                (_, _, resolver) => resolver.Props<RuleSetUpdater>())
            .WithShardRegion<IRuleSetRegion>("ruleset-worker",
                (_, _, resolver) => nttId => resolver.Props<RuleSetWorker>(nttId),
                new ShardMessageExtractor(),
                new ShardOptions { PassivateIdleEntityAfter = TimeSpan.FromMinutes(5), ShouldPassivateIdleEntities = true });
    }

    private static void RegisterEnrichmentActors(AkkaConfigurationBuilder builder)
    {
        builder
            .WithSingleton<IEnrichmentManager>("enrichment-manager",
                (_, _, resolver) => resolver.Props<EnrichmentManager>());
    }
}
