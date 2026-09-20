using FunkArr.Messages;

namespace FunkArr.Messages.Search;

public sealed record SearchMovie(
    Guid SearchId,
    SearchSource Source,
    string? Query,
    string? ImdbId,
    int? TmdbId,
    int? Limit,
    int? Offset) : SearchRequest(SearchId, Source, Query, Limit, Offset);

public abstract record SearchMovieResponse;

public sealed record SearchMovieCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total) : SearchMovieResponse;

public sealed record SearchMovieFailed(
    Guid SearchId,
    Exception Cause) : SearchMovieResponse;
