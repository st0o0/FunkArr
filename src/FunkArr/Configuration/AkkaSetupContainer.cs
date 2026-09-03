using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Sql.Hosting;
using Akka.Remote.Hosting;
using FunkArr.Core;
using FunkArr.Download;
using FunkArr.MatchMagic;
using FunkArr.RuleSet;
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
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<FunkArrOptions>>().CurrentValue;
        var dbPath = Path.GetFullPath(options.PersistencePath);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var connectionString = $"Data Source={dbPath}";

        builder
            .ConfigureLoggers(loggers =>
            {
                loggers.ClearLoggers();
                loggers.AddLoggerFactory();
            })
            .WithSqlPersistence(connectionString, ProviderName.SQLiteMS, autoInitialize: true,
                journalBuilder: journal => journal.WithHealthCheck(),
                snapshotBuilder: snapshot => snapshot.WithHealthCheck())
            .WithActorSystemLivenessCheck()
            .WithRemoting(new RemoteOptions
            {
                HostName = "localhost",
                Port = 2552,
            })
            .WithClustering(new ClusterOptions
            {
                SeedNodes = ["akka.tcp://funkarr@localhost:2552"],
            })
            .WithSingleton<IMediathekManager>("mediathek-view-web-manager",
                (_, _, resolver) => resolver.Props<MediathekViewWebManager>())
            .WithSingleton<IRuleSetResolver>("ruleset-resolver",
                (_, _, resolver) => resolver.Props<RuleSetResolver>())
            .WithSingleton<IRuleSetManager>("ruleset-manager",
                (_, _, resolver) => resolver.Props<RuleSetManager>())
            .WithSingleton<IRuleSetUpdater>("ruleset-updater",
                (_, _, resolver) => resolver.Props<RuleSetUpdater>())
            .WithSingleton<ISearchManager>("search-manager",
                (_, _, resolver) => resolver.Props<SearchManager>())
            .WithSingleton<IDownloadManager>("download-manager",
                (_, _, resolver) => resolver.Props<DownloadManager>())
            .WithSingleton<IDownloadHistoryManager>("download-history",
                (_, _, resolver) => resolver.Props<DownloadHistoryManager>())
            .WithSingleton<IMatchMagicManager>("match-magic-manager",
                (_, _, resolver) => resolver.Props<MatchMagicManager>())
            .WithShardRegion<ITvSearchRegion>("tv-search",
                (_, _, resolver) => _ => resolver.Props<TvSearchWorker>(),
                new ShardMessageExtractor(),
                new ShardOptions())
            .WithShardRegion<IMovieSearchRegion>(
                "movie-search",
                (_, _, resolver) => _ => resolver.Props<MovieSearchWorker>(),
                new ShardMessageExtractor(),
                new ShardOptions())
            .WithShardRegion<IDownloadRegion>(
                "download-worker",
                (_, _, resolver) => _ => resolver.Props<DownloadWorker>(),
                new ShardMessageExtractor(),
                new ShardOptions())
            .WithShardRegion<IRuleSetRegion>(
                "ruleset-worker",
                (_, _, resolver) => _ => resolver.Props<RuleSetWorker>(),
                new ShardMessageExtractor(),
                new ShardOptions())
            .WithShardRegion<IMatchHistoryRegion>(
                "match-history",
                (_, _, resolver) => entityId => resolver.Props<MatchHistoryWorker>(entityId),
                new ShardMessageExtractor(),
                new ShardOptions());
    }
}
