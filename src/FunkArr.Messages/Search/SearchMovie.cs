namespace FunkArr.Messages.Search;

public sealed record SearchMovie(
    Guid SearchId,
    string Source,
    string? Query,
    string? ImdbId,
    int? TmdbId,
    int? Limit,
    int? Offset) : IWithSearchId;

public abstract record SearchMovieResponse;

public sealed record SearchMovieCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total) : SearchMovieResponse;

public sealed record SearchMovieFailed(
    Guid SearchId,
    Exception Cause) : SearchMovieResponse;
