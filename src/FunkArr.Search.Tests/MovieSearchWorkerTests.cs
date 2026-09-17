using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class MovieSearchWorkerTests : TestKit
{
    private record TestProbes(
        Akka.TestKit.TestProbe Mediathek,
        Akka.TestKit.TestProbe Scoring,
        Akka.TestKit.TestProbe Resolver,
        Akka.TestKit.TestProbe Enrichment);

    private TestProbes RegisterProbes()
    {
        var probes = new TestProbes(
            CreateTestProbe(),
            CreateTestProbe(),
            CreateTestProbe(),
            CreateTestProbe());

        var registry = ActorRegistry.For(Sys);
        registry.Register<IMediathekManager>(probes.Mediathek);
        registry.Register<IScoringManager>(probes.Scoring);
        registry.Register<IRuleSetResolver>(probes.Resolver);
        registry.Register<IEnrichmentManager>(probes.Enrichment);

        return probes;
    }

    [Fact]
    public void Successful_movie_search()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new SearchMovie(searchId, "radarr", "Das Boot", null, null, null, null), TestActor);

        var mediathekQuery = probes.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Contains("title", mediathekQuery.Fields[0].Fields);
        Assert.Contains("topic", mediathekQuery.Fields[0].Fields);
        Assert.Equal(3600, mediathekQuery.DurationMin);

        probes.Mediathek.Reply(new QueryMediathekCompleted(
        [
            new MediathekItem("ARD", "Das Boot", "Das Boot", null, 1719244800, 7200, 2400000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null),
        ], 1));

        var resolveRequest = probes.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Equal("Das Boot", resolveRequest.TopicOrAlias);
        probes.Resolver.Reply(new RuleSetResolved("das-boot", "Das Boot"));

        var scoreRequest = probes.Scoring.ExpectMsg<ScoreItems>();
        Assert.Equal("das-boot", scoreRequest.RuleSetId);

        probes.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Equal(2, result.Items.Length);
        Assert.Contains(result.Items, i => i.Quality == 1080);
        Assert.Contains(result.Items, i => i.Quality == 720);
    }

    [Fact]
    public void No_query_queries_mediathek_for_recent_items()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new SearchMovie(searchId, "radarr", null, null, null, null, null), TestActor);

        var query = probes.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Empty(query.Fields);
        probes.Mediathek.Reply(new QueryMediathekCompleted([], 0));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void RuleSet_not_found_returns_unscored_results()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new SearchMovie(searchId, "radarr", "Unknown Movie", null, null, null, null), TestActor);

        probes.Mediathek.ExpectMsg<QueryMediathek>();
        probes.Mediathek.Reply(new QueryMediathekCompleted(
        [
            new MediathekItem("ZDF", "Unknown Movie", "Unknown Movie", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        probes.Resolver.ExpectMsg<ResolveRuleSet>();
        probes.Resolver.Reply(new RuleSetFailed(new RuleSetNotFoundException("Unknown Movie")));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Equal(searchId, result.SearchId);
        var item = Assert.Single(result.Items);
        Assert.Equal(0.0, item.Score);
    }

    [Fact]
    public void ImdbId_only_search_resolves_then_queries_mvw()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new SearchMovie(searchId, "radarr", null, "tt0806910", null, null, null), TestActor);

        var resolveRequest = probes.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Null(resolveRequest.TopicOrAlias);
        Assert.Equal("tt0806910", resolveRequest.ImdbId);
        probes.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        var mediathekQuery = probes.Mediathek.ExpectMsg<QueryMediathek>();
        Assert.Contains("Tatort", mediathekQuery.Fields[0].Query);
        Assert.Equal(3600, mediathekQuery.DurationMin);

        probes.Mediathek.Reply(new QueryMediathekCompleted(
        [
            new MediathekItem("ARD", "Tatort", "Tatort: Der Film", null, 1719244800, 7200, 2400000000,
                null, "https://example.com/sd.mp4", "https://example.com/hd.mp4", null, null),
        ], 1));

        var scoreRequest = probes.Scoring.ExpectMsg<ScoreItems>();
        Assert.Equal("tatort", scoreRequest.RuleSetId);

        probes.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.85, true)]));

        var enrichRequest = probes.Enrichment.ExpectMsg<EnrichMovies>();
        Assert.Equal("tt0806910", enrichRequest.ImdbId);
        probes.Enrichment.Reply(new EnrichMoviesFailed(new Exception("Not configured")));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Equal(2, result.Items.Length);
        Assert.All(result.Items, i => Assert.Equal("tt0806910", i.ImdbId));
    }

    [Fact]
    public void TmdbId_only_search_resolves_then_queries_mvw()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new SearchMovie(searchId, "radarr", null, null, 550, null, null), TestActor);

        var resolveRequest = probes.Resolver.ExpectMsg<ResolveRuleSet>();
        Assert.Equal(550, resolveRequest.TmdbId);
        probes.Resolver.Reply(new RuleSetResolved("fight-club", "Fight Club"));

        probes.Mediathek.ExpectMsg<QueryMediathek>();
        probes.Mediathek.Reply(new QueryMediathekCompleted(
        [
            new MediathekItem("ARD", "Fight Club", "Fight Club", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        probes.Scoring.ExpectMsg<ScoreItems>();
        probes.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.7, true)]));

        var enrichRequest = probes.Enrichment.ExpectMsg<EnrichMovies>();
        Assert.Equal(550, enrichRequest.TmdbId);
        probes.Enrichment.Reply(new EnrichMoviesCompleted([
            new EnrichedMovie(0, "Fight Club", 1999, null, 550, 0.95f, MatchMethod.TitleMatch),
        ]));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Single(result.Items);
        Assert.Equal(550, result.Items[0].TmdbId);
        Assert.Equal(0.95f, result.Items[0].MatchConfidence);
        Assert.Equal(MatchMethod.TitleMatch, result.Items[0].MatchMethod);
    }

    [Fact]
    public void ImdbId_only_unresolved_returns_empty()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new SearchMovie(searchId, "radarr", null, "tt9999999", null, null, null), TestActor);

        probes.Resolver.ExpectMsg<ResolveRuleSet>();
        probes.Resolver.Reply(new RuleSetFailed(new RuleSetNotFoundException("")));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void Text_search_carries_imdbId_through_to_results()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new SearchMovie(searchId, "radarr", "Das Boot", "tt1234567", null, null, null), TestActor);

        probes.Mediathek.ExpectMsg<QueryMediathek>();
        probes.Mediathek.Reply(new QueryMediathekCompleted(
        [
            new MediathekItem("ARD", "Das Boot", "Das Boot", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        probes.Resolver.ExpectMsg<ResolveRuleSet>();
        probes.Resolver.Reply(new RuleSetFailed(new RuleSetNotFoundException("Das Boot")));

        var result = ExpectMsg<SearchMovieCompleted>();
        var item = Assert.Single(result.Items);
        Assert.Equal("tt1234567", item.ImdbId);
    }

    [Fact]
    public void Resolution_skipped_when_no_ids()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        worker.Tell(new SearchMovie(Guid.NewGuid(), "radarr", "Das Boot", null, null, null, null), TestActor);

        probes.Mediathek.ExpectMsg<QueryMediathek>();
        probes.Mediathek.Reply(new QueryMediathekCompleted(
        [
            new MediathekItem("ARD", "Das Boot", "Das Boot", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        probes.Resolver.ExpectMsg<ResolveRuleSet>();
        probes.Resolver.Reply(new RuleSetResolved("das-boot", "Das Boot"));

        probes.Scoring.ExpectMsg<ScoreItems>();
        probes.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Single(result.Items);
        probes.Enrichment.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Resolution_failure_falls_back_gracefully()
    {
        var probes = RegisterProbes();

        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        worker.Tell(new SearchMovie(Guid.NewGuid(), "radarr", null, "tt1234567", null, null, null), TestActor);

        probes.Resolver.ExpectMsg<ResolveRuleSet>();
        probes.Resolver.Reply(new RuleSetResolved("das-boot", "Das Boot"));

        probes.Mediathek.ExpectMsg<QueryMediathek>();
        probes.Mediathek.Reply(new QueryMediathekCompleted(
        [
            new MediathekItem("ARD", "Das Boot", "Das Boot", null, 0, 7200, 0,
                null, "https://x.com/v.mp4", null, null, null),
        ], 1));

        probes.Scoring.ExpectMsg<ScoreItems>();
        probes.Scoring.Reply(new ScoreCompleted(Guid.Empty, [new ScoredItem(0, 0.8, true)]));

        probes.Enrichment.ExpectMsg<EnrichMovies>();
        probes.Enrichment.Reply(new EnrichMoviesFailed(new Exception("TMDB unavailable")));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Single(result.Items);
        Assert.Null(result.Items[0].MatchConfidence);
    }

    [Fact]
    public void ImdbId_search_with_zero_mvw_results_returns_empty()
    {
        var probes = RegisterProbes();
        var worker = Sys.ActorOf(Props.Create(() => new MovieSearchWorker()));

        var searchId = Guid.NewGuid();
        worker.Tell(new SearchMovie(searchId, "radarr", null, "tt0806910", null, null, null), TestActor);

        probes.Resolver.ExpectMsg<ResolveRuleSet>();
        probes.Resolver.Reply(new RuleSetResolved("tatort", "Tatort"));

        probes.Mediathek.ExpectMsg<QueryMediathek>();
        probes.Mediathek.Reply(new QueryMediathekCompleted([], 0));

        var result = ExpectMsg<SearchMovieCompleted>();
        Assert.Equal(searchId, result.SearchId);
        Assert.Empty(result.Items);
    }
}
