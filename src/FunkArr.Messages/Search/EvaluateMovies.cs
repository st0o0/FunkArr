using FunkArr.Messages.Mediathek;
using FunkArr.Messages.RuleSet;

namespace FunkArr.Messages.Search;

public sealed record EvaluateMovies(
    Guid SearchId,
    SearchSource Source,
    MediathekItem[] Items,
    string? ImdbId = null,
    int? TmdbId = null,
    RuleSetResolved? RuleSet = null) : IWithSearchId;

public abstract record EvaluateMoviesResponse;

public sealed record EvaluateMoviesCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total) : EvaluateMoviesResponse;

public sealed record EvaluateMoviesFailed(
    Guid SearchId,
    Exception Cause) : EvaluateMoviesResponse;
