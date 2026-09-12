using Akka.Actor;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Messages.Search;
using Microsoft.AspNetCore.Http;

namespace FunkArr.ArrApi.Newznab;

internal sealed class SearchHandler(IActorRef gateway, string baseUrl, string apiKey)
{
    private static readonly TimeSpan _searchTimeout = TimeSpan.FromSeconds(30);

    internal async Task<IResult> Handle(IndexerRequest req)
    {
        var (cmd, category) = BuildCommand(req);

        if (cmd is null)
        {
            return NewznabApiEndpoints.EmptyResult(req.Offset ?? 0);
        }

        return await AskAndFormat(cmd, req.Offset ?? 0, req.Limit ?? NewznabApiEndpoints.DefaultLimit, category);
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
        var cmd = new SearchCommand("sonarr", req.Q, null,
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
        var cmd = new SearchCommand("radarr", req.Q, null,
            NewznabApiEndpoints.CapLimit(req.Limit), req.Offset,
            new SearchCommand.MovieParams(req.ImdbId, NewznabApiEndpoints.ParseInt(req.TmdbId)));

        return (cmd, NewznabCategory.Movie);
    }

    private static (SearchCommand?, NewznabCategory) BuildGeneralSearch(IndexerRequest req)
    {
        var cat = NewznabApiEndpoints.ParseInt(req.Cat);
        var category = NewznabCategory.FromCat(cat) ?? NewznabCategory.Tv;
        var cmd = new SearchCommand("search", req.Q, cat, NewznabApiEndpoints.CapLimit(req.Limit), req.Offset, null);
        return (cmd, category);
    }

    private async Task<IResult> AskAndFormat(SearchCommand cmd, int offset, int limit, NewznabCategory category)
    {
        try
        {
            var response = await gateway.Ask<ISearchResponse>(cmd, _searchTimeout);
            return response switch
            {
                SearchCompleted completed => NewznabApiEndpoints.XmlResult(
                    NewznabApiEndpoints.Serialize(this.ToRss(completed, offset, limit, category))),
                SearchFailed failed => NewznabApiEndpoints.ErrorResult(NewznabError.UnknownError(failed.Reason)),
                _ => NewznabApiEndpoints.ErrorResult(NewznabError.UnknownError("Unexpected response")),
            };
        }
        catch (Exception)
        {
            return NewznabApiEndpoints.ErrorResult(NewznabError.UnknownError("Search timed out"));
        }
    }

    internal Rss ToRss(SearchCompleted completed, int offset, int limit, NewznabCategory category)
    {
        var paged = completed.Items.Take(limit);

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
