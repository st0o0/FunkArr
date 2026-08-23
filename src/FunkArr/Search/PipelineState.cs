using Akka.Actor;
using FunkArr.RuleSet;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

internal sealed class PipelineState
{
    public required string CoalesceKey { get; init; }
    public required string SearchType { get; init; }
    public required string SearchTerm { get; set; }
    public List<PendingCaller> Callers { get; } = [];

    public string? ShowName { get; set; }
    public TvdbEpisodeInfo[]? Episodes { get; set; }
    public IReadOnlyList<Rule>? Rules { get; set; }
    public TmdbMovieInfo? MovieInfo { get; set; }
    public MediathekResultItem[]? RawItems { get; set; }
    public IReadOnlyList<SearchResult>? MatchedResults { get; set; }
    public MatchRecord? MatchRecord { get; set; }
    public IReadOnlyList<SearchResult>? ProbedResults { get; set; }

    public bool ShowResolved { get; set; }
    public bool RulesResolved { get; set; }
}

internal sealed record PendingCaller(IActorRef Ref, SearchCoordinator.TvSearchRequest? TvRequest,
    SearchCoordinator.MovieSearchRequest? MovieRequest, SearchCoordinator.TextSearchRequest? TextRequest);
