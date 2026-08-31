namespace FunkArr.Messages.Search;

public sealed record MovieSearchCommand(
    Guid SearchId,
    string? Query,
    string? ImdbId,
    int? TmdbId) : IWithSearchId;
