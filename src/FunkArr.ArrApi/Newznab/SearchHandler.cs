using Akka.Actor;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Messages;
using FunkArr.Messages.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FunkArr.ArrApi.Newznab;

internal sealed class SearchHandler(IActorRef gateway, SearchResultCache cache, string baseUrl, string apiKey, ILogger<SearchHandler> logger)
{
    private static readonly TimeSpan _searchTimeout = TimeSpan.FromSeconds(30);

    internal async Task<IResult> Handle(IndexerRequest req)
    {
        var (cmd, category) = BuildCommand(req);

        if (cmd is null)
        {
            return NewznabApiEndpoints.EmptyResult(req.Offset ?? 0);
        }

        var offset = req.Offset ?? 0;
        var limit = req.Limit ?? NewznabApiEndpoints.DefaultLimit;
        var key = SearchResultCache.BuildKey(cmd);

        try
        {
            var allItems = await cache.GetOrAddAsync(key, async () =>
            {
                var fullCmd = cmd with { Offset = null, Limit = null };
                var response = await gateway.Ask<SearchCommandResponse>(fullCmd, _searchTimeout);
                return response is SearchCommandCompleted completed ? completed.Items : [];
            });

            var paged = allItems.Skip(offset).Take(limit).ToArray();
            var completed = new SearchCommandCompleted(Guid.Empty, paged, allItems.Length);
            return NewznabApiEndpoints.XmlResult(
                NewznabApiEndpoints.Serialize(this.ToRss(completed, offset, limit, category)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search failed for {SearchType} query {Query}", cmd.Source, cmd.Query);
            return NewznabApiEndpoints.ErrorResult(NewznabError.UnknownError("Search timed out"));
        }
    }

    private (SearchCommand? Cmd, NewznabCategory Category) BuildCommand(IndexerRequest req)
    {
        return (req.T ?? "") switch
        {
            "tvsearch" => BuildTvSearch(req),
            "movie" => BuildMovieSearch(req),
            "search" => BuildGeneralSearch(req),
            _ => (null, NewznabCategory.Tv),
        };
    }

    private static (SearchCommand?, NewznabCategory) BuildTvSearch(IndexerRequest req)
    {
        var cmd = new SearchCommand(SearchSource.Sonarr, req.Q, null,
            NewznabApiEndpoints.CapLimit(req.Limit), req.Offset,
            new SearchCommand.TvParams(
                NewznabApiEndpoints.ParseInt(req.Season),
                NewznabApiEndpoints.ParseInt(req.Ep),
                NewznabApiEndpoints.ParseInt(req.TvdbId),
                req.ImdbId));

        return (cmd, NewznabCategory.Tv);
    }

    private static (SearchCommand?, NewznabCategory) BuildMovieSearch(IndexerRequest req)
    {
        var cmd = new SearchCommand(SearchSource.Radarr, req.Q, null,
            NewznabApiEndpoints.CapLimit(req.Limit), req.Offset,
            new SearchCommand.MovieParams(req.ImdbId, NewznabApiEndpoints.ParseInt(req.TmdbId)));

        return (cmd, NewznabCategory.Movie);
    }

    private static (SearchCommand?, NewznabCategory) BuildGeneralSearch(IndexerRequest req)
    {
        var cat = NewznabApiEndpoints.ParseInt(req.Cat);
        var category = NewznabCategory.FromCat(cat) ?? NewznabCategory.Tv;
        var cmd = new SearchCommand(SearchSource.Prowlarr, req.Q, cat, NewznabApiEndpoints.CapLimit(req.Limit), req.Offset, null);
        return (cmd, category);
    }

    internal Rss ToRss(SearchCommandCompleted completed, int offset, int limit, NewznabCategory category)
    {
        var paged = completed.Items.Skip(offset).Take(limit);

        var items = paged.Select(item =>
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
                Response = new NewznabResponse { Offset = offset, Total = completed.Total },
                Items = items,
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

    private static string Strip(string value) => value.Replace('\t', ' ');
}
