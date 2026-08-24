using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using FunkArr.Search.Matching;
using FunkArr.Search.Quality;
using FunkArr.Search.Resolvers;
using FunkArr.Shared;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

public sealed class MovieSearchActor : SearchActorBase
{
    public sealed record Search(string? ImdbId, string? Query) : IShardedMessage
    {
        public string EntityKey => ImdbId ?? $"q:{Query}";
    }

    private sealed record MovieResolvedEnvelope(string EntityKey, MovieResolved Result);
    private sealed record ItemsFetchedEnvelope(string EntityKey, ItemsFetched Result, bool IsOriginalTitleRetry);
    private sealed record ResolveFailed(string EntityKey, Exception Error);
    private sealed record FetchFailed(string EntityKey, Exception Error);

    private readonly Dictionary<string, CachedEntry> _cache = new();
    private readonly Dictionary<string, List<IActorRef>> _pendingCallers = new();
    private readonly Dictionary<string, TmdbMovieInfo?> _resolvedMovies = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(55);
    private static readonly TimeSpan PassivationTimeout = TimeSpan.FromMinutes(60);

    private sealed record CachedEntry(IReadOnlyList<SearchResult> Results, DateTimeOffset CachedAt);

    public MovieSearchActor(IReadOnlyActorRegistry registry) : base(registry, PassivationTimeout)
    {
        Receive<Search>(HandleSearch);
        Receive<MovieResolvedEnvelope>(HandleMovieResolved);
        Receive<ItemsFetchedEnvelope>(HandleItemsFetched);
        Receive<ResolveFailed>(HandleResolveFailed);
        Receive<FetchFailed>(HandleFetchFailed);
    }

    private void HandleSearch(Search message)
    {
        var entityKey = message.EntityKey;

        if (_cache.TryGetValue(entityKey, out var cached) &&
            DateTimeOffset.UtcNow - cached.CachedAt < CacheTtl)
        {
            Sender.Tell(new SearchResponse(cached.Results));
            return;
        }

        if (_pendingCallers.TryGetValue(entityKey, out var callers))
        {
            callers.Add(Sender);
            return;
        }

        _pendingCallers[entityKey] = [Sender];

        var resolver = Registry.Get<MovieResolver>();
        resolver.Ask<MovieResolved>(new ResolveMovie(message.ImdbId, message.Query), TimeSpan.FromSeconds(10))
            .PipeTo(Self,
                success: r => new MovieResolvedEnvelope(entityKey, r),
                failure: ex => new ResolveFailed(entityKey, ex));
    }

    private void HandleMovieResolved(MovieResolvedEnvelope envelope)
    {
        var entityKey = envelope.EntityKey;
        var movieInfo = envelope.Result.Info;

        _resolvedMovies[entityKey] = movieInfo;

        var query = movieInfo?.Title;
        if (string.IsNullOrEmpty(query))
        {
            query = entityKey.StartsWith("q:", StringComparison.Ordinal)
                ? entityKey[2..]
                : entityKey;
        }

        var gateway = Registry.Get<MediathekGatewayActor>();
        gateway.Ask<ItemsFetched>(new FetchItems(query), TimeSpan.FromSeconds(30))
            .PipeTo(Self,
                success: r => new ItemsFetchedEnvelope(entityKey, r, IsOriginalTitleRetry: false),
                failure: ex => new FetchFailed(entityKey, ex));
    }

    private void HandleItemsFetched(ItemsFetchedEnvelope envelope)
    {
        var entityKey = envelope.EntityKey;
        var items = envelope.Result.Items;

        _resolvedMovies.TryGetValue(entityKey, out var movieInfo);

        if (items.Length == 0 && !envelope.IsOriginalTitleRetry && movieInfo is not null &&
            !string.Equals(movieInfo.Title, movieInfo.OriginalTitle, StringComparison.OrdinalIgnoreCase))
        {
            Log.Debug("No results for '{Title}', falling back to original '{Original}'",
                movieInfo.Title, movieInfo.OriginalTitle);

            var gateway = Registry.Get<MediathekGatewayActor>();
            gateway.Ask<ItemsFetched>(new FetchItems(movieInfo.OriginalTitle), TimeSpan.FromSeconds(30))
                .PipeTo(Self,
                    success: r => new ItemsFetchedEnvelope(entityKey, r, IsOriginalTitleRetry: true),
                    failure: ex => new FetchFailed(entityKey, ex));
            return;
        }

        var context = new MatchContext
        {
            ShowName = movieInfo?.Title,
            ImdbId = entityKey.StartsWith("q:", StringComparison.Ordinal) ? null : entityKey,
            ExpectedDurationSeconds = movieInfo?.RuntimeMinutes * 60,
        };

        var results = ExpandQualities(items, context);

        _cache[entityKey] = new CachedEntry(results, DateTimeOffset.UtcNow);
        _resolvedMovies.Remove(entityKey);

        if (_pendingCallers.Remove(entityKey, out var callers))
        {
            var response = new SearchResponse(results);
            foreach (var caller in callers)
            {
                caller.Tell(response);
            }
        }
    }

    private void HandleResolveFailed(ResolveFailed failure)
    {
        Log.Warning(failure.Error, "Resolve failed for entity '{EntityKey}'", failure.EntityKey);
        _resolvedMovies.Remove(failure.EntityKey);
        if (_pendingCallers.Remove(failure.EntityKey, out var callers))
        {
            var response = new SearchResponse([]);
            foreach (var caller in callers)
            {
                caller.Tell(response);
            }
        }
    }

    private void HandleFetchFailed(FetchFailed failure)
    {
        Log.Warning(failure.Error, "Fetch failed for entity '{EntityKey}'", failure.EntityKey);
        _resolvedMovies.Remove(failure.EntityKey);
        if (_pendingCallers.Remove(failure.EntityKey, out var callers))
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
