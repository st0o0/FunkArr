namespace FunkArr.Messages.Search;

public abstract record SearchRequest(
    Guid SearchId,
    string Source,
    string? Query,
    int? Limit,
    int? Offset) : IWithSearchId;
