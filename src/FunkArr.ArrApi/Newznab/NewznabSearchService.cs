using Akka.Actor;
using Akka.Hosting;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi.Newznab;

public sealed class NewznabSearchService(
    IActorRegistry registry,
    SearchResultCache cache,
    IOptions<ArrApiOptions> options,
    ILogger<NewznabSearchService> logger)
{
    internal const int MaxLimit = 500;
    internal const int DefaultLimit = 100;

    internal async Task<SearchServiceResult> Search(IndexerRequest req, string baseUrl, string apiKey)
    {
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
            var gateway = await registry.GetAsync<ISearchManager>();
            var allItems = await cache.GetOrAddAsync(key, async () =>
            {
                var fullCmd = cmd with { Offset = null, Limit = null };
                var response = await gateway.Ask<SearchCommandResponse>(fullCmd, timeout);
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
            return items.Where(i =>
                int.TryParse(i.Season, out var s) && s == season &&
                int.TryParse(i.Episode, out var e) && e == episode).ToArray();
        }

        return items.Where(i => int.TryParse(i.Season, out var s) && s == season).ToArray();
    }

    internal static Rss ToRss(
        SearchResultItem[] items, int total, int offset,
        string baseUrl, string apiKey, NewznabCategory category)
    {
        var rssItems = items.Select(item =>
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
                Guid = new ItemGuid { Value = nzbId, IsPermaLink = false },
                Link = getNzbUrl,
                PubDate = item.AiredAt?.ToString("R") ?? "",
                Category = category.DisplayName(item.Quality),
                Description = $"{item.Channel} - {item.Topic}",
                Enclosure = new Enclosure { Url = getNzbUrl, Length = item.Size },
                Attributes = BuildAttributes(item, category),
            };
        }).ToList();

        return new Rss
        {
            Channel = new Channel
            {
                Response = new NewznabResponse { Offset = offset, Total = total },
                Items = rssItems,
            },
        };
    }

    internal static List<NewznabAttribute> BuildAttributes(SearchResultItem item, NewznabCategory category)
    {
        var attrs = new List<NewznabAttribute>
        {
            new() { Name = "size", Value = item.Size.ToString() },
            new() { Name = "category", Value = category.CategoryId(item.Quality) },
        };

        if (item.Season is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "season", Value = item.Season });
        }

        if (item.Episode is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "episode", Value = item.Episode });
        }

        if (item.TvdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "tvdbid", Value = item.TvdbId.Value.ToString() });
        }

        if (item.ImdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "imdb", Value = item.ImdbId });
        }

        if (item.TmdbId is not null)
        {
            attrs.Add(new NewznabAttribute { Name = "tmdbid", Value = item.TmdbId.Value.ToString() });
        }

        return attrs;
    }

    private static (SearchCommand? Cmd, NewznabCategory Category) BuildCommand(IndexerRequest req) =>
        (req.T ?? "") switch
        {
            "tvsearch" => BuildTvSearch(req),
            "movie" => BuildMovieSearch(req),
            "search" => BuildGeneralSearch(req),
            _ => (null, NewznabCategory.Tv),
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

    internal static int? CapLimit(int? limit) => limit switch
    {
        null => null,
        > MaxLimit => MaxLimit,
        _ => limit,
    };

    internal static int? ParseInt(string? value) =>
        int.TryParse(value, out var result) ? result : null;

    private static string Strip(string value) => value.Replace('\t', ' ');
}
