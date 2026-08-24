using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using FunkArr.Search.Matching;
using FunkArr.Search.Quality;
using FunkArr.Shared;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

public sealed class TextSearchActor : SearchActorBase
{
    public sealed record Search(string Query) : IShardedMessage
    {
        public string EntityKey => Query;
    }

    private sealed record ItemsFetchedEnvelope(string Query, ItemsFetched Result);
    private sealed record FetchFailed(string Query, Exception Error);

    private readonly Dictionary<string, CachedEntry> _cache = new();
    private readonly Dictionary<string, List<IActorRef>> _pendingCallers = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(55);
    private static readonly TimeSpan PassivationTimeout = TimeSpan.FromMinutes(60);

    private sealed record CachedEntry(IReadOnlyList<SearchResult> Results, DateTimeOffset CachedAt);

    public TextSearchActor(IReadOnlyActorRegistry registry) : base(registry, PassivationTimeout)
    {
        Receive<Search>(HandleSearch);
        Receive<ItemsFetchedEnvelope>(HandleItemsFetched);
        Receive<FetchFailed>(HandleFetchFailed);
    }

    private void HandleSearch(Search message)
    {
        var query = message.Query;

        if (_cache.TryGetValue(query, out var cached) &&
            DateTimeOffset.UtcNow - cached.CachedAt < CacheTtl)
        {
            Sender.Tell(new SearchResponse(cached.Results));
            return;
        }

        if (_pendingCallers.TryGetValue(query, out var callers))
        {
            callers.Add(Sender);
            return;
        }

        _pendingCallers[query] = [Sender];

        var gateway = Registry.Get<MediathekGatewayActor>();
        gateway.Ask<ItemsFetched>(new FetchItems(query), TimeSpan.FromSeconds(30))
            .PipeTo(Self,
                success: r => new ItemsFetchedEnvelope(query, r),
                failure: ex => new FetchFailed(query, ex));
    }

    private void HandleItemsFetched(ItemsFetchedEnvelope envelope)
    {
        var query = envelope.Query;
        var items = envelope.Result.Items;

        var context = new MatchContext();
        var matched = MatchingPipeline.Execute(items, context);

        var results = ExpandQualities(items, context);

        _cache[query] = new CachedEntry(results, DateTimeOffset.UtcNow);

        if (_pendingCallers.Remove(query, out var callers))
        {
            var response = new SearchResponse(results);
            foreach (var caller in callers)
            {
                caller.Tell(response);
            }
        }
    }

    private void HandleFetchFailed(FetchFailed failure)
    {
        Log.Warning(failure.Error, "Fetch failed for query '{Query}'", failure.Query);
        if (_pendingCallers.Remove(failure.Query, out var callers))
        {
            var response = new SearchResponse([]);
            foreach (var caller in callers)
            {
                caller.Tell(response);
            }
        }
    }

    private static IReadOnlyList<SearchResult> ExpandQualities(
        MediathekResultItem[] items, MatchContext context)
    {
        var filtered = items
            .Where(i => !ContentFilter.ShouldSkip(i.Title, i.Topic))
            .Where(i => MatchingPipeline.MatchesShow(i, context))
            .Where(i => MatchingPipeline.MatchesEpisode(i, context));

        var results = new List<SearchResult>();

        foreach (var item in filtered)
        {
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp);

            AddVariant(results, item, item.UrlVideoHd, QualityTier.HD1080, timestamp);
            AddVariant(results, item, item.UrlVideo, QualityTier.HD720, timestamp);
            AddVariant(results, item, item.UrlVideoLow, QualityTier.SD, timestamp);
        }

        return results
            .Select(r => MatchingPipeline.ScoreResult(r, context))
            .OrderByDescending(r => r.Score)
            .ToList();
    }

    private static void AddVariant(
        List<SearchResult> results, MediathekResultItem item,
        string url, QualityTier fallbackTier, DateTimeOffset timestamp)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        var pattern = UrlPatternAnalyzer.Analyze(url);
        var sizeBytes = pattern?.BitrateKbps is > 0
            ? (long)item.Duration * pattern.BitrateKbps.Value * 1000 / 8
            : QualityProbeService.EstimateSize(item.Duration, fallbackTier);

        results.Add(new SearchResult
        {
            Title = item.Title,
            Topic = item.Topic,
            Channel = item.Channel,
            Url = url,
            UrlSubtitle = string.IsNullOrEmpty(item.UrlSubtitle) ? null : item.UrlSubtitle,
            DurationSeconds = item.Duration,
            SizeBytes = sizeBytes,
            Timestamp = timestamp,
            Description = string.IsNullOrEmpty(item.Description) ? null : item.Description,
            Quality = fallbackTier,
        });
    }
}
