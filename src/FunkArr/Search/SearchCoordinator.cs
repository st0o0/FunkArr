using System.Diagnostics;
using System.Diagnostics.Metrics;
using Akka.Actor;
using Akka.Event;
using FunkArr.Configuration;
using FunkArr.Diagnostics;
using FunkArr.RuleSet;
using FunkArr.Shared.Models;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class SearchCoordinator : ReceiveActor, IWithStash
{
    private readonly MediathekClient _mediathekClient;
    private readonly TvdbClient _tvdbClient;
    private readonly TmdbClient _tmdbClient;
    private readonly QualityProbeService _qualityProbeService;
    private readonly int _probeLimit;
    private readonly Dictionary<string, CachedSearchResult> _cache = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(55);
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Counter<long> _searchTotal = FunkArrMetrics.Instance.AddSearchTotal();
    private readonly Histogram<double> _searchDuration = FunkArrMetrics.Instance.AddSearchDuration();
    private readonly Counter<long> _cacheHitTotal = FunkArrMetrics.Instance.AddCacheHitTotal();
    private readonly Dictionary<string, Stopwatch> _pendingTimers = new();
    private readonly Dictionary<string, PipelineState> _inflight = new();

    private IActorRef _showResolver = ActorRefs.Nobody;
    private IActorRef _mediathekGateway = ActorRefs.Nobody;
    private IActorRef _matchWorker = ActorRefs.Nobody;
    private IActorRef _qualityProbe = ActorRefs.Nobody;
    private IActorRef _scoreWorker = ActorRefs.Nobody;

    private IActorRef? _ruleSetRegistry;

    public IStash Stash { get; set; } = null!;

    public sealed record TvSearchRequest(
        int TvdbId, string? ShowName, int? Season, int? Episode, string? Query);

    public sealed record MovieSearchRequest(string? ImdbId, string? Query);
    public sealed record TextSearchRequest(string Query);
    public sealed record SearchResponse(IReadOnlyList<SearchResult> Results);

    private sealed record CachedSearchResult(
        IReadOnlyList<SearchResult> Results, DateTimeOffset CachedAt);

    private sealed record RuleSetRegistryResolved(IActorRef Ref);

    private sealed record TvShowResolvedEnvelope(string CoalesceKey, TvShowResolved Result);
    private sealed record MovieResolvedEnvelope(string CoalesceKey, MovieResolved Result);
    private sealed record RulesResponseEnvelope(string CoalesceKey, RuleSetCoordinator.RulesResponse Result);
    private sealed record ItemsFetchedEnvelope(string CoalesceKey, ItemsFetched Result);
    private sealed record ItemsMatchedEnvelope(string CoalesceKey, ItemsMatched Result);
    private sealed record UrlsProbedEnvelope(string CoalesceKey, UrlsProbed Result);
    private sealed record ResultsScoredEnvelope(string CoalesceKey, ResultsScored Result);

    public SearchCoordinator(
        MediathekClient mediathekClient,
        TvdbClient tvdbClient,
        TmdbClient tmdbClient,
        QualityProbeService qualityProbeService,
        IOptions<SearchOptions> options)
    {
        _mediathekClient = mediathekClient;
        _tvdbClient = tvdbClient;
        _tmdbClient = tmdbClient;
        _qualityProbeService = qualityProbeService;
        _probeLimit = options.Value.QualityProbeLimit;

        Resolving();
    }

    protected override void PreStart()
    {
        _showResolver = Context.ActorOf(Props.Create(() =>
            new ShowResolverWorker(_tvdbClient, _tmdbClient)), "show-resolver");
        _mediathekGateway = Context.ActorOf(Props.Create(() =>
            new MediathekGatewayWorker(_mediathekClient)), "mediathek-gateway");
        _matchWorker = Context.ActorOf(Props.Create(() =>
            new MatchWorker()), "match");
        _qualityProbe = Context.ActorOf(Props.Create(() =>
            new QualityProbeWorker(_qualityProbeService)), "quality-probe");
        _scoreWorker = Context.ActorOf(Props.Create(() =>
            new ScoreWorker()), "score");

        Context.GetActorAsync<RuleSetCoordinator>().PipeTo(Self,
            success: r => new RuleSetRegistryResolved(r));
    }

    private void Resolving()
    {
        Receive<RuleSetRegistryResolved>(msg =>
        {
            _ruleSetRegistry = msg.Ref;
            Context.Watch(_ruleSetRegistry);
            _log.Info("RuleSetCoordinator resolved, becoming ready");
            Become(Ready);
            Stash.UnstashAll();
        });
        Receive<Terminated>(HandleTerminated);
        ReceiveAny(_ => Stash.Stash());
    }

    private void Ready()
    {
        Receive<TvSearchRequest>(HandleTvSearch);
        Receive<MovieSearchRequest>(HandleMovieSearch);
        Receive<TextSearchRequest>(HandleTextSearch);

        Receive<TvShowResolvedEnvelope>(msg => OnTvShowResolved(msg.CoalesceKey, msg.Result));
        Receive<MovieResolvedEnvelope>(msg => OnMovieResolved(msg.CoalesceKey, msg.Result));
        Receive<RulesResponseEnvelope>(msg => OnRulesResolved(msg.CoalesceKey, msg.Result));
        Receive<ItemsFetchedEnvelope>(msg => OnItemsFetched(msg.CoalesceKey, msg.Result));
        Receive<ItemsMatchedEnvelope>(msg => OnItemsMatched(msg.CoalesceKey, msg.Result));
        Receive<UrlsProbedEnvelope>(msg => OnUrlsProbed(msg.CoalesceKey, msg.Result));
        Receive<ResultsScoredEnvelope>(msg => OnResultsScored(msg.CoalesceKey, msg.Result));

        Receive<RuleSetRegistryResolved>(msg =>
        {
            Context.Unwatch(_ruleSetRegistry!);
            _ruleSetRegistry = msg.Ref;
            Context.Watch(_ruleSetRegistry);
        });
        Receive<Terminated>(HandleTerminated);
    }

    // --- TV Search Pipeline ---

    private void HandleTvSearch(TvSearchRequest request)
    {
        var showName = request.ShowName;
        var searchTerm = showName ?? request.Query ?? string.Empty;
        var coalesceKey = $"tv:{request.TvdbId}:{searchTerm}";

        if (TryReplyFromCache(coalesceKey, "tv", request))
            return;

        var caller = new PendingCaller(Sender, request, null, null);

        if (_inflight.TryGetValue(coalesceKey, out var existing))
        {
            existing.Callers.Add(caller);
            return;
        }

        var state = new PipelineState
        {
            CoalesceKey = coalesceKey,
            SearchType = "tv",
            SearchTerm = searchTerm,
        };
        state.Callers.Add(caller);
        _inflight[coalesceKey] = state;
        _pendingTimers[coalesceKey] = Stopwatch.StartNew();

        _showResolver.Ask<TvShowResolved>(new ResolveTvShow(request.TvdbId, request.Season), TimeSpan.FromSeconds(10))
            .PipeTo(Self, success: r => new TvShowResolvedEnvelope(coalesceKey, r));

        _ruleSetRegistry!.Ask<RuleSetCoordinator.RulesResponse>(
            new RuleSetCoordinator.GetRulesForTopic(searchTerm, request.TvdbId), TimeSpan.FromSeconds(5))
            .PipeTo(Self, success: r => new RulesResponseEnvelope(coalesceKey, r));
    }

    private void OnTvShowResolved(string key, TvShowResolved result)
    {
        if (!_inflight.TryGetValue(key, out var state)) return;

        state.ShowName = result.ShowName;
        state.Episodes = result.Episodes;
        state.ShowResolved = true;

        if (result.ShowName is not null && state.SearchTerm != result.ShowName)
            state.SearchTerm = result.ShowName;

        TryAdvanceTvAfterResolution(key, state);
    }

    private void OnRulesResolved(string key, RuleSetCoordinator.RulesResponse result)
    {
        if (!_inflight.TryGetValue(key, out var state)) return;

        state.Rules = result.Rules;
        state.RulesResolved = true;

        TryAdvanceTvAfterResolution(key, state);
    }

    private void TryAdvanceTvAfterResolution(string key, PipelineState state)
    {
        if (!state.ShowResolved || !state.RulesResolved) return;

        _mediathekGateway.Ask<ItemsFetched>(new FetchItems(state.SearchTerm), TimeSpan.FromSeconds(30))
            .PipeTo(Self, success: r => new ItemsFetchedEnvelope(key, r));
    }

    // --- Movie Search Pipeline ---

    private void HandleMovieSearch(MovieSearchRequest request)
    {
        var searchTerm = request.Query ?? string.Empty;
        var coalesceKey = $"movie:{request.ImdbId ?? searchTerm}";

        if (TryReplyFromCache(coalesceKey, "movie", tvRequest: null, movieRequest: request))
            return;

        var caller = new PendingCaller(Sender, null, request, null);

        if (_inflight.TryGetValue(coalesceKey, out var existing))
        {
            existing.Callers.Add(caller);
            return;
        }

        var state = new PipelineState
        {
            CoalesceKey = coalesceKey,
            SearchType = "movie",
            SearchTerm = searchTerm,
        };
        state.Callers.Add(caller);
        _inflight[coalesceKey] = state;
        _pendingTimers[coalesceKey] = Stopwatch.StartNew();

        _showResolver.Ask<MovieResolved>(new ResolveMovie(request.ImdbId, searchTerm), TimeSpan.FromSeconds(10))
            .PipeTo(Self, success: r => new MovieResolvedEnvelope(coalesceKey, r));
    }

    private void OnMovieResolved(string key, MovieResolved result)
    {
        if (!_inflight.TryGetValue(key, out var state)) return;

        state.MovieInfo = result.Info;
        state.ShowResolved = true;

        var movieSearchTerm = result.Info?.Title ?? state.SearchTerm;
        state.SearchTerm = movieSearchTerm;

        _mediathekGateway.Ask<ItemsFetched>(new FetchItems(movieSearchTerm), TimeSpan.FromSeconds(30))
            .PipeTo(Self, success: r => new ItemsFetchedEnvelope(key, r));
    }

    // --- Text Search Pipeline ---

    private void HandleTextSearch(TextSearchRequest request)
    {
        var coalesceKey = $"text:{request.Query}";

        if (TryReplyFromCache(coalesceKey, "text", tvRequest: null, movieRequest: null, textRequest: request))
            return;

        var caller = new PendingCaller(Sender, null, null, request);

        if (_inflight.TryGetValue(coalesceKey, out var existing))
        {
            existing.Callers.Add(caller);
            return;
        }

        var state = new PipelineState
        {
            CoalesceKey = coalesceKey,
            SearchType = "text",
            SearchTerm = request.Query,
        };
        state.Callers.Add(caller);
        state.Rules = [];
        state.RulesResolved = true;
        state.ShowResolved = true;
        _inflight[coalesceKey] = state;
        _pendingTimers[coalesceKey] = Stopwatch.StartNew();

        _mediathekGateway.Ask<ItemsFetched>(new FetchItems(request.Query), TimeSpan.FromSeconds(30))
            .PipeTo(Self, success: r => new ItemsFetchedEnvelope(coalesceKey, r));
    }

    // --- Shared Pipeline Steps ---

    private void OnItemsFetched(string key, ItemsFetched result)
    {
        if (!_inflight.TryGetValue(key, out var state)) return;

        state.RawItems = result.Items;

        if (state.SearchType == "movie" && result.Items.Length == 0 && state.MovieInfo is not null &&
            !string.Equals(state.MovieInfo.Title, state.MovieInfo.OriginalTitle, StringComparison.OrdinalIgnoreCase))
        {
            _log.Debug("No results for '{Title}', falling back to original '{Original}'",
                state.MovieInfo.Title, state.MovieInfo.OriginalTitle);
            state.SearchTerm = state.MovieInfo.OriginalTitle;
            _mediathekGateway.Ask<ItemsFetched>(new FetchItems(state.MovieInfo.OriginalTitle), TimeSpan.FromSeconds(30))
                .PipeTo(Self, success: r => new ItemsFetchedEnvelope(key, r));
            return;
        }

        var context = BuildMatchContext(state);

        _matchWorker.Ask<ItemsMatched>(
            new MatchItems(result.Items, context, state.Rules ?? [], state.Episodes ?? [], state.ShowName),
            TimeSpan.FromSeconds(30))
            .PipeTo(Self, success: r => new ItemsMatchedEnvelope(key, r));
    }

    private void OnItemsMatched(string key, ItemsMatched result)
    {
        if (!_inflight.TryGetValue(key, out var state)) return;

        state.MatchedResults = result.Results;
        state.MatchRecord = result.Record;

        _qualityProbe.Ask<UrlsProbed>(new ProbeUrls(result.Results, _probeLimit), TimeSpan.FromSeconds(60))
            .PipeTo(Self, success: r => new UrlsProbedEnvelope(key, r));
    }

    private void OnUrlsProbed(string key, UrlsProbed result)
    {
        if (!_inflight.TryGetValue(key, out var state)) return;

        state.ProbedResults = result.Results;
        var context = BuildMatchContext(state);

        _scoreWorker.Ask<ResultsScored>(new ScoreResults(result.Results, context), TimeSpan.FromSeconds(10))
            .PipeTo(Self, success: r => new ResultsScoredEnvelope(key, r));
    }

    private void OnResultsScored(string key, ResultsScored result)
    {
        if (!_inflight.TryGetValue(key, out var state)) return;

        _inflight.Remove(key);

        CacheResults(key, result.Results);

        if (state.MatchRecord is not null)
            _ruleSetRegistry!.Tell(new MatchQualityWorker.RecordMatchResult(state.MatchRecord));

        RecordMetrics(key, state.SearchType);

        foreach (var caller in state.Callers)
        {
            var filtered = FilterForCaller(result.Results, caller, state);
            caller.Ref.Tell(new SearchResponse(filtered));
        }
    }

    // --- Helpers ---

    private MatchContext BuildMatchContext(PipelineState state)
    {
        var firstTvCaller = state.Callers.FirstOrDefault(c => c.TvRequest is not null)?.TvRequest;

        return new MatchContext
        {
            ShowName = state.ShowName ?? state.MovieInfo?.Title,
            Season = firstTvCaller?.Season,
            Episode = firstTvCaller?.Episode,
            AirDate = null,
            ImdbId = state.Callers.FirstOrDefault(c => c.MovieRequest is not null)?.MovieRequest?.ImdbId,
            ExpectedDurationSeconds = state.MovieInfo?.RuntimeMinutes * 60,
        };
    }

    private static IReadOnlyList<SearchResult> FilterForCaller(
        IReadOnlyList<SearchResult> results, PendingCaller caller, PipelineState state)
    {
        if (caller.TvRequest is { Season: not null, Episode: not null } tvReq)
        {
            return results
                .Where(r => MatchesEpisodeFilter(r, tvReq.Season.Value, tvReq.Episode.Value))
                .ToList();
        }

        return results;
    }

    private static bool MatchesEpisodeFilter(SearchResult result, int season, int episode)
    {
        var se = MatchingPipeline.ExtractSeasonEpisode(result.Title);
        if (se is not null)
            return se.Value.season == season && se.Value.episode == episode;
        return true;
    }

    private bool TryReplyFromCache(string key, string type,
        TvSearchRequest? tvRequest = null,
        MovieSearchRequest? movieRequest = null,
        TextSearchRequest? textRequest = null)
    {
        if (!_cache.TryGetValue(key, out var cached) ||
            DateTimeOffset.UtcNow - cached.CachedAt >= _cacheDuration)
            return false;

        _cacheHitTotal.Add(1, new KeyValuePair<string, object?>("type", type));

        var results = cached.Results;
        if (tvRequest is { Season: not null, Episode: not null })
        {
            results = results
                .Where(r => MatchesEpisodeFilter(r, tvRequest.Season.Value, tvRequest.Episode.Value))
                .ToList();
        }

        Sender.Tell(new SearchResponse(results));
        return true;
    }

    private void CacheResults(string key, IReadOnlyList<SearchResult> results)
    {
        _cache[key] = new CachedSearchResult(results, DateTimeOffset.UtcNow);
    }

    private void RecordMetrics(string key, string type)
    {
        _searchTotal.Add(1,
            new KeyValuePair<string, object?>("type", type),
            new KeyValuePair<string, object?>("outcome", "success"));

        if (_pendingTimers.Remove(key, out var sw))
        {
            sw.Stop();
            _searchDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("type", type));
        }
    }

    private void HandleTerminated(Terminated msg)
    {
        if (_ruleSetRegistry is not null && msg.ActorRef.Equals(_ruleSetRegistry))
        {
            _log.Warning("RuleSetCoordinator terminated, re-resolving");
            _ruleSetRegistry = null;
            Become(Resolving);
            Context.GetActorAsync<RuleSetCoordinator>().PipeTo(Self,
                success: r => new RuleSetRegistryResolved(r));
        }
    }
}
