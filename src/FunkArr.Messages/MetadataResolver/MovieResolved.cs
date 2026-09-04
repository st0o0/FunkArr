namespace FunkArr.Messages.MetadataResolver;

public sealed record MovieResolved(
    int Index,
    string Title,
    int Year,
    string? ImdbId,
    int? TmdbId,
    float Confidence,
    string Strategy);
