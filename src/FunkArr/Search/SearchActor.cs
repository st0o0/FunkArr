using Akka.Actor;
using Akka.Event;
using FunkArr.Configuration;
using FunkArr.RuleSet;
using FunkArr.Shared.Models;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.Search;

public sealed class SearchActor : ReceiveActor, IWithStash
{
    private readonly MediathekClient _mediathekClient;
    private readonly TvdbClient _tvdbClient;
    private readonly QualityProbeService _qualityProbeService;
    private readonly int _probeLimit;
    private readonly Dictionary<string, CachedSearchResult> _cache = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(55);
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private IActorRef _tvSearchActor = null!;
    private IActorRef _movieSearchActor = null!;
    private IActorRef _textSearchActor = null!;

    private IActorRef? _ruleSetRegistry;
    private IActorRef? _matchLedger;

    public IStash Stash { get; set; } = null!;

    public sealed record TvSearchRequest(
        int TvdbId, string? ShowName, int? Season, int? Episode, string? Query);

    public sealed record MovieSearchRequest(string? ImdbId, string? Query);
    public sealed record TextSearchRequest(string Query);
    public sealed record SearchResponse(IReadOnlyList<SearchResult> Results);

    private sealed record CachedSearchResult(
        IReadOnlyList<SearchResult> Results, DateTimeOffset CachedAt);

    private sealed record RuleSetRegistryResolved(IActorRef Ref);
    private sealed record MatchLedgerResolved(IActorRef Ref);

    public SearchActor(
        MediathekClient mediathekClient,
        TvdbClient tvdbClient,
        QualityProbeService qualityProbeService,
        IOptions<SearchOptions> options)
    {
        _mediathekClient = mediathekClient;
        _tvdbClient = tvdbClient;
        _qualityProbeService = qualityProbeService;
        _probeLimit = options.Value.QualityProbeLimit;

        Resolving();
    }

    protected override void PreStart()
    {
        _tvSearchActor = Context.ActorOf(Props.Create(() =>
            new TvSearchActor(_mediathekClient, _tvdbClient, _qualityProbeService, _probeLimit)), "tv");
        _movieSearchActor = Context.ActorOf(Props.Create(() =>
            new MovieSearchActor(_mediathekClient, _qualityProbeService, _probeLimit)), "movie");
        _textSearchActor = Context.ActorOf(Props.Create(() =>
            new TextSearchActor(_mediathekClient, _qualityProbeService, _probeLimit)), "text");

        Context.GetActorAsync<RuleSetRegistryActor>().PipeTo(Self,
            success: r => new RuleSetRegistryResolved(r));
        Context.GetActorAsync<MatchLedgerActor>().PipeTo(Self,
            success: r => new MatchLedgerResolved(r));
    }

    private void Resolving()
    {
        Receive<RuleSetRegistryResolved>(msg =>
        {
            _ruleSetRegistry = msg.Ref;
            Context.Watch(_ruleSetRegistry);
            TryBecomeReady();
        });
        Receive<MatchLedgerResolved>(msg =>
        {
            _matchLedger = msg.Ref;
            Context.Watch(_matchLedger);
            TryBecomeReady();
        });
        Receive<Terminated>(HandleTerminated);
        ReceiveAny(_ => Stash.Stash());
    }

    private void TryBecomeReady()
    {
        if (_ruleSetRegistry is not null && _matchLedger is not null)
        {
            _log.Info("All dependencies resolved, becoming ready");
            Become(Ready);
            Stash.UnstashAll();
        }
    }

    private void Ready()
    {
        ReceiveAsync<TvSearchRequest>(HandleTvSearch);
        Receive<MovieSearchRequest>(HandleMovieSearch);
        Receive<TextSearchRequest>(HandleTextSearch);
        Receive<SearchCompleted>(HandleSearchCompleted);
        Receive<RuleSetRegistryResolved>(msg =>
        {
            Context.Unwatch(_ruleSetRegistry!);
            _ruleSetRegistry = msg.Ref;
            Context.Watch(_ruleSetRegistry);
        });
        Receive<MatchLedgerResolved>(msg =>
        {
            Context.Unwatch(_matchLedger!);
            _matchLedger = msg.Ref;
            Context.Watch(_matchLedger);
        });
        Receive<Terminated>(HandleTerminated);
    }

    private void HandleTerminated(Terminated msg)
    {
        if (_ruleSetRegistry is not null && msg.ActorRef.Equals(_ruleSetRegistry))
        {
            _log.Warning("RuleSetRegistryActor terminated, re-resolving");
            _ruleSetRegistry = null;
            Context.GetActorAsync<RuleSetRegistryActor>().PipeTo(Self,
                success: r => new RuleSetRegistryResolved(r));
        }

        if (_matchLedger is not null && msg.ActorRef.Equals(_matchLedger))
        {
            _log.Warning("MatchLedgerActor terminated, re-resolving");
            _matchLedger = null;
            Context.GetActorAsync<MatchLedgerActor>().PipeTo(Self,
                success: r => new MatchLedgerResolved(r));
        }

        if (_ruleSetRegistry is null || _matchLedger is null)
        {
            Become(Resolving);
        }
    }

    private async Task HandleTvSearch(TvSearchRequest request)
    {
        var showName = request.ShowName;

        if (showName is null && request.TvdbId > 0)
        {
            var show = await _tvdbClient.GetShowAsync(request.TvdbId);
            if (show is not null)
            {
                showName = show.SeriesName;
            }
        }

        var searchTerm = showName ?? request.Query ?? string.Empty;
        var cacheKey = $"tv:{request.TvdbId}:{request.Season}:{request.Episode}:{searchTerm}";

        if (TryGetCached(cacheKey, out var cached))
        {
            Sender.Tell(new SearchResponse(cached));
            return;
        }

        var rulesResponse = await _ruleSetRegistry!.Ask<RuleSetRegistryActor.RulesResponse>(
            new RuleSetRegistryActor.GetRulesForTopic(searchTerm, request.TvdbId),
            TimeSpan.FromSeconds(5));

        _tvSearchActor.Tell(new ExecuteTvSearch(
            cacheKey, request, searchTerm, showName, rulesResponse.Rules, Sender));
    }

    private void HandleMovieSearch(MovieSearchRequest request)
    {
        var searchTerm = request.Query ?? string.Empty;
        var cacheKey = $"movie:{request.ImdbId}:{searchTerm}";

        if (TryGetCached(cacheKey, out var cached))
        {
            Sender.Tell(new SearchResponse(cached));
            return;
        }

        _movieSearchActor.Tell(new ExecuteMovieSearch(cacheKey, request, searchTerm, Sender));
    }

    private void HandleTextSearch(TextSearchRequest request)
    {
        var cacheKey = $"text:{request.Query}";

        if (TryGetCached(cacheKey, out var cached))
        {
            Sender.Tell(new SearchResponse(cached));
            return;
        }

        _textSearchActor.Tell(new ExecuteTextSearch(cacheKey, request, Sender));
    }

    private void HandleSearchCompleted(SearchCompleted msg)
    {
        CacheResults(msg.CacheKey, msg.Results);

        if (msg.MatchRecord is not null)
        {
            _matchLedger!.Tell(new MatchLedgerActor.RecordMatchResult(msg.MatchRecord));
        }

        msg.ReplyTo.Tell(new SearchResponse(msg.Results));
    }

    private bool TryGetCached(string key, out IReadOnlyList<SearchResult> results)
    {
        if (_cache.TryGetValue(key, out var cached) &&
            DateTimeOffset.UtcNow - cached.CachedAt < _cacheDuration)
        {
            results = cached.Results;
            return true;
        }

        results = [];
        return false;
    }

    private void CacheResults(string key, IReadOnlyList<SearchResult> results)
    {
        _cache[key] = new CachedSearchResult(results, DateTimeOffset.UtcNow);
    }
}
