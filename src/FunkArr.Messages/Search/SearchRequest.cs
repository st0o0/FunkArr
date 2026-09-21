namespace FunkArr.Messages.Search;

public abstract record SearchRequest(
    Guid SearchId,
    SearchSource Source,
    string? Query,
    int? Limit,
    int? Offset) : IWithSearchId;
