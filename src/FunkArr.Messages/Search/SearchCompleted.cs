namespace FunkArr.Messages.Search;

public sealed record SearchCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total);
