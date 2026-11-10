using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Messages.Search;

public sealed record EvaluateMovies(
    Guid SearchId,
    SearchSource Source,
    MediathekItem[] Items,
    string? ImdbId = null,
    int? TmdbId = null,
    string? RuleSetId = null,
    string? MediaName = null,
    EnrichmentConfig? Enrichment = null) : IWithSearchId;

public abstract record EvaluateMoviesResponse;

public sealed record EvaluateMoviesCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total) : EvaluateMoviesResponse;

public sealed record EvaluateMoviesFailed(
    Guid SearchId,
    Exception Cause) : EvaluateMoviesResponse;
