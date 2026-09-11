namespace FunkArr.Messages.Search;

public sealed record MovieSearchCommand(
    Guid SearchId,
    string Source,
    string? Query,
    string? ImdbId,
    int? TmdbId,
    int? Limit,
    int? Offset) : IWithSearchId;
