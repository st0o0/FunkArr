namespace FunkArr.Messages.Search;

public sealed record SearchFailed(
    Guid SearchId,
    string Reason);
