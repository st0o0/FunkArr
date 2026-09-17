namespace FunkArr.Messages.Enrichment;

public sealed record MovieCandidate(
    int Index,
    string Title,
    DateTimeOffset? AiredAt,
    int Duration);
