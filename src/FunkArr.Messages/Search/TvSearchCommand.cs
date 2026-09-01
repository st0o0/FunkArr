namespace FunkArr.Messages.Search;

public sealed record TvSearchCommand(
    Guid SearchId,
    string? Query,
    int? Season,
    int? Episode,
    int? TvdbId,
    string? ImdbId,
    int? Limit,
    int? Offset) : IWithSearchId;
