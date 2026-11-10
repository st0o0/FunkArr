using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.ArrApi.Newznab.Requests;
using FunkArr.Core.Actors;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Search;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi.Newznab.Services;

public sealed class NewznabSearchService(
    IActorRegistry registry,
    IMemoryCache cache,
    IOptions<ArrApiOptions> arrApiOptions,
    RssItemBuilder rssItemBuilder,
    TimeProvider timeProvider,
    ILogger<NewznabSearchService> logger)
{
    internal const int MaxLimit = 500;
    internal const int DefaultLimit = 100;

    internal async Task<SearchServiceResult> Search(IndexerRequest req, string baseUrl, string apiKey, CancellationToken ct)
    {
        req = MapTitleToQuery(req);

        if (IsWithoutSearchCriteria(req))
        {
            return await FeedOrValidation(req, baseUrl, apiKey, ct);
        }

        var (cmd, category) = BuildCommand(req);

        if (cmd is null)
        {
            return new SearchServiceResult.Empty(req.Offset ?? 0);
        }

        var offset = req.Offset ?? 0;
        var limit = req.Limit ?? DefaultLimit;
        var key = BuildKey(cmd);
        try
        {
            var gateway = await registry.GetAsync<ISearchManager>(ct);
            var cacheTtl = TimeSpan.FromSeconds(arrApiOptions.Value.SearchCacheTtlSeconds);
            var allItems = await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = cacheTtl;
                entry.Size = 1;
                var fullCmd = cmd with { Offset = null, Limit = null };
                var response = await gateway.Ask<SearchCommandResponse>(fullCmd, ct);
                return response is SearchCommandCompleted completed ? completed.Items : [];
            }) ?? [];

            var filtered = FilterBySeasonEpisode(allItems, cmd.Params as SearchCommand.TvParams);
            var paged = filtered.Skip(offset).Take(limit).ToArray();
            var rss = rssItemBuilder.ToRss(paged, filtered.Length, offset, baseUrl, apiKey, category);
            return new SearchServiceResult.Success(rss);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Search timed out for {SearchType} query {Query}", cmd.Source, cmd.Query);
            return new SearchServiceResult.Failed("Search timed out");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search failed for {SearchType} query {Query}", cmd.Source, cmd.Query);
            return new SearchServiceResult.Failed("Search failed");
        }
    }

    internal static SearchResultItem[] FilterBySeasonEpisode(SearchResultItem[] items, SearchCommand.TvParams? tvParams)
    {
        if (tvParams is null)
        {
            return items;
        }

        var hasSeason = tvParams.Season is not null;
        var hasEpisode = tvParams.Episode is not null;

        if (!hasSeason)
        {
            return items;
        }

        var season = tvParams.Season!.Value;

        if (hasEpisode)
        {
            var episode = tvParams.Episode!.Value;

            if (season >= 1900 && TryParseMmdd(season, episode, out var dailyDate))
            {
                return [.. items.Where(i => i.AiredAt?.Date == dailyDate)];
            }

            return
            [
                .. items.Where(i =>
                    int.TryParse(i.Metadata?.Season, out var s) && s == season &&
                    int.TryParse(i.Metadata?.Episode, out var e) && e == episode)
            ];
        }

        return [.. items.Where(i => int.TryParse(i.Metadata?.Season, out var s) && s == season)];
    }

    internal static bool TryParseMmdd(int year, int mmdd, out DateTime date)
    {
        var month = mmdd / 100;
        var day = mmdd % 100;
        return DateTime.TryParse($"{year}-{month:D2}-{day:D2}", out date);
    }

    private static (SearchCommand? Cmd, NewznabCategory Category) BuildCommand(IndexerRequest req) =>
        (req.T ?? "") switch
        {
            "tvsearch" => BuildTvSearch(req),
            "movie" => BuildMovieSearch(req),
            "search" => BuildGeneralSearch(req),
            _ => (null, NewznabCategory.Tv)
        };

    private static (SearchCommand?, NewznabCategory) BuildTvSearch(IndexerRequest req)
    {
        var cmd = new SearchCommand(SearchSource.Sonarr, req.Q, null,
            CapLimit(req.Limit), req.Offset,
            new SearchCommand.TvParams(
                ParseInt(req.Season),
                ParseInt(req.Ep),
                ParseInt(req.TvdbId),
                req.ImdbId));

        return (cmd, NewznabCategory.Tv);
    }

    private static (SearchCommand?, NewznabCategory) BuildMovieSearch(IndexerRequest req)
    {
        var cmd = new SearchCommand(SearchSource.Radarr, req.Q, null,
            CapLimit(req.Limit), req.Offset,
            new SearchCommand.MovieParams(req.ImdbId, ParseInt(req.TmdbId)));

        return (cmd, NewznabCategory.Movie);
    }

    private static (SearchCommand?, NewznabCategory) BuildGeneralSearch(IndexerRequest req)
    {
        var cat = ParseInt(req.Cat);
        var category = NewznabCategory.FromCat(cat) ?? NewznabCategory.Tv;
        var cmd = new SearchCommand(SearchSource.Prowlarr, req.Q, cat, CapLimit(req.Limit), req.Offset, null);
        return (cmd, category);
    }

    private async Task<SearchServiceResult> FeedOrValidation(IndexerRequest req, string baseUrl, string apiKey, CancellationToken ct)
    {
        var offset = Math.Max(0, req.Offset ?? 0);
        var limit = CapLimit(req.Limit) ?? DefaultLimit;
        var feedAnswered = false;

        try
        {
            var feed = await registry.GetAsync<IFeedStoreManager>(ct);
            var response = await feed.Ask<FeedResponse>(new QueryFeed(FeedMediaType(req), offset, limit), ct);
            feedAnswered = response is FeedResult;
            if (response is FeedResult { FilteredTotal: > 0 } result)
            {
                var entries = FilterFeedByQuery(result.Entries, req.Q);
                var total = string.IsNullOrWhiteSpace(req.Q) ? result.FilteredTotal : entries.Length;
                if (total > 0)
                {
                    return new SearchServiceResult.Success(rssItemBuilder.ToFeedRss(entries, total, offset, baseUrl, apiKey));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Feed unavailable for {Function} request, answering with a validation item", req.T);
        }

        var validationCategory = ValidationCategory(req);
        logger.LogDebug("Answering {Function} request without feed entries with a validation item ({Category})",
            req.T, validationCategory.Label);
        return new SearchServiceResult.Success(
            ValidationRss.Create(validationCategory, offset, timeProvider.GetUtcNow(), baseUrl, apiKey),
            Cacheable: feedAnswered);
    }

    internal static MediaType? FeedMediaType(IndexerRequest req) => req.T switch
    {
        "tvsearch" => MediaType.Show,
        "movie" => MediaType.Movie,
        "search" when RequestsOnlyMovieCategories(req.Cat) => MediaType.Movie,
        "search" when RequestsOnlyTvCategories(req.Cat) => MediaType.Show,
        _ => null
    };

    internal static bool RequestsOnlyTvCategories(string? cat)
    {
        var categories = ParseCategories(cat);
        return categories.Length > 0 && categories.All(c => NewznabCategory.FromCat(c) == NewznabCategory.Tv);
    }

    private static int[] ParseCategories(string? cat) =>
    [
        .. (cat ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseInt)
            .OfType<int>()
    ];

    internal static FunkArr.Messages.Feed.FeedEntry[] FilterFeedByQuery(
        FunkArr.Messages.Feed.FeedEntry[] entries, string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? entries
            : [.. entries.Where(e => e.Topic.Contains(query, StringComparison.OrdinalIgnoreCase))];

    internal static IndexerRequest MapTitleToQuery(IndexerRequest req) =>
        string.IsNullOrWhiteSpace(req.Q) && !string.IsNullOrWhiteSpace(req.Title)
            ? req with { Q = req.Title }
            : req;

    internal static bool IsWithoutSearchCriteria(IndexerRequest req) =>
        req.T is "tvsearch" or "movie" or "search"
        && string.IsNullOrWhiteSpace(req.Q)
        && string.IsNullOrWhiteSpace(req.TvdbId)
        && string.IsNullOrWhiteSpace(req.ImdbId)
        && string.IsNullOrWhiteSpace(req.TmdbId);

    internal static NewznabCategory ValidationCategory(IndexerRequest req) => req.T switch
    {
        "movie" => NewznabCategory.Movie,
        "search" when RequestsOnlyMovieCategories(req.Cat) => NewznabCategory.Movie,
        _ => NewznabCategory.Tv
    };

    internal static bool RequestsOnlyMovieCategories(string? cat)
    {
        var categories = ParseCategories(cat);
        return categories.Length > 0 && categories.All(c => NewznabCategory.FromCat(c) == NewznabCategory.Movie);
    }

    internal static int? CapLimit(int? limit) => limit switch
    {
        null => null,
        > MaxLimit => MaxLimit,
        _ => limit
    };

    internal static int? ParseInt(string? value) =>
        int.TryParse(value, out var result) ? result : null;

    internal static string BuildKey(SearchCommand cmd) => cmd.Params switch
    {
        SearchCommand.TvParams tv =>
            $"newznab:tv:{Normalize(cmd.Query)}:{tv.TvdbId}:{tv.ImdbId}",
        SearchCommand.MovieParams movie =>
            $"newznab:movie:{Normalize(cmd.Query)}:{movie.ImdbId}:{movie.TmdbId}",
        _ =>
            $"newznab:general:{Normalize(cmd.Query)}:{cmd.Cat}"
    };

    private static string Normalize(string? value) =>
        value?.Trim().ToLowerInvariant() ?? "";
}
