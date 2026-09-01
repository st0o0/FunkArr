using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Sql.Hosting;
using FunkArr.Core;
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
        var options = serviceProvider.GetRequiredService<IOptions<FunkArrOptions>>().Value;
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
            .WithClustering()
            .WithSingleton<IMediathekManager>(
                "mediathek-view-web-manager",
                (_, _, resolver) => resolver.Props<MediathekViewWebManager>())
            .WithSingleton<IRuleSetResolver>(
                "ruleset-resolver",
                Props.Create(() => new RuleSetResolver()))
            .WithSingleton<IRuleSetManager>(
                "ruleset-manager",
                Props.Create(() => new RuleSetManager()))
            .WithShardRegion<IRuleSetRegion>(
                "ruleset-worker",
                (_, _, _) => _ => Props.Create(() => new RuleSetWorker()),
                new ShardMessageExtractor(),
                new ShardOptions())
            .WithShardRegion<IMatchHistoryRegion>(
                "match-history",
                (_, _, _) => entityId => Props.Create(() => new MatchHistoryWorker(entityId)),
                new ShardMessageExtractor(),
                new ShardOptions())
            .WithSingleton<IMatchMagicManager>(
                "match-magic-manager",
                Props.Create(() => new MatchMagicManager(options.ScoringPoolSize)))
            .WithShardRegion<ITvSearchRegion>(
                "tv-search",
                (_, _, _) => _ => Props.Create(() => new TvSearchWorker()),
                new ShardMessageExtractor(),
                new ShardOptions())
            .WithShardRegion<IMovieSearchRegion>(
                "movie-search",
                (_, _, _) => _ => Props.Create(() => new MovieSearchWorker()),
                new ShardMessageExtractor(),
                new ShardOptions())
            .WithSingleton<ISearchManager>(
                "search-manager",
                Props.Create(() => new SearchManager()));
    }
}
