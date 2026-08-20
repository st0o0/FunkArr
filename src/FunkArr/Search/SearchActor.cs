using System.Globalization;
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
        IOptions<FunkArrOptions> options)
    {
        _mediathekClient = mediathekClient;
        _tvdbClient = tvdbClient;
        _qualityProbeService = qualityProbeService;
        _probeLimit = options.Value.QualityProbeLimit;

        Resolving();
    }

    protected override void PreStart()
    {
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
        ReceiveAsync<MovieSearchRequest>(HandleMovieSearch);
        ReceiveAsync<TextSearchRequest>(HandleTextSearch);
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
        DateTimeOffset? airDate = null;

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

        var results = await SearchMediathekAsync(searchTerm);

        var rulesResponse = await _ruleSetRegistry!.Ask<RuleSetRegistryActor.RulesResponse>(
            new RuleSetRegistryActor.GetRulesForTopic(searchTerm, request.TvdbId),
            TimeSpan.FromSeconds(5));

        IReadOnlyList<SearchResult> filtered;

        if (rulesResponse.Rules.Count > 0)
        {
            filtered = await ApplyRuleSetMatchingWithTraces(
                results, rulesResponse.Rules,
                await GetTvdbEpisodesAsync(request.TvdbId, request.Season),
                showName ?? searchTerm, searchTerm, request.TvdbId,
                request.Season, request.Episode, "ruleset");
        }
        else
        {
            if (request.Season is not null && request.Episode is not null && request.TvdbId > 0)
            {
                var episodes = await _tvdbClient.GetEpisodesAsync(request.TvdbId, request.Season.Value);
                var ep = episodes?.FirstOrDefault(e =>
                    e.AiredSeason == request.Season && e.AiredEpisodeNumber == request.Episode);
                if (ep is not null && DateTime.TryParseExact(
                        ep.FirstAired, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    airDate = new DateTimeOffset(date, TimeSpan.Zero);
                }
            }

            var context = new MatchContext
            {
                ShowName = showName,
                Season = request.Season,
                Episode = request.Episode,
                AirDate = airDate,
            };
            filtered = await MatchingPipeline.ExecuteAsync(results, context, _qualityProbeService, _probeLimit);

            EmitGenericPipelineRecord(searchTerm, request.TvdbId, request.Season, request.Episode,
                results.Length, filtered.Count);
        }

        CacheResults(cacheKey, filtered);
        Sender.Tell(new SearchResponse(filtered));
    }

    private async Task HandleMovieSearch(MovieSearchRequest request)
    {
        var searchTerm = request.Query ?? string.Empty;
        var cacheKey = $"movie:{request.ImdbId}:{searchTerm}";

        if (TryGetCached(cacheKey, out var cached))
        {
            Sender.Tell(new SearchResponse(cached));
            return;
        }

        var context = new MatchContext
        {
            ShowName = request.Query,
            ImdbId = request.ImdbId,
        };

        var results = await SearchMediathekAsync(searchTerm);
        var filtered = await MatchingPipeline.ExecuteAsync(results, context, _qualityProbeService, _probeLimit);

        EmitGenericPipelineRecord(searchTerm, null, null, null, results.Length, filtered.Count);

        CacheResults(cacheKey, filtered);
        Sender.Tell(new SearchResponse(filtered));
    }

    private async Task HandleTextSearch(TextSearchRequest request)
    {
        var cacheKey = $"text:{request.Query}";

        if (TryGetCached(cacheKey, out var cached))
        {
            Sender.Tell(new SearchResponse(cached));
            return;
        }

        var context = new MatchContext();

        var results = await SearchMediathekAsync(request.Query);
        var filtered = await MatchingPipeline.ExecuteAsync(results, context, _qualityProbeService, _probeLimit);

        EmitGenericPipelineRecord(request.Query, null, null, null, results.Length, filtered.Count);

        CacheResults(cacheKey, filtered);
        Sender.Tell(new SearchResponse(filtered));
    }

    private async Task<IReadOnlyList<SearchResult>> ApplyRuleSetMatchingWithTraces(
        MediathekResultItem[] items,
        IReadOnlyList<Rule> rules,
        TvdbEpisodeInfo[] tvdbEpisodes,
        string showName,
        string searchTopic,
        int? tvdbId,
        int? season,
        int? episode,
        string source)
    {
        var (matches, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            items, rules, tvdbEpisodes, showName);

        var matchedItems = new HashSet<string>(
            traces.OfType<MatchedTrace>().Select(t => t.ItemTitle));

        var searchResults = new List<SearchResult>();
        var count = 0;
        foreach (var item in items)
        {
            if (matchedItems.Contains(item.Title))
            {
                var variants = await _qualityProbeService.ExpandWithProbingAsync(item, _probeLimit, count);
                searchResults.AddRange(variants);
                count += variants.Count;
            }
        }

        var record = new MatchRecord
        {
            Id = Guid.NewGuid().ToString("N")[..10],
            Timestamp = DateTimeOffset.UtcNow,
            SearchTopic = searchTopic,
            TvdbId = tvdbId,
            Season = season,
            Episode = episode,
            Source = source,
            TotalResults = items.Length,
            Matched = traces.OfType<MatchedTrace>().ToList(),
            Filtered = traces.OfType<FilteredTrace>().ToList(),
            Unmatched = traces.OfType<UnmatchedTrace>().ToList(),
        };

        _matchLedger!.Tell(new MatchLedgerActor.RecordMatchResult(record));

        _log.Debug(
            "Match result for '{Topic}': {Matched} matched, {Filtered} filtered, {Unmatched} unmatched out of {Total}",
            searchTopic, record.Matched.Count, record.Filtered.Count, record.Unmatched.Count, items.Length);

        return searchResults
            .OrderByDescending(r => r.Quality)
            .ToList();
    }

    private void EmitGenericPipelineRecord(
        string searchTopic, int? tvdbId, int? season, int? episode,
        int totalResults, int matchedCount)
    {
        var record = new MatchRecord
        {
            Id = Guid.NewGuid().ToString("N")[..10],
            Timestamp = DateTimeOffset.UtcNow,
            SearchTopic = searchTopic,
            TvdbId = tvdbId,
            Season = season,
            Episode = episode,
            Source = "generic-pipeline",
            TotalResults = totalResults,
            Matched = [],
            Filtered = [],
            Unmatched = [],
        };

        _matchLedger!.Tell(new MatchLedgerActor.RecordMatchResult(record));

        _log.Debug(
            "Generic pipeline result for '{Topic}': {Matched}/{Total} results",
            searchTopic, matchedCount, totalResults);
    }

    private async Task<TvdbEpisodeInfo[]> GetTvdbEpisodesAsync(int tvdbId, int? season)
    {
        if (tvdbId <= 0)
        {
            return [];
        }

        if (season is not null)
        {
            var episodes = await _tvdbClient.GetEpisodesAsync(tvdbId, season.Value);
            return episodes ?? [];
        }

        var allEpisodes = await _tvdbClient.GetEpisodesAsync(tvdbId, 1);
        return allEpisodes ?? [];
    }

    private async Task<MediathekResultItem[]> SearchMediathekAsync(string searchTerm)
    {
        try
        {
            var query = new MediathekQuery
            {
                Queries =
                [
                    new MediathekQueryItem { Fields = ["topic", "title"], Query = searchTerm },
                ],
            };

            var response = await _mediathekClient.QueryAsync(query);
            return response?.Result ?? [];
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "MediathekViewWeb query failed for '{SearchTerm}'", searchTerm);
            return [];
        }
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
