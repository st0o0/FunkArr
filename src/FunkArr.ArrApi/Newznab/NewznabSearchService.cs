using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi.Newznab;

public sealed class NewznabSearchService(
    IActorRegistry registry,
    SearchResultCache cache,
    IOptions<ArrApiOptions> options,
    TimeProvider timeProvider,
    ILogger<NewznabSearchService> logger)
{
    internal const int MaxLimit = 500;
    internal const int DefaultLimit = 100;

    private static readonly TimeSpan _feedTimeout = TimeSpan.FromSeconds(5);

    internal async Task<SearchServiceResult> Search(IndexerRequest req, string baseUrl, string apiKey, CancellationToken ct)
    {
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
        var key = SearchResultCache.BuildKey(cmd);
        var timeout = TimeSpan.FromSeconds(options.Value.SearchTimeoutSeconds);

        try
        {
            var gateway = await registry.GetAsync<ISearchManager>(ct);
            var allItems = await cache.GetOrAddAsync(key, async () =>
            {
                var fullCmd = cmd with { Offset = null, Limit = null };
                var response = await gateway.Ask<SearchCommandResponse>(fullCmd, timeout, ct);
                return response is SearchCommandCompleted completed ? completed.Items : [];
            });

            var filtered = FilterBySeasonEpisode(allItems, cmd.Params as SearchCommand.TvParams);
            var paged = filtered.Skip(offset).Take(limit).ToArray();
            var rss = ToRss(paged, filtered.Length, offset, baseUrl, apiKey, category);
            return new SearchServiceResult.Success(rss);
        }
        catch (TimeoutException ex)
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
            return
            [
                .. items.Where(i =>
                    int.TryParse(i.Metadata?.Season, out var s) && s == season &&
                    int.TryParse(i.Metadata?.Episode, out var e) && e == episode)
            ];
        }

        return [.. items.Where(i => int.TryParse(i.Metadata?.Season, out var s) && s == season)];
    }

    internal static Rss ToRss(
        SearchResultItem[] items, int total, int offset,
        string baseUrl, string apiKey, NewznabCategory category) =>
        CreateRss([.. items.Select(item => ToItem(item, category, baseUrl, apiKey))], total, offset);

    internal static Rss ToFeedRss(FeedEntry[] entries, int total, int offset, string baseUrl, string apiKey) =>
        CreateRss(
        [
            .. entries.Select(entry => ToItem(
                entry.Item,
                entry.MediaType == MediaType.Movie ? NewznabCategory.Movie : NewznabCategory.Tv,
                baseUrl,
                apiKey,
                entry.StableId,
                entry.FirstSeenAt))
        ], total, offset);

    private static Rss CreateRss(List<Item> items, int total, int offset) =>
        new()
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = offset, Total = total },
                Items = items
            }
        };

    private static Item ToItem(
        SearchResultItem item, NewznabCategory category, string baseUrl, string apiKey,
        string? guid = null, DateTimeOffset? pubDate = null)
    {
        var nzbPayload = string.Join('\t',
            Strip(item.Title),
            Strip(item.Url),
            Strip(item.SubtitleUrl ?? ""),
            Strip(item.Channel),
            item.Duration.ToString(),
            item.Size.ToString(),
            category == NewznabCategory.Movie ? "movie" : "tv");
        var nzbId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(nzbPayload));
        var getNzbUrl = $"{baseUrl}/index/api?t=get&id={Uri.EscapeDataString(nzbId)}&apikey={Uri.EscapeDataString(apiKey)}";
        return new Item
        {
            Title = item.Title,
            Guid = new ItemGuid { Value = guid ?? nzbId, IsPermaLink = false },
            Link = getNzbUrl,
            PubDate = (pubDate ?? item.AiredAt)?.ToString("R") ?? "",
            Category = category.DisplayName(item.Quality),
            Description = $"{item.Channel} - {item.Topic}",
            Enclosure = new Enclosure { Url = getNzbUrl, Length = item.Size },
            Attributes = BuildAttributes(item, category)
        };
    }

    internal static List<NewznabAttribute> BuildAttributes(SearchResultItem item, NewznabCategory category)
    {
        var attrs = new List<NewznabAttribute>
        {
            new() { Name = "size", Value = item.Size.ToString() },
            new() { Name = "category", Value = category.CategoryId(item.Quality) }
        };

        if (item.Metadata?.Season is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "season", Value = item.Metadata.Season });
        }

        if (item.Metadata?.Episode is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "episode", Value = item.Metadata.Episode });
        }

        if (item.Metadata?.Ids?.TvdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "tvdbid", Value = item.Metadata.Ids.TvdbId.Value.ToString() });
        }

        if (item.Metadata?.Ids?.ImdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "imdb", Value = item.Metadata.Ids.ImdbId });
        }

        if (item.Metadata?.Ids?.TmdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "tmdbid", Value = item.Metadata.Ids.TmdbId.Value.ToString() });
        }

        return attrs;
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
            var feed = await registry.GetAsync<IFeedManager>(ct);
            var response = await feed.Ask<FeedResponse>(new QueryFeed(FeedMediaType(req), offset, limit), _feedTimeout, ct);
            feedAnswered = response is FeedResult;
            if (response is FeedResult { Total: > 0 } result)
            {
                return new SearchServiceResult.Success(ToFeedRss(result.Entries, result.Total, offset, baseUrl, apiKey));
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

    private static string Strip(string value) => value.Replace('\t', ' ');
}
