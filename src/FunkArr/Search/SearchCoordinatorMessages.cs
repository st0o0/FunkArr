using FunkArr.RuleSet;
using FunkArr.Search.Matching;
using FunkArr.Search.Resolvers;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

public sealed record ResolveTvShow(int TvdbId, int? Season);
public sealed record TvShowResolved(string? ShowName, TvdbEpisodeInfo[]? Episodes);

public sealed record ResolveMovie(string? ImdbId, string? SearchTerm);
public sealed record MovieResolved(TmdbMovieInfo? Info);

internal sealed record CachedShow(string ShowName, long TimestampUtcTicks);
internal sealed record CachedMovie(string Title, string? OriginalTitle, int? RuntimeMinutes, long TimestampUtcTicks);

public sealed record FetchItems(string SearchTerm);
public sealed record ItemsFetched(MediathekResultItem[] Items);

internal sealed record MatchItems(
    MediathekResultItem[] Items,
    MatchContext Context,
    IReadOnlyList<Rule> Rules,
    TvdbEpisodeInfo[] TvdbEpisodes,
    string? ShowName);
internal sealed record ItemsMatched(IReadOnlyList<SearchResult> Results, MatchRecord? Record);

internal sealed record ProbeUrls(IReadOnlyList<SearchResult> Results, int ProbeLimit);
internal sealed record UrlsProbed(IReadOnlyList<SearchResult> Results);

internal sealed record ScoreResults(IReadOnlyList<SearchResult> Results, MatchContext Context);
internal sealed record ResultsScored(IReadOnlyList<SearchResult> Results);

public sealed record SearchResponse(IReadOnlyList<SearchResult> Results);
