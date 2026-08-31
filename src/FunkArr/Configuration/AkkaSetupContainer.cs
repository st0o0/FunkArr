using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Sql.Hosting;
using FunkArr.Core;
using FunkArr.MatchMagic;
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
            .WithSingleton<IMediathekGateway>(
                "mediathek-view-web-manager",
                (_, _, resolver) => resolver.Props<MediathekViewWebManager>())
            .WithSingleton<IMatchMagicService>(
                "match-magic-manager",
                Props.Create(() => new MatchMagicManager()))
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
            .WithSingleton<ISearchGateway>(
                "search-gateway-manager",
                Props.Create(() => new SearchGatewayManager()));
    }
}
