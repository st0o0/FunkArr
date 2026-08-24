using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using FunkArr.RuleSet;
using FunkArr.Search.Matching;
using FunkArr.Search.Quality;
using FunkArr.Search.Resolvers;
using FunkArr.Shared;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

public sealed class TvSearchActor : SearchActorBase
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(55);

    private readonly Dictionary<int, CachedResult> _cache = new();
    private readonly Dictionary<int, PendingPipeline> _inflight = new();

    public sealed record Search(int TvdbId, string? ShowName, int? Season, int? Episode, string? Query) : IShardedMessage
    {
        public string EntityKey => TvdbId.ToString();
    }

    private sealed record CachedResult(IReadOnlyList<SearchResult> Results, DateTimeOffset CachedAt);

    private sealed record TvShowResolvedEnvelope(int TvdbId, TvShowResolved Result);
    private sealed record RulesResolvedEnvelope(int TvdbId, RuleSetActor.RulesResponse Result);
    private sealed record ItemsFetchedEnvelope(int TvdbId, ItemsFetched Result);
    private sealed record ShowResolveFailed(int TvdbId, Exception Error);
    private sealed record RulesResolveFailed(int TvdbId, Exception Error);
    private sealed record FetchFailed(int TvdbId, Exception Error);

    private sealed record PendingCaller(IActorRef Ref, int? Season, int? Episode);

    private sealed class PendingPipeline
    {
        public List<PendingCaller> Callers { get; } = [];
        public string SearchTerm { get; set; } = string.Empty;
        public string? ShowName { get; set; }
        public TvdbEpisodeInfo[]? Episodes { get; set; }
        public IReadOnlyList<Rule>? Rules { get; set; }
        public bool ShowResolved { get; set; }
        public bool RulesResolved { get; set; }
    }

    public TvSearchActor(IReadOnlyActorRegistry registry) : base(registry, TimeSpan.FromMinutes(120))
    {
        Receive<Search>(HandleSearch);
        Receive<TvShowResolvedEnvelope>(msg => OnTvShowResolved(msg.TvdbId, msg.Result));
        Receive<RulesResolvedEnvelope>(msg => OnRulesResolved(msg.TvdbId, msg.Result));
        Receive<ItemsFetchedEnvelope>(msg => OnItemsFetched(msg.TvdbId, msg.Result));
        Receive<ShowResolveFailed>(HandleShowResolveFailed);
        Receive<RulesResolveFailed>(HandleRulesResolveFailed);
        Receive<FetchFailed>(HandleFetchFailed);
    }

    private void HandleSearch(Search request)
    {
        var tvdbId = request.TvdbId;
        var caller = new PendingCaller(Sender, request.Season, request.Episode);

        if (TryReplyFromCache(tvdbId, caller))
        {
            return;
        }

        if (_inflight.TryGetValue(tvdbId, out var existing))
        {
            existing.Callers.Add(caller);
            return;
        }

        var searchTerm = request.ShowName ?? request.Query ?? string.Empty;

        var state = new PendingPipeline { SearchTerm = searchTerm };
        state.Callers.Add(caller);
        _inflight[tvdbId] = state;

        var seriesResolver = Registry.Get<SeriesResolver>();
        seriesResolver.Ask<TvShowResolved>(new ResolveTvShow(tvdbId, request.Season), TimeSpan.FromSeconds(10))
            .PipeTo(Self,
                success: r => new TvShowResolvedEnvelope(tvdbId, r),
                failure: ex => new ShowResolveFailed(tvdbId, ex));

        var ruleSetActor = Registry.Get<RuleSetActor>();
        ruleSetActor.Ask<RuleSetActor.RulesResponse>(
                new RuleSetActor.GetRulesForTopic(searchTerm, tvdbId), TimeSpan.FromSeconds(5))
            .PipeTo(Self,
                success: r => new RulesResolvedEnvelope(tvdbId, r),
                failure: ex => new RulesResolveFailed(tvdbId, ex));
    }

    private void OnTvShowResolved(int tvdbId, TvShowResolved result)
    {
        if (!_inflight.TryGetValue(tvdbId, out var state))
        {
            return;
        }

        state.ShowName = result.ShowName;
        state.Episodes = result.Episodes;
        state.ShowResolved = true;

        if (result.ShowName is not null && state.SearchTerm != result.ShowName)
        {
            state.SearchTerm = result.ShowName;
        }

        TryAdvanceAfterResolution(tvdbId, state);
    }

    private void OnRulesResolved(int tvdbId, RuleSetActor.RulesResponse result)
    {
        if (!_inflight.TryGetValue(tvdbId, out var state))
        {
            return;
        }

        state.Rules = result.Rules;
        state.RulesResolved = true;

        TryAdvanceAfterResolution(tvdbId, state);
    }

    private void TryAdvanceAfterResolution(int tvdbId, PendingPipeline state)
    {
        if (!state.ShowResolved || !state.RulesResolved)
        {
            return;
        }

        var mediathekGateway = Registry.Get<MediathekGatewayActor>();
        mediathekGateway.Ask<ItemsFetched>(new FetchItems(state.SearchTerm), TimeSpan.FromSeconds(30))
            .PipeTo(Self,
                success: r => new ItemsFetchedEnvelope(tvdbId, r),
                failure: ex => new FetchFailed(tvdbId, ex));
    }

    private void OnItemsFetched(int tvdbId, ItemsFetched result)
    {
        if (!_inflight.TryGetValue(tvdbId, out var state))
        {
            return;
        }

        var rules = state.Rules ?? [];
        var episodes = state.Episodes ?? [];
        var showName = state.ShowName;

        IReadOnlyList<SearchResult> results;

        if (rules.Count > 0)
        {
            results = ExecuteRuleSetPath(result.Items, rules, episodes, showName ?? string.Empty);
        }
        else
        {
            var context = new MatchContext
            {
                ShowName = showName,
                Season = state.Callers.FirstOrDefault()?.Season,
                Episode = state.Callers.FirstOrDefault()?.Episode,
            };
            results = MatchingPipeline.Execute(result.Items, context);
        }

        _inflight.Remove(tvdbId);

        CacheResults(tvdbId, results);

        foreach (var caller in state.Callers)
        {
            var filtered = FilterForCaller(results, caller);
            caller.Ref.Tell(new SearchResponse(filtered));
        }
    }

    private void HandleShowResolveFailed(ShowResolveFailed failure)
    {
        Log.Warning(failure.Error, "Show resolve failed for tvdbId {TvdbId}", failure.TvdbId);
        if (_inflight.Remove(failure.TvdbId, out var state))
        {
            var response = new SearchResponse([]);
            foreach (var caller in state.Callers)
            {
                caller.Ref.Tell(response);
            }
        }
    }

    private void HandleRulesResolveFailed(RulesResolveFailed failure)
    {
        Log.Warning(failure.Error, "Rules resolve failed for tvdbId {TvdbId}", failure.TvdbId);
        if (_inflight.Remove(failure.TvdbId, out var state))
        {
            var response = new SearchResponse([]);
            foreach (var caller in state.Callers)
            {
                caller.Ref.Tell(response);
            }
        }
    }

    private void HandleFetchFailed(FetchFailed failure)
    {
        Log.Warning(failure.Error, "Fetch failed for tvdbId {TvdbId}", failure.TvdbId);
        if (_inflight.Remove(failure.TvdbId, out var state))
        {
            var response = new SearchResponse([]);
            foreach (var caller in state.Callers)
            {
                caller.Ref.Tell(response);
            }
        }
    }

    private static IReadOnlyList<SearchResult> ExecuteRuleSetPath(
        MediathekResultItem[] items,
        IReadOnlyList<Rule> rules,
        TvdbEpisodeInfo[] episodes,
        string showName)
    {
        var (_, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            items, rules, episodes, showName);

        var matchedTitles = new HashSet<string>(
            traces.OfType<MatchedTrace>().Select(t => t.ItemTitle));

        var results = new List<SearchResult>();

        foreach (var item in items)
        {
            if (!matchedTitles.Contains(item.Title))
            {
                continue;
            }

            var timestamp = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp);
            var subtitle = string.IsNullOrEmpty(item.UrlSubtitle) ? null : item.UrlSubtitle;
            var description = string.IsNullOrEmpty(item.Description) ? null : item.Description;

            var urls = new (string? Url, QualityTier FallbackTier)[]
            {
                (item.UrlVideoHd, QualityTier.HD1080),
                (item.UrlVideo, QualityTier.HD720),
                (item.UrlVideoLow, QualityTier.SD),
            };

            foreach (var (url, fallbackTier) in urls)
            {
                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                var pattern = UrlPatternAnalyzer.Analyze(url);
                var quality = pattern?.Resolution is not null
                    ? pattern.Resolution.Value.Height switch
                    {
                        >= 1080 => QualityTier.HD1080,
                        >= 720 => QualityTier.HD720,
                        _ => QualityTier.SD,
                    }
                    : fallbackTier;

                var sizeBytes = pattern?.BitrateKbps is > 0
                    ? (long)item.Duration * pattern.BitrateKbps.Value * 1000 / 8
                    : QualityProbeService.EstimateSize(item.Duration, quality);

                results.Add(new SearchResult
                {
                    Title = item.Title,
                    Topic = item.Topic,
                    Channel = item.Channel,
                    Url = url,
                    UrlSubtitle = subtitle,
                    Description = description,
                    DurationSeconds = item.Duration,
                    SizeBytes = sizeBytes,
                    Timestamp = timestamp,
                    Quality = quality,
                });
            }
        }

        return results;
    }

    private static IReadOnlyList<SearchResult> FilterForCaller(
        IReadOnlyList<SearchResult> results, PendingCaller caller)
    {
        if (caller is { Season: not null, Episode: not null })
        {
            return results
                .Where(r => MatchesEpisodeFilter(r, caller.Season.Value, caller.Episode.Value))
                .ToList();
        }

        return results;
    }

    private static bool MatchesEpisodeFilter(SearchResult result, int season, int episode)
    {
        var se = MatchingPipeline.ExtractSeasonEpisode(result.Title);
        if (se is not null)
        {
            return se.Value.season == season && se.Value.episode == episode;
        }

        return true;
    }

    private bool TryReplyFromCache(int tvdbId, PendingCaller caller)
    {
        if (!_cache.TryGetValue(tvdbId, out var cached) ||
            DateTimeOffset.UtcNow - cached.CachedAt >= _cacheDuration)
        {
            return false;
        }

        var filtered = FilterForCaller(cached.Results, caller);
        caller.Ref.Tell(new SearchResponse(filtered));
        return true;
    }

    private void CacheResults(int tvdbId, IReadOnlyList<SearchResult> results)
    {
        _cache[tvdbId] = new CachedResult(results, DateTimeOffset.UtcNow);
    }
}
