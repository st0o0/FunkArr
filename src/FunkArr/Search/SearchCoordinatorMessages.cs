using FunkArr.RuleSet;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

internal sealed record ResolveTvShow(int TvdbId, int? Season);
internal sealed record TvShowResolved(string? ShowName, TvdbEpisodeInfo[]? Episodes);

internal sealed record ResolveMovie(string? ImdbId, string? SearchTerm);
internal sealed record MovieResolved(TmdbMovieInfo? Info);

internal sealed record FetchItems(string SearchTerm);
internal sealed record ItemsFetched(MediathekResultItem[] Items);

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
