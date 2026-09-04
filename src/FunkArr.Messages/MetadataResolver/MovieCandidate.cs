namespace FunkArr.Messages.MetadataResolver;

public sealed record MovieCandidate(
    int Index,
    string Title,
    DateTimeOffset? AiredAt,
    int Duration);
